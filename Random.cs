using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Prelude
{

    /// <summary>
    /// Generates an unbounded source of random values
    /// </summary>
    /// <typeparam name="T">The type of value generated</typeparam>
    public abstract class RandomSource<T> : IEnumerable<T>, IComparable<RandomSource<T>>
    {
        protected static readonly Random random = new Random();

        protected abstract T Next();

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            RandomSource<T> rs;

            public Enumerator(RandomSource<T> rs) : this()
            {
                this.rs = rs;
            }

            public T Current => rs.Next();

            object IEnumerator.Current => Current;

            public bool MoveNext() => true;

            public void Reset() { }
            public void Dispose() { }
        }

        public U Select<U>(Func<T, U> f) => f(Next());

        public virtual int CompareTo(RandomSource<T> that) => random.Next();
        public override int GetHashCode() => random.Next();
        public static implicit operator T(RandomSource<T> r_t) => r_t.Next();
    }

    public sealed class RandomIntegerSource : RandomSource<int>
    {
        readonly int min, max;

        public RandomIntegerSource() : this(int.MinValue, int.MaxValue) { }

        public RandomIntegerSource(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        protected override int Next() => random.Next(min, max);
    }

    public sealed class RandomAsciiLetterOrDigitSource : RandomSource<char>
    {
        const int
            A = 65,
            Z = A + 26,
            a = 97,
            z = a + 26,
            d0 = 48,
            d9 = d0 + 10;

        delegate char get();

        static readonly get[] gets = new[] { (get) upper, lower, digit };

        static char upper() => (char) random.Next(A, Z);
        static char lower() => (char) random.Next(a, z);
        static char digit() => (char) random.Next(d0, d9);

        protected override char Next() => gets[random.Next(0, 3)]();
    }
}
