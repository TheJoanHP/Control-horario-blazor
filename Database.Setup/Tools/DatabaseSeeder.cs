using Microsoft.Extensions.Logging;
using Shared.Models.Enums;
using Shared.Services.Security;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Database.Setup.Tools
{
    /// <summary>
    /// Herramienta para sembrar datos iniciales en las bases de datos del sistema Sphere Time Control
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

        #region Métodos Principales

        /// <summary>
        /// Sembrar todos los datos necesarios del sistema
        /// </summary>
        public async Task SeedAllAsync()
        {
            _logger.LogInformation("🌱 Iniciando siembra completa de datos del sistema Sphere Time Control");

            try
            {
                // 1. Crear y sembrar base de datos central
                await CreateCentralDatabaseAsync();
                
                // 2. Crear tenant de demostración
                await CreateDemoTenantAsync();
                
                _logger.LogInformation("✅ Siembra completa finalizada exitosamente");
                
                Console.WriteLine("\n🎉 ¡Configuración completada con éxito!");
                Console.WriteLine("=========================================");
                Console.WriteLine("📊 Base de datos central: SphereTimeControl_Central");
                Console.WriteLine("🏢 Tenant demo: SphereTimeControl_demo");
                Console.WriteLine("\n👤 Credenciales Super Admin:");
                Console.WriteLine("   Email: admin@spheretimecontrol.com");
                Console.WriteLine("   Password: admin123");
                Console.WriteLine("\n👨‍💼 Credenciales Company Admin:");
                Console.WriteLine("   Email: admin@empresademo.com");
                Console.WriteLine("   Password: admin123");
                Console.WriteLine("\n👨‍💻 Credenciales Empleado Demo:");
                Console.WriteLine("   Email: juan.perez@empresademo.com");
                Console.WriteLine("   Password: empleado123");
                Console.WriteLine("=========================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error durante la siembra completa del sistema");
                throw;
            }
        }

        /// <summary>
        /// Limpiar todas las bases de datos del sistema
        /// </summary>
        public async Task CleanupAllDatabasesAsync()
        {
            _logger.LogInformation("🧹 Iniciando limpieza completa de bases de datos");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // Obtener todas las bases de datos del sistema
                var getDatabasesQuery = @"
                    SELECT datname FROM pg_database 
                    WHERE datname LIKE 'SphereTimeControl_%'";

                var databases = new List<string>();
                using (var cmd = new NpgsqlCommand(getDatabasesQuery, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        databases.Add(reader.GetString(0));
                    }
                }

                // Eliminar cada base de datos
                foreach (var dbName in databases)
                {
                    // Terminar conexiones activas
                    var terminateQuery = $@"
                        SELECT pg_terminate_backend(pg_stat_activity.pid)
                        FROM pg_stat_activity 
                        WHERE pg_stat_activity.datname = '{dbName}' 
                        AND pid <> pg_backend_pid()";
                        
                    using (var terminateCmd = new NpgsqlCommand(terminateQuery, connection))
                    {
                        await terminateCmd.ExecuteNonQueryAsync();
                    }

                    // Eliminar base de datos
                    var dropQuery = $@"DROP DATABASE IF EXISTS ""{dbName}""";
                    using (var dropCmd = new NpgsqlCommand(dropQuery, connection))
                    {
                        await dropCmd.ExecuteNonQueryAsync();
                    }
                    
                    _logger.LogInformation("🗑️ Base de datos eliminada: {DatabaseName}", dbName);
                }

                _logger.LogInformation("✅ Limpieza completada exitosamente");
                Console.WriteLine("\n🧹 Todas las bases de datos del sistema han sido eliminadas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error durante la limpieza del sistema");
                throw;
            }
        }

        /// <summary>
        /// Mostrar estado de las bases de datos del sistema
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
                Console.WriteLine("=====================================");
                
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"• {reader.GetString(0),-35} {reader.GetString(1),10}");
                }
                
                Console.WriteLine("=====================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estado de las bases de datos");
            }
        }

        #endregion

        #region Base de Datos Central

        /// <summary>
        /// Crear y configurar la base de datos central del sistema
        /// </summary>
        public async Task CreateCentralDatabaseAsync()
        {
            _logger.LogInformation("📦 Creando base de datos central de Sphere Time Control");

            try
            {
                // Crear base de datos si no existe
                await CreateDatabaseIfNotExistsAsync("SphereTimeControl_Central");
                
                var centralConnectionString = _connectionString.Replace("Database=postgres", "Database=SphereTimeControl_Central");
                
                using var connection = new NpgsqlConnection(centralConnectionString);
                await connection.OpenAsync();

                // Crear estructura
                await CreateCentralStructureAsync(connection);
                
                // Sembrar datos iniciales
                await SeedCentralDataAsync(connection);

                _logger.LogInformation("✅ Base de datos central creada y configurada exitosamente");
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
            
            var commands = new List<string>
            {
                // Extensiones PostgreSQL
                @"CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";",
                @"CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";",
                
                // Tabla: Tenants (Empresas/Clientes)
                @"CREATE TABLE IF NOT EXISTS ""Tenants"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Code"" VARCHAR(50) NOT NULL UNIQUE,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Subdomain"" VARCHAR(50) NOT NULL UNIQUE,
                    ""DatabaseName"" VARCHAR(200) NOT NULL UNIQUE,
                    ""ContactEmail"" VARCHAR(255) NOT NULL,
                    ""ContactPhone"" VARCHAR(20),
                    ""Address"" VARCHAR(500),
                    ""City"" VARCHAR(100),
                    ""Country"" VARCHAR(100),
                    ""PostalCode"" VARCHAR(20),
                    ""TaxId"" VARCHAR(50),
                    ""Website"" VARCHAR(255),
                    ""LogoUrl"" VARCHAR(500),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""LicenseType"" INTEGER NOT NULL DEFAULT 0,
                    ""MaxEmployees"" INTEGER NOT NULL DEFAULT 10,
                    ""LicenseExpiresAt"" TIMESTAMP NOT NULL DEFAULT (NOW() + INTERVAL '30 days'),
                    ""MonthlyPrice"" DECIMAL(10,2) DEFAULT 0.00,
                    ""Currency"" VARCHAR(3) DEFAULT 'EUR',
                    ""TrialStartedAt"" TIMESTAMP,
                    ""TrialEndedAt"" TIMESTAMP,
                    ""LastPaymentAt"" TIMESTAMP,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""DeletedAt"" TIMESTAMP
                );",
                
                // Tabla: SphereAdmins (Super Administradores)
                @"CREATE TABLE IF NOT EXISTS ""SphereAdmins"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""FirstName"" VARCHAR(100) NOT NULL,
                    ""LastName"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""Phone"" VARCHAR(20),
                    ""AvatarUrl"" VARCHAR(500),
                    ""Role"" INTEGER NOT NULL DEFAULT 0,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""EmailVerified"" BOOLEAN NOT NULL DEFAULT false,
                    ""EmailVerificationToken"" VARCHAR(255),
                    ""PasswordResetToken"" VARCHAR(255),
                    ""PasswordResetExpires"" TIMESTAMP,
                    ""LastLoginAt"" TIMESTAMP,
                    ""LastLoginIp"" VARCHAR(50),
                    ""FailedLoginAttempts"" INTEGER DEFAULT 0,
                    ""LockedUntil"" TIMESTAMP,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Tabla: SystemConfigs (Configuración del Sistema)
                @"CREATE TABLE IF NOT EXISTS ""SystemConfigs"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Category"" VARCHAR(50) NOT NULL,
                    ""Key"" VARCHAR(100) NOT NULL UNIQUE,
                    ""Value"" TEXT NOT NULL,
                    ""ValueType"" VARCHAR(20) DEFAULT 'string',
                    ""Description"" VARCHAR(500),
                    ""IsPublic"" BOOLEAN NOT NULL DEFAULT false,
                    ""IsEncrypted"" BOOLEAN NOT NULL DEFAULT false,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Índices para optimización
                @"CREATE INDEX IF NOT EXISTS ""IX_Tenants_Subdomain"" ON ""Tenants"" (""Subdomain"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Tenants_Active"" ON ""Tenants"" (""Active"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_SphereAdmins_Email"" ON ""SphereAdmins"" (""Email"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_SystemConfigs_Key"" ON ""SystemConfigs"" (""Key"");"
            };

            // Ejecutar cada comando
            foreach (var command in commands)
            {
                try
                {
                    using var cmd = new NpgsqlCommand(command, connection);
                    cmd.CommandTimeout = 120;
                    await cmd.ExecuteNonQueryAsync();
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ejecutando comando SQL en BD central");
                    throw;
                }
            }
        }

        private async Task SeedCentralDataAsync(NpgsqlConnection connection)
        {
            _logger.LogInformation("🌱 Sembrando datos iniciales en BD central");

            // Crear configuraciones del sistema
            await CreateSystemConfigurationsAsync(connection);
            
            // Crear super administrador
            await CreateSuperAdminAsync(connection);
            
            _logger.LogInformation("✅ Datos centrales sembrados exitosamente");
        }

        private async Task CreateSystemConfigurationsAsync(NpgsqlConnection connection)
        {
            var configs = new Dictionary<string, (string category, string value, string description)>
            {
                // Sistema
                { "SystemName", ("System", "Sphere Time Control", "Nombre del sistema") },
                { "SystemVersion", ("System", "1.0.0", "Versión actual del sistema") },
                { "DefaultLanguage", ("System", "es", "Idioma por defecto") },
                { "TimeZone", ("System", "Europe/Madrid", "Zona horaria por defecto") },
                
                // Licencias
                { "TrialDurationDays", ("License", "30", "Duración del período de prueba en días") },
                { "MaxEmployeesTrial", ("License", "5", "Máximo de empleados en versión Trial") },
                
                // Precios
                { "DefaultCurrency", ("Billing", "EUR", "Moneda por defecto") },
                { "TrialMonthlyPrice", ("Billing", "0.00", "Precio mensual versión Trial") },
                
                // Seguridad
                { "PasswordMinLength", ("Security", "8", "Longitud mínima de contraseña") },
                { "MaxLoginAttempts", ("Security", "5", "Máximo de intentos de login") },
                { "SessionTimeoutMinutes", ("Security", "60", "Timeout de sesión en minutos") }
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
                        INSERT INTO ""SystemConfigs"" (""Category"", ""Key"", ""Value"", ""Description"", ""CreatedAt"", ""UpdatedAt"")
                        VALUES (@category, @key, @value, @description, @createdAt, @updatedAt)";

                    using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                    insertCmd.Parameters.AddWithValue("category", config.Value.category);
                    insertCmd.Parameters.AddWithValue("key", config.Key);
                    insertCmd.Parameters.AddWithValue("value", config.Value.value);
                    insertCmd.Parameters.AddWithValue("description", config.Value.description);
                    insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                    insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            
            _logger.LogInformation("⚙️ Configuraciones del sistema creadas");
        }

        private async Task CreateSuperAdminAsync(NpgsqlConnection connection)
        {
            var email = "admin@spheretimecontrol.com";
            
            var checkQuery = @"SELECT COUNT(*) FROM ""SphereAdmins"" WHERE ""Email"" = @email";
            using var checkCmd = new NpgsqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("email", email);
            
            var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
            
            if (!exists)
            {
                var passwordHash = _passwordService.HashPassword("admin123");
                
                var insertQuery = @"
                    INSERT INTO ""SphereAdmins"" (
                        ""FirstName"", ""LastName"", ""Email"", ""PasswordHash"", 
                        ""Role"", ""Active"", ""EmailVerified"", ""CreatedAt"", ""UpdatedAt""
                    )
                    VALUES (
                        @firstName, @lastName, @email, @passwordHash, 
                        @role, @active, @emailVerified, @createdAt, @updatedAt
                    )";

                using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("firstName", "Super");
                insertCmd.Parameters.AddWithValue("lastName", "Admin");
                insertCmd.Parameters.AddWithValue("email", email);
                insertCmd.Parameters.AddWithValue("passwordHash", passwordHash);
                insertCmd.Parameters.AddWithValue("role", 0); // SuperAdmin
                insertCmd.Parameters.AddWithValue("active", true);
                insertCmd.Parameters.AddWithValue("emailVerified", true);
                insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                await insertCmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("👤 Super administrador creado: {Email}", email);
            }
        }

        #endregion

        #region Base de Datos Tenant

        /// <summary>
        /// Crear tenant de demostración completo
        /// </summary>
        public async Task CreateDemoTenantAsync()
        {
            _logger.LogInformation("🏢 Creando tenant de demostración");

            try
            {
                // Registrar en BD central
                await CreateTenantRecordAsync("demo", "Empresa Demo S.L.", "admin@empresademo.com");
                
                // Crear base de datos del tenant
                await CreateDatabaseIfNotExistsAsync("SphereTimeControl_demo");
                
                // Crear estructura del tenant (SIMPLIFICADA)
                await CreateSimpleTenantStructureAsync("SphereTimeControl_demo");
                
                // Sembrar datos básicos del tenant
                await SeedSimpleTenantDataAsync("SphereTimeControl_demo");
                
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

            // Verificar si ya existe
            var checkQuery = @"SELECT COUNT(*) FROM ""Tenants"" WHERE ""Code"" = @code";
            using var checkCmd = new NpgsqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("code", tenantCode);
            
            var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
            
            if (!exists)
            {
                var insertQuery = @"
                    INSERT INTO ""Tenants"" (
                        ""Code"", ""Name"", ""Subdomain"", ""DatabaseName"", 
                        ""ContactEmail"", ""Active"", ""LicenseType"", ""MaxEmployees"",
                        ""LicenseExpiresAt"", ""CreatedAt"", ""UpdatedAt""
                    )
                    VALUES (
                        @code, @name, @subdomain, @databaseName, 
                        @contactEmail, @active, @licenseType, @maxEmployees,
                        @licenseExpiresAt, @createdAt, @updatedAt
                    )";

                using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("code", tenantCode);
                insertCmd.Parameters.AddWithValue("name", companyName);
                insertCmd.Parameters.AddWithValue("subdomain", tenantCode.ToLower());
                insertCmd.Parameters.AddWithValue("databaseName", $"SphereTimeControl_{tenantCode}");
                insertCmd.Parameters.AddWithValue("contactEmail", adminEmail);
                insertCmd.Parameters.AddWithValue("active", true);
                insertCmd.Parameters.AddWithValue("licenseType", 0); // Trial
                insertCmd.Parameters.AddWithValue("maxEmployees", 5);
                insertCmd.Parameters.AddWithValue("licenseExpiresAt", DateTime.UtcNow.AddDays(30));
                insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                await insertCmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("📝 Tenant registrado en BD central: {TenantCode}", tenantCode);
            }
        }

        private async Task CreateSimpleTenantStructureAsync(string databaseName)
        {
            _logger.LogInformation("🏗️ Creando estructura SIMPLIFICADA del tenant: {DatabaseName}", databaseName);
            
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            var commands = new List<string>
            {
                // Tabla: Companies (SIMPLIFICADA)
                @"CREATE TABLE IF NOT EXISTS ""Companies"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Tabla: Users (SIMPLIFICADA)
                @"CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""FirstName"" VARCHAR(100) NOT NULL,
                    ""LastName"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""Role"" INTEGER NOT NULL DEFAULT 3,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Users_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
                );",
                
                // Tabla: Employees (SIMPLIFICADA)
                @"CREATE TABLE IF NOT EXISTS ""Employees"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""UserId"" INTEGER NOT NULL,
                    ""EmployeeCode"" VARCHAR(50) NOT NULL,
                    ""FirstName"" VARCHAR(100) NOT NULL,
                    ""LastName"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL,
                    ""Position"" VARCHAR(100),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Employees_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Employees_Users"" FOREIGN KEY (""UserId"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
                );",
                
                // Tabla: TimeRecords (SIMPLIFICADA)
                @"CREATE TABLE IF NOT EXISTS ""TimeRecords"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""Type"" INTEGER NOT NULL,
                    ""Date"" DATE NOT NULL,
                    ""Time"" TIME NOT NULL,
                    ""Timestamp"" TIMESTAMP NOT NULL,
                    ""Status"" INTEGER DEFAULT 1,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_TimeRecords_Employees"" FOREIGN KEY (""EmployeeId"") 
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ejecutando comando SQL en tenant");
                    throw;
                }
            }
            
            _logger.LogInformation("✅ Estructura SIMPLIFICADA del tenant creada");
        }

        private async Task SeedSimpleTenantDataAsync(string databaseName)
        {
            _logger.LogInformation("🌱 Sembrando datos BÁSICOS del tenant: {DatabaseName}", databaseName);
            
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            // 1. Crear empresa
            var companyId = await CreateSimpleCompanyAsync(connection);
            
            // 2. Crear administrador
            await CreateSimpleAdminAsync(connection, companyId);
            
            // 3. Crear empleado de prueba
            await CreateSimpleEmployeeAsync(connection, companyId);
            
            _logger.LogInformation("✅ Datos básicos del tenant sembrados");
        }

        private async Task<int> CreateSimpleCompanyAsync(NpgsqlConnection connection)
        {
            var insertQuery = @"
                INSERT INTO ""Companies"" (""Name"", ""Email"", ""CreatedAt"", ""UpdatedAt"")
                VALUES (@name, @email, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("name", "Empresa Demo S.L.");
            cmd.Parameters.AddWithValue("email", "info@empresademo.com");
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        private async Task CreateSimpleAdminAsync(NpgsqlConnection connection, int companyId)
        {
            var passwordHash = _passwordService.HashPassword("admin123");
            
            // Crear usuario admin
            var userQuery = @"
                INSERT INTO ""Users"" (
                    ""CompanyId"", ""FirstName"", ""LastName"", ""Email"", 
                    ""PasswordHash"", ""Role"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @firstName, @lastName, @email, 
                    @passwordHash, @role, @active, @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            int userId;
            using (var cmd = new NpgsqlCommand(userQuery, connection))
            {
                cmd.Parameters.AddWithValue("companyId", companyId);
                cmd.Parameters.AddWithValue("firstName", "Ana");
                cmd.Parameters.AddWithValue("lastName", "García");
                cmd.Parameters.AddWithValue("email", "admin@empresademo.com");
                cmd.Parameters.AddWithValue("passwordHash", passwordHash);
                cmd.Parameters.AddWithValue("role", 1); // CompanyAdmin
                cmd.Parameters.AddWithValue("active", true);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                userId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            }

            // Crear empleado admin
            var employeeQuery = @"
                INSERT INTO ""Employees"" (
                    ""CompanyId"", ""UserId"", ""EmployeeCode"", ""FirstName"", ""LastName"", 
                    ""Email"", ""Position"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @userId, @employeeCode, @firstName, @lastName, 
                    @email, @position, @active, @createdAt, @updatedAt
                )";

            using var empCmd = new NpgsqlCommand(employeeQuery, connection);
            empCmd.Parameters.AddWithValue("companyId", companyId);
            empCmd.Parameters.AddWithValue("userId", userId);
            empCmd.Parameters.AddWithValue("employeeCode", "ADMIN001");
            empCmd.Parameters.AddWithValue("firstName", "Ana");
            empCmd.Parameters.AddWithValue("lastName", "García");
            empCmd.Parameters.AddWithValue("email", "admin@empresademo.com");
            empCmd.Parameters.AddWithValue("position", "Directora General");
            empCmd.Parameters.AddWithValue("active", true);
            empCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            empCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            await empCmd.ExecuteNonQueryAsync();
        }

        private async Task CreateSimpleEmployeeAsync(NpgsqlConnection connection, int companyId)
        {
            var passwordHash = _passwordService.HashPassword("empleado123");
            
            // Crear usuario empleado
            var userQuery = @"
                INSERT INTO ""Users"" (
                    ""CompanyId"", ""FirstName"", ""LastName"", ""Email"", 
                    ""PasswordHash"", ""Role"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @firstName, @lastName, @email, 
                    @passwordHash, @role, @active, @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            int userId;
            using (var cmd = new NpgsqlCommand(userQuery, connection))
            {
                cmd.Parameters.AddWithValue("companyId", companyId);
                cmd.Parameters.AddWithValue("firstName", "Juan");
                cmd.Parameters.AddWithValue("lastName", "Pérez");
                cmd.Parameters.AddWithValue("email", "juan.perez@empresademo.com");
                cmd.Parameters.AddWithValue("passwordHash", passwordHash);
                cmd.Parameters.AddWithValue("role", 3); // Employee
                cmd.Parameters.AddWithValue("active", true);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                userId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            }

            // Crear empleado
            var employeeQuery = @"
                INSERT INTO ""Employees"" (
                    ""CompanyId"", ""UserId"", ""EmployeeCode"", ""FirstName"", ""LastName"", 
                    ""Email"", ""Position"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @userId, @employeeCode, @firstName, @lastName, 
                    @email, @position, @active, @createdAt, @updatedAt
                )";

            using var empCmd = new NpgsqlCommand(employeeQuery, connection);
            empCmd.Parameters.AddWithValue("companyId", companyId);
            empCmd.Parameters.AddWithValue("userId", userId);
            empCmd.Parameters.AddWithValue("employeeCode", "EMP001");
            empCmd.Parameters.AddWithValue("firstName", "Juan");
            empCmd.Parameters.AddWithValue("lastName", "Pérez");
            empCmd.Parameters.AddWithValue("email", "juan.perez@empresademo.com");
            empCmd.Parameters.AddWithValue("position", "Desarrollador");
            empCmd.Parameters.AddWithValue("active", true);
            empCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            empCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            await empCmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Métodos Auxiliares

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
                createCmd.CommandTimeout = 120;
                await createCmd.ExecuteNonQueryAsync();
                
                _logger.LogInformation("📦 Base de datos creada: {DatabaseName}", databaseName);
            }
            else
            {
                _logger.LogInformation("📦 Base de datos ya existe: {DatabaseName}", databaseName);
            }
        }

        /// <summary>
        /// Crear un tenant básico con configuración mínima
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

                // Registrar en BD central
                await CreateTenantRecordAsync(tenantCode, companyName, adminEmail);
                
                // Crear base de datos
                await CreateDatabaseIfNotExistsAsync(databaseName);
                
                // Crear estructura
                await CreateSimpleTenantStructureAsync(databaseName);
                
                // Sembrar datos básicos
                await SeedBasicTenantDataAsync(databaseName, companyName, adminEmail, adminPassword);
                
                _logger.LogInformation("✅ Tenant básico {TenantCode} creado exitosamente", tenantCode);
                
                Console.WriteLine($"\n🎉 Tenant '{tenantCode}' creado exitosamente!");
                Console.WriteLine($"🏢 Empresa: {companyName}");
                Console.WriteLine($"📧 Admin: {adminEmail} / {adminPassword}");
                Console.WriteLine($"🗄️ Base de datos: {databaseName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creando tenant básico {TenantCode}", tenantCode);
                throw;
            }
        }

        private async Task SeedBasicTenantDataAsync(string databaseName, string companyName, string adminEmail, string adminPassword)
        {
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            // Crear empresa
            var companyId = await CreateBasicCompanyAsync(connection, companyName, adminEmail);
            
            // Crear usuario administrador
            await CreateBasicAdminUserAsync(connection, companyId, adminEmail, adminPassword);
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

        private async Task CreateBasicAdminUserAsync(NpgsqlConnection connection, int companyId, string email, string password)
        {
            // Extraer nombre del email
            var emailParts = email.Split('@')[0].Split('.');
            var firstName = emailParts.Length > 0 ? 
                char.ToUpper(emailParts[0][0]) + emailParts[0].Substring(1) : "Admin";
            var lastName = emailParts.Length > 1 ? 
                char.ToUpper(emailParts[1][0]) + emailParts[1].Substring(1) : "Usuario";

            var passwordHash = _passwordService.HashPassword(password);
            
            // Crear usuario
            var userQuery = @"
                INSERT INTO ""Users"" (
                    ""CompanyId"", ""FirstName"", ""LastName"", ""Email"", 
                    ""PasswordHash"", ""Role"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @firstName, @lastName, @email, 
                    @passwordHash, @role, @active, @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            int userId;
            using (var cmd = new NpgsqlCommand(userQuery, connection))
            {
                cmd.Parameters.AddWithValue("companyId", companyId);
                cmd.Parameters.AddWithValue("firstName", firstName);
                cmd.Parameters.AddWithValue("lastName", lastName);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("passwordHash", passwordHash);
                cmd.Parameters.AddWithValue("role", 1); // CompanyAdmin
                cmd.Parameters.AddWithValue("active", true);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                userId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            }

            // Crear empleado
            var employeeQuery = @"
                INSERT INTO ""Employees"" (
                    ""CompanyId"", ""UserId"", ""EmployeeCode"", ""FirstName"", ""LastName"", 
                    ""Email"", ""Position"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @userId, @employeeCode, @firstName, @lastName, 
                    @email, @position, @active, @createdAt, @updatedAt
                )";

            using var empCmd = new NpgsqlCommand(employeeQuery, connection);
            empCmd.Parameters.AddWithValue("companyId", companyId);
            empCmd.Parameters.AddWithValue("userId", userId);
            empCmd.Parameters.AddWithValue("employeeCode", "ADMIN001");
            empCmd.Parameters.AddWithValue("firstName", firstName);
            empCmd.Parameters.AddWithValue("lastName", lastName);
            empCmd.Parameters.AddWithValue("email", email);
            empCmd.Parameters.AddWithValue("position", "Administrador");
            empCmd.Parameters.AddWithValue("active", true);
            empCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            empCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
            
            await empCmd.ExecuteNonQueryAsync();
        }

        #endregion
    }
}