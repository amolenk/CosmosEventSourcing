using Newtonsoft.Json.Linq;

namespace Projections
{
    public interface IView
    {
        JObject Payload { get; set; }
    }
}