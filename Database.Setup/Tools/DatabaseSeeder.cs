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
                    ""Description"" TEXT,
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
                
                // Tabla: Licenses (Licencias de Tenants)
                @"CREATE TABLE IF NOT EXISTS ""Licenses"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""TenantId"" INTEGER NOT NULL,
                    ""Type"" INTEGER NOT NULL DEFAULT 0,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""MaxEmployees"" INTEGER NOT NULL,
                    ""MaxDepartments"" INTEGER DEFAULT 10,
                    ""MaxLocations"" INTEGER DEFAULT 1,
                    ""Features"" JSONB,
                    ""MonthlyPrice"" DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                    ""YearlyPrice"" DECIMAL(10,2),
                    ""Currency"" VARCHAR(3) DEFAULT 'EUR',
                    ""BillingCycle"" INTEGER DEFAULT 1,
                    ""StartDate"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""EndDate"" TIMESTAMP NOT NULL,
                    ""RenewalDate"" TIMESTAMP,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""AutoRenew"" BOOLEAN NOT NULL DEFAULT false,
                    ""IsTrial"" BOOLEAN NOT NULL DEFAULT false,
                    ""Notes"" TEXT,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Licenses_Tenants"" FOREIGN KEY (""TenantId"") 
                        REFERENCES ""Tenants"" (""Id"") ON DELETE CASCADE
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
                
                // Tabla: BillingRecords (Registros de Facturación)
                @"CREATE TABLE IF NOT EXISTS ""BillingRecords"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""TenantId"" INTEGER NOT NULL,
                    ""LicenseId"" INTEGER NOT NULL,
                    ""InvoiceNumber"" VARCHAR(100) UNIQUE,
                    ""Amount"" DECIMAL(10,2) NOT NULL,
                    ""Tax"" DECIMAL(10,2) DEFAULT 0.00,
                    ""TotalAmount"" DECIMAL(10,2) NOT NULL,
                    ""Currency"" VARCHAR(3) NOT NULL DEFAULT 'EUR',
                    ""PeriodStart"" TIMESTAMP NOT NULL,
                    ""PeriodEnd"" TIMESTAMP NOT NULL,
                    ""Status"" INTEGER NOT NULL DEFAULT 0,
                    ""PaymentMethod"" VARCHAR(50),
                    ""TransactionId"" VARCHAR(255),
                    ""PaidAt"" TIMESTAMP,
                    ""DueDate"" DATE,
                    ""Notes"" TEXT,
                    ""InvoiceUrl"" VARCHAR(500),
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_BillingRecords_Tenants"" FOREIGN KEY (""TenantId"") 
                        REFERENCES ""Tenants"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_BillingRecords_Licenses"" FOREIGN KEY (""LicenseId"") 
                        REFERENCES ""Licenses"" (""Id"") ON DELETE CASCADE
                );",
                
                // Tabla: AuditLogs (Logs de Auditoría)
                @"CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""TenantId"" INTEGER,
                    ""UserId"" INTEGER,
                    ""UserEmail"" VARCHAR(255),
                    ""Action"" VARCHAR(100) NOT NULL,
                    ""EntityType"" VARCHAR(100),
                    ""EntityId"" INTEGER,
                    ""OldValues"" JSONB,
                    ""NewValues"" JSONB,
                    ""IpAddress"" VARCHAR(50),
                    ""UserAgent"" TEXT,
                    ""Success"" BOOLEAN DEFAULT true,
                    ""ErrorMessage"" TEXT,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Tabla: EmailTemplates (Plantillas de Email)
                @"CREATE TABLE IF NOT EXISTS ""EmailTemplates"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Code"" VARCHAR(50) NOT NULL UNIQUE,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Subject"" VARCHAR(255) NOT NULL,
                    ""Body"" TEXT NOT NULL,
                    ""Variables"" JSONB,
                    ""Language"" VARCHAR(5) DEFAULT 'es',
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Índices para optimización
                @"CREATE INDEX IF NOT EXISTS ""IX_Tenants_Subdomain"" ON ""Tenants"" (""Subdomain"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Tenants_Active"" ON ""Tenants"" (""Active"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Tenants_LicenseType"" ON ""Tenants"" (""LicenseType"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_SphereAdmins_Email"" ON ""SphereAdmins"" (""Email"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_SystemConfigs_Key"" ON ""SystemConfigs"" (""Key"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_SystemConfigs_Category"" ON ""SystemConfigs"" (""Category"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Licenses_TenantId"" ON ""Licenses"" (""TenantId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_BillingRecords_TenantId"" ON ""BillingRecords"" (""TenantId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_BillingRecords_InvoiceNumber"" ON ""BillingRecords"" (""InvoiceNumber"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_TenantId"" ON ""AuditLogs"" (""TenantId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_CreatedAt"" ON ""AuditLogs"" (""CreatedAt"");"
            };

            // Ejecutar cada comando
            foreach (var command in commands)
            {
                try
                {
                    using var cmd = new NpgsqlCommand(command, connection);
                    cmd.CommandTimeout = 120;
                    await cmd.ExecuteNonQueryAsync();
                    await Task.Delay(50); // Pequeña pausa entre comandos
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error ejecutando comando SQL en tenant");
                    throw;
                }
            }
        }

        private async Task CreateTenantIndexesAsync(NpgsqlConnection connection)
        {
            var indexes = new List<string>
            {
                // Índices únicos
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Companies_Code"" ON ""Companies"" (""Code"") WHERE ""Code"" IS NOT NULL;",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Companies_Email"" ON ""Companies"" (""Email"");",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Employees_CompanyId_EmployeeCode"" ON ""Employees"" (""CompanyId"", ""EmployeeCode"");",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Employees_Email"" ON ""Employees"" (""Email"");",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Departments_CompanyId_Code"" ON ""Departments"" (""CompanyId"", ""Code"") WHERE ""Code"" IS NOT NULL;",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Locations_CompanyId_Code"" ON ""Locations"" (""CompanyId"", ""Code"") WHERE ""Code"" IS NOT NULL;",
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_WorkSchedules_CompanyId_Code"" ON ""WorkSchedules"" (""CompanyId"", ""Code"") WHERE ""Code"" IS NOT NULL;",
                
                // Índices de búsqueda y rendimiento
                @"CREATE INDEX IF NOT EXISTS ""IX_Employees_CompanyId"" ON ""Employees"" (""CompanyId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Employees_DepartmentId"" ON ""Employees"" (""DepartmentId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Employees_LocationId"" ON ""Employees"" (""LocationId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Employees_ManagerId"" ON ""Employees"" (""ManagerId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Employees_Active"" ON ""Employees"" (""Active"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_TimeRecords_EmployeeId_Date"" ON ""TimeRecords"" (""EmployeeId"", ""Date"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_TimeRecords_Date"" ON ""TimeRecords"" (""Date"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_TimeRecords_Timestamp"" ON ""TimeRecords"" (""Timestamp"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_TimeRecords_Status"" ON ""TimeRecords"" (""Status"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Breaks_EmployeeId_Date"" ON ""Breaks"" (""EmployeeId"", ""Date"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Overtime_EmployeeId_Date"" ON ""Overtime"" (""EmployeeId"", ""Date"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Overtime_Status"" ON ""Overtime"" (""Status"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_VacationRequests_EmployeeId"" ON ""VacationRequests"" (""EmployeeId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_VacationRequests_Status"" ON ""VacationRequests"" (""Status"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_VacationRequests_StartDate_EndDate"" ON ""VacationRequests"" (""StartDate"", ""EndDate"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_VacationBalances_EmployeeId_Year"" ON ""VacationBalances"" (""EmployeeId"", ""Year"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_Holidays_CompanyId_Date"" ON ""Holidays"" (""CompanyId"", ""Date"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_CreatedAt"" ON ""AuditLogs"" (""CreatedAt"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_UserId"" ON ""AuditLogs"" (""UserId"");",
                @"CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_EntityType_EntityId"" ON ""AuditLogs"" (""EntityType"", ""EntityId"");"
            };

            foreach (var index in indexes)
            {
                try
                {
                    using var cmd = new NpgsqlCommand(index, connection);
                    cmd.CommandTimeout = 120;
                    await cmd.ExecuteNonQueryAsync();
                    await Task.Delay(25);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Advertencia creando índice (puede que ya exista)");
                    // No lanzar excepción, continuar con otros índices
                }
            }
        }

        private async Task CreateTenantViewsAsync(NpgsqlConnection connection)
        {
            var views = new List<string>
            {
                // Vista de empleados con información completa
                @"CREATE OR REPLACE VIEW ""EmployeesView"" AS
                SELECT 
                    e.""Id"",
                    e.""CompanyId"",
                    e.""UserId"",
                    e.""DepartmentId"",
                    e.""LocationId"",
                    e.""EmployeeCode"",
                    e.""FirstName"",
                    e.""LastName"",
                    (e.""FirstName"" || ' ' || e.""LastName"") as ""FullName"",
                    e.""Email"",
                    e.""Phone"",
                    e.""Position"",
                    e.""Active"",
                    e.""HireDate"",
                    e.""CreatedAt"",
                    u.""Role"" as ""UserRole"",
                    u.""LastLoginAt"",
                    d.""Name"" as ""DepartmentName"",
                    l.""Name"" as ""LocationName"",
                    c.""Name"" as ""CompanyName"",
                    m.""FirstName"" || ' ' || m.""LastName"" as ""ManagerName""
                FROM ""Employees"" e
                INNER JOIN ""Users"" u ON e.""UserId"" = u.""Id""
                LEFT JOIN ""Departments"" d ON e.""DepartmentId"" = d.""Id""
                LEFT JOIN ""Locations"" l ON e.""LocationId"" = l.""Id""
                LEFT JOIN ""Companies"" c ON e.""CompanyId"" = c.""Id""
                LEFT JOIN ""Employees"" m ON e.""ManagerId"" = m.""Id"";",
                
                // Vista de registros de tiempo con información del empleado
                @"CREATE OR REPLACE VIEW ""TimeRecordsView"" AS
                SELECT 
                    tr.""Id"",
                    tr.""EmployeeId"",
                    tr.""Type"",
                    tr.""Date"",
                    tr.""Time"",
                    tr.""Timestamp"",
                    tr.""Location"",
                    tr.""Latitude"",
                    tr.""Longitude"",
                    tr.""Method"",
                    tr.""Status"",
                    tr.""IsManualEntry"",
                    tr.""Notes"",
                    tr.""CreatedAt"",
                    e.""EmployeeCode"",
                    e.""FirstName"",
                    e.""LastName"",
                    (e.""FirstName"" || ' ' || e.""LastName"") as ""EmployeeName"",
                    e.""DepartmentId"",
                    d.""Name"" as ""DepartmentName"",
                    l.""Name"" as ""LocationName"",
                    CASE tr.""Type""
                        WHEN 0 THEN 'Entrada'
                        WHEN 1 THEN 'Salida'
                        WHEN 2 THEN 'Inicio Descanso'
                        WHEN 3 THEN 'Fin Descanso'
                        ELSE 'Otro'
                    END as ""TypeDisplay"",
                    CASE tr.""Status""
                        WHEN 0 THEN 'Pendiente'
                        WHEN 1 THEN 'Aprobado'
                        WHEN 2 THEN 'Rechazado'
                        ELSE 'Desconocido'
                    END as ""StatusDisplay""
                FROM ""TimeRecords"" tr
                INNER JOIN ""Employees"" e ON tr.""EmployeeId"" = e.""Id""
                LEFT JOIN ""Departments"" d ON e.""DepartmentId"" = d.""Id""
                LEFT JOIN ""Locations"" l ON tr.""LocationId"" = l.""Id"";",
                
                // Vista de solicitudes de vacaciones con información completa
                @"CREATE OR REPLACE VIEW ""VacationRequestsView"" AS
                SELECT 
                    vr.""Id"",
                    vr.""EmployeeId"",
                    vr.""Type"",
                    vr.""StartDate"",
                    vr.""EndDate"",
                    vr.""TotalDays"",
                    vr.""Status"",
                    vr.""Reason"",
                    vr.""ReviewedAt"",
                    vr.""CreatedAt"",
                    e.""EmployeeCode"",
                    e.""FirstName"",
                    e.""LastName"",
                    (e.""FirstName"" || ' ' || e.""LastName"") as ""EmployeeName"",
                    e.""Email"" as ""EmployeeEmail"",
                    d.""Name"" as ""DepartmentName"",
                    vp.""Name"" as ""PolicyName"",
                    r.""FirstName"" || ' ' || r.""LastName"" as ""ReviewerName"",
                    CASE vr.""Status""
                        WHEN 0 THEN 'Pendiente'
                        WHEN 1 THEN 'Aprobada'
                        WHEN 2 THEN 'Rechazada'
                        WHEN 3 THEN 'Cancelada'
                        ELSE 'Desconocido'
                    END as ""StatusDisplay""
                FROM ""VacationRequests"" vr
                INNER JOIN ""Employees"" e ON vr.""EmployeeId"" = e.""Id""
                LEFT JOIN ""Departments"" d ON e.""DepartmentId"" = d.""Id""
                LEFT JOIN ""VacationPolicies"" vp ON vr.""VacationPolicyId"" = vp.""Id""
                LEFT JOIN ""Users"" r ON vr.""ReviewedById"" = r.""Id"";",
                
                // Vista de resumen diario de asistencia
                @"CREATE OR REPLACE VIEW ""DailyAttendanceSummary"" AS
                SELECT 
                    e.""Id"" as ""EmployeeId"",
                    e.""EmployeeCode"",
                    e.""FirstName"" || ' ' || e.""LastName"" as ""EmployeeName"",
                    tr.""Date"",
                    MIN(CASE WHEN tr.""Type"" = 0 THEN tr.""Time"" END) as ""FirstCheckIn"",
                    MAX(CASE WHEN tr.""Type"" = 1 THEN tr.""Time"" END) as ""LastCheckOut"",
                    COUNT(CASE WHEN tr.""Type"" = 0 THEN 1 END) as ""CheckInCount"",
                    COUNT(CASE WHEN tr.""Type"" = 1 THEN 1 END) as ""CheckOutCount"",
                    COUNT(CASE WHEN tr.""Type"" = 2 THEN 1 END) as ""BreakStartCount"",
                    COUNT(CASE WHEN tr.""Type"" = 3 THEN 1 END) as ""BreakEndCount""
                FROM ""Employees"" e
                LEFT JOIN ""TimeRecords"" tr ON e.""Id"" = tr.""EmployeeId""
                WHERE tr.""Date"" IS NOT NULL
                GROUP BY e.""Id"", e.""EmployeeCode"", e.""FirstName"", e.""LastName"", tr.""Date"";"
            };

            foreach (var view in views)
            {
                try
                {
                    using var cmd = new NpgsqlCommand(view, connection);
                    cmd.CommandTimeout = 120;
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Advertencia creando vista");
                }
            }
        }

        private async Task CreateTenantTriggersAsync(NpgsqlConnection connection)
        {
            // Función para actualizar UpdatedAt automáticamente
            var updateFunction = @"
                CREATE OR REPLACE FUNCTION update_updated_at_column()
                RETURNS TRIGGER AS $
                BEGIN
                    NEW.""UpdatedAt"" = NOW();
                    RETURN NEW;
                END;
                $ LANGUAGE plpgsql;";

            try
            {
                using var funcCmd = new NpgsqlCommand(updateFunction, connection);
                await funcCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Advertencia creando función de trigger");
            }

            // Crear triggers para actualizar UpdatedAt
            var tables = new[] 
            { 
                "Companies", "Departments", "Locations", "Users", "Employees", 
                "WorkSchedules", "TimeRecords", "Breaks", "Overtime",
                "VacationPolicies", "VacationRequests", "VacationBalances", "Holidays"
            };

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
                    await triggerCmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Advertencia creando trigger para tabla {Table}", table);
                }
            }
        }

        private async Task SeedTenantDataAsync(string databaseName)
        {
            _logger.LogInformation("🌱 Sembrando datos del tenant: {DatabaseName}", databaseName);
            
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            // Crear empresa
            var companyId = await CreateCompanyAsync(connection);
            
            // Crear ubicación principal
            var locationId = await CreateMainLocationAsync(connection, companyId);
            
            // Crear departamentos
            var departmentIds = await CreateDepartmentsAsync(connection, companyId);
            
            // Crear horarios de trabajo
            var workScheduleIds = await CreateWorkSchedulesAsync(connection, companyId);
            
            // Crear políticas de vacaciones
            var vacationPolicyId = await CreateVacationPolicyAsync(connection, companyId);
            
            // Crear empleados (admin y empleados de prueba)
            await CreateEmployeesAsync(connection, companyId, departmentIds, locationId, workScheduleIds["standard"], vacationPolicyId);
            
            // Crear saldos de vacaciones
            await CreateVacationBalancesAsync(connection, vacationPolicyId);
            
            // Crear días festivos
            await CreateHolidaysAsync(connection, companyId);
            
            // Crear algunos registros de tiempo de ejemplo
            await CreateSampleTimeRecordsAsync(connection);
            
            _logger.LogInformation("✅ Datos del tenant sembrados exitosamente");
        }

        private async Task<int> CreateCompanyAsync(NpgsqlConnection connection)
        {
            var insertQuery = @"
                INSERT INTO ""Companies"" (
                    ""Name"", ""Code"", ""TaxId"", ""Email"", ""Phone"", 
                    ""Website"", ""Address"", ""City"", ""State"", ""Country"", ""PostalCode"",
                    ""WorkStartTime"", ""WorkEndTime"", ""LunchStartTime"", ""LunchEndTime"",
                    ""ToleranceMinutes"", ""VacationDaysPerYear"", ""SickDaysPerYear"",
                    ""Currency"", ""TimeZone"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @name, @code, @taxId, @email, @phone,
                    @website, @address, @city, @state, @country, @postalCode,
                    @workStartTime, @workEndTime, @lunchStartTime, @lunchEndTime,
                    @toleranceMinutes, @vacationDaysPerYear, @sickDaysPerYear,
                    @currency, @timeZone, @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("name", "Empresa Demo S.L.");
            cmd.Parameters.AddWithValue("code", "DEMO");
            cmd.Parameters.AddWithValue("taxId", "B12345678");
            cmd.Parameters.AddWithValue("email", "info@empresademo.com");
            cmd.Parameters.AddWithValue("phone", "+34 911 234 567");
            cmd.Parameters.AddWithValue("website", "https://www.empresademo.com");
            cmd.Parameters.AddWithValue("address", "Calle Principal 123");
            cmd.Parameters.AddWithValue("city", "Madrid");
            cmd.Parameters.AddWithValue("state", "Madrid");
            cmd.Parameters.AddWithValue("country", "España");
            cmd.Parameters.AddWithValue("postalCode", "28001");
            cmd.Parameters.AddWithValue("workStartTime", new TimeSpan(9, 0, 0));
            cmd.Parameters.AddWithValue("workEndTime", new TimeSpan(18, 0, 0));
            cmd.Parameters.AddWithValue("lunchStartTime", new TimeSpan(14, 0, 0));
            cmd.Parameters.AddWithValue("lunchEndTime", new TimeSpan(15, 0, 0));
            cmd.Parameters.AddWithValue("toleranceMinutes", 15);
            cmd.Parameters.AddWithValue("vacationDaysPerYear", 22);
            cmd.Parameters.AddWithValue("sickDaysPerYear", 12);
            cmd.Parameters.AddWithValue("currency", "EUR");
            cmd.Parameters.AddWithValue("timeZone", "Europe/Madrid");
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var companyId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            _logger.LogInformation("🏢 Empresa creada con ID: {CompanyId}", companyId);
            return companyId;
        }

        private async Task<int> CreateMainLocationAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""Locations"" (
                    ""CompanyId"", ""Code"", ""Name"", ""Address"", ""City"", 
                    ""State"", ""Country"", ""PostalCode"", ""Phone"", ""Email"",
                    ""Latitude"", ""Longitude"", ""RadiusMeters"", ""IsMainLocation"",
                    ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @code, @name, @address, @city,
                    @state, @country, @postalCode, @phone, @email,
                    @latitude, @longitude, @radiusMeters, @isMainLocation,
                    @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("code", "MAIN");
            cmd.Parameters.AddWithValue("name", "Oficina Central");
            cmd.Parameters.AddWithValue("address", "Calle Principal 123");
            cmd.Parameters.AddWithValue("city", "Madrid");
            cmd.Parameters.AddWithValue("state", "Madrid");
            cmd.Parameters.AddWithValue("country", "España");
            cmd.Parameters.AddWithValue("postalCode", "28001");
            cmd.Parameters.AddWithValue("phone", "+34 911 234 567");
            cmd.Parameters.AddWithValue("email", "central@empresademo.com");
            cmd.Parameters.AddWithValue("latitude", 40.4168m);
            cmd.Parameters.AddWithValue("longitude", -3.7038m);
            cmd.Parameters.AddWithValue("radiusMeters", 100);
            cmd.Parameters.AddWithValue("isMainLocation", true);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var locationId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            _logger.LogInformation("📍 Ubicación principal creada con ID: {LocationId}", locationId);
            return locationId;
        }

        private async Task<Dictionary<string, int>> CreateDepartmentsAsync(NpgsqlConnection connection, int companyId)
        {
            var departments = new Dictionary<string, (string code, string description)>
            {
                { "Administración", ("ADMIN", "Departamento de administración y recursos humanos") },
                { "Desarrollo", ("DEV", "Departamento de desarrollo de software") },
                { "Marketing", ("MKT", "Departamento de marketing y comunicación") },
                { "Ventas", ("SALES", "Departamento comercial y de ventas") },
                { "Soporte", ("SUPPORT", "Departamento de soporte técnico") },
                { "Finanzas", ("FIN", "Departamento de finanzas y contabilidad") }
            };

            var departmentIds = new Dictionary<string, int>();

            foreach (var dept in departments)
            {
                var insertQuery = @"
                    INSERT INTO ""Departments"" (
                        ""CompanyId"", ""Code"", ""Name"", ""Description"", 
                        ""CreatedAt"", ""UpdatedAt""
                    )
                    VALUES (@companyId, @code, @name, @description, @createdAt, @updatedAt)
                    RETURNING ""Id""";

                using var cmd = new NpgsqlCommand(insertQuery, connection);
                cmd.Parameters.AddWithValue("companyId", companyId);
                cmd.Parameters.AddWithValue("code", dept.Value.code);
                cmd.Parameters.AddWithValue("name", dept.Key);
                cmd.Parameters.AddWithValue("description", dept.Value.description);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                var deptId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                departmentIds[dept.Key] = deptId;
            }

            _logger.LogInformation("🏬 {Count} departamentos creados", departmentIds.Count);
            return departmentIds;
        }

        private async Task<Dictionary<string, int>> CreateWorkSchedulesAsync(NpgsqlConnection connection, int companyId)
        {
            var schedules = new Dictionary<string, int>();

            // Horario estándar
            var standardQuery = @"
                INSERT INTO ""WorkSchedules"" (
                    ""CompanyId"", ""Code"", ""Name"", ""Description"",
                    ""StartTime"", ""EndTime"", ""BreakStartTime"", ""BreakEndTime"",
                    ""Monday"", ""Tuesday"", ""Wednesday"", ""Thursday"", ""Friday"",
                    ""Saturday"", ""Sunday"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @code, @name, @description,
                    @startTime, @endTime, @breakStartTime, @breakEndTime,
                    @monday, @tuesday, @wednesday, @thursday, @friday,
                    @saturday, @sunday, @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            using (var cmd = new NpgsqlCommand(standardQuery, connection))
            {
                cmd.Parameters.AddWithValue("companyId", companyId);
                cmd.Parameters.AddWithValue("code", "STD");
                cmd.Parameters.AddWithValue("name", "Horario Estándar");
                cmd.Parameters.AddWithValue("description", "Horario de oficina estándar");
                cmd.Parameters.AddWithValue("startTime", new TimeSpan(9, 0, 0));
                cmd.Parameters.AddWithValue("endTime", new TimeSpan(18, 0, 0));
                cmd.Parameters.AddWithValue("breakStartTime", new TimeSpan(14, 0, 0));
                cmd.Parameters.AddWithValue("breakEndTime", new TimeSpan(15, 0, 0));
                cmd.Parameters.AddWithValue("monday", true);
                cmd.Parameters.AddWithValue("tuesday", true);
                cmd.Parameters.AddWithValue("wednesday", true);
                cmd.Parameters.AddWithValue("thursday", true);
                cmd.Parameters.AddWithValue("friday", true);
                cmd.Parameters.AddWithValue("saturday", false);
                cmd.Parameters.AddWithValue("sunday", false);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                schedules["standard"] = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            }

            // Horario flexible
            using (var cmd = new NpgsqlCommand(standardQuery, connection))
            {
                cmd.Parameters.AddWithValue("companyId", companyId);
                cmd.Parameters.AddWithValue("code", "FLEX");
                cmd.Parameters.AddWithValue("name", "Horario Flexible");
                cmd.Parameters.AddWithValue("description", "Horario con entrada y salida flexible");
                cmd.Parameters.AddWithValue("startTime", new TimeSpan(8, 0, 0));
                cmd.Parameters.AddWithValue("endTime", new TimeSpan(19, 0, 0));
                cmd.Parameters.AddWithValue("breakStartTime", new TimeSpan(13, 0, 0));
                cmd.Parameters.AddWithValue("breakEndTime", new TimeSpan(16, 0, 0));
                cmd.Parameters.AddWithValue("monday", true);
                cmd.Parameters.AddWithValue("tuesday", true);
                cmd.Parameters.AddWithValue("wednesday", true);
                cmd.Parameters.AddWithValue("thursday", true);
                cmd.Parameters.AddWithValue("friday", true);
                cmd.Parameters.AddWithValue("saturday", false);
                cmd.Parameters.AddWithValue("sunday", false);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                schedules["flexible"] = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            }

            _logger.LogInformation("📅 {Count} horarios de trabajo creados", schedules.Count);
            return schedules;
        }

        private async Task<int> CreateVacationPolicyAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""VacationPolicies"" (
                    ""CompanyId"", ""Code"", ""Name"", ""Description"",
                    ""AnnualDays"", ""MinDaysPerRequest"", ""MaxDaysPerRequest"",
                    ""MinAdvanceNoticeDays"", ""RequiresApproval"", ""AllowHalfDays"",
                    ""CarryOverAllowed"", ""MaxCarryOverDays"", ""ProbationMonths"",
                    ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @code, @name, @description,
                    @annualDays, @minDaysPerRequest, @maxDaysPerRequest,
                    @minAdvanceNoticeDays, @requiresApproval, @allowHalfDays,
                    @carryOverAllowed, @maxCarryOverDays, @probationMonths,
                    @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("code", "STD");
            cmd.Parameters.AddWithValue("name", "Política Estándar");
            cmd.Parameters.AddWithValue("description", "Política de vacaciones estándar de la empresa");
            cmd.Parameters.AddWithValue("annualDays", 22);
            cmd.Parameters.AddWithValue("minDaysPerRequest", 1);
            cmd.Parameters.AddWithValue("maxDaysPerRequest", 15);
            cmd.Parameters.AddWithValue("minAdvanceNoticeDays", 7);
            cmd.Parameters.AddWithValue("requiresApproval", true);
            cmd.Parameters.AddWithValue("allowHalfDays", true);
            cmd.Parameters.AddWithValue("carryOverAllowed", true);
            cmd.Parameters.AddWithValue("maxCarryOverDays", 5);
            cmd.Parameters.AddWithValue("probationMonths", 3);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            var policyId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            _logger.LogInformation("📋 Política de vacaciones creada con ID: {PolicyId}", policyId);
            return policyId;
        }

        private async Task CreateEmployeesAsync(NpgsqlConnection connection, int companyId, 
            Dictionary<string, int> departmentIds, int locationId, int workScheduleId, int vacationPolicyId)
        {
            var employees = new[]
            {
                new { 
                    FirstName = "Ana", LastName = "García", Email = "admin@empresademo.com",
                    Phone = "+34 666 111 222", Code = "EMP001", Position = "Directora General",
                    Role = 1, Department = "Administración", Password = "admin123"
                },
                new { 
                    FirstName = "Carlos", LastName = "Martínez", Email = "carlos.martinez@empresademo.com",
                    Phone = "+34 666 333 444", Code = "EMP002", Position = "Jefe de Desarrollo",
                    Role = 2, Department = "Desarrollo", Password = "supervisor123"
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
                },
                new { 
                    FirstName = "Pedro", LastName = "Sánchez", Email = "pedro.sanchez@empresademo.com",
                    Phone = "+34 666 999 000", Code = "EMP005", Position = "Analista de Marketing",
                    Role = 3, Department = "Marketing", Password = "empleado123"
                }
            };

            foreach (var emp in employees)
            {
                // Crear usuario
                var userId = await CreateUserAsync(connection, companyId, emp.FirstName, emp.LastName, 
                    emp.Email, emp.Phone, emp.Password, emp.Role);
                
                // Crear empleado
                await CreateEmployeeAsync(connection, companyId, userId, departmentIds[emp.Department], 
                    locationId, emp.Code, emp.FirstName, emp.LastName, emp.Email, emp.Phone, 
                    emp.Position, workScheduleId, vacationPolicyId);
            }
            
            _logger.LogInformation("👥 {Count} empleados creados", employees.Length);
        }

        private async Task<int> CreateUserAsync(NpgsqlConnection connection, int companyId, 
            string firstName, string lastName, string email, string phone, string password, int role)
        {
            var passwordHash = _passwordService.HashPassword(password);
            
            var insertQuery = @"
                INSERT INTO ""Users"" (
                    ""CompanyId"", ""FirstName"", ""LastName"", ""Email"", 
                    ""PasswordHash"", ""Phone"", ""Role"", ""Active"", 
                    ""EmailVerified"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @firstName, @lastName, @email, 
                    @passwordHash, @phone, @role, @active, 
                    @emailVerified, @createdAt, @updatedAt
                )
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("firstName", firstName);
            cmd.Parameters.AddWithValue("lastName", lastName);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("passwordHash", passwordHash);
            cmd.Parameters.AddWithValue("phone", phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("role", role);
            cmd.Parameters.AddWithValue("active", true);
            cmd.Parameters.AddWithValue("emailVerified", true);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        private async Task CreateEmployeeAsync(NpgsqlConnection connection, int companyId, int userId, 
            int departmentId, int locationId, string employeeCode, string firstName, string lastName, 
            string email, string phone, string position, int workScheduleId, int vacationPolicyId)
        {
            var insertQuery = @"
                INSERT INTO ""Employees"" (
                    ""CompanyId"", ""UserId"", ""DepartmentId"", ""LocationId"",
                    ""EmployeeCode"", ""FirstName"", ""LastName"", ""Email"", 
                    ""Phone"", ""Position"", ""WorkScheduleId"", ""VacationPolicyId"",
                    ""HireDate"", ""Active"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @companyId, @userId, @departmentId, @locationId,
                    @employeeCode, @firstName, @lastName, @email, 
                    @phone, @position, @workScheduleId, @vacationPolicyId,
                    @hireDate, @active, @createdAt, @updatedAt
                )";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("departmentId", departmentId);
            cmd.Parameters.AddWithValue("locationId", locationId);
            cmd.Parameters.AddWithValue("employeeCode", employeeCode);
            cmd.Parameters.AddWithValue("firstName", firstName);
            cmd.Parameters.AddWithValue("lastName", lastName);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("phone", phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("position", position);
            cmd.Parameters.AddWithValue("workScheduleId", workScheduleId);
            cmd.Parameters.AddWithValue("vacationPolicyId", vacationPolicyId);
            cmd.Parameters.AddWithValue("hireDate", DateTime.Today.AddMonths(-6));
            cmd.Parameters.AddWithValue("active", true);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task CreateVacationBalancesAsync(NpgsqlConnection connection, int vacationPolicyId)
        {
            var currentYear = DateTime.Now.Year;
            
            var insertQuery = @"
                INSERT INTO ""VacationBalances"" (
                    ""EmployeeId"", ""Year"", ""VacationPolicyId"", ""TotalDays"", 
                    ""UsedDays"", ""PendingDays"", ""CreatedAt"", ""UpdatedAt""
                )
                SELECT 
                    ""Id"", @year, @vacationPolicyId, 22, 
                    0, 0, NOW(), NOW()
                FROM ""Employees""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("year", currentYear);
            cmd.Parameters.AddWithValue("vacationPolicyId", vacationPolicyId);
            await cmd.ExecuteNonQueryAsync();
            
            _logger.LogInformation("📅 Saldos de vacaciones creados para el año {Year}", currentYear);
        }

        private async Task CreateHolidaysAsync(NpgsqlConnection connection, int companyId)
        {
            var year = DateTime.Now.Year;
            var holidays = new[]
            {
                new { Date = new DateTime(year, 1, 1), Name = "Año Nuevo" },
                new { Date = new DateTime(year, 1, 6), Name = "Día de Reyes" },
                new { Date = new DateTime(year, 5, 1), Name = "Día del Trabajo" },
                new { Date = new DateTime(year, 5, 2), Name = "Día de la Comunidad de Madrid" },
                new { Date = new DateTime(year, 8, 15), Name = "Asunción de la Virgen" },
                new { Date = new DateTime(year, 10, 12), Name = "Fiesta Nacional de España" },
                new { Date = new DateTime(year, 11, 1), Name = "Día de Todos los Santos" },
                new { Date = new DateTime(year, 12, 6), Name = "Día de la Constitución" },
                new { Date = new DateTime(year, 12, 8), Name = "Inmaculada Concepción" },
                new { Date = new DateTime(year, 12, 25), Name = "Navidad" }
            };

            foreach (var holiday in holidays)
            {
                var insertQuery = @"
                    INSERT INTO ""Holidays"" (
                        ""CompanyId"", ""Date"", ""Name"", ""Type"", 
                        ""IsRecurring"", ""CreatedAt"", ""UpdatedAt""
                    )
                    VALUES (
                        @companyId, @date, @name, @type, 
                        @isRecurring, @createdAt, @updatedAt
                    )";

                using var cmd = new NpgsqlCommand(insertQuery, connection);
                cmd.Parameters.AddWithValue("companyId", companyId);
                cmd.Parameters.AddWithValue("date", holiday.Date);
                cmd.Parameters.AddWithValue("name", holiday.Name);
                cmd.Parameters.AddWithValue("type", 0); // Nacional
                cmd.Parameters.AddWithValue("isRecurring", true);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                await cmd.ExecuteNonQueryAsync();
            }
            
            _logger.LogInformation("🎉 {Count} días festivos creados", holidays.Length);
        }

        private async Task CreateSampleTimeRecordsAsync(NpgsqlConnection connection)
        {
            // Obtener empleados
            var getEmployeesQuery = @"SELECT ""Id"", ""LocationId"" FROM ""Employees"" WHERE ""Active"" = true";
            var employees = new List<(int Id, int LocationId)>();
            
            using (var cmd = new NpgsqlCommand(getEmployeesQuery, connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    employees.Add((reader.GetInt32(0), reader.GetInt32(1)));
                }
            }

            // Crear registros para los últimos 5 días laborables
            var today = DateTime.Today;
            var startDate = today.AddDays(-7);
            
            while (startDate < today)
            {
                // Solo días laborables
                if (startDate.DayOfWeek != DayOfWeek.Saturday && startDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    foreach (var emp in employees)
                    {
                        // Entrada
                        var checkInTime = new TimeSpan(8 + Random.Shared.Next(0, 2), Random.Shared.Next(0, 60), 0);
                        await CreateTimeRecordAsync(connection, emp.Id, emp.LocationId, 0, startDate, checkInTime);
                        
                        // Salida
                        var checkOutTime = new TimeSpan(17 + Random.Shared.Next(0, 2), Random.Shared.Next(0, 60), 0);
                        await CreateTimeRecordAsync(connection, emp.Id, emp.LocationId, 1, startDate, checkOutTime);
                    }
                }
                
                startDate = startDate.AddDays(1);
            }
            
            _logger.LogInformation("⏰ Registros de tiempo de ejemplo creados");
        }

        private async Task CreateTimeRecordAsync(NpgsqlConnection connection, int employeeId, int locationId, 
            int type, DateTime date, TimeSpan time)
        {
            var insertQuery = @"
                INSERT INTO ""TimeRecords"" (
                    ""EmployeeId"", ""Type"", ""Date"", ""Time"", ""Timestamp"", 
                    ""LocationId"", ""Method"", ""Status"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @employeeId, @type, @date, @time, @timestamp, 
                    @locationId, @method, @status, @createdAt, @updatedAt
                )";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("employeeId", employeeId);
            cmd.Parameters.AddWithValue("type", type);
            cmd.Parameters.AddWithValue("date", date.Date);
            cmd.Parameters.AddWithValue("time", time);
            cmd.Parameters.AddWithValue("timestamp", date.Date + time);
            cmd.Parameters.AddWithValue("locationId", locationId);
            cmd.Parameters.AddWithValue("method", 0); // Web
            cmd.Parameters.AddWithValue("status", 1); // Aprobado
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Métodos Auxiliares

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
                await CreateTenantStructureAsync(databaseName);
                
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
            
            // Crear departamento administrativo
            var departmentId = await CreateBasicDepartmentAsync(connection, companyId);
            
            // Crear ubicación principal
            var locationId = await CreateBasicLocationAsync(connection, companyId);
            
            // Crear horario estándar
            var workScheduleId = await CreateBasicWorkScheduleAsync(connection, companyId);
            
            // Crear política de vacaciones
            var vacationPolicyId = await CreateBasicVacationPolicyAsync(connection, companyId);
            
            // Crear usuario administrador
            await CreateBasicAdminAsync(connection, companyId, departmentId, locationId, 
                workScheduleId, vacationPolicyId, adminEmail, adminPassword);
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
            cmd.Parameters.AddWithValue("description", "Departamento administrativo");
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        private async Task<int> CreateBasicLocationAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""Locations"" (
                    ""CompanyId"", ""Name"", ""IsMainLocation"", 
                    ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (@companyId, @name, @isMainLocation, @createdAt, @updatedAt)
                RETURNING ""Id""";

            using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("companyId", companyId);
            cmd.Parameters.AddWithValue("name", "Oficina Principal");
            cmd.Parameters.AddWithValue("isMainLocation", true);
            cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        private async Task<int> CreateBasicWorkScheduleAsync(NpgsqlConnection connection, int companyId)
        {
            var insertQuery = @"
                INSERT INTO ""WorkSchedules"" (
                    ""CompanyId"", ""Name"", ""StartTime"", ""EndTime"", 
                    ""CreatedAt"", ""UpdatedAt""
                )
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
                INSERT INTO ""VacationPolicies"" (
                    ""CompanyId"", ""Name"", ""AnnualDays"", 
                    ""CreatedAt"", ""UpdatedAt""
                )
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

        private async Task CreateBasicAdminAsync(NpgsqlConnection connection, int companyId, int departmentId, 
            int locationId, int workScheduleId, int vacationPolicyId, string email, string password)
        {
            // Extraer nombre del email
            var emailParts = email.Split('@')[0].Split('.');
            var firstName = emailParts.Length > 0 ? 
                char.ToUpper(emailParts[0][0]) + emailParts[0].Substring(1) : "Admin";
            var lastName = emailParts.Length > 1 ? 
                char.ToUpper(emailParts[1][0]) + emailParts[1].Substring(1) : "Usuario";

            // Crear usuario
            var userId = await CreateUserAsync(connection, companyId, firstName, lastName, email, null, password, 1);
            
            // Crear empleado
            await CreateEmployeeAsync(connection, companyId, userId, departmentId, locationId, 
                "ADMIN001", firstName, lastName, email, null, "Administrador", 
                workScheduleId, vacationPolicyId);
            
            // Crear balance de vacaciones
            var currentYear = DateTime.Now.Year;
            var insertBalanceQuery = @"
                INSERT INTO ""VacationBalances"" (
                    ""EmployeeId"", ""Year"", ""VacationPolicyId"", ""TotalDays"", 
                    ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    (SELECT ""Id"" FROM ""Employees"" WHERE ""Email"" = @email), 
                    @year, @vacationPolicyId, 22, @createdAt, @updatedAt
                )";

            using var balanceCmd = new NpgsqlCommand(insertBalanceQuery, connection);
            balanceCmd.Parameters.AddWithValue("email", email);
            balanceCmd.Parameters.AddWithValue("year", currentYear);
            balanceCmd.Parameters.AddWithValue("vacationPolicyId", vacationPolicyId);
            balanceCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            balanceCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
            
            await balanceCmd.ExecuteNonQueryAsync();
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
    }
}
                {
                    _logger.LogError(ex, "Error ejecutando comando SQL");
                    throw;
                }
            }
            
            _logger.LogInformation("✅ Estructura central creada exitosamente");
        }

        private async Task SeedCentralDataAsync(NpgsqlConnection connection)
        {
            _logger.LogInformation("🌱 Sembrando datos iniciales en BD central");

            // Crear configuraciones del sistema
            await CreateSystemConfigurationsAsync(connection);
            
            // Crear super administrador
            await CreateSuperAdminAsync(connection);
            
            // Crear plantillas de email
            await CreateEmailTemplatesAsync(connection);
            
            _logger.LogInformation("✅ Datos centrales sembrados exitosamente");
        }

        private async Task CreateSystemConfigurationsAsync(NpgsqlConnection connection)
        {
            var configs = new Dictionary<string, (string category, string value, string description)>
            {
                // Sistema
                { "SystemName", ("System", "Sphere Time Control", "Nombre del sistema") },
                { "SystemVersion", ("System", "1.0.0", "Versión actual del sistema") },
                { "MaintenanceMode", ("System", "false", "Modo de mantenimiento activo") },
                { "DefaultLanguage", ("System", "es", "Idioma por defecto") },
                { "TimeZone", ("System", "Europe/Madrid", "Zona horaria por defecto") },
                
                // Licencias
                { "DefaultLicenseType", ("License", "Trial", "Tipo de licencia por defecto") },
                { "TrialDurationDays", ("License", "30", "Duración del período de prueba en días") },
                { "MaxEmployeesTrial", ("License", "5", "Máximo de empleados en versión Trial") },
                { "MaxEmployeesBasic", ("License", "10", "Máximo de empleados en versión Basic") },
                { "MaxEmployeesProfessional", ("License", "50", "Máximo de empleados en versión Professional") },
                { "MaxEmployeesEnterprise", ("License", "999", "Máximo de empleados en versión Enterprise") },
                
                // Precios
                { "DefaultCurrency", ("Billing", "EUR", "Moneda por defecto") },
                { "TrialMonthlyPrice", ("Billing", "0.00", "Precio mensual versión Trial") },
                { "BasicMonthlyPrice", ("Billing", "29.99", "Precio mensual versión Basic") },
                { "ProfessionalMonthlyPrice", ("Billing", "79.99", "Precio mensual versión Professional") },
                { "EnterpriseMonthlyPrice", ("Billing", "199.99", "Precio mensual versión Enterprise") },
                { "TaxRate", ("Billing", "21", "Porcentaje de impuestos") },
                
                // Email
                { "SmtpHost", ("Email", "smtp.gmail.com", "Servidor SMTP") },
                { "SmtpPort", ("Email", "587", "Puerto SMTP") },
                { "SmtpFromEmail", ("Email", "noreply@spheretimecontrol.com", "Email remitente") },
                { "SmtpFromName", ("Email", "Sphere Time Control", "Nombre remitente") },
                
                // Seguridad
                { "PasswordMinLength", ("Security", "8", "Longitud mínima de contraseña") },
                { "PasswordRequireUppercase", ("Security", "true", "Requerir mayúsculas en contraseña") },
                { "PasswordRequireLowercase", ("Security", "true", "Requerir minúsculas en contraseña") },
                { "PasswordRequireNumbers", ("Security", "true", "Requerir números en contraseña") },
                { "PasswordRequireSpecialChars", ("Security", "false", "Requerir caracteres especiales") },
                { "MaxLoginAttempts", ("Security", "5", "Máximo de intentos de login") },
                { "LockoutDurationMinutes", ("Security", "30", "Duración del bloqueo en minutos") },
                { "SessionTimeoutMinutes", ("Security", "60", "Timeout de sesión en minutos") },
                { "JwtSecretKey", ("Security", "YOUR_SECRET_KEY_HERE_MIN_32_CHARS_LONG!", "Clave secreta para JWT") },
                { "JwtIssuer", ("Security", "SphereTimeControl", "Emisor de JWT") },
                { "JwtAudience", ("Security", "SphereTimeControlUsers", "Audiencia de JWT") }
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

        private async Task CreateEmailTemplatesAsync(NpgsqlConnection connection)
        {
            var templates = new[]
            {
                new
                {
                    Code = "WELCOME_TENANT",
                    Name = "Bienvenida Nuevo Tenant",
                    Subject = "¡Bienvenido a Sphere Time Control!",
                    Body = @"<h1>¡Bienvenido a Sphere Time Control!</h1>
                            <p>Hola {{CompanyName}},</p>
                            <p>Tu cuenta ha sido creada exitosamente.</p>
                            <p>Puedes acceder a tu panel en: <a href='{{LoginUrl}}'>{{LoginUrl}}</a></p>
                            <p>Credenciales:</p>
                            <ul>
                                <li>Email: {{AdminEmail}}</li>
                                <li>Contraseña temporal: {{TempPassword}}</li>
                            </ul>
                            <p>Por favor, cambia tu contraseña en el primer inicio de sesión.</p>
                            <p>Saludos,<br>El equipo de Sphere Time Control</p>"
                },
                new
                {
                    Code = "PASSWORD_RESET",
                    Name = "Recuperación de Contraseña",
                    Subject = "Recupera tu contraseña - Sphere Time Control",
                    Body = @"<h1>Recuperación de Contraseña</h1>
                            <p>Hola {{UserName}},</p>
                            <p>Hemos recibido una solicitud para restablecer tu contraseña.</p>
                            <p>Haz clic en el siguiente enlace para crear una nueva contraseña:</p>
                            <p><a href='{{ResetLink}}'>Restablecer Contraseña</a></p>
                            <p>Este enlace expirará en 24 horas.</p>
                            <p>Si no solicitaste este cambio, puedes ignorar este email.</p>
                            <p>Saludos,<br>El equipo de Sphere Time Control</p>"
                },
                new
                {
                    Code = "EMPLOYEE_WELCOME",
                    Name = "Bienvenida Empleado",
                    Subject = "Bienvenido a {{CompanyName}} - Sphere Time Control",
                    Body = @"<h1>¡Bienvenido!</h1>
                            <p>Hola {{EmployeeName}},</p>
                            <p>Tu cuenta de empleado ha sido creada en Sphere Time Control.</p>
                            <p>Puedes acceder a la aplicación en: <a href='{{AppUrl}}'>{{AppUrl}}</a></p>
                            <p>Tus credenciales son:</p>
                            <ul>
                                <li>Email: {{Email}}</li>
                                <li>Contraseña: {{TempPassword}}</li>
                            </ul>
                            <p>Por favor, cambia tu contraseña en el primer inicio de sesión.</p>
                            <p>Saludos,<br>{{CompanyName}}</p>"
                }
            };

            foreach (var template in templates)
            {
                var checkQuery = @"SELECT COUNT(*) FROM ""EmailTemplates"" WHERE ""Code"" = @code";
                using var checkCmd = new NpgsqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("code", template.Code);
                
                var exists = (long)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
                
                if (!exists)
                {
                    var insertQuery = @"
                        INSERT INTO ""EmailTemplates"" (""Code"", ""Name"", ""Subject"", ""Body"", ""Language"", ""Active"", ""CreatedAt"", ""UpdatedAt"")
                        VALUES (@code, @name, @subject, @body, @language, @active, @createdAt, @updatedAt)";

                    using var insertCmd = new NpgsqlCommand(insertQuery, connection);
                    insertCmd.Parameters.AddWithValue("code", template.Code);
                    insertCmd.Parameters.AddWithValue("name", template.Name);
                    insertCmd.Parameters.AddWithValue("subject", template.Subject);
                    insertCmd.Parameters.AddWithValue("body", template.Body);
                    insertCmd.Parameters.AddWithValue("language", "es");
                    insertCmd.Parameters.AddWithValue("active", true);
                    insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                    insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            
            _logger.LogInformation("📧 Plantillas de email creadas");
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
                    )
                    RETURNING ""Id""";

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

                var tenantId = (int)(await insertCmd.ExecuteScalarAsync() ?? 0);
                
                // Crear licencia trial
                await CreateTenantLicenseAsync(connection, tenantId);
                
                _logger.LogInformation("📝 Tenant registrado en BD central: {TenantCode}", tenantCode);
            }
        }

        private async Task CreateTenantLicenseAsync(NpgsqlConnection connection, int tenantId)
        {
            var insertQuery = @"
                INSERT INTO ""Licenses"" (
                    ""TenantId"", ""Type"", ""Name"", ""MaxEmployees"", 
                    ""MonthlyPrice"", ""StartDate"", ""EndDate"", 
                    ""Active"", ""IsTrial"", ""CreatedAt"", ""UpdatedAt""
                )
                VALUES (
                    @tenantId, @type, @name, @maxEmployees, 
                    @monthlyPrice, @startDate, @endDate, 
                    @active, @isTrial, @createdAt, @updatedAt
                )";

            using var insertCmd = new NpgsqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("tenantId", tenantId);
            insertCmd.Parameters.AddWithValue("type", 0); // Trial
            insertCmd.Parameters.AddWithValue("name", "Licencia Trial");
            insertCmd.Parameters.AddWithValue("maxEmployees", 5);
            insertCmd.Parameters.AddWithValue("monthlyPrice", 0.00m);
            insertCmd.Parameters.AddWithValue("startDate", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("endDate", DateTime.UtcNow.AddDays(30));
            insertCmd.Parameters.AddWithValue("active", true);
            insertCmd.Parameters.AddWithValue("isTrial", true);
            insertCmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();
        }

        private async Task CreateTenantStructureAsync(string databaseName)
        {
            _logger.LogInformation("🏗️ Creando estructura del tenant: {DatabaseName}", databaseName);
            
            var tenantConnectionString = _connectionString.Replace("Database=postgres", $"Database={databaseName}");
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            // Crear tablas principales
            await CreateTenantTablesAsync(connection);
            
            // Crear índices
            await CreateTenantIndexesAsync(connection);
            
            // Crear vistas
            await CreateTenantViewsAsync(connection);
            
            // Crear triggers
            await CreateTenantTriggersAsync(connection);
            
            _logger.LogInformation("✅ Estructura del tenant creada exitosamente");
        }

        private async Task CreateTenantTablesAsync(NpgsqlConnection connection)
        {
            var commands = new List<string>
            {
                // Extensiones
                @"CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";",
                @"CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";",
                
                // Tabla: Companies (Datos de la empresa)
                @"CREATE TABLE IF NOT EXISTS ""Companies"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Code"" VARCHAR(50),
                    ""TaxId"" VARCHAR(50),
                    ""Email"" VARCHAR(255) NOT NULL,
                    ""Phone"" VARCHAR(20),
                    ""Website"" VARCHAR(255),
                    ""Address"" VARCHAR(500),
                    ""City"" VARCHAR(100),
                    ""State"" VARCHAR(100),
                    ""Country"" VARCHAR(100),
                    ""PostalCode"" VARCHAR(20),
                    ""LogoUrl"" VARCHAR(500),
                    ""WorkStartTime"" TIME DEFAULT '09:00:00',
                    ""WorkEndTime"" TIME DEFAULT '18:00:00',
                    ""LunchStartTime"" TIME DEFAULT '14:00:00',
                    ""LunchEndTime"" TIME DEFAULT '15:00:00',
                    ""ToleranceMinutes"" INTEGER DEFAULT 15,
                    ""OvertimeMultiplier"" DECIMAL(3,2) DEFAULT 1.5,
                    ""WeekendMultiplier"" DECIMAL(3,2) DEFAULT 2.0,
                    ""VacationDaysPerYear"" INTEGER DEFAULT 22,
                    ""SickDaysPerYear"" INTEGER DEFAULT 12,
                    ""PersonalDaysPerYear"" INTEGER DEFAULT 3,
                    ""Currency"" VARCHAR(3) DEFAULT 'EUR',
                    ""TimeZone"" VARCHAR(50) DEFAULT 'Europe/Madrid',
                    ""DateFormat"" VARCHAR(20) DEFAULT 'DD/MM/YYYY',
                    ""TimeFormat"" VARCHAR(20) DEFAULT 'HH:mm',
                    ""WeekStartsOn"" INTEGER DEFAULT 1,
                    ""FiscalYearStartMonth"" INTEGER DEFAULT 1,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""Settings"" JSONB,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
                );",
                
                // Tabla: Departments (Departamentos)
                @"CREATE TABLE IF NOT EXISTS ""Departments"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""ParentDepartmentId"" INTEGER,
                    ""Code"" VARCHAR(50),
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Description"" VARCHAR(500),
                    ""ManagerId"" INTEGER,
                    ""CostCenter"" VARCHAR(50),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Departments_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Departments_ParentDepartment"" FOREIGN KEY (""ParentDepartmentId"") 
                        REFERENCES ""Departments"" (""Id"") ON DELETE SET NULL
                );",
                
                // Tabla: Locations (Ubicaciones/Sedes)
                @"CREATE TABLE IF NOT EXISTS ""Locations"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""Code"" VARCHAR(50),
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Address"" VARCHAR(500),
                    ""City"" VARCHAR(100),
                    ""State"" VARCHAR(100),
                    ""Country"" VARCHAR(100),
                    ""PostalCode"" VARCHAR(20),
                    ""Phone"" VARCHAR(20),
                    ""Email"" VARCHAR(255),
                    ""Latitude"" DECIMAL(10,8),
                    ""Longitude"" DECIMAL(11,8),
                    ""RadiusMeters"" INTEGER DEFAULT 100,
                    ""TimeZone"" VARCHAR(50),
                    ""IsMainLocation"" BOOLEAN DEFAULT false,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Locations_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
                );",
                
                // Tabla: Users (Usuarios del sistema)
                @"CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""FirstName"" VARCHAR(100) NOT NULL,
                    ""LastName"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL UNIQUE,
                    ""PasswordHash"" TEXT NOT NULL,
                    ""Phone"" VARCHAR(20),
                    ""AvatarUrl"" VARCHAR(500),
                    ""Role"" INTEGER NOT NULL DEFAULT 3,
                    ""Language"" VARCHAR(5) DEFAULT 'es',
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""EmailVerified"" BOOLEAN NOT NULL DEFAULT false,
                    ""EmailVerificationToken"" VARCHAR(255),
                    ""PasswordResetToken"" VARCHAR(255),
                    ""PasswordResetExpires"" TIMESTAMP,
                    ""LastLoginAt"" TIMESTAMP,
                    ""LastLoginIp"" VARCHAR(50),
                    ""FailedLoginAttempts"" INTEGER DEFAULT 0,
                    ""LockedUntil"" TIMESTAMP,
                    ""TwoFactorEnabled"" BOOLEAN DEFAULT false,
                    ""TwoFactorSecret"" VARCHAR(255),
                    ""Preferences"" JSONB,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Users_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
                );",
                
                // Tabla: Employees (Empleados)
                @"CREATE TABLE IF NOT EXISTS ""Employees"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""UserId"" INTEGER NOT NULL,
                    ""DepartmentId"" INTEGER,
                    ""LocationId"" INTEGER,
                    ""EmployeeCode"" VARCHAR(50) NOT NULL,
                    ""EmployeeNumber"" VARCHAR(50),
                    ""FirstName"" VARCHAR(100) NOT NULL,
                    ""LastName"" VARCHAR(100) NOT NULL,
                    ""Email"" VARCHAR(255) NOT NULL,
                    ""Phone"" VARCHAR(20),
                    ""Mobile"" VARCHAR(20),
                    ""Position"" VARCHAR(100),
                    ""JobTitle"" VARCHAR(100),
                    ""ManagerId"" INTEGER,
                    ""EmploymentType"" INTEGER DEFAULT 0,
                    ""ContractType"" INTEGER DEFAULT 0,
                    ""HireDate"" DATE,
                    ""TerminationDate"" DATE,
                    ""BirthDate"" DATE,
                    ""Gender"" VARCHAR(10),
                    ""MaritalStatus"" VARCHAR(20),
                    ""Nationality"" VARCHAR(50),
                    ""IdentificationNumber"" VARCHAR(50),
                    ""SocialSecurityNumber"" VARCHAR(50),
                    ""Address"" VARCHAR(500),
                    ""City"" VARCHAR(100),
                    ""State"" VARCHAR(100),
                    ""Country"" VARCHAR(100),
                    ""PostalCode"" VARCHAR(20),
                    ""EmergencyContact"" VARCHAR(100),
                    ""EmergencyPhone"" VARCHAR(20),
                    ""BankAccount"" VARCHAR(100),
                    ""Salary"" DECIMAL(10,2),
                    ""HourlyRate"" DECIMAL(10,2),
                    ""WorkScheduleId"" INTEGER,
                    ""VacationPolicyId"" INTEGER,
                    ""PinCode"" VARCHAR(10),
                    ""CardNumber"" VARCHAR(50),
                    ""FaceId"" TEXT,
                    ""FingerprintId"" TEXT,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""Notes"" TEXT,
                    ""CustomFields"" JSONB,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Employees_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Employees_Users"" FOREIGN KEY (""UserId"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Employees_Departments"" FOREIGN KEY (""DepartmentId"") 
                        REFERENCES ""Departments"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_Employees_Locations"" FOREIGN KEY (""LocationId"") 
                        REFERENCES ""Locations"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_Employees_Manager"" FOREIGN KEY (""ManagerId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE SET NULL
                );",
                
                // Tabla: WorkSchedules (Horarios de trabajo)
                @"CREATE TABLE IF NOT EXISTS ""WorkSchedules"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""Code"" VARCHAR(50),
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Description"" VARCHAR(500),
                    ""Type"" INTEGER DEFAULT 0,
                    ""StartTime"" TIME NOT NULL,
                    ""EndTime"" TIME NOT NULL,
                    ""BreakStartTime"" TIME,
                    ""BreakEndTime"" TIME,
                    ""Monday"" BOOLEAN DEFAULT true,
                    ""Tuesday"" BOOLEAN DEFAULT true,
                    ""Wednesday"" BOOLEAN DEFAULT true,
                    ""Thursday"" BOOLEAN DEFAULT true,
                    ""Friday"" BOOLEAN DEFAULT true,
                    ""Saturday"" BOOLEAN DEFAULT false,
                    ""Sunday"" BOOLEAN DEFAULT false,
                    ""FlexibleHours"" BOOLEAN DEFAULT false,
                    ""CoreHoursStart"" TIME,
                    ""CoreHoursEnd"" TIME,
                    ""MinHoursPerDay"" DECIMAL(4,2),
                    ""MaxHoursPerDay"" DECIMAL(4,2),
                    ""MinHoursPerWeek"" DECIMAL(4,2),
                    ""MaxHoursPerWeek"" DECIMAL(4,2),
                    ""OvertimeAllowed"" BOOLEAN DEFAULT true,
                    ""MaxOvertimePerDay"" DECIMAL(4,2),
                    ""MaxOvertimePerWeek"" DECIMAL(4,2),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_WorkSchedules_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
                );",
                
                // Tabla: TimeRecords (Registros de tiempo)
                @"CREATE TABLE IF NOT EXISTS ""TimeRecords"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""Type"" INTEGER NOT NULL,
                    ""Date"" DATE NOT NULL,
                    ""Time"" TIME NOT NULL,
                    ""Timestamp"" TIMESTAMP NOT NULL,
                    ""LocationId"" INTEGER,
                    ""Location"" VARCHAR(200),
                    ""Latitude"" DECIMAL(10,8),
                    ""Longitude"" DECIMAL(11,8),
                    ""Accuracy"" DECIMAL(6,2),
                    ""Method"" INTEGER DEFAULT 0,
                    ""DeviceInfo"" VARCHAR(500),
                    ""IpAddress"" VARCHAR(50),
                    ""PhotoUrl"" VARCHAR(500),
                    ""Notes"" VARCHAR(500),
                    ""Status"" INTEGER DEFAULT 0,
                    ""IsManualEntry"" BOOLEAN DEFAULT false,
                    ""IsOffline"" BOOLEAN DEFAULT false,
                    ""SyncedAt"" TIMESTAMP,
                    ""ApprovedById"" INTEGER,
                    ""ApprovedAt"" TIMESTAMP,
                    ""ApprovalNotes"" VARCHAR(500),
                    ""CreatedById"" INTEGER,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_TimeRecords_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_TimeRecords_Locations"" FOREIGN KEY (""LocationId"") 
                        REFERENCES ""Locations"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_TimeRecords_ApprovedBy"" FOREIGN KEY (""ApprovedById"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_TimeRecords_CreatedBy"" FOREIGN KEY (""CreatedById"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE SET NULL
                );",
                
                // Tabla: Breaks (Descansos)
                @"CREATE TABLE IF NOT EXISTS ""Breaks"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""TimeRecordId"" INTEGER,
                    ""Date"" DATE NOT NULL,
                    ""StartTime"" TIMESTAMP NOT NULL,
                    ""EndTime"" TIMESTAMP,
                    ""Duration"" INTEGER,
                    ""Type"" INTEGER DEFAULT 0,
                    ""IsPaid"" BOOLEAN DEFAULT true,
                    ""Notes"" VARCHAR(500),
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Breaks_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Breaks_TimeRecords"" FOREIGN KEY (""TimeRecordId"") 
                        REFERENCES ""TimeRecords"" (""Id"") ON DELETE SET NULL
                );",
                
                // Tabla: Overtime (Horas extra)
                @"CREATE TABLE IF NOT EXISTS ""Overtime"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""Date"" DATE NOT NULL,
                    ""Hours"" DECIMAL(4,2) NOT NULL,
                    ""Type"" INTEGER DEFAULT 0,
                    ""Multiplier"" DECIMAL(3,2) DEFAULT 1.5,
                    ""Reason"" VARCHAR(500),
                    ""Status"" INTEGER DEFAULT 0,
                    ""RequestedById"" INTEGER,
                    ""ApprovedById"" INTEGER,
                    ""ApprovedAt"" TIMESTAMP,
                    ""ApprovalNotes"" VARCHAR(500),
                    ""CompensationType"" INTEGER DEFAULT 0,
                    ""CompensatedAt"" TIMESTAMP,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Overtime_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Overtime_RequestedBy"" FOREIGN KEY (""RequestedById"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_Overtime_ApprovedBy"" FOREIGN KEY (""ApprovedById"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE SET NULL
                );",
                
                // Tabla: VacationPolicies (Políticas de vacaciones)
                @"CREATE TABLE IF NOT EXISTS ""VacationPolicies"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""Code"" VARCHAR(50),
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Description"" VARCHAR(500),
                    ""AnnualDays"" INTEGER NOT NULL,
                    ""MinDaysPerRequest"" INTEGER DEFAULT 1,
                    ""MaxDaysPerRequest"" INTEGER DEFAULT 30,
                    ""MinAdvanceNoticeDays"" INTEGER DEFAULT 7,
                    ""RequiresApproval"" BOOLEAN DEFAULT true,
                    ""AllowHalfDays"" BOOLEAN DEFAULT true,
                    ""CarryOverAllowed"" BOOLEAN DEFAULT true,
                    ""MaxCarryOverDays"" INTEGER DEFAULT 5,
                    ""CarryOverExpireMonths"" INTEGER DEFAULT 3,
                    ""AccrualType"" INTEGER DEFAULT 0,
                    ""AccrualFrequency"" INTEGER DEFAULT 12,
                    ""ProbationMonths"" INTEGER DEFAULT 3,
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""Rules"" JSONB,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_VacationPolicies_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE
                );",
                
                // Tabla: VacationRequests (Solicitudes de vacaciones)
                @"CREATE TABLE IF NOT EXISTS ""VacationRequests"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""VacationPolicyId"" INTEGER,
                    ""Type"" INTEGER DEFAULT 0,
                    ""StartDate"" DATE NOT NULL,
                    ""EndDate"" DATE NOT NULL,
                    ""StartPeriod"" INTEGER DEFAULT 0,
                    ""EndPeriod"" INTEGER DEFAULT 0,
                    ""TotalDays"" DECIMAL(4,1) NOT NULL,
                    ""WorkingDays"" DECIMAL(4,1),
                    ""Status"" INTEGER NOT NULL DEFAULT 0,
                    ""Reason"" VARCHAR(500),
                    ""Comments"" TEXT,
                    ""AttachmentUrl"" VARCHAR(500),
                    ""ReviewedById"" INTEGER,
                    ""ReviewedAt"" TIMESTAMP,
                    ""ReviewNotes"" TEXT,
                    ""CancelledById"" INTEGER,
                    ""CancelledAt"" TIMESTAMP,
                    ""CancelReason"" VARCHAR(500),
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_VacationRequests_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_VacationRequests_VacationPolicies"" FOREIGN KEY (""VacationPolicyId"") 
                        REFERENCES ""VacationPolicies"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_VacationRequests_ReviewedBy"" FOREIGN KEY (""ReviewedById"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_VacationRequests_CancelledBy"" FOREIGN KEY (""CancelledById"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE SET NULL
                );",
                
                // Tabla: VacationBalances (Saldos de vacaciones)
                @"CREATE TABLE IF NOT EXISTS ""VacationBalances"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""EmployeeId"" INTEGER NOT NULL,
                    ""Year"" INTEGER NOT NULL,
                    ""VacationPolicyId"" INTEGER,
                    ""TotalDays"" DECIMAL(5,2) NOT NULL,
                    ""UsedDays"" DECIMAL(5,2) DEFAULT 0,
                    ""PendingDays"" DECIMAL(5,2) DEFAULT 0,
                    ""ApprovedDays"" DECIMAL(5,2) DEFAULT 0,
                    ""CarriedOverDays"" DECIMAL(5,2) DEFAULT 0,
                    ""ExpiredDays"" DECIMAL(5,2) DEFAULT 0,
                    ""AdjustmentDays"" DECIMAL(5,2) DEFAULT 0,
                    ""AdjustmentReason"" VARCHAR(500),
                    ""LastCalculatedAt"" TIMESTAMP,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_VacationBalances_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_VacationBalances_VacationPolicies"" FOREIGN KEY (""VacationPolicyId"") 
                        REFERENCES ""VacationPolicies"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""UQ_VacationBalances_EmployeeYear"" UNIQUE (""EmployeeId"", ""Year"")
                );",
                
                // Tabla: Holidays (Días festivos)
                @"CREATE TABLE IF NOT EXISTS ""Holidays"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""CompanyId"" INTEGER NOT NULL,
                    ""LocationId"" INTEGER,
                    ""Date"" DATE NOT NULL,
                    ""Name"" VARCHAR(100) NOT NULL,
                    ""Description"" VARCHAR(500),
                    ""Type"" INTEGER DEFAULT 0,
                    ""IsRecurring"" BOOLEAN DEFAULT false,
                    ""RecurrencePattern"" VARCHAR(50),
                    ""Active"" BOOLEAN NOT NULL DEFAULT true,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    ""UpdatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_Holidays_Companies"" FOREIGN KEY (""CompanyId"") 
                        REFERENCES ""Companies"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_Holidays_Locations"" FOREIGN KEY (""LocationId"") 
                        REFERENCES ""Locations"" (""Id"") ON DELETE SET NULL
                );",
                
                // Tabla: AuditLogs (Logs de auditoría del tenant)
                @"CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""UserId"" INTEGER,
                    ""EmployeeId"" INTEGER,
                    ""UserEmail"" VARCHAR(255),
                    ""Action"" VARCHAR(100) NOT NULL,
                    ""EntityType"" VARCHAR(100),
                    ""EntityId"" INTEGER,
                    ""OldValues"" JSONB,
                    ""NewValues"" JSONB,
                    ""IpAddress"" VARCHAR(50),
                    ""UserAgent"" TEXT,
                    ""Details"" TEXT,
                    ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                    
                    CONSTRAINT ""FK_AuditLogs_Users"" FOREIGN KEY (""UserId"") 
                        REFERENCES ""Users"" (""Id"") ON DELETE SET NULL,
                    CONSTRAINT ""FK_AuditLogs_Employees"" FOREIGN KEY (""EmployeeId"") 
                        REFERENCES ""Employees"" (""Id"") ON DELETE SET NULL
                );"
            };

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
                    _logger.LogError(ex, "Error ejecutando comando SQL en tenant");
                    throw;
                }
            }
        }
        #endregion