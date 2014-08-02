using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PRMasterServer.Servers
{
    public class NatNegMessage
    {
        public int Constant; // always 1e 66 6a b2
        public byte ProtocolVersion;
        public byte RecordType;
        public byte[] RecordSpecificData;

        public int ClientId;
        public byte SequenceId; // ? (0x00 to 0x03)
        public byte Hoststate; // (0x00 for guest, 0x01 for host)
        public byte UseGamePort;
        public string PrivateIPAddress;
        public short LocalPort;
        public string GameName;

        public override string ToString()
        {
            XmlSerializer ser = new XmlSerializer(typeof(NatNegMessage));
            System.IO.StringWriter writer = new System.IO.StringWriter();
            ser.Serialize(writer, this);
            return writer.ToString();
        }

        public static NatNegMessage ParseData(byte[] bytes)
        {
            if (bytes.Length < 8) return null;
            if (bytes[0] != 0xFD || bytes[1] != 0xFC) return null;
            NatNegMessage msg = new NatNegMessage();
            msg.Constant = _toInt(_getBytes(bytes, 2, 4));
            msg.ProtocolVersion = bytes[6];
            msg.RecordType = bytes[7];
            if (bytes.Length > 8) msg.RecordSpecificData = _getBytes(bytes, 8, bytes.Length - 8);
            if (msg.RecordType == 0)
            {
                // INIT
                msg.ClientId =  _toInt(_getBytes(msg.RecordSpecificData, 0, 4));
                msg.SequenceId = msg.RecordSpecificData[4];
                msg.Hoststate = msg.RecordSpecificData[5];
                msg.UseGamePort = msg.RecordSpecificData[6];
                msg.PrivateIPAddress = _toIpAddress(_getBytes(msg.RecordSpecificData, 7, 4));
                msg.LocalPort = _toShort(_getBytes(msg.RecordSpecificData, 11, 2));
                msg.GameName = _toString(_getBytes(msg.RecordSpecificData, 13, msg.RecordSpecificData.Length-13));
            }
            return msg;
        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(0xFD);
            bytes.Add(0xFC);
            bytes.Add(0x1E);
            bytes.Add(0x66);
            bytes.Add(0x6a);
            bytes.Add(0xb2);
            bytes.Add(ProtocolVersion);
            bytes.Add(RecordType);
            if (RecordType == 1)
            {
                // INIT_ACK (0x01)
                _addInt(bytes, ClientId);
                bytes.Add(SequenceId);
                bytes.Add(Hoststate);
                _addBytes(bytes, 0xFF, 0xFF); //ff ff		unknown_5. Always 0xffff
                _addBytes(bytes, 0x6d, 0x16, 0xb5, 0x7d, 0xea); //	unknown_6. Any useless constant (also for other games).
            }
            return bytes.ToArray();
        }

        private static string _toString(byte[] bytes) {
            List<byte> bs = new List<byte>();
            for (int i = 0; i < bytes.Length && bytes[i] > 0; i++)
                bs.Add(bytes[i]);
            return Encoding.ASCII.GetString(bs.ToArray());
        }

        private static void _addBytes(List<byte> bytes, params byte[] adds)
        {
            bytes.AddRange(adds);
        }

        private static void _addInt(List<byte> bytes, int value)
        {
            List<byte> b = new List<byte>(BitConverter.GetBytes((int)value));
            if (BitConverter.IsLittleEndian)
            {
                b.Reverse();
            }
            while (b.Count < 4) b.Insert(0, 0);
            bytes.AddRange(b);
        }

        private static int _toInt(byte[] bytes) {
            return (int)bytes[0] * 256 * 256 * 256 + (int)bytes[1] * 256 * 256 + (int)bytes[2] * 256 + bytes[3];
        }

        private static short _toShort(byte[] bytes) {
            return (short)(bytes[0] * 256 + bytes[1]);
        }

        private static string _toIpAddress(byte[] bytes) {
            return string.Join(".", bytes.Select((b) => { return b.ToString(); }));
        }

        private static byte[] _getBytes(byte[] bytes, int index, int nofBytes) {
            byte[] result = new byte[nofBytes];
            for(int i = 0; i < nofBytes; i++) {
                if(index+i >= bytes.Length) result[i] = 0; else result[i] = bytes[index+i];
            }
            return result;
        }
    }
}
