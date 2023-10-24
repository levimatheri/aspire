// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class AzureStorageResource(string name) : Resource(name), IAzureResource
{
    public Uri? TableUri { get; set; }
    public Uri? QueueUri { get; set; }
    public Uri? BlobUri { get; set; }
}

public class AzureTableStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IAzureResource,
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageResource>
{
    public AzureStorageResource Parent => storage;

    public string? GetConnectionString() => Parent.TableUri?.ToString();
}

public class AzureBlobStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IAzureResource,
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageResource>
{
    public AzureStorageResource Parent => storage;

    public string? GetConnectionString() => Parent.BlobUri?.ToString();
}

public class AzureQueueStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IAzureResource,
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageResource>
{
    public AzureStorageResource Parent => storage;

    public string? GetConnectionString() => Parent.QueueUri?.ToString();
}
