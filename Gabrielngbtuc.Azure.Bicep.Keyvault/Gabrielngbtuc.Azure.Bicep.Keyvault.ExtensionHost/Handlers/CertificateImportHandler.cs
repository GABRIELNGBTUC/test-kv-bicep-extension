using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Azure;
using Bicep.Local.Extension.Protocol;
using Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Helpers;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Handlers;

public class CertificateImportHandler : IResourceHandler
{
    public string ResourceType => nameof(CertificateImport);

    private record Identifiers(
        string? Name);

    public Task<LocalExtensionOperationResponse> CreateOrUpdate(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async clientContainer =>
        {
            Console.WriteLine(JsonSerializer.Serialize(request, Program.JsonSerializerOptions));
            var properties = RequestHelper.GetProperties<CertificateImport>(request.Properties);
            Console.WriteLine("Deserialized properties");
            var certificateExists = false;

            try
            {
                Console.WriteLine("Trying to get certificate");
                var certificate =
                    await clientContainer.CertificateClient.GetCertificateAsync(properties.Name,
                        cancellationToken: cancellationToken);
                certificateExists = certificate is { HasValue: true };
            }
            catch (RequestFailedException e)
            {
                if (e.ErrorCode == "CertificateNotFound")
                {
                    Console.WriteLine("Certificate does not exist");
                    certificateExists = false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to get certificate with error");
                Console.WriteLine(e);
            }

            if (certificateExists)
            {
                return RequestHelper.CreateErrorResponse("operation-not-supported", "Certificate already exists. Only create operations are supported.");
            }

            var importOptions = new AzureCertificateNamespace.ImportCertificateOptions(properties.Name,
                Convert.FromBase64String(properties.Value));
            if (properties.Password is not null)
            {
                importOptions.Password = properties.Password;
            }
            importOptions.Policy = CertificateHandler.MapToAzureCertificatePolicy(properties);
            importOptions.Enabled = properties.Enabled;
            foreach (var propertiesTag in properties.Tags ?? [])
            {
                importOptions.Tags.Add(propertiesTag.Key, propertiesTag.Value);
            }
            importOptions.PreserveCertificateOrder = properties.PreserveCertificateOrder;

            var result = await clientContainer.CertificateClient.ImportCertificateAsync(importOptions
                , cancellationToken: cancellationToken);

            var certificateData = CertificateHandler.MapToCertificate(properties, result.Value) with
            {
                policy = CertificateHandler.MapToCertificatePolicy(result.Value.Policy)
            };
            
            return RequestHelper.CreateSuccessResponse(request, certificateData, new Identifiers(properties.Name));
        });

    public Task<LocalExtensionOperationResponse> Preview(ResourceSpecification request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [ExcludeFromCodeCoverage]
    public Task<LocalExtensionOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            //Get identifiers
            var name = RequestHelper.GetIdentifierData(request, nameof(Certificate.Name))?.ToString();

            var azureCertificate = await client.CertificateClient.GetCertificateAsync(name, cancellationToken);

            var serializedProperties =
                JsonSerializer.Serialize(azureCertificate.Value.Properties, Program.JsonSerializerOptions);

            var bicepCertificate = new Certificate(name, azureCertificate.Value.Policy.IssuerName,
                azureCertificate.Value.Policy.Subject,
                CertificateHandler.MapToCertificatePolicy(azureCertificate.Value.Policy),
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
}