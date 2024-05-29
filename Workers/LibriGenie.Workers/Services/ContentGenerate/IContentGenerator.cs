using LibriGenie.Workers.Services.ContentGenerate.Models;

namespace LibriGenie.Workers.Services.ContentGenerate;

public interface IContentGenerator
{
    Task<GeneratedContent> Generate(string category, CancellationToken cancellationToken);
}
