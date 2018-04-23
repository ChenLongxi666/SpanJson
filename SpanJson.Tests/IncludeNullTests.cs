﻿using System;
using System.Collections.Generic;
using System.Text;
using SpanJson.Resolvers;
using Xunit;

namespace SpanJson.Tests
{
    public class IncludeNullTests
    {
        public class IncludeNull : IEquatable<IncludeNull>
        {
            public class Nested : IEquatable<Nested>
            {
                public string Text { get; set; }

                public bool Equals(Nested other)
                {
                    if (ReferenceEquals(null, other)) return false;
                    if (ReferenceEquals(this, other)) return true;
                    return string.Equals(Text, other.Text);
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj)) return false;
                    if (ReferenceEquals(this, obj)) return true;
                    if (obj.GetType() != this.GetType()) return false;
                    return Equals((Nested) obj);
                }

                public override int GetHashCode()
                {
                    return 0;
                }
            }



            public int Key { get; set; }
            public string Value { get; set; }
            public Nested Child { get; set; }

            public bool Equals(IncludeNull other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Key == other.Key && string.Equals(Value, other.Value) && Equals(Child, other.Child);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((IncludeNull) obj);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        [Fact]
        public void SerializeDeserializeGeneric()
        {
            var includeNull = new IncludeNull {Key = 1};
            var serialized = JsonSerializer.Generic.Serialize<IncludeNull, char, IncludeNullsOriginalCaseResolver<char>>(includeNull);
            Assert.NotNull(serialized);
            Assert.Contains("null", serialized);
            var deserialized = JsonSerializer.Generic.Deserialize<IncludeNull, char, IncludeNullsOriginalCaseResolver<char>>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(includeNull, deserialized);
        }

        [Fact]
        public void SerializeDeserializeNonGeneric()
        {
            var includeNull = new IncludeNull {Key = 1};
            var serialized = JsonSerializer.NonGeneric.Serialize<char, IncludeNullsOriginalCaseResolver<char>>(includeNull);
            Assert.NotNull(serialized);
            Assert.Contains("null", serialized);
            var deserialized = JsonSerializer.NonGeneric.Deserialize<char, IncludeNullsOriginalCaseResolver<char>>(serialized, typeof(IncludeNull));
            Assert.NotNull(deserialized);
            Assert.Equal(includeNull, deserialized);
        }
    }
}
