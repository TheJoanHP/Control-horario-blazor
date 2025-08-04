using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Database.Setup.Tools;
using Shared.Services.Security;

namespace Database.Setup
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🕐 Sphere Time Control - Database Setup Tool");
            Console.WriteLine("==============================================");
            
            // Configurar servicios
            var host = CreateHostBuilder(args).Build();
            var configuration = host.Services.GetRequiredService<IConfiguration>();
            var logger = host.Services.GetRequiredService<ILogger<DatabaseSeeder>>();
            var passwordService = host.Services.GetRequiredService<IPasswordService>();
            
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string no configurada");
            
            var seeder = new DatabaseSeeder(logger, passwordService, connectionString);
            var tenantCreator = new TenantCreator(connectionString, "Scripts", host.Services.GetService<ILogger<TenantCreator>>());
            
            try
            {
                if (args.Length == 0)
                {
                    await ShowMenuAsync(seeder, tenantCreator);
                }
                else
                {
                    await ProcessCommandLineAsync(args, seeder, tenantCreator);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error crítico: {ex.Message}");
                Console.WriteLine($"📝 Detalles: {ex}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Crea el host builder con configuración
        /// </summary>
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false)
                          .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                          .AddEnvironmentVariables()
                          .AddCommandLine(args);
                })
                .ConfigureServices((context, services) =>
                {
                    // Registrar servicios necesarios
                    services.AddSingleton<IJwtService, JwtService>();
                    services.AddSingleton<IPasswordService, PasswordService>();
                });

        /// <summary>
        /// Muestra el menú interactivo
        /// </summary>
        static async Task ShowMenuAsync(DatabaseSeeder seeder, TenantCreator tenantCreator)
        {
            bool exit = false;
            
            while (!exit)
            {
                Console.WriteLine("\n🔧 Opciones disponibles:");
                Console.WriteLine("1. 🏗️  Configuración completa (Central + Demo Tenant)");
                Console.WriteLine("2. 📦 Crear solo base de datos central");
                Console.WriteLine("3. 🏢 Crear nuevo tenant");
                Console.WriteLine("4. 📋 Listar tenants existentes");
                Console.WriteLine("5. 🗑️  Eliminar tenant");
                Console.WriteLine("6. 🧹 Limpiar todas las bases de datos");
                Console.WriteLine("7. 📊 Mostrar estado de bases de datos");
                Console.WriteLine("8. ❌ Salir");
                Console.Write("\n👉 Seleccione una opción (1-8): ");

                var option = Console.ReadLine();

                try
                {
                    switch (option)
                    {
                        case "1":
                            await FullSetupAsync(seeder);
                            break;
                        case "2":
                            await CreateCentralDatabaseAsync(seeder);
                            break;
                        case "3":
                            await CreateNewTenantAsync(tenantCreator);
                            break;
                        case "4":
                            await tenantCreator.ListTenantsAsync();
                            break;
                        case "5":
                            await DeleteTenantAsync(tenantCreator);
                            break;
                        case "6":
                            await CleanupDatabasesAsync(seeder);
                            break;
                        case "7":
                            await ShowDatabaseStatusAsync(seeder);
                            break;
                        case "8":
                            exit = true;
                            Console.WriteLine("👋 ¡Hasta luego!");
                            break;
                        default:
                            Console.WriteLine("❌ Opción no válida. Intente nuevamente.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    Console.WriteLine("📝 Presione cualquier tecla para continuar...");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>
        /// Procesa comandos de línea de comandos
        /// </summary>
        static async Task ProcessCommandLineAsync(string[] args, DatabaseSeeder seeder, TenantCreator tenantCreator)
        {
            var command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "setup":
                case "install":
                    Console.WriteLine("🚀 Ejecutando configuración completa...");
                    await seeder.SeedAllAsync();
                    break;

                case "central":
                    Console.WriteLine("📦 Creando base de datos central...");
                    await seeder.CreateCentralDatabaseAsync();
                    break;

                case "tenant":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("❌ Uso: dotnet run tenant <tenant-id> [company-name] [admin-email]");
                        return;
                    }
                    var tenantId = args[1];
                    var companyName = args.Length > 2 ? args[2] : null;
                    var adminEmail = args.Length > 3 ? args[3] : null;
                    
                    await tenantCreator.CreateTenantAsync(tenantId, companyName, adminEmail);
                    break;

                case "list":
                    await tenantCreator.ListTenantsAsync();
                    break;

                case "delete":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("❌ Uso: dotnet run delete <tenant-id>");
                        return;
                    }
                    await tenantCreator.DeleteTenantAsync(args[1]);
                    break;

                case "cleanup":
                case "clean":
                    Console.WriteLine("🧹 Limpiando bases de datos...");
                    await seeder.CleanupAllDatabasesAsync();
                    break;

                case "status":
                    await seeder.ShowDatabaseStatusAsync();
                    break;

                case "help":
                case "--help":
                case "-h":
                    ShowHelpInfo();
                    break;

                default:
                    Console.WriteLine($"❌ Comando desconocido: {command}");
                    Console.WriteLine("💡 Use 'dotnet run help' para ver comandos disponibles");
                    break;
            }
        }

        /// <summary>
        /// Configuración completa del sistema
        /// </summary>
        static async Task FullSetupAsync(DatabaseSeeder seeder)
        {
            Console.WriteLine("🚀 Iniciando configuración completa del sistema...");
            Console.WriteLine("=====================================");
            
            try
            {
                await seeder.SeedAllAsync();
                Console.WriteLine("✅ Configuración completa finalizada");
                Console.WriteLine("\n📋 Resumen:");
                Console.WriteLine("• Base de datos central: SphereTimeControl_Central");
                Console.WriteLine("• Tenant demo: SphereTimeControl_demo");
                Console.WriteLine("• Super Admin: admin@spheretimecontrol.com / admin123");
                Console.WriteLine("• Company Admin: admin@demo.com / admin123");
                Console.WriteLine("\n🌐 URLs de acceso:");
                Console.WriteLine("• Sphere Admin: https://sphere-admin.tudominio.com");
                Console.WriteLine("• Company Admin: https://demo.tudominio.com");
                Console.WriteLine("• Employee App: https://app.tudominio.com");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error durante la configuración: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crear solo la base de datos central
        /// </summary>
        static async Task CreateCentralDatabaseAsync(DatabaseSeeder seeder)
        {
            Console.WriteLine("📦 Creando base de datos central de Sphere...");
            
            try
            {
                await seeder.CreateCentralDatabaseAsync();
                Console.WriteLine("✅ Base de datos central creada");
                Console.WriteLine("👤 Super Admin: admin@spheretimecontrol.com / admin123");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creando base de datos central: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crear nuevo tenant interactivamente
        /// </summary>
        static async Task CreateNewTenantAsync(TenantCreator tenantCreator)
        {
            Console.WriteLine("🏢 Crear nuevo tenant");
            Console.WriteLine("====================");

            Console.Write("📝 ID del tenant (ej: empresa1): ");
            var tenantId = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                Console.WriteLine("❌ El ID del tenant es requerido");
                return;
            }

            Console.Write("🏢 Nombre de la empresa (opcional): ");
            var companyName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(companyName)) companyName = null;

            Console.Write("📧 Email del administrador (opcional): ");
            var adminEmail = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(adminEmail)) adminEmail = null;

            Console.Write("🔐 Contraseña del administrador [admin123]: ");
            var adminPassword = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(adminPassword)) adminPassword = "admin123";

            try
            {
                await tenantCreator.CreateTenantAsync(tenantId, companyName, adminEmail, adminPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creando tenant: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Eliminar tenant interactivamente
        /// </summary>
        static async Task DeleteTenantAsync(TenantCreator tenantCreator)
        {
            Console.WriteLine("🗑️ Eliminar tenant");
            Console.WriteLine("==================");

            // Mostrar tenants existentes primero
            await tenantCreator.ListTenantsAsync();

            Console.Write("\n📝 ID del tenant a eliminar: ");
            var tenantId = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                Console.WriteLine("❌ El ID del tenant es requerido");
                return;
            }

            Console.WriteLine($"⚠️ ADVERTENCIA: Se eliminará permanentemente el tenant '{tenantId}' y todos sus datos.");
            Console.Write("❓ ¿Está seguro? (escriba 'SI' para confirmar): ");
            var confirmation = Console.ReadLine();

            if (confirmation?.ToUpperInvariant() == "SI")
            {
                try
                {
                    await tenantCreator.DeleteTenantAsync(tenantId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error eliminando tenant: {ex.Message}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine("❌ Operación cancelada");
            }
        }

        /// <summary>
        /// Limpiar todas las bases de datos
        /// </summary>
        static async Task CleanupDatabasesAsync(DatabaseSeeder seeder)
        {
            Console.WriteLine("🧹 Limpiar bases de datos");
            Console.WriteLine("========================");
            Console.WriteLine("⚠️ ADVERTENCIA: Esta operación eliminará TODAS las bases de datos y datos del sistema.");
            Console.Write("❓ ¿Está seguro? (escriba 'SI' para confirmar): ");
            var confirmation = Console.ReadLine();

            if (confirmation?.ToUpperInvariant() == "SI")
            {
                try
                {
                    await seeder.CleanupAllDatabasesAsync();
                    Console.WriteLine("✅ Limpieza completada");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error durante la limpieza: {ex.Message}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine("❌ Operación cancelada");
            }
        }

        /// <summary>
        /// Muestra el estado de las bases de datos
        /// </summary>
        static async Task ShowDatabaseStatusAsync(DatabaseSeeder seeder)
        {
            Console.WriteLine("📊 Estado de las bases de datos");
            Console.WriteLine("===============================");
            
            try
            {
                await seeder.ShowDatabaseStatusAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al verificar estado: {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra información de ayuda
        /// </summary>
        static void ShowHelpInfo()
        {
            Console.WriteLine("📚 Sphere Time Control - Database Setup Tool");
            Console.WriteLine("============================================");
            Console.WriteLine();
            Console.WriteLine("🔧 Comandos disponibles:");
            Console.WriteLine();
            Console.WriteLine("  setup               Configuración completa del sistema");
            Console.WriteLine("  central             Crear solo base de datos central");
            Console.WriteLine("  tenant <id>         Crear nuevo tenant");
            Console.WriteLine("  list                Listar tenants existentes");
            Console.WriteLine("  delete <id>         Eliminar tenant");
            Console.WriteLine("  cleanup             Limpiar todas las bases de datos");
            Console.WriteLine("  status              Mostrar estado de bases de datos");
            Console.WriteLine("  help                Mostrar esta ayuda");
            Console.WriteLine();
            Console.WriteLine("📝 Ejemplos:");
            Console.WriteLine();
            Console.WriteLine("  dotnet run setup");
            Console.WriteLine("  dotnet run tenant empresa1");
            Console.WriteLine("  dotnet run tenant empresa1 \"Mi Empresa\" admin@miempresa.com");
            Console.WriteLine("  dotnet run list");
            Console.WriteLine("  dotnet run delete empresa1");
            Console.WriteLine();
            Console.WriteLine("⚙️ Configuración:");
            Console.WriteLine();
            Console.WriteLine("  Edite appsettings.json para configurar la conexión a PostgreSQL");
            Console.WriteLine("  Valor por defecto: Host=localhost;Database=postgres;Username=postgres;Password=postgres");
        }
    }
}