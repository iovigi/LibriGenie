namespace LibriGenie.Api.Configuration;

public class AppSettings
{
    public MongoDBConfig MongoDBConfig { get; set; }
    public BasicAuthentication BasicAuthentication { get; set; }
    public hMail hMail { get; set; }

    public GmailSettings GmailSettings { get; set; }

    public string LogFile { get; set; } = string.Empty;
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

public class hMail
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string NoReplyEmail { get; set; }
    public string NoReplyPassword { get; set; }
}

public class GmailSettings
{
    public string Email { get; set; }

    public string Password { get; set; }
}
