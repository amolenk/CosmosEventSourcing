using System.Threading.Tasks;
using Demo.Domain;

namespace Demo.Domain
{
    public interface IMeterRepository
    {
        Task<Meter> LoadMeterAsync(string id);

        Task<bool> SaveMeterAsync(Meter meter);
    }
}