using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    internal class EvaluateArgumentValueElement
    {
        private object _fallThrough;

        public bool FallbackSet { get; set; }

        public int H { get; set; }

        public object FallThrough
        {
            get => _fallThrough;
            set
            {
                _fallThrough = value;
                FallbackSet = true;
            }
        }

        public object V { get; set; }

        public DateTime D { get; set; }

        public object[] A { get; set; }

        public object O { get; internal set; }
    }
}