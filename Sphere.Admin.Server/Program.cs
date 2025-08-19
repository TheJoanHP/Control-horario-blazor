// Sphere.Admin.Server/Program.cs
// Versión completa con solución de ciclos de referencia y configuración correcta

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Sphere.Admin.Server.Data;
using Shared.Models.Core;
using Shared.Models.Enums;
using System.Security.Cryptography;
using Shared.Services.Security;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CONFIGURACIÓN DE CONTROLADORES CON JSON SERIALIZER CORREGIDO
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // IMPORTANTE: Ignorar ciclos de referencia
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        
        // Opcional: Configurar la profundidad máxima (por defecto es 32)
        options.JsonSerializerOptions.MaxDepth = 64;
        
        // Ignorar propiedades nulas para reducir el tamaño del JSON
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        
        // Convertir enums a strings en lugar de números
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        
        // Permitir campos de solo lectura
        options.JsonSerializerOptions.IncludeFields = true;
    });

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Configuración de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Sphere Admin API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSphereClients", policy =>
    {
        policy.WithOrigins(
                // Puertos del SERVER
                "https://localhost:7051",
                "http://localhost:5110",
                
                // Puertos del CLIENT
                "https://localhost:7001", 
                "http://localhost:5001",
                
                // Puertos adicionales de desarrollo
                "https://localhost:7156",
                "http://localhost:5156"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
    
    // Política más permisiva para desarrollo
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("DevelopmentCors", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    }
});

// Configuración de Entity Framework
builder.Services.AddDbContext<SphereDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    
    options.UseNpgsql(connectionString);
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Logging mejorado
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

var app = builder.Build();

