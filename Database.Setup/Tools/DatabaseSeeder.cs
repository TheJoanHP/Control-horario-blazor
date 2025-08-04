using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models.Core;
using Shared.Models.Enums;
using Shared.Services.Security;
using Npgsql;

namespace Database.Setup.Tools
{
    /// <summary>
    /// Herramienta para sembrar datos iniciales en las bases de datos
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly ILogger<DatabaseSeeder> _logger;
        private readonly IPasswordService _passwordService;
        private readonly string _connectionString;

        public DatabaseSeeder(ILogger<DatabaseSeeder> logger, IPasswordService passwordService, string connectionString)
        {
            _logger = logger;
            _passwordService = passwordService;
            _connectionString = connectionString;
        }

        /// <summary>
        /// Sembrar todos los datos necesarios
        /// </summary>
        public async Task SeedAllAsync()
        {
            _logger.LogInformation("🌱 Iniciando siembra completa de datos");

            try
            {
                await CreateCentralDatabaseAsync();
                await CreateDemoTenantAsync();
                
                _logger.LogInformation("✅ Siembra completa finalizada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error durante la siembra completa");
                throw;
            }
        }

        /// <summary>
        /// Crear y sembrar la base de datos central
        /// </summary>
        public async Task CreateCentralDatabaseAsync()
        {
            _logger.LogInformation("📦 Creando base de datos central de Sphere");

            try
            {
                // Crear la base de datos central si no existe
                await CreateDatabaseIfNotExistsAsync("SphereTimeControl_Central");
                
                // Conectar a la BD central y sembrar datos
                var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
                
                using var connection = new NpgsqlConnection(centralConnectionString);
                await connection.OpenAsync();

                // Ejecutar script de estructura si es necesario
                await ExecuteSqlScriptAsync(connection, "Scripts/01-CreateCentralDB.sql");

                // Sembrar datos iniciales
                await SeedCentralDataAsync(connection);

                _logger.LogInformation("✅ Base de datos central creada y sembrada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando base de datos central");
                throw;
            }
        }

        /// <summary>
        /// Crear tenant de demostración
        /// </summary>
        public async Task CreateDemoTenantAsync()
        {
            _logger.LogInformation("🏢 Creando tenant de demostración");

            try
            {
                var tenantCreator = new TenantCreator(_connectionString);
                await tenantCreator.CreateTenantAsync("demo", "Empresa Demo", "admin@demo.com");
                
                _logger.LogInformation("✅ Tenant demo creado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando tenant demo");
                throw;
            }
        }

        /// <summary>
        /// Sembrar datos iniciales en la base de datos central
        /// </summary>
        private async Task SeedCentralDataAsync(NpgsqlConnection connection)
        {
            _logger.LogInformation("🌱 Sembrando datos en BD central");

            // Crear super administrador por defecto
            await CreateSuperAdminAsync(connection);
            
            // Crear configuraciones del sistema
            await CreateSystemConfigurationsAsync(connection);
        }

        /// <summary>
        /// Crear super administrador por defecto
        /// </summary>
        private async Task CreateSuperAdminAsync(NpgsqlConnection connection)
        {
            var checkQuery = @"SELECT COUNT(*) FROM ""SphereAdmins"" WHERE ""Email"" = @email";
            
            using var checkCmd = new NpgsqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("email", "admin@spheretimecontrol.com");
            
            var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
            
            if (!exists)
            {
                var passwordHash = _passwordService.HashPassword("admin123");
                
                var insertQuery = @"
                    INSERT INTO ""SphereAdmins"" (""FirstName"", ""LastName"", ""Email"", ""PasswordHash"", ""Active"", ""CreatedAt"", ""UpdatedAt"")
                    VALUES (@firstName, @lastName, @email, @passwordHash, @active, @createdAt, @updatedAt)";

                using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("firstName", "Super");
                insertCmd.Parameters.AddWithValue("lastName", "Admin");
                insertCmd.Parameters.AddWithValue("email", "admin@spheretimecontrol.com");
                insertCmd.Parameters.AddWithValue("passwordHash", passwordHash);
                insertCmd.Parameters.AddWithValue("active", true);
                insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                await insertCmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("👤 Super administrador creado: admin@spheretimecontrol.com");
            }
            else
            {
                _logger.LogInformation("👤 Super administrador ya existe");
            }
        }

