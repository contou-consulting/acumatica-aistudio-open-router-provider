using System;
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using PX.AIStudio.Services.Core.ChatClients;
using PX.AIStudio.Services.Core.Connection;

namespace LS.AIStudio.OpenRouterProvider.ChatClients
{
    /// <summary>
    /// Direct AI Automation provider for openrouter.ai. OpenRouter speaks the OpenAI chat
    /// completions wire format, so the stock OpenAI client is pointed at its endpoint.
    /// Listed on ML201000 only when web.config has ai-studio:EnableDirectProviders=true.
    /// </summary>
    public class LSAIOpenRouterChatClientFactory : IChatClientFactory
    {
        public const string ProviderId = "direct-openrouter";

        private const string ApiKey = "ApiKey";
        private const string Model = "Model";

        private static readonly Uri Endpoint = new Uri("https://openrouter.ai/api/v1");

        public static ConnectionMetadata Metadata { get; } = new ConnectionMetadata(
            typeof(LSAIOpenRouterChatClientFactory),
            ProviderId,
            "OpenRouter (Direct)",
            "OpenAI-compatible access to openrouter.ai",
            new Version(2026, 1, 1),
            new[]
            {
                new ConnectionParameterMetadata(ApiKey, "API Key", null, IsRequired: true, IsEncrypted: true),
                new ConnectionParameterMetadata(Model, "Model",
                    "OpenRouter model id that supports tool calling and structured outputs, e.g. openai/gpt-4o-mini",
                    IsRequired: true),
            });

        public IChatClient CreateClient(ConnectionSettings settings) =>
            new OpenAIClient(
                    new ApiKeyCredential(settings.GetRequiredParameter(ApiKey)),
                    new OpenAIClientOptions { Endpoint = Endpoint })
                .GetChatClient(settings.GetRequiredParameter(Model))
                .AsIChatClient();
    }
}
