using System.Data;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace RepositoryFramework.Infrastructure.MsSql
{
    public sealed class PropertyHelper<T>
    {
        internal string Name { get; }
        private string? _realName;
        public string? ColumnName
        {
            get => _realName ?? Name;
            set
            {
                _realName = value;
            }
        }
        internal PropertyInfo PropertyInfo { get; }
        internal PropertyHelper(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
            Name = PropertyInfo.Name;
            var mapping = Mapping(PropertyInfo.PropertyType);
            IsPrimitive = PropertyInfo.PropertyType.IsPrimitive();
            IsNullable = mapping.IsNullable;
            SqlType = mapping.Type;
            Dimension = mapping.DefaultDimension;
        }
        internal bool IsPrimitive { get; }
        public bool IsNullable { get; set; }
        public bool IsAutomaticCreated { get; set; }
        internal SqlParameter? SetEntityForDatabase(T entity)
        {
            var value = PropertyInfo.GetValue(entity);
            if (value != null)
                return new SqlParameter(ColumnName, IsPrimitive ? value : value.ToJson());
            return null;
        }
        internal void SetEntity(SqlDataReader reader, T entity)
        {
            var value = reader[ColumnName];
            if (value != null && value != DBNull.Value)
                PropertyInfo.SetValue(entity, IsPrimitive ? value.Cast(PropertyInfo.PropertyType) : value.ToString()!.FromJson(PropertyInfo.PropertyType));
        }
        public SqlDbType SqlType { get; set; }
        public int[]? Dimension { get; set; }
        internal string CreationalString => $"[{ColumnName}] {SqlType}{(Dimension != null && Dimension.Length > 0 ? $"({string.Join(',', Dimension.Select(x => x == int.MaxValue ? "max" : x.ToString()))})" : string.Empty)} {(!IsNullable ? "NOT NULL" : string.Empty)}";
        internal static (SqlDbType Type, bool IsNullable, int[] DefaultDimension) Mapping(Type type)
        {
            var isNullable = false;
            if (type.Name.Contains("Nullable"))
            {
                isNullable = true;
                type = type.GetGenericArguments().First();
            }
            if (!type.IsPrimitive())
                return (SqlDbType.VarChar, isNullable, new int[1] { int.MaxValue });
            else if (type == typeof(long))
                return (SqlDbType.BigInt, isNullable, Array.Empty<int>());
            else if (type == typeof(byte[]))
                return (SqlDbType.VarBinary, isNullable, Array.Empty<int>());
            else if (type == typeof(bool))
                return (SqlDbType.Bit, isNullable, Array.Empty<int>());
            else if (type == typeof(char))
                return (SqlDbType.Char, isNullable, Array.Empty<int>());
            else if (type == typeof(DateTime))
                return (SqlDbType.DateTime2, isNullable, Array.Empty<int>());
            else if (type == typeof(DateTimeOffset))
                return (SqlDbType.DateTimeOffset, isNullable, Array.Empty<int>());
            else if (type == typeof(decimal))
                return (SqlDbType.Decimal, isNullable, new int[2] { 14, 4 });
            else if (type == typeof(byte[]))
                return (SqlDbType.VarBinary, isNullable, new int[1] { 1 });
            else if (type == typeof(float))
                return (SqlDbType.Float, isNullable, Array.Empty<int>());
            else if (type == typeof(int))
                return (SqlDbType.Int, isNullable, Array.Empty<int>());
            else if (type == typeof(char[]))
                return (SqlDbType.NChar, isNullable, new int[1] { 10 });
            else if (type == typeof(double))
                return (SqlDbType.Real, isNullable, Array.Empty<int>());
            else if (type == typeof(TimeSpan))
                return (SqlDbType.Time, isNullable, Array.Empty<int>());
            else if (type == typeof(Guid))
                return (SqlDbType.UniqueIdentifier, isNullable, Array.Empty<int>());
            else if (type == typeof(string))
                return (SqlDbType.VarChar, isNullable, new int[1] { 100 });
            return (SqlDbType.Variant, isNullable, Array.Empty<int>());
        }
    }
}
