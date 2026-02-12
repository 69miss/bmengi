using Dobo.Appl.Device;
using Instrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.HunterCmd
{
    public class HTAdapter : IProtocolAdapter
    {
        ProductSetup nowProduct;
        string ip;
        ushort port;
        TCPIP tcp;
        public string ConnectionString { get;private set; }

        public bool IsConnected => tcp.IsConnected();

        public string ProtocolName =>"HTST";

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<Tuple<string, object>> DataReceived;

        public HTAdapter(string ip, ushort port)
        {
            this.ip = ip;
            this.port = port;
            ConnectionString = $"{ip}:{port}";
        }
        public HTAdapter():this("192.168.0.55", 10001) { }
        public Task<bool> ConnectAsync()
        {
            if (tcp != null && tcp.IsConnected())
            {
                tcp.CloseTCPIPPort();
            }
            tcp = new TCPIP();
            tcp.RemoteHost = ip;
            tcp.RemotePort = port;
            tcp.LongTimeout = 3000;
            tcp.Message += Tcp_Message;
            tcp.AsynchData += Tcp_AsynchData;//AsynchData
            tcp.Timeout += Tcp_Timeout;
            tcp.OpenTCPIPPort();
            return Task.FromResult(tcp.IsConnected());
        }
        private void Tcp_Timeout(string aCommand, short Attempts)
        {

            DataReceived?.Invoke(this,Tuple.Create("HT.Tcp_Timeout" ,(object) Attempts));
        }

        private void Tcp_AsynchData(string reStr)
        {
            DataReceived?.Invoke(this, Tuple.Create("HT.Tcp_AsynchData", (object)reStr));
        }
        private void Tcp_Message(string reStr)
        {
            DataReceived?.Invoke(this, Tuple.Create("HT.Tcp_Message", (object)reStr));
        }
        public Task DisconnectAsync()
        {
            if (tcp != null && tcp.IsConnected())
            {
                tcp.CloseTCPIPPort();
            }
            return Task.CompletedTask;
        }

        public Task<T> ReadAsync<T>(string address)
        {
            lock (tcp)
            {
                if (tcp.IsConnected())
                {

                    var re = tcp.SendCommand(address);
                    if (re is T reT)
                    {
                        return Task.FromResult(reT);
                    }
                    return Task.FromResult((T)(object)re);
                }
                throw new ArgumentException("未连接");
            }
        }

        public Task<IDictionary<string, object>> ReadBatchAsync(IEnumerable<string> addresses)
        {
            throw new NotImplementedException();
        }

        public Task<bool> WriteAsync<T>(string address, T value)
        {
            throw new NotImplementedException();
        }
    }
}
