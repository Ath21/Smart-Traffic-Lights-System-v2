using System;

namespace DetectionCacheData.Settings;

public class DetectionCacheDbSettings
{
    public string? Host { get; set; } 
    public int Port { get; set; } 
    public string? Password { get; set; } 
    public int Database { get; set; } 

    public KeyPrefixSettings? KeyPrefix { get; set; }
}