﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RepositoryFramework.Test.Domain;
using RepositoryFramework.Test.Infrastructure.EntityFramework;
using RepositoryFramework.Test.Infrastructure.EntityFramework.Models.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUserRepositoryWithDatabaseSqlAndEntityFramework(this IServiceCollection services,
            IConfiguration configuration, string? name = null)
        {
            services.AddDbContext<SampleContext>(options =>
            {
                options.UseSqlServer(configuration["ConnectionString:Database"]);
            }, ServiceLifetime.Scoped);

            services.AddRepository<AppUser, AppUserKey>(repositoryBuilder =>
                {
                    repositoryBuilder.SetStorage<AppUserStorage>(name);
                    repositoryBuilder.Translate<User>()
                        .With(x => x.Id, x => x.Identificativo)
                        .With(x => x.Username, x => x.Nome)
                        .With(x => x.Email, x => x.IndirizzoElettronico);
                    repositoryBuilder
                        .AddBusiness()
                            .AddBusinessBeforeInsert<AppUserBeforeInsertBusiness>()
                            .AddBusinessBeforeInsert<AppUserBeforeInsertBusiness2>();
                });

            services
                .AddRepository<MappingUser, int>(builder =>
                {
                    builder.WithEntityFramework<MappingUser, int, User, SampleContext>(
                        t =>
                        {
                            t.DbSet = x => x.Users;
                            t.References = x => x.Include(x => x.IdGruppos);
                        });
                    builder.Translate<User>()
                        .With(x => x.Username, x => x.Nome)
                        .With(x => x.Username, x => x.Cognome)
                        .With(x => x.Email, x => x.IndirizzoElettronico)
                        .With(x => x.Groups, x => x.IdGruppos)
                        .With(x => x.Id, x => x.Identificativo)
                        .WithKey(x => x, x => x.Identificativo);
                    builder
                        .AddBusiness()
                            .AddBusinessBeforeInsert<MappingUserBeforeInsertBusiness>()
                            .AddBusinessBeforeInsert<MappingUserBeforeInsertBusiness2>();
                });

            services
                .AddRepository<User, int>(x =>
                {
                    x.WithEntityFramework<User, int, SampleContext>(x =>
                    {
                        x.DbSet = x => x.Users;
                        x.References = x => x.Include(x => x.IdGruppos);
                    })
                        .WithKey(x => x, x => x.Identificativo);
                });
            services.AddBusinessForRepository<User, int>()
                .AddBusinessBeforeInsert<UserBeforeInsertBusiness>()
                .AddBusinessBeforeInsert<UserBeforeInsertBusiness2>();
            return services;
        }
    }
}
