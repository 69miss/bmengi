using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
namespace Dobo.Appl
{

    [JsonSerializable(typeof(SysCfg))]
    public partial class SysCfg
    {
        static Lazy<SysCfg> cfg = new Lazy<SysCfg>(() =>
        {
            return SysCfg.GetCfg();
        });
        public static SysCfg Cfg => cfg.Value;
        static string path = "syscfg";
        public static SysCfg GetCfg()
        {
            if (!Path.Exists(path))
                return new SysCfg();
            var str = File.ReadAllText(path);
            var jobj = JsonSerializer.Deserialize<SysCfg>(str, SourceGenerationContext.Default.SysCfg);
            return jobj;
        }
        public void Save(string key, object value)
        {
            using var stream = File.OpenWrite(path);
            JsonSerializer.Serialize(stream, value, SourceGenerationContext.Default.SysCfg);
            stream.Flush();
            stream.Close();
        }
        public string MssqlConnStr { get; set; } = @"Data Source=.;Initial Catalog=LAB;Integrated Security=True;Trust Server Certificate=True";
#if DEBUG
        public string SqliteConnStr { get;  } = @"Data Source=dobo.dat;Password=db@dobo";
#endif
#if !DEBUG
        //public string SqliteConnStr { get;  } = @"Data Source=dobo.dat;";
        public string SqliteConnStr { get;  } = @"Data Source=dobo.dat;Password=db@dobo";
#endif
    }
}
