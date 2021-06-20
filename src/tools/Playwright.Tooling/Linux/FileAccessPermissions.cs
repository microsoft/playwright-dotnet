using System;

namespace DriverDownloader.Linux
{
    [Flags]
    internal enum FileAccessPermissions : uint
    {
        OtherExecute = 1,
        OtherWrite = 2,
        OtherRead = 4,

        GroupExecute = 8,
        GroupWrite = 16,
        GroupRead = 32,

        UserExecute = 64,
        UserWrite = 128,
        UserRead = 256,
    }
}
