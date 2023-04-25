using log4net;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Configuration;

namespace ZudamalMavjiSomonServices
{
    static class SocketConnect
    {
        static private readonly ILog _log = LogManager.GetLogger(typeof(SocketConnect));
        static private string _response;
        static public bool RequestPassed { get; set; }

        static public void Request(string message)
        {
            string address = ConfigurationManager.AppSettings["providerIP"];
            int port = Convert.ToInt32(ConfigurationManager.AppSettings["providerPort"]);
            StringBuilder result = new StringBuilder();
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);
            _response = null;
            RequestPassed = false;

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(ipPoint);
                byte[] data = Encoding.UTF8.GetBytes(message);
                socket.Send(data);
                data = new byte[256];
                int bytes = 0;

                _log.Info($"Request: \n\n{message}\n");

                do
                {
                    bytes = socket.Receive(data, data.Length, 0);
                    result.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (socket.Available > 0);

                RequestPassed = true;

                _log.Info($"Response: \n\n{result}\n");
            }

            _response = result.ToString();
        }

        static public string Response()
        {
            return _response;
        }

    }
}
