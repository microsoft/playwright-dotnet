using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Transport.Converters;

namespace Microsoft.Playwright
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

        internal class SpecialType : EvaluateArgumentValueElement
        {
            public object V { get; set; }
        }

        internal class Datetime : EvaluateArgumentValueElement
        {
            public DateTime? D { get; set; }
        }

        internal class String : EvaluateArgumentValueElement
        {
            public string S { get; set; }
        }

        internal class Number : EvaluateArgumentValueElement
        {
            public object N { get; set; }
        }

        internal class Boolean : EvaluateArgumentValueElement
        {
            public bool B { get; set; }
        }

        internal class Array : EvaluateArgumentValueElement
        {
            public object[] A { get; set; }
        }

        internal class Object : EvaluateArgumentValueElement
        {
            public KeyValueObject[] O { get; set; }
        }

        internal class Handle : EvaluateArgumentValueElement
        {
            public int? H { get; set; }
        }
    }
}
