using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Dobo.Appl
{
    internal class SerializabelComm
    {
    }
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(SysCfg))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
 
    }
}
