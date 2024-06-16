using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MyMQTT_Broker_Test
{
    public class MQTTBroker
    {
        private TcpListener _server;

        private List<MQTTClient> Clients;

        public MQTTBroker(int port)
        {
            IPAddress own = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToList()[0];
            this._server = new TcpListener(own, 1883);
            this.Clients = new List<MQTTClient>();
        }

        public void Start() 
        { 
            _server.Start();
            Console.WriteLine("Broker gestartet...");

            while (true) 
            {
                var client = this._server.AcceptTcpClient();
                Console.WriteLine("Client connected");
                Task.Run(() => { HandleClient(client); });
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            MQTTClient MQTTclient = null;

            while (client.Connected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    PacketType packetType = MQTTFrameIdentifier.GetPacketTypeOfFrame(buffer);
                    switch (packetType)
                    {
                        case PacketType.CONNECT:
                            MQTTCONNECT Connect = new MQTTCONNECT(buffer);
                            Console.WriteLine(Connect.ToString());

                            var existingClient = this.Clients.FirstOrDefault(x => x.ClientIdentifier == Connect.ClientIdentifier);

                            if (existingClient != null) 
                            {
                                // Boar ich hab halt keine Ahnung von so Client-Server Geschichten ._.
                                //existingClient.TcpClient.Close();
                                Console.WriteLine("Client mit dem selben bereits vorhanden.");
                                break; 
                            }

                            MQTTCONNACK mqttCONNACK = new MQTTCONNACK();
                            Console.WriteLine(BitConverter.ToString(mqttCONNACK.GetFrame()));

                            stream.Write(mqttCONNACK.GetFrame(), 0, mqttCONNACK.GetFrame().Length);
                            Console.WriteLine("Sent CONNACK frame to client.");

                            MQTTclient = new MQTTClient(Connect.ClientIdentifier, client);
                            this.Clients.Add(MQTTclient);
                            break;
                        case PacketType.DISCONNECT:
                            break;
                        case PacketType.PUBLISH:
                            break;
                        default:
                            Console.WriteLine($"Packettype: {packetType} is not supported yet");
                            break;
                    }
                }
            }
            client.Close();
        }
    }
}