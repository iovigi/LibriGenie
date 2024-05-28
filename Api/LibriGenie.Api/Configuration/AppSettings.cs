namespace LibriGenie.Api.Configuration;

public class AppSettings
{
    public MongoDBConfig MongoDBConfig { get; set; }
    public BasicAuthentication BasicAuthentication { get; set; }
}

public class MongoDBConfig
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
}

public class BasicAuthentication
{
    public string Username { get; set; }
    public string Password { get; set; }
}
