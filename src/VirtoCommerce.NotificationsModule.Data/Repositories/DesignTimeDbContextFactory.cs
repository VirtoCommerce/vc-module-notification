using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VirtoCommerce.NotificationsModule.Data.Repositories
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
    {
        public NotificationDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<NotificationDbContext>();

            builder.UseSqlServer("Data Source=(local);Initial Catalog=VirtoCommerce3target38;Trusted_Connection=True;MultipleActiveResultSets=True;Connect Timeout=30");

            return new NotificationDbContext(builder.Options);
        }
    }
}
