using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

[BicepSerializableType]
public record CertificatePolicy(
    [property: TypeAnnotation(
        "The content type of the certificate. Set to Pkcs12 when Cer contains your raw PKCS#12/PFX bytes, or to Pem when Cer contains your ASCII PEM-encoded bytes. If not specified, Pkcs12 is assumed.",
        ObjectTypePropertyFlags.Required)]
    [property: JsonConverter(typeof(BicepEnumConverter<CertificateContentType>))]
    CertificateContentType ContentType,
    [property: TypeAnnotation("Whether the certificate is exportable or not",
        ObjectTypePropertyFlags.Required)]
    bool Exportable,
    [property: TypeAnnotation("Gets or sets the type of backing key to be generated when issuing new certificates.",
        ObjectTypePropertyFlags.Required)]
    [property: JsonConverter(typeof(BicepEnumConverter<CertificateKeyType>))]
    CertificateKeyType KeyType,
    [property: TypeAnnotation("The size of the RSA key. Must either be 2048 or 4096", ObjectTypePropertyFlags.Required)]
    [property: BicepStringLiteralUnion(false, "2048", "4096")]
    [property: JsonConverter(typeof(StringToIntJsonConverter))]
    int KeySize,
    [property: TypeAnnotation("Whether the key should be reused when rotating certificates",
        ObjectTypePropertyFlags.Required)]
    bool ReuseKey,
    [property: TypeAnnotation("The allowed enhanced key usages of the certificate", ObjectTypePropertyFlags.ReadOnly)]
    string[]? EnhancedKeyUsages = null,
    [property: TypeAnnotation("Gets or sets the curve which back an Elliptic Curve (EC) key.")]
    [property: BicepNullableType]
    string? KeyCurveName = null,
    [property: TypeAnnotation("Validity of the certificate in months")]
    int? ValidityInMonths = null,
    [property: TypeAnnotation("If the certificate is enabled")]
    bool? Enabled = null,
    [property: TypeAnnotation("The subject alternative names of the certificate", ObjectTypePropertyFlags.ReadOnly)]
    SubjectAlternativeNamesData? SubjectAlternativeNames = null,
    [property: TypeAnnotation(
        "Gets or sets a value indicating whether a certificate should be published to the certificate transparency list when created.")]
    bool? CertificateTransparency = null,
    [property: TypeAnnotation("The type of the certificate")]
    [property: BicepNullableType]
    string? CertificateType = null,
    [property: TypeAnnotation("The allowed usages of the key of the certificate", ObjectTypePropertyFlags.ReadOnly)]
    [property: JsonConverter(typeof(BicepEnumArrayConverter<KeyUsageType>))]
    KeyUsageType[]? KeyUsage = null,
    [property: TypeAnnotation("The lifetime actions")]
    LifetimeAction? LifetimeActions = null);

public record SubjectAlternativeNamesData(
    [property: TypeAnnotation("The list of domains")]
    string[]? DnsNames,
    [property: TypeAnnotation("The list of emails")]
    string[]? Emails,
    [property: TypeAnnotation("The list of upns")]
    string[]? UserPrincipalNames);

public record LifetimeAction(
    [property: TypeAnnotation("The action type of the lifetime action"),
               JsonConverter(typeof(BicepEnumConverter<CertificatePolicyActionType>))]
    CertificatePolicyActionType ActionType,
    [property: TypeAnnotation("The lifetime percentage at which to trigger the action. Should be between 1 and 99")]
    int? LifetimePercentage,
    [property: TypeAnnotation("The number of days before the expiration after which the action should execute")]
    int? DaysBeforeExpiry);

public enum CertificatePolicyActionType
{
    [Display(Name = "EmailContacts")]
    EmailContacts,
    [Display(Name = "AutoRenew")]
    AutoRenew,
}

    
public enum KeyUsageType
{
    [Display(Name = "crlSign")]
    CrlSign,
    [Display(Name = "dataEncipherment")]
    DataEncipherment,
    [Display(Name = "decipherOnly")]
    DecipherOnly,
    [Display(Name = "digitalSignature")]
    DigitalSignature,
    [Display(Name = "encipherOnly")]
    EncipherOnly,
    [Display(Name = "keyAgreement")]
    KeyAgreement,
    [Display(Name = "keyCertSign")]
    KeyCertSign,
    [Display(Name = "keyEncipherment")]
    KeyEncipherment,
    [Display(Name = "nonRepudiation")]
    NonRepudiation,
}

public enum CertificateKeyType
{
    [Display(Name = "EC")]
    Ec,
    [Display(Name = "EC-HSM")]
    EcHsm,
    [Display(Name = "RSA")]
    Rsa,
    [Display(Name = "RSA-HSM")]
    RsaHsm
}

public enum CertificateContentType
{
    [Display(Name = "application/x-pkcs12")]
    Pkcs12,
    [Display(Name = "application/x-pem-file")]
    Pem
}

public record RootObject(
    Policy policy,
    string id,
    string name,
    string keyId,
    string secretId,
    Properties properties,
    string cer,
    object preserveCertificateOrder
);

public record Policy(
    KeyType keyType,
    bool reuseKey,
    bool exportable,
    object keyCurveName,
    int keySize,
    string subject,
    object subjectAlternativeNames,
    string issuerName,
    ContentType contentType,
    object certificateType,
    object certificateTransparency,
    int validityInMonths,
    bool enabled,
    string updatedOn,
    string createdOn,
    KeyUsage[] keyUsage,
    string[] enhancedKeyUsage,
    LifetimeActions[] lifetimeActions
);

public record KeyType(

);

public record ContentType(

);

public record KeyUsage(

);

public record LifetimeActions(
    object daysBeforeExpiry,
    int lifetimePercentage,
    Action action
);

public record Action(

);

public record Properties(
    string id,
    string name,
    string vaultUri,
    string version,
    string x509Thumbprint,
    string x509ThumbprintString,
    Tags tags,
    bool enabled,
    string notBefore,
    string expiresOn,
    string createdOn,
    string updatedOn,
    int recoverableDays,
    string recoveryLevel
);

public record Tags(

);

