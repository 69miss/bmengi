using Dobo.Appl;
using PumpsSystem.Pump;
using PumpsSystem.Pump2;
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
[JsonSerializable(typeof(Pump2.PumpModel))]
[JsonSerializable(typeof(Pump2.PumpModel[]))]
[JsonSerializable(typeof(IDataItemProp[]))]
[JsonSerializable(typeof(DataItemProp[]))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
