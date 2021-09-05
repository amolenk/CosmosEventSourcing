using System.Threading.Tasks;

namespace Migrator
{
    public interface IMigrationEngine
    {
        void RegisterMigration(IMigrator migrator);

        Task StartAsync(string instanceName);

        Task StopAsync();
    }
}