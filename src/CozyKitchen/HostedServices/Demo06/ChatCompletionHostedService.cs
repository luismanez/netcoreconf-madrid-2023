using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;

namespace CozyKitchen.HostedServices;
public class ChatCompletionHostedService : IHostedService
{
    private readonly IChatCompletion _chatCompletion;
    private readonly ILogger<ChatCompletionHostedService> _logger;

    public ChatCompletionHostedService(
        IKernel kernel,
        ILogger<ChatCompletionHostedService> logger)
    {
        _chatCompletion = kernel.GetService<IChatCompletion>();
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var chat = _chatCompletion.CreateNewChat("You are an AI willing to help");
        // simulating adding chat history:
        chat.AddUserMessage("List me 3 famous songs from The Beattles");
        chat.AddAssistantMessage("Hey Jude, Let it be, Yesterday");
        // new question (based on history):
        chat.AddUserMessage("When was written the 3rd one?");

        var chatRequestSettings = new OpenAIRequestSettings()
        {
            MaxTokens = 1024,
            ResultsPerPrompt = 1,
            Temperature = 1,
            TopP = 0.5,
            FrequencyPenalty = 0,
        };

        var completions = await _chatCompletion.GetChatCompletionsAsync(chat, chatRequestSettings);
        var completion = completions.FirstOrDefault();
        _logger.LogInformation("ChatCompletion result for 'When was written the 3rd one?'");
        var chatMessage = await completion!.GetChatMessageAsync();
        _logger.LogInformation($"{chatMessage.Content}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("HostedService Stopped");
        return Task.CompletedTask;
    }
}
