namespace PlaywrightSharp
{
    public partial class ElementHandleBoundingBoxResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ElementHandleBoundingBoxResult"/> class.
        /// </summary>
        /// <param name="x"><para>the x coordinate of the element in pixels.</para></param>
        /// <param name="y"><para>the y coordinate of the element in pixels.</para></param>
        /// <param name="width"><para>the width of the element in pixels.</para></param>
        /// <param name="height"><para>the height of the element in pixels.</para></param>
        public ElementHandleBoundingBoxResult(float x = 0, float y = 0, float width = 0, float height = 0)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
