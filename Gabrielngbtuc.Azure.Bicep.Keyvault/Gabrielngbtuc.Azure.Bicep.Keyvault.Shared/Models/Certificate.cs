using Azure.Bicep.Types.Concrete;
using Azure.Security.KeyVault.Certificates;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

[BicepSerializableType]
public record Certificate(
    [property: TypeAnnotation("The name of the certificate",
        ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    string Name,
    [property: TypeAnnotation("The version of the certificate", ObjectTypePropertyFlags.Required)]
    string IssuerName,
    [property: TypeAnnotation("Subject of the certificate", ObjectTypePropertyFlags.Required)]
    string Subject,
    [property: TypeAnnotation("The policy of the certificate", ObjectTypePropertyFlags.Required)]
    CertificatePolicy policy,
    //Retrieved from Azure
    [property: TypeAnnotation("The certificate id", ObjectTypePropertyFlags.ReadOnly)]
    string Id,
    [property: TypeAnnotation("Id of the backing key", ObjectTypePropertyFlags.ReadOnly)]
    string KeyId,
    [property: TypeAnnotation("Id of the backing secret", ObjectTypePropertyFlags.ReadOnly)]
    string SecretId,
    [property: TypeAnnotation("The certificate data.", ObjectTypePropertyFlags.ReadOnly)]
    CertificateData? Data = null,
    [property: TypeAnnotation("The public key content in base64 format", ObjectTypePropertyFlags.ReadOnly)]
    string? Cer = null,
    [property: TypeAnnotation("TWhether the certificate is enabled on creation")]
    bool? Enabled = null,
    [property: TypeAnnotation("The tags of the certificate")]
    Dictionary<string, string> Tags = default,
    [property: TypeAnnotation(
        "Whether the certificate order is preserved. if false, the leaf certificate will be placed at index 0")]
    bool? PreserveCertificateOrder = null
);
    
    public record CertificateData(
        [property: TypeAnnotation("Id of the certificate", ObjectTypePropertyFlags.ReadOnly)]
        string Id,
        [property: TypeAnnotation("Name of the certificate", ObjectTypePropertyFlags.ReadOnly)]
        string Name,
        [property: TypeAnnotation("Url of the keyvault containing the certificate", ObjectTypePropertyFlags.ReadOnly)]
        string VaultUri,
        [property: TypeAnnotation("Version of the certificate", ObjectTypePropertyFlags.ReadOnly)]
        string Version,
        [property: TypeAnnotation("Base64 representation of the thumbprint", ObjectTypePropertyFlags.ReadOnly)]
        string X509Thumbprint,
        [property: TypeAnnotation("Decoded base64 representation of the thumbprint", ObjectTypePropertyFlags.ReadOnly)]
        string X509ThumbprintString,
        [property: TypeAnnotation("Tags of the certificate", ObjectTypePropertyFlags.ReadOnly)]
        Dictionary<string, string>? Tags,
        [property: TypeAnnotation("Whether the certificate is enabled", ObjectTypePropertyFlags.ReadOnly)]
        bool? Enabled,
        [property: TypeAnnotation("Activation date of the certificate", ObjectTypePropertyFlags.ReadOnly)]
        string? NotBefore,
        [property: TypeAnnotation("Expiration date of the certificate", ObjectTypePropertyFlags.ReadOnly)]
        string? ExpiresOn,
        [property: TypeAnnotation("Creation date of the certificate", ObjectTypePropertyFlags.ReadOnly)]
        string? CreatedOn,
        [property: TypeAnnotation("Last update date of the certificate", ObjectTypePropertyFlags.ReadOnly)]
        string? UpdatedOn,
        [property: TypeAnnotation("Number of days before the certificate is purged on a soft-delete", ObjectTypePropertyFlags.ReadOnly)]
        int? RecoverableDays,
        [property: TypeAnnotation("Recovery level of the certificate", ObjectTypePropertyFlags.ReadOnly)]
        string? RecoveryLevel
        );