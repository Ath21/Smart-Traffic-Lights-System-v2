using System;

namespace NotificationData.Settings;

public class NotificationDbSettings
{
    public string? ConnectionString { get; set; } 
    public string? Database { get; set; }

    public CollectionsSettings? Collections { get; set; } 
}