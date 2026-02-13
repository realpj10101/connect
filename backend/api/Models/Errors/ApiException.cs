using MongoDB.Bson;

namespace api.Models.Errors;

public class ApiException
{
    public ObjectId Id { get; init; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public string? Details { get; set; }
    public DateTime Time { get; set; }   
}