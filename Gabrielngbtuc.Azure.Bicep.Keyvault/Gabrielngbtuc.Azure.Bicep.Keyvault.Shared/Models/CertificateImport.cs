using Azure.Bicep.Types.Concrete;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

[BicepSerializableType]
[BicepAllowInheritedProperties]
public record CertificateImport(
    [property: TypeAnnotation("The Base64 representation of the certificate", 
        ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.WriteOnly)]
    string Value,
    [property: TypeAnnotation("The password of the certificate", ObjectTypePropertyFlags.WriteOnly, true)]
    string? Password,
    [property: TypeAnnotation("The name of the certificate", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    string Name,
    [property: TypeAnnotation("The version of the certificate", ObjectTypePropertyFlags.Required)]
    string IssuerName,
    [property: TypeAnnotation("Subject of the certificate", ObjectTypePropertyFlags.Required)]
    string Subject,
    [property: TypeAnnotation("The policy of the certificate", ObjectTypePropertyFlags.Required)]
    CertificatePolicy policy,
    [property: TypeAnnotation("TWhether the certificate is enabled on creation")]
    bool? Enabled,
    [property: TypeAnnotation("The tags of the certificate")]
    Dictionary<string, string>? Tags,
    [property: TypeAnnotation("Whether the certificate order is preserved. if false, the leaf certificate will be placed at index 0")]
    bool? PreserveCertificateOrder,
    //Retrieved from Azure
    [property: TypeAnnotation("The certificate id", ObjectTypePropertyFlags.ReadOnly)]
    string Id,
    [property: TypeAnnotation("Id of the backing key", ObjectTypePropertyFlags.ReadOnly)]
    string KeyId,
    [property: TypeAnnotation("Id of the backing secret", ObjectTypePropertyFlags.ReadOnly)]
    string SecretId,
    [property: TypeAnnotation("The certificate data.", ObjectTypePropertyFlags.ReadOnly)]
    CertificateData? Data,
    [property: TypeAnnotation("The public key content in base64 format", ObjectTypePropertyFlags.ReadOnly)]
    string? Cer): Certificate(Name, IssuerName, Subject, policy, Enabled, Tags, PreserveCertificateOrder, Id, KeyId, SecretId, Data, Cer);