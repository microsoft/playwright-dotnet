using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightSharp
{
    /// <summary>
    /// ...
    /// </summary>
    public partial interface IPlaywright : IDisposable
    {
        /// <summary>
        /// Gets a <see cref="IBrowserType"/>.
        /// </summary>
        /// <param name="browserType"><see cref="IBrowserType"/> name. You can get the names from <see cref="BrowserType"/>.
        /// e.g.: <see cref="BrowserType.Chromium"/>, <see cref="BrowserType.Firefox"/> or <see cref="BrowserType.Webkit"/>.
        /// </param>
        IBrowserType this[string browserType] { get; }
    }
}
