using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using RepositoryFramework.Api.Server.Authorization;

namespace RepositoryFramework.WebApi.Models
{
    public class PolicyHandlerForSuperUser : IRepositoryAuthorization<SuperUser, string>
    {
        public async Task<AuthorizedRepositoryResponse> HandleRequirementAsync(IHttpContextAccessor httpContextAccessor, AuthorizationHandlerContext context, RepositoryRequirement requirement, RepositoryMethods method, string? key, SuperUser? value)
        {
            await Task.CompletedTask;
            return new AuthorizedRepositoryResponse
            {
                Success = true,
                Message = "Error for something"
            };
        }
    }

    public class PlusSuperUserKeyAfterInsert : IRepositoryBusinessAfterInsert<NonPlusSuperUser, PlusSuperUserKey>, IRepositoryBusinessAfterQuery<NonPlusSuperUser, PlusSuperUserKey>
    {
        public int Priority => 3;
        public Task<State<NonPlusSuperUser, PlusSuperUserKey>> AfterInsertAsync(State<NonPlusSuperUser, PlusSuperUserKey> state, Entity<NonPlusSuperUser, PlusSuperUserKey> entity, CancellationToken cancellationToken = default)
            => Task.FromResult(state);
        public Task<Entity<NonPlusSuperUser, PlusSuperUserKey>> AfterQueryAsync(Entity<NonPlusSuperUser, PlusSuperUserKey>? entity, IFilterExpression filter, CancellationToken cancellationToken = default) 
            => Task.FromResult(entity);
    }
    public class PlusSuperUserKeyAfterInsert2 : IRepositoryBusinessAfterInsert<NonPlusSuperUser, NonPlusSuperUserKey>, IRepositoryBusinessAfterQuery<NonPlusSuperUser, NonPlusSuperUserKey>
    {
        public int Priority => 3;
        public Task<State<NonPlusSuperUser, NonPlusSuperUserKey>> AfterInsertAsync(State<NonPlusSuperUser, NonPlusSuperUserKey> state, Entity<NonPlusSuperUser, NonPlusSuperUserKey> entity, CancellationToken cancellationToken = default)
            => Task.FromResult(state);
        public Task<Entity<NonPlusSuperUser, NonPlusSuperUserKey>> AfterQueryAsync(Entity<NonPlusSuperUser, NonPlusSuperUserKey>? entity, IFilterExpression filter, CancellationToken cancellationToken = default)
            => Task.FromResult(entity);
    }
    public class SuperiorUser : CreativeUser
    {
        public SuperiorUser(string email) : base(email)
        {
        }
    }
    public class SuperUser : CreativeUser
    {
        public SuperUser(string email) : base(email)
        {
        }
    }
    public class NonPlusSuperUserKey : IKey
    {
        public string A { get; set; }
        public string B { get; set; }
        public static IKey Parse(string keyAsString)
        {
            var splitted = keyAsString.Split("^^^^");
            return new NonPlusSuperUserKey
            {
                A = splitted[0],
                B = splitted[1]
            };
        }

        public string AsString()
        {
            return $"{A}^^^^{B}";
        }
    }
    public class PlusSuperUserKey
    {
        public string A { get; set; }
        public string B { get; set; }
    }
    public class NonPlusSuperUser : CreativeUser
    {
        public NonPlusSuperUser(string email) : base(email)
        {
        }
    }
    public class CreativeUser
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; }
        [Description("Port is great")]
        public int Port { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
        public CreativeUser(string email)
        {
            Email = email;
        }
    }
}
