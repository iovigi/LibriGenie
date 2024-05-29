using System.Net.Http.Headers;
using System.Text;

namespace LibriGenie.Workers.Services;

public class WordpressPublisher(IHttpClientFactory factory) : IWordpressPublisher
{
    private const string URL_TEMPLATE = "wp-json/wp/v2/posts?content={0}&title={1}";

    public async System.Threading.Tasks.Task Publish(Models.Task task, string title, string content, CancellationToken cancellationToken)
    {
        try
        {
            if (!task.EnableWordpress)
            {
                return;
            }

            string endSlash = "/";
            var template = string.Format(URL_TEMPLATE, content, title);
            string url = string.Empty;
            if (task.UrlWordpress!.EndsWith(endSlash))
            {
                url = task.UrlWordpress + template;
            }
            else
            {
                url = task.UrlWordpress + endSlash + template;
            }

            var httpClient = factory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = GetAuthenticationHeaderValue(task.UsernameWordpress!, task.PasswordWordpress!);
            await httpClient.PostAsync(url, null, cancellationToken);
        }
        catch
        {
        }
    }

    public AuthenticationHeaderValue GetAuthenticationHeaderValue(string username, string password)
    {
        var stringBuffer = Encoding.UTF8.GetBytes($"{username}:{password}");
        var base64 = Convert.ToBase64String(stringBuffer);

        return new AuthenticationHeaderValue("Basic", base64);
    }
}
