using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using AzureKeyVaultEmulator.Aspire.Client;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.ExtensionHost;

public class KeyVaultClientContainer
{
    private readonly KeyClient _keyClient;
    private readonly CertificateClient _certificateClient;
    public KeyVaultClientContainer(string keyvaultUri)
    {
        
        #if DEBUG
        TokenCredential credentials = new EmulatedTokenCredential(keyvaultUri);
        var options = new CertificateClientOptions()
        {
            DisableChallengeResourceVerification = true
        };
        _keyClient = new KeyClient(new Uri(keyvaultUri), credentials);
        _certificateClient = new CertificateClient(new Uri(keyvaultUri), credentials, options);
        #else
        TokenCredential credentials = new DefaultAzureCredential();
        _keyClient = new KeyClient(new Uri(keyvaultUri), credentials);
        _certificateClient = new CertificateClient(new Uri(keyvaultUri), credentials);
        #endif
        
    }
    
    public KeyClient KeyClient => _keyClient;
    public CertificateClient CertificateClient => _certificateClient;
}