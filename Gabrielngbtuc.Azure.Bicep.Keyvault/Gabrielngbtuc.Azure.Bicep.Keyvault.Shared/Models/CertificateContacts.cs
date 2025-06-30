using Azure.Bicep.Types.Concrete;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

[BicepSerializableType]
public record CertificateContacts(
    [property: TypeAnnotation("The list of contacts", ObjectTypePropertyFlags.Required)]
    Contact[] Contacts
    );

public record Contact( 
    [property: TypeAnnotation("The email of the contact", ObjectTypePropertyFlags.Required),
    BicepNonNullableString,
    BicepStringTypeRegexPattern("^.+@.+\\..+$")]
    string Email,
    [property: TypeAnnotation("The name of the contact", ObjectTypePropertyFlags.Required),
               BicepNullableType]
    string? Name,
    [property: TypeAnnotation("The phone number of the contact", ObjectTypePropertyFlags.Required),
               BicepNullableType]
    string? Phone
    );