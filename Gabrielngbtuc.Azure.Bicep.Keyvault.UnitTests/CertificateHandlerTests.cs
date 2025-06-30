using System.Text.Json;
using Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost.Handlers;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.UnitTests;

[TestClass]
public class CertificateHandlerTests : TestContainerClass
{
    public static CertificateHandler handler = new();

    [TestMethod]
    public async Task TestGet()
    {
        await RunContainerTest(async _ =>
        {
            var certificateToRetrieve = new Certificate(
                "TestCert",
                "Self",
                "CN=TestCert",
                new CertificatePolicy(
                    ContentType: CertificateContentType.Pkcs12,
                    KeyType: CertificateKeyType.Rsa,
                    KeySize: 2048,
                    ReuseKey: true,
                    Exportable: true
                ),
                null,
                null,
                null
            );
            await handler.CreateOrUpdate(
                HandlerHelper.GetResourceSpecification(certificateToRetrieve),
                CancellationToken.None
            );

            var getResponse = await handler.Get(
                HandlerHelper.GetResourceReference(
                    new CertificateHandler.Identifiers(certificateToRetrieve.Name, null)
                ), CancellationToken.None
            );

            var certificateResponse =
                (getResponse?.Resource?.Properties).Deserialize<Certificate>(
                    HandlerHelper.JsonSerializerOptions
                );
            Assert.IsNotNull(certificateResponse);
            Assert.AreEqual(certificateToRetrieve.Name, certificateResponse.Name);
            Assert.IsNotNull(certificateResponse.Cer);
        });
    }

    

    [TestMethod]
    [DynamicData(nameof(CreateCertificatesData))]
    public async Task TestCreate(Certificate certificateToCreate)
    {
        await RunContainerTest(async _ =>
        {
            var createResponse = await handler.CreateOrUpdate(
                HandlerHelper.GetResourceSpecification(certificateToCreate),
                CancellationToken.None
            );

            var certificateResponse =
                (createResponse?.Resource?.Properties).Deserialize<Certificate>(
                    HandlerHelper.JsonSerializerOptions
                );
            Assert.IsNotNull(certificateResponse);
            Assert.AreEqual(certificateToCreate.Name, certificateResponse.Name);
            Assert.IsNotNull(certificateResponse.Cer);
            Assert.IsNotNull(certificateResponse.Id);
            Assert.IsNotNull(certificateResponse.KeyId);
            Assert.IsNotNull(certificateResponse.SecretId);
        });
    }

    [TestMethod]
    public async Task TestUpdate()
    {
        await RunContainerTest(async _ =>
        {
            var certificateToRetrieve = new Certificate(
                "TestCert",
                "Self",
                "CN=TestCert",
                new CertificatePolicy(
                    ContentType: CertificateContentType.Pkcs12,
                    KeyType: CertificateKeyType.Rsa,
                    KeySize: 2048,
                    ReuseKey: true,
                    Exportable: false
                ),
                null,
                null,
                null
            );
            await handler.CreateOrUpdate(
                HandlerHelper.GetResourceSpecification(certificateToRetrieve),
                CancellationToken.None
            );

            var updateResponse = await handler.CreateOrUpdate(
                HandlerHelper.GetResourceSpecification(certificateToRetrieve
                    with
                    {
                        policy = certificateToRetrieve.policy with { Exportable = true }
                    }
                ),
                CancellationToken.None
            );


            var certificateResponse =
                (updateResponse?.Resource?.Properties).Deserialize<Certificate>(
                    HandlerHelper.JsonSerializerOptions
                );
            Assert.IsNotNull(certificateResponse);
            Assert.IsTrue(certificateResponse.policy.Exportable);
            Assert.IsNotNull(certificateResponse.Cer);
        });
    }

    #region TestData

    public static IEnumerable<Certificate> CreateCertificatesData
    {
        get
        {
            return new[]
            {
                new Certificate(
                    "TestCert",
                    "Self",
                    "CN=TestCert",
                    new CertificatePolicy(
                        ContentType: CertificateContentType.Pkcs12,
                        KeyType: CertificateKeyType.Rsa,
                        KeySize: 2048,
                        ReuseKey: true,
                        Exportable: false
                    ),
                    null,
                    null,
                    null
                ),
                new Certificate(
                    "TestCert2",
                    "Self",
                    "CN=TestCert2",
                    new CertificatePolicy(
                        ContentType: CertificateContentType.Pkcs12,
                        KeyType: CertificateKeyType.Rsa,
                        KeySize: 2048,
                        ReuseKey: true,
                        Exportable: true,
                        LifetimeActions: new LifetimeAction(CertificatePolicyActionType.AutoRenew,
                            50, 10)
                    ),
                    null,
                    null,
                    null
                ),
                new Certificate(
                    "TestCert",
                    "Self",
                    "CN=TestCert",
                    new CertificatePolicy(
                        ContentType: CertificateContentType.Pkcs12,
                        KeyType: CertificateKeyType.Rsa,
                        KeySize: 2048,
                        ReuseKey: true,
                        Exportable: false,
                        LifetimeActions: new LifetimeAction(CertificatePolicyActionType.EmailContacts,
                            null, null),
                        CertificateTransparency: true,
                        SubjectAlternativeNames: new SubjectAlternativeNamesData([
                                "test.com"
                            ],
                            [
                                "test2@local.com"
                            ],
                            [
                                "test3@upnlocal.com"
                            ])
                    ),
                    null,
                    null,
                    null,
                    Tags: new Dictionary<string, string> { { "test", "test" } }
                ),
            };
        }
    }

    #endregion
    
    
}