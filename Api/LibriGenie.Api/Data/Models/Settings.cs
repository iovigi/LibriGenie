using MongoDB.Bson.Serialization.Attributes;

namespace LibriGenie.Api.Data.Models;

[BsonIgnoreExtraElements]
public class Settings
{
    [BsonElement("category")]
    public string Category { get; set; }
    [BsonElement("time")]
    public TimeSpan Time { get; set; }
    [BsonElement("enableWordpress")]
    public bool EnableWordpress { get; set; }
    [BsonElement("urlWordpress")]
    public string? UrlWordpress { get; set; }
    [BsonElement("usernameWordpress")]
    public string? UsernameWordpress { get; set; }
    [BsonElement("passwordWordpress")]
    public string? PasswordWordpress { get; set; }
    [BsonElement("enable")]
    public bool Enable { get; set; }
    [BsonElement("lastRun")]
    public DateTime? LastRun { get; set; }
    [BsonElement("symbols")]
    public List<string> Symbols { get; set; } = new List<string>();
    [BsonElement("primarySymbols")]
    public List<string> PrimarySymbols { get; set; } = new List<string>();
    [BsonElement("coinbaseName")]
    public string? CoinbaseName { get; set; }
    [BsonElement("coinbasePrivateKey")]
    public string? CoinbasePrivateKey { get; set; }
}
