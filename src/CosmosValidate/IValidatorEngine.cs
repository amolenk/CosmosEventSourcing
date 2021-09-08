using System.Threading.Tasks;

namespace CosmosValidate
{
    public interface IValidatorEngine
    {
        void RegisterComparer(IValidator validator);

        Task StartAsync(string instanceName);

        Task StopAsync();
    }
}