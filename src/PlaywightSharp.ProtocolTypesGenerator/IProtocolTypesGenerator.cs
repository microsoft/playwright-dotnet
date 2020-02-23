using System.Threading.Tasks;

namespace PlaywrightSharp.ProtocolTypesGenerator
{
    public interface IProtocolTypesGenerator
    {
        Task GenerateTypesAsync(RevisionInfo revision);
    }
}
