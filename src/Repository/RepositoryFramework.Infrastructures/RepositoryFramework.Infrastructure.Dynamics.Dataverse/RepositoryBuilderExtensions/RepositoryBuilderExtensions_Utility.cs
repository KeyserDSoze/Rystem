using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using RepositoryFramework.Infrastructure.Dynamics.Dataverse;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        private static async Task DataverseCreateTableOrMergeNewColumnsInExistingTableAsync<T, TKey>(
            DataverseRepository<T, TKey>? repository)
            where TKey : notnull
        {
            var serviceClient = repository!.Options!.Client;
            RetrieveEntityRequest retrieveEntityRequest = new()
            {
                LogicalName = repository.Settings.LogicalTableName,
                EntityFilters = EntityFilters.All
            };
            var response = await Try.WithDefaultOnCatchAsync(
                () => serviceClient.ExecuteAsync(retrieveEntityRequest)).NoContext();
            if (response.Entity == default)
            {
                CreateEntityRequest createrequest = new()
                {
                    SolutionUniqueName = repository.Settings.SolutionName,
                    Entity = new EntityMetadata
                    {
                        SchemaName = repository.Settings.TableNameWithPrefix,
                        DisplayName = new Label(repository.Settings.TableName, 1033),
                        DisplayCollectionName = new Label(repository.Settings.TableName, 1033),
                        Description = new Label(repository.Settings.Description ?? $"A table to store information about {repository.Settings.TableName} entity.", 1033),
                        OwnershipType = OwnershipTypes.UserOwned,
                        IsActivity = false,

                    },
                    PrimaryAttribute = new StringAttributeMetadata
                    {
                        SchemaName = repository.Settings.PrimaryKeyWithPrefix,
                        RequiredLevel = new AttributeRequiredLevelManagedProperty(AttributeRequiredLevel.None),
                        MaxLength = 100,
                        FormatName = StringFormatName.Text,
                        DisplayName = new Label(repository.Settings.PrimaryKey, 1033),
                        Description = new Label($"The primary attribute for the {repository.Settings.TableName} entity.", 1033)
                    }

                };
                var creationResponse = await Try.WithDefaultOnCatchAsync(
                    () => serviceClient.ExecuteAsync(createrequest)).NoContext();
                if (creationResponse.Entity == default)
                    throw new ArgumentException($"Error in table creation for {repository.Settings.TableName}");
            }
            await repository.Settings.CheckIfExistColumnsAsync().NoContext();
        }
    }
}
