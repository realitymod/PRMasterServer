using Alivate;
using MaxMind.GeoIP2;
using PRMasterServer.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace PRMasterServer.Servers
{
	internal class ServerNatNeg
	{
		private const string Category = "NatNegotiation";

		public Action<string, string> Log = (x, y) => { };
		public Action<string, string> LogError = (x, y) => { };

		public Thread Thread;

		private const int BufferSize = 65535;
		private Socket _socket;
		private SocketAsyncEventArgs _socketReadEvent;
		private byte[] _socketReceivedBuffer;

		// 09 then 4 00's then battlefield2
		private readonly byte[] _initialMessage = new byte[] { 0x09, 0x00, 0x00, 0x00, 0x00, 0x62, 0x61, 0x74, 0x74, 0x6c, 0x65, 0x66, 0x69, 0x65, 0x6c, 0x64, 0x32, 0x00 };

		public ServerNatNeg(IPAddress listen, ushort port, Action<string, string> log, Action<string, string> logError)
		{
			Log = log;
			LogError = logError;

			GeoIP.Initialize(log, Category);

			Thread = new Thread(StartServer) {
				Name = "Server NatNeg Socket Thread"
			};
			Thread.Start(new AddressInfo() {
				Address = listen,
				Port = port
			});
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			try {
				if (disposing) {
					if (_socket != null) {
						_socket.Close();
						_socket.Dispose();
						_socket = null;
					}
				}
			} catch (Exception) {
			}
		}

		~ServerNatNeg()
		{
			Dispose(false);
		}

        private byte[] ParseHexString(string hex)
        {
            string[] hexes = hex.Split(' ');
            return hexes.Select((h) => { return (byte)Convert.ToInt32(h, 16); }).ToArray();
        }

		private void StartServer(object parameter)
		{
			AddressInfo info = (AddressInfo)parameter;

			Log(Category, "Starting Nat Neg Listener");

            //byte[] response = ProcessMessage(ParseHexString("FD FC 1E 66 6A B2 03 00 11 6C 5D 5E 03 01 01 C0 A8 00 C7 08 0E 63 69 76 34 62 74 73 00"));
            //if (response != null)
            //{
            //    Log(Category, "Response: ");
            //    Log(Category, string.Join(" ", response.Select((b) => { return b.ToString("X2"); }).ToArray()));
            //}

			try {
				_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) {
					SendTimeout = 5000,
					ReceiveTimeout = 5000,
					SendBufferSize = BufferSize,
					ReceiveBufferSize = BufferSize
				};

				_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
				_socket.Bind(new IPEndPoint(info.Address, info.Port));

				_socketReadEvent = new SocketAsyncEventArgs() {
					RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0)
				};
				_socketReceivedBuffer = new byte[BufferSize];
				_socketReadEvent.SetBuffer(_socketReceivedBuffer, 0, BufferSize);
				_socketReadEvent.Completed += OnDataReceived;
			} catch (Exception e) {
				LogError(Category, String.Format("Unable to bind Server List Reporting to {0}:{1}", info.Address, info.Port));
				LogError(Category, e.ToString());
				return;
			}

			WaitForData();
		}

		private void WaitForData()
		{
			Thread.Sleep(10);
			GC.Collect();

			try {
				_socket.ReceiveFromAsync(_socketReadEvent);
			} catch (SocketException e) {
				LogError(Category, "Error receiving data");
				LogError(Category, e.ToString());
				return;
			}
		}

        private byte[] ProcessMessage(byte[] receivedBytes)
        {
            // there by a bunch of different message formats...
            Log(Category, "Received bytes: ");
            Log(Category, string.Join(" ", receivedBytes.Select((b) => { return b.ToString("X2"); }).ToArray()));
            NatNegMessage message = null;
            try
            {
                message = NatNegMessage.ParseData(receivedBytes);
            }
            catch (Exception ex)
            {
                LogError(Category, ex.ToString());
            }
            if (message == null)
            {
                Log(Category, "Message is not a NatNeg message.");
                return null;
            }
            else
            {
                Log(Category, "Parsed message:");
                Log(Category, message.ToString());
                if (message.RecordType == 0)
                {
                    // INIT, return INIT_ACK
                    message.RecordType = 1;
                    return message.ToBytes();
                }
                else
                {
                    return null;
                }
            }
        }

		private void OnDataReceived(object sender, SocketAsyncEventArgs e)
		{
            // Description                                   gamename        gamekey
            // Civilization IV: Beyond the Sword             civ4bts         Cs2iIq

            // NatNeg implementation:
            // See http://aluigi.altervista.org/papers/gsnatneg.c
            // Protocol specification:
            // http://wiki.tockdom.com/wiki/MKWii_Network_Protocol/Server/mariokartwii.natneg.gs.nintendowifi.net
			try {
				IPEndPoint remote = (IPEndPoint)e.RemoteEndPoint;

				byte[] receivedBytes = new byte[e.BytesTransferred];
				Array.Copy(e.Buffer, e.Offset, receivedBytes, 0, e.BytesTransferred);
                byte[] response = ProcessMessage(receivedBytes);
                if (response != null)
                {
                    Log(Category, "Sending response: ");
                    Log(Category, string.Join(" ", response.Select((b) => { return b.ToString("X2"); }).ToArray()));
                    _socket.SendTo(response, remote);
                }
			} catch (Exception ex) {
				LogError(Category, ex.ToString());
			}

			WaitForData();
		}

    }
}
