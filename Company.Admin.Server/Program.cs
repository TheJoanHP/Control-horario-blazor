using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Company.Admin.Server.Data;
using Company.Admin.Server.Services;
using Shared.Services.Security;
using Shared.Services.Database;

var builder = WebApplication.CreateBuilder(args);

// *** IMPORTANTE: Registrar HttpContextAccessor PRIMERO ***
builder.Services.AddHttpContextAccessor();

// Configuración de la base de datos
builder.Services.AddDbContext<CompanyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? 
    throw new InvalidOperationException("JWT SecretKey no está configurada");

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
    
    // SOLUCIÓN: Configurar IDs de esquema únicos para evitar conflictos
    c.CustomSchemaIds(type => 
    {
        if (type.FullName?.Contains("Shared.Models.DTOs.Auth.CompanyInfo") == true)
            return "AuthCompanyInfo";
        if (type.FullName?.Contains("Shared.Models.DTOs.Employee.CompanyInfo") == true)
            return "EmployeeCompanyInfo";
        if (type.FullName?.Contains("Shared.Models.DTOs.Auth.EmployeeInfo") == true)
            return "AuthEmployeeInfo";
        if (type.FullName?.Contains("Shared.Models.DTOs.Employee.EmployeeInfo") == true)
            return "EmployeeEmployeeInfo";
        return type.Name;
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

// Registrar servicios compartidos
builder.Services.AddScoped<ITenantResolver, TenantResolver>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Registrar servicios específicos (solo los que existen)
builder.Services.AddScoped<ITimeTrackingService, TimeTrackingService>();
// TODO: Implementar estos servicios gradualmente:
// builder.Services.AddScoped<IEmployeeService, EmployeeService>();
// builder.Services.AddScoped<IDepartmentService, DepartmentService>();
// builder.Services.AddScoped<IVacationService, VacationService>();

var app = builder.Build();

// Configuración del pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Company Admin API v1");
        c.RoutePrefix = string.Empty;
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName 
});

// Ejecutar migraciones automáticamente en desarrollo (SIMPLIFICADO para evitar errores)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
        
        // Solo verificar que la BD se puede conectar
        if (await context.Database.CanConnectAsync())
        {
            app.Logger.LogInformation("✅ Conexión a base de datos establecida");
        }
        else
        {
            app.Logger.LogWarning("⚠️ No se pudo conectar a la base de datos");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Error al conectar con la base de datos");
    }
}

app.Run();