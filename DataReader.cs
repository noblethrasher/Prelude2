using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("ArcReaction")]

namespace Prelude
{
    

    public struct Maybe<T>
    {
        readonly string failed;

        public T Value { get; }

        internal Maybe(T value, string failed)
        {
            this.Value = value;
            this.failed = failed;
        }

        public bool IsSuccess => failed == null;

        public static Maybe<T> FailWith(string error)
        {
            if (error == null)
                throw new ArgumentException("Error message must be non-null");

            return new Maybe<T>(default(T), error);
        }

        public Maybe<Result> Select<Result>(Func<T, Result> f)
        {
            return new Maybe<Result>(f(Value), null);
        }

        public Maybe<V> SelectMany<U, V>(Func<T, Maybe<U>> f, Func<T, U, V> g)
        {       
            if (this.failed != null)
                return new Maybe<V>(default(V), failed);

            var u = f(Value);

            if (u.failed != null)
                return new Maybe<V>(default(V), u.failed);

            return new Maybe<V>(g(Value, u.Value), null);
        }
    }

    public static class DataReaderModule
    {
        static Maybe<T> GetValue<T>(IDataReader rdr, string name, Func<int, T> getvalue)
        {
            var ord = rdr.GetOrdinal(name);

            if (ord < 0)
                return new Maybe<T>(default(T), $"Column {name} does not exist.");

            if (rdr.IsDBNull(ord))
                return new Maybe<T>(default(T), $"Column '{name}' is null");

            try
            {
                var n = getvalue(ord);

                return new Maybe<T>(n, null);
            }
            catch(InvalidCastException ex)
            {
                return new Maybe<T>(default(T), $"Column '{name}' could not be retieved as type {typeof(T).FullName}.");
            }
        }

        static Maybe<T?> MaybeGetValue<T>(IDataReader rdr, string name,  Func<int, T> getValue)
            where T : struct
        {
            var ord = rdr.GetOrdinal(name);

            if (ord < 0)
                return new Maybe<T?>(default(T), $"Column {name} does not exist.");

            if (rdr.IsDBNull(ord))
                return new Maybe<T?>(null, null);

            try
            {
                var n = getValue(ord);

                return new Maybe<T?>(n, null);
            }
            catch (InvalidCastException ex)
            {
                return new Maybe<T?>(null, $"Column '{name}' could not be retieved as type {typeof(T).FullName}.");
            }
        }

        public static string GetString(this IDataReader rdr, string name) => rdr.GetString(rdr.GetOrdinal(name));

        public static Maybe<bool> GetBoolean(this IDataReader rdr, string name) => GetValue(rdr, name, rdr.GetBoolean);
        public static Maybe<int> GetInt32(this IDataReader rdr, string name) => GetValue(rdr, name, rdr.GetInt32);
        public static Maybe<short> GetInt16(this IDataReader rdr, string name) => GetValue(rdr, name, rdr.GetInt16);
        public static Maybe<byte> GetByte(this IDataReader rdr, string name) => GetValue(rdr, name, rdr.GetByte);

        public static Maybe<Guid> GetGuid(this IDataReader rdr, string name) => GetValue(rdr, name, rdr.GetGuid);
        public static Maybe<decimal> GetDecimal(this IDataReader rdr, string name) => GetValue(rdr, name, rdr.GetDecimal);
        public static Maybe<float> GetFloat(this IDataReader rdr, string name) => GetValue(rdr, name, rdr.GetFloat);
        public static Maybe<double> GetDouble(this IDataReader rdr, string name) => GetValue(rdr, name, rdr.GetDouble);
        public static Maybe<DateTime> GetDateTime(this IDataReader rdr, string name) => GetValue(rdr, name, rdr.GetDateTime);
        public static Maybe<non_empty_string> GetNonEmptyString(this IDataReader rdr, string name)
        {
            int ord = 0;

            try
            {
                ord = rdr.GetOrdinal(name);
            }

            #pragma warning disable

            catch(IndexOutOfRangeException ex)
            {
                return new Maybe<non_empty_string>(new non_empty_string(), $"Column '{name}' does not exist.");
            }

            #pragma warning restore

            if (rdr.IsDBNull(ord))
                return new Maybe<non_empty_string>(new non_empty_string(), $"Column '{name}' is null.");

            var s = rdr.GetString(ord);

            if (string.IsNullOrEmpty(s))
                return new Maybe<non_empty_string>(new non_empty_string(), $"Column '{name}' is an empty string, but a non-empty string is expected.");

            return new Maybe<non_empty_string>(new non_empty_string(s), null);
        }
    }
}
