using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string no configurada");
            
            var seeder = new DatabaseSeeder(connectionString);
            var tenantCreator = new TenantCreator(connectionString);
            
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
                    services.AddSingleton<JwtService>();
                    services.AddSingleton<PasswordService>();
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
                Console.WriteLine("4. 🔄 Recrear tenant demo");
                Console.WriteLine("5. 🧹 Limpiar todas las bases de datos");
                Console.WriteLine("6. 📊 Ver estado de las bases de datos");
                Console.WriteLine("0. ❌ Salir");
                Console.WriteLine();
                Console.Write("Selecciona una opción: ");
                
                var input = Console.ReadLine();
                
                try
                {
                    switch (input)
                    {
                        case "1":
                            await seeder.SeedAllAsync();
                            break;
                            
                        case "2":
                            await seeder.CreateCentralDatabaseAsync();
                            break;
                            
                        case "3":
                            Console.Write("Ingresa el ID del tenant (ej: empresa1): ");
                            var tenantId = Console.ReadLine();
                            if (!string.IsNullOrEmpty(tenantId))
                            {
                                await tenantCreator.CreateTenantAsync(tenantId);
                            }
                            break;
                            
                        case "4":
                            await seeder.CreateDemoTenantAsync();
                            break;
                            
                        case "5":
                            if (await ConfirmActionAsync("¿Estás seguro de que deseas eliminar TODAS las bases de datos? (s/N)"))
                            {
                                await CleanupDatabasesAsync(seeder);
                            }
                            break;
                            
                        case "6":
                            await ShowDatabaseStatusAsync(seeder);
                            break;
                            
                        case "0":
                            exit = true;
                            Console.WriteLine("👋 ¡Hasta luego!");
                            break;
                            
                        default:
                            Console.WriteLine("⚠️  Opción no válida");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    Console.WriteLine("Presiona cualquier tecla para continuar...");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>
        /// Procesa comandos de línea de comandos
        /// </summary>
        static async Task ProcessCommandLineAsync(string[] args, DatabaseSeeder seeder, TenantCreator tenantCreator)
        {
            var command = args[0].ToLower();
            
            switch (command)
            {
                case "setup":
                case "init":
                    await seeder.SeedAllAsync();
                    break;
                    
                case "central":
                    await seeder.CreateCentralDatabaseAsync();
                    break;
                    
                case "tenant":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("❌ Uso: dotnet run tenant <tenant-id>");
                        Environment.Exit(1);
                    }
                    await tenantCreator.CreateTenantAsync(args[1]);
                    break;
                    
                case "demo":
                    await seeder.CreateDemoTenantAsync();
                    break;
                    
                case "clean":
                    if (await ConfirmActionAsync("¿Eliminar todas las bases de datos? (s/N)"))
                    {
                        await CleanupDatabasesAsync(seeder);
                    }
                    break;
                    
                case "status":
                    await ShowDatabaseStatusAsync(seeder);
                    break;
                    
                case "help":
                case "--help":
                case "-h":
                    ShowHelp();
                    break;
                    
                default:
                    Console.WriteLine($"❌ Comando desconocido: {command}");
                    ShowHelp();
                    Environment.Exit(1);
                    break;
            }
        }

        /// <summary>
        /// Muestra la ayuda de comandos
        /// </summary>
        static void ShowHelp()
        {
            Console.WriteLine("\n📖 Sphere Time Control - Database Setup Tool");
            Console.WriteLine("==============================================");
            Console.WriteLine("Uso: dotnet run [comando] [parámetros]");
            Console.WriteLine();
            Console.WriteLine("Comandos disponibles:");
            Console.WriteLine("  setup, init           - Configuración completa");
            Console.WriteLine("  central              - Crear solo BD central");
            Console.WriteLine("  tenant <id>          - Crear nuevo tenant");
            Console.WriteLine("  demo                 - Crear tenant demo");
            Console.WriteLine("  clean                - Limpiar todas las BDs");
            Console.WriteLine("  status               - Ver estado de las BDs");
            Console.WriteLine("  help                 - Mostrar esta ayuda");
            Console.WriteLine();
            Console.WriteLine("Ejemplos:");
            Console.WriteLine("  dotnet run setup                    # Configuración completa");
            Console.WriteLine("  dotnet run tenant empresa1          # Crear tenant empresa1");
            Console.WriteLine("  dotnet run clean                    # Limpiar todo");
            Console.WriteLine();
        }

        /// <summary>
        /// Solicita confirmación del usuario
        /// </summary>
        static async Task<bool> ConfirmActionAsync(string message)
        {
            Console.WriteLine();
            Console.WriteLine($"⚠️  {message}");
            var response = Console.ReadLine()?.ToLower();
            return response == "s" || response == "si" || response == "yes" || response == "y";
        }

        /// <summary>
        /// Limpia todas las bases de datos
        /// </summary>
        static async Task CleanupDatabasesAsync(DatabaseSeeder seeder)
        {
            Console.WriteLine("🧹 Iniciando limpieza de bases de datos...");
            
            try
            {
                // Este método debería estar en DatabaseSeeder
                await seeder.CleanupAllDatabasesAsync();
                Console.WriteLine("✅ Limpieza completada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error durante la limpieza: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Muestra el estado de las bases de datos
        /// </summary>
        static async Task ShowDatabaseStatusAsync(DatabaseSeeder seeder)
        {
            Console.WriteLine("📊 Verificando estado de las bases de datos...");
            Console.WriteLine("==============================================");
            
            try
            {
                // Este método debería estar en DatabaseSeeder
                await seeder.ShowDatabaseStatusAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al verificar estado: {ex.Message}");
            }
        }
    }
}