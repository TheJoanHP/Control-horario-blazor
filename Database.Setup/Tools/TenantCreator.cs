using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Logging;
using Shared.Models.Enums;

namespace Database.Setup.Tools
{
    /// <summary>
    /// Utilidad para crear nuevos tenants en el sistema
    /// </summary>
    public class TenantCreator
    {
        private readonly string _connectionString;
        private readonly string _scriptsPath;
        private readonly ILogger<TenantCreator>? _logger;

        public TenantCreator(string connectionString, string? scriptsPath = null, ILogger<TenantCreator>? logger = null)
        {
            _connectionString = connectionString;
            _scriptsPath = scriptsPath ?? "Scripts";
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo tenant completo
        /// </summary>
        /// <param name="tenantId">ID único del tenant</param>
        /// <param name="companyName">Nombre de la empresa</param>
        /// <param name="adminEmail">Email del administrador</param>
        /// <param name="adminPassword">Contraseña del administrador</param>
        public async Task CreateTenantAsync(
            string tenantId, 
            string? companyName = null, 
            string? adminEmail = null, 
            string adminPassword = "admin123")
        {
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("El ID del tenant es requerido", nameof(tenantId));

            // Sanitizar tenant ID
            tenantId = SanitizeTenantId(tenantId);
            
            // Valores por defecto si no se proporcionan
            companyName ??= $"Empresa {tenantId.ToUpper()}";
            adminEmail ??= $"admin@{tenantId.ToLower()}.com";

            Console.WriteLine($"🏢 Creando tenant: {tenantId}");
            Console.WriteLine($"   Empresa: {companyName}");
            Console.WriteLine($"   Admin: {adminEmail}");
            Console.WriteLine("=====================================");

            try
            {
                // 1. Verificar que no existe
                await ValidateTenantNotExistsAsync(tenantId);

                // 2. Crear registro en la BD central
                await CreateTenantRecordAsync(tenantId, companyName, adminEmail);

                // 3. Crear base de datos del tenant
                var tenantDbName = $"SphereTimeControl_{tenantId}";
                await CreateTenantDatabaseAsync(tenantDbName);

                // 4. Crear estructura de tablas
                await CreateTenantStructureAsync(tenantDbName);

                // 5. Insertar datos iniciales
                await SeedTenantDataAsync(tenantDbName, tenantId, companyName, adminEmail, adminPassword);

                Console.WriteLine("✅ ¡Tenant creado exitosamente!");
                Console.WriteLine($"📊 Database: {tenantDbName}");
                Console.WriteLine($"🔗 URL: https://{tenantId}.tudominio.com");
                Console.WriteLine($"👤 Admin: {adminEmail} / {adminPassword}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creando tenant: {ex.Message}");
                _logger?.LogError(ex, "Error creando tenant {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Sanitiza el ID del tenant para que sea válido como nombre de BD
        /// </summary>
        private string SanitizeTenantId(string tenantId)
        {
            // Convertir a minúsculas y quitar caracteres especiales
            var sanitized = tenantId.ToLowerInvariant()
                .Replace(" ", "")
                .Replace("-", "_")
                .Replace(".", "_");

            // Solo permitir letras, números y guiones bajos
            var result = "";
            foreach (char c in sanitized)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    result += c;
                }
            }

            // Asegurar que empiece con letra
            if (result.Length > 0 && !char.IsLetter(result[0]))
            {
                result = "t" + result;
            }

            return result;
        }

        /// <summary>
        /// Validar que el tenant no existe ya
        /// </summary>
        private async Task ValidateTenantNotExistsAsync(string tenantId)
        {
            var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
            
            try
            {
                using var connection = new NpgsqlConnection(centralConnectionString);
                await connection.OpenAsync();

                var query = @"SELECT COUNT(*) FROM ""Tenants"" WHERE ""Code"" = @tenantId OR ""Subdomain"" = @tenantId";
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("tenantId", tenantId);

                var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
                
                if (count > 0)
                {
                    throw new InvalidOperationException($"El tenant '{tenantId}' ya existe");
                }
            }
            catch (NpgsqlException ex) when (ex.SqlState == "3D000") // Database does not exist
            {
                // Si la BD central no existe, está bien, la crearemos después
                _logger?.LogWarning("Base de datos central no existe, se creará automáticamente");
            }
        }

        /// <summary>
        /// Crear registro del tenant en la base de datos central
        /// </summary>
        private async Task CreateTenantRecordAsync(string tenantId, string companyName, string adminEmail)
        {
            var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
            
            using var connection = new NpgsqlConnection(centralConnectionString);
            await connection.OpenAsync();

            var insertQuery = @"
                INSERT INTO ""Tenants"" (
                    ""Code"", ""Name"", ""Subdomain"", ""ContactEmail"", ""DatabaseName"", 
                    ""Active"", ""LicenseType"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @code, @name, @subdomain, @contactEmail, @databaseName, 
                    @active, @licenseType, @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("code", tenantId);
            cmd.Parameters.AddWithValue("name", companyName);
            cmd.Parameters.AddWithValue("subdomain", tenantId);
            cmd.Parameters.AddWithValue("contactEmail", adminEmail);
            cmd.Parameters.AddWithValue("databaseName", $"SphereTimeControl_{tenantId}");
            cmd.Parameters.AddWithValue("active", true);
            cmd.Parameters.AddWithValue("licenseType", (int)LicenseType.Trial);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var tenantDbId = await cmd.ExecuteScalarAsync();
            
            // Crear licencia por defecto para el tenant
            await CreateDefaultLicenseAsync(connection, Convert.ToInt32(tenantDbId));
            
            Console.WriteLine($"📝 Tenant registrado en BD central con ID: {tenantDbId}");
        }

        /// <summary>
        /// Crear licencia por defecto para el tenant
        /// </summary>
        private async Task CreateDefaultLicenseAsync(NpgsqlConnection connection, int tenantId)
        {
            var insertQuery = @"
                INSERT INTO ""Licenses"" (
                    ""TenantId"", ""LicenseType"", ""MaxEmployees"", ""HasReports"", ""HasAPI"", ""HasMobileApp"", 
                    ""MonthlyPrice"", ""StartDate"", ""EndDate"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @tenantId, @licenseType, @maxEmployees, @hasReports, @hasAPI, @hasMobileApp, 
                    @monthlyPrice, @startDate, @endDate, @active, @createdAt, @updatedAt
                )";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("licenseType", (int)LicenseType.Trial);
            cmd.Parameters.AddWithValue("maxEmployees", 5);
            cmd.Parameters.AddWithValue("hasReports", false);
            cmd.Parameters.AddWithValue("hasAPI", false);
            cmd.Parameters.AddWithValue("hasMobileApp", true);
            cmd.Parameters.AddWithValue("monthlyPrice", 0m);
            cmd.Parameters.AddWithValue("startDate", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("endDate", DateTime.UtcNow.AddDays(30)); // 30 días de trial
            cmd.Parameters.AddWithValue("active", true);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
            
            Console.WriteLine("📜 Licencia trial creada (30 días, 5 empleados)");
        }

        /// <summary>
        /// Crear la base de datos del tenant
        /// </summary>
        private async Task CreateTenantDatabaseAsync(string databaseName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Verificar si la BD ya existe
            var checkQuery = @"SELECT 1 FROM pg_database WHERE datname = @databaseName";
            using var checkCmd = new NpgsqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("databaseName", databaseName);
            
            var exists = await checkCmd.ExecuteScalarAsync() != null;
            
            if (!exists)
            {
                var createQuery = $@"CREATE DATABASE ""{databaseName}"" WITH ENCODING = 'UTF8'";
                using var createCmd = new NpgsqlCommand(createQuery, connection);
                await createCmd.ExecuteNonQueryAsync();
                
                Console.WriteLine($"🗄️ Base de datos creada: {databaseName}");
            }
            else
            {
                Console.WriteLine($"🗄️ Base de datos ya existe: {databaseName}");
            }
        }

        /// <summary>
        /// Crear estructura de tablas en la BD del tenant
        /// </summary>
        private async Task CreateTenantStructureAsync(string databaseName)
        {
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            // Leer y ejecutar script de estructura
            var scriptPath = Path.Combine(_scriptsPath, "02-CreateTenantTemplate.sql");
            
            if (File.Exists(scriptPath))
            {
                var script = await File.ReadAllTextAsync(scriptPath);
                // Reemplazar placeholder si existe
                script = script.Replace("{TENANT_ID}", databaseName.Replace("SphereTimeControl_", ""));
                
                using var cmd = new NpgsqlCommand(script, connection);
                await cmd.ExecuteNonQueryAsync();
                
                Console.WriteLine("🏗️ Estructura de tablas creada");
            }
            else
            {
                // Crear estructura básica si no hay script
                await CreateBasicTenantStructureAsync(connection);
            }
        }

        /// <summary>
        /// Crear estructura básica si no hay script disponible
        /// </summary>
        private async Task CreateBasicTenantStructureAsync(NpgsqlConnection connection)
        {
            var createTablesScript = @"
                -- Extensiones
                CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";
                CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";

                -- Tabla Companies
                CREATE TABLE IF NOT EXISTS ""Companies"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""TaxId"" VARCHAR(20),
                    ""Address"" VARCHAR(255),
                    ""Phone"" VARCHAR(20),
                    ""Email"" VARCHAR(255),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""WorkStartTime"" TIME NOT NULL DEFAULT '09:00:00',
                    ""WorkEndTime"" TIME NOT NULL DEFAULT '17:00:00',
                    ""ToleranceMinutes"" INTEGER NOT NULL DEFAULT 15,
                    ""VacationDaysPerYear"" INTEGER NOT NULL DEFAULT 22,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Tabla Departments
                CREATE TABLE IF NOT EXISTS ""Departments"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL REFERENCES ""Companies""(""Id""),
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Description"" VARCHAR(255),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Tabla Employees
                CREATE TABLE IF NOT EXISTS ""Employees"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL REFERENCES ""Companies""(""Id""),
                    ""DepartmentId"" INTEGER REFERENCES ""Departments""(""Id""),
                    ""FirstName"" VARCHAR(50) NOT NULL,
                    ""LastName"" VARCHAR(50) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                    ""Phone"" VARCHAR(20),
                    ""EmployeeCode"" VARCHAR(20) NOT NULL UNIQUE,
                    ""Position"" VARCHAR(100),
                    ""Role"" INTEGER NOT NULL DEFAULT 3,
                    ""PasswordHash"" VARCHAR(255) NOT NULL,
                    ""HireDate"" DATE,
                    ""Salary"" DECIMAL(10,2),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Tabla TimeRecords
                CREATE TABLE IF NOT EXISTS ""TimeRecords"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL REFERENCES ""Employees""(""Id""),
                    ""RecordType"" INTEGER NOT NULL, -- 0=CheckIn, 1=CheckOut, 2=BreakStart, 3=BreakEnd
                    ""RecordDateTime"" TIMESTAMP NOT NULL,
                    ""Location"" VARCHAR(255),
                    ""Notes"" VARCHAR(500),
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Tabla WorkSchedules
                CREATE TABLE IF NOT EXISTS ""WorkSchedules"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL REFERENCES ""Employees""(""Id""),
                    ""DayOfWeek"" INTEGER NOT NULL, -- 0=Sunday, 1=Monday, etc.
                    ""StartTime"" TIME NOT NULL,
                    ""EndTime"" TIME NOT NULL,
                    ""IsWorkingDay"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Tabla VacationRequests
                CREATE TABLE IF NOT EXISTS ""VacationRequests"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL REFERENCES ""Employees""(""Id""),
                    ""StartDate"" DATE NOT NULL,
                    ""EndDate"" DATE NOT NULL,
                    ""DaysRequested"" INTEGER NOT NULL,
                    ""Reason"" VARCHAR(500),
                    ""Status"" INTEGER NOT NULL DEFAULT 0, -- 0=Pending, 1=Approved, 2=Rejected
                    ""ApprovedById"" INTEGER REFERENCES ""Employees""(""Id""),
                    ""ApprovedAt"" TIMESTAMP,
                    ""Comments"" VARCHAR(500),
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Índices
                CREATE INDEX IF NOT EXISTS ""IX_Companies_Active"" ON ""Companies"" (""Active"");
                CREATE INDEX IF NOT EXISTS ""IX_Departments_CompanyId"" ON ""Departments"" (""CompanyId"");
                CREATE INDEX IF NOT EXISTS ""IX_Employees_CompanyId"" ON ""Employees"" (""CompanyId"");
                CREATE INDEX IF NOT EXISTS ""IX_Employees_DepartmentId"" ON ""Employees"" (""DepartmentId"");
                CREATE INDEX IF NOT EXISTS ""IX_Employees_Email"" ON ""Employees"" (""Email"");
                CREATE INDEX IF NOT EXISTS ""IX_Employees_EmployeeCode"" ON ""Employees"" (""EmployeeCode"");
                CREATE INDEX IF NOT EXISTS ""IX_TimeRecords_EmployeeId"" ON ""TimeRecords"" (""EmployeeId"");
                CREATE INDEX IF NOT EXISTS ""IX_TimeRecords_RecordDateTime"" ON ""TimeRecords"" (""RecordDateTime"");
                CREATE INDEX IF NOT EXISTS ""IX_WorkSchedules_EmployeeId"" ON ""WorkSchedules"" (""EmployeeId"");
                CREATE INDEX IF NOT EXISTS ""IX_VacationRequests_EmployeeId"" ON ""VacationRequests"" (""EmployeeId"");
                CREATE INDEX IF NOT EXISTS ""IX_VacationRequests_Status"" ON ""VacationRequests"" (""Status"");
            ";

            using var cmd = new NpgsqlCommand(createTablesScript, connection);
            await cmd.ExecuteNonQueryAsync();
            
            Console.WriteLine("🏗️ Estructura básica de tablas creada");
        }

        /// <summary>
        /// Sembrar datos iniciales en el tenant
        /// </summary>
        private async Task SeedTenantDataAsync(string databaseName, string tenantId, string companyName, string adminEmail, string adminPassword)
        {
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            // Leer y ejecutar script de datos si existe
            var scriptPath = Path.Combine(_scriptsPath, "03-SeedTenantData.sql");
            
            if (File.Exists(scriptPath))
            {
                var script = await File.ReadAllTextAsync(scriptPath);
                // Reemplazar placeholders
                script = script.Replace("{COMPANY_NAME}", companyName);
                script = script.Replace("{ADMIN_EMAIL}", adminEmail);
                script = script.Replace("{ADMIN_PASSWORD_HASH}", BCrypt.Net.BCrypt.HashPassword(adminPassword));
                
                using var cmd = new NpgsqlCommand(script, connection);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                // Crear datos básicos si no hay script
                await CreateBasicTenantDataAsync(connection, companyName, adminEmail, adminPassword);
            }
            
            Console.WriteLine("🌱 Datos iniciales sembrados");
        }

        /// <summary>
        /// Crear datos básicos del tenant si no hay script disponible
        /// </summary>
        private async Task CreateBasicTenantDataAsync(NpgsqlConnection connection, string companyName, string adminEmail, string adminPassword)
        {
            // Crear empresa
            var insertCompanyQuery = @"
                INSERT INTO ""Companies"" (""Name"", ""Email"", ""Active"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@name, @email, @active, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var companyCmd = new NpgsqlCommand(insertCompanyQuery, connection);
            companyCmd.Parameters.AddWithValue("name", companyName);
            companyCmd.Parameters.AddWithValue("email", adminEmail);
            companyCmd.Parameters.AddWithValue("active", true);
            companyCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            companyCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var companyId = Convert.ToInt32(await companyCmd.ExecuteScalarAsync());

            // Crear departamento por defecto
            var insertDeptQuery = @"
                INSERT INTO ""Departments"" (""CompanyId"", ""Name"", ""Description"", ""Active"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@companyId, @name, @description, @active, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var deptCmd = new NpgsqlCommand(insertDeptQuery, connection);
            deptCmd.Parameters.AddWithValue("companyId", companyId);
            deptCmd.Parameters.AddWithValue("name", "Administración");
            deptCmd.Parameters.AddWithValue("description", "Departamento de administración");
            deptCmd.Parameters.AddWithValue("active", true);
            deptCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            deptCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var deptId = Convert.ToInt32(await deptCmd.ExecuteScalarAsync());

            // Crear administrador
            var insertAdminQuery = @"
                INSERT INTO ""Employees"" (
                    ""CompanyId"", ""DepartmentId"", ""FirstName"", ""LastName"", ""Email"", 
                    ""EmployeeCode"", ""Role"", ""PasswordHash"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @deptId, @firstName, @lastName, @email, 
                    @employeeCode, @role, @passwordHash, @active, @createdAt, @updatedAt
                )";

            using var adminCmd = new NpgsqlCommand(insertAdminQuery, connection);
            adminCmd.Parameters.AddWithValue("companyId", companyId);
            adminCmd.Parameters.AddWithValue("deptId", deptId);
            adminCmd.Parameters.AddWithValue("firstName", "Admin");
            adminCmd.Parameters.AddWithValue("lastName", "User");
            adminCmd.Parameters.AddWithValue("email", adminEmail);
            adminCmd.Parameters.AddWithValue("employeeCode", "ADMIN001");
            adminCmd.Parameters.AddWithValue("role", (int)UserRole.CompanyAdmin);
            adminCmd.Parameters.AddWithValue("passwordHash", BCrypt.Net.BCrypt.HashPassword(adminPassword));
            adminCmd.Parameters.AddWithValue("active", true);
            adminCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            adminCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            await adminCmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Eliminar un tenant completamente
        /// </summary>
        public async Task DeleteTenantAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("El ID del tenant es requerido", nameof(tenantId));

            tenantId = SanitizeTenantId(tenantId);
            var databaseName = $"SphereTimeControl_{tenantId}";

            Console.WriteLine($"🗑️ Eliminando tenant: {tenantId}");

            try
            {
                // 1. Eliminar base de datos del tenant
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var dropQuery = $@"DROP DATABASE IF EXISTS ""{databaseName}""";
                using var dropCmd = new NpgsqlCommand(dropQuery, connection);
                await dropCmd.ExecuteNonQueryAsync();

                Console.WriteLine($"🗄️ Base de datos eliminada: {databaseName}");

                // 2. Eliminar registro de la BD central
                var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
                
                using var centralConnection = new NpgsqlConnection(centralConnectionString);
                await centralConnection.OpenAsync();

                var deleteQuery = @"DELETE FROM ""Tenants"" WHERE ""Code"" = @tenantId";
                using var deleteCmd = new NpgsqlCommand(deleteQuery, centralConnection);
                deleteCmd.Parameters.AddWithValue("tenantId", tenantId);

                var deleted = await deleteCmd.ExecuteNonQueryAsync();
                
                if (deleted > 0)
                {
                    Console.WriteLine("📝 Registro eliminado de BD central");
                    Console.WriteLine("✅ Tenant eliminado exitosamente");
                }
                else
                {
                    Console.WriteLine("⚠️ No se encontró el tenant en la BD central");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error eliminando tenant: {ex.Message}");
                _logger?.LogError(ex, "Error eliminando tenant {TenantId}", tenantId);
                throw;
            }
        }

        /// <summary>
        /// Listar todos los tenants existentes
        /// </summary>
        public async Task ListTenantsAsync()
        {
            var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
            
            try
            {
                using var connection = new NpgsqlConnection(centralConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT t.""Code"", t.""Name"", t.""ContactEmail"", t.""Active"", 
                           l.""LicenseType"", l.""MaxEmployees"", l.""EndDate""
                    FROM ""Tenants"" t
                    LEFT JOIN ""Licenses"" l ON t.""Id"" = l.""TenantId""
                    ORDER BY t.""CreatedAt""";

                using var cmd = new NpgsqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                Console.WriteLine("\n📋 Tenants registrados:");
                Console.WriteLine("┌──────────────┬────────────────────────┬───────────────────────────┬────────┬─────────┬──────────┬────────────┐");
                Console.WriteLine("│ Code         │ Name                   │ Email                     │ Active │ License │ Employees│ Expires    │");
                Console.WriteLine("├──────────────┼────────────────────────┼───────────────────────────┼────────┼─────────┼──────────┼────────────┤");

                var hasResults = false;
                while (await reader.ReadAsync())
                {
                    hasResults = true;
                    var code = reader.GetString(0).PadRight(12);
                    var name = reader.GetString(1).Length > 22 ? reader.GetString(1).Substring(0, 19) + "..." : reader.GetString(1).PadRight(22);
                    var email = reader.GetString(2).Length > 25 ? reader.GetString(2).Substring(0, 22) + "..." : reader.GetString(2).PadRight(25);
                    var active = (reader.GetBoolean(3) ? "✅" : "❌").PadRight(6);
                    var license = (!reader.IsDBNull(4) ? ((LicenseType)reader.GetInt32(4)).ToString() : "N/A").PadRight(7);
                    var employees = (!reader.IsDBNull(5) ? reader.GetInt32(5).ToString() : "N/A").PadRight(8);
                    var expires = (!reader.IsDBNull(6) ? reader.GetDateTime(6).ToString("yyyy-MM-dd") : "N/A").PadRight(10);

                    Console.WriteLine($"│ {code} │ {name} │ {email} │ {active} │ {license} │ {employees} │ {expires} │");
                }

                Console.WriteLine("└──────────────┴────────────────────────┴───────────────────────────┴────────┴─────────┴──────────┴────────────┘");

                if (!hasResults)
                {
                    Console.WriteLine("│                                    No hay tenants registrados                                    │");
                    Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────────────────────┘");
                }
            }
            catch (NpgsqlException ex) when (ex.SqlState == "3D000")
            {
                Console.WriteLine("⚠️ La base de datos central no existe. Ejecuta 'setup' primero.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error listando tenants: {ex.Message}");
                _logger?.LogError(ex, "Error listando tenants");
                throw;
            }
        }
    }
}