using System.Collections.Generic;

namespace PlaywrightSharp.Helpers
{
    internal static class ArrayExtensions
    {
        public static T[] InsertAt<T>(this T[] array, int index, T value)
        {
            var list = new List<T>(array);
            list.Insert(index, value);
            return list.ToArray();
        }
    }
}
