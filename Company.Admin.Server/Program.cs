using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Company.Admin.Server.Data;
using Company.Admin.Server.Services;
using Company.Admin.Server.Middleware;
using Shared.Services.Security;
using Shared.Services.Database;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la base de datos
builder.Services.AddDbContext<CompanyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de AutoMapper
builder.Services.AddAutoMapper(typeof(Company.Admin.Server.Mappings.AutoMapperProfile));

// Configuración CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7000", // Company.Admin.Client
                "http://localhost:5173",  // Vite dev server
                "http://localhost:3000"   // Posible frontend alternativo
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configuración de autenticación JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your-super-secret-jwt-key-that-should-be-at-least-32-characters-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SphereTimeControl";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CompanyAdmin";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();

// Configuración de controladores
builder.Services.AddControllers();

// Configuración de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Company Admin API", 
        Version = "v1",
        Description = "API para administración de empresas y empleados"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Servicios compartidos
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITenantResolver, TenantResolver>();

// Servicios de la aplicación
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ITimeTrackingService, TimeTrackingService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Configuración de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

var app = builder.Build();

// Configuración del pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Company Admin API v1");
        c.RoutePrefix = string.Empty; // Para que Swagger esté en la raíz
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Middleware personalizado para multi-tenant (DEBE ir antes de Authentication)
app.UseMiddleware<TenantMiddleware>();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

// Middleware de manejo de errores global
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName 
});

// Ejecutar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
        await context.Database.EnsureCreatedAsync();
        
        // Seed data si es necesario
        await SeedDevelopmentDataAsync(context, scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

app.Run();

// Método para sembrar datos de desarrollo
static async Task SeedDevelopmentDataAsync(CompanyDbContext context, IServiceProvider serviceProvider)
{
    // Solo agregar datos si no existen
    if (!context.Companies.Any())
    {
        var passwordService = serviceProvider.GetRequiredService<IPasswordService>();
        
        var company = new Shared.Models.Core.Company
        {
            Name = "Empresa Demo",
            Email = "admin@empresademo.com",
            Phone = "+34 666 777 888",
            Address = "Calle Principal 123, Madrid",
            Active = true,
            WorkStartTime = new TimeSpan(9, 0, 0),
            WorkEndTime = new TimeSpan(18, 0, 0),
            ToleranceMinutes = 15,
            VacationDaysPerYear = 22,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        
        // Agregar departamento por defecto
        var department = new Shared.Models.Core.Department
        {
            CompanyId = company.Id,
            Name = "Administración",
            Description = "Departamento de administración general",
            Active = true,
            CreatedAt = DateTime.UtcNow
        };
        
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        
        // Agregar administrador por defecto
        var admin = new Shared.Models.Core.Employee
        {
            CompanyId = company.Id,
            DepartmentId = department.Id,
            FirstName = "Admin",
            LastName = "Demo",
            Email = "admin@empresademo.com",
            EmployeeCode = "ADMIN001",
            Role = Shared.Models.Enums.UserRole.CompanyAdmin,
            PasswordHash = passwordService.HashPassword("admin123"),
            Active = true,
            HiredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        context.Employees.Add(admin);
        await context.SaveChangesAsync();

        Console.WriteLine("✅ Datos de desarrollo creados:");
        Console.WriteLine("   • Empresa: Empresa Demo");
        Console.WriteLine("   • Admin: admin@empresademo.com / admin123");
    }
}