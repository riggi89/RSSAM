// RSSAM unit tests.
// Copyright (c) 2026 Daniel Riggi (riggi89).
// Distributed under the project license; see LICENSE.md and NOTICE.md.

using System.Text;
using RSSAM.Infrastructure.SteamSchema;

namespace RSSAM.UnitTests;

public sealed class SteamSchemaTests
{
    [Fact]
    public void ReadAsBinary_ReadsNestedValues()
    {
        using var stream = new MemoryStream();
        WriteNodeHeader(stream, KeyValueType.None, "stats");
        WriteNodeHeader(stream, KeyValueType.Int32, "score");
        stream.Write(BitConverter.GetBytes(42));
        WriteNodeHeader(stream, KeyValueType.String, "name");
        WriteCString(stream, "Player");
        stream.WriteByte((byte)KeyValueType.End);
        stream.WriteByte((byte)KeyValueType.End);
        stream.Position = 0;

        var root = new KeyValue();

        Assert.True(root.ReadAsBinary(stream));
        Assert.Equal(42, root["stats"]["score"].AsInteger(-1));
        Assert.Equal("Player", root["stats"]["name"].AsString(""));
    }

    [Fact]
    public void ReadAsBinary_RejectsTruncatedAndTrailingData()
    {
        var truncated = new KeyValue();
        Assert.False(truncated.ReadAsBinary(new MemoryStream([(byte)KeyValueType.Int32])));

        var trailing = new KeyValue();
        Assert.False(trailing.ReadAsBinary(new MemoryStream([(byte)KeyValueType.End, 1])));
    }

    [Fact]
    public void MissingChild_IsInvalidAndReturnsDefaults()
    {
        var root = new KeyValue();
        Assert.True(root.ReadAsBinary(new MemoryStream([(byte)KeyValueType.End])));

        Assert.False(root["missing"].Valid);
        Assert.Equal(123, root["missing"].AsInteger(123));
        Assert.Equal("fallback", root["missing"].AsString("fallback"));
    }

    private static void WriteNodeHeader(Stream stream, KeyValueType type, string name)
    {
        stream.WriteByte((byte)type);
        WriteCString(stream, name);
    }

    private static void WriteCString(Stream stream, string value)
    {
        stream.Write(Encoding.UTF8.GetBytes(value));
        stream.WriteByte(0);
    }
}
