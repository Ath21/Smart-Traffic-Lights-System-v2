namespace LogData.Settings;

public class LogDbSettings
{
    public string? ConnectionString { get; set; } 
    public string? Database { get; set; } 
    public CollectionsSettings? Collections { get; set; } 
}
