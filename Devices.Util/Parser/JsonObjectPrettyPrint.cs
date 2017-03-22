using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace Devices.Util.Parser
{
    public static class JsonObjectPrettyPrint
    {

        public static string PrettyPrint(this JsonObject jsonObject)
        {
            StringBuilder builder = new StringBuilder();
            StringReader reader = new StringReader(jsonObject.Stringify());

            return builder.ToString();
        }


    }
}
