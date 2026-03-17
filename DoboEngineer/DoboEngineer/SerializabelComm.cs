using Dobo.Appl;
using DoboEngineer.Pump;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace DoboEngineer
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(PumpModel2))]
    [JsonSerializable(typeof(PumpModel2[]))]
    [JsonSerializable(typeof(PumpModel))]
    [JsonSerializable(typeof(PumpModel[]))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
  
    }
}
