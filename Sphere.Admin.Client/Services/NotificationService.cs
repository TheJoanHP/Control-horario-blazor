// Ruta: Sphere.Admin.Client/Services/NotificationService.cs
using MudBlazor;

namespace Sphere.Admin.Client.Services
{
    public class NotificationService
    {
        private readonly ISnackbar _snackbar;

        public NotificationService(ISnackbar snackbar)
        {
            _snackbar = snackbar;
        }

        public void ShowSuccess(string message)
        {
            _snackbar.Add(message, Severity.Success, config =>
            {
                config.ShowCloseIcon = true;
                config.VisibleStateDuration = 5000;
            });
        }

        public void ShowError(string message)
        {
            _snackbar.Add(message, Severity.Error, config =>
            {
                config.ShowCloseIcon = true;
                config.VisibleStateDuration = 7000;
            });
        }

        public void ShowWarning(string message)
        {
            _snackbar.Add(message, Severity.Warning, config =>
            {
                config.ShowCloseIcon = true;
                config.VisibleStateDuration = 6000;
            });
        }

        public void ShowInfo(string message)
        {
            _snackbar.Add(message, Severity.Info, config =>
            {
                config.ShowCloseIcon = true;
                config.VisibleStateDuration = 5000;
            });
        }

        public void ShowNotification(string message, Severity severity = Severity.Normal, int duration = 5000)
        {
            _snackbar.Add(message, severity, config =>
            {
                config.ShowCloseIcon = true;
                config.VisibleStateDuration = duration;
            });
        }

        public void ShowPersistent(string message, Severity severity = Severity.Normal)
        {
            _snackbar.Add(message, severity, config =>
            {
                config.ShowCloseIcon = true;
                config.RequireInteraction = true;
            });
        }

        public void Clear()
        {
            _snackbar.Clear();
        }
    }
}