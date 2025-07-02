using System.Net;
using System.Text.Json;
using Azure;
using Bicep.Local.Extension.Protocol;
using Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Helpers;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Handlers;

public class CertificateIssuerHandler : IResourceHandler
{
    public string ResourceType => nameof(CertificateIssuer);

    public record Identifiers(
        string? Name);

    public Task<LocalExtensionOperationResponse> CreateOrUpdate(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async clientContainer =>
        {
            var properties = RequestHelper.GetProperties<CertificateIssuer>(request.Properties);

            var issuerExists = false;

            try
            {
                var issuer = await clientContainer.CertificateClient
                    .GetIssuerAsync(properties.Name, cancellationToken: cancellationToken);
                if (issuer is { HasValue: true })
                {
                    issuerExists = true;
                }
            }
            catch (RequestFailedException requestFailedException)
            {
                if (requestFailedException.Status == (int)HttpStatusCode.NotFound)
                {
                    //Issuer does not exist
                    Console.WriteLine("Issuer does not exist");
                }
                else
                {
                    throw;
                }
            }

            if (issuerExists)
            {
                //Update
                Console.WriteLine("Updating issuer");
                var azureIssuer = MapFromCertificateIssuerCredentials(properties.Provider, properties.Name);
                Console.WriteLine("Azure issuer mapped");
                
                
                
                var result = await clientContainer.CertificateClient.UpdateIssuerAsync(azureIssuer, cancellationToken: cancellationToken);
                Console.WriteLine("Updated issuer");
                return RequestHelper.CreateSuccessResponse(request, properties with
                {
                    Provider = MapToCertificateIssuerCredentials(result)
                }, new Identifiers(properties.Name));
            }

            else
            {
                //Create
                Console.WriteLine("Creating issuer");
                var azureIssuer = MapFromCertificateIssuerCredentials(properties.Provider, properties.Name);
                
                Console.WriteLine("Azure issuer mapped");
                Console.WriteLine(JsonSerializer.Serialize(azureIssuer, Program.JsonSerializerOptions));   
                var result = await clientContainer.CertificateClient.CreateIssuerAsync(azureIssuer, cancellationToken: cancellationToken);
                
                Console.WriteLine("Created issuer");
                Console.WriteLine(JsonSerializer.Serialize(result.Value, Program.JsonSerializerOptions));
                
                return RequestHelper.CreateSuccessResponse(request, properties with
                {
                    Provider = MapToCertificateIssuerCredentials(result)
                }, new Identifiers(properties.Name));
            }
        });

    public Task<LocalExtensionOperationResponse> Preview(ResourceSpecification request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<LocalExtensionOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async clientContainer =>
        {
            var name = RequestHelper.GetIdentifierData(request, nameof(CertificateIssuer.Name))?.ToString();
            
            var azureIssuer = await clientContainer.CertificateClient.GetIssuerAsync(name, cancellationToken: cancellationToken);

            var certificateIssuerCredentials = MapToCertificateIssuerCredentials(azureIssuer);
            var certificateIssuer = new CertificateIssuer(name!, certificateIssuerCredentials);
            
            return RequestHelper.CreateSuccessResponse(request, certificateIssuer, new Identifiers(name));
        });

    public Task<LocalExtensionOperationResponse> Delete(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async clientContainer =>
        {
            var name = RequestHelper.GetIdentifierData(request, nameof(CertificateIssuer.Name))?.ToString();
            
            await clientContainer.CertificateClient.DeleteIssuerAsync(name, cancellationToken: cancellationToken);
            
            return RequestHelper.CreateSuccessResponse(request,  request, new Identifiers(name));
        });

    public static CertificateIssuerCredential MapToCertificateIssuerCredentials(AzureCertificateNamespace.CertificateIssuer issuer)
    {
        if (issuer.Provider == "DigiCert")
        {
            return new DigicertCredentials(
                issuer.OrganizationId, issuer.AccountId, null);
        }
        else if (issuer.Provider == "GlobalSign")
        {
            var administratorInfo = issuer.AdministratorContacts.FirstOrDefault();
            return new GlobalSignCredentials(
                administratorInfo?.FirstName, administratorInfo?.LastName, administratorInfo?.Email,
                administratorInfo?.Phone, issuer.AccountId, null);
        }
        else
        {
            return new UnknownCredentials(issuer.Provider);
        }
    }
    
    public static AzureCertificateNamespace.CertificateIssuer MapFromCertificateIssuerCredentials(
        CertificateIssuerCredential credential, string issuerName)
    {
        return credential switch
        {
            DigicertCredentials digicertCredentials => new AzureCertificateNamespace.CertificateIssuer(issuerName,
                EnumHelper.GetDisplayName(CertificateIssuerProvider.DigiCert))
            {
                OrganizationId = digicertCredentials?.OrganizationId,
                AccountId = digicertCredentials?.AccountId,
                Password = digicertCredentials?.Password
            },
            GlobalSignCredentials globalSignCredentials => new AzureCertificateNamespace.CertificateIssuer(issuerName,
                EnumHelper.GetDisplayName(CertificateIssuerProvider.GlobalSign))
            {
                AccountId = globalSignCredentials?.AccountId,
                Password = globalSignCredentials?.Password,
                AdministratorContacts =
                {
                    new AzureCertificateNamespace.AdministratorContact
                    {
                        FirstName = globalSignCredentials?.FirstName,
                        LastName = globalSignCredentials?.LastName,
                        Email = globalSignCredentials?.Email,
                        Phone = globalSignCredentials?.Phone
                    }
                }
            },
            UnknownCredentials => throw new InvalidOperationException(
                "The provider 'Unknown' is not supported for reverse mapping."),
            _ => throw new NotSupportedException($"The provider sent to the handler is not recognized.")
        };
    }
}