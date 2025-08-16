using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Shared.Models.DTOs.Auth;
using static Sphere.Admin.Client.Services.ApiService;

namespace Sphere.Admin.Client.Services
{
    public class AuthService
    {
        private readonly ApiService _apiService;
        private readonly ILocalStorageService _localStorage;
        private readonly NavigationManager _navigation;

        private const string TOKEN_KEY = "sphere_auth_token";
        private const string USER_KEY = "sphere_user_info";

        public AuthService(
            ApiService apiService, 
            ILocalStorageService localStorage,
            NavigationManager navigation)
        {
            _apiService = apiService;
            _localStorage = localStorage;
            _navigation = navigation;
        }

        public event Action<bool>? AuthStateChanged;

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var request = new LoginRequest(email, password);
                var response = await _apiService.LoginAsync(request);

                if (response != null)
                {
                    // Guardar token y info del usuario
                    await _localStorage.SetItemAsync(TOKEN_KEY, response.Token);
                    
                    // Crear UserInfo desde la respuesta
                    var userInfo = new UserInfo
                    {
                        Email = response.Email,
                        FirstName = response.FirstName,
                        LastName = response.LastName,
                        Active = true
                    };
                    
                    await _localStorage.SetItemAsync(USER_KEY, userInfo);

                    // Configurar header de autorización
                    _apiService.SetAuthorizationHeader(response.Token);

                    // Notificar cambio de estado
                    AuthStateChanged?.Invoke(true);

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
                // Limpiar almacenamiento local
                await _localStorage.RemoveItemAsync(TOKEN_KEY);
                await _localStorage.RemoveItemAsync(USER_KEY);

                // Limpiar header de autorización
                _apiService.ClearAuthorizationHeader();

                // Notificar cambio de estado
                AuthStateChanged?.Invoke(false);

                // Redirigir al login
                _navigation.NavigateTo("/login", replace: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en logout: {ex.Message}");
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>(TOKEN_KEY);
                
                if (string.IsNullOrEmpty(token))
                    return false;

                // Configurar header si hay token válido
                _apiService.SetAuthorizationHeader(token);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verificando autenticación: {ex.Message}");
                return false;
            }
        }

        public async Task<UserInfo?> GetUserInfoAsync()
        {
            try
            {
                var userInfo = await _localStorage.GetItemAsync<UserInfo>(USER_KEY);
                return userInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo info del usuario: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await _localStorage.GetItemAsync<string>(TOKEN_KEY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo token: {ex.Message}");
                return null;
            }
        }
    }

    // CLASE UserInfo ELIMINADA - Ahora usa Shared.Models.DTOs.Auth.UserInfo
}