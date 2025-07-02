using System.Runtime.ConstrainedExecution;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using AzureKeyVaultEmulator.Aspire.Client;
using Bicep.Local.Extension.Protocol;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.UnitTests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task TestDeserialization()
    {
        // var realClient = new CertificateClient(new Uri("https://tr-kv-dev-devops-testing.vault.azure.net/"), new DefaultAzureCredential());
        // TokenCredential credentials = new EmulatedTokenCredential("https://localhost:4997/");
        // var options = new CertificateClientOptions()
        // {
        //     DisableChallengeResourceVerification = true
        // };
        // var fakeClient = new CertificateClient(new Uri("https://localhost:4997/"), credentials, options);
        // var certificatePolicy = new CertificatePolicy("Self", "CN=localhost")
        // {
        //     Exportable = true
        // };
        // Console.WriteLine("Creating real certificate");
        // var realCert = await realClient.StartCreateCertificateAsync("testUpdate", certificatePolicy);
        // await realCert.WaitForCompletionAsync();
        // Console.WriteLine("Creating fake certificate");
        // var fakeCert = await fakeClient.StartCreateCertificateAsync("testUpdate", certificatePolicy);
        // await fakeCert.WaitForCompletionAsync();
        // var certProperties = new CertificateProperties(realCert.Value.Id)
        // {
        //     Enabled = false
        // };
        //
        // var fakecertProperties = new CertificateProperties(fakeCert.Value.Id)
        // {
        //     Enabled = false
        // };
        //
        // certificatePolicy.Exportable = false;
        //
        // Console.WriteLine("Updating real certificate policy");
        // await realClient.UpdateCertificatePolicyAsync("testUpdate", certificatePolicy);
        // Console.WriteLine("Updating fake certificate policy");
        // await fakeClient.UpdateCertificatePolicyAsync("testUpdate", certificatePolicy);
        //
        // Console.WriteLine("Updating real certificate properties");
        // await realClient.UpdateCertificatePropertiesAsync(certProperties);
        // Console.WriteLine("Updating fake certificate properties");
        // await fakeClient.UpdateCertificatePropertiesAsync(fakecertProperties);
    }
    
    [TestMethod]
    public async Task TestHandler()
    {

    }
}