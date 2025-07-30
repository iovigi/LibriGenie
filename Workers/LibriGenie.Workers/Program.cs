using LibriGenie.Workers;
using LibriGenie.Workers.Configuration;
using LibriGenie.Workers.Services;
using LibriGenie.Workers.Services.Brevo;
using LibriGenie.Workers.Services.ContentGenerate;
using LibriGenie.Workers.Services.News;
using OllamaClient.Extensions;

const string SERVICE_NAME = "Libri Genie Worker!";

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton(builder.Configuration.Get<AppSettings>()!);
builder.Services.AddHttpClient<WordpressPublisher>();
builder.Services.AddHttpClient<LibriGenieClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<AppSettings>();
    client.DefaultRequestHeaders.Authorization = settings.ApiConfiguration.GetAuthenticationHeaderValue();
    client.BaseAddress = new Uri(settings.ApiConfiguration.Endpoint);
});

builder.Services.AddOllamaClient(x=> x.OllamaEndpoint = builder.Configuration.Get<AppSettings>()!.OllamaSettings.OllamaEndpoint);
builder.Services.AddSingleton<ILibriGenieClient, LibriGenieClient>();
builder.Services.AddSingleton<IWordpressPublisher, WordpressPublisher>();
builder.Services.AddSingleton<INewClient, NewClient>();
builder.Services.AddSingleton<IMailService, MailService>();
builder.Services.AddSingleton<IContentGenerator, ContentGenerator>();
builder.Services.AddSingleton<IDataClient, DataClient>();
builder.Services.AddHttpClient<ICryptoManager, CryptoManager>();
builder.Services.AddMemoryCache();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = SERVICE_NAME;
});


var host = builder.Build();
host.Run();
