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
        var container = GetKeyvaultEmulatorContainer();
        await container.StartAsync();
        try
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
            Assert.IsTrue(getResponse != null && getResponse.ErrorData?.Error.Details != null && getResponse.ErrorData.Error.Details.Any(d => d.Code == "400"));
        }
        catch
        {
            throw;
        }
        finally
        {
            await container.StopAsync();
            await container.DisposeAsync();
            await Task.Delay(5000);
        }
    }
    
    [TestMethod]
    public void TestUpdate()
    {
        
    }
    
    
    [TestMethod]
    public async Task TestGet()
    {
        var container = GetKeyvaultEmulatorContainer();
        await container.StartAsync();
        try
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
            Assert.IsTrue( contactsResponse.Contacts.Any(c => c == contactToMatch));
        }
        catch
        {
            throw;
        }
        finally
        {
            await container.StopAsync();
            await container.DisposeAsync();
            await Task.Delay(5000);
        }
    }
    
    
}