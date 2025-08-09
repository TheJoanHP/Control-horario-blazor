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
                await CreateDatabaseIfNotExistsAsync("SphereTimeControl_Central");
                
                var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
                
                using var connection = new NpgsqlConnection(centralConnectionString);
                await connection.OpenAsync();

                await CreateCentralStructureAsync(connection);
                await SeedCentralDataAsync(connection);

                _logger.LogInformation("✅ Base de datos central creada y sembrada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando base de datos central");
                throw;
            }
        }

        private async Task CreateCentralStructureAsync(NpgsqlConnection connection)
        {
            _logger.LogInformation("🏗️ Creando estructura de base de datos central");
            
            // DIVIDIR EN MÚLTIPLES COMANDOS PEQUEÑOS
            var commands = new List<string>
            {
                // Extensiones
                @"CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";",
                @"CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";",
                
                // Tabla Tenants
                @"CREATE TABLE IF NOT EXISTS ""Tenants"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Subdomain"" VARCHAR(50) NOT NULL UNIQUE,
                    ""DatabaseName"" VARCHAR(200) NOT NULL UNIQUE,
                    ""ContactEmail"" VARCHAR(255) NOT NULL,
                    ""Phone"" VARCHAR(20),
                    ""Address"" VARCHAR(500),
                    ""TaxId"" VARCHAR(50),
                    ""Website"" VARCHAR(255),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""LicenseType"" INTEGER NOT NULL DEFAULT 0,
                    ""MaxEmployees"" INTEGER NOT NULL DEFAULT 10,
                    ""LicenseExpiresAt"" TIMESTAMP NOT NULL DEFAULT (NOW() + INTERVAL '30 days'),
                    ""MonthlyPrice"" DECIMAL(10,2) DEFAULT 0.00,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Tabla SphereAdmins
                @"CREATE TABLE IF NOT EXISTS ""SphereAdmins"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""FirstName"" VARCHAR(100) NOT NULL,
                    ""LastName"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""LastLoginAt"" TIMESTAMP,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Tabla Licenses
                @"CREATE TABLE IF NOT EXISTS ""Licenses"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""TenantId"" INTEGER NOT NULL,
                    ""Type"" INTEGER NOT NULL DEFAULT 0,
                    ""MaxEmployees"" INTEGER NOT NULL,
                    ""MonthlyPrice"" DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                    ""StartDate"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""EndDate"" TIMESTAMP NOT NULL,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""AutoRenew"" BOOLEAN NOT NULL DEFAULT false,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Licenses_Tenants"" FOREIGN KEY (""TenantId"") 
                        REFERENCES ""Tenants"" (""Id"") ON DELETE CASCADE
                );",
                
                // Tabla SystemConfigs
                @"CREATE TABLE IF NOT EXISTS ""SystemConfigs"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Key"" VARCHAR(100) NOT NULL UNIQUE,
                    ""Value"" TEXT NOT NULL,
                    ""Description"" VARCHAR(500),
                    ""IsPublic"" BOOLEAN NOT NULL DEFAULT false,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Tabla BillingRecords
                @"CREATE TABLE IF NOT EXISTS ""BillingRecords"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""TenantId"" INTEGER NOT NULL,
                    ""LicenseId"" INTEGER NOT NULL,
                    ""Amount"" DECIMAL(10,2) NOT NULL,
                    ""Currency"" VARCHAR(3) NOT NULL DEFAULT 'EUR',
                    ""PeriodStart"" TIMESTAMP NOT NULL,
                    ""PeriodEnd"" TIMESTAMP NOT NULL,
                    ""Status"" INTEGER NOT NULL DEFAULT 0,
                    ""PaidAt"" TIMESTAMP,
                    ""PaymentMethod"" VARCHAR(50),
                    ""InvoiceNumber"" VARCHAR(100),
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_BillingRecords_Tenants"" FOREIGN KEY (""TenantId"") 
                        REFERENCES ""Tenants"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_BillingRecords_Licenses"" FOREIGN KEY (""LicenseId"") 
                        REFERENCES ""Licenses"" (""Id"") ON DELETE CASCADE
                );",
                
                // Índices
                @"CREATE INDEX IF NOT EXISTS ""IX_Tenants_Subdomain"" ON ""Tenants"" (""Subdomain"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Tenants_Active"" ON ""Tenants"" (""Active"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_SphereAdmins_Email"" ON ""SphereAdmins"" (""Email"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_SystemConfigs_Key"" ON ""SystemConfigs"" (""Key"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Licenses_TenantId"" ON ""Licenses"" (""TenantId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_BillingRecords_TenantId"" ON ""BillingRecords"" (""TenantId"");"
            };

            // Ejecutar cada comando por separado
            foreach (var command in commands)
            {
                try
                {
                    _logger.LogDebug("Ejecutando comando: {Command}", command.Substring(0, Math.Min(50, command.Length)) + "...");
                    
                    using var cmd = new NpgsqlCommand(command, connection);
                    cmd.CommandTimeout = 120; // 2 minutos de timeout
                    await cmd.ExecuteNonQueryAsync();
                    
                    // Pequeña pausa entre comandos
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ejecutando comando: {Command}", command);
                    throw;
                }
            }
            
            _logger.LogInformation("✅ Estructura central creada exitosamente");
        }

        private async Task SeedCentralDataAsync(NpgsqlConnection connection)
        {
            _logger.LogInformation("🌱 Sembrando datos en BD central");

            await CreateSystemConfigurationsAsync(connection);
            await CreateSuperAdminAsync(connection);
        }

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
        }

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
                { "MaintenanceMode", "false" },
                { "DefaultCurrency", "EUR" },
                { "BasicMonthlyPrice", "29.99" },
                { "ProfessionalMonthlyPrice", "59.99" },
                { "EnterpriseMonthlyPrice", "99.99" }
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
                await CreateTenantRecordAsync("demo", "Empresa Demo", "admin@empresademo.com");
                await CreateDatabaseIfNotExistsAsync("SphereTimeControl_demo");
                await CreateTenantStructureAsync("SphereTimeControl_demo");
                await SeedTenantDataAsync("SphereTimeControl_demo");
                
                _logger.LogInformation("✅ Tenant demo creado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando tenant demo");
                throw;
            }
        }

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

        private async Task CreateTenantStructureAsync(string databaseName)
        {
            _logger.LogInformation("🏗️ Creando estructura del tenant: {DatabaseName}", databaseName);
            
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            // DIVIDIR EN COMANDOS SEPARADOS
            var commands = new List<string>
            {
                // Extensiones
                @"CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";",
                @"CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";",
                
                // Tabla Companies
                @"CREATE TABLE IF NOT EXISTS ""Companies"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Code"" VARCHAR(50),
                    ""Subdomain"" VARCHAR(50),
                    ""TaxId"" VARCHAR(20),
                    ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                    ""Phone"" VARCHAR(20),
                    ""Website"" VARCHAR(255),
                    ""Address"" VARCHAR(500),
                    ""WorkStartTime"" TIME DEFAULT '09:00:00',
                    ""WorkEndTime"" TIME DEFAULT '18:00:00',
                    ""ToleranceMinutes"" INTEGER DEFAULT 15,
                    ""VacationDaysPerYear"" INTEGER DEFAULT 22,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Tabla Departments
                @"CREATE TABLE IF NOT EXISTS ""Departments"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Description"" VARCHAR(500),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Departments_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
                );",
                
                // Tabla Users
                @"CREATE TABLE IF NOT EXISTS ""Users"" (
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
                );",
                
                // Tabla Employees
                @"CREATE TABLE IF NOT EXISTS ""Employees"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""UserId"" INTEGER,
                    ""DepartmentId"" INTEGER,
                    ""FirstName"" VARCHAR(50) NOT NULL,
                    ""LastName"" VARCHAR(50) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                    ""Phone"" VARCHAR(20),
                    ""EmployeeCode"" VARCHAR(50) NOT NULL,
                    ""EmployeeNumber"" VARCHAR(20),
                    ""Position"" VARCHAR(100),
                    ""Role"" INTEGER NOT NULL DEFAULT 3,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""HireDate"" DATE,
                    ""Salary"" DECIMAL(10,2),
                    ""WorkScheduleId"" INTEGER,
                    ""LastLoginAt"" TIMESTAMP,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Employees_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Employees_Users"" FOREIGN KEY (""UserId"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_Employees_Departments"" FOREIGN KEY (""DepartmentId"") 
                        REFERENCES ""Departments"" (""Id"") ON DELETE SET NULL
                );"
            };

            // Ejecutar comandos de creación de tablas principales
            foreach (var command in commands)
            {
                try
                {
                    _logger.LogDebug("Ejecutando comando tenant: {Command}", command.Substring(0, Math.Min(50, command.Length)) + "...");
                    
                    using var cmd = new NpgsqlCommand(command, connection);
                    cmd.CommandTimeout = 120;
                    await cmd.ExecuteNonQueryAsync();
                    
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ejecutando comando tenant: {Command}", command);
                    throw;
                }
            }

            // Crear las tablas restantes en otro batch
            await CreateRemainingTenantTablesAsync(connection);
            
            // Crear índices
            await CreateTenantIndexesAsync(connection);
            
            // Crear vistas y triggers
            await CreateTenantViewsAndTriggersAsync(connection);
        }

        private async Task CreateRemainingTenantTablesAsync(NpgsqlConnection connection)
        {
            var commands = new List<string>
            {
                // WorkSchedules
                @"CREATE TABLE IF NOT EXISTS ""WorkSchedules"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""StartTime"" TIME NOT NULL,
                    ""EndTime"" TIME NOT NULL,
                    ""Monday"" BOOLEAN DEFAULT true,
                    ""Tuesday"" BOOLEAN DEFAULT true,
                    ""Wednesday"" BOOLEAN DEFAULT true,
                    ""Thursday"" BOOLEAN DEFAULT true,
                    ""Friday"" BOOLEAN DEFAULT true,
                    ""Saturday"" BOOLEAN DEFAULT false,
                    ""Sunday"" BOOLEAN DEFAULT false,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_WorkSchedules_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
                );",
                
                // TimeRecords
                @"CREATE TABLE IF NOT EXISTS ""TimeRecords"" (
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
                    ""CreatedBy"" INTEGER,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_TimeRecords_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE
                );",
                
                // VacationPolicies
                @"CREATE TABLE IF NOT EXISTS ""VacationPolicies"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""AnnualDays"" INTEGER NOT NULL,
                    ""MinDaysPerRequest"" INTEGER DEFAULT 1,
                    ""MaxDaysPerRequest"" INTEGER DEFAULT 30,
                    ""MinAdvanceNoticeDays"" INTEGER DEFAULT 7,
                    ""RequiresApproval"" BOOLEAN DEFAULT true,
                    ""CarryOverDays"" INTEGER DEFAULT 0,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_VacationPolicies_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
                );",
                
                // VacationRequests
                @"CREATE TABLE IF NOT EXISTS ""VacationRequests"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""VacationPolicyId"" INTEGER,
                    ""StartDate"" DATE NOT NULL,
                    ""EndDate"" DATE NOT NULL,
                    ""TotalDays"" INTEGER NOT NULL,
                    ""Status"" INTEGER NOT NULL DEFAULT 0,
                    ""Comments"" VARCHAR(1000),
                    ""ResponseComments"" VARCHAR(1000),
                    ""ReviewedById"" INTEGER,
                    ""ReviewedAt"" TIMESTAMP,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_VacationRequests_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_VacationRequests_VacationPolicies"" FOREIGN KEY (""VacationPolicyId"") 
                        REFERENCES ""VacationPolicies"" (""Id"") ON DELETE SET NULL
                );",
                
                // VacationBalances
                @"CREATE TABLE IF NOT EXISTS ""VacationBalances"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""Year"" INTEGER NOT NULL,
                    ""TotalDays"" INTEGER NOT NULL,
                    ""UsedDays"" INTEGER DEFAULT 0,
                    ""PendingDays"" INTEGER DEFAULT 0,
                    ""CarriedOverDays"" INTEGER DEFAULT 0,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_VacationBalances_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE
                );"
            };

            foreach (var command in commands)
            {
                try
                {
                    using var cmd = new NpgsqlCommand(command, connection);
                    cmd.CommandTimeout = 120;
                    await cmd.ExecuteNonQueryAsync();
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando tabla: {Command}", command);
                    throw;
                }
            }
        }

        private async Task CreateTenantIndexesAsync(NpgsqlConnection connection)
        {
            var indexes = new List<string>
            {
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Companies_Email"" ON ""Companies"" (""Email"");",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Employees_Email"" ON ""Employees"" (""Email"");",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Employees_CompanyId_EmployeeCode"" ON ""Employees"" (""CompanyId"", ""EmployeeCode"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_TimeRecords_EmployeeId_Date"" ON ""TimeRecords"" (""EmployeeId"", ""Date"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_TimeRecords_Timestamp"" ON ""TimeRecords"" (""Timestamp"");",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_VacationBalances_EmployeeId_Year"" ON ""VacationBalances"" (""EmployeeId"", ""Year"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_VacationRequests_EmployeeId"" ON ""VacationRequests"" (""EmployeeId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_VacationRequests_Status"" ON ""VacationRequests"" (""Status"");"
            };

            foreach (var index in indexes)
            {
                try
                {
                    using var cmd = new NpgsqlCommand(index, connection);
                    cmd.CommandTimeout = 120;
                    await cmd.ExecuteNonQueryAsync();
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando índice: {Index}", index);
                    throw;
                }
            }
        }

        private async Task CreateTenantViewsAndTriggersAsync(NpgsqlConnection connection)
        {
            // Vista de empleados
            var employeesView = @"
                CREATE OR REPLACE VIEW ""EmployeesView"" AS
                SELECT 
                    e.""Id"", e.""CompanyId"", e.""DepartmentId"", e.""FirstName"", e.""LastName"",
                    (e.""FirstName"" || ' ' || e.""LastName"") as ""FullName"",
                    e.""Email"", e.""Phone"", e.""EmployeeCode"", e.""Position"", e.""Role"", e.""Active"",
                    e.""LastLoginAt"", e.""HireDate"", e.""CreatedAt"",
                    d.""Name"" as ""DepartmentName"", c.""Name"" as ""CompanyName"",
                    c.""WorkStartTime"", c.""WorkEndTime""
                FROM ""Employees"" e
                LEFT JOIN ""Departments"" d ON e.""DepartmentId"" = d.""Id""
                LEFT JOIN ""Companies"" c ON e.""CompanyId"" = c.""Id"";";

            try
            {
                using var viewCmd = new NpgsqlCommand(employeesView, connection);
                viewCmd.CommandTimeout = 120;
                await viewCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando vista de empleados");
                throw;
            }

            // Función para triggers
            var triggerFunction = @"
                CREATE OR REPLACE FUNCTION update_updated_at_column()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.""UpdatedAt"" = NOW();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;";
            try
            {
                using var funcCmd = new NpgsqlCommand(triggerFunction, connection);
                funcCmd.CommandTimeout = 120;
                await funcCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando función de trigger");
                throw;
            }

            // Crear triggers uno por uno
            var tables = new[] { "Companies", "Departments", "Users", "Employees", "WorkSchedules", "TimeRecords", "VacationPolicies", "VacationRequests", "VacationBalances" };
            
            foreach (var table in tables)
            {
                var triggerSql = $@"
                    DROP TRIGGER IF EXISTS update_{table.ToLower()}_updated_at ON ""{table}"";
                    CREATE TRIGGER update_{table.ToLower()}_updated_at 
                        BEFORE UPDATE ON ""{table}"" 
                        FOR EACH ROW 
                        EXECUTE FUNCTION update_updated_at_column();";

                try
                {
                    using var triggerCmd = new NpgsqlCommand(triggerSql, connection);
                    triggerCmd.CommandTimeout = 120;
                    await triggerCmd.ExecuteNonQueryAsync();
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creando trigger para tabla {Table}", table);
                    // No lanzar excepción aquí, continuar con las demás tablas
                }
            }
        }

        private async Task SeedTenantDataAsync(string databaseName)
        {
            _logger.LogInformation("🌱 Sembrando datos del tenant: {DatabaseName}", databaseName);
            
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            var companyId = await CreateCompanyAsync(connection);
            var departmentIds = await CreateDepartmentsAsync(connection, companyId);
            var workScheduleId = await CreateWorkSchedulesAsync(connection, companyId);
            var vacationPolicyId = await CreateVacationPolicyAsync(connection, companyId);
            
            await CreateEmployeesAsync(connection, companyId, departmentIds, workScheduleId);
            await CreateVacationBalancesAsync(connection);
        }

        private async Task<int> CreateCompanyAsync(NpgsqlConnection connection)
        {
            var insertQuery = @"
                INSERT INTO ""Companies"" (
                    ""Name"", ""Code"", ""TaxId"", ""Email"", ""Phone"", ""Website"", ""Address"", 
                    ""WorkStartTime"", ""WorkEndTime"", ""ToleranceMinutes"", ""VacationDaysPerYear"", 
                    ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @name, @code, @taxId, @email, @phone, @website, @address, 
                    @workStartTime, @workEndTime, @toleranceMinutes, @vacationDaysPerYear, 
                    @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("name", "Empresa Demo S.L.");
            cmd.Parameters.AddWithValue("code", "DEMO");
            cmd.Parameters.AddWithValue("taxId", "B12345678");
            cmd.Parameters.AddWithValue("email", "info@empresademo.com");
            cmd.Parameters.AddWithValue("phone", "+34 911 234 567");
            cmd.Parameters.AddWithValue("website", "https://www.empresademo.com");
            cmd.Parameters.AddWithValue("address", "Calle Principal 123, 28001 Madrid, España");
            cmd.Parameters.AddWithValue("workStartTime", new TimeSpan(9, 0, 0));
            cmd.Parameters.AddWithValue("workEndTime", new TimeSpan(18, 0, 0));
            cmd.Parameters.AddWithValue("toleranceMinutes", 15);
            cmd.Parameters.AddWithValue("vacationDaysPerYear", 22);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var companyId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            _logger.LogInformation("🏢 Empresa creada con ID: {CompanyId}", companyId);
            return companyId;
        }

        private async Task<Dictionary<string, int>> CreateDepartmentsAsync(NpgsqlConnection connection, int companyId)
        {
            var departments = new Dictionary<string, string>
            {
                { "Administración", "Departamento de administración y recursos humanos" },
                { "Desarrollo", "Departamento de desarrollo de software" },
                { "Marketing", "Departamento de marketing y comunicación" },
                { "Ventas", "Departamento comercial y de ventas" }
            };

            var departmentIds = new Dictionary<string, int>();

            foreach (var dept in departments)
            {
                var insertQuery = @"
                    INSERT INTO ""Departments"" (""CompanyId"", ""Name"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                    VALUES (@companyId, @name, @description, @createdAt, @updatedAt)
                    RETURNING ""Id""";

                using var cmd = new NpgsqlCommand(insertQuery, connection);
                cmd.Parameters.AddWithValue("companyId", companyId);
                cmd.Parameters.AddWithValue("name", dept.Key);
                cmd.Parameters.AddWithValue("description", dept.Value);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                var deptId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                departmentIds[dept.Key] = deptId;
            }

            _logger.LogInformation("🏬 {Count} departamentos creados", departmentIds.Count);
            return departmentIds;
        }

        private async Task<int> CreateWorkSchedulesAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""WorkSchedules"" (
                    ""CompanyId"", ""Name"", ""StartTime"", ""EndTime"", 
                    ""Monday"", ""Tuesday"", ""Wednesday"", ""Thursday"", ""Friday"", 
                    ""Saturday"", ""Sunday"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @name, @startTime, @endTime, 
                    @monday, @tuesday, @wednesday, @thursday, @friday,
                    @saturday, @sunday, @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("name", "Horario Estándar");
            cmd.Parameters.AddWithValue("startTime", new TimeSpan(9, 0, 0));
            cmd.Parameters.AddWithValue("endTime", new TimeSpan(18, 0, 0));
            cmd.Parameters.AddWithValue("monday", true);
            cmd.Parameters.AddWithValue("tuesday", true);
            cmd.Parameters.AddWithValue("wednesday", true);
            cmd.Parameters.AddWithValue("thursday", true);
            cmd.Parameters.AddWithValue("friday", true);
            cmd.Parameters.AddWithValue("saturday", false);
            cmd.Parameters.AddWithValue("sunday", false);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var scheduleId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            
            _logger.LogInformation("📅 Horario de trabajo creado con ID: {ScheduleId}", scheduleId);
            return scheduleId;
        }

        private async Task<int> CreateVacationPolicyAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""VacationPolicies"" (
                    ""CompanyId"", ""Name"", ""AnnualDays"", ""MinDaysPerRequest"", 
                    ""MaxDaysPerRequest"", ""MinAdvanceNoticeDays"", ""RequiresApproval"", 
                    ""CarryOverDays"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @name, @annualDays, @minDaysPerRequest, 
                    @maxDaysPerRequest, @minAdvanceNoticeDays, @requiresApproval, 
                    @carryOverDays, @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("name", "Política Estándar");
            cmd.Parameters.AddWithValue("annualDays", 22);
            cmd.Parameters.AddWithValue("minDaysPerRequest", 1);
            cmd.Parameters.AddWithValue("maxDaysPerRequest", 15);
            cmd.Parameters.AddWithValue("minAdvanceNoticeDays", 7);
            cmd.Parameters.AddWithValue("requiresApproval", true);
            cmd.Parameters.AddWithValue("carryOverDays", 5);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var policyId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            _logger.LogInformation("📋 Política de vacaciones creada con ID: {PolicyId}", policyId);
            return policyId;
        }

        private async Task CreateEmployeesAsync(NpgsqlConnection connection, int companyId, Dictionary<string, int> departmentIds, int workScheduleId)
        {
            var employees = new[]
            {
                new { 
                    FirstName = "Ana", LastName = "García", Email = "admin@empresademo.com", 
                    Phone = "+34 666 111 222", Code = "EMP001", Position = "Administradora", 
                    Role = 1, Department = "Administración", Password = "admin123"
                },
                new { 
                    FirstName = "Juan", LastName = "Pérez", Email = "juan.perez@empresademo.com", 
                    Phone = "+34 666 555 666", Code = "EMP003", Position = "Desarrollador Senior", 
                    Role = 3, Department = "Desarrollo", Password = "empleado123"
                },
                new { 
                    FirstName = "María", LastName = "López", Email = "maria.lopez@empresademo.com", 
                    Phone = "+34 666 777 888", Code = "EMP004", Position = "Diseñadora UX/UI", 
                    Role = 3, Department = "Desarrollo", Password = "empleado123"
                }
            };

            foreach (var emp in employees)
            {
                var userId = await CreateUserAsync(connection, companyId, emp.FirstName, emp.LastName, emp.Email, emp.Password, emp.Role);
                await CreateEmployeeAsync(connection, companyId, userId, departmentIds[emp.Department], 
                    emp.FirstName, emp.LastName, emp.Email, emp.Code, emp.Position, emp.Password, emp.Role, workScheduleId);
            }
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

        private async Task CreateEmployeeAsync(NpgsqlConnection connection, int companyId, int userId, int departmentId, 
            string firstName, string lastName, string email, string employeeCode, string position, string password, int role, int workScheduleId)
        {
            var passwordHash = _passwordService.HashPassword(password);
            
            var insertQuery = @"
                INSERT INTO ""Employees"" (
                    ""CompanyId"", ""UserId"", ""DepartmentId"", ""FirstName"", ""LastName"", 
                    ""Email"", ""EmployeeCode"", ""Position"", ""Role"", ""PasswordHash"", 
                    ""HireDate"", ""WorkScheduleId"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @userId, @departmentId, @firstName, @lastName, 
                    @email, @employeeCode, @position, @role, @passwordHash, 
                    @hireDate, @workScheduleId, @createdAt, @updatedAt
                )";

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
            cmd.Parameters.AddWithValue("workScheduleId", workScheduleId);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("👤 Empleado creado: {Email}", email);
        }

        private async Task CreateVacationBalancesAsync(NpgsqlConnection connection)
        {
            var currentYear = DateTime.Now.Year;
            
            var insertQuery = @"
                INSERT INTO ""VacationBalances"" (""EmployeeId"", ""Year"", ""TotalDays"", ""UsedDays"", ""CarriedOverDays"", ""CreatedAt"", ""UpdatedAt"")
                SELECT ""Id"", @year, 22, 0, 0, NOW(), NOW()
                FROM ""Employees""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("year", currentYear);
            await cmd.ExecuteNonQueryAsync();
            
            _logger.LogInformation("📅 Saldos de vacaciones creados para {Year}", currentYear);
        }

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
                createCmd.CommandTimeout = 120;
                await createCmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("📦 Database created: {DatabaseName}", databaseName);
            }
            else
            {
                _logger.LogInformation("📦 Database already exists: {DatabaseName}", databaseName);
            }
        }

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
                    var terminateQuery = $@"
                        SELECT pg_terminate_backend(pg_stat_activity.pid)
                        FROM pg_stat_activity 
                        WHERE pg_stat_activity.datname = '{dbName}' 
                        AND pid <> pg_backend_pid()";
                        
                    using var terminateCmd = new NpgsqlCommand(terminateQuery, connection);
                    await terminateCmd.ExecuteNonQueryAsync();

                    var dropQuery = $@"DROP DATABASE IF EXISTS ""{dbName}""";
                    using var dropCmd = new NpgsqlCommand(dropQuery, connection);
                    await dropCmd.ExecuteNonQueryAsync();
                    
                    _logger.LogInformation("🗑️ Database dropped: {DatabaseName}", dbName);
                }

                _logger.LogInformation("✅ Limpieza completada");
                Console.WriteLine("\n🧹 Todas las bases de datos han sido eliminadas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la limpieza");
                throw;
            }
        }

        /// <summary>
        /// Versión simplificada para crear tenant básico sin datos de ejemplo
        /// </summary>
        public async Task CreateBasicTenantAsync(string tenantCode, string companyName, string adminEmail, string adminPassword = "admin123")
        {
            _logger.LogInformation("🏢 Creando tenant básico: {TenantCode}", tenantCode);

            try
            {
                if (string.IsNullOrWhiteSpace(tenantCode) || tenantCode.Length < 2)
                {
                    throw new ArgumentException("El código del tenant debe tener al menos 2 caracteres");
                }

                var databaseName = $"SphereTimeControl_{tenantCode.ToLower()}";

                await CreateTenantRecordAsync(tenantCode.ToLower(), companyName, adminEmail);
                await CreateDatabaseIfNotExistsAsync(databaseName);
                await CreateTenantStructureAsync(databaseName);
                await SeedBasicTenantDataAsync(databaseName, companyName, adminEmail, adminPassword);
                
                _logger.LogInformation("✅ Tenant básico {TenantCode} creado exitosamente", tenantCode);
                
                Console.WriteLine($"\n🎉 Tenant '{tenantCode}' creado exitosamente!");
                Console.WriteLine($"🏢 Empresa: {companyName}");
                Console.WriteLine($"📧 Admin: {adminEmail} / {adminPassword}");
                Console.WriteLine($"🗄️ Base de datos: {databaseName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando tenant {TenantCode}", tenantCode);
                throw;
            }
        }

        private async Task SeedBasicTenantDataAsync(string databaseName, string companyName, string adminEmail, string adminPassword)
        {
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            var companyId = await CreateBasicCompanyAsync(connection, companyName, adminEmail);
            var departmentId = await CreateBasicDepartmentAsync(connection, companyId);
            var workScheduleId = await CreateBasicWorkScheduleAsync(connection, companyId);
            var vacationPolicyId = await CreateBasicVacationPolicyAsync(connection, companyId);
            
            await CreateBasicAdminAsync(connection, companyId, departmentId, workScheduleId, adminEmail, adminPassword);
        }

        private async Task<int> CreateBasicCompanyAsync(NpgsqlConnection connection, string companyName, string email)
        {
            var insertQuery = @"
                INSERT INTO ""Companies"" (""Name"", ""Email"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@name, @email, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("name", companyName);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        private async Task<int> CreateBasicDepartmentAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""Departments"" (""CompanyId"", ""Name"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@companyId, @name, @description, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("name", "Administración");
            cmd.Parameters.AddWithValue("description", "Departamento de administración general");
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        private async Task<int> CreateBasicWorkScheduleAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""WorkSchedules"" (""CompanyId"", ""Name"", ""StartTime"", ""EndTime"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@companyId, @name, @startTime, @endTime, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("name", "Horario Estándar");
            cmd.Parameters.AddWithValue("startTime", new TimeSpan(9, 0, 0));
            cmd.Parameters.AddWithValue("endTime", new TimeSpan(18, 0, 0));
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        private async Task<int> CreateBasicVacationPolicyAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""VacationPolicies"" (""CompanyId"", ""Name"", ""AnnualDays"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@companyId, @name, @annualDays, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("name", "Política Estándar");
            cmd.Parameters.AddWithValue("annualDays", 22);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        private async Task CreateBasicAdminAsync(NpgsqlConnection connection, int companyId, int departmentId, int workScheduleId, string email, string password)
        {
            var emailParts = email.Split('@')[0].Split('.');
            var firstName = emailParts.Length > 0 ? char.ToUpper(emailParts[0][0]) + emailParts[0].Substring(1) : "Admin";
            var lastName = emailParts.Length > 1 ? char.ToUpper(emailParts[1][0]) + emailParts[1].Substring(1) : "Usuario";

            var userId = await CreateUserAsync(connection, companyId, firstName, lastName, email, password, 1);
            await CreateEmployeeAsync(connection, companyId, userId, departmentId, firstName, lastName, email, "ADMIN001", "Administrador", password, 1, workScheduleId);
            
            var currentYear = DateTime.Now.Year;
            var insertBalanceQuery = @"
                INSERT INTO ""VacationBalances"" (""EmployeeId"", ""Year"", ""TotalDays"", ""CreatedAt"", ""UpdatedAt"")
                VALUES ((SELECT ""Id"" FROM ""Employees"" WHERE ""Email"" = @email), @year, 22, @createdAt, @updatedAt)";

            using var balanceCmd = new NpgsqlCommand(insertBalanceQuery, connection);
            balanceCmd.Parameters.AddWithValue("email", email);
            balanceCmd.Parameters.AddWithValue("year", currentYear);
            balanceCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            balanceCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
            await balanceCmd.ExecuteNonQueryAsync();
        }
    }
}