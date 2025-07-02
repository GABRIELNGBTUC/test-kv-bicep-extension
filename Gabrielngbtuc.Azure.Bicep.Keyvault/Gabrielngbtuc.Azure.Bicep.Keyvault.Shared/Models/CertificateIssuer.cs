using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

[BicepSerializableType]
public record CertificateIssuer(
    [property: TypeAnnotation("The name of the issue", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required),
               BicepNonNullableString]
    string Name,
    [property: TypeAnnotation("The provider of the certificate", ObjectTypePropertyFlags.Required)]
    CertificateIssuerCredential Provider
    );

public record CertificateIssuerDiscriminator();

[BicepDiscriminatorType(typeof(CertificateIssuerDiscriminator)),
 JsonPolymorphic(TypeDiscriminatorPropertyName = "provider"),
JsonDerivedType(typeof(DigicertCredentials), "DigiCert"),
JsonDerivedType(typeof(GlobalSignCredentials), "GlobalSign"),
JsonDerivedType(typeof(UnknownCredentials), "Unknown")]
public record CertificateIssuerCredential(
    [property: TypeAnnotation("The account ID with the provider", ObjectTypePropertyFlags.Required),
               BicepNonNullableString]
    string AccountId,
    [property: TypeAnnotation("The password of the account", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.WriteOnly, true),
               BicepNonNullableString]
    string Password,
    [property: TypeAnnotation("The name of the provider", ObjectTypePropertyFlags.ReadOnly)]
    string ProviderName);

public record DigicertCredentials(
    [property: TypeAnnotation("The organization ID", ObjectTypePropertyFlags.Required),
        BicepNonNullableString]
    string OrganizationId,
    string AccountId,
    string Password
    ) : CertificateIssuerCredential(AccountId, Password, nameof(CertificateIssuerProvider.DigiCert));

public record GlobalSignCredentials(
    [property: TypeAnnotation("The first name of the administrator", ObjectTypePropertyFlags.Required),
               BicepNonNullableString]
    string FirstName,
    [property: TypeAnnotation("The last name of the administrator", ObjectTypePropertyFlags.Required),
               BicepNonNullableString]
    string LastName,
    [property: TypeAnnotation("The email of the administrator", ObjectTypePropertyFlags.Required),
               BicepNonNullableString]
    string Email,
    [property: TypeAnnotation("The phone number of the administrator", ObjectTypePropertyFlags.Required),
               BicepNonNullableString]
    string Phone,
    string AccountId,
    string Password
    ) : CertificateIssuerCredential(AccountId, Password, nameof(CertificateIssuerProvider.GlobalSign));

public record UnknownCredentials(
    [property: TypeAnnotation("The name of the issuer as found in the keyvault", ObjectTypePropertyFlags.ReadOnly),]
    string ProviderNameFound
) : CertificateIssuerCredential("Unknown", "Unknown", ProviderNameFound);


public enum CertificateIssuerProvider
{
    [Display(Name = "DigiCert")]
    DigiCert,
    [Display(Name = "GlobalSign")]
    GlobalSign,
    [Display(Name = "Unknown")]
    Unknown
}