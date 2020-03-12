using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    internal interface IProtocolTypesGenerator
    {
        Task GenerateTypesAsync(RevisionInfo revision);
    }
}
