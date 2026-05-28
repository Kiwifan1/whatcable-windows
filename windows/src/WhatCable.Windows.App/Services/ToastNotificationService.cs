using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using WhatCable.Windows.App.Core.Services;

namespace WhatCable.Windows.App.Services;

/// <summary>
/// <see cref="INotificationService"/> over <c>AppNotificationManager</c> (Windows App SDK toasts),
/// used for connect/disconnect notifications.
/// </summary>
public sealed class ToastNotificationService : INotificationService
{
    public void Notify(string title, string message)
    {
        var notification = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message)
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }
}
