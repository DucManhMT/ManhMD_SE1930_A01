using System.Reflection;
using Xunit;

namespace FUNews.Client.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Frontend_ShouldNotReference_EntityFrameworkOrDatabase()
    {
        var frontendAssemblies = new[]
        {
            typeof(ManhMD_SE1930_A01_FE.Pages.IndexModel).Assembly,
            typeof(FUNews.Client.BusinessLogic.ClientBusinessLogicMarker).Assembly,
            typeof(FUNews.Client.DataAccess.ClientDataAccessMarker).Assembly
        };

        var forbiddenTokens = new[]
        {
            "EntityFramework",
            "Microsoft.EntityFrameworkCore",
            "FUNews.DataAccess",
            "System.Data.SqlClient",
            "Microsoft.Data.SqlClient"
        };

        foreach (var assembly in frontendAssemblies)
        {
            var referencedAssemblies = assembly.GetReferencedAssemblies();

            foreach (var referenced in referencedAssemblies)
            {
                foreach (var forbidden in forbiddenTokens)
                {
                    Assert.DoesNotContain(forbidden, referenced.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void Frontend_Assemblies_ShouldTargetNet8()
    {
        var frontendAssemblies = new[]
        {
            typeof(ManhMD_SE1930_A01_FE.Pages.IndexModel).Assembly,
            typeof(FUNews.Client.BusinessLogic.ClientBusinessLogicMarker).Assembly,
            typeof(FUNews.Client.DataAccess.ClientDataAccessMarker).Assembly
        };

        foreach (var assembly in frontendAssemblies)
        {
            var targetFrameworkAttr = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();
            Assert.NotNull(targetFrameworkAttr);
            Assert.Contains(".NETCoreApp,Version=v8.0", targetFrameworkAttr.FrameworkName);
        }
    }
}
