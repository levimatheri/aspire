// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AI.Search.Tests;

public class AspireAzureAISearchExtensionsTests
{
    private const string ConnectionString = "Endpoint=https://aspireaisearchtests.search.windows.net/;Key=fake";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:aisearch", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureAISearch("aisearch");
        }
        else
        {
            builder.AddAzureAISearch("aisearch");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<SearchIndexClient>("aisearch") :
            host.Services.GetRequiredService<SearchIndexClient>();

        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var uri = new Uri("https://aspireaisearchtests.search.windows.net/");
        var key = "fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);

        if (useKeyed)
        {
            builder.AddKeyedAzureAISearch("aisearch", settings => { settings.Endpoint = uri; settings.Key = key; });
        }
        else
        {
            builder.AddAzureAISearch("aisearch", settings => { settings.Endpoint = uri; settings.Key = key; });
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<SearchIndexClient>("aisearch") :
            host.Services.GetRequiredService<SearchIndexClient>();

        Assert.NotNull(client);
    }
}
