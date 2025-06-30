
using AzureKeyVaultEmulator.TestContainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.UnitTests;

public class TestContainerClass
{
    public AzureKeyVaultEmulatorContainer GetKeyvaultEmulatorContainer()
    {
        var container = new AzureKeyVaultEmulatorContainer();
        return container;
    }

    public async Task RunContainerTest(Func<object, Task> function)
    {
        var container = GetKeyvaultEmulatorContainer();
        await container.StartAsync();
        
        try
        {
            await function.Invoke(default);
        }
        catch
        {
            throw;
        }
        finally
        {
            await container.StopAsync();
            await container.DisposeAsync();
        }
    }
}