// ============================================================
// AUTO-MIGRACIÓN Y SEEDING DE DATOS INICIALES
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SphereDbContext>();
    var startupLogger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        startupLogger.LogInformation("🔄 Verificando base de datos...");

        if (context.Database.GetPendingMigrations().Any())
        {
            startupLogger.LogInformation("📦 Aplicando migraciones pendientes...");
            await context.Database.MigrateAsync();
            startupLogger.LogInformation("✅ Migraciones aplicadas exitosamente");
        }
        else
        {
            startupLogger.LogInformation("✅ Base de datos actualizada");
        }

        // OJO: aquí te falta definir passwordService antes de llamarlo
        var passwordService = services.GetRequiredService<IPasswordService>();

        await SeedInitialDataAsync(context, passwordService, startupLogger);
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "❌ Error durante la configuración de la base de datos");
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// Configuración del pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sphere Admin API v1");
        c.RoutePrefix = "swagger";
    });
    
    // Usar política permisiva en desarrollo
    app.UseCors("DevelopmentCors");
}
else
{
    // Usar política restrictiva en producción
    app.UseCors("AllowSphereClients");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoint de health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Endpoint de información
app.MapGet("/", () => Results.Ok(new
{
    name = "Sphere Admin API",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    documentation = "/swagger"
}));

app.Run();

// ============================================================
// FUNCIÓN DE SEEDING DE DATOS INICIALES
// ============================================================
async Task SeedInitialDataAsync(SphereDbContext context, IPasswordService passwordService, ILogger logger)
{
    bool hasChanges = false;

    try
    {
        // 1. CREAR SUPER ADMIN SI NO EXISTE
        if (!await context.SphereAdmins.AnyAsync())
        {
            logger.LogInformation("👤 Creando Super Admin por defecto...");

            var adminPassword = "Admin123!"; // Contraseña por defecto
            var hashedPassword = passwordService.HashPassword(adminPassword);

            var superAdmin = new SphereAdmin
            {
                FirstName = "Super",
                LastName = "Admin",
                Email = "admin@spheretimecontrol.com",
                PasswordHash = hashedPassword,
                Role = UserRole.SuperAdmin,
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.SphereAdmins.Add(superAdmin);
            hasChanges = true;

            logger.LogInformation("✅ Super Admin creado:");
            logger.LogInformation($"   📧 Email: admin@spheretimecontrol.com");
            logger.LogInformation($"   🔑 Contraseña: Admin123!");
            logger.LogInformation("   ⚠️  IMPORTANTE: Cambie esta contraseña en el primer inicio de sesión");
        }

        // 2. CREAR CONFIGURACIONES DEL SISTEMA SI NO EXISTEN
        if (!await context.SystemConfigs.AnyAsync())
        {
            logger.LogInformation("⚙️ Creando configuraciones del sistema...");

            var systemConfigs = new List<SystemConfig>
            {
                new SystemConfig
                {
                    Key = "SystemName",
                    Value = "Sphere Time Control",
                    Description = "Nombre del sistema",
                    Category = "General",
                    IsEditable = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemConfig
                {
                    Key = "SystemVersion",
                    Value = "1.0.0",
                    Description = "Versión del sistema",
                    Category = "General",
                    IsEditable = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemConfig
                {
                    Key = "DefaultTrialDays",
                    Value = "30",
                    Description = "Días de prueba por defecto para nuevos tenants",
                    Category = "License",
                    IsEditable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemConfig
                {
                    Key = "MaxEmployeesPerTenant",
                    Value = "1000",
                    Description = "Máximo de empleados por tenant",
                    Category = "License",
                    IsEditable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemConfig
                {
                    Key = "AllowSelfRegistration",
                    Value = "true",
                    Description = "Permitir auto-registro de nuevos tenants",
                    Category = "System",
                    IsEditable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemConfig
                {
                    Key = "MaintenanceMode",
                    Value = "false",
                    Description = "Modo de mantenimiento del sistema",
                    Category = "System",
                    IsEditable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemConfig
                {
                    Key = "SystemEmail",
                    Value = "noreply@spheretimecontrol.com",
                    Description = "Email del sistema para notificaciones",
                    Category = "Email",
                    IsEditable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemConfig
                {
                    Key = "SmtpHost",
                    Value = "smtp.gmail.com",
                    Description = "Servidor SMTP para envío de emails",
                    Category = "Email",
                    IsEditable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SystemConfig
                {
                    Key = "SmtpPort",
                    Value = "587",
                    Description = "Puerto SMTP",
                    Category = "Email",
                    IsEditable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.SystemConfigs.AddRange(systemConfigs);
            hasChanges = true;
            logger.LogInformation($"✅ {systemConfigs.Count} configuraciones del sistema creadas");
        }

        // 3. CREAR TENANT DE DEMOSTRACIÓN (OPCIONAL)
        if (!await context.Tenants.AnyAsync() && app.Environment.IsDevelopment())
        {
            logger.LogInformation("🏢 Creando tenant de demostración...");

            var demoTenant = new Tenant
            {
                Code = "DEMO",
                Name = "Empresa Demo",
                Description = "Empresa de demostración para pruebas",
                Subdomain = "demo",
                DatabaseName = "spheretimecontrol_demo",
                ContactEmail = "admin@demo.com",
                ContactPhone = "+34 900 123 456",
                Address = "Calle Demo 123",
                City = "Madrid",
                PostalCode = "28001",
                Country = "España",
                TaxId = "B12345678",
                LicenseType = LicenseType.Trial,
                MaxEmployees = 10,
                MonthlyPrice = 0,
                Currency = "EUR",
                TrialStartedAt = DateTime.UtcNow,
                TrialEndedAt = DateTime.UtcNow.AddDays(30),
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Tenants.Add(demoTenant);
            await context.SaveChangesAsync(); // Guardar para obtener el ID

            // Crear licencia para el tenant demo
            var demoLicense = new License
            {
                TenantId = demoTenant.Id,
                LicenseType = LicenseType.Trial,
                MaxEmployees = 10,
                MonthlyPrice = 0,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Active = true,
                HasReports = true,
                HasAdvancedReports = false,
                HasMobileApp = true,
                HasAPI = false,
                HasGeolocation = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Licenses.Add(demoLicense);
            hasChanges = true;

            logger.LogInformation("✅ Tenant de demostración creado:");
            logger.LogInformation($"   🏢 Empresa: {demoTenant.Name}");
            logger.LogInformation($"   🌐 Subdominio: {demoTenant.Subdomain}");
            logger.LogInformation($"   📧 Email: {demoTenant.ContactEmail}");
            logger.LogInformation($"   📅 Trial hasta: {demoTenant.TrialEndedAt:yyyy-MM-dd}");
        }

        // 4. GUARDAR TODOS LOS CAMBIOS
        if (hasChanges)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("💾 Todos los datos iniciales guardados correctamente");
        }
        else
        {
            logger.LogInformation("ℹ️ No se requieren datos iniciales, la base de datos ya contiene información");
        }

        // 5. MOSTRAR RESUMEN
        logger.LogInformation("📊 Resumen de la base de datos:");
        logger.LogInformation($"   👥 Super Admins: {await context.SphereAdmins.CountAsync()}");
        logger.LogInformation($"   🏢 Tenants: {await context.Tenants.CountAsync()}");
        logger.LogInformation($"   📜 Licencias: {await context.Licenses.CountAsync()}");
        logger.LogInformation($"   ⚙️ Configuraciones: {await context.SystemConfigs.CountAsync()}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error durante el seeding de datos iniciales");
        throw;
    }
}