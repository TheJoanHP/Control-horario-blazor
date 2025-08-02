using System.Net;
using System.Text.Json;

namespace Employee.App.Server.Middleware
{
    public class EmployeeErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EmployeeErrorHandlingMiddleware> _logger;

        public EmployeeErrorHandlingMiddleware(RequestDelegate next, ILogger<EmployeeErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no manejado en Employee App");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new EmployeeErrorResponse();

            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Datos de entrada no válidos";
                    response.UserFriendlyMessage = "Por favor verifica los datos ingresados";
                    break;

                case InvalidOperationException invOpEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = invOpEx.Message;
                    response.UserFriendlyMessage = invOpEx.Message;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "No autorizado";
                    response.UserFriendlyMessage = "No tienes permisos para realizar esta acción";
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Recurso no encontrado";
                    response.UserFriendlyMessage = "La información solicitada no fue encontrada";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "Error interno del servidor";
                    response.UserFriendlyMessage = "Ocurrió un error inesperado. Por favor intenta nuevamente";
                    break;
            }

            context.Response.StatusCode = response.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class EmployeeErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserFriendlyMessage { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? RequestId { get; set; }
    }
}