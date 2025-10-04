using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace LibriGenie.Api.Data.Models;

[BsonIgnoreExtraElements]
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    [BsonElement("email")]
    public string Email { get; set; }

    [BsonElement("settings")]
    public List<Settings> Settings { get; set; } = new List<Settings>();
}
