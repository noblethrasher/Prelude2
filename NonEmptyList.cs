using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prelude
{
    public sealed class NonEmptyList<T> :  IReadOnlyList<T>
    {
        readonly T head;
        readonly List<T> rest;

        public NonEmptyList(T head, IEnumerable<T> rest)
        {
            this.head = head;

            if (rest != null)
                this.rest = new List<T>(rest);
        }

        public NonEmptyList(T head) : this(head, null) { }
        public NonEmptyList(T head, params T[] rest) : this(head, rest as IEnumerable<T>) { }

        public T this[int index]
        {
            get
            {
                if (index != 0)
                    if (index < 0 || rest == null)
                        throw new IndexOutOfRangeException();

                return index == 0 ? head : rest[index - 1];
            }
        }

        public int Count => 1 + (rest?.Count ?? 0);

        public IEnumerator<T> GetEnumerator()
        {
            yield return head;

            if (rest != null)
                foreach (var item in rest)
                    yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
