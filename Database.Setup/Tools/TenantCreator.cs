using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

namespace Database.Setup.Tools
{
    /// <summary>
    /// Utilidad para crear nuevos tenants en el sistema
    /// </summary>
    public class TenantCreator
    {
        private readonly string _connectionString;
        private readonly string _scriptsPath;

        public TenantCreator(string connectionString, string scriptsPath = "Scripts")
        {
            _connectionString = connectionString;
            _scriptsPath = scriptsPath;
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
            string companyName = null, 
            string adminEmail = null, 
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
                Console.WriteLine("=====================================");
                Console.WriteLine("🔑 Credenciales de acceso:");
                Console.WriteLine($"   URL: https://{tenantId}.tudominio.com");
                Console.WriteLine($"   Email: {adminEmail}");
                Console.WriteLine($"   Contraseña: {adminPassword}");
                Console.WriteLine("=====================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear tenant {tenantId}: {ex.Message}");
                
                // Intentar limpiar en caso de error
                await CleanupFailedTenantAsync(tenantId);
                throw;
            }
        }

        /// <summary>
        /// Elimina un tenant específico
        /// </summary>
        public async Task DeleteTenantAsync(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
                throw new ArgumentException("El ID del tenant es requerido", nameof(tenantId));

            tenantId = SanitizeTenantId(tenantId);

            Console.WriteLine($"🗑️  Eliminando tenant: {tenantId}");

            try
            {
                // 1. Eliminar BD del tenant
                var tenantDbName = $"SphereTimeControl_{tenantId}";
                await DropTenantDatabaseAsync(tenantDbName);

                // 2. Eliminar registro de la BD central
                await DeleteTenantRecordAsync(tenantId);

                Console.WriteLine($"✅ Tenant {tenantId} eliminado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al eliminar tenant {tenantId}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lista todos los tenants existentes
        /// </summary>
        public async Task ListTenantsAsync()
        {
            Console.WriteLine("🏢 Listado de Tenants:");
            Console.WriteLine("=====================");

            try
            {
                var centralConnectionString = GetCentralConnectionString();

                using var connection = new NpgsqlConnection(centralConnectionString);
                await connection.OpenAsync();

                var command = new NpgsqlCommand(@"
                    SELECT Id, CompanyName, AdminEmail, CreatedAt, IsActive
                    FROM Tenants 
                    ORDER BY CreatedAt DESC", connection);

                using var reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    Console.WriteLine("❌ No hay tenants configurados");
                    return;
                }

                while (await reader.ReadAsync())
                {
                    var id = reader.GetString("Id");
                    var companyName = reader.GetString("CompanyName");
                    var adminEmail = reader.GetString("AdminEmail");
                    var createdAt = reader.GetDateTime("CreatedAt");
                    var isActive = reader.GetBoolean("IsActive");

                    var status = isActive ? "🟢 Activo" : "🔴 Inactivo";
                    
                    Console.WriteLine($"ID: {id}");
                    Console.WriteLine($"   Empresa: {companyName}");
                    Console.WriteLine($"   Admin: {adminEmail}");
                    Console.WriteLine($"   Estado: {status}");
                    Console.WriteLine($"   Creado: {createdAt:yyyy-MM-dd HH:mm}");
                    Console.WriteLine($"   URL: https://{id}.tudominio.com");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al listar tenants: {ex.Message}");
            }
        }

        /// <summary>
        /// Sanitiza el ID del tenant
        /// </summary>
        private string SanitizeTenantId(string tenantId)
        {
            return tenantId.ToLower()
                          .Replace(" ", "")
                          .Replace("-", "")
                          .Replace("_", "");
        }

        /// <summary>
        /// Valida que el tenant no exista ya
        /// </summary>
        private async Task ValidateTenantNotExistsAsync(string tenantId)
        {
            var centralConnectionString = GetCentralConnectionString();

            using var connection = new NpgsqlConnection(centralConnectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                "SELECT COUNT(*) FROM Tenants WHERE Id = @tenantId", 
                connection);
            command.Parameters.AddWithValue("tenantId", tenantId);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());

            if (count > 0)
            {
                throw new InvalidOperationException($"El tenant '{tenantId}' ya existe");
            }
        }

        /// <summary>
        /// Crea el registro del tenant en la BD central
        /// </summary>
        private async Task CreateTenantRecordAsync(string tenantId, string companyName, string adminEmail)
        {
            Console.WriteLine("📝 Registrando tenant en la BD central...");

            var centralConnectionString = GetCentralConnectionString();

            using var connection = new NpgsqlConnection(centralConnectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(@"
                INSERT INTO Tenants (Id, CompanyName, AdminEmail, DatabaseName, IsActive, CreatedAt, LicenseType)
                VALUES (@tenantId, @companyName, @adminEmail, @databaseName, @isActive, @createdAt, @licenseType)", 
                connection);

            command.Parameters.AddWithValue("tenantId", tenantId);
            command.Parameters.AddWithValue("companyName", companyName);
            command.Parameters.AddWithValue("adminEmail", adminEmail);
            command.Parameters.AddWithValue("databaseName", $"SphereTimeControl_{tenantId}");
            command.Parameters.AddWithValue("isActive", true);
            command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("licenseType", "Trial"); // Licencia de prueba por defecto

            await command.ExecuteNonQueryAsync();

            Console.WriteLine("✅ Tenant registrado en BD central");
        }

        /// <summary>
        /// Crea la base de datos física del tenant
        /// </summary>
        private async Task CreateTenantDatabaseAsync(string databaseName)
        {
            Console.WriteLine($"📦 Creando base de datos: {databaseName}");

            var masterConnectionString = GetMasterConnectionString();

            using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();

            // Verificar si ya existe
            var checkCommand = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @dbname", 
                connection);
            checkCommand.Parameters.AddWithValue("dbname", databaseName);

            var exists = await checkCommand.ExecuteScalarAsync();

            if (exists != null)
            {
                throw new InvalidOperationException($"La base de datos {databaseName} ya existe");
            }

            // Crear la base de datos
            var createCommand = new NpgsqlCommand($@"
                CREATE DATABASE ""{databaseName}""
                WITH OWNER = postgres
                ENCODING = 'UTF8'
                LC_COLLATE = 'en_US.utf8'
                LC_CTYPE = 'en_US.utf8'
                TABLESPACE = pg_default
                CONNECTION LIMIT = -1", 
                connection);

            await createCommand.ExecuteNonQueryAsync();

            Console.WriteLine($"✅ Base de datos {databaseName} creada");
        }

        /// <summary>
        /// Crea la estructura de tablas del tenant
        /// </summary>
        private async Task CreateTenantStructureAsync(string databaseName)
        {
            Console.WriteLine($"🏗️  Creando estructura de tablas...");

            var scriptPath = Path.Combine(_scriptsPath, "02-CreateTenantTemplate.sql");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Script no encontrado: {scriptPath}");
            }

            var script = await File.ReadAllTextAsync(scriptPath);
            var tenantConnectionString = GetTenantConnectionString(databaseName);

            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            using var command = new NpgsqlCommand(script, connection);
            command.CommandTimeout = 300;

            await command.ExecuteNonQueryAsync();

            Console.WriteLine("✅ Estructura de tablas creada");
        }

        /// <summary>
        /// Inserta datos iniciales del tenant
        /// </summary>
        private async Task SeedTenantDataAsync(string databaseName, string tenantId, string companyName, string adminEmail, string adminPassword)
        {
            Console.WriteLine("🌱 Insertando datos iniciales...");

            var tenantConnectionString = GetTenantConnectionString(databaseName);

            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();

            // 1. Crear la empresa
            var createCompanyCommand = new NpgsqlCommand(@"
                INSERT INTO Companies (Id, Name, TenantId, CreatedAt)
                VALUES (@id, @name, @tenantId, @createdAt)", connection);

            createCompanyCommand.Parameters.AddWithValue("id", Guid.NewGuid());
            createCompanyCommand.Parameters.AddWithValue("name", companyName);
            createCompanyCommand.Parameters.AddWithValue("tenantId", tenantId);
            createCompanyCommand.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

            await createCompanyCommand.ExecuteNonQueryAsync();

            // 2. Crear departamento por defecto
            var departmentId = Guid.NewGuid();
            var createDepartmentCommand = new NpgsqlCommand(@"
                INSERT INTO Departments (Id, Name, Description, CreatedAt)
                VALUES (@id, @name, @description, @createdAt)", connection);

            createDepartmentCommand.Parameters.AddWithValue("id", departmentId);
            createDepartmentCommand.Parameters.AddWithValue("name", "Administración");
            createDepartmentCommand.Parameters.AddWithValue("description", "Departamento administrativo");
            createDepartmentCommand.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

            await createDepartmentCommand.ExecuteNonQueryAsync();

            // 3. Crear horario por defecto
            var scheduleId = Guid.NewGuid();
            var createScheduleCommand = new NpgsqlCommand(@"
                INSERT INTO WorkSchedules (Id, Name, StartTime, EndTime, BreakDuration, IsDefault, CreatedAt)
                VALUES (@id, @name, @startTime, @endTime, @breakDuration, @isDefault, @createdAt)", connection);

            createScheduleCommand.Parameters.AddWithValue("id", scheduleId);
            createScheduleCommand.Parameters.AddWithValue("name", "Horario Estándar");
            createScheduleCommand.Parameters.AddWithValue("startTime", new TimeSpan(8, 0, 0)); // 8:00 AM
            createScheduleCommand.Parameters.AddWithValue("endTime", new TimeSpan(17, 0, 0)); // 5:00 PM
            createScheduleCommand.Parameters.AddWithValue("breakDuration", 60); // 60 minutos
            createScheduleCommand.Parameters.AddWithValue("isDefault", true);
            createScheduleCommand.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

            await createScheduleCommand.ExecuteNonQueryAsync();

            // 4. Crear administrador
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            var adminId = Guid.NewGuid();

            var createAdminCommand = new NpgsqlCommand(@"
                INSERT INTO Employees (Id, FirstName, LastName, Email, PasswordHash, Role, DepartmentId, WorkScheduleId, IsActive, CreatedAt)
                VALUES (@id, @firstName, @lastName, @email, @passwordHash, @role, @departmentId, @workScheduleId, @isActive, @createdAt)", connection);

            createAdminCommand.Parameters.AddWithValue("id", adminId);
            createAdminCommand.Parameters.AddWithValue("firstName", "Administrador");
            createAdminCommand.Parameters.AddWithValue("lastName", "Principal");
            createAdminCommand.Parameters.AddWithValue("email", adminEmail);
            createAdminCommand.Parameters.AddWithValue("passwordHash", hashedPassword);
            createAdminCommand.Parameters.AddWithValue("role", "CompanyAdmin");
            createAdminCommand.Parameters.AddWithValue("departmentId", departmentId);
            createAdminCommand.Parameters.AddWithValue("workScheduleId", scheduleId);
            createAdminCommand.Parameters.AddWithValue("isActive", true);
            createAdminCommand.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

            await createAdminCommand.ExecuteNonQueryAsync();

            // 5. Crear empleados de ejemplo
            await CreateSampleEmployeesAsync(connection, departmentId, scheduleId);

            Console.WriteLine("✅ Datos iniciales insertados");
        }

        /// <summary>
        /// Crea empleados de ejemplo para el tenant
        /// </summary>
        private async Task CreateSampleEmployeesAsync(NpgsqlConnection connection, Guid departmentId, Guid scheduleId)
        {
            var sampleEmployees = new[]
            {
                new { FirstName = "Juan", LastName = "Pérez", Email = "juan.perez@empresa.com" },
                new { FirstName = "María", LastName = "García", Email = "maria.garcia@empresa.com" },
                new { FirstName = "Carlos", LastName = "López", Email = "carlos.lopez@empresa.com" },
                new { FirstName = "Ana", LastName = "Martínez", Email = "ana.martinez@empresa.com" }
            };

            foreach (var emp in sampleEmployees)
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword("123456"); // Contraseña temporal

                var createEmployeeCommand = new NpgsqlCommand(@"
                    INSERT INTO Employees (Id, FirstName, LastName, Email, PasswordHash, Role, DepartmentId, WorkScheduleId, IsActive, CreatedAt)
                    VALUES (@id, @firstName, @lastName, @email, @passwordHash, @role, @departmentId, @workScheduleId, @isActive, @createdAt)", connection);

                createEmployeeCommand.Parameters.AddWithValue("id", Guid.NewGuid());
                createEmployeeCommand.Parameters.AddWithValue("firstName", emp.FirstName);
                createEmployeeCommand.Parameters.AddWithValue("lastName", emp.LastName);
                createEmployeeCommand.Parameters.AddWithValue("email", emp.Email);
                createEmployeeCommand.Parameters.AddWithValue("passwordHash", hashedPassword);
                createEmployeeCommand.Parameters.AddWithValue("role", "Employee");
                createEmployeeCommand.Parameters.AddWithValue("departmentId", departmentId);
                createEmployeeCommand.Parameters.AddWithValue("workScheduleId", scheduleId);
                createEmployeeCommand.Parameters.AddWithValue("isActive", true);
                createEmployeeCommand.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

                await createEmployeeCommand.ExecuteNonQueryAsync();
            }

            Console.WriteLine("✅ Empleados de ejemplo creados");
        }

        /// <summary>
        /// Elimina la base de datos del tenant
        /// </summary>
        private async Task DropTenantDatabaseAsync(string databaseName)
        {
            Console.WriteLine($"🗑️  Eliminando base de datos: {databaseName}");

            var masterConnectionString = GetMasterConnectionString();

            using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();

            // Terminar conexiones activas
            var killConnectionsCommand = new NpgsqlCommand($@"
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{databaseName}'
                AND pid <> pg_backend_pid()", connection);

            await killConnectionsCommand.ExecuteNonQueryAsync();

            // Eliminar la base de datos
            var dropCommand = new NpgsqlCommand($@"DROP DATABASE IF EXISTS ""{databaseName}""", connection);
            await dropCommand.ExecuteNonQueryAsync();

            Console.WriteLine($"✅ Base de datos {databaseName} eliminada");
        }

        /// <summary>
        /// Elimina el registro del tenant de la BD central
        /// </summary>
        private async Task DeleteTenantRecordAsync(string tenantId)
        {
            Console.WriteLine("🗑️  Eliminando registro del tenant...");

            var centralConnectionString = GetCentralConnectionString();

            using var connection = new NpgsqlConnection(centralConnectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(
                "DELETE FROM Tenants WHERE Id = @tenantId", 
                connection);
            command.Parameters.AddWithValue("tenantId", tenantId);

            await command.ExecuteNonQueryAsync();

            Console.WriteLine("✅ Registro del tenant eliminado");
        }

        /// <summary>
        /// Limpia un tenant que falló durante la creación
        /// </summary>
        private async Task CleanupFailedTenantAsync(string tenantId)
        {
            Console.WriteLine($"🧹 Limpiando tenant fallido: {tenantId}");

            try
            {
                // Intentar eliminar BD del tenant
                var tenantDbName = $"SphereTimeControl_{tenantId}";
                await DropTenantDatabaseAsync(tenantDbName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  No se pudo limpiar BD del tenant: {ex.Message}");
            }

            try
            {
                // Intentar eliminar registro central
                await DeleteTenantRecordAsync(tenantId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  No se pudo limpiar registro central: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la cadena de conexión para la BD central
        /// </summary>
        private string GetCentralConnectionString()
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString)
            {
                Database = "SphereTimeControl"
            };
            return builder.ToString();
        }

        /// <summary>
        /// Obtiene la cadena de conexión master (postgres)
        /// </summary>
        private string GetMasterConnectionString()
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString)
            {
                Database = "postgres"
            };
            return builder.ToString();
        }

        /// <summary>
        /// Obtiene la cadena de conexión para un tenant específico
        /// </summary>
        private string GetTenantConnectionString(string databaseName)
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString)
            {
                Database = databaseName
            };
            return builder.ToString();
        }
    }
}