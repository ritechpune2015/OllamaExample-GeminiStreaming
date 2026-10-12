using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OllamaExample.Entities;
using OllamaExample.Options;
using OllamaExample.Repositories;
using OllamaExample.Services;
using OllamaExample.Services.Context;
using OllamaExample.Services.Conversation;
using OllamaExample.Services.Gemini;
using OllamaExample.Services.Memory;
using OllamaExample.Services.Ollama;
using OllamaExample.Services.OpenAI;

var builder =
    WebApplication.CreateBuilder(args);

//builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==================================================
// DATABASE
// ==================================================

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration
                .GetConnectionString("scon")));

// ==================================================
// OPENAI
// ==================================================

builder.Services.Configure<OpenAIOptions>(
    builder.Configuration
        .GetSection("OpenAI"));

// Gemini configuration
builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection("Gemini"));

builder.Services.AddSingleton<IStreamingLLMService>(sp => sp.GetRequiredService<GeminiLLMService>());

// Select provider
var provider = builder.Configuration["LLM:Provider"];

builder.Services.AddSingleton<IStreamingLLMService>(sp => sp.GetRequiredService<GeminiLLMService>());

if (string.Equals(
        provider,
        "OpenAI",
        StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<OpenAILLMService>(sp =>
    {
        var options = sp
            .GetRequiredService<IOptions<OpenAIOptions>>()
            .Value;

        return new OpenAILLMService(options);
    });

    builder.Services.AddSingleton<ILLMService>(
        sp => sp.GetRequiredService<OpenAILLMService>());

    builder.Services.AddSingleton<IStreamingLLMService>(
        sp => sp.GetRequiredService<OpenAILLMService>());
}
else if (string.Equals(
    provider,
    "Gemini",
    StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<GeminiLLMService>(sp =>
    {
        var options = sp
            .GetRequiredService<IOptions<GeminiOptions>>()
            .Value;

        return new GeminiLLMService(options);
    });

    builder.Services.AddSingleton<ILLMService>(
        sp => sp.GetRequiredService<GeminiLLMService>());

    builder.Services.AddSingleton<IStreamingLLMService>(
        sp => sp.GetRequiredService<GeminiLLMService>());
}
else if (string.Equals(
             provider,
             "Ollama",
             StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<ILLMService>(sp =>
    {
        var options = sp
            .GetRequiredService<IOptions<OllamaOptions>>()
            .Value;

        return new OllamaLLMService(new HttpClient(),options);
    });
}
else
{
    throw new InvalidOperationException(
        $"Unsupported LLM provider '{provider}'. " +
        "Set LLM:Provider to OpenAI or Gemini.");
}

// ==================================================
// CONVERSATION
// ==================================================

builder.Services.AddScoped<
    IConversationRepository,
    ConversationRepository>();

builder.Services.AddScoped<
    IConversationService,
    ConversationService>();

builder.Services.AddScoped<
    IContextBuilder,
    ContextBuilder>();

builder.Services.AddScoped<
    IConversationSummaryService,
    ConversationSummaryService>();

// ==================================================
// MEMORY
// ==================================================

// SQL Server persistent memory
builder.Services.AddScoped<
    IMemoryStore,
    SqlMemoryStore>();

builder.Services.AddScoped<
    IMemoryService,
    MemoryService>();

builder.Services.AddScoped<
    IMemoryContextBuilder,
    MemoryContextBuilder>();

builder.Services.AddScoped<
    IMemoryExtractionService,
    MemoryExtractionService>();

// ==================================================
// APPLICATION
// ==================================================

var app =
    builder.Build();

app.UseSwagger();

app.UseSwaggerUI();

app.MapDefaultControllerRoute();

app.MapControllers();

app.Run();
