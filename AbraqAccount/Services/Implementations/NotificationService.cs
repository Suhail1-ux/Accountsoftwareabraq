using AbraqAccount.Services.Interfaces;

namespace AbraqAccount.Services.Implementations;

public class NotificationService : INotificationService
{
    public event Action<string, NotificationType>? OnShow;

    public void ShowSuccess(string message) => OnShow?.Invoke(message, NotificationType.Success);
    public void ShowError(string message) => OnShow?.Invoke(message, NotificationType.Error);
    public void ShowWarning(string message) => OnShow?.Invoke(message, NotificationType.Warning);
    public void ShowInfo(string message) => OnShow?.Invoke(message, NotificationType.Info);
}
