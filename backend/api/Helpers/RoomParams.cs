using System.ComponentModel.DataAnnotations;
using api.Enums;
using MongoDB.Bson;

namespace api.Helpers;

public class RoomParams : PaginationParams
{
    public ObjectId UserId { get; set; }

    public OrderByEnum OrderBy { get; set; } = OrderByEnum.All;

    [MaxLength(100)]
    public string? Search { get; set; }
}