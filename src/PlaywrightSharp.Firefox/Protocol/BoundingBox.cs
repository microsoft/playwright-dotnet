namespace PlaywrightSharp.Firefox.Protocol.Page
{
    internal partial class BoundingBox
    {
        public static implicit operator BoundingBox(Rect rect)
            => rect == null ? null : new BoundingBox
            {
                X = rect.X,
                Y = rect.Y,
                Height = rect.Height,
                Width = rect.Width,
            };

        public static implicit operator Rect(BoundingBox boundingBox)
            => boundingBox == null ? null : new Rect
            {
                X = (double)boundingBox.X,
                Y = (double)boundingBox.Y,
                Height = (double)boundingBox.Height,
                Width = (double)boundingBox.Width,
            };
    }
}
