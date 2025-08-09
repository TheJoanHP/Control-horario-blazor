using Microsoft.Extensions.Logging;
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
                
                Console.WriteLine("\n🎉 ¡Configuración completada!");
                Console.WriteLine("=================================");
                Console.WriteLine("📊 Base de datos central: SphereTimeControl_Central");
                Console.WriteLine("🏢 Tenant demo: SphereTimeControl_demo");
                Console.WriteLine("👤 Super Admin: admin@spheretimecontrol.com / admin123");
                Console.WriteLine("👨‍💼 Company Admin: admin@empresademo.com / admin123");
                Console.WriteLine("👨‍💻 Empleado demo: juan.perez@empresademo.com / empleado123");
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
                
                // Conectar a la BD central y crear estructura
                var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
                
                using var connection = new NpgsqlConnection(centralConnectionString);
                await connection.OpenAsync();

                // Crear estructura básica directamente
                await CreateCentralStructureAsync(connection);

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
        /// Crear estructura de la base de datos central
        /// </summary>
        private async Task CreateCentralStructureAsync(NpgsqlConnection connection)
        {
            _logger.LogInformation("🏗️ Creando estructura de base de datos central");
            
            var centralStructure = @"
                -- Extensiones
                CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";
                CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";

                -- Tabla Tenants
                CREATE TABLE IF NOT EXISTS ""Tenants"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Subdomain"" VARCHAR(50) NOT NULL UNIQUE,
                    ""DatabaseName"" VARCHAR(200) NOT NULL UNIQUE,
                    ""ContactEmail"" VARCHAR(255) NOT NULL,
                    ""Phone"" VARCHAR(20),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""LicenseType"" INTEGER NOT NULL DEFAULT 0,
                    ""MaxEmployees"" INTEGER NOT NULL DEFAULT 10,
                    ""LicenseExpiresAt"" TIMESTAMP NOT NULL DEFAULT (NOW() + INTERVAL '30 days'),
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Tabla SphereAdmins
                CREATE TABLE IF NOT EXISTS ""SphereAdmins"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""FirstName"" VARCHAR(100) NOT NULL,
                    ""LastName"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""LastLoginAt"" TIMESTAMP,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Tabla SystemConfigs
                CREATE TABLE IF NOT EXISTS ""SystemConfigs"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Key"" VARCHAR(100) NOT NULL UNIQUE,
                    ""Value"" TEXT NOT NULL,
                    ""Description"" VARCHAR(500),
                    ""IsPublic"" BOOLEAN NOT NULL DEFAULT false,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Índices
                CREATE INDEX IF NOT EXISTS ""IX_Tenants_Subdomain"" ON ""Tenants"" (""Subdomain"");
                CREATE INDEX IF NOT EXISTS ""IX_Tenants_Active"" ON ""Tenants"" (""Active"");
                CREATE INDEX IF NOT EXISTS ""IX_SphereAdmins_Email"" ON ""SphereAdmins"" (""Email"");
                CREATE INDEX IF NOT EXISTS ""IX_SystemConfigs_Key"" ON ""SystemConfigs"" (""Key"");";

            using var cmd = new NpgsqlCommand(centralStructure, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Sembrar datos iniciales en la base de datos central
        /// </summary>
        private async Task SeedCentralDataAsync(NpgsqlConnection connection)
        {
            _logger.LogInformation("🌱 Sembrando datos en BD central");

            // Crear configuraciones del sistema
            await CreateSystemConfigurationsAsync(connection);
            
            // Crear super administrador
            await CreateSuperAdminAsync(connection);
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
        /// Crear tenant de demostración
        /// </summary>
        public async Task CreateDemoTenantAsync()
        {
            _logger.LogInformation("🏢 Creando tenant de demostración");

            try
            {
                // Crear registro en BD central
                await CreateTenantRecordAsync("demo", "Empresa Demo", "admin@empresademo.com");
                
                // Crear BD del tenant
                await CreateDatabaseIfNotExistsAsync("SphereTimeControl_demo");
                
                // Crear estructura del tenant
                await CreateTenantStructureAsync("SphereTimeControl_demo");
                
                // Sembrar datos del tenant
                await SeedTenantDataAsync("SphereTimeControl_demo");
                
                _logger.LogInformation("✅ Tenant demo creado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando tenant demo");
                throw;
            }
        }

        /// <summary>
        /// Crear registro del tenant en BD central
        /// </summary>
        private async Task CreateTenantRecordAsync(string tenantCode, string companyName, string adminEmail)
        {
            var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
            
            using var connection = new NpgsqlConnection(centralConnectionString);
            await connection.OpenAsync();

            var checkQuery = @"SELECT COUNT(*) FROM ""Tenants"" WHERE ""Subdomain"" = @subdomain";
            using var checkCmd = new NpgsqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("subdomain", tenantCode);
            
            var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
            
            if (!exists)
            {
                var insertQuery = @"
                    INSERT INTO ""Tenants"" (""Name"", ""Subdomain"", ""DatabaseName"", ""ContactEmail"", ""Active"", ""CreatedAt"", ""UpdatedAt"")
                    VALUES (@name, @subdomain, @databaseName, @contactEmail, @active, @createdAt, @updatedAt)";

                using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("name", companyName);
                insertCmd.Parameters.AddWithValue("subdomain", tenantCode);
                insertCmd.Parameters.AddWithValue("databaseName", $"SphereTimeControl_{tenantCode}");
                insertCmd.Parameters.AddWithValue("contactEmail", adminEmail);
                insertCmd.Parameters.AddWithValue("active", true);
                insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                await insertCmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("📝 Tenant registrado en BD central");
            }
        }

        /// <summary>
        /// Crear estructura del tenant
        /// </summary>
        private async Task CreateTenantStructureAsync(string databaseName)
        {
            _logger.LogInformation("🏗️ Creando estructura del tenant: {DatabaseName}", databaseName);
            
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            var tenantStructure = @"
                -- Extensiones
                CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";
                CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";

                -- Tabla Companies
                CREATE TABLE IF NOT EXISTS ""Companies"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL,
                    ""Phone"" VARCHAR(20),
                    ""Address"" VARCHAR(500),
                    ""WorkStartTime"" TIME DEFAULT '09:00:00',
                    ""WorkEndTime"" TIME DEFAULT '18:00:00',
                    ""ToleranceMinutes"" INTEGER DEFAULT 15,
                    ""VacationDaysPerYear"" INTEGER DEFAULT 22,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );

                -- Tabla Departments
                CREATE TABLE IF NOT EXISTS ""Departments"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Description"" VARCHAR(500),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Departments_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
                );

                -- Tabla Users
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER,
                    ""FirstName"" VARCHAR(100) NOT NULL,
                    ""LastName"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""Role"" INTEGER NOT NULL DEFAULT 3,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""LastLogin"" TIMESTAMP,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Users_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE SET NULL
                );

                -- Tabla Employees
                CREATE TABLE IF NOT EXISTS ""Employees"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""UserId"" INTEGER,
                    ""DepartmentId"" INTEGER,
                    ""FirstName"" VARCHAR(50) NOT NULL,
                    ""LastName"" VARCHAR(50) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL,
                    ""Phone"" VARCHAR(20),
                    ""EmployeeCode"" VARCHAR(20) NOT NULL,
                    ""Position"" VARCHAR(100),
                    ""Role"" INTEGER NOT NULL DEFAULT 3,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""HireDate"" DATE,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Employees_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Employees_Users"" FOREIGN KEY (""UserId"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_Employees_Departments"" FOREIGN KEY (""DepartmentId"") 
                        REFERENCES ""Departments"" (""Id"") ON DELETE SET NULL
                );

                -- Tabla TimeRecords
                CREATE TABLE IF NOT EXISTS ""TimeRecords"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""Type"" INTEGER NOT NULL,
                    ""Date"" DATE NOT NULL,
                    ""Time"" TIME NOT NULL,
                    ""Timestamp"" TIMESTAMP NOT NULL,
                    ""Notes"" VARCHAR(500),
                    ""Location"" VARCHAR(100),
                    ""Latitude"" DOUBLE PRECISION,
                    ""Longitude"" DOUBLE PRECISION,
                    ""IsManualEntry"" BOOLEAN DEFAULT false,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_TimeRecords_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE
                );

                -- Tabla VacationRequests
                CREATE TABLE IF NOT EXISTS ""VacationRequests"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""StartDate"" DATE NOT NULL,
                    ""EndDate"" DATE NOT NULL,
                    ""DaysRequested"" INTEGER NOT NULL,
                    ""TotalDays"" INTEGER NOT NULL,
                    ""Status"" INTEGER NOT NULL DEFAULT 0,
                    ""Comments"" VARCHAR(1000),
                    ""ResponseComments"" VARCHAR(1000),
                    ""ReviewedById"" INTEGER,
                    ""ReviewedAt"" TIMESTAMP,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_VacationRequests_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE
                );

                -- Tabla VacationBalances
                CREATE TABLE IF NOT EXISTS ""VacationBalances"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""Year"" INTEGER NOT NULL,
                    ""TotalDays"" INTEGER NOT NULL,
                    ""UsedDays"" INTEGER DEFAULT 0,
                    ""PendingDays"" INTEGER DEFAULT 0,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_VacationBalances_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE
                );

                -- Índices importantes
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Companies_Email"" ON ""Companies"" (""Email"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Employees_Email"" ON ""Employees"" (""Email"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Employees_CompanyId_EmployeeCode"" ON ""Employees"" (""CompanyId"", ""EmployeeCode"");
                CREATE INDEX IF NOT EXISTS ""IX_TimeRecords_EmployeeId_Date"" ON ""TimeRecords"" (""EmployeeId"", ""Date"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_VacationBalances_EmployeeId_Year"" ON ""VacationBalances"" (""EmployeeId"", ""Year"");";

            using var cmd = new NpgsqlCommand(tenantStructure, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Sembrar datos del tenant
        /// </summary>
        private async Task SeedTenantDataAsync(string databaseName)
        {
            _logger.LogInformation("🌱 Sembrando datos del tenant: {DatabaseName}", databaseName);
            
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            // 1. Crear empresa
            var companyId = await CreateCompanyAsync(connection);
            
            // 2. Crear departamento
            var departmentId = await CreateDepartmentAsync(connection, companyId);
            
            // 3. Crear usuarios y empleados
            await CreateEmployeesAsync(connection, companyId, departmentId);
            
            // 4. Crear saldos de vacaciones
            await CreateVacationBalancesAsync(connection);
        }

        private async Task<int> CreateCompanyAsync(NpgsqlConnection connection)
        {
            var insertQuery = @"
                INSERT INTO ""Companies"" (""Name"", ""Email"", ""Phone"", ""Address"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@name, @email, @phone, @address, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("name", "Empresa Demo");
            cmd.Parameters.AddWithValue("email", "info@empresademo.com");
            cmd.Parameters.AddWithValue("phone", "+34 666 777 888");
            cmd.Parameters.AddWithValue("address", "Calle Principal 123, Madrid");
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var companyId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            _logger.LogInformation("🏢 Empresa creada con ID: {CompanyId}", companyId);
            return companyId;
        }

        private async Task<int> CreateDepartmentAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""Departments"" (""CompanyId"", ""Name"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@companyId, @name, @description, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("name", "Desarrollo");
            cmd.Parameters.AddWithValue("description", "Departamento de desarrollo de software");
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var departmentId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            _logger.LogInformation("🏬 Departamento creado con ID: {DepartmentId}", departmentId);
            return departmentId;
        }

        private async Task CreateEmployeesAsync(NpgsqlConnection connection, int companyId, int departmentId)
        {
            // Admin de empresa
            var adminUserId = await CreateUserAsync(connection, companyId, "Admin", "Demo", "admin@empresademo.com", "admin123", (int)UserRole.CompanyAdmin);
            await CreateEmployeeAsync(connection, companyId, adminUserId, departmentId, "Admin", "Demo", "admin@empresademo.com", "ADMIN001", "Administrador", "admin123", (int)UserRole.CompanyAdmin);
            
            // Empleado demo
            var employeeUserId = await CreateUserAsync(connection, companyId, "Juan", "Pérez", "juan.perez@empresademo.com", "empleado123", (int)UserRole.Employee);
            await CreateEmployeeAsync(connection, companyId, employeeUserId, departmentId, "Juan", "Pérez", "juan.perez@empresademo.com", "EMP001", "Desarrollador", "empleado123", (int)UserRole.Employee);
        }

        private async Task<int> CreateUserAsync(NpgsqlConnection connection, int companyId, string firstName, string lastName, string email, string password, int role)
        {
            var passwordHash = _passwordService.HashPassword(password);
            
            var insertQuery = @"
                INSERT INTO ""Users"" (""CompanyId"", ""FirstName"", ""LastName"", ""Email"", ""PasswordHash"", ""Role"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@companyId, @firstName, @lastName, @email, @passwordHash, @role, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("firstName", firstName);
            cmd.Parameters.AddWithValue("lastName", lastName);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("passwordHash", passwordHash);
            cmd.Parameters.AddWithValue("role", role);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        private async Task CreateEmployeeAsync(NpgsqlConnection connection, int companyId, int userId, int departmentId, string firstName, string lastName, string email, string employeeCode, string position, string password, int role)
        {
            var passwordHash = _passwordService.HashPassword(password);
            
            var insertQuery = @"
                INSERT INTO ""Employees"" (""CompanyId"", ""UserId"", ""DepartmentId"", ""FirstName"", ""LastName"", ""Email"", ""EmployeeCode"", ""Position"", ""Role"", ""PasswordHash"", ""HireDate"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@companyId, @userId, @departmentId, @firstName, @lastName, @email, @employeeCode, @position, @role, @passwordHash, @hireDate, @createdAt, @updatedAt)";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("departmentId", departmentId);
            cmd.Parameters.AddWithValue("firstName", firstName);
            cmd.Parameters.AddWithValue("lastName", lastName);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("employeeCode", employeeCode);
            cmd.Parameters.AddWithValue("position", position);
            cmd.Parameters.AddWithValue("role", role);
            cmd.Parameters.AddWithValue("passwordHash", passwordHash);
            cmd.Parameters.AddWithValue("hireDate", DateTime.Today.AddMonths(-6));
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("👤 Empleado creado: {Email}", email);
        }

        private async Task CreateVacationBalancesAsync(NpgsqlConnection connection)
        {
            var insertQuery = @"
                INSERT INTO ""VacationBalances"" (""EmployeeId"", ""Year"", ""TotalDays"", ""CreatedAt"", ""UpdatedAt"")
                SELECT ""Id"", 2025, 22, NOW(), NOW()
                FROM ""Employees""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            await cmd.ExecuteNonQueryAsync();
            
            _logger.LogInformation("📅 Saldos de vacaciones creados para 2025");
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
        /// Mostrar estado de las bases de datos
        /// </summary>
        public async Task ShowDatabaseStatusAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT datname, pg_size_pretty(pg_database_size(datname)) as size
                    FROM pg_database 
                    WHERE datname LIKE 'SphereTimeControl_%'
                    ORDER BY datname";

                using var cmd = new NpgsqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                Console.WriteLine("\n📊 Estado de las bases de datos:");
                Console.WriteLine("================================");
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"• {reader.GetString(0)} - {reader.GetString(1)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estado de las bases de datos");
            }
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

                var getDatabasesQuery = @"
                    SELECT datname FROM pg_database 
                    WHERE datname LIKE 'SphereTimeControl_%'";

                var databases = new List<string>();
                using var cmd = new NpgsqlCommand(getDatabasesQuery, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    databases.Add(reader.GetString(0));
                }
                reader.Close();

                foreach (var dbName in databases)
                {
                    var dropQuery = $@"DROP DATABASE IF EXISTS ""{dbName}""";
                    using var dropCmd = new NpgsqlCommand(dropQuery, connection);
                    await dropCmd.ExecuteNonQueryAsync();
                    
                    _logger.LogInformation("🗑️ Database dropped: {DatabaseName}", dbName);
                }

                _logger.LogInformation("✅ Limpieza completada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la limpieza");
                throw;
            }
        }
    }
}