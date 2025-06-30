// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Azure;
using Bicep.Local.Extension.Protocol;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Helpers;

public static class RequestHelper
{
    public static async Task<LocalExtensionOperationResponse> HandleRequest(JsonObject? config,
        Func<KeyVaultClientContainer, Task<LocalExtensionOperationResponse>> onExecuteFunc)
    {
        var configuration =
            JsonSerializer.Deserialize<Configuration>(config?.ToString() ?? "{}", Program.JsonSerializerOptions);

        if (configuration is null)
        {
            return CreateErrorResponse("InvalidConfiguration", "Configuration is invalid");
        }

        try
        {
            Console.WriteLine("Creating client with url " + configuration.KeyVaultUrl + "");
            return await onExecuteFunc(new KeyVaultClientContainer(configuration.KeyVaultUrl)); //onExecuteFunc(client);
        }
        catch (RequestFailedException exception)
        {
            return CreateErrorResponse(nameof(RequestFailedException), exception.Message,
            [
                new ErrorDetail(exception.Status.ToString(), configuration.KeyVaultUrl, exception.Message)
            ]);
        }
        catch (Exception exception)
        {
            return CreateErrorResponse("UnhandledError", exception.Message);
        }
    }

    public static TProperties GetProperties<TProperties>(JsonObject properties)
        => properties.Deserialize<TProperties>(Program.JsonSerializerOptions)!;

    public static LocalExtensionOperationResponse CreateSuccessResponse<TProperties, TIdentifiers>(
        ResourceReference request, TProperties properties, TIdentifiers identifiers)
    {
        return new(
            new(
                request.Type,
                request.ApiVersion,
                "Succeeded",
                (JsonNode.Parse(JsonSerializer.Serialize(identifiers,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!,
                request.Config,
                (JsonNode.Parse(JsonSerializer.Serialize(properties,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!),
            null);
    }

    public static LocalExtensionOperationResponse CreateSuccessResponse<TProperties, TIdentifiers>(
        ResourceSpecification request, TProperties properties, TIdentifiers identifiers)
    {
        return new(
            new(
                request.Type,
                request.ApiVersion,
                "Succeeded",
                (JsonNode.Parse(JsonSerializer.Serialize(identifiers,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!,
                request.Config,
                (JsonNode.Parse(JsonSerializer.Serialize(properties,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))) as JsonObject)!),
            null);
    }

    public static LocalExtensionOperationResponse CreateErrorResponse(string code, string message,
        ErrorDetail[]? details = null, string? target = null)
    {
        return new LocalExtensionOperationResponse(
            null,
            new(new(code, target ?? "", message, details ?? [], [])));
    }

    public static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
        {
            return input;
        }

        return char.ToLower(input[0]) + input.Substring(1);
    }


    public static JsonNode? GetIdentifierData(ResourceReference reference, string propertyName) =>
        reference.Identifiers[ToCamelCase(propertyName)];
}