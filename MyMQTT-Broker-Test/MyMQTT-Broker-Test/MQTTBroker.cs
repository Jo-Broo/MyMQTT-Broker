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

        //private void HandleClient(TcpClient client)
        //{
        //    NetworkStream stream = client.GetStream();
        //    byte[] buffer = new byte[1024];
        //    MQTTClient MQTTclient = null;

        //    while (client.Connected)
        //    {
        //        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        //        if (bytesRead > 0)
        //        {
        //            PacketType packetType = MQTTFrameIdentifier.GetPacketTypeOfFrame(buffer);
        //            switch (packetType)
        //            {
        //                case PacketType.CONNECT:
        //                    MQTTCONNECT Connect = new MQTTCONNECT(buffer);
        //                    Console.WriteLine(Connect.ToString());

        //                    var existingClient = this.Clients.FirstOrDefault(x => x.ClientIdentifier == Connect.ClientIdentifier);

        //                    if (existingClient != null) 
        //                    {
        //                        // Boar ich hab halt keine Ahnung von so Client-Server Geschichten ._.
        //                        //existingClient.TcpClient.Close();
        //                        Console.WriteLine("Client mit dem selben bereits vorhanden.");
        //                        break; 
        //                    }

        //                    MQTTCONNACK mqttCONNACK = new MQTTCONNACK();
        //                    Console.WriteLine(BitConverter.ToString(mqttCONNACK.GetFrame()));

        //                    stream.Write(mqttCONNACK.GetFrame(), 0, mqttCONNACK.GetFrame().Length);
        //                    Console.WriteLine("Sent CONNACK frame to client.");

        //                    MQTTclient = new MQTTClient(Connect.ClientIdentifier, client);
        //                    this.Clients.Add(MQTTclient);
        //                    break;
        //                case PacketType.DISCONNECT:
        //                    break;
        //                case PacketType.PUBLISH:
        //                    break;
        //                default:
        //                    Console.WriteLine($"Packettype: {packetType} is not supported yet");
        //                    break;
        //            }
        //        }
        //    }
        //    client.Close();
        //}

        public async Task HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            MQTTClient mqttClient = null;

            try
            {
                while (true)
                {
                    // Lesen der Daten vom Stream
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // Wenn keine Daten gelesen wurden, hat der Client die Verbindung geschlossen
                        Console.WriteLine("Client disconnected.");
                        break;
                    }

                    // Der Packettyp wird ermittelt
                    PacketType packetType = MQTTFrameIdentifier.GetPacketTypeOfFrame(buffer);
                    switch (packetType)
                    {
                        case PacketType.CONNECT:
                            // Das Packet wird ausgelesen
                            MQTTCONNECT Connect = new MQTTCONNECT(buffer);
                            //Console.WriteLine(Connect.ToString());

                            // ein neues Objekt wird angelegt
                            mqttClient = new MQTTClient(client, Connect.ClientIdentifier);

                            // hier wird geschaut ob es bereits eine Verbindung gibt mit dem selben Client Identifier
                            MQTTClient existingClient = this.Clients.FirstOrDefault(x => x.ClientIdentifier == mqttClient.ClientIdentifier && x != mqttClient);
                            if (existingClient != null)
                            {
                                Console.WriteLine("Client mit selbem Namen gefunden. \n Verbindung wird beendet.");
                                existingClient.TcpClient.Close();
                            }

                            // Es wird das entsprechende Antwort Packet erstellt und an den Client gesendet
                            MQTTCONNACK mqttCONNACK = new MQTTCONNACK();
                            Console.WriteLine(BitConverter.ToString(mqttCONNACK.GetFrame()));

                            stream.Write(mqttCONNACK.GetFrame(), 0, mqttCONNACK.GetFrame().Length);
                            Console.WriteLine("Sent CONNACK frame to client.");

                            // Der Client wird nun in die Liste der verbundenen Clients hinzugefügt
                            this.Clients.Add(mqttClient);

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
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }
    }
}