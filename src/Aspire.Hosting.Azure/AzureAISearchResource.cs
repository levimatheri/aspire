// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure AI Search.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureAISearchResource(string name) : Resource(name), IAzureResource, IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the connection string for the AI Search resource.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets the connection string for the AI Search service.
    /// </summary>
    /// <returns>The connection string for the AI Search service.</returns>
    string? IResourceWithConnectionString.GetConnectionString() => ConnectionString;
}

