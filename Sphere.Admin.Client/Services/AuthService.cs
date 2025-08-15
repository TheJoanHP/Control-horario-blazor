using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
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
                    await _localStorage.SetItemAsync(USER_KEY, new UserInfo(
                        response.Email,
                        response.FirstName,
                        response.LastName,
                        response.ExpiresAt
                    ));

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

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>(TOKEN_KEY);
                var userInfo = await _localStorage.GetItemAsync<UserInfo>(USER_KEY);

                if (string.IsNullOrEmpty(token) || userInfo == null)
                    return false;

                // Verificar si el token ha expirado
                if (DateTime.UtcNow >= userInfo.ExpiresAt)
                {
                    await LogoutAsync();
                    return false;
                }

                // Configurar header de autorización
                _apiService.SetAuthorizationHeader(token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<UserInfo?> GetUserInfoAsync()
        {
            try
            {
                return await _localStorage.GetItemAsync<UserInfo>(USER_KEY);
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
                // Limpiar almacenamiento local
                await _localStorage.RemoveItemAsync(TOKEN_KEY);
                await _localStorage.RemoveItemAsync(USER_KEY);

                // Limpiar header de autorización
                _apiService.ClearAuthorizationHeader();

                // Notificar cambio de estado
                AuthStateChanged?.Invoke(false);

                // Navegar al login
                _navigation.NavigateTo("/login", forceLoad: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en logout: {ex.Message}");
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await _localStorage.GetItemAsync<string>(TOKEN_KEY);
            }
            catch
            {
                return null;
            }
        }
    }

    public record UserInfo(
        string Email,
        string FirstName,
        string LastName,
        DateTime ExpiresAt
    );
}