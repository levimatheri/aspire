// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.MongoDB;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding MongoDB resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class MongoDBBuilderExtensions
{
    private const int DefaultContainerPort = 27017;

    /// <summary>
    /// Adds a MongoDB resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port for MongoDB.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBServerResource> AddMongoDB(this IDistributedApplicationBuilder builder, string name, int? port = null)
    {
        var mongoDBContainer = new MongoDBServerResource(name);

        return builder
            .AddResource(mongoDBContainer)
            .WithManifestPublishingCallback(WriteMongoDBServerToManifest)
            .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, port: port, containerPort: DefaultContainerPort)) // Internal port is always 27017.
            .WithAnnotation(new ContainerImageAnnotation { Image = "mongo", Tag = "latest" });
    }

    /// <summary>
    /// Adds a MongoDB database to the application model.
    /// </summary>
    /// <param name="builder">The MongoDB server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MongoDBDatabaseResource> AddDatabase(this IResourceBuilder<MongoDBServerResource> builder, string name)
    {
        var mongoDBDatabase = new MongoDBDatabaseResource(name, builder.Resource);

        return builder.ApplicationBuilder
            .AddResource(mongoDBDatabase)
            .WithManifestPublishingCallback(context => context.WriteMongoDBDatabaseToManifest(mongoDBDatabase));
    }

    /// <summary>
    /// Adds a MongoExpress administration and development platform for MongoDB to the application model.
    /// </summary>
    /// <param name="builder">The MongoDB server resource builder.</param>
    /// <param name="hostPort">The host port for the application ui.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithMongoExpress<T>(this IResourceBuilder<T> builder, int? hostPort = null, string? containerName = null) where T : MongoDBServerResource
    {
        containerName ??= $"{builder.Resource.Name}-mongoexpress";

        var mongoExpressContainer = new MongoExpressContainerResource(containerName);
        builder.ApplicationBuilder.AddResource(mongoExpressContainer)
                                  .WithAnnotation(new ContainerImageAnnotation { Image = "mongo-express", Tag = "latest" })
                                  .WithEnvironment(context => ConfigureMongoExpressContainer(context, builder.Resource))
                                  .WithHttpEndpoint(containerPort: 8081, hostPort: hostPort, name: containerName)
                                  .ExcludeFromManifest();

        return builder;
    }

    private static void ConfigureMongoExpressContainer(EnvironmentCallbackContext context, IResource resource)
    {
        var hostPort = GetResourcePort(resource);
        
        context.EnvironmentVariables.Add("ME_CONFIG_MONGODB_URL", $"mongodb://host.docker.internal:{hostPort}/?directConnection=true");
        context.EnvironmentVariables.Add("ME_CONFIG_BASICAUTH", "false");

        static int GetResourcePort(IResource resource)
        {
            if (!resource.TryGetAllocatedEndPoints(out var allocatedEndpoints))
            {
                throw new DistributedApplicationException(
                    $"MongoDB resource \"{resource.Name}\" does not have endpoint annotation.");
            }

            return allocatedEndpoints.Single().Port;
        }
    }

    public static IResourceBuilder<MongoDBServerResource> PublishAsContainer(this IResourceBuilder<MongoDBServerResource> builder)
    {
        return builder.WithManifestPublishingCallback(context => WriteMongoDBContainerToManifest(context, builder.Resource));
    }

    private static void WriteMongoDBContainerToManifest(this ManifestPublishingContext context, MongoDBServerResource resource)
    {
        context.WriteContainer(resource);
        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"{{{resource.Name}.bindings.tcp.host}}:{{{resource.Name}.bindings.tcp.port}}");
    }

    private static void WriteMongoDBServerToManifest(this ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "mongodb.server.v0");
    }

    private static void WriteMongoDBDatabaseToManifest(this ManifestPublishingContext context, MongoDBDatabaseResource mongoDbDatabase)
    {
        context.Writer.WriteString("type", "mongodb.database.v0");
        context.Writer.WriteString("parent", mongoDbDatabase.Parent.Name);
    }
}
