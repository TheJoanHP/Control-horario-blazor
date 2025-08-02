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
                Console.