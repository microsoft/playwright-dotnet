/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

#pragma warning disable SA1201
#pragma warning disable SA1202
#pragma warning disable SA1204
#pragma warning disable SA1407
#pragma warning disable SA1600

namespace Microsoft.Playwright.Helpers;

internal sealed class ClearcoteMmdbReader : IDisposable
{
    private const int MetadataMaxLength = 128 * 1024;
    private const int DataSectionSeparatorLength = 16;
    private const int IPv4InIPv6PrefixBits = 96;
    private const int MaxDecodeDepth = 128;

    private static readonly byte[] _metadataMarker =
    {
        0xAB, 0xCD, 0xEF,
        (byte)'M', (byte)'a', (byte)'x', (byte)'M', (byte)'i', (byte)'n', (byte)'d', (byte)'.', (byte)'c', (byte)'o', (byte)'m',
    };

    private readonly int _nodeCount;
    private readonly int _recordSize;
    private readonly int _nodeByteSize;
    private readonly int _searchTreeSize;
    private readonly int _dataSectionStart;
    private readonly int _ipVersion;
    private byte[] _bytes;

    internal ClearcoteMmdbReader(string path)
    {
        _bytes = File.ReadAllBytes(path);
        var metadataStart = FindMetadataStart(_bytes);
        var metadata = new DataDecoder(_bytes, metadataStart).Read(metadataStart, out _) as Dictionary<string, object>
            ?? throw new InvalidOperationException("MMDB metadata is not a map");

        _nodeCount = RequiredInt(metadata, "node_count");
        _recordSize = RequiredInt(metadata, "record_size");
        _ipVersion = RequiredInt(metadata, "ip_version");
        _nodeByteSize = _recordSize switch
        {
            24 => 6,
            28 => 7,
            32 => 8,
            _ => throw new InvalidOperationException("unsupported MMDB record size: " + _recordSize),
        };
        _searchTreeSize = checked(_nodeCount * _nodeByteSize);
        _dataSectionStart = checked(_searchTreeSize + DataSectionSeparatorLength);
        if ((_ipVersion != 4 && _ipVersion != 6) || _dataSectionStart >= metadataStart)
        {
            throw new InvalidOperationException("invalid MMDB metadata");
        }
    }

    public void Dispose()
    {
        _bytes = Array.Empty<byte>();
    }

    internal Dictionary<string, object>? Find(IPAddress address)
    {
        var bytes = AddressBytes(address);
        if (bytes == null)
        {
            return null;
        }

        var node = 0L;
        for (var bitIndex = 0; bitIndex < bytes.Length * 8; bitIndex++)
        {
            if (node >= _nodeCount)
            {
                return DecodeRecord(node);
            }

            var bit = (bytes[bitIndex / 8] >> (7 - (bitIndex % 8))) & 1;
            node = ReadNode((int)node, bit);
        }

        return node >= _nodeCount ? DecodeRecord(node) : null;
    }

    private byte[]? AddressBytes(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        if (_ipVersion == 4)
        {
            return bytes.Length == 4 ? bytes : null;
        }

        if (bytes.Length == 16)
        {
            return bytes;
        }

        if (bytes.Length == 4)
        {
            var mapped = new byte[16];
            Buffer.BlockCopy(bytes, 0, mapped, IPv4InIPv6PrefixBits / 8, bytes.Length);
            return mapped;
        }

        return null;
    }

    private Dictionary<string, object>? DecodeRecord(long record)
    {
        if (record == _nodeCount)
        {
            return null;
        }

        if (record < _nodeCount + DataSectionSeparatorLength)
        {
            return null;
        }

        var offset = checked((int)(record - _nodeCount) + _searchTreeSize);
        var value = new DataDecoder(_bytes, _dataSectionStart).Read(offset, out _);
        return value as Dictionary<string, object>;
    }

    private long ReadNode(int node, int bit)
    {
        var offset = checked(node * _nodeByteSize);
        return _recordSize switch
        {
            24 => ReadNode24(offset, bit),
            28 => ReadNode28(offset, bit),
            32 => ReadNode32(offset, bit),
            _ => throw new InvalidOperationException("unsupported MMDB record size"),
        };
    }

    private long ReadNode24(int offset, int bit)
    {
        EnsureAvailable(_bytes, offset, 6);
        return bit == 0
            ? (_bytes[offset] << 16) | (_bytes[offset + 1] << 8) | _bytes[offset + 2]
            : (_bytes[offset + 3] << 16) | (_bytes[offset + 4] << 8) | _bytes[offset + 5];
    }

