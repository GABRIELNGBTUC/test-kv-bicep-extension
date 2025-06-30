using System.Text.Json;
using Azure;
using Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Handlers;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.UnitTests;

[TestClass]
public class CertificateContactsHandlerTests : TestContainerClass
{
    public static CertificateContactsHandler handler = new();

    [TestMethod]
    public async Task TestDelete()
    {
        await RunContainerTest(async _ =>
        {
            Contact contactToMatch = new Contact("gettest@gmail.com", "GetTest", "1234567890");
            var newContact = new CertificateContacts([
                contactToMatch
            ]);
            await handler.CreateOrUpdate(
                HandlerHelper.GetResourceSpecification(newContact),
                CancellationToken.None
            );

            await handler.Delete(
                HandlerHelper.GetResourceReference(new CertificateContactsHandler.Identifiers()),
                CancellationToken.None
            );

            var getResponse = await handler.Get(
                HandlerHelper.GetResourceReference(new CertificateContactsHandler.Identifiers()),
                CancellationToken.None
            );

            var contactsResponse =
                (getResponse?.Resource?.Properties).Deserialize<CertificateContacts>(
                    HandlerHelper.JsonSerializerOptions);
            //Keyvault emulator throws when no element is found for the get operation
            Assert.IsTrue(getResponse != null && getResponse.ErrorData?.Error.Details != null &&
                          getResponse.ErrorData.Error.Details.Any(d => d.Code == "400"));
        });
    }

    [TestMethod]
    public async Task TestUpdate()
    {
        await RunContainerTest(async _ =>
        {
            Contact[] contactsToMatch =
            [
                new Contact("gettest@gmail.com", "GetTest", "1234567890"),
                new Contact("gettest2@gmail.com", "GetTest2", "1234567890")
            ];
            var newContact = new CertificateContacts(contactsToMatch);
            await handler.CreateOrUpdate(
                HandlerHelper.GetResourceSpecification(newContact),
                CancellationToken.None
            );

            var getResponse = await handler.Get(
                HandlerHelper.GetResourceReference(new CertificateContactsHandler.Identifiers()),
                CancellationToken.None
            );

            var contactsResponse =
                (getResponse?.Resource?.Properties).Deserialize<CertificateContacts>(
                    HandlerHelper.JsonSerializerOptions);
            Assert.IsNotNull(contactsResponse);
            Assert.IsTrue(
                contactsResponse.Contacts.All(c => contactsToMatch.Any(c2 => c2 == c))
            );
        });
    }


    [TestMethod]
    public async Task TestGet()
    {
        await RunContainerTest(async _ =>
        {
            Contact contactToMatch = new Contact("gettest@gmail.com", "GetTest", "1234567890");
            var newContact = new CertificateContacts([
                contactToMatch
            ]);
            await handler.CreateOrUpdate(
                HandlerHelper.GetResourceSpecification(newContact),
                CancellationToken.None
            );

            var getResponse = await handler.Get(
                HandlerHelper.GetResourceReference(new CertificateContactsHandler.Identifiers()),
                CancellationToken.None
            );

            var contactsResponse =
                (getResponse?.Resource?.Properties).Deserialize<CertificateContacts>(
                    HandlerHelper.JsonSerializerOptions);

            Assert.IsNotNull(contactsResponse);
            Assert.IsTrue(contactsResponse.Contacts.Any(c => c == contactToMatch));
        });
    }

    [TestMethod]

    public async Task ShouldNotAllowEmptyContacts()
    {
        await RunContainerTest(async _ =>
        {
            var result = await handler.CreateOrUpdate(
                HandlerHelper.GetResourceSpecification(new CertificateContacts([])),
                CancellationToken.None
            );
            
            Assert.IsTrue(result.ErrorData?.Error.Details != null &&
                          result.ErrorData.Error.Code == nameof(RequestFailedException));
        });
    }
}