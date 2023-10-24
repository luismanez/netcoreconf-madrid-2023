using CozyKitchen.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace CozyKitchen.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSemanticKernelWithChatAndTextCompletions(
        this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var openAiOptions = serviceProvider.GetRequiredService<IOptions<OpenAiOptions>>()!.Value;

        var kernel = new KernelBuilder()
            .WithLoggerFactory(loggerFactory!)
            .WithAzureChatCompletionService(
                endpoint: openAiOptions.ApiEndpoint,
                deploymentName: openAiOptions.ChatModelName,
                apiKey: openAiOptions.ApiKey
            )
            .WithAzureTextEmbeddingGenerationService(
                endpoint: openAiOptions.ApiEndpoint,
                deploymentName: openAiOptions.ChatModelName,
                apiKey: openAiOptions.ApiKey
            )
            .Build();

        services.AddSingleton(kernel);

        return services;
    }
}
