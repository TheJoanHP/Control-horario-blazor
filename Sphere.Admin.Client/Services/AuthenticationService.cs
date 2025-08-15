using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Sphere.Admin.Client.Services;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "sphere_admin_token";
    private const string UserKey = "sphere_admin_user";

    public AuthenticationService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        try
        {
            var loginRequest = new AuthLoginRequest
            {
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<AuthLoginResponse>();
                
                if (loginResponse?.Success == true && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    // Guardar token y usuario en localStorage
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, loginResponse.Token);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserKey, JsonSerializer.Serialize(loginResponse.User));
                    
                    // Configurar header de autenticación
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.Token);
                    
                    return new LoginResult { Success = true };
                }
            }
            
            return new LoginResult 
            { 
                Success = false, 
                ErrorMessage = "Credenciales inválidas" 
            };
        }
        catch (Exception ex)
        {
            return new LoginResult 
            { 
                Success = false, 
                ErrorMessage = "Error de conexión con el servidor" 
            };
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            
            if (string.IsNullOrEmpty(token))
                return false;
            
            // Verificar si el token es válido
            return !IsTokenExpired(token);
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var userJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", UserKey);
            
            if (string.IsNullOrEmpty(userJson))
                return null;
            
            return JsonSerializer.Deserialize<UserInfo>(userJson);
        }
        catch
        {
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Llamar al endpoint de logout si existe
            await _httpClient.PostAsync("api/auth/logout", null);
        }
        catch
        {
            // Ignorar errores del logout del servidor
        }
        finally
        {
            // Limpiar datos locales
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserKey);
            
            // Limpiar header de autenticación
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            
            if (!string.IsNullOrEmpty(token) && !IsTokenExpired(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // Ignorar errores de inicialización
        }
    }

    private bool IsTokenExpired(string token)
    {
        try
        {
            // Decodificar JWT para verificar expiración
            var parts = token.Split('.');
            if (parts.Length != 3)
                return true;

            var payload = parts[1];
            // Agregar padding si es necesario
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Convert.FromBase64String(payload);
            var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (claims?.ContainsKey("exp") == true)
            {
                var exp = Convert.ToInt64(claims["exp"]);
                var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
                return expDate <= DateTimeOffset.UtcNow;
            }
            
            return true;
        }
        catch
        {
            return true;
        }
    }
}

// DTOs para la autenticación
public class AuthLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthLoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public UserInfo? User { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}