using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright
{
    internal partial class Selectors : ISelectors
    {
        private readonly List<SelectorsOwner> _channels = new List<SelectorsOwner>();
        private readonly List<SelectorsRegisterParams> _registrations = new List<SelectorsRegisterParams>();

        internal Selectors()
        {
        }

        internal static Selectors SharedSelectors { get; } = new Selectors();

        public async Task RegisterAsync(string name, string script, string path, bool? contentScript = null)
        {
            script = ScriptsHelper.EvaluationScript(script, path, false);

            var registerParam = new SelectorsRegisterParams
            {
                Name = name,
                Source = script,
                ContentScript = contentScript,
            };

            var tasks = new List<Task>();
            foreach (var channel in _channels)
            {
                tasks.Add(channel.RegisterAsync(registerParam));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.Message.Contains("Connection closed"))
            {
                // Ignore connection closed exceptions.
            }

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
            catch (Exception ex) when (ex.Message.Contains("Connection closed"))
            {
                // Ignore connection closed exceptions.
            }
        }
    }
}
