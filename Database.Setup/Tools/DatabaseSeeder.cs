using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

namespace Database.Setup.Tools
{
    public class DatabaseSeeder
    {
        private readonly string _connectionString;
        private readonly string _scriptsPath;

        public DatabaseSeeder(string connectionString, string scriptsPath = "Scripts")
        {
            _connectionString = connectionString;
            _scriptsPath = scriptsPath;
        }

        /// <summary>
        /// Ejecuta todos los scripts de configuración inicial
        /// </summary>
        public async Task SeedAllAsync()
        {
            Console.WriteLine("🚀 Iniciando configuración completa de la base de datos...");
            
            try
            {
                await CreateCentralDatabaseAsync();
                await CreateDemoTenantAsync();
                
                Console.WriteLine("✅ ¡Configuración completada exitosamente!");
                Console.WriteLine("==============================================");
                Console.WriteLine("📊 Resumen:");
                Console.WriteLine("- Base de datos central creada");
                Console.WriteLine("- Tenant demo configurado");
                Console.WriteLine("- Datos de prueba insertados");
                Console.WriteLine("==============================================");
                Console.WriteLine("🔑 Credenciales por defecto:");
                Console.WriteLine("Super Admin: admin@spheretimecontrol.com / admin123");
                Console.WriteLine("Company Admin: admin@empresademo.com / admin123");
                Console.WriteLine("Empleados: [nombre].[apellido]@empresademo.com / admin123");
                Console.WriteLine("==============================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error durante la configuración: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crea la base de datos central
        /// </summary>
        public async Task CreateCentralDatabaseAsync()
        {
            Console.WriteLine("📦 Creando base de datos central...");
            
            var scriptPath = Path.Combine(_scriptsPath, "01-CreateCentralDB.sql");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Script no encontrado: {scriptPath}");
            }

            var script = await File.ReadAllTextAsync(scriptPath);
            
            // Ejecutar con conexión a postgres (para crear la BD)
            var masterConnectionString = GetMasterConnectionString(_connectionString);
            
            using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(script, connection);
            command.CommandTimeout = 300; // 5 minutos
            
            await command.ExecuteNonQueryAsync();
            
            Console.WriteLine("✅ Base de datos central creada");
        }

        /// <summary>
        /// Crea un tenant específico
        /// </summary>
        public async Task CreateTenantAsync(string tenantId)
        {
            Console.WriteLine($"🏢 Creando tenant: {tenantId}...");
            
            var tenantDbName = $"SphereTimeControl_{tenantId}";
            
            // 1. Crear la base de datos del tenant
            await CreateTenantDatabaseAsync(tenantDbName);
            
            // 2. Crear la estructura de tablas
            await CreateTenantStructureAsync(tenantDbName);
            
            // 3. Insertar datos iniciales
            await SeedTenantDataAsync(tenantDbName);
            
            Console.WriteLine($"✅ Tenant {tenantId} creado exitosamente");
        }

        /// <summary>
        /// Crea el tenant demo por defecto
        /// </summary>
        public async Task CreateDemoTenantAsync()
        {
            await CreateTenantAsync("demo");
        }

        /// <summary>
        /// Crea la base de datos física del tenant
        /// </summary>
        private async Task CreateTenantDatabaseAsync(string databaseName)
        {
            var masterConnectionString = GetMasterConnectionString(_connectionString);
            
            using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();
            
            // Verificar si la BD ya existe
            var checkCommand = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @dbname", 
                connection);
            checkCommand.Parameters.AddWithValue("dbname", databaseName);
            
            var exists = await checkCommand.ExecuteScalarAsync();
            
            if (exists == null)
            {
                Console.WriteLine($"📦 Creando base de datos: {databaseName}");
                
                var createCommand = new NpgsqlCommand(
                    $@"CREATE DATABASE ""{databaseName}""
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
            else
            {
                Console.WriteLine($"ℹ️  Base de datos {databaseName} ya existe");
            }
        }

        /// <summary>
        /// Crea la estructura de tablas del tenant
        /// </summary>
        private async Task CreateTenantStructureAsync(string databaseName)
        {
            Console.WriteLine($"🏗️  Creando estructura de tablas en {databaseName}...");
            
            var scriptPath = Path.Combine(_scriptsPath, "02-CreateTenantTemplate.sql");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Script no encontrado: {scriptPath}");
            }

            var script = await File.ReadAllTextAsync(scriptPath);
            
            var tenantConnectionString = _connectionString.Replace("SphereTimeControl_Central", databaseName);
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(script, connection);
            command.CommandTimeout = 300;
            
            await command.ExecuteNonQueryAsync();
            
            Console.WriteLine("✅ Estructura de tablas creada");
        }

        /// <summary>
        /// Inserta datos iniciales en el tenant
        /// </summary>
        private async Task SeedTenantDataAsync(string databaseName)
        {
            Console.WriteLine($"🌱 Insertando datos iniciales en {databaseName}...");
            
            var scriptPath = Path.Combine(_scriptsPath, "03-SeedTenantData.sql");
            if (!File.Exists(scriptPath))
            {
                Console.WriteLine("⚠️  Script de datos iniciales no encontrado, omitiendo...");
                return;
            }

            var script = await File.ReadAllTextAsync(scriptPath);
            
            var tenantConnectionString = _connectionString.Replace("SphereTimeControl_Central", databaseName);
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand(script, connection);
            command.CommandTimeout = 300;
            
            await command.ExecuteNonQueryAsync();
            
            Console.WriteLine("✅ Datos iniciales insertados");
        }

        /// <summary>
        /// Elimina un tenant completamente
        /// </summary>
        public async Task DropTenantAsync(string tenantId)
        {
            Console.WriteLine($"🗑️  Eliminando tenant: {tenantId}...");
            
            var tenantDbName = $"SphereTimeControl_{tenantId}";
            var masterConnectionString = GetMasterConnectionString(_connectionString);
            
            using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();
            
            // Terminar conexiones activas
            var killConnectionsCommand = new NpgsqlCommand(
                $@"SELECT pg_terminate_backend(pid)
                   FROM pg_stat_activity 
                   WHERE datname = '{tenantDbName}' AND pid <> pg_backend_pid()", 
                connection);
            
            await killConnectionsCommand.ExecuteNonQueryAsync();
            
            // Eliminar base de datos
            var dropCommand = new NpgsqlCommand($@"DROP DATABASE IF EXISTS ""{tenantDbName}""", connection);
            await dropCommand.ExecuteNonQueryAsync();
            
            Console.WriteLine($"✅ Tenant {tenantId} eliminado");
        }

        /// <summary>
        /// Verifica el estado de un tenant
        /// </summary>
        public async Task<bool> VerifyTenantAsync(string tenantId)
        {
            try
            {
                var tenantDbName = $"SphereTimeControl_{tenantId}";
                var tenantConnectionString = _connectionString.Replace("SphereTimeControl_Central", tenantDbName);
                
                using var connection = new NpgsqlConnection(tenantConnectionString);
                await connection.OpenAsync();
                
                // Verificar que las tablas principales existan
                var checkCommand = new NpgsqlCommand(
                    @"SELECT COUNT(*) FROM information_schema.tables 
                      WHERE table_schema = 'public' 
                      AND table_name IN ('Companies', 'Employees', 'TimeRecords')",
                    connection);
                
                var tableCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                
                return tableCount >= 3;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene estadísticas de un tenant
        /// </summary>
        public async Task<TenantStats> GetTenantStatsAsync(string tenantId)
        {
            var tenantDbName = $"SphereTimeControl_{tenantId}";
            var tenantConnectionString = _connectionString.Replace("SphereTimeControl_Central", tenantDbName);
            
            using var connection = new NpgsqlConnection(tenantConnectionString);
            await connection.OpenAsync();
            
            var stats = new TenantStats { TenantId = tenantId };
            
            // Contar empleados
            var employeeCommand = new NpgsqlCommand("SELECT COUNT(*) FROM \"Employees\"", connection);
            stats.TotalEmployees = Convert.ToInt32(await employeeCommand.ExecuteScalarAsync());
            
            // Contar departamentos
            var deptCommand = new NpgsqlCommand("SELECT COUNT(*) FROM \"Departments\"", connection);
            stats.TotalDepartments = Convert.ToInt32(await deptCommand.ExecuteScalarAsync());
            
            // Contar registros de tiempo
            var recordsCommand = new NpgsqlCommand("SELECT COUNT(*) FROM \"TimeRecords\"", connection);
            stats.TotalTimeRecords = Convert.ToInt32(await recordsCommand.ExecuteScalarAsync());
            
            return stats;
        }

        /// <summary>
        /// Obtiene la cadena de conexión para operaciones de BD master
        /// </summary>
        private static string GetMasterConnectionString(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = "postgres"
            };
            return builder.ToString();
        }

        /// <summary>
        /// Reset completo del sistema (¡CUIDADO!)
        /// </summary>
        public async Task ResetAllAsync()
        {
            Console.WriteLine("⚠️  ATENCIÓN: Reseteando todo el sistema...");
            Console.WriteLine("Esta operación eliminará TODOS los datos.");
            
            // Eliminar BD central
            var masterConnectionString = GetMasterConnectionString(_connectionString);
            
            using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();
            
            // Eliminar todas las BDs de tenants
            var getTenantsCommand = new NpgsqlCommand(
                @"SELECT datname FROM pg_database 
                  WHERE datname LIKE 'SphereTimeControl_%'", 
                connection);
            
            using var reader = await getTenantsCommand.ExecuteReaderAsync();
            var tenantDbs = new List<string>();
            
            while (await reader.ReadAsync())
            {
                tenantDbs.Add(reader.GetString(0));
            }
            
            reader.Close();
            
            foreach (var dbName in tenantDbs)
            {
                Console.WriteLine($"🗑️  Eliminando {dbName}...");
                
                // Terminar conexiones
                var killCommand = new NpgsqlCommand(
                    $@"SELECT pg_terminate_backend(pid)
                       FROM pg_stat_activity 
                       WHERE datname = '{dbName}' AND pid <> pg_backend_pid()", 
                    connection);
                await killCommand.ExecuteNonQueryAsync();
                
                // Eliminar BD
                var dropCommand = new NpgsqlCommand($@"DROP DATABASE IF EXISTS ""{dbName}""", connection);
                await dropCommand.ExecuteNonQueryAsync();
            }
            
            Console.WriteLine("🔄 Sistema reseteado. Ejecuta SeedAllAsync() para reconfigurar.");
        }
    }

    /// <summary>
    /// Estadísticas de un tenant
    /// </summary>
    public class TenantStats
    {
        public string TenantId { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalTimeRecords { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}