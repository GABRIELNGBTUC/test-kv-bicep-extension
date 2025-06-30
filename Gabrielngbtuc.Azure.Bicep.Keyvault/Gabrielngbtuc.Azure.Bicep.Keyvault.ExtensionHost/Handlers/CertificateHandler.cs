using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Azure;
using Bicep.Local.Extension.Protocol;
using Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Helpers;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Handlers;

public class CertificateHandler : IResourceHandler
{
    public string ResourceType => nameof(Certificate);

    public record Identifiers(
        string? Name,
        string? Version);

    public Task<LocalExtensionOperationResponse> CreateOrUpdate(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async clientContainer =>
        {
            Console.WriteLine(JsonSerializer.Serialize(request, Program.JsonSerializerOptions));
            var properties = RequestHelper.GetProperties<Certificate>(request.Properties);
            Console.WriteLine("Deserialized properties");
            var certificateExists = false;
            AzureCertificateNamespace.KeyVaultCertificateWithPolicy originalCertificate = null;
            
            try
            {
                Console.WriteLine("Trying to get certificate");
                var certificate =
                    await clientContainer.CertificateClient.GetCertificateAsync(properties.Name,
                        cancellationToken: cancellationToken);
                certificateExists = certificate is { HasValue: true };
                originalCertificate = certificate.Value;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get certificate with error");
                Console.WriteLine(e);
            }

            if (certificateExists is false)
            {
                Console.WriteLine("Certificate does not exist");
                Console.WriteLine("Creating certificate");
                var policy = MapToAzureCertificatePolicy(properties);
                Console.WriteLine(JsonSerializer.Serialize(policy, Program.JsonSerializerOptions));

                var result = await clientContainer.CertificateClient.StartCreateCertificateAsync(properties.Name, policy,
                    cancellationToken: cancellationToken);
                await result.WaitForCompletionAsync(cancellationToken);
                Console.WriteLine("Operation fully completed");
                properties = MapToCertificate(properties,
                        result.Value) with
                    {
                        policy = MapToCertificatePolicy(result.Value.Policy)
                    };
            }

            else
            {
                // Update
                Console.WriteLine("Certificate exists");
                Console.WriteLine("Updating certificate");
                var policy = MapToAzureCertificatePolicy(properties);
                var updatedPolicy = await clientContainer.CertificateClient.UpdateCertificatePolicyAsync(properties.Name, policy,
                    cancellationToken);
                Console.WriteLine("Policy updated");
                var props = new AzureCertificateNamespace.CertificateProperties(properties.Name)
                {
                    Enabled = properties.Enabled,
                    Tags = { }
                };
                Console.WriteLine("Updating certificate properties");
                foreach (var tag in properties.Tags ?? [])
                {
                    props.Tags.Add(tag.Key, tag.Value);
                }
                Console.WriteLine("Tags updated");

                try
                {
                    var keyvaultCertificate =
                        await clientContainer.CertificateClient.UpdateCertificatePropertiesAsync(props,
                            cancellationToken);
                    Console.WriteLine("Certificate updated");

                    properties = MapToCertificate(properties, keyvaultCertificate) with
                    {
                        policy = MapToCertificatePolicy(updatedPolicy)
                    };
                }
                catch (RequestFailedException requestFailedException)
                {
                    if (requestFailedException.ErrorCode == "BadParameter" &&
                        requestFailedException.Message.Contains("Nothing to update",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Nothing to update");
                        properties = MapToCertificate(properties, originalCertificate!) with
                        {
                            policy = MapToCertificatePolicy(updatedPolicy)
                        };
                    }
                    else
                    {
                        throw;
                    }
                }
               
                
            }
            return RequestHelper.CreateSuccessResponse(request, properties,
                new Identifiers(properties.Name, null));
        });

    // Corresponds to existing
    public Task<LocalExtensionOperationResponse> Preview(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            var properties = RequestHelper.GetProperties<Certificate>(request.Properties);


            await Task.Yield();

            // Remove any property that is not needed in the response

            return RequestHelper.CreateSuccessResponse(request, properties,
                new Identifiers(properties.Name, null));
        });

