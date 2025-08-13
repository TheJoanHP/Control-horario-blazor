using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Sphere.Admin.Client;
using Sphere.Admin.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configurar HttpClient para comunicación con la API
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("https://localhost:7051") // URL del Sphere.Admin.Server
});

// Servicios de MudBlazor
builder.Services.AddMudServices();

// Servicios personalizados
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AuthenticationService>();

// Configuración de logging
builder.Logging.SetMinimumLevel(LogLevel.Information);

await builder.Build().RunAsync();