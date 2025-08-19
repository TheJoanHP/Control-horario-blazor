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
                config.Icon = Icons.Material.Filled.CheckCircle;
                config.VisibleStateDuration = 4000;
            });
        }

        public void ShowError(string message)
        {
            _snackbar.Add(message, Severity.Error, config =>
            {
                config.Icon = Icons.Material.Filled.Error;
                config.VisibleStateDuration = 6000;
            });
        }

        public void ShowWarning(string message)
        {
            _snackbar.Add(message, Severity.Warning, config =>
            {
                config.Icon = Icons.Material.Filled.Warning;
                config.VisibleStateDuration = 5000;
            });
        }

        public void ShowInfo(string message)
        {
            _snackbar.Add(message, Severity.Info, config =>
            {
                config.Icon = Icons.Material.Filled.Info;
                config.VisibleStateDuration = 4000;
            });
        }

        public void ShowActionRequired(string message, string actionText = "Acción", Func<Task>? action = null)
        {
            _snackbar.Add(message, Severity.Normal, config =>
            {
                config.Icon = Icons.Material.Filled.NotificationImportant;
                config.Action = actionText;
                config.ActionColor = Color.Primary;
                config.OnClick = action != null ? _ => action() : null;
                config.VisibleStateDuration = 8000;
            });
        }

        public void ShowLoading(string message)
        {
            _snackbar.Add(message, Severity.Info, config =>
            {
                config.Icon = Icons.Material.Filled.Sync;
                config.VisibleStateDuration = 2000;
            });
        }

        public void Clear()
        {
            _snackbar.Clear();
        }
    }
}