using CozyKitchen.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace CozyKitchen.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSemanticKernelWithChatCompletionsAndEmbeddingGeneration(
        this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var openAiOptions = serviceProvider.GetRequiredService<IOptions<OpenAiOptions>>()!.Value;

        var kernel = new KernelBuilder()
            .WithLoggerFactory(loggerFactory!)
            .WithAzureOpenAIChatCompletionService(
                endpoint: openAiOptions.ApiEndpoint,
                deploymentName: openAiOptions.ChatModelName,
                apiKey: openAiOptions.ApiKey
            )
            .WithAzureOpenAITextEmbeddingGenerationService(
                endpoint: openAiOptions.ApiEndpoint,
                deploymentName: openAiOptions.EmbeddingsModelName,
                apiKey: openAiOptions.ApiKey
            )
            .Build();

        services.AddSingleton(kernel);

        return services;
    }
}
