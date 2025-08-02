using System;
using System.Threading.Tasks;
using Npgsql;
using Shared.Services.Security;

namespace Database.Setup.Tools
{
    public class TenantCreator
    {
        private readonly string _centralConnectionString;
        private readonly DatabaseSeeder _seeder;
        private readonly IPasswordService _passwordService;

        public TenantCreator(string centralConnectionString)
        {
            _centralConnectionString = centralConnectionString;
            _seeder = new DatabaseSeeder(centralConnectionString);
            _passwordService = new PasswordService();
        }

        /// <summary>
        /// Crea un nuevo tenant completo en el sistema
        /// </summary>
        public async Task<CreateTenantResult> CreateTenantAsync(CreateTenantRequest request)
        {
            Console.WriteLine($"🏢 Creando nuevo tenant: {request.CompanyName}");
            
            try
            {
                // 1. Validar que el subdominio esté disponible
                await ValidateSubdomainAsync(request.Subdomain);
                
                // 2. Crear entrada en BD central
                var tenantId = await CreateTenantRecordAsync(request);
                
                // 3. Crear base de datos del tenant
                await _seeder.CreateTenantAsync(request.Subdomain);
                
                // 4. Configurar empresa y admin inicial
                await SetupTenantCompanyAsync(request.Subdomain, request);
                
                // 5. Verificar que todo esté correcto
                var isValid = await _seeder.VerifyTenantAsync(request.Subdomain);
                
                if (!isValid)
                {
                    throw new Exception("Error en la verificación del tenant creado");
                }

                Console.WriteLine($"✅ Tenant {request.CompanyName} creado exitosamente");
                
                return new CreateTenantResult
                {
                    Success = true,
                    TenantId = tenantId,
                    DatabaseName = $"SphereTimeControl_{request.Subdomain}",
                    AdminCredentials = new AdminCredentials
                    {
                        Email = request.AdminEmail,
                        TemporaryPassword = request.AdminPassword
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creando tenant: {ex.Message}");
                
                // Cleanup en caso de error
                try
                {
                    await CleanupFailedTenantAsync(request.Subdomain);
                }
                catch
                {
                    // Ignorar errores de cleanup
                }
                
                return new CreateTenantResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Valida que el subdominio esté disponible
        /// </summary>
        private async Task ValidateSubdomainAsync(string subdomain)
        {
            // Validar formato
            if (!IsValidSubdomain(subdomain))
            {
                throw new ArgumentException("El subdominio no tiene un formato válido");
            }

            // Verificar disponibilidad en BD central
            using var connection = new NpgsqlConnection(_centralConnectionString);
            await connection.OpenAsync();
            
            var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM \"Tenants\" WHERE \"Subdomain\" = @subdomain", 
                connection);
            command.Parameters.AddWithValue("subdomain", subdomain);
            
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            
            if (count > 0)
            {
                throw new InvalidOperationException($"El subdominio '{subdomain}' ya está en uso");
            }
        }

        /// <summary>
        /// Crea el registro del tenant en la BD central
        /// </summary>
        private async Task<int> CreateTenantRecordAsync(CreateTenantRequest request)
        {
            using var connection = new NpgsqlConnection(_centralConnectionString);
            await connection.OpenAsync();
            
            var command = new NpgsqlCommand(
                @"INSERT INTO ""Tenants"" 
                  (""Name"", ""Subdomain"", ""DatabaseName"", ""ContactEmail"", ""Phone"", 
                   ""LicenseType"", ""MaxEmployees"", ""LicenseExpiresAt"")
                  VALUES (@name, @subdomain, @dbname, @email, @phone, @licensetype, @maxemp, @expires)
                  RETURNING ""Id""", 
                connection);
            
            command.Parameters.AddWithValue("name", request.CompanyName);
            command.Parameters.AddWithValue("subdomain", request.Subdomain);
            command.Parameters.AddWithValue("dbname", $"SphereTimeControl_{request.Subdomain}");
            command.Parameters.AddWithValue("email", request.AdminEmail);
            command.Parameters.AddWithValue("phone", request.Phone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("licensetype", (int)request.LicenseType);
            command.Parameters.AddWithValue("maxemp", request.MaxEmployees);
            command.Parameters.AddWithValue("expires", request.LicenseExpiresAt);
            
            var tenantId = Convert.ToInt32(await command.ExecuteScalarAsync());
            
            // Crear licencia
            var licenseCommand = new NpgsqlCommand(
                @"INSERT INTO ""Licenses"" 
                  (""TenantId"", ""Type"", ""MaxEmployees"", ""MonthlyPrice"", ""ExpiresAt"")
                  VALUES (@tenantid, @type, @maxemp, @price, @expires)", 
                connection);
            
            licenseCommand.Parameters.AddWithValue("tenantid", tenantId);
            licenseCommand.Parameters.AddWithValue("type", (int)request.LicenseType);
            licenseCommand.Parameters.AddWithValue("maxemp", request.MaxEmployees);
            licenseCommand.Parameters.AddWithValue("price", request.MonthlyPrice);
            licenseCommand.Parameters.AddWithValue("expires", request.LicenseExpiresAt);
            
            await licenseCommand.ExecuteNonQueryAsync();
            
            return tenantId;
        }

        /// <summary>
        /// Configura la empresa y admin inicial del tenant
        /// </summary>
        private async Task SetupTenantCompanyAsync(string tenantId, CreateTenantRequest request)
        {
            var tenantConnectionString = _centralConnectionString
                .Replace("SphereTimeControl_Central", $"SphereTimeControl_{tenantId}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();
            
            // 1. Crear empresa
            var companyCommand = new NpgsqlCommand(
                @"INSERT INTO ""Companies"" 
                  (""Name"", ""Email"", ""Phone"", ""Address"")
                  VALUES (@name, @email, @phone, @address)
                  RETURNING ""Id""", 
                connection);
            
            companyCommand.Parameters.AddWithValue("name", request.CompanyName);
            companyCommand.Parameters.AddWithValue("email", request.AdminEmail);
            companyCommand.Parameters.AddWithValue("phone", request.Phone ?? (object)DBNull.Value);
            companyCommand.Parameters.AddWithValue("address", request.Address ?? (object)DBNull.Value);
            
            var companyId = Convert.ToInt32(await companyCommand.ExecuteScalarAsync());
            
            // 2. Crear departamento por defecto
            var deptCommand = new NpgsqlCommand(
                @"INSERT INTO ""Departments"" 
                  (""CompanyId"", ""Name"", ""Description"")
                  VALUES (@companyid, @name, @desc)
                  RETURNING ""Id""", 
                connection);
            
            deptCommand.Parameters.AddWithValue("companyid", companyId);
            deptCommand.Parameters.AddWithValue("name", "Administración");
            deptCommand.Parameters.AddWithValue("desc", "Departamento de administración general");
            
            var departmentId = Convert.ToInt32(await deptCommand.ExecuteScalarAsync());
            
            // 3. Crear administrador
            var passwordHash = _passwordService.HashPassword(request.AdminPassword);
            
            var adminCommand = new NpgsqlCommand(
                @"INSERT INTO ""Employees"" 
                  (""CompanyId"", ""DepartmentId"", ""FirstName"", ""LastName"", ""Email"", 
                   ""EmployeeCode"", ""Role"", ""PasswordHash"")
                  VALUES (@companyid, @deptid, @fname, @lname, @email, @code, @role, @password)", 
                connection);
            
            var nameParts = request.AdminName.Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";
            
            adminCommand.Parameters.AddWithValue("companyid", companyId);
            adminCommand.Parameters.AddWithValue("deptid", departmentId);
            adminCommand.Parameters.AddWithValue("fname", firstName);
            adminCommand.Parameters.AddWithValue("lname", lastName);
            adminCommand.Parameters.AddWithValue("email", request.AdminEmail);
            adminCommand.Parameters.AddWithValue("code", "ADMIN001");
            adminCommand.Parameters.AddWithValue("role", 1); // CompanyAdmin
            adminCommand.Parameters.AddWithValue("password", passwordHash);
            
            await adminCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Limpia un tenant que falló al crearse
        /// </summary>
        private async Task CleanupFailedTenantAsync(string subdomain)
        {
            Console.WriteLine($"🧹 Limpiando tenant fallido: {subdomain}");
            
            // Eliminar de BD central
            using var connection = new NpgsqlConnection(_centralConnectionString);
            await connection.OpenAsync();
            
            var command = new NpgsqlCommand(
                "DELETE FROM \"Tenants\" WHERE \"Subdomain\" = @subdomain", 
                connection);
            command.Parameters.AddWithValue("subdomain", subdomain);
            
            await command.ExecuteNonQueryAsync();
            
            // Eliminar BD del tenant
            try
            {
                await _seeder.DropTenantAsync(subdomain);
            }
            catch
            {
                // Ignorar errores
            }
        }

        /// <summary>
        /// Valida formato de subdominio
        /// </summary>
        private static bool IsValidSubdomain(string subdomain)
        {
            if (string.IsNullOrWhiteSpace(subdomain) || subdomain.Length < 3 || subdomain.Length > 50)
                return false;
            
            // Solo letras, números y guiones
            if (!System.Text.RegularExpressions.Regex.IsMatch(subdomain, @"^[a-z0-9-]+$"))
                return false;
            
            // No puede empezar o terminar con guión
            if (subdomain.StartsWith("-") || subdomain.EndsWith("-"))
                return false;
            
            // Subdominios reservados
            var reserved = new[] { "www", "api", "admin", "mail", "ftp", "test", "dev", "stage", "prod", "demo" };
            if (reserved.Contains(subdomain.ToLower()))
                return false;
            
            return true;
        }
    }

    // DTOs para crear tenants
    public class CreateTenantRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminPassword { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public Shared.Models.Enums.LicenseType LicenseType { get; set; } = Shared.Models.Enums.LicenseType.Trial;
        public int MaxEmployees { get; set; } = 10;
        public decimal MonthlyPrice { get; set; } = 0;
        public DateTime LicenseExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
    }

    public class CreateTenantResult
    {
        public bool Success { get; set; }
        public int TenantId { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public AdminCredentials? AdminCredentials { get; set; }
    }

    public class AdminCredentials
    {
        public string Email { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}