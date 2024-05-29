using LibriGenie.Workers.Configuration;
using LibriGenie.Workers.Services.ContentGenerate.Models;
using LibriGenie.Workers.Services.News;
using Microsoft.Extensions.Caching.Memory;
using OllamaClient;
using System.Text;
using System.Xml.Serialization;

namespace LibriGenie.Workers.Services.ContentGenerate;

public class ContentGenerator(IOllamaHttpClient ollamaHttpClient, INewClient newClient, IMemoryCache memoryCache, AppSettings appSettings) : IContentGenerator
{
    private const string END_PROMTH = "Generate Title and content. It should be xml with <GeneratedContent><title></title><content></content></GeneratedContent>, it should be valid xml";

    public async Task<GeneratedContent> Generate(string category, CancellationToken cancellationToken)
    {
        string promth = string.Empty;

        switch (category)
        {
            case "Joke":
                promth = GetJokePromt();
                break;
            case "Poem":
                promth = GetPoemPromt();
                break;
            case "Summarize":
                promth = await GetNewsPromt(cancellationToken);
                break;
        }

        var result = await ollamaHttpClient.SendChat(new OllamaClient.Models.ChatRequest()
        {
            Model = appSettings.OllamaSettings.Model,
            Messages = [ new OllamaClient.Models.Message()
            {
                Content = promth,
                Role = "user"
            } ]
        }, CancellationToken.None);

        return GetContent(result.Message!.Content);
    }

    private static string GetJokePromt()
    {
        return $"Please tell me a joke. {END_PROMTH}";
    }

    private static string GetPoemPromt()
    {
        return $"Please write me a poem.. {END_PROMTH}";
    }

    private async Task<string> GetNewsPromt(CancellationToken cancellationToken)
    {
        var result = await memoryCache.GetOrCreateAsync<string>("News", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var news = await newClient.GetNews(cancellationToken);
            entry.Value = news;

            StringBuilder sb = new StringBuilder();

            foreach(var item in news.Results)
            {
                if (!string.IsNullOrEmpty(item.Description))
                {
                    sb.Append(item.Description);
                    sb.Append(".");
                }
            }

            return sb.ToString();
        });

        return $"Summarize following news: {result}. {END_PROMTH}";
    }

    public GeneratedContent GetContent(string xml)
    {
        var xmlS = new XmlSerializer(typeof(GeneratedContent));

        using StringReader reader = new(xml);

        return (GeneratedContent)xmlS.Deserialize(reader)!;
    }
}