    public Task<LocalExtensionOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async clientContainer =>
        {
            //Get identifiers
            var name = RequestHelper.GetIdentifierData(request, nameof(Certificate.Name))?.ToString();

            var azureCertificate = await clientContainer.CertificateClient.GetCertificateAsync(name, cancellationToken);

            var serializedProperties =
                JsonSerializer.Serialize(azureCertificate.Value.Properties, Program.JsonSerializerOptions);

            var bicepCertificate = new Certificate(name, azureCertificate.Value.Policy.IssuerName,
                azureCertificate.Value.Policy.Subject,
                MapToCertificatePolicy(azureCertificate.Value.Policy),
                Id: azureCertificate.Value.Id.ToString(), KeyId: azureCertificate.Value.KeyId.ToString(),
                SecretId: azureCertificate.Value.SecretId.ToString(),
                Data: JsonSerializer.Deserialize<CertificateData>(serializedProperties, Program.JsonSerializerOptions),
                Cer: Convert.ToBase64String(azureCertificate.Value.Cer), Enabled: azureCertificate.Value.Properties.Enabled, Tags: azureCertificate.Value.Properties.Tags.ToDictionary(), PreserveCertificateOrder: azureCertificate.Value.PreserveCertificateOrder);
            return RequestHelper.CreateSuccessResponse(request, bicepCertificate, request.Identifiers);
        });

    [ExcludeFromCodeCoverage]
    public Task<LocalExtensionOperationResponse> Delete(ResourceReference request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public static Certificate MapToCertificate(Certificate certificate,
        AzureCertificateNamespace.KeyVaultCertificate keyVaultCertificate)
    {
        Console.WriteLine("Mapping Azure certificate type to bicep type");
        var serializedProperties = JsonSerializer.Serialize(keyVaultCertificate.Properties, Program.JsonSerializerOptions);
        certificate = certificate with
        {
            PreserveCertificateOrder = keyVaultCertificate.PreserveCertificateOrder,
            Data = JsonSerializer.Deserialize<CertificateData>(serializedProperties, Program.JsonSerializerOptions),
            Id = keyVaultCertificate.Id.ToString(),
            KeyId = keyVaultCertificate.KeyId.ToString(),
            SecretId = keyVaultCertificate.SecretId.ToString(),
            Cer = Convert.ToBase64String(keyVaultCertificate.Cer),
            Name = keyVaultCertificate.Name
        };
        return certificate;
    }

    public static CertificatePolicy MapToCertificatePolicy(AzureCertificateNamespace.CertificatePolicy policy)
    {
        Console.WriteLine("Mapping Azure certificate policy to bicep type");
        var lifetimeAction = policy.LifetimeActions is null ? null :
            policy.LifetimeActions.Count == 1 ? new LifetimeAction(
                EnumHelper.GetEnumFromDisplayName<CertificatePolicyActionType>(policy.LifetimeActions[0].Action.ToString()),
                policy.LifetimeActions[0].LifetimePercentage,
                policy.LifetimeActions[0].DaysBeforeExpiry) : null;
        var contentType =
            EnumHelper.GetEnumFromDisplayName<CertificateContentType>(policy.ContentType.Value.ToString());
        var keyType = EnumHelper.GetEnumFromDisplayName<CertificateKeyType>(policy.KeyType.Value.ToString());
        return new CertificatePolicy(contentType, 
            Exportable: policy.Exportable ?? false, 
            KeyType: keyType, 
            KeySize: policy.KeySize.Value, 
            ReuseKey: policy.ReuseKey.Value, 
            EnhancedKeyUsages: policy.EnhancedKeyUsage.ToArray(), 
            KeyCurveName: policy.KeyCurveName.Value.ToString(), 
            ValidityInMonths: policy.ValidityInMonths.Value, 
            Enabled: policy.Enabled, 
            SubjectAlternativeNames: policy.SubjectAlternativeNames is null
                ? null
                : new SubjectAlternativeNamesData(policy.SubjectAlternativeNames.DnsNames.ToArray(),
                    policy.SubjectAlternativeNames.Emails.ToArray(),
                    policy.SubjectAlternativeNames.UserPrincipalNames.ToArray()
                ), CertificateTransparency: policy.CertificateTransparency, CertificateType: policy.CertificateType, KeyUsage: policy.KeyUsage.Select(ku => EnumHelper.GetEnumFromDisplayName<KeyUsageType>(ku.ToString())).ToArray(), LifetimeActions: lifetimeAction);
    }

    public static AzureCertificateNamespace.CertificatePolicy MapToAzureCertificatePolicy(Certificate certificate)
    {
        var sans = new AzureCertificateNamespace.SubjectAlternativeNames();

        if (certificate.policy.SubjectAlternativeNames is not null
            && certificate.policy.SubjectAlternativeNames.Emails is not null
            && certificate.policy.SubjectAlternativeNames.Emails.Any())
        {
            foreach (var email in certificate.policy.SubjectAlternativeNames?.Emails ?? [])
            {
                sans.Emails.Add(email);
            }
        }

        if (certificate.policy.SubjectAlternativeNames != null &&
            certificate.policy.SubjectAlternativeNames.DnsNames != null &&
            certificate.policy.SubjectAlternativeNames.DnsNames.Any())
        {
            foreach (var dns in certificate.policy.SubjectAlternativeNames?.DnsNames ?? [])
            {
                sans.DnsNames.Add(dns);
            }
        }

        if (certificate.policy.SubjectAlternativeNames?.UserPrincipalNames != null &&
            certificate.policy.SubjectAlternativeNames.UserPrincipalNames.Any())
        {
            foreach (var upn in certificate.policy.SubjectAlternativeNames?.UserPrincipalNames ?? [])
            {
                sans.UserPrincipalNames.Add(upn);
            }
        }

        var sansIsValid = sans.Emails.Any() || sans.DnsNames.Any() || sans.UserPrincipalNames.Any();
        if (sansIsValid)
        {
            var policy =
                new AzureCertificateNamespace.CertificatePolicy(certificate.IssuerName, certificate.Subject, sans)
                {
                    ContentType = new AzureCertificateNamespace.CertificateContentType(EnumHelper.GetDisplayName(certificate.policy.ContentType)),
                    Enabled = certificate.policy.Enabled,
                    Exportable = certificate.policy.Exportable,
                    CertificateTransparency = certificate.policy.CertificateTransparency,
                    CertificateType = certificate.policy.CertificateType,
                    KeySize = certificate.policy.KeySize,
                    KeyType = EnumHelper.GetDisplayName(certificate.policy.KeyType),
                    ReuseKey = certificate.policy.ReuseKey,
                    ValidityInMonths = certificate.policy.ValidityInMonths
                };
            return certificate.policy.LifetimeActions is null
                ? policy
                : new AzureCertificateNamespace.CertificatePolicy(certificate.IssuerName, certificate.Subject, sans)
                {
                    ContentType = new AzureCertificateNamespace.CertificateContentType(EnumHelper.GetDisplayName(certificate.policy.ContentType)),
                    Enabled = certificate.policy.Enabled,
                    Exportable = certificate.policy.Exportable,
                    CertificateTransparency = certificate.policy.CertificateTransparency,
                    CertificateType = certificate.policy.CertificateType,
                    KeySize = certificate.policy.KeySize,
                    KeyType = EnumHelper.GetDisplayName(certificate.policy.KeyType),
                    LifetimeActions =
                    {
                        certificate.policy.LifetimeActions is null
                            ? null
                            : new AzureCertificateNamespace.LifetimeAction(
                                certificate.policy.LifetimeActions.ActionType == CertificatePolicyActionType.AutoRenew
                                    ? AzureCertificateNamespace.CertificatePolicyAction.AutoRenew
                                    : AzureCertificateNamespace.CertificatePolicyAction.EmailContacts)
                            {
                                LifetimePercentage = certificate.policy.LifetimeActions.LifetimePercentage,
                                DaysBeforeExpiry = certificate.policy.LifetimeActions.DaysBeforeExpiry
                            }
                    },
                    ReuseKey = certificate.policy.ReuseKey,
                    ValidityInMonths = certificate.policy.ValidityInMonths
                };
        }
        else
        {
            var policy = new AzureCertificateNamespace.CertificatePolicy(certificate.IssuerName, certificate.Subject)
            {
                ContentType = new AzureCertificateNamespace.CertificateContentType(EnumHelper.GetDisplayName(certificate.policy.ContentType)),
                Enabled = certificate.policy.Enabled,
                Exportable = certificate.policy.Exportable,
                CertificateTransparency = certificate.policy.CertificateTransparency,
                CertificateType = certificate.policy.CertificateType,
                KeySize = certificate.policy.KeySize,
                KeyType = EnumHelper.GetDisplayName(certificate.policy.KeyType),
                ReuseKey = certificate.policy.ReuseKey,
                ValidityInMonths = certificate.policy.ValidityInMonths
            };
            return certificate.policy.LifetimeActions is null
                ? policy
                : new AzureCertificateNamespace.CertificatePolicy(certificate.IssuerName, certificate.Subject)
                {
                    ContentType = new AzureCertificateNamespace.CertificateContentType(EnumHelper.GetDisplayName(certificate.policy.ContentType)),
                    Enabled = certificate.policy.Enabled,
                    Exportable = certificate.policy.Exportable,
                    CertificateTransparency = certificate.policy.CertificateTransparency,
                    CertificateType = certificate.policy.CertificateType,
                    KeySize = certificate.policy.KeySize,
                    KeyType = EnumHelper.GetDisplayName(certificate.policy.KeyType),
                    LifetimeActions =
                    {
                        certificate.policy.LifetimeActions is null
                            ? null
                            : new AzureCertificateNamespace.LifetimeAction(
                                certificate.policy.LifetimeActions.ActionType == CertificatePolicyActionType.AutoRenew
                                    ? AzureCertificateNamespace.CertificatePolicyAction.AutoRenew
                                    : AzureCertificateNamespace.CertificatePolicyAction.EmailContacts)
                            {
                                LifetimePercentage = certificate.policy.LifetimeActions.LifetimePercentage,
                                DaysBeforeExpiry = certificate.policy.LifetimeActions.DaysBeforeExpiry
                            }
                    },
                    ReuseKey = certificate.policy.ReuseKey,
                    ValidityInMonths = certificate.policy.ValidityInMonths
                };
        }
    }
}