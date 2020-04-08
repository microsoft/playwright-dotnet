namespace PlaywrightSharp.Firefox.Protocol.Page
{
    internal partial class DOMQuad
    {
        public static implicit operator Quad[](DOMQuad quad)
            => new[]
            {
                new Quad { X = quad.P1.X.Value, Y = quad.P1.Y.Value },
                new Quad { X = quad.P2.X.Value, Y = quad.P2.Y.Value },
                new Quad { X = quad.P3.X.Value, Y = quad.P3.Y.Value },
                new Quad { X = quad.P4.X.Value, Y = quad.P4.Y.Value },
            };
    }
}
