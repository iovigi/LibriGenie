using LibriGenie.Workers.Configuration;
using LibriGenie.Workers.Services.ContentGenerate.Models;
using LibriGenie.Workers.Services.News;
using Microsoft.Extensions.Caching.Memory;
using OllamaClient;
using System.Text;
using System.Xml.Serialization;

namespace LibriGenie.Workers.Services.ContentGenerate;

public class ContentGenerator(IOllamaHttpClient ollamaHttpClient, INewClient newClient, IMemoryCache memoryCache, AppSettings appSettings, ILogger<ContentGenerator> logger) : IContentGenerator
{
    private const string END_PROMTH = "Generate Title and content. It should be xml with <GeneratedContent><title></title><content></content></GeneratedContent>, it should be valid xml";

    public async Task<GeneratedContent> Generate(string category, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting content generation for category: {category}", category);
        
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

        logger.LogDebug("Generated prompt for category {category}: {prompt}", category, promth);

        try
        {
            var result = await ollamaHttpClient.SendChat(new OllamaClient.Models.ChatRequest()
            {
                Model = appSettings.OllamaSettings.Model,
                Messages = [ new OllamaClient.Models.Message()
                {
                    Content = promth,
                    Role = "user"
                } ]
            }, CancellationToken.None);

            logger.LogInformation("Successfully generated content for category: {category}", category);
            return GetContent(result.Message!.Content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate content for category {category}: {message}", category, ex.Message);
            throw;
        }
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
        logger.LogDebug("Parsing XML content: {xml}", xml);
        
        try
        {
            var xmlS = new XmlSerializer(typeof(GeneratedContent));

            using StringReader reader = new(xml);

            var result = (GeneratedContent)xmlS.Deserialize(reader)!;
            logger.LogDebug("Successfully parsed XML content with title: {title}", result.Title);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse XML content: {message}", ex.Message);
            throw;
        }
    }
}
