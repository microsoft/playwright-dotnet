using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Playwright.Tests;

public class ConventionTests
{
    // To ensure that public method are not inlined by the nwe PGO guided tiered JIT,
    // we need to mark them with [MethodImpl(MethodImplOptions.NoInlining)]
    [Test]
    public void EnsurePublicMethodsAreNotInlined()
    {
        var assembly = typeof(Playwright).Assembly;

        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.BaseType != null && t.BaseType.Name == "ChannelOwnerBase");

        foreach (var type in types)
        {
            var methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => m.Name != "Dispose" && m.Name != "DisposeAsync" && m.Name != "ToString");

            foreach (var method in methods)
            {
                Assert.True(method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), $"{type.Name}.{method.Name} is not marked with [MethodImpl(MethodImplOptions.NoInlining)]");
            }
        }
    }
}
