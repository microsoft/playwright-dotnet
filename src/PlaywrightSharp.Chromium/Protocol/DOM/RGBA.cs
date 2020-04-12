using System.Drawing;
using System.Runtime;

namespace PlaywrightSharp.Chromium.Protocol.DOM
{
    internal partial class RGBA
    {
        public static implicit operator RGBA(Color color)
            => new RGBA
            {
                A = color.A,
                R = color.R,
                B = color.B,
                G = color.G,
            };

        public static implicit operator Color(RGBA rgba)
            => rgba == null ? default : Color.FromArgb((int)rgba.A, (int)rgba.R, (int)rgba.G, (int)rgba.B);
    }
}
