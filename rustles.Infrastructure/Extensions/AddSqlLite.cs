using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace rustles.Infrastructure.Registration
{
    public static class AddSqlLite
    {
        public static IServiceCollection AddUserDataSqlite(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<UsersDataContext>(options =>
            {
                options.UseSqlite(connectionString);
            });

            return services;
        }
    }
}
