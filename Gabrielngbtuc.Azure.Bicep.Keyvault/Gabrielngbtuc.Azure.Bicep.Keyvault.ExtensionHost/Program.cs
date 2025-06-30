using System.Text.Json;
using Bicep.Local.Extension;
using Bicep.Local.Extension.Protocol;
using Bicep.Local.Extension.Rpc;
using Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost;

public static class Program
{
    public static readonly JsonSerializerOptions JsonSerializerOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public static async Task Main(string[] args)
    {
        await ProviderExtension.Run(new KestrelProviderExtension(), RegisterHandlers, args);
    }

    public static void RegisterHandlers(ResourceDispatcherBuilder builder) => builder
        .AddHandler(new CertificateHandler())
        .AddHandler(new CertificateImportHandler())
        .AddHandler(new CertificateIssuerHandler())
        .AddHandler(new CertificateContactsHandler());
}

public class KestrelProviderExtension : ProviderExtension
{
    protected override async Task RunServer(ConnectionOptions connectionOptions, ResourceDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
        {
            switch (connectionOptions)
            {
                case { Socket: { }, Pipe: null }:
                    options.ListenUnixSocket(connectionOptions.Socket,
                        listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    break;
                case { Socket: null, Pipe: { } }:
                    options.ListenNamedPipe(connectionOptions.Pipe,
                        listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                    break;
                default:
                    throw new InvalidOperationException("Either socketPath or pipeName must be specified.");
            }
        });

        builder.Services.AddGrpc();
        builder.Services.AddSingleton(dispatcher);
        var app = builder.Build();
        app.MapGrpcService<BicepExtensionImpl>();

        await app.RunAsync();
    }
}