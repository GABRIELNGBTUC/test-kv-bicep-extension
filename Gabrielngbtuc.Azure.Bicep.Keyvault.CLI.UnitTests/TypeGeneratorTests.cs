using System.Reflection;
using System.Text.Json;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Generator;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.CLI.UnitTests;

[TestClass]
public class TypeGeneratorTests
{
    [TestMethod]
    public void ResourceTypesAreGenerated()
    {
        var types = TypeGenerator.GenerateTypes("az-keyvault-tuc-ext", "1.0.0", typeof(Configuration), typeof(Configuration));
        var serializableTypes =Assembly.GetAssembly(typeof(Configuration)).GetTypes()
                    .Where(t => t.GetCustomAttribute<BicepSerializableType>() != null);
        var indexContent = JsonDocument.Parse(types["index.json"]);
        var resourceKeys = indexContent.RootElement.GetProperty("resources").EnumerateObject().Select(k => k.Name);
        Assert.IsTrue(resourceKeys.All(k => serializableTypes.Any(t => t.Name == k)));
    }
}