using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using RepositoryFramework;
using RepositoryFramework.Infrastructure.Dynamics.Dataverse;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        private static async Task DataverseCreateTableOrMergeNewColumnsInExistingTableAsync<T, TKey>(
            DataverseOptions<T, TKey> options)
            where TKey : notnull
        {
            var serviceClient = options.GetClient();
            RetrieveEntityRequest retrieveEntityRequest = new()
            {
                LogicalName = options.LogicalTableName,
                EntityFilters = EntityFilters.All
            };
            var response = await Try.WithDefaultOnCatchAsync(
                () => serviceClient.ExecuteAsync(retrieveEntityRequest));
            if (response.Entity == default)
            {
                CreateEntityRequest createrequest = new()
                {
                    SolutionUniqueName = options.SolutionName,
                    Entity = new EntityMetadata
                    {
                        SchemaName = options.TableNameWithPrefix,
                        DisplayName = new Label(options.TableName, 1033),
                        DisplayCollectionName = new Label(options.TableName, 1033),
                        Description = new Label(options.Description ?? $"A table to store information about {options.TableName} entity.", 1033),
                        OwnershipType = OwnershipTypes.UserOwned,
                        IsActivity = false,

                    },
                    PrimaryAttribute = new StringAttributeMetadata
                    {
                        SchemaName = options.PrimaryKeyWithPrefix,
                        RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                        MaxLength = 100,
                        FormatName = StringFormatName.Text,
                        DisplayName = new Label(options.PrimaryKey, 1033),
                        Description = new Label($"The primary attribute for the {options.TableName} entity.", 1033)
                    }

                };
                var creationResponse = await Try.WithDefaultOnCatchAsync(
                    () => serviceClient.ExecuteAsync(createrequest));
                if (creationResponse.Entity == default)
                    throw new ArgumentException($"Error in table creation for {options.TableName}");
            }
            await options.CheckIfExistColumnsAsync();
        }
    }
}
