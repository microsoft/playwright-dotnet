namespace PlaywrightSharp.Firefox.Protocol.Page
{
    internal partial class BoundingBox
    {
        public static implicit operator BoundingBox(Rect rect) => rect == null ? null : new BoundingBox
        {
            X = rect.X,
            Y = rect.Y,
            Height = rect.Height,
            Width = rect.Width,
        };
    }
}
