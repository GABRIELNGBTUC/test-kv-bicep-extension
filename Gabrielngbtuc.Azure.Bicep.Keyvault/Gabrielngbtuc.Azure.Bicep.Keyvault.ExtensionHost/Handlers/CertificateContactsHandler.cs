using System.Text.Json;
using Azure;
using Bicep.Local.Extension.Protocol;
using Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Helpers;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Handlers;

public class CertificateContactsHandler : IResourceHandler
{
    public string ResourceType => nameof(CertificateContacts);

    public record Identifiers();

    public Task<LocalExtensionOperationResponse> CreateOrUpdate(ResourceSpecification request,
        CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async clientContainer =>
        {
            Console.WriteLine(JsonSerializer.Serialize(request, Program.JsonSerializerOptions));
            var properties = RequestHelper.GetProperties<CertificateContacts?>(request.Properties);
            Console.WriteLine("Deserialized properties for CertificateContacts:");
            Console.WriteLine(JsonSerializer.Serialize(properties, Program.JsonSerializerOptions));

            if (properties?.Contacts is null)
            {
                RequestHelper.CreateErrorResponse("invalid-request", "Contacts cannot be null");
            }

            else if (properties.Contacts.Length == 0)
            {
                await clientContainer.CertificateClient.DeleteContactsAsync(cancellationToken: cancellationToken);

            }
            else
            {
                var response = await clientContainer.CertificateClient.SetContactsAsync(
                    properties.Contacts.Select(c => new AzureCertificateNamespace.CertificateContact()
                    {
                        Name = c.Name,
                        Email = c.Email,
                        Phone = c.Phone
                    }), cancellationToken: cancellationToken
                );
            }



            return RequestHelper.CreateSuccessResponse(request, properties,
                new Identifiers());
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
                new Identifiers());
        });

    public Task<LocalExtensionOperationResponse> Get(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async client =>
        {
            var contacts = await client.CertificateClient.GetContactsAsync(cancellationToken);

            var bicepContacts = new CertificateContacts(
                contacts.Value.Select(c => new Contact(c.Email, c.Name, c.Phone)).ToArray()
            );

            return RequestHelper.CreateSuccessResponse(request, bicepContacts, request.Identifiers);
        });

    public Task<LocalExtensionOperationResponse> Delete(ResourceReference request, CancellationToken cancellationToken)
        => RequestHelper.HandleRequest(request.Config, async clientContainer =>
        {
            var contactToDelete = RequestHelper.GetIdentifierData(request, nameof(Contact.Name))?.ToString();

            await clientContainer.CertificateClient.DeleteContactsAsync(cancellationToken);

            return RequestHelper.CreateSuccessResponse(request, new CertificateContacts([]), request.Identifiers);
        });
}