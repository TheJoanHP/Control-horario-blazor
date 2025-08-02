var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Company Admin API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    cusing Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Company.Admin.Server.Data;
using Company.Admin.Server.Services;
using Company.Admin.Server.Middleware;
using Company.Admin.Server.Mappings;
using Shared.Services.Security;
using Shared.Services.Database;
using Shared.Services.Communication;
using Shared.Services.Storage;
using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Company Admin API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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
            new string[] {}
        }
    });
});

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7002", // Company.Admin.Client
                "http://localhost:5002",
                "https://localhost:7003", // Employee.App.Client  
                "http://localhost:5003"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configuración de Entity Framework
builder.Services.AddDbContext<CompanyDbContext>((serviceProvider, options) =>
{
    var tenantResolver = serviceProvider.GetRequiredService<ITenantResolver>();
    var connectionString = tenantResolver.GetConnectionString();
    options.UseNpgsql(connectionString);
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configuración de autenticación JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no está configurada");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated for: " + context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CompanyAdmin", policy => 
        policy.RequireRole("CompanyAdmin"));
    options.AddPolicy("Supervisor", policy => 
        policy.RequireRole("CompanyAdmin", "Supervisor"));
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Servicios compartidos
builder.Services.AddScoped<ITenantResolver, TenantResolver>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileService, FileService>();

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
        await SeedDevelopmentDataAsync(context);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

app.Run();

// Método para sembrar datos de desarrollo
static async Task SeedDevelopmentDataAsync(CompanyDbContext context)
{
    // Solo agregar datos si no existen
    if (!context.Companies.Any())
    {
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
            VacationDaysPerYear = 22
        };
        
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        
        // Agregar departamento por defecto
        var department = new Shared.Models.Core.Department
        {
            CompanyId = company.Id,
            Name = "Administración",
            Description = "Departamento de administración general",
            Active = true
        };
        
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        
        // Agregar administrador por defecto
        var passwordService = new Shared.Services.Security.PasswordService();
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
            HiredAt = DateTime.UtcNow
        };
        
        context.Employees.Add(admin);
        await context.SaveChangesAsync();
    }
}