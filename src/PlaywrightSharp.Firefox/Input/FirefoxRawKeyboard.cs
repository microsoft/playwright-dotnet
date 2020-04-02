using System.Threading.Tasks;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Firefox.Input
{
    internal class FirefoxRawKeyboard : IRawKeyboard
    {
        public FirefoxRawKeyboard(FirefoxSession session)
        {
        }

        public Task SendTextAsync(string text)
        {
            throw new System.NotImplementedException();
        }
    }
}
