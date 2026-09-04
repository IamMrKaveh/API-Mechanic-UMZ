using System.Reflection;
using System.Runtime.CompilerServices;

namespace Tests.Domain;

public class DomainAssemblyAttributeTests
{
    [Fact]
    public void DomainAssembly_ExposesInternalsToTests()
    {
        var attributes = typeof(global::Domain.Wishlist.Aggregates.Wishlist)
            .Assembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(a => a.AssemblyName)
            .ToList();

        attributes.ShouldContain("Tests");
    }
}
