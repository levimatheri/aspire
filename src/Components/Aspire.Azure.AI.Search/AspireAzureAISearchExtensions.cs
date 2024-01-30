// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure;
using Azure.Core;
using Azure.Core.Extensions;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Aspire.Azure.AI.Search;

/// <summary>
/// Provides extension methods for registering <see cref="SearchIndexClient"/> as a singleton in the services provided by the <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireAzureAISearchExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:AI:Search";
    /// <summary>
    /// Registers <see cref="SearchIndexClient"/> as a singleton in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureAISearchSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{SearchIndexClient, SearchClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.Search" section.</remarks>
    public static void AddAzureAISearch(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureAISearchSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SearchIndexClient, SearchClientOptions>>? configureClientBuilder = null)
    {
        new AISearchComponent().AddClient(builder, DefaultConfigSectionName, configureSettings, configureClientBuilder, connectionName, serviceKey: null);
    }

    /// <summary>
    /// Registers <see cref="SearchIndexClient"/> as a singleton for given <paramref name="name"/> in the services provided by the <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing the <see cref="AzureAISearchSettings"/>. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureClientBuilder">An optional method that can be used for customizing the <see cref="IAzureClientBuilder{SearchIndexClient, SearchClientOptions}"/>.</param>
    /// <remarks>Reads the configuration from "Aspire.Azure.AI.Search:{name}" section.</remarks>
    public static void AddKeyedAzureAISearch(
        this IHostApplicationBuilder builder,
        string name,
        Action<AzureAISearchSettings>? configureSettings = null,
        Action<IAzureClientBuilder<SearchIndexClient, SearchClientOptions>>? configureClientBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var configurationSectionName = AISearchComponent.GetKeyedConfigurationSectionName(name, DefaultConfigSectionName);

        new AISearchComponent().AddClient(builder, configurationSectionName, configureSettings, configureClientBuilder, connectionName: name, serviceKey: name);
    }

    private sealed class AISearchComponent : AzureComponent<AzureAISearchSettings, SearchIndexClient, SearchClientOptions>
    {
        // `SearchIndexClient` is in the Azure.Search.Documents.Indexes namespace
        // but uses `SearchClientOptions` which is in the Azure.Search.Documents namespace
        // https://github.com/Azure/azure-sdk-for-net/blob/bed506dee05319ff2de27ca98500daa10573fe7d/sdk/search/Azure.Search.Documents/src/Indexes/SearchIndexClient.cs#L92
        protected override string[] ActivitySourceNames => ["Azure.Search.Documents.*"];

        protected override IAzureClientBuilder<SearchIndexClient, SearchClientOptions> AddClient<TBuilder>(TBuilder azureFactoryBuilder, AzureAISearchSettings settings, string connectionName, string configurationSectionName)
        {
            return azureFactoryBuilder.RegisterClientFactory<SearchIndexClient, SearchClientOptions>((options, cred) =>
            {
                if (settings.Endpoint is null)
                {
                    throw new InvalidOperationException($"A SearchIndexClient could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify an '{nameof(AzureAISearchSettings.Endpoint)}' in the '{configurationSectionName}' configuration section.");
                }

                if (!string.IsNullOrWhiteSpace(settings.Key))
                {
                    return new SearchIndexClient(settings.Endpoint, new AzureKeyCredential(settings.Key), options);
                }
                else
                {
                    return new SearchIndexClient(settings.Endpoint, settings.Credential ?? new DefaultAzureCredential(), options);
                }
            });
        }

        protected override void BindClientOptionsToConfiguration(IAzureClientBuilder<SearchIndexClient, SearchClientOptions> clientBuilder, IConfiguration configuration)
        {
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - needed so the ConfigBinder Source Generator works
            clientBuilder.ConfigureOptions(options => configuration.Bind(options));
#pragma warning restore IDE0200
        }

        protected override void BindSettingsToConfiguration(AzureAISearchSettings settings, IConfiguration config)
        {
            config.Bind(settings);
        }

        protected override IHealthCheck CreateHealthCheck(SearchIndexClient client, AzureAISearchSettings settings)
        {
            throw new NotImplementedException();
        }

        protected override bool GetHealthCheckEnabled(AzureAISearchSettings settings)
        {
            return false;
        }

        protected override TokenCredential? GetTokenCredential(AzureAISearchSettings settings)
            => settings.Credential;

        protected override bool GetTracingEnabled(AzureAISearchSettings settings)
            => settings.Tracing;
    }
}
