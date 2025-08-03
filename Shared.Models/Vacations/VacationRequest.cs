using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Company.Admin.Server.Data;
using Company.Admin.Server.Services;
using Company.Admin.Server.Mappings;
using Shared.Services.Security;
using Shared.Services.Tenant;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la base de datos
builder.Services.AddDbContext<CompanyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Agregar servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCompanyAdmin", policy =>
    {
        policy.WithOrigins("https://localhost:5002", "http://localhost:5002")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configuración de autenticación JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey no está configurada");

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

// Configuración de autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CompanyAdmin", policy => 
        policy.RequireRole("CompanyAdmin", "Supervisor"));
    options.AddPolicy("Supervisor", policy => 
        policy.RequireRole("CompanyAdmin", "Supervisor"));
    options.AddPolicy("Employee", policy => 
        policy.RequireRole("Employee", "Supervisor", "CompanyAdmin"));
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Company Admin API",
        Version = "v1",
        Description = "API para la administración de empleados y control horario"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header usando el esquema Bearer. 
                      Introduce 'Bearer' [espacio] y luego tu token en el campo de texto a continuación.
                      Ejemplo: 'Bearer 12345abcdef'",
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

// Configuración del pipeline de la aplicación
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

// CORS debe ir antes de Authentication y Authorization
app.UseCors("AllowCompanyAdmin");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new 
{ 
    Status = "Healthy", 
    Service = "Company Admin API",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0"
});

// Endpoint de información de la API
app.MapGet("/api/info", () => new
{
    Name = "Company Admin API",
    Version = "1.0.0",
    Description = "API para la administración de empleados y control horario",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
});

// Inicializar la base de datos en desarrollo
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
        try
        {
            await context.Database.EnsureCreatedAsync();
            app.Logger.LogInformation("Base de datos inicializada correctamente");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error al inicializar la base de datos");
        }
    }
}

app.Run();