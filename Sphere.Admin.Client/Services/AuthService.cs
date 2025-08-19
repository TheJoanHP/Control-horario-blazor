using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Shared.Models.DTOs.Auth;
using System.IdentityModel.Tokens.Jwt;

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
            catch (Exception)
            {
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
            }
            catch (Exception)
            {
                // En caso de error, forzar limpieza
                await _localStorage.ClearAsync();
                _apiService.ClearAuthorizationHeader();
                AuthStateChanged?.Invoke(false);
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>(TOKEN_KEY);
                
                if (string.IsNullOrEmpty(token))
                    return false;

                // Verificar si el token ha expirado
                if (IsTokenExpired(token))
                {
                    await LogoutAsync();
                    return false;
                }

                // Configurar header de autorización si el token es válido
                _apiService.SetAuthorizationHeader(token);
                return true;
            }
            catch (Exception)
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
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await _localStorage.GetItemAsync<string>(TOKEN_KEY);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task UpdateUserInfoAsync(UserInfo userInfo)
        {
            try
            {
                await _localStorage.SetItemAsync(USER_KEY, userInfo);
            }
            catch (Exception)
            {
                // Log error but don't throw
            }
        }

        private bool IsTokenExpired(string token)
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                return jwt.ValidTo <= DateTime.UtcNow;
            }
            catch (Exception)
            {
                // Si no se puede leer el token, asumir que ha expirado
                return true;
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var currentToken = await GetTokenAsync();
                if (string.IsNullOrEmpty(currentToken))
                    return false;

                // TODO: Implementar refresh token con la API
                // var refreshResponse = await _apiService.RefreshTokenAsync(currentToken);
                
                // Por ahora, solo verificamos si el token actual sigue siendo válido
                return !IsTokenExpired(currentToken);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                // TODO: Implementar cambio de contraseña con la API
                // var request = new ChangePasswordRequest(currentPassword, newPassword);
                // var success = await _apiService.ChangePasswordAsync(request);
                
                // Por ahora retornamos true simulando éxito
                await Task.Delay(1000);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            try
            {
                // TODO: Implementar solicitud de reset de contraseña
                // var request = new PasswordResetRequest(email);
                // var success = await _apiService.RequestPasswordResetAsync(request);
                
                await Task.Delay(1000);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}