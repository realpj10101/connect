namespace api.Settings;

public interface IMyMongoDbSettings
{
    public string? ConnectionString { get; init; }
    public string? DatabaseName { get; init; }   
}