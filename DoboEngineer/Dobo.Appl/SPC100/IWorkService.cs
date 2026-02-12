using Dobo.Appl.Entity;
using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.SPC100
{
    public interface IWorkService: IDisposable
    {
        Action<Result<OpticalData>> PhotometricDataReturnHandle { get; set; }
        Action<Result<Tuple<SpcStateInfo, OpticalData[]>>> StateHandle { get; set; }
        public Action<Tuple<int, string, object>> MsgInfoHandle { get; set; }
        Task StandardSetAsync(Action<string> step);
        Task StartAsync();
        Task StopAsync();
 
    }
}
