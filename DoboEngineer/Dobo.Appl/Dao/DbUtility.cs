using FreeSql.DataAnnotations;
using FreeSql.Internal.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Dao;

    internal class DbUtility
    {
        
        static Lazy<IFreeSql> mssqlLazy = new Lazy<IFreeSql>(() =>
        {
            FreeSql.Internal.Utils.IsStrict = false;
            var fsql = new FreeSql.FreeSqlBuilder()
                .UseAdoConnectionPool(true)
                .UseConnectionString(FreeSql.DataType.SqlServer, SysCfg.Cfg.MssqlConnStr)
#if DEBUG
                //.UseAutoSyncStructure(true) 
                .UseMonitorCommand(cmd => Trace.WriteLine($"Sql：{cmd.CommandText}"))
#endif
                .Build();
            return fsql;
        });
        static Lazy<IFreeSql> sqliteLazy = new Lazy<IFreeSql>(() =>
        {
            FreeSql.Internal.Utils.IsStrict = false;
            var fsql = new FreeSql.FreeSqlBuilder()
                .UseAdoConnectionPool(true)
               .UseConnectionString(FreeSql.DataType.Sqlite, SysCfg.Cfg.SqliteConnStr)
#if DEBUG
                .UseAutoSyncStructure(true)
                .UseMonitorCommand(cmd => Trace.WriteLine($"Sql：{cmd.CommandText}"))
#endif
                 
                .Build();

            FreeSql.Internal.Utils.TypeHandlers.TryAdd(typeof(DateTimeOffset), new DateTimeOffsetTypeHandler());
            return fsql;
        });

        public  static IFreeSql MainDb => mssqlLazy.Value;
        public static IFreeSql LiteDb => sqliteLazy.Value;
    }
class DateTimeOffsetTypeHandler : TypeHandler<DateTimeOffset>
{
    public override object Serialize(DateTimeOffset value) => value.ToString("o", CultureInfo.InvariantCulture);
    public override DateTimeOffset Deserialize(object value) => DateTimeOffset.TryParse((string)value, out var dts) ? dts : DateTimeOffset.MinValue;
    public override void FluentApi(ColumnFluent col) => col.MapType(typeof(string)).DbType("datetime");
}
