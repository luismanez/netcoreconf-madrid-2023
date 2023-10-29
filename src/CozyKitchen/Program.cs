﻿using CozyKitchen.Extensions;
using CozyKitchen.HostedServices;
using CozyKitchen.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(configHost =>
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        configHost.SetBasePath(currentDirectory);
        configHost.AddJsonFile("hostsettings.json", optional: false);
        configHost.AddCommandLine(args);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        services.AddOptions();
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SettingsSectionName));

        services.AddLogging(configure => configure.AddConsole());

        services.AddSemanticKernelWithChatAndTextCompletions();

        var demoToRun = args.Length > 0 ? args[0] : "1";

        switch (demoToRun)
        {
            default:
                services.AddHostedService<HelloSemanticWorldHostedService>();
                break;
            case "2":
                services.AddHostedService<SemanticFunctionWithParamsHostedService>();
                break;
            case "3":
                //services.AddHostedService<NativeFunctionHostedService>();
                services.AddHostedService<NestedFunctionHostedService>();
                break;
            case "4":
                services.AddHostedService<PlannerHostedService>();
                break;
        }
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<IHost>>();
try
{
    host.Run();
}
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures)
    {
        logger!.LogError(failure);
    }
}