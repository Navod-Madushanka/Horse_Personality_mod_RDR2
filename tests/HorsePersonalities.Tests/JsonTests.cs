using HorsePersonalities.Core.Json;
using Xunit;

namespace HorsePersonalities.Tests
{
    public class JsonTests
    {
        [Fact]
        public void RoundTripsNestedValues()
        {
            const string text = "{\"a\":1,\"b\":[true,false,null],\"c\":{\"d\":\"x\\\"y\\n\",\"e\":-0.25}}";
            var v = JsonValue.Parse(text);
            Assert.Equal(1, v["a"].AsInt());
            Assert.True(v["b"].Items[0].AsBool());
            Assert.True(v["b"].Items[2].IsNull);
            Assert.Equal("x\"y\n", v["c"]["d"].AsString());
            Assert.Equal(-0.25, v["c"]["e"].AsNumber());
            Assert.Equal(text, v.ToJson(indented: false));
            Assert.Equal(text, JsonValue.Parse(v.ToJson()).ToJson(indented: false));
        }

        [Fact]
        public void KeepsKeyOrderAndReplacesInPlace()
        {
            var o = JsonValue.NewObject().Set("z", 1).Set("a", 2).Set("z", 3);
            Assert.Equal("{\"z\":3,\"a\":2}", o.ToJson(false));
        }

        [Theory]
        [InlineData("{\"a\":1,}", "line 1")]
        [InlineData("{\n  \"a\": tru\n}", "line 2")]
        [InlineData("[1 2]", "Expected ','")]
        [InlineData("{\"a\":1,\"a\":2}", "Duplicate key")]
        [InlineData("01", "after the end")]
        [InlineData("\"unterminated", "Unterminated")]
        [InlineData("", "end of JSON")]
        public void ReportsReadableErrors(string text, string expected)
        {
            var ex = Assert.Throws<JsonException>(() => JsonValue.Parse(text));
            Assert.Contains(expected, ex.Message);
        }

        [Fact]
        public void AcceptsByteOrderMark() => Assert.Equal(5, JsonValue.Parse("﻿{\"a\":5}")["a"].AsInt());

        [Theory]
        [InlineData(1.0, "1")]
        [InlineData(0.1, "0.1")]
        [InlineData(-2.0, "-2")]
        [InlineData(1.17, "1.17")]
        [InlineData(0.04, "0.04")]
        public void WritesShortNumbers(double value, string expected) => Assert.Equal(expected, JsonValue.From(value).ToJson(false));

        [Fact]
        public void MissingMembersReadAsNullAndFallbacksApply()
        {
            var v = JsonValue.Parse("{\"a\":{}}");
            Assert.True(v["nope"]["deeper"].IsNull);
            Assert.Equal(7, v.GetInt("nope", 7));
            Assert.Throws<JsonException>(() => v.Require("nope"));
        }

        [Fact]
        public void RejectsNaN() => Assert.Throws<System.ArgumentException>(() => JsonValue.From(double.NaN));
    }
}
