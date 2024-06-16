using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MyMQTT_Broker_Test
{
    internal class MQTTBroker
    {
        private TcpListener _server;

        public MQTTBroker(int port)
        {
            IPAddress own = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToList()[0];
            this._server = new TcpListener(own, 1883);
        }

        public void Start() 
        { 
            _server.Start();
            Console.WriteLine("Broker gestartet...");

            while (true) 
            {
                var client = this._server.AcceptTcpClient();
                Console.WriteLine("Client connected");
                this.HandleClient(client);
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            bool disconnect = false;

            while (client.Connected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    // Parse the incoming frame
                    var frame = new MQTTFrame(buffer);
                    switch (frame.PacketType)
                    {
                        case PacketType.CONNECT:
                            MQTTCONNECT mqttCONNECT = new MQTTCONNECT(buffer);
                            Console.WriteLine(mqttCONNECT.ToString());

                            MQTTCONNACK mqttCONNACK = new MQTTCONNACK();
                            Console.WriteLine(BitConverter.ToString(mqttCONNACK.GetFrame()));

                            stream.Write(mqttCONNACK.GetFrame(), 0, mqttCONNACK.GetFrame().Length);
                            Console.WriteLine("Sent CONNACK frame to client.");
                            break;

                        case PacketType.DISCONNECT:
                            disconnect = true;
                            break;

                        default:
                            Console.WriteLine(BitConverter.ToString(buffer));
                            Console.WriteLine("This packet type is not supported yet.");
                            break;
                    }
                }
                if (disconnect) { break; }
            }

            client.Close();
        }


    }
}