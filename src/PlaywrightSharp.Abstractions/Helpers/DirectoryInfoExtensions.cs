using System.IO;

namespace PlaywrightSharp.Helpers
{
    internal static class DirectoryInfoExtensions
    {
        public static void CreateIfNotExists(this DirectoryInfo dir)
        {
            if (!dir.Exists)
            {
                dir.Create();
            }
        }
    }
}