        /// <summary>
        /// Crear configuraciones del sistema
        /// </summary>
        private async Task CreateSystemConfigurationsAsync(NpgsqlConnection connection)
        {
            var configs = new Dictionary<string, string>
            {
                { "SystemName", "Sphere Time Control" },
                { "SystemVersion", "1.0.0" },
                { "DefaultLicenseType", "Trial" },
                { "TrialDurationDays", "30" },
                { "MaxEmployeesBasic", "10" },
                { "MaxEmployeesProfessional", "50" },
                { "MaxEmployeesEnterprise", "999" },
                { "MaintenanceMode", "false" }
            };

            foreach (var config in configs)
            {
                var checkQuery = @"SELECT COUNT(*) FROM ""SystemConfigs"" WHERE ""Key"" = @key";
                
                using var checkCmd = new NpgsqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("key", config.Key);
                
                var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
                
                if (!exists)
                {
                    var insertQuery = @"
                        INSERT INTO ""SystemConfigs"" (""Key"", ""Value"", ""CreatedAt"", ""UpdatedAt"")
                        VALUES (@key, @value, @createdAt, @updatedAt)";

                    using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                    insertCmd.Parameters.AddWithValue("key", config.Key);
                    insertCmd.Parameters.AddWithValue("value", config.Value);
                    insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                    insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            
            _logger.LogInformation("⚙️ Configuraciones del sistema creadas");
        }

        /// <summary>
        /// Sembrar datos de empresa en un tenant
        /// </summary>
        public async Task SeedTenantDataAsync(string databaseName, string tenantCode, string companyName, string adminEmail, string adminPassword)
        {
            _logger.LogInformation("🏢 Sembrando datos en tenant {TenantCode}", tenantCode);

            try
            {
                var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
                
                using var connection = new NpgsqlConnection(tenantConnectionString);
                await connection.OpenAsync();

                // Crear datos de la empresa
                await CreateCompanyDataAsync(connection, companyName);
                
                // Crear departamentos por defecto
                await CreateDefaultDepartmentsAsync(connection);
                
                // Crear administrador de la empresa
                await CreateCompanyAdminAsync(connection, adminEmail, adminPassword);
                
                // Crear empleados de demostración
                await CreateDemoEmployeesAsync(connection);

                _logger.LogInformation("✅ Datos del tenant sembrados exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sembrando datos del tenant {TenantCode}", tenantCode);
                throw;
            }
        }

        /// <summary>
        /// Crear datos de la empresa
        /// </summary>
        private async Task CreateCompanyDataAsync(NpgsqlConnection connection, string companyName)
        {
            var checkQuery = @"SELECT COUNT(*) FROM ""Companies""";
            
            using var checkCmd = new NpgsqlCommand(checkQuery, connection);
            var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
            
            if (!exists)
            {
                var insertQuery = @"
                    INSERT INTO ""Companies"" (""Name"", ""TaxId"", ""Address"", ""Phone"", ""Email"", ""Active"", ""CreatedAt"", ""UpdatedAt"")
                    VALUES (@name, @taxId, @address, @phone, @email, @active, @createdAt, @updatedAt)";

                using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("name", companyName);
                insertCmd.Parameters.AddWithValue("taxId", "B12345678");
                insertCmd.Parameters.AddWithValue("address", "Calle Principal 123, Madrid, España");
                insertCmd.Parameters.AddWithValue("phone", "+34 911 234 567");
                insertCmd.Parameters.AddWithValue("email", $"info@{companyName.ToLower().Replace(" ", "")}.com");
                insertCmd.Parameters.AddWithValue("active", true);
                insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                await insertCmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("🏢 Company data created: {CompanyName}", companyName);
            }
        }

        /// <summary>
        /// Crear departamentos por defecto
        /// </summary>
        private async Task CreateDefaultDepartmentsAsync(NpgsqlConnection connection)
        {
            var departments = new[]
            {
                ("Administración", "Departamento de administración y recursos humanos"),
                ("Desarrollo", "Departamento de desarrollo de software"),
                ("Marketing", "Departamento de marketing y comunicación"),
                ("Ventas", "Departamento comercial y de ventas"),
                ("Soporte", "Departamento de soporte técnico")
            };

            foreach (var (name, description) in departments)
            {
                var checkQuery = @"SELECT COUNT(*) FROM ""Departments"" WHERE ""Name"" = @name";
                
                using var checkCmd = new NpgsqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("name", name);
                
                var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
                
                if (!exists)
                {
                    var insertQuery = @"
                        INSERT INTO ""Departments"" (""CompanyId"", ""Name"", ""Description"", ""Active"", ""CreatedAt"", ""UpdatedAt"")
                        VALUES (1, @name, @description, @active, @createdAt, @updatedAt)";

                    using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                    insertCmd.Parameters.AddWithValue("name", name);
                    insertCmd.Parameters.AddWithValue("description", description);
                    insertCmd.Parameters.AddWithValue("active", true);
                    insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                    insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            
            _logger.LogInformation("🏬 Default departments created");
        }

        /// <summary>
        /// Crear administrador de la empresa
        /// </summary>
        private async Task CreateCompanyAdminAsync(NpgsqlConnection connection, string adminEmail, string adminPassword)
        {
            var checkQuery = @"SELECT COUNT(*) FROM ""Employees"" WHERE ""Email"" = @email";
            
            using var checkCmd = new NpgsqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("email", adminEmail);
            
            var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
            
            if (!exists)
            {
                var passwordHash = _passwordService.HashPassword(adminPassword);
                
                var insertQuery = @"
                    INSERT INTO ""Employees"" (
                        ""CompanyId"", ""DepartmentId"", ""FirstName"", ""LastName"", ""Email"", 
                        ""Phone"", ""EmployeeCode"", ""Role"", ""PasswordHash"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                    )
                    VALUES (
                        1, 1, @firstName, @lastName, @email, 
                        @phone, @employeeCode, @role, @passwordHash, @active, @createdAt, @updatedAt
                    )";

                using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("firstName", "Admin");
                insertCmd.Parameters.AddWithValue("lastName", "Company");
                insertCmd.Parameters.AddWithValue("email", adminEmail);
                insertCmd.Parameters.AddWithValue("phone", "+34 666 111 222");
                insertCmd.Parameters.AddWithValue("employeeCode", "EMP001");
                insertCmd.Parameters.AddWithValue("role", (int)UserRole.CompanyAdmin);
                insertCmd.Parameters.AddWithValue("passwordHash", passwordHash);
                insertCmd.Parameters.AddWithValue("active", true);
                insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                await insertCmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("👤 Company admin created: {AdminEmail}", adminEmail);
            }
        }

        /// <summary>
        /// Crear empleados de demostración
        /// </summary>
        private async Task CreateDemoEmployeesAsync(NpgsqlConnection connection)
        {
            var employees = new[]
            {
                ("Juan", "Pérez", "juan.perez@demo.com", "+34 666 222 333", "EMP002", 2, UserRole.Supervisor),
                ("María", "García", "maria.garcia@demo.com", "+34 666 333 444", "EMP003", 2, UserRole.Employee),
                ("Carlos", "López", "carlos.lopez@demo.com", "+34 666 444 555", "EMP004", 3, UserRole.Employee),
                ("Ana", "Martín", "ana.martin@demo.com", "+34 666 555 666", "EMP005", 4, UserRole.Employee)
            };

            foreach (var (firstName, lastName, email, phone, code, deptId, role) in employees)
            {
                var checkQuery = @"SELECT COUNT(*) FROM ""Employees"" WHERE ""Email"" = @email";
                
                using var checkCmd = new NpgsqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("email", email);
                
                var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
                
                if (!exists)
                {
                    var passwordHash = _passwordService.HashPassword("employee123");
                    
                    var insertQuery = @"
                        INSERT INTO ""Employees"" (
                            ""CompanyId"", ""DepartmentId"", ""FirstName"", ""LastName"", ""Email"", 
                            ""Phone"", ""EmployeeCode"", ""Role"", ""PasswordHash"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                        )
                        VALUES (
                            1, @deptId, @firstName, @lastName, @email, 
                            @phone, @employeeCode, @role, @passwordHash, @active, @createdAt, @updatedAt
                        )";

                    using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                    insertCmd.Parameters.AddWithValue("deptId", deptId);
                    insertCmd.Parameters.AddWithValue("firstName", firstName);
                    insertCmd.Parameters.AddWithValue("lastName", lastName);
                    insertCmd.Parameters.AddWithValue("email", email);
                    insertCmd.Parameters.AddWithValue("phone", phone);
                    insertCmd.Parameters.AddWithValue("employeeCode", code);
                    insertCmd.Parameters.AddWithValue("role", (int)role);
                    insertCmd.Parameters.AddWithValue("passwordHash", passwordHash);
                    insertCmd.Parameters.AddWithValue("active", true);
                    insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                    insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            
            _logger.LogInformation("👥 Demo employees created");
        }

        /// <summary>
        /// Limpiar todas las bases de datos
        /// </summary>
        public async Task CleanupAllDatabasesAsync()
        {
            _logger.LogInformation("🧹 Iniciando limpieza de bases de datos");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Obtener lista de bases de datos del sistema
                var getDatabasesQuery = @"
                    SELECT datname FROM pg_database 
                    WHERE datname LIKE 'SphereTimeControl_%' 
                    AND datname != 'SphereTimeControl_Central'";

                var databases = new List<string>();
                using var cmd = new NpgsqlCommand(getDatabasesQuery, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    databases.Add(reader.GetString(0));
                }
                reader.Close();

                // Eliminar bases de datos de tenants
                foreach (var dbName in databases)
                {
                    var dropQuery = $@"DROP DATABASE IF EXISTS ""{dbName}""";
                    using var dropCmd = new NpgsqlCommand(dropQuery, connection);
                    await dropCmd.ExecuteNonQueryAsync();
                    
                    _logger.LogInformation("🗑️ Database dropped: {DatabaseName}", dbName);
                }

                // Limpiar registros de la BD central
                await CleanupCentralDatabaseAsync(connection);

                _logger.LogInformation("✅ Limpieza completada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error durante la limpieza");
                throw;
            }
        }

        /// <summary>
        /// Limpiar datos de la base de datos central
        /// </summary>
        private async Task CleanupCentralDatabaseAsync(NpgsqlConnection connection)
        {
            var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
            
            using var centralConnection = new NpgsqlConnection(centralConnectionString);
            await centralConnection.OpenAsync();

            // Eliminar todos los tenants excepto el demo
            var cleanupQuery = @"DELETE FROM ""Tenants"" WHERE ""Code"" != 'demo'";
            using var cleanupCmd = new NpgsqlCommand(cleanupQuery, centralConnection);
            await cleanupCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("🧹 Central database cleaned");
        }

        /// <summary>
        /// Mostrar estado de las bases de datos
        /// </summary>
        public async Task ShowDatabaseStatusAsync()
        {
            _logger.LogInformation("📊 Estado de las bases de datos:");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Obtener información de las bases de datos
                var query = @"
                    SELECT 
                        datname,
                        pg_size_pretty(pg_database_size(datname)) as size,
                        (SELECT count(*) FROM pg_stat_activity WHERE datname = pg_stat_activity.datname) as connections
                    FROM pg_database 
                    WHERE datname LIKE 'SphereTimeControl_%'
                    ORDER BY datname";

                using var cmd = new NpgsqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                Console.WriteLine("┌─────────────────────────────────┬─────────────┬─────────────┐");
                Console.WriteLine("│ Database                        │ Size        │ Connections │");
                Console.WriteLine("├─────────────────────────────────┼─────────────┼─────────────┤");

                while (await reader.ReadAsync())
                {
                    var name = reader.GetString(0).PadRight(31);
                    var size = reader.GetString(1).PadRight(11);
                    var connections = reader.GetInt32(2).ToString().PadRight(11);
                    
                    Console.WriteLine($"│ {name} │ {size} │ {connections} │");
                }

                Console.WriteLine("└─────────────────────────────────┴─────────────┴─────────────┘");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo estado de las bases de datos");
                throw;
            }
        }

        /// <summary>
        /// Crear base de datos si no existe
        /// </summary>
        private async Task CreateDatabaseIfNotExistsAsync(string databaseName)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var checkQuery = @"SELECT 1 FROM pg_database WHERE datname = @databaseName";
            using var checkCmd = new NpgsqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("databaseName", databaseName);
            
            var exists = await checkCmd.ExecuteScalarAsync() != null;
            
            if (!exists)
            {
                var createQuery = $@"CREATE DATABASE ""{databaseName}"" WITH ENCODING = 'UTF8'";
                using var createCmd = new NpgsqlCommand(createQuery, connection);
                await createCmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("📦 Database created: {DatabaseName}", databaseName);
            }
            else
            {
                _logger.LogInformation("📦 Database already exists: {DatabaseName}", databaseName);
            }
        }

        /// <summary>
        /// Ejecutar script SQL desde archivo
        /// </summary>
        private async Task ExecuteSqlScriptAsync(NpgsqlConnection connection, string scriptPath)
        {
            if (File.Exists(scriptPath))
            {
                var script = await File.ReadAllTextAsync(scriptPath);
                using var cmd = new NpgsqlCommand(script, connection);
                await cmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("📜 Script executed: {ScriptPath}", scriptPath);
            }
            else
            {
                _logger.LogWarning("⚠️ Script not found: {ScriptPath}", scriptPath);
            }
        }
    }
}