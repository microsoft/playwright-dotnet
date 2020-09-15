using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using PlaywrightSharp.Transport.Converters;

namespace PlaywrightSharp
{
    internal class EvaluateArgumentValueElement
    {
        private object _fallThrough;

        [JsonIgnore]
        public bool FallbackSet { get; set; }

        [JsonIgnore]
        public object FallThrough
        {
            get => _fallThrough;
            set
            {
                _fallThrough = value;
                FallbackSet = true;
            }
        }

        public class SpecialType : EvaluateArgumentValueElement
        {
            public object V { get; set; }
        }

        public class Datetime : EvaluateArgumentValueElement
        {
            public DateTime? D { get; set; }
        }

        public class String : EvaluateArgumentValueElement
        {
            public string S { get; set; }
        }

        public class Number : EvaluateArgumentValueElement
        {
            public object N { get; set; }
        }

        public class Boolean : EvaluateArgumentValueElement
        {
            public bool B { get; set; }
        }

        public class Array : EvaluateArgumentValueElement
        {
            public object[] A { get; set; }
        }

        public class Object : EvaluateArgumentValueElement
        {
            public KeyValueObject[] O { get; set; }
        }

        public class Handle : EvaluateArgumentValueElement
        {
            public int? H { get; set; }
        }
    }
}
