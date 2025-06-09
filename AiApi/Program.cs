using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory()
});

// Swagger & API versioning
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

builder.Configuration.AddEnvironmentVariables(); 

var key = builder.Configuration["OpenAIKey"];
var model = builder.Configuration["ModelName"];

if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(key))
{
    throw new InvalidOperationException("OpenAIKey or ModelName is not configured in user secrets.");
}

// Set up OpenAI client
IChatClient chatClient = new OpenAIClient(key)
    .GetChatClient(model)
    .AsIChatClient();

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI();


// System prompt (AI personality)
var systemMessage = new ChatMessage(ChatRole.System, """
    You are a friendly hiking enthusiast who helps people discover fun hikes in their area.
    You introduce yourself when first saying hello.
    When helping people out, you always ask them for this information
    to inform the hiking recommendation you provide:

    1. The location where they would like to hike
    2. What hiking intensity they are looking for
""");

// AI chat endpoint
app.MapPost("/api/v1/chat", async (ChatPromptRequest req) =>
{
    var messages = new List<ChatMessage>
    {
        systemMessage,
        new ChatMessage(ChatRole.User, req.Prompt)
    };

    string responseText = "";
    await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
    {
        responseText += update.Text;
    }

    return Results.Ok(new ChatPromptResponse(responseText));
})
.WithName("PostChat")
.WithOpenApi()
.WithTags("AI Chat");

app.Run();

// === DTOs ===
record ChatPromptRequest(string Prompt);
record ChatPromptResponse(string Response);
