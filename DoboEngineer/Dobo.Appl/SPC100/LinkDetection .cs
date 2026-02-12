using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.SPC100;
public class LinkDetection
{
    public static Result<long> Detection(string ip)
    {

        Ping p1 = new Ping();
        PingReply reply = p1.Send(ip); //发送主机名或Ip地址
        if (reply.Status == IPStatus.Success)
        {
            return Result.Success(reply.RoundtripTime);
        }
        return Result.Error<long>(reply.Status + "");
    }
}

