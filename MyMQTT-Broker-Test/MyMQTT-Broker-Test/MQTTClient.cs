using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MyMQTT_Broker_Test
{
    public class MQTTClient
    {
        public string ClientIdentifier { get; set; }
        public TcpClient TcpClient { get; set; }

        public MQTTClient(TcpClient client, string clientIdentifier)
        {
            this.TcpClient = client;
            this.ClientIdentifier = clientIdentifier; 
        }
    }
}
