using System.Reflection;
using Xunit;

namespace FUNews.Tests;

public class BackendStructureTests
{
    [Fact]
    public void Backend_Assemblies_ShouldTargetNet8()
    {
        var backendAssemblies = new[]
        {
            typeof(FUNews.BusinessLogic.BusinessLogicMarker).Assembly,
            typeof(FUNews.DataAccess.DataAccessMarker).Assembly
        };

        foreach (var assembly in backendAssemblies)
        {
            var targetFrameworkAttr = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            Assert.NotNull(targetFrameworkAttr);
            Assert.Contains(".NETCoreApp,Version=v8.0", targetFrameworkAttr.FrameworkName);
        }
    }

    [Fact]
    public void BusinessLogic_ShouldReference_DataAccess()
    {
        var blAssembly = typeof(FUNews.BusinessLogic.BusinessLogicMarker).Assembly;
        var referencedAssemblies = blAssembly.GetReferencedAssemblies();

        Assert.Contains(referencedAssemblies, r => string.Equals(r.Name, "FUNews.DataAccess", StringComparison.OrdinalIgnoreCase));
    }
}
