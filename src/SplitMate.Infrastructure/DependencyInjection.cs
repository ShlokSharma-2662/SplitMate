using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SplitMate.Application.Common.Interfaces;
using SplitMate.Infrastructure.Identity;
using SplitMate.Infrastructure.Persistence;
using SplitMate.Infrastructure.Persistence.Repositories;

namespace SplitMate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Registers both IDbContextFactory (used by repositories; safe for Blazor
        // Server circuits) and a scoped SplitMateDbContext (used by Identity stores).
        services.AddDbContextFactory<SplitMateDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ISettlementRepository, SettlementRepository>();
        services.AddScoped<IUserDirectory, UserDirectory>();

        return services;
    }
}
