using Microsoft.Playwright.Transport;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class StdIOTransportEndiannessTests
{
    [Test]
    public void DecodeMessageSizeShouldHandleLittleEndianEncoding()
    {
        // Little-endian encoding: least significant byte first
        byte[] littleEndian = [0x05, 0x00, 0x00, 0x00]; // 5
        Assert.AreEqual(5, StdIOTransport.DecodeMessageSize(littleEndian, 0));

        byte[] larger = [0xFF, 0x00, 0x00, 0x01]; // 1 * 2^24 + 255 = 16777471
        Assert.AreEqual(16777471, StdIOTransport.DecodeMessageSize(larger, 0));
    }

    [Test]
    public void DecodeMessageSizeShouldRejectMaximumSizeExceeded()
    {
        // MaxMessageSize + 1 in little-endian
        uint maxSize = (uint)StdIOTransport.MaxMessageSize + 1;
        byte[] oversized =
        [
            (byte)(maxSize & 0xFF),
            (byte)((maxSize >> 8) & 0xFF),
            (byte)((maxSize >> 16) & 0xFF),
            (byte)((maxSize >> 24) & 0xFF),
        ];

        Assert.Throws<PlaywrightException>(() => StdIOTransport.DecodeMessageSize(oversized, 0));
    }

    [Test]
    public void DecodeMessageSizeShouldRejectZero()
    {
        Assert.Throws<PlaywrightException>(() => StdIOTransport.DecodeMessageSize(new byte[] { 0, 0, 0, 0 }, 0));
    }

    [Test]
    public void DecodeMessageSizeShouldAcceptValidSizes()
    {
        // Valid sizes: 1 through MaxMessageSize
        byte[] small = [0x01, 0x00, 0x00, 0x00];
        Assert.AreEqual(1, StdIOTransport.DecodeMessageSize(small, 0));

        // MaxMessageSize (256MB) in little-endian
        uint max = (uint)StdIOTransport.MaxMessageSize;
        byte[] large =
        [
            (byte)(max & 0xFF),
            (byte)((max >> 8) & 0xFF),
            (byte)((max >> 16) & 0xFF),
            (byte)((max >> 24) & 0xFF),
        ];
        Assert.AreEqual(StdIOTransport.MaxMessageSize, StdIOTransport.DecodeMessageSize(large, 0));
    }

    [Test]
    public void DecodeMessageSizeShouldRejectNegativeWrappingInteger()
    {
        // When interpreted as little-endian signed int, 0x80000000 is int.MinValue (negative)
        byte[] signBit = [0x00, 0x00, 0x00, 0x80];
        Assert.Throws<PlaywrightException>(() => StdIOTransport.DecodeMessageSize(signBit, 0));
    }

    [Test]
    public void DecodeMessageSizeShouldHandleOffsetCorrectly()
    {
        // Data with some bytes before the size prefix
        byte[] data = [0xAA, 0xBB, 0x05, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05];
        Assert.AreEqual(5, StdIOTransport.DecodeMessageSize(data, 2));
    }

    [Test]
    public void DecodeMessageSizeWithAlternativeEndianness()
    {
        // If the platform were big-endian, the same bytes would be interpreted differently.
        // The method handles this with BitConverter.IsLittleEndian check.
        // In little-endian: bytes [0, 0, 0, 5] = 5
        // In big-endian: bytes [0, 0, 0, 5] = 83886080
        // We can't test both branches without mocking, but we can verify correctness
        // on the current platform.
        byte[] leEncoded = [0x05, 0x00, 0x00, 0x00];
        if (BitConverter.IsLittleEndian)
        {
            Assert.AreEqual(5, StdIOTransport.DecodeMessageSize(leEncoded, 0));
        }
        else
        {
            // On big-endian, this same byte sequence would decode to 0x05000000 = 83886080
            // which exceeds MaxMessageSize, so it should throw.
            Assert.Throws<PlaywrightException>(() => StdIOTransport.DecodeMessageSize(leEncoded, 0));
        }
    }
}
