using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;


namespace rustles.Infrastructure.Registration
{
    public static class AddSqlUserData
    {
        public static IServiceCollection AddUserDataSqlServer(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<UsersDataContext>(x =>
            {
                x.EnableSensitiveDataLogging()
                .UseSqlServer(connectionString);
            });
            return services;
        }
    }
}