    private long ReadNode28(int offset, int bit)
    {
        EnsureAvailable(_bytes, offset, 7);
        var middle = _bytes[offset + 3];
        return bit == 0
            ? ((middle >> 4) << 24) | (_bytes[offset] << 16) | (_bytes[offset + 1] << 8) | _bytes[offset + 2]
            : ((middle & 0x0F) << 24) | (_bytes[offset + 4] << 16) | (_bytes[offset + 5] << 8) | _bytes[offset + 6];
    }

    private long ReadNode32(int offset, int bit)
    {
        EnsureAvailable(_bytes, offset, 8);
        return bit == 0
            ? BinaryPrimitives.ReadUInt32BigEndian(_bytes.AsSpan(offset, 4))
            : BinaryPrimitives.ReadUInt32BigEndian(_bytes.AsSpan(offset + 4, 4));
    }

    private static int RequiredInt(Dictionary<string, object> map, string key)
    {
        if (!map.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException("MMDB metadata missing " + key);
        }

        return value switch
        {
            int i => i,
            long l when l <= int.MaxValue => (int)l,
            uint u when u <= int.MaxValue => (int)u,
            ulong ul when ul <= int.MaxValue => (int)ul,
            _ => throw new InvalidOperationException("MMDB metadata has invalid " + key),
        };
    }

    private static int FindMetadataStart(byte[] bytes)
    {
        var minimum = Math.Max(0, bytes.Length - MetadataMaxLength);
        for (var offset = bytes.Length - _metadataMarker.Length; offset >= minimum; offset--)
        {
            if (Matches(bytes, offset, _metadataMarker))
            {
                return offset + _metadataMarker.Length;
            }
        }

        throw new InvalidOperationException("MMDB metadata marker not found");
    }

