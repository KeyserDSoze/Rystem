using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace RepositoryFramework.Infrastructure.MsSql
{
    public sealed class MsSqlOptions<T, TKey>
    {
        public string Schema { get; set; } = "dbo";
        public string TableName { get; set; } = typeof(T).Name;
        public string ConnectionString { get; set; } = null!;
        public Type ModelType { get; } = typeof(T);
        public Type KeyType { get; } = typeof(TKey);
        public bool KeyIsPrimitive { get; } = typeof(TKey).IsPrimitive();
        public string? PrimaryKey { get; internal set; }
        internal List<PropertyHelper<T>> Properties { get; } = new();
        internal static MsSqlOptions<T, TKey> Instance { get; } = new();
        private MsSqlOptions()
        {
            foreach (var property in typeof(T).GetProperties())
                Properties.Add(new PropertyHelper<T>(property));
            RefreshColumnNames();
        }
        internal string Top1 { get; private set; } = null!;
        internal string Exist { get; private set; } = null!;
        internal string Delete { get; private set; } = null!;
        internal string Insert { get; private set; } = null!;
        internal string Update { get; private set; } = null!;
        internal string BaseQuery { get; private set; } = null!;
        internal void RefreshColumnNames()
        {
            var columns = string.Join(',', Properties.Select(x => $"[{x.ColumnName}]"));
            Exist = $"select top 1 count(*) from [{Schema}].[{TableName}] where [{PrimaryKey}] = @Key";
            Top1 = $"select top 1 {columns} from [{Schema}].[{TableName}] where [{PrimaryKey}] = @Key";
            Delete = $"delete from [{Schema}].[{TableName}] where [{PrimaryKey}] = @Key";
            BaseQuery = $"select {columns} from [{Schema}].[{TableName}]";
            Insert = $"INSERT INTO [{Schema}].[{TableName}] ({{0}}) VALUES ({{1}});";
            Update = $"Update [{Schema}].[{TableName}] SET {{0}} where [{PrimaryKey}] = @Key";
        }
        internal List<SqlParameter> SetEntityForDatabase(T entity, TKey key)
        {
            List<SqlParameter> parameters = new();
            foreach (var property in Properties)
            {
                var parameter = property.SetEntityForDatabase(entity);
                if (parameter != null)
                    parameters.Add(parameter);
            }
            return parameters;
        }
        internal TKey SetEntity(SqlDataReader reader, T entity)
        {
            foreach (var property in Properties)
                property.SetEntity(reader, entity);
            if (KeyIsPrimitive)
                return reader[PrimaryKey].Cast<TKey>()!;
            else
                return reader[PrimaryKey].ToString()!.FromJson<TKey>();
        }
        public string GetCreationalQueryForTable()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append($"CREATE TABLE [{Schema}].[{TableName}](");
            foreach (var property in Properties)
            {
                stringBuilder.Append(property.CreationalString);
                stringBuilder.Append(',');
            }
            stringBuilder.Append($"CONSTRAINT [PK_{TableName}] PRIMARY KEY CLUSTERED ([{PrimaryKey}] ASC)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]) ON [PRIMARY]");
            return stringBuilder.ToString();
        }

    }
}
