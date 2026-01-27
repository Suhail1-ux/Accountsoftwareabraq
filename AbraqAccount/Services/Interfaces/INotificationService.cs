namespace AbraqAccount.Services.Interfaces;

public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info
}

public interface INotificationService
{
    event Action<string, NotificationType>? OnShow;
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
}
