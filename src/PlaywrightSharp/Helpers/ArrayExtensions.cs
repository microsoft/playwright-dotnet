namespace PlaywrightSharp.Helpers
{
    internal static class ArrayExtensions
    {
        public static T[] Prepend<T>(this T[] array, params T[] values)
        {
            var newArray = new T[array.Length + values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                newArray[i] = values[i];
            }

            array.CopyTo(newArray, values.Length);
            return newArray;
        }
    }
}
