
using AzureKeyVaultEmulator.TestContainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.UnitTests;

public class TestContainerClass
{
    public AzureKeyVaultEmulatorContainer GetKeyvaultEmulatorContainer()
    {
        var container = new AzureKeyVaultEmulatorContainer("D:/KeyvaultEmulator", false);
        return container;
    }
}