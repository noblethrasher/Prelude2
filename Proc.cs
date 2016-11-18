using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using System.Collections;

namespace Prelude
{
    public sealed class Person
    {
        public int ID { get; }
        public string FullName { get; }
        
        public Person(IDataReader rdr)
        {
            ID = rdr.GetInt32(rdr.GetOrdinal("id"));

            var fn =  rdr.GetString(rdr.GetOrdinal("first_name"));
            var ln = rdr.GetString(rdr.GetOrdinal("last_name"));

            FullName = $"{fn} {ln}";
        }

        public override string ToString() => $"{ID} {FullName}";
    }

    public partial class Proc<T, DbConnection, DbCommand, DbParameter, DbDataReader>
        where DbConnection : class, IDbConnection
        where DbDataReader :  IDataReader
        where DbCommand :  IDbCommand
        where DbParameter : IDbDataParameter
    {
        protected struct CommandParameters
        {
            public string CommandText { get; }
            public IEnumerable<DbParameter> Parameters { get; }

            public CommandParameters(string command, IEnumerable<DbParameter> @params)
            {
                CommandText = command;
                Parameters = @params;
            }
        }

        protected abstract class MaybeManagedConnection : IDisposable        
        {
            public DbConnection Connection { get; }

            public MaybeManagedConnection(DbConnection conn)
            {
                Connection = conn;
            }

            public abstract Command CommandFromParameters(CommandParameters parameters);

            public abstract MaybeManagedConnection Open();
            public abstract MaybeManagedConnection Close();

            public abstract void Dispose();

            public ConnectionState State => Connection.State;

            public bool IsOpen => (State & ConnectionState.Open) != 0;            
        }

        protected abstract class Unmanaged : MaybeManagedConnection
        {
            public Unmanaged(DbConnection conn) : base(conn) { }

            public sealed override MaybeManagedConnection Open() => this;
            public sealed override MaybeManagedConnection Close() => this;
            public sealed override void Dispose() { }
        }

        protected abstract class Managed : MaybeManagedConnection            
        {
            public Managed(DbConnection conn) : base(conn) { }
        }

        protected abstract class Command
        {
            protected readonly DbCommand command;

            public Command(DbCommand command)
            {
                this.command = command;
            }

            public abstract DbDataReader ExecuteReader();
            public abstract int ExecuteNonQuery();
        }

        protected struct Request : IEnumerable<DbDataReader>
        {
            readonly MaybeManagedConnection connection;
            readonly CommandParameters parameters;

            public Request(MaybeManagedConnection conn, CommandParameters parameters)
            {
                this.connection = conn;
                this.parameters = parameters;
            }

            public DbDataReader ExecuteReader() => connection.Open().CommandFromParameters(parameters).ExecuteReader();
            public int Execute() => connection.Open().CommandFromParameters(parameters).ExecuteNonQuery();

            public Enumerator GetEnumerator() => new Enumerator(connection.Open(), parameters);

            IEnumerator<DbDataReader> IEnumerable<DbDataReader>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator :  IEnumerator<DbDataReader>
            {
                readonly DbDataReader reader;
                readonly MaybeManagedConnection connection;

                #if DEBUG
                    
                int n;

                #endif

                public Enumerator(MaybeManagedConnection conn, CommandParameters parameters)
                {
                    reader = (connection = conn.Open()).CommandFromParameters(parameters).ExecuteReader();

                    #if DEBUG

                    n = 0;
    
                    #endif
                }

                public DbDataReader Current => reader;

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    reader.Dispose();
                    connection.Dispose();

                    #if DEBUG

                    Console.WriteLine($"Disposed after {n} iterations.");

                    #endif
                }

                public bool MoveNext()
                {
                    #if DEBUG


                    n++;

                    #endif

                    return reader.Read();
                }

                public void Reset() { }
            }
        }
    }

    public abstract partial class Proc<T, DbConnection, DbCommand, DbParameter, DbDataReader>
        where DbConnection : class, IDbConnection
        where DbDataReader : IDataReader
        where DbCommand : IDbCommand
        where DbParameter : IDbDataParameter
    {
        protected abstract CommandParameters CurrentParamters { get; }
        protected abstract T Submit(Request query);
        protected abstract MaybeManagedConnection DefaultConnection(DbConnection conn);

        public T Execute() => Execute(null);

        public virtual T Execute(DbConnection conn)
        {
            using (var managed_connection = DefaultConnection(conn))
            {
                if (!managed_connection.IsOpen)
                    managed_connection.Open();

                return Submit(new Request(managed_connection, CurrentParamters));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SchemaAttribute : Attribute
    {
        readonly static char[] delimiter = new[] { '.' };

        public string Name { get; }

        public SchemaAttribute(string name)
        {
            Name = string.Join(".", name.Split(delimiter, StringSplitOptions.RemoveEmptyEntries));
        }

        public override string ToString() => Name;

        public static explicit operator string(SchemaAttribute attr) => attr != null ? attr.Name : null;
    }

    public abstract class SqlProc<T> : Proc<T, SqlConnection, SqlCommand, SqlParameter, SqlDataReader>
    {
        readonly Func<CommandParameters> sql;
        readonly Dictionary<string, SqlParameter> parameters = new Dictionary<string, SqlParameter>();

        static Dictionary<Type, SqlDbType> clr_to_sql_types = new Dictionary<Type, SqlDbType>()
        {
            { typeof(int), SqlDbType.Int },
            { typeof(byte), SqlDbType.TinyInt },
            { typeof(short), SqlDbType.SmallInt},
            { typeof(DateTime), SqlDbType.DateTime},
            { typeof(Guid), SqlDbType.UniqueIdentifier},
            { typeof(bool), SqlDbType.Bit},
            { typeof(decimal), SqlDbType.Decimal },
            { typeof(float), SqlDbType.Float},
            { typeof(double), SqlDbType.Float},
            { typeof(long), SqlDbType.BigInt },
            { typeof(byte[]), SqlDbType.Binary },            
        };

        protected void Add(SqlParameter p) => parameters.Add(p.ParameterName, p);

        protected void Add(sql_parameter_name name, base64 value) => Add(name, value.ToString());

        protected void Add(sql_parameter_name name, string value)  => parameters.Add(name, new SqlParameter(name, SqlDbType.NVarChar) { Value = value });

        protected void Add(sql_parameter_name name, non_empty_string value) => Add(name, value.ToString());

        protected void Add<K>(sql_parameter_name name, K value)
        {
            SqlParameter p = new SqlParameter(name, value);
            SqlDbType type;

            if (!clr_to_sql_types.TryGetValue(typeof(K), out type))
                type = SqlDbType.Variant;

            p.SqlDbType = type;            
                
            parameters.Add(name, p);
        }

        void Add<K>(sql_parameter_name name, K? value)
            where K : struct            
        {
            if (value != null)
                Add(name, value.Value);
        }

        public SqlProc(Func<string> sql) { this.sql = () => new CommandParameters(sql(), parameters.Values); }
        public SqlProc(string sql) { this.sql = () => new CommandParameters(sql, parameters.Values); }

        public SqlProc(string sql, IEnumerable<SqlParameter> sql_parameter) : this(sql)
        {
            foreach (var p in sql_parameter)
                Add(p);
        }

        public SqlProc()
        {
            var type = GetType();

            var schema = String.Join(".", from attr in type.GetCustomAttributes(typeof(SchemaAttribute), true) select (string)(attr as SchemaAttribute));

            schema = !string.IsNullOrWhiteSpace(schema) ? schema + "." : null;

            var command_text = schema + type.Name;

            this.sql = () => new CommandParameters(command_text, parameters.Values);
        }

        protected sealed class OutputParameter<K>
        {
            static readonly System.Data.SqlDbType type;

            static  OutputParameter()
            {
                type = clr_to_sql_types[typeof(K)];
            }

            readonly SqlParameter param;

            public  OutputParameter(sql_parameter_name name)
            {
                param = new SqlParameter(name, type) { Direction = ParameterDirection.InputOutput, Value = null };
            }

            public K Value => (K) param.Value;
            
            public static implicit operator K (OutputParameter<K> output)
            {
                if (output.param == null)
                    throw new InvalidOperationException("Output parameter must be initialized");

                return output.Value;
            }

            public static implicit operator SqlParameter (OutputParameter<K> output)
            {
                if (output.param == null)
                    throw new InvalidOperationException("Output parameter must be initialized");

                return output.param;
            }
        }

        protected struct sql_parameter_name
        {
            readonly string name;

            public sql_parameter_name(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Parameter name must not be null or whitespace");

                if (name[0] != '@')
                    name = "@" + name;

                this.name = name;
            }

            public static implicit operator string(sql_parameter_name name) => name.name;
            public static implicit operator sql_parameter_name(string name) => new sql_parameter_name(name);

            public override string ToString() => name;
        }

        protected virtual string DefaultConnectionString => ConfigurationManager.ConnectionStrings["default"].ConnectionString;

        protected override CommandParameters CurrentParamters => sql();

        protected override MaybeManagedConnection DefaultConnection(SqlConnection conn)
        {
            if (conn == null)
                return new ManagedSqlConnection(DefaultConnectionString);
            else
                return new UnmanagedSqlConnection(conn);
        }

        static int IndexOfNonWhitespace(string s)
        {
            for (var i = 0; i < s.Length; i++)
                if (char.IsWhiteSpace(s[i]))
                    return i;

            return -1;
        }

        static Command CommandFromParameters(CommandParameters parameters, MaybeManagedConnection conn)
        {
            var cmd = new SqlCommand(parameters.CommandText, conn.Open().Connection);

            if (cmd.CommandText.StartsWith("[") || IndexOfNonWhitespace(cmd.CommandText) == -1)
                cmd.CommandType = CommandType.StoredProcedure;

            if (parameters.Parameters != null)
                foreach (var p in parameters.Parameters)
                    cmd.Parameters.Add(p);

            return new Sql_Command(cmd);
        }

        sealed class Sql_Command : Command
        {
            public Sql_Command(SqlCommand cmd) : base(cmd) { }

            public override SqlDataReader ExecuteReader() => command.ExecuteReader();
            public override int ExecuteNonQuery() => command.ExecuteNonQuery();
        }

        sealed class UnmanagedSqlConnection : Unmanaged
        {
            public UnmanagedSqlConnection(SqlConnection conn) : base(conn) { }

            public override Command CommandFromParameters(CommandParameters parameters) => SqlProc<T>.CommandFromParameters(parameters, this);
        }

        sealed class ManagedSqlConnection : Managed
        {
            readonly string connection_string;

            public ManagedSqlConnection(string connection_string) : base(new SqlConnection(connection_string)) { this.connection_string = connection_string; }

            public override MaybeManagedConnection Close()
            {
                if(IsOpen)
                    this.Connection.Close();

                return this;
            }

            public override Command CommandFromParameters(CommandParameters parameters) => SqlProc<T>.CommandFromParameters(parameters,  this);

            public override void Dispose()
            {
#if DEBUG

                Console.WriteLine("Disposed called.");

#endif

                this.Connection.Dispose();
            }

            public override MaybeManagedConnection Open()
            {
                if(!IsOpen)
                {
                    this.Connection.ConnectionString = connection_string;
                    this.Connection.Open();
                }

                return this;
            }
        }
    }

    public sealed class AdHocSqlNonQuery : SqlProc<int>
    {
        public AdHocSqlNonQuery(string sql, IEnumerable<SqlParameter> parameters) : base(sql)
        {
            foreach (var p in parameters)
                Add(p);
        }

        protected override int Submit(Request query) => query.Execute();
    }
}
