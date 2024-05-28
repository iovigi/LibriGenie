using LibriGenie.Api.Authentication;
using LibriGenie.Api.Configuration;
using LibriGenie.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
    {
        Description = "Basic auth added to authorization header",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "basic",
        Type = SecuritySchemeType.Http
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Basic" }
                    },
                    new List<string>()
                }
            });
});

builder.Services.Configure<MongoDBConfig>(builder.Configuration.GetSection(nameof(MongoDBConfig)));
builder.Services.AddSingleton<AppSettings>(builder.Configuration.Get<AppSettings>()!);

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var config = sp.GetService<IOptions<MongoDBConfig>>();
    var settings = MongoClientSettings.FromConnectionString(config!.Value.ConnectionString);

    return new MongoClient(settings);
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetService<IOptions<MongoDBConfig>>()!;
    var mongoClient = sp.GetRequiredService<IMongoClient>();

    return mongoClient.GetDatabase(config.Value.DatabaseName);
});

builder.Services.AddScoped<ITaskService, TaskService>();

builder.Services.AddAuthentication("Basic")
.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
