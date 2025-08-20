// Ruta: Sphere.Admin.Client/Services/ApiService.cs
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Models.Core;
using Shared.Models.DTOs.Auth;

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
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ========== AUTENTICACIÓN ==========
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request, _jsonOptions);
                
                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                    
                    // Configurar el token en los headers para futuras peticiones
                    if (loginResponse?.Token != null)
                    {
                        SetAuthorizationHeader(loginResponse.Token);
                    }
                    
                    return loginResponse;
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
                ClearAuthorizationHeader();
                return response.IsSuccessStatusCode;
            }
            catch
            {
                ClearAuthorizationHeader();
                return false;
            }
        }

        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UserInfo>("api/auth/me", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo usuario actual: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cambiando contraseña: {ex.Message}");
                return false;
            }
        }

        // ========== TENANTS ==========
        public async Task<List<Tenant>?> GetTenantsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Tenant>>("api/tenants", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo tenants: {ex.Message}");
                return null;
            }
        }

        public async Task<Tenant?> GetTenantAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Tenant>($"api/tenants/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo tenant {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<Tenant?> CreateTenantAsync(Tenant tenant)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/tenants", tenant, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Tenant>(_jsonOptions);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando tenant: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateTenantAsync(int id, Tenant tenant)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/tenants/{id}", tenant, _jsonOptions);
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

        public async Task<bool> ToggleTenantStatusAsync(int id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/tenants/{id}/toggle-status", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cambiando estado del tenant {id}: {ex.Message}");
                return false;
            }
        }

        // ========== LICENCIAS ==========
        public async Task<List<License>?> GetLicensesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<License>>("api/licenses", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo licencias: {ex.Message}");
                return null;
            }
        }

        public async Task<License?> GetLicenseAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<License>($"api/licenses/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo licencia {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<License?> GetLicenseByTenantAsync(int tenantId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<License>($"api/licenses/tenant/{tenantId}", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo licencia del tenant {tenantId}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateLicenseAsync(License license)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/licenses", license, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando licencia: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateLicenseAsync(int id, License license)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/licenses/{id}", license, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando licencia {id}: {ex.Message}");
                return false;
            }
        }

        // ========== DASHBOARD / ESTADÍSTICAS ==========
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

        public async Task<List<ChartData>?> GetRevenueChartDataAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ChartData>>("api/dashboard/revenue-chart", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo datos de ingresos: {ex.Message}");
                return null;
            }
        }

        public async Task<List<ActivityLog>?> GetRecentActivityAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ActivityLog>>("api/dashboard/recent-activity", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo actividad reciente: {ex.Message}");
                return null;
            }
        }

        // ========== SISTEMA ==========
        public async Task<List<SystemConfig>?> GetSystemConfigsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<SystemConfig>>("api/system/configs", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo configuraciones: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateSystemConfigAsync(int id, SystemConfig config)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/system/configs/{id}", config, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando configuración {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<SystemLog>?> GetSystemLogsAsync(int page = 1, int pageSize = 50)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<SystemLog>>(
                    $"api/system/logs?page={page}&pageSize={pageSize}", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo logs: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Backup>?> GetBackupsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Backup>>("api/system/backups", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo backups: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateBackupAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("api/system/backups/create", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando backup: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestoreBackupAsync(int backupId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/system/backups/{backupId}/restore", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restaurando backup {backupId}: {ex.Message}");
                return false;
            }
        }

        // ========== UTILIDADES ==========
        public void SetAuthorizationHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearAuthorizationHeader()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        // ========== MÉTODOS GENÉRICOS ==========
        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<T>(endpoint, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GET {endpoint}: {ex.Message}");
                return default;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, request, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                }
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en POST {endpoint}: {ex.Message}");
                return default;
            }
        }

        public async Task<bool> PutAsync<T>(string endpoint, T request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(endpoint, request, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en PUT {endpoint}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en DELETE {endpoint}: {ex.Message}");
                return false;
            }
        }

        public async Task<byte[]?> GetBytesAsync(string endpoint)
        {
            try
            {
                return await _httpClient.GetByteArrayAsync(endpoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo bytes de {endpoint}: {ex.Message}");
                return null;
            }
        }
    }

    // ========== DTOs ADICIONALES ==========
    public class DashboardStats
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal AnnualRevenue { get; set; }
        public int TrialTenants { get; set; }
        public int PaidTenants { get; set; }
        public double GrowthRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ChartData
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string? Color { get; set; }
        public Dictionary<string, object>? Additional { get; set; }
    }

    public class ActivityLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class SystemLog
    {
        public int Id { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Source { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Exception { get; set; }
        public Dictionary<string, string>? Properties { get; set; }
    }

    public class Backup
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}