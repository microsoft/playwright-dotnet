using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Protocol.Page;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Firefox.Input
{
    internal class FirefoxRawKeyboard : IRawKeyboard
    {
        private readonly FirefoxSession _session;

        public FirefoxRawKeyboard(FirefoxSession session) => _session = session;

        public Task SendTextAsync(string text) => _session.SendAsync(new PageInsertTextRequest { Text = text });

        public Task KeyDownAsync(Modifier[] modifiers, string code, int keyCode, int keyCodeWithoutLocation, string key, double location, bool autoRepeat, string text)
        {
            if (code == "MetaLeft")
            {
                code = "OSLeft";
            }
            else if (code == "MetaRight")
            {
                code = "OSRight";
            }

            return _session.SendAsync(new PageDispatchKeyEventRequest
            {
                Type = "keydown",
                KeyCode = keyCodeWithoutLocation,
                Code = code,
                Key = key,
                Repeat = autoRepeat,
                Location = location,
            });
        }

        public Task KeyUpAsync(Modifier[] modifiers, string code, int keyCode, int keyCodeWithoutLocation, string key, double location)
        {
            if (code == "MetaLeft")
            {
                code = "OSLeft";
            }
            else if (code == "MetaRight")
            {
                code = "OSRight";
            }

            return _session.SendAsync(new PageDispatchKeyEventRequest
            {
                Type = "keyup",
                KeyCode = keyCodeWithoutLocation,
                Code = code,
                Key = key,
                Location = location,
                Repeat = false,
            });
        }
    }
}
