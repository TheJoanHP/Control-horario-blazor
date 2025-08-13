using System.Net.Http.Json;
using System.Text.Json;

namespace Sphere.Admin.Client.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        // Autenticación
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en login: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("api/auth/logout", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Tenants
        public async Task<List<TenantDto>?> GetTenantsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TenantDto>>("api/tenants", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo tenants: {ex.Message}");
                return null;
            }
        }

        public async Task<TenantDto?> GetTenantAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<TenantDto>($"api/tenants/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo tenant {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateTenantAsync(CreateTenantRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/tenants", request, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando tenant: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateTenantAsync(int id, UpdateTenantRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/tenants/{id}", request, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando tenant {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteTenantAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/tenants/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando tenant {id}: {ex.Message}");
                return false;
            }
        }

        // Dashboard Statistics
        public async Task<DashboardStats?> GetDashboardStatsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<DashboardStats>("api/dashboard/stats", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo estadísticas: {ex.Message}");
                return null;
            }
        }

        // Sistema
        public async Task<List<SystemConfigDto>?> GetSystemConfigsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<SystemConfigDto>>("api/system/configs", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo configuraciones: {ex.Message}");
                return null;
            }
        }

        // Configurar token de autorización
        public void SetAuthorizationHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearAuthorizationHeader()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    // DTOs para la comunicación con la API
    public record LoginRequest(string Email, string Password);
    
    public record LoginResponse(
        string Token, 
        string Email, 
        string FirstName, 
        string LastName, 
        DateTime ExpiresAt);

    public record TenantDto(
        int Id,
        string Code,
        string Name,
        string ContactEmail,
        string LicenseType,
        int MaxEmployees,
        bool Active,
        DateTime CreatedAt,
        DateTime? ExpiresAt);

    public record CreateTenantRequest(
        string Code,
        string Name,
        string ContactEmail,
        string ContactPhone,
        string LicenseType,
        int MaxEmployees);

    public record UpdateTenantRequest(
        string Name,
        string ContactEmail,
        string ContactPhone,
        string LicenseType,
        int MaxEmployees,
        bool Active);

    public record DashboardStats(
        int TotalTenants,
        int ActiveTenants,
        int TotalUsers,
        decimal MonthlyRevenue,
        int TrialTenants);

    public record SystemConfigDto(
        int Id,
        string Key,
        string Value,
        string Category,
        string? Description);
}