    private static bool Matches(byte[] bytes, int offset, byte[] marker)
    {
        if (offset < 0 || offset + marker.Length > bytes.Length)
        {
            return false;
        }

        for (var i = 0; i < marker.Length; i++)
        {
            if (bytes[offset + i] != marker[i])
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureAvailable(byte[] bytes, int offset, int count)
    {
        if (offset < 0 || count < 0 || offset > bytes.Length - count)
        {
            throw new InvalidOperationException("truncated MMDB data");
        }
    }

    private sealed class DataDecoder
    {
        private readonly byte[] _bytes;
        private readonly int _pointerBase;

        internal DataDecoder(byte[] bytes, int pointerBase)
        {
            _bytes = bytes;
            _pointerBase = pointerBase;
        }

        internal object? Read(int offset, out int nextOffset)
            => Read(offset, out nextOffset, 0);

        private object? Read(int offset, out int nextOffset, int depth)
        {
            if (depth > MaxDecodeDepth)
            {
                throw new InvalidOperationException("MMDB pointer recursion limit exceeded");
            }

            EnsureAvailable(_bytes, offset, 1);
            var control = _bytes[offset++];
            var type = control >> 5;
            var sizeMarker = control & 0x1F;

            if (type == 1)
            {
                var pointer = ReadPointer(control, ref offset);
                nextOffset = offset;
                return Read(checked(_pointerBase + pointer), out _, depth + 1);
            }

            if (type == 0)
            {
                EnsureAvailable(_bytes, offset, 1);
                type = _bytes[offset++] + 7;
            }

            var size = ReadSize(sizeMarker, ref offset);
            nextOffset = offset;
            switch (type)
            {
                case 2:
                    EnsureAvailable(_bytes, offset, size);
                    nextOffset = offset + size;
                    return Encoding.UTF8.GetString(_bytes, offset, size);
                case 3:
                    EnsureAvailable(_bytes, offset, 8);
                    nextOffset = offset + 8;
                    return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(_bytes.AsSpan(offset, 8)));
                case 4:
                    EnsureAvailable(_bytes, offset, size);
                    var bytes = new byte[size];
                    Buffer.BlockCopy(_bytes, offset, bytes, 0, size);
                    nextOffset = offset + size;
                    return bytes;
                case 5:
                case 6:
                case 9:
                case 10:
                    return ReadUnsigned(offset, size, out nextOffset);
                case 7:
                    return ReadMap(offset, size, out nextOffset, depth);
                case 8:
                    return ReadInt32(offset, size, out nextOffset);
                case 11:
                    return ReadArray(offset, size, out nextOffset, depth);
                case 13:
                    return null;
                case 14:
                    return size != 0;
                case 15:
                    EnsureAvailable(_bytes, offset, 4);
                    nextOffset = offset + 4;
                    return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(_bytes.AsSpan(offset, 4)));
                default:
                    throw new InvalidOperationException("unsupported MMDB data type: " + type);
            }
        }

        private Dictionary<string, object> ReadMap(int offset, int count, out int nextOffset, int depth)
        {
            var map = new Dictionary<string, object>(StringComparer.Ordinal);
            for (var i = 0; i < count; i++)
            {
                var key = Read(offset, out offset, depth + 1) as string
                    ?? throw new InvalidOperationException("MMDB map key is not a string");
                var value = Read(offset, out offset, depth + 1);
                if (value != null)
                {
                    map[key] = value;
                }
            }

            nextOffset = offset;
            return map;
        }

        private List<object> ReadArray(int offset, int count, out int nextOffset, int depth)
        {
            var list = new List<object>(count);
            for (var i = 0; i < count; i++)
            {
                var value = Read(offset, out offset, depth + 1);
                if (value != null)
                {
                    list.Add(value);
                }
            }

            nextOffset = offset;
            return list;
        }

        private object ReadUnsigned(int offset, int size, out int nextOffset)
        {
            if (size > 16)
            {
                throw new InvalidOperationException("invalid MMDB integer size");
            }

            EnsureAvailable(_bytes, offset, size);
            ulong value = 0;
            var readable = Math.Min(size, sizeof(ulong));
            for (var i = 0; i < readable; i++)
            {
                value = (value << 8) | _bytes[offset + i];
            }

            nextOffset = offset + size;
            return value <= long.MaxValue ? (long)value : value;
        }

        private int ReadInt32(int offset, int size, out int nextOffset)
        {
            if (size > sizeof(int))
            {
                throw new InvalidOperationException("invalid MMDB int32 size");
            }

            EnsureAvailable(_bytes, offset, size);
            nextOffset = offset + size;
            if (size == 0)
            {
                return 0;
            }

            var value = 0;
            for (var i = 0; i < size; i++)
            {
                value = (value << 8) | _bytes[offset + i];
            }

            if (size == sizeof(int))
            {
                return value;
            }

            return value;
        }

        private int ReadSize(int marker, ref int offset)
        {
            switch (marker)
            {
                case < 29:
                    return marker;
                case 29:
                    EnsureAvailable(_bytes, offset, 1);
                    return 29 + _bytes[offset++];
                case 30:
                    EnsureAvailable(_bytes, offset, 2);
                    var size16 = BinaryPrimitives.ReadUInt16BigEndian(_bytes.AsSpan(offset, 2));
                    offset += 2;
                    return 285 + size16;
                default:
                    EnsureAvailable(_bytes, offset, 3);
                    var size24 = (_bytes[offset] << 16) | (_bytes[offset + 1] << 8) | _bytes[offset + 2];
                    offset += 3;
                    return 65821 + size24;
            }
        }

        private int ReadPointer(int control, ref int offset)
        {
            var size = (control & 0x18) >> 3;
            var valueBits = control & 0x07;
            switch (size)
            {
                case 0:
                    EnsureAvailable(_bytes, offset, 1);
                    return (valueBits << 8) | _bytes[offset++];
                case 1:
                    EnsureAvailable(_bytes, offset, 2);
                    var pointer16 = (valueBits << 16) | BinaryPrimitives.ReadUInt16BigEndian(_bytes.AsSpan(offset, 2));
                    offset += 2;
                    return pointer16 + 2048;
                case 2:
                    EnsureAvailable(_bytes, offset, 3);
                    var pointer24 = (valueBits << 24) | (_bytes[offset] << 16) | (_bytes[offset + 1] << 8) | _bytes[offset + 2];
                    offset += 3;
                    return pointer24 + 526336;
                default:
                    EnsureAvailable(_bytes, offset, 4);
                    var pointer32 = BinaryPrimitives.ReadUInt32BigEndian(_bytes.AsSpan(offset, 4));
                    offset += 4;
                    if (pointer32 > int.MaxValue)
                    {
                        throw new InvalidOperationException("MMDB pointer is too large");
                    }

                    return (int)pointer32;
            }
        }
    }
}
