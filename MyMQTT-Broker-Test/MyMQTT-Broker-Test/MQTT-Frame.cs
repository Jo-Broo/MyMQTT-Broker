using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyMQTT_Broker_Test
{
    /*
     Ich muss generell nochmal die ganze Struktur überarbeiten, eine erste verbindung hab ich bereits hinbekommnen
     jetzt muss ich noch das disconnect hinbekommen dann hab ich schon einen guten Anfang aber wie gesagt übertarbeite nochmal
     die ganze struktur mit den Klassen das wird sonst doof
     */

    public static class MQTTFrameIdentifier
    {
        public static PacketType GetPacketTypeOfFrame(byte[] data)
        {
            byte controlfield = data[0];
            byte packettype = (byte)(controlfield >> 4);

            return (PacketType)packettype;
        }
    }

    /// <summary>
    /// Basis Klasse für alle Arten von Frames
    /// </summary>
    public class MQTTFrame
    {
        /// <summary>
        /// enthält den kompletten Frame 
        /// </summary>
        public byte[] rawData;
        /// <summary>
        /// enthält den Fixed Header Teil
        /// </summary>
        public byte[] fixedHeader;
        /// <summary>
        /// enthält den Variable Header Teil
        /// </summary>
        public byte[]? variableHeader;
        /// <summary>
        /// enthält den Payload Teil
        /// </summary>
        public byte[]? payload;
        /// <summary>
        /// index zum analysieren des Frames
        /// </summary>
        public int index;
        /// <summary>
        /// Referenz für den Packettype
        /// </summary>
        public PacketType PacketType;

        public int remainingLength;

        public MQTTFrame() { }
        public MQTTFrame(byte[] data) 
        { 
            this.rawData = data;
            this.index = 0;
            this.ParseFixedHeader();
        }

        internal void ParseFixedHeader()
        {
            #region Fixed Header
            byte controlfield = this.rawData[index++];
            byte packettype = (byte)(controlfield >> 4);
            byte flags = (byte)(controlfield & 0x0F);

            // Remaining Length
            this.remainingLength = 0;
            int multiplier = 1;
            byte encodedByte;
            do
            {
                encodedByte = this.rawData[index++];
                this.remainingLength += (encodedByte & 127) * multiplier;
                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            this.fixedHeader = new byte[index];
            Array.Copy(this.rawData, 0, this.fixedHeader, 0, index);
            #endregion
        }

        internal bool isBitset(byte data, int bit)
        {
            return (data & (1 << bit)) != 0;
        }
    }

    public class MQTTCONNECT : MQTTFrame
    {
        public string ProtokollName { get; private set; }
        public bool UsernameFlag { get; private set; }
        public bool PasswordFlag { get; private set; }
        public bool RetainFlag { get; private set; }
        public QualityOfService QoSLevel { get; private set; }
        public bool LastWillFlag { get; private set; }
        public bool CleanFlag { get; private set; }
        public int KeepAlive {  get; private set; }

        
        public string ClientIdentifier { get; private set; }

        // Konstruktor, der das empfangene Daten-Array parst
        public MQTTCONNECT(byte[] data) : base(data)
        {
            this.rawData = data;
            this.ParseVariableHeader();
            this.ParsePayLoad();

            if (this.variableHeader == null) { this.variableHeader = new byte[1]; }
            if (this.payload == null) { this.payload = new byte[1]; }
        }

        private void ParseVariableHeader()
        {
            #region Variable Length Header
            int startIndex = index;
            byte[] protocolNameLength = new byte[]
            {
            this.rawData[index++],
            this.rawData[index++]
            };
            int length = (protocolNameLength[0] << 8) | protocolNameLength[1];
            byte[] protocolName = new byte[length];
            Array.Copy(this.rawData, index, protocolName, 0, length);
            this.ProtokollName = System.Text.Encoding.ASCII.GetString(protocolName, 0, protocolName.Length);
            index += length;

            byte protocolLevel = this.rawData[index++];
            byte connectFlag = this.rawData[index++];
            this.UsernameFlag = this.isBitset(connectFlag, 7);
            this.PasswordFlag = this.isBitset(connectFlag, 6);
            this.RetainFlag = this.isBitset(connectFlag, 5);
            this.QoSLevel = (QualityOfService)((connectFlag & 0xC) >> 2);
            this.LastWillFlag = this.isBitset(connectFlag, 2);
            this.CleanFlag = this.isBitset(connectFlag, 1);
            byte[] keepAlive = new byte[]
            {
            this.rawData[index++],
            this.rawData[index++]
            };
            this.KeepAlive = (keepAlive[0] << 8) | keepAlive[1];

            int variableHeaderLength = index - startIndex;
            this.remainingLength -= variableHeaderLength;

            this.variableHeader = new byte[variableHeaderLength];
            Array.Copy(this.rawData, startIndex, this.variableHeader, 0, variableHeaderLength);
            #endregion
        }

        private void ParsePayLoad()
        {
            #region Payload
            this.payload = new byte[this.remainingLength];
            Array.Copy(this.rawData, index, this.payload, 0, this.remainingLength);

            byte[] length_clientIdentifier = new byte[]
                {
                        this.rawData[index++],
                        this.rawData[index++]
                };
            int length = (length_clientIdentifier[0] << 8) | length_clientIdentifier[1];
            byte[] data = new byte[length];
            Array.Copy(this.rawData, index, data, 0, length);
            index += length;
            this.ClientIdentifier = System.Text.Encoding.ASCII.GetString(data, 0, length);
            #endregion
        }

        public override string ToString()
        {
            return $"PacketType: {this.PacketType}, Protokoll: {this.ProtokollName} FixedHeader: {BitConverter.ToString(this.fixedHeader)}, VariableHeader: {BitConverter.ToString(this.variableHeader)}, Payload: {BitConverter.ToString(this.payload)}, Client: {this.ClientIdentifier}";
        }
    }

    public class MQTTCONNACK : MQTTFrame
    {
        public bool SessionsPresent { get; internal set; }
        private byte connectReturnCode;
        public MQTTCONNACK() : base()
        {
            this.PacketType = PacketType.CONNACK;
            this.remainingLength = 2;
            this.SessionsPresent = false;
            this.connectReturnCode = 0;
        }

        public byte[] GetFrame()
        {
            byte[] frame = new byte[4];
            frame[0] = 0x20; // CONNACK Pakettyp und Flags (0010 0000)
            frame[1] = 0x02; // Remaining Length (2 Bytes)
            frame[2] = 0x00; // Connect Acknowledge Flags
            frame[3] = 0x00; // Connect Return Code (0 = Connection Accepted)

            return frame;
        }
    }

    public class MQTTSUBSCRIBE : MQTTFrame 
    {
        // TODO
    }

    public class MQTTSUBACK : MQTTFrame
    {
        // TODO
    }

    public enum PacketType
    {
        CONNECT = 1,
        CONNACK = 2,
        DISCONNECT = 14,
        PUBLISH = 3,
        PUBACK = 4
        //PUBREC = 5,
        //PUBREL = 6,
        //PUBCOMP = 7,
        //SUBSCRIBE = 8,
        //SUBACK = 9,
        //UNSUBSCRIBE = 10,
        //UNSUBACK = 11,
        //PINGREQ = 12,
        //PINGRESP = 13,
    }

    public enum QualityOfService
    {
        AtMostOnce = 0,
        AtLeastOnce = 1,
        ExactlyOnce = 2
    }

}
