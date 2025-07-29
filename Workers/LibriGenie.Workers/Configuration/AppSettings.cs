using System.Net.Http.Headers;
using System.Text;

namespace LibriGenie.Workers.Configuration;

public class AppSettings
{
    public OllamaSettings OllamaSettings { get; set; } = new OllamaSettings();
    public ApiConfiguration ApiConfiguration { get; set; } = new ApiConfiguration();
    public string NewUrl { get; set; } = string.Empty;
    public MailSettings MailSettings { get; set; } = new MailSettings();
    public DataClientSettings DataClientSettings { get; set; } = new DataClientSettings();
}

public class OllamaSettings
{
    public string OllamaEndpoint { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}

public class ApiConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public AuthenticationHeaderValue GetAuthenticationHeaderValue()
    {
        var stringBuffer = Encoding.UTF8.GetBytes($"{Username}:{Password}");
        var base64 = Convert.ToBase64String(stringBuffer);

        return new AuthenticationHeaderValue("Basic", base64);
    }
}

public class MailSettings
{
    public string BrevoApiKey { get; set; }
    public string NoReplyEmail { get; set; }
}

public class DataClientSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SystemAuthKey { get; set; } = string.Empty;
}
