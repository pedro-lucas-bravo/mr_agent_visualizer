//
//	  UnityOSC - Open Sound Control interface for the Unity3d game engine
//
//	  Copyright (c) 2012 Jorge Garcia Martin
//
// 	  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// 	  documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// 	  the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// 	  and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// 	  The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// 	  of the Software.
//
// 	  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// 	  TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// 	  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// 	  CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// 	  IN THE SOFTWARE.
//

using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

#if ENABLE_WINMD_SUPPORT
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#else
using System.Net.Sockets;
using System.Threading;
#endif



namespace UnityOSC
{
    public delegate void PacketReceivedEventHandler(OSCServer sender, OSCPacket packet);

	/// <summary>
	/// Receives incoming OSC messages
	/// </summary>
	public class OSCServer
    {
        #region Delegates
        public event PacketReceivedEventHandler PacketReceivedEvent;
        #endregion

        #region Constructors
        public OSCServer (int localPort)
		{
            PacketReceivedEvent += delegate(OSCServer s, OSCPacket p) { };

			_localPort = localPort;
			Connect();
		}
		#endregion
		
		#region Member Variables

#if ENABLE_WINMD_SUPPORT
		DatagramSocket socket;
		private OSCPacket _lastReceivedPacket;
#else
		private UdpClient _udpClient;
		private Thread _receiverThread;
		private OSCPacket _lastReceivedPacket;
#endif
        private int _localPort;
        private int _sleepMilliseconds = 10;
		#endregion

		#region Properties
#if ENABLE_WINMD_SUPPORT
		private async void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
		Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
		{		
			// lock multi event 
			socket.MessageReceived -= Socket_MessageReceived;

			byte[] bytes = null;
			using (DataReader dataReader = args.GetDataReader()) {
                // Get unread buffer size
                uint length = dataReader.UnconsumedBufferLength;
                // Byte order acquisition (little endian or big endian)
                // It is planned to transmit in BINARY communication mode, so it must be little endian
                ByteOrder order = dataReader.ByteOrder;
                // Read received data
                bytes = new byte[length];
                dataReader.ReadBytes(bytes);  
				
				//Test += "\nTESTX...";
				//for (int i = 0; i < aucRecvPacket.Length; i++) {
				//	Test += "\n" + aucRecvPacket[i];
				//}
            }

			OSCPacket packet = OSCPacket.Unpack(bytes);
			_lastReceivedPacket = packet;

			PacketReceivedEvent(this, _lastReceivedPacket);	

			// unlock multi event 
			socket.MessageReceived += Socket_MessageReceived;
		}

#else
		public UdpClient UDPClient
		{
			get
			{
				return _udpClient;
			}
			set
			{
				_udpClient = value;
			}
		}
#endif
		
		public int LocalPort
		{
			get
			{
				return _localPort;
			}
			set
			{
				_localPort = value;
			}
		}
		
		public OSCPacket LastReceivedPacket
		{
			get
			{
				return _lastReceivedPacket;
			}
		}

		/// <summary>
		/// "Osc Receive Loop" sleep duration per message.
		/// </summary>
		/// <value>The sleep milliseconds.</value>
		public int SleepMilliseconds
		{
			get
			{
				return _sleepMilliseconds;
			}
			set
			{
				_sleepMilliseconds = value;
			}
		}
        #endregion

#region Methods

/// <summary>
/// Opens the server at the given port and starts the listener thread.
/// </summary>
/// 
#if ENABLE_WINMD_SUPPORT
        public async void Connect()
        {
            Debug.Log("Waiting for a connection...");
            socket = new DatagramSocket();
            socket.MessageReceived += Socket_MessageReceived;

            try
            {
                await socket.BindEndpointAsync(null, _localPort.ToString());

            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                Debug.Log(SocketError.GetStatus(e.HResult).ToString());
                return;
            }

        }
#else
        public void Connect()
		{
			if(this._udpClient != null) Close();
			try
			{
				_udpClient = new UdpClient(_localPort);
				_receiverThread = new Thread(new ThreadStart(this.ReceivePool));
				_receiverThread.Start();
			}
			catch(Exception e)
			{
				throw e;
			}
		}
#endif


        /// <summary>
        /// Closes the server and terminates its listener thread.
        /// </summary>
        public void Close()
		{
#if ENABLE_WINMD_SUPPORT
			socket.Dispose();
#else
			if(_receiverThread !=null) _receiverThread.Abort();
			_receiverThread = null;
			_udpClient.Close();
			_udpClient = null;
#endif
		}

		//public static string Test = "";

#if !ENABLE_WINMD_SUPPORT
		/// <summary>
		/// Receives and unpacks an OSC packet.
		/// A <see cref="OSCPacket"/>
		/// </summary>
		private void Receive()
		{
			IPEndPoint ip = null;
			
			//try
			//{
				byte[] bytes = _udpClient.Receive(ref ip);

				if(bytes != null && bytes.Length > 0)
				{					
                    OSCPacket packet = OSCPacket.Unpack(bytes);

                    _lastReceivedPacket = packet;

                    PacketReceivedEvent(this, _lastReceivedPacket);	
				}
			//}
			//catch (Exception e){
				//throw new Exception(String.Format("Can't create server at port {0}: {1}", _localPort, e.Message));
			//	throw e;
  			//}
		}		
		
		/// <summary>
		/// Thread pool that receives upcoming messages.
		/// </summary>
		private void ReceivePool()
		{
			while( true )
			{
				Receive();
				
				Thread.Sleep(_sleepMilliseconds);
			}
		}
#endif
 #endregion
    }
}

