using System.Text.Json;
using System.Text.Json.Nodes;
using Bicep.Local.Extension.Protocol;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.UnitTests;

public static class HandlerHelper
{
    public static JsonSerializerOptions JsonSerializerOptions { get; } =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public static ResourceSpecification GetResourceSpecification<T>(T properties)
    {
        return new ResourceSpecification("MockedType", null, JsonNode.Parse(
            JsonSerializer.Serialize(properties, JsonSerializerOptions)
        )!.AsObject(), JsonNode.Parse(
            JsonSerializer.Serialize(new Configuration("https://localhost:4997"), JsonSerializerOptions)
        )!.AsObject());
    }

    public static ResourceReference GetResourceReference<T>(T identifiers)
    {
        return new ResourceReference("MockedType", null, JsonNode.Parse(
            JsonSerializer.Serialize(identifiers, JsonSerializerOptions)
        )!.AsObject(), JsonNode.Parse(
            JsonSerializer.Serialize(new Configuration("https://localhost:4997"), JsonSerializerOptions)
        )!.AsObject());
    }
}