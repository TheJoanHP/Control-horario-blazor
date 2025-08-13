using Microsoft.JSInterop;
using System.Text.Json;

namespace Sphere.Admin.Client.Services
{
    public class AuthenticationService
    {
        private readonly ApiService _apiService;
        private readonly IJSRuntime _jsRuntime;
        private const string TokenKey = "sphere_admin_token";
        private const string UserKey = "sphere_admin_user";

        public AuthenticationService(ApiService apiService, IJSRuntime jsRuntime)
        {
            _apiService = apiService;
            _jsRuntime = jsRuntime;
        }

        public event Action<bool>? AuthenticationStateChanged;

        public bool IsAuthenticated { get; private set; }
        public CurrentUser? CurrentUser { get; private set; }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var request = new LoginRequest(email, password);
                var response = await _apiService.LoginAsync(request);

                if (response != null)
                {
                    // Guardar token y usuario en localStorage
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, response.Token);
                    
                    var user = new CurrentUser(
                        response.Email,
                        response.FirstName,
                        response.LastName,
                        response.ExpiresAt);
                        
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserKey, JsonSerializer.Serialize(user));

                    // Configurar header de autorización
                    _apiService.SetAuthorizationHeader(response.Token);

                    // Actualizar estado
                    IsAuthenticated = true;
                    CurrentUser = user;
                    
                    AuthenticationStateChanged?.Invoke(true);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en login: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                // Llamar al endpoint de logout
                await _apiService.LogoutAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en logout: {ex.Message}");
            }
            finally
            {
                // Limpiar datos locales
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserKey);
                
                _apiService.ClearAuthorizationHeader();
                
                IsAuthenticated = false;
                CurrentUser = null;
                
                AuthenticationStateChanged?.Invoke(false);
            }
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                // Verificar si hay un token guardado
                var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
                var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserKey);

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
                {
                    var user = JsonSerializer.Deserialize<CurrentUser>(userJson);
                    
                    if (user != null && user.ExpiresAt > DateTime.UtcNow)
                    {
                        // Token válido
                        _apiService.SetAuthorizationHeader(token);
                        IsAuthenticated = true;
                        CurrentUser = user;
                        
                        AuthenticationStateChanged?.Invoke(true);
                        return true;
                    }
                    else
                    {
                        // Token expirado - limpiar
                        await LogoutAsync();
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inicializando autenticación: {ex.Message}");
                await LogoutAsync();
                return false;
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            // TODO: Implementar refresh token si es necesario
            // Por ahora, si el token expira, el usuario debe hacer login nuevamente
            await LogoutAsync();
            return false;
        }
    }

    public record CurrentUser(
        string Email,
        string FirstName,
        string LastName,
        DateTime ExpiresAt)
    {
        public string FullName => $"{FirstName} {LastName}";
        public string DisplayName => !string.IsNullOrEmpty(FullName.Trim()) ? FullName : Email;
    }
}