using System.Threading.Tasks;
using Windows.Data.Json;

namespace Devices.Controllers.Base
{
    public delegate Task ControllerActionDelegate(JsonObject data);
}
