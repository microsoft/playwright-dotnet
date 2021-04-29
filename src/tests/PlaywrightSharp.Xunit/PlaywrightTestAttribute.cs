using System;

namespace Microsoft.Playwright.Test.Xunit
{
    /// <summary>
    /// Enables decorating test facts with information about the corresponding test in the upstream repository.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class PlaywrightTestAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the attribute.
        /// </summary>
        /// <param name="fileName"><see cref="FileName"/></param>
        /// <param name="nameOfTest"><see cref="TestName"/></param>
        public PlaywrightTestAttribute(string fileName, string nameOfTest)
        {
            FileName = fileName;
            TestName = nameOfTest;
        }

        /// <summary>
        /// Creates a new instance of the attribute.
        /// </summary>
        /// <param name="fileName"><see cref="FileName"/></param>
        /// <param name="describe"><see cref="Describe"/></param>
        /// <param name="nameOfTest"><see cref="TestName"/></param>
        public PlaywrightTestAttribute(string fileName, string describe, string nameOfTest) : this(fileName, nameOfTest)
        {
            Describe = describe;
        }

        /// <summary>
        /// The file name origin of the test.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Returns the trimmed file name.
        /// </summary>
        public string TrimmedName => FileName.Substring(0, FileName.IndexOf('.'));

        /// <summary>
        /// The name of the test, the decorated code is based on.
        /// </summary>
        public string TestName { get; }

        /// <summary>
        /// The describe of the test, the decorated code is based on, if one exists.
        /// </summary>
        public string Describe { get; }
    }
}
