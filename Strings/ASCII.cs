using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prelude
{
    public struct ascii
    {
        readonly string s;

        public ascii(string s)
        {
            foreach (var c in s)
                if (c > 128)
                    throw new ArgumentException("String must contain only ASCII characters");

            this.s = s;
        }

        public override string ToString()
        {
            if (s == null)
                throw new InvalidOperationException($"Type {typeof(ascii).FullName} must be initialized with a non null instance of System.String");

            return s;
        }

        public static explicit operator ascii(string s) => new ascii(s);
        public static implicit operator string (ascii ascii) => ascii.s;
    }
}
