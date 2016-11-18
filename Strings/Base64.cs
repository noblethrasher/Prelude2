using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prelude
{
    public struct base64
    {
        readonly byte[] bytes;

        public base64(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public static implicit operator base64 (byte[] bytes) => new base64(bytes);
        public static implicit operator string (base64 b64) =>  b64.ToString();
        public override string ToString() => Convert.ToBase64String(bytes);
    }

    public struct base64_decoding
    {
        readonly string s;

        public base64_decoding(string s)
        {
            this.s = s;
        }

        public static implicit operator byte[] (base64_decoding b) => Convert.FromBase64String(b.s);
        public static explicit operator base64_decoding(string s) =>new base64_decoding(s);
        public static implicit operator base64_decoding(non_empty_string s) => new base64_decoding(s);
    }
}
