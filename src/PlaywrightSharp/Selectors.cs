using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    /// <summary>
    /// Selectors can be used to install custom selector engines.
    /// </summary>
    public class Selectors
    {
        private readonly List<SelectorsOwner> _channels = new List<SelectorsOwner>();
        private readonly List<SelectorsRegisterParams> _registrations = new List<SelectorsRegisterParams>();

        internal Selectors()
        {
        }

        internal static Selectors SharedSelectors { get; } = new Selectors();

        /// <summary>
        /// Registers a new selector engine.
        /// </summary>
        /// <param name="name">Name that is used in selectors as a prefix, e.g. {name: 'foo'} enables foo=myselectorbody selectors. May only contain [a-zA-Z0-9_] characters.</param>
        /// <param name="script">Script that evaluates to a selector engine instance.</param>
        /// <param name="path">Path to the JavaScript file. If path is a relative path, then it is resolved relative to current working directory.</param>
        /// <param name="content">Raw script content.</param>
        /// <param name="contentScript">Whether to run this selector engine in isolated JavaScript environment. This environment has access to the same DOM, but not any JavaScript objects from the frame's scripts. Defaults to false. Note that running as a content script is not guaranteed when this engine is used together with other registered engines.</param>
        /// <returns>A <see cref="Task"/> that completes when the engine is registered.</returns>
        public async Task RegisterAsync(string name, string script = null, string path = null, string content = null, bool? contentScript = null)
        {
            if (string.IsNullOrEmpty(script))
            {
                script = ScriptsHelper.EvaluationScript(content, path);
            }

            var registerParam = new SelectorsRegisterParams
            {
                Name = name,
                Source = $"({script})(undefined)",
                ContentScript = contentScript,
            };

            var tasks = new List<Task>();
            foreach (var channel in _channels)
            {
                tasks.Add(channel.RegisterAsync(registerParam));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            _registrations.Add(registerParam);
        }

        internal async Task AddChannelAsync(SelectorsOwner channel)
        {
            _channels.Add(channel);
            var tasks = new List<Task>();
            foreach (var registration in _registrations)
            {
                tasks.Add(channel.RegisterAsync(registration));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }
}
