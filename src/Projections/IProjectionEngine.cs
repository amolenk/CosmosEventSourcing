using System.Threading.Tasks;

namespace Projections
{
    public interface IProjectionEngine
    {
        void RegisterProjection(IProjection projection);

        Task StartAsync(string instanceName);

        Task StopAsync();
    }
}