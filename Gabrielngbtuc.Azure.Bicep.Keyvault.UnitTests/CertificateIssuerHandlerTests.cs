using System.Text.Json;
using Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Handlers;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.UnitTests;

[TestClass]
public class CertificateIssuerHandlerTests : TestContainerClass
{
    public static CertificateIssuerHandler handler = new();
    public static IEnumerable<CertificateIssuer> GetTestData
    {
        get
        {
            return
            [
                new CertificateIssuer("TestDigicert", new DigicertCredentials(
                    "test", "test", "test")),
                new CertificateIssuer("TestGlobalSign", new GlobalSignCredentials(
                    "test", "test", "test@test.test",
                    "1234567890", "testAccount", "test"))
            ];
        }
    }
    
    [TestMethod]
    [DynamicData(nameof(GetTestData))]
    public async Task TestGet(CertificateIssuer issuerToRetrieve)
    {
        await RunContainerTest(async _ =>
        {
            var createResult = await handler.CreateOrUpdate(
                HandlerHelper.GetResourceSpecification(issuerToRetrieve),
                CancellationToken.None
            );

            var getResponse = await handler.Get(
                HandlerHelper.GetResourceReference(new CertificateIssuerHandler.Identifiers(issuerToRetrieve.Name)),
                CancellationToken.None
            );

            var issuerResponse =
                (getResponse?.Resource?.Properties).Deserialize<CertificateIssuer>(
                    HandlerHelper.JsonSerializerOptions
                );
            
            Assert.IsNotNull(issuerResponse);
            Assert.AreEqual(issuerToRetrieve.Name, issuerResponse.Name);
            Assert.AreEqual(issuerToRetrieve.Provider.AccountId, issuerResponse.Provider.AccountId);
            
        });
    }
}