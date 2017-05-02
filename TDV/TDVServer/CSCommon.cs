/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
#define SERVER
#define DEBUG
using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
#if SERVER
namespace TDVServer
#else
namespace TDV
#endif
 {
	public static class CSCommon {
		public enum DataType {
			objectUpdate,
			weaponUpdate,
			command,
			none
		}
		public enum AddOns {
			extraCruiseMissiles = 1,
			extraFuel,
			flyingCruiseMissile,
			missileInterceptor
		}

		public const byte cmd_none = 0,
		 cmd_distributeServerTag = 1,
		 cmd_startGame = 2,
		 cmd_closeGame = 3,
		 cmd_requestCreate = 4,
		 cmd_createGame = 5,
		 cmd_joinGame = 6,
		 cmd_chat = 7,
		 cmd_serverMessage = 8,
		 cmd_joinFreeForAll = 9,
		 cmd_disconnectMe = 10,
				  cmd_forceDisconnect = 11,
				  cmd_requestGameList = 12,
				  cmd_deleteFromGame = 13,
				  cmd_position = 14,
				  cmd_whois = 15,
				  cmd_gameEnded = 16,
				  cmd_updatePoints = 17,
				  cmd_victory = 18,
				  cmd_requestStartGame = 19,
				  cmd_newval = 20,
				  cmd_createBot = 21,
				  cmd_removeBot = 22,
				  cmd_notifyDemo = 23,
				  cmd_viewAddOns = 24,
				  cmd_viewMyAddOns = 25,
				  cmd_incAddOn = 26,
				  cmd_decAddOn = 27,
		cmd_buyAddOn = 28,
		cmd_getStats = 29,
		cmd_createChatRoom = 30,
		cmd_joinChatRoom = 31,
		cmd_leaveChatRoom = 32,
		cmd_viewChatRooms = 33,
		cmd_reboot = 34,
		cmd_requestAdmin = 35,
		cmd_setMessage = 36,
		cmd_resp = 37,
		cmd_test = 38,
		cmd_addMember = 39,
		cmd_removeMember = 40;
		private static int m_secondsTimeout = 5;
		public static int secondsTimeout { get { return m_secondsTimeout; } }

		/// <summary>
		/// Initializes options.
		/// </summary>
		/// <param name="secondsTimeout">Number of seconds before timing out</param>
		public static void initialize(int secondsTimeout) {
			m_secondsTimeout = secondsTimeout;
		}
		/* Returns a valid commandstring
		 * to the caller, in the format cmd:command|arg1&arg2&...&argN
		 * */
		public static MemoryStream buildCMDString(byte command, params Object[] args) {
			MemoryStream m = null;
			BinaryWriter b = new BinaryWriter(m = new MemoryStream());
			b.Write((sbyte)1);
			b.Write(command);
			if (args != null && args.Length > 0) {
				foreach (Object s in args) {
					if (s is long)
						b.Write((long)s);
					else if (s is bool)
						b.Write((bool)s);
					else if (s is String)
						b.Write((String)s);
					else if (s is int)
						b.Write((int)s);
					else if (s is short)
						b.Write((short)s);
					else if (s is byte)
						b.Write((byte)s);
					else if (s is MemoryStream)
						((MemoryStream)s).WriteTo(m);
					else
						throw new ArrayTypeMismatchException("The value supplied is not a supported type.");
				}
			}
			b.Flush();
			m.Position = 0;
			return m;
		}


		/// <summary>
		/// Outputs data to the TCP client.
		/// </summary>
		/// <param name="client">The TCP client to send data to.</param>
		/// <param name="data">The MemoryStream containing the data to transmit.</param>
		/// <returns>True on success, false on failure.</returns>
		public static bool sendData(TcpClient client, MemoryStream data) {
			bool success = false;
			data.Position = 0;
			NetworkStream stream = client.GetStream();
			byte[] buffer = data.ToArray();
			try {
#if SERVER
				stream.BeginWrite(BitConverter.GetBytes(buffer.Length), 0, 4, new AsyncCallback(writeEnded), stream);
				stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(writeEnded), stream);
#else
				stream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
				stream.Write(buffer, 0, buffer.Length);
#endif
				success = true;
			} catch { //disconnected
				success = false;
			}
			return success;
		}

		private static void writeEnded(IAsyncResult r) {
			try {
				NetworkStream stream = (NetworkStream)r.AsyncState;
				stream.EndWrite(r);
			} catch (IOException) {
			} catch (ObjectDisposedException) {
			} catch (Exception e) {
#if SERVER
				Server.output(e.Message + e.StackTrace);
#endif
			}
		}

		public static bool sendData(TcpClient client, BinaryWriter data) {
			data.Flush();
			return sendData(client, (MemoryStream)data.BaseStream);
		}

		public static bool sendData(TcpClient client, String data) {
			return sendData(client, new MemoryStream(Encoding.ASCII.GetBytes(data)));
		}


		public static bool sendData(TcpClient client, byte data) {
			MemoryStream stream = new MemoryStream(new byte[] { data });
			return sendData(client, stream);
		}

		public static bool sendData(TcpClient client, bool data) {
			using (BinaryWriter w = new BinaryWriter(new MemoryStream())) {
				w.Write(data);
				return sendData(client, (MemoryStream)w.BaseStream);
			}
		}

		/* Reads the data from client,
		 * and returns it as a String[].
		 * Each position in the array is a separate command, sent separately.
		 * This way, we can have mutliple commands to execute on one call.
		 * Callers should crawl through the array to account for all commands.
		 * This method WILL NOT return empty commands. They will be ignored.
		 * */
		public static MemoryStream getData(TcpClient client, int ms, bool wait, bool ssl) {
			NetworkStream stream = client.GetStream();
			int msWaited = 0;
			MemoryStream s = null;
			SslStream secureStream = null;

			if (wait && !stream.DataAvailable) {
				while (!stream.DataAvailable) {
					Thread.Sleep(5);
					msWaited += 5;
					if (ms != -1 && msWaited > ms)
						throw new TimeoutException("Timeout occured while waiting for data in getData("
						+ ms + ")");
				} //while
			} //if we want to wait for data

			if (stream.DataAvailable) {
				if (ssl) {
					secureStream = new SslStream(stream, true);

					try {
						secureStream.AuthenticateAsServer(new X509Certificate2("bpcprograms.p12"));
					} catch (Exception e) {
#if SERVER
						Server.output("While authenticating" + e.Message, true);
						Server.output(e.StackTrace, true);
						if (e.InnerException != null)
							Server.output(e.InnerException.Message, true);
#endif
						throw;
					}
				} //if ssl

				byte[] sizeBuffer = new byte[4];
				int sizeSize = 0; //how many bytes of the first int we read
				do {
					sizeSize += (ssl) ? secureStream.Read(sizeBuffer, sizeSize, sizeBuffer.Length - sizeSize) : stream.Read(sizeBuffer, sizeSize, sizeBuffer.Length - sizeSize);
				} while (sizeSize < 4);


				int size = 0; //How many bytes we read.
				int totalSize = 0; //Total bytes read from the stream
				int sizeToRead = BitConverter.ToInt32(sizeBuffer, 0); //How many bytes ultimately make up this packet?

				byte[] buffer = new byte[sizeToRead];
				do {
					if (!ssl && !stream.DataAvailable) {
						DateTime startTime = DateTime.Now;
						do {
							Thread.Sleep(0);
							if (DateTime.Now.Subtract(startTime).TotalSeconds > secondsTimeout) {
#if SERVER
								Server.output("Waited " + secondsTimeout + " seconds. No payload arrived. Returning null.", true);
#else
				System.Diagnostics.Trace.WriteLine("Waited " + secondsTimeout + " seconds. No payload arrived. Returning null.", true);
#endif
								return null;
							}
						} while (!stream.DataAvailable);
					}
					if (ssl)
						size = secureStream.Read(buffer, 0, buffer.Length - totalSize);
					else
						size = stream.Read(buffer, 0, buffer.Length - totalSize);
					totalSize += size;
					if (s == null)
						s = new MemoryStream();
					s.Write(buffer,
					0, size); //don't get 0 bytes at end
				} while (totalSize < sizeToRead);
			} //if data available

			if (s == null)
				return null;
			s.Position = 0;
			return s;
		}

		public static MemoryStream getData(TcpClient client, int ms, bool wait) {
			return getData(client, ms, wait, false);
		}

		/*Gets data, returns null if no data available.*/
		public static MemoryStream getData(TcpClient client) {
			return getData(client, -1, false);
		}

		/*Waits for data for the indicated amount of milliseconds.
		 * Timeout will occur afterwards.
		 * */
		public static MemoryStream getData(TcpClient client, int ms) {
			return getData(client, ms, true);
		}

		public static bool isLiveConnection(TcpClient c) {
			NetworkStream stream = c.GetStream();
			if (c.Client.Poll(1, SelectMode.SelectRead) && !stream.DataAvailable)
				return false;
			return true;
		}

		public static void sendResponse(TcpClient client, MemoryStream data) {
			data.Position = 0;
			sendData(client, buildCMDString(cmd_resp, (int)data.Length, data));
		}

		public static void sendResponse(TcpClient client, BinaryWriter w) {
			w.Flush();
			sendResponse(client, (MemoryStream)w.BaseStream);
		}

		public static void sendResponse(TcpClient client, bool data) {
			sendResponse(client, new MemoryStream((data) ? new byte[] { 1 } : new byte[] { 0 }));
		}

	}
}
