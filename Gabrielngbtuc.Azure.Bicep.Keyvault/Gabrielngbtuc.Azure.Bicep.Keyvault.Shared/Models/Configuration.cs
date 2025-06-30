using Azure.Bicep.Types.Concrete;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

[BicepSerializableType]
public record Configuration(
    [property: TypeAnnotation("The full url of the keyvault",
        ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required),
    BicepStringTypeRegexPattern("^https:\\/\\/[a-zA-Z0-9\\-\\.]+(\\/\\S*)?$")]
    string KeyVaultUrl
);
