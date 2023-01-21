using System.Reflection;
using System.Text.Json;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Rest;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace RepositoryFramework.Infrastructure.Dynamics.Dataverse
{
    public sealed class DataverseOptions<T, TKey>
    {
        public string Environment { get; set; } = null!;
        private string _prefix = "new_";
        public string Prefix
        {
            get => _prefix;
            set
            {
                _prefix = value;
                ColumnSet = new ColumnSet(Properties.Select(x => x.LogicalName).Concat(new List<string> { LogicalPrimaryKey }).ToArray());
            }
        }
        public void SetConnection(string environment, DataverseAppRegistrationAccount identity)
        {
            Environment = environment;
            ApplicationIdentity = identity;
        }
        public string TableName { get; set; } = typeof(T).Name;
        public string LogicalTableName => $"{Prefix}{TableName.ToLower()}";
        public string TableNameWithPrefix => $"{Prefix}{TableName}";
        public string? SolutionName { get; set; }
        public string? Description { get; set; }
        public DataverseAppRegistrationAccount? ApplicationIdentity { get; set; } = null!;
        public Type ModelType { get; } = typeof(T);
        public Type KeyType { get; } = typeof(TKey);
        public bool KeyIsPrimitive { get; } = typeof(TKey).IsPrimitive();
        public string PrimaryKey { get; set; } = "Id";
        public string LogicalPrimaryKey => $"{Prefix}{PrimaryKey.ToLower()}";
        public string PrimaryKeyWithPrefix => $"{Prefix}{PrimaryKey}";
        internal List<PropertyHelper<T>> Properties { get; } = new();
        internal static DataverseOptions<T, TKey> Instance { get; } = new();
        public ColumnSet ColumnSet { get; private set; }
        private DataverseOptions()
        {
            foreach (var property in typeof(T).GetProperties())
            {
                Properties.Add(new PropertyHelper<T>
                {
                    Name = property.Name,
                    PrefixName = () => Prefix,
                    Property = property,
                });
            }
            ColumnSet = new ColumnSet(Properties.Select(x => x.LogicalName).Concat(new List<string> { LogicalPrimaryKey }).ToArray());
        }
        public void SetDataverseEntity(Microsoft.Xrm.Sdk.Entity dataverseEntity, T entity, TKey key)
        {
            foreach (var property in Properties)
                property.SetDataverseEntity(dataverseEntity, entity);
            dataverseEntity[LogicalPrimaryKey] = KeyIsPrimitive ? key!.ToString() : key.ToJson();
        }
        public TKey SetEntity(Microsoft.Xrm.Sdk.Entity dataverseEntity, T entity)
        {
            foreach (var property in Properties)
                property.SetEntity(dataverseEntity, entity);
            if (KeyIsPrimitive)
                return dataverseEntity[LogicalPrimaryKey].Cast<TKey>()!;
            else
                return dataverseEntity[LogicalPrimaryKey].ToString()!.FromJson<TKey>();
        }
        public async Task CheckIfExistColumnsAsync()
        {
            var serviceClient = GetClient();
            foreach (var property in Properties)
            {
                RetrieveAttributeRequest attributeRequest = new()
                {
                    EntityLogicalName = LogicalTableName,
                    LogicalName = property.Name.ToLower(),
                    RetrieveAsIfPublished = true
                };
                var attributeResponse = await Try.WithDefaultOnCatchAsync(
                    () => serviceClient.ExecuteAsync(attributeRequest));
                if (attributeResponse.Entity == null)
                {
                    RetrieveAttributeRequest attributeRequestWithPrefix = new()
                    {
                        EntityLogicalName = LogicalTableName,
                        LogicalName = $"{Prefix}{property.Name.ToLower()}",
                        RetrieveAsIfPublished = true
                    };
                    var attributeResponseWithPrefix = await Try.WithDefaultOnCatchAsync(
                        () => serviceClient.ExecuteAsync(attributeRequestWithPrefix));

                    if (attributeResponseWithPrefix.Entity == default)
                    {
                        CreateAttributeRequest createAttribute = new()
                        {
                            EntityName = LogicalTableName,
                            Attribute = new StringAttributeMetadata
                            {
                                SchemaName = $"{Prefix}{property.Name}",
                                RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                                MaxLength = property.IsPrimitive ? 100 : 2000,
                                FormatName = StringFormatName.Text,
                                DisplayName = new Label(property.Name, 1033),
                                Description = new Label(property.Name, 1033)
                            }
                        };
                        var creationAttributeResponse = await Try.WithDefaultOnCatchAsync(
                            () => serviceClient.ExecuteAsync(createAttribute));
                        if (creationAttributeResponse.Entity == default)
                            throw new ArgumentException($"Impossible to create a new column for {property.Name} for {TableName} entity.");
                    }
                }
                else
                    property.Prefix ??= string.Empty;
            }
        }
        public ServiceClient GetClient() => new($"Url=https://{Environment}.dynamics.com;AuthType=ClientSecret;ClientId={ApplicationIdentity!.ClientId};ClientSecret={ApplicationIdentity!.ClientSecret};RequireNewInstance=true");
    }
}
