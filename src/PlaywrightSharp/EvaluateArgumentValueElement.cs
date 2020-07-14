using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PlaywrightSharp
{
    internal class EvaluateArgumentValueElement
    {
        private object _fallThrough;

        [JsonIgnore]
        public bool FallbackSet { get; set; }

        public int? H { get; set; }

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

        public class Array : EvaluateArgumentValueElement
        {
            public object[] A { get; set; }
        }

        public class Object : EvaluateArgumentValueElement
        {
            public object O { get; internal set; }
        }
    }
}
