using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prelude
{
    public struct ColumnName
    {
        readonly string name;

        public ColumnName(string name)
        {
            this.name = name;
        }

        public static implicit operator ColumnName(string name) => new ColumnName(name);
        public static implicit operator string(ColumnName name) => name.name;

        public override string ToString() => name;
    }
}
