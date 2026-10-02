namespace TrafficLightCacheData.Settings;

public class TrafficLightCacheDbSettings
{
    public string? Host { get; set; } 
    public int Port { get; set; } 
    public string? Password { get; set; } 
    public int Database { get; set; } 

    public KeyPrefixSettings? KeyPrefix { get; set; }
}
