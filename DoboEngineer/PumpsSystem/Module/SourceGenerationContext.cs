using Dobo.Appl;
using PumpsSystem.Pump;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace PumpsSystem.Module;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PumpModel))]
[JsonSerializable(typeof(PumpModel[]))] 
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
