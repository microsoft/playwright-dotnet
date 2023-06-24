using System.Globalization;
using System.Reflection;
using System.Text;

namespace Microsoft.Playwright.Tests;

public class ConventionTests
{
    // To ensure that public method are not inlined by the new tiered PGO JIT mode,
    // we need to mark them with [MethodImpl(MethodImplOptions.NoInlining)]
    [Test]
    public void EnsurePublicMethodsAreNotInlined()
    {
        var assembly = typeof(Playwright).Assembly;

        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.BaseType != null && t.BaseType.Name == "ChannelOwnerBase");

        var failedAssertions = new StringBuilder();

        foreach (var type in types)
        {
            var methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => m.Name != "Dispose" && m.Name != "DisposeAsync" && m.Name != "ToString");

            foreach (var method in methods)
            {
                if (!method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining))
                {
#pragma warning disable CA1305
                    failedAssertions.AppendLine($"{type.Name}.{method.Name} is not marked with [MethodImpl(MethodImplOptions.NoInlining)]");
#pragma warning restore CA1305
                }
            }
        }

        if (failedAssertions.Length > 0)
        {
            throw new AssertionException(failedAssertions.ToString());
        }
    }
}
