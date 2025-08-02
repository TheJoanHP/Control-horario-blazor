using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Company.Admin.Server.Data;
using Company.Admin.Server.Services;
using Company.Admin.Server.Middleware;
using Employee.App.Server.Services;
using Employee.App.Server.Middleware;
using Shared.Services.Security;
using Shared.Services.Database;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Employee App API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme",
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

// Configuración de CORS para PWA
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEmployeeApp", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7003", // Employee.App.Client
                "http://localhost:5003"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configuración de Entity Framework (reutilizando CompanyDbContext)
builder.Services.AddDbContext<CompanyDbContext>((serviceProvider, options) =>
{
    var tenantResolver = serviceProvider.GetRequiredService<ITenantResolver>();
    var connectionString = tenantResolver.GetConnectionString();
    options.UseNpgsql(connectionString);
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => 
        policy.RequireRole("Employee", "Supervisor", "CompanyAdmin"));
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Servicios compartidos
builder.Services.AddScoped<ITenantResolver, TenantResolver>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Servicios reutilizados de Company.Admin.Server
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ITimeTrackingService, TimeTrackingService>();

// Servicios específicos para empleados
builder.Services.AddScoped<IEmployeeAppService, EmployeeAppService>();

var app = builder.Build();

// Configuración del pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee App API v1");
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

// Middleware personalizado para multi-tenant (específico para empleados)
app.UseMiddleware<EmployeeTenantMiddleware>();

app.UseCors("AllowEmployeeApp");

app.UseAuthentication();
app.UseAuthorization();

// Middleware de manejo de errores
app.UseMiddleware<EmployeeErrorHandlingMiddleware>();

app.MapControllers();

// Health check
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    App = "Employee App",
    Timestamp = DateTime.UtcNow 
});

// Endpoint para obtener información de la app (para PWA)
app.MapGet("/app-info", () => new
{
    Name = "Control Horario - Empleados",
    Version = "1.0.0",
    Description = "Aplicación para el control de horarios de empleados",
    SupportOffline = true
});

app.Run();