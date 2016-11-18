using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prelude
{
    public struct non_empty_string
    {
        readonly string s;

        public non_empty_string(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("String must be non null and non-whitespace");

            this.s = s;
        }
        public static implicit operator string  (non_empty_string s) => s.s;
        public static implicit operator non_empty_string (string s) => new non_empty_string(s);

        public override string ToString() => s;

        public CharEnumerator GetEnumerator() => s.GetEnumerator();
    }
}
