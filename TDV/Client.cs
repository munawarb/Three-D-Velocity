/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.ExtendedAudio;
namespace TDV
{
	public enum MessageType : byte
	{
		normal,
		enterRoom,
		leaveRoom,
		critical,
		privateMessage
	}

	class ClientRecord
	{
		private MemoryStream m_data, m_deferred;
		private MemoryStream m_previousObjectUpdate;
		private int m_dataCount, m_deferredCount;
		public void setPreviousObjectUpdate(MemoryStream stream)
		{
			m_previousObjectUpdate.SetLength(0);
			m_previousObjectUpdate.Position = 0;
			stream.WriteTo(m_previousObjectUpdate);
			m_previousObjectUpdate.Position = 0;
		}
		public MemoryStream previousObjectUpdate
		{
			get { return m_previousObjectUpdate; }
		}



		public int dataCount
		{
			get { return m_dataCount; }
			set { m_dataCount = value; }
		}
		public int deferredCount
		{
			get { return m_deferredCount; }
			set { m_deferredCount = value; }
		}


		public MemoryStream deferred
		{
			get { return m_deferred; }
		}

		public MemoryStream data
		{
			get { return m_data; }
		}


		public ClientRecord()
		{
			deferredCount = 0;
			dataCount = 0;
			m_deferred = new MemoryStream();
			m_data = new MemoryStream();
			m_previousObjectUpdate = new MemoryStream();
		}

	}

	public enum OnlineRole : byte
	{
		none,
		receiver,
		sender,
		bot
	}
	public enum ObjectType : byte
	{
		aircraft,
		carrierBlue,
		carrierGreen,
		carrierRed,
		carrierYellow
	}

	public static class Client
	{
		[Flags]
		public enum LoginMessages
		{
			none = 0,
			noCallSign = 1,
			demo = 2,
			wrongCredentials = 4,
			unauthorized = 8,
			serverAssignedTag = 16,
			badVersion = 32,
			messageOfTheDay = 64
		}

		public enum Fields: byte
		{
			damage = 1,
			direction = 2,
			x = 3,
			y = 4,
			z = 5,
			speed = 6,
			throttlePosition = 7,
			isOnRunway = 8,
			afterburnersActive = 9,
			cloakStatus = 10
		}

		private static List<ChatRoomMember> members;
		private static bool m_spec;
		public static bool spectatorPending
		{
			get { return m_spec; }
			set
			{
				if (value)
				{
					System.Diagnostics.Trace.WriteLine("Set spectator pending to true!");
					System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
					System.Diagnostics.Trace.WriteLine(trace.ToString());
				}
				m_spec = value;
			}
		}

		public static int chatPointer
		{
			get;
			set;
		}
		private static List<String> chatMessages;

		private static ExtendedAudioBuffer chatSound, chatEnterSound, chatLeaveSound, privateMessageSound, serverMessageSound;
		private static byte[] responseStream;
		private static AutoResetEvent waitingForResponse;
		private static LoginMessages m_messages;
		private static AddOnArgs[] addOns;
		private static bool m_hostStartedGame;

		/// <summary>
		/// Indicates whether or not the host has started the game yet.
		/// </summary>
		public static bool hostStartedGame
		{
			get { return Client.m_hostStartedGame; }
			set { Client.m_hostStartedGame = value; }
		}
		private static Dictionary<String, ClientRecord> senders;
		private static int[] ports = null;
		private static bool m_gameHost;
		public static bool gameHost
		{
			get { return m_gameHost; }
			set { m_gameHost = value; }
		}
		private static int m_port;
		public static int port
		{
			get { return m_port; }
			set { m_port = value; }
		}
		private static StreamWriter theFile;
		private static bool log = false;
		private static bool isConnected, error;
		private static bool live;
		private static TcpClient client;
		private static String m_serverTag;
		private static Thread processThread;
		private static long m_next;
		public static object dataLocker, chatLocker;
		public static bool closed
		{
			get { return !live; }
		}
		public static long next { get { return m_next; } set { m_next = value; } }
		public static string serverTag
		{
			get { return m_serverTag; }
			set { m_serverTag = value; }
		}

		public static bool connect(String host, String callSign, int port)
		{
				  ports = new int[]{4444, 4445, 4567, 6969, 32000 };
			if (dataLocker == null)
				dataLocker = new object();
			if (chatLocker == null)
				chatLocker = new object();
			if (chatMessages == null)
				chatMessages = new List<String>();
			if (members == null)
				members = new List<ChatRoomMember>();
			chatPointer = 0;
			chatSound = DSound.LoadSound(DSound.SoundPath + "\\chat1.wav");
			chatEnterSound = DSound.LoadSound(DSound.SoundPath + "\\chat2.wav");
			chatLeaveSound = DSound.LoadSound(DSound.SoundPath + "\\chat3.wav");
			privateMessageSound = DSound.LoadSound(DSound.SoundPath + "\\chat4.wav");
			serverMessageSound = DSound.LoadSound(DSound.SoundPath + "\\chat5.wav");
			senders = new Dictionary<string, ClientRecord>();
			waitingForResponse = new AutoResetEvent(false);
			isConnected = false; error = false;
			live = false;
			client = new TcpClient();
			int i = (port != 0) ? Array.IndexOf(ports, port) : 0;
			int time = 0;

			while (i < ports.Length)
			{
				error = false;
				time = 0;
				client.BeginConnect(host, port = ports[i++],
					new AsyncCallback(connectedEvent), null);
				while (!isConnected && !error)
				{
					Application.DoEvents();
					if (time >= 3000)
					{
						endConnect(); //stop trying to connect on this port
						break;
					}
					time += 100;
					Thread.Sleep(100);
				}
				if (isConnected)
					break;
			} //search ports
			if (!isConnected || error)
				return false;
			Options.writeToFile();
			try
			{
				using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
				{
					writer.Write(callSign);
					writer.Flush();
					CSCommon.sendData(client, writer);
				} //using
				LoginMessages resp = LoginMessages.none;
				using (BinaryReader reader = new BinaryReader(CSCommon.getData(client, 5000)))
				{
					resp = (LoginMessages)reader.ReadInt32();
					m_messages = resp;
					if ((resp & LoginMessages.serverAssignedTag) == LoginMessages.serverAssignedTag)
					{
						serverTag = reader.ReadString();
						String messageOfTheDay = reader.ReadString();
						if ((resp & LoginMessages.messageOfTheDay) == LoginMessages.messageOfTheDay)
						{
							// We now need to speak the message and then show an input box for the user to
							// press ENTER to continue. This is because some screen readers
							// Don't have a way to stop the running thread.
							SapiSpeech.speak("[Welcome message]: " + messageOfTheDay + " (press ENTER to continue)", SapiSpeech.SpeakFlag.interruptable);
							Common.mainGUI.receiveInput().Trim();
						}
							System.Diagnostics.Trace.WriteLine("Server sent tag: " + serverTag);
					}
				} //using
				if ((resp & LoginMessages.demo) == LoginMessages.demo)
					BPCSharedComponent.ExtendedAudio.DSound.playAndWait(BPCSharedComponent.ExtendedAudio.DSound.NSoundPath + "\\cd" + Common.getRandom(1, 2) + ".wav");
				if ((resp & LoginMessages.noCallSign) == LoginMessages.noCallSign)
					BPCSharedComponent.ExtendedAudio.DSound.playAndWait(BPCSharedComponent.ExtendedAudio.DSound.NSoundPath + "\\ncs.wav");
				if ((resp & LoginMessages.badVersion) == LoginMessages.badVersion)
				{
					SapiSpeech.speak("There is a newer version of TDV available. Please update before logging on.", SapiSpeech.SpeakFlag.noInterrupt);
					return false;
				}
				if ((resp & LoginMessages.wrongCredentials) == LoginMessages.wrongCredentials)
				{
					BPCSharedComponent.ExtendedAudio.DSound.playAndWait(BPCSharedComponent.ExtendedAudio.DSound.NSoundPath + "\\pw2.wav");
					return false;
				}
			}
			catch (IOException)
			{
				error = true;
			}
			catch (TimeoutException)
			{
				error = true;
			}
			if (error)
				return false;

			if (log)
				theFile = new StreamWriter(Addendums.File.appPath + "\\server_output.log");
			live = true;
			processThread = new Thread(processRCV);
			processThread.Start();
			return true;
		}

		private static void connectedEvent(IAsyncResult result)
		{
			try
			{
				client.EndConnect(result);
				isConnected = true;
			}
			catch (SocketException)
			{
				error = true;
			}
		}

		/// <param name="forceUpdate">Set to true if you want to send the data, even if it is a duplicate. This is useful if you want to send
		/// the final data string to the server.</param>
		public static void sendObjectUpdate(MemoryStream stream, String id, bool forceUpdate)
		{
			ClientRecord cr = senders[id];
			if (!forceUpdate && stream.Length != cr.previousObjectUpdate.Length)
			{
				bool areEqual = true;
				for (int i = 1; i <= stream.Length; i++)
				{
					areEqual = stream.ReadByte() == cr.previousObjectUpdate.ReadByte();
					if (!areEqual)
						break;
				}
				if (areEqual)
					return;
			}
			cr.setPreviousObjectUpdate(stream);
			CSCommon.sendData(client, stream);
			stream.Close();
		}

		/// <summary>
		/// Sends data to the server.  Appropriate tags will be prepended to the data,
		/// so all the client has to do is send the update.
		/// This method will also take care of duplicates. We don't need to flood the server with objects whose states are not changing,
		/// so this method will not send data if it is the same as what it sent last time. This is useful for consecutive, repeated commands.
		/// </summary>
		/// <param name="info">The data to send, usually obtained by calling CompleteBuild()</param>
		/// <param name="id">The client ID of the sender.</param>
		public static void sendObjectUpdate(MemoryStream stream, String id)
		{
			sendObjectUpdate(stream, id, false);
		}

		public static void sendCommand(byte command, params Object[] args)
		{
			sendData(CSCommon.buildCMDString(command, args));
		}

		private static void processRCV()
		{
			try
			{
				while (live)
				{
					if (!CSCommon.isLiveConnection(client))
					{
						live = false;
						SapiSpeech.speak("Error: Server crash.", SapiSpeech.SpeakFlag.noInterrupt);
						Common.exitMenus = true;
						Common.repop();
						return;
					}
					MemoryStream stream = null;
					//Bytes have to be explicitly copied into new stream since cmds is closed to save memory later on, so we'll lose rcvPauseData.
					stream = CSCommon.getData(client);
					BinaryReader cmds = null;
					if (stream != null)
						cmds = new BinaryReader(stream);
					if (cmds != null)
					{
						sbyte t;
						long start = 0; //start position of current packet
						while (cmds.BaseStream.Length > cmds.BaseStream.Position)
						{
							start = cmds.BaseStream.Position;
							System.Diagnostics.Trace.WriteLine(String.Format("S: {0}, L: {1}", start, cmds.BaseStream.Length));
							t = cmds.ReadSByte();
							if (t == 1)
							{
								byte command = cmds.ReadByte();
								System.Diagnostics.Trace.WriteLine("co " + command);
								switch (command)
								{
									case CSCommon.cmd_addMember:
										addMember(cmds.ReadString(), cmds.ReadString());
										break;

									case CSCommon.cmd_removeMember:
										removeMember(cmds.ReadString());
										break;

									case CSCommon.cmd_resp:
										int respLength = cmds.ReadInt32();
										responseStream = new byte[respLength];
										cmds.BaseStream.Read(responseStream, 0, respLength);
										waitingForResponse.Set();
										break;

									case CSCommon.cmd_notifyDemo:
										DSound.PlaySound(DSound.LoadSound(DSound.NSoundPath + "\\cd3.wav"), true, false);
										break;

									case CSCommon.cmd_newval:
										SelfVoice.purge(true);
										int amount = cmds.ReadInt32();
										SelfVoice.NLS("#" + amount + "&points.wav", true, true);
										addChatMessage(String.Format("You earned {0} point{1}", amount, (amount == 1) ? "" : "s"));
										break;

									case CSCommon.cmd_position:
										next = cmds.ReadInt64();
										addOns = processAddOns(cmds);
										break;

									case CSCommon.cmd_chat:
										MessageType type = (MessageType)cmds.ReadByte();
										if (type == MessageType.normal)
											DSound.PlaySound(chatSound, true, false);
										else if (type == MessageType.enterRoom)
											DSound.PlaySound(chatEnterSound, true, false);
										else if (type == MessageType.leaveRoom)
											DSound.PlaySound(chatLeaveSound, true, false);
										else if (type == MessageType.privateMessage)
											DSound.PlaySound(privateMessageSound, true, false);
										else
											DSound.PlaySound(serverMessageSound, true, false);
										String incomingChatMessage = cmds.ReadString();
										SapiSpeech.speak(incomingChatMessage, SapiSpeech.SpeakFlag.interruptable);
										addChatMessage(incomingChatMessage);
										Common.mainGUI.addToHistory(incomingChatMessage);
										break;

									case CSCommon.cmd_serverMessage:
										String incomingServerMessage = cmds.ReadString();
										SapiSpeech.speak(incomingServerMessage, SapiSpeech.SpeakFlag.interruptable);
										addChatMessage(incomingServerMessage);
										break;

									case CSCommon.cmd_forceDisconnect: //Player was disconnected from the server
										//By the server itself and not through an in-game event,
										//so if we receive this command, we are being told to wipe our copy of the player in question.
										//Still, the server will send forceDisconnect even if object has already been disconnected
										//due to in-game event. This is ok since if the object is already gone,
										//this command will do nothing. It is just a redundancy check, more or less.
										String idToTerminate = cmds.ReadString();
										Projector pToTerm = Interaction.objectAt(idToTerminate);
										if (pToTerm != null)
											pToTerm.requestingTerminate();
										Interaction.clearLocks(idToTerminate);
										break;

									case CSCommon.cmd_requestCreate: //Response from server for requestCreate command sent by client.
										Interaction.createPlayer(cmds.ReadString(), cmds.ReadInt32());
										addSender(Mission.player.id);
										if (addOns != null)
											Mission.player.setAddOns(addOns);
										addOns = null;
										if (Options.mode == Options.Modes.teamDeath)
											Mission.player.team = Options.team;
										Interaction.startMultiplayerGame();
										break;

									case CSCommon.cmd_distributeServerTag:
										Projector o = Interaction.createObjectFromServer(cmds.ReadString(), cmds.ReadString(), OnlineRole.receiver, (ObjectType)cmds.ReadByte());
										System.Diagnostics.Trace.WriteLine("Received request to create opponent " + o.name);
										if (Options.mode == Options.Modes.teamDeath)
											o.team = (Projector.TeamColors)cmds.ReadInt32();
										AddOnArgs[] distAdd = processAddOns(cmds);
										if (distAdd != null)
											o.setAddOns(distAdd);
										break;

									case CSCommon.cmd_createBot:
										String createBotId = cmds.ReadString();
										String createBotName = cmds.ReadString();
										Projector bot = Interaction.createObjectFromServer(createBotId, createBotName, OnlineRole.bot, (ObjectType)cmds.ReadByte()); //will create bot or update current receiver to bot status.
										addSender(bot.id);
										break;

									case CSCommon.cmd_startGame:
										hostStartedGame = true;

										System.Diagnostics.Trace.WriteLine("Host started game");
										break;

									case CSCommon.cmd_gameEnded:
										Options.serverEndedGame = true;
										break;
								} //switch
								System.Diagnostics.Trace.WriteLine("Command: " + t);
								continue;
							} //if command

							System.Diagnostics.Trace.Write(String.Format("Stream position: {0}, start: {1}, Char: {2} ", (int)cmds.BaseStream.Position, start, t));
							int size = cmds.ReadInt32(); //total update size
							System.Diagnostics.Trace.WriteLine("Size: " + size);
							int temSize = size;
							String tag = cmds.ReadString();
							System.Diagnostics.Trace.WriteLine(tag + " queuing data...");
							Projector p = null;
							size = size - (int)(cmds.BaseStream.Position - start);
							byte[] buffer = null;
							try
							{
								buffer = new byte[size];
							}
							catch (OverflowException)
							{
								throw new OverflowException(String.Format("Size: {0}, Stream position: {1}, start: {2}, original size: {3} Char: {4}", size, (int)cmds.BaseStream.Position, start, temSize, t));
							}
							cmds.BaseStream.Read(buffer, 0, size);

							//Object could have been deleted by the time this command is reached
							if ((p = Interaction.objectAt(tag)) != null)
							{
								System.Diagnostics.Trace.WriteLine("Object found. sending queue");
								p.queueData(t, buffer);
							} //if object exists
							else
								System.Diagnostics.Trace.WriteLine("Object not found.");
						} //while more data to read
						cmds.Close();
					} //if got data
					Thread.Sleep(50);
				} //while live connection
			}
			catch (Exception e)
			{
				Common.handleError(e);
			} //catch
			finally
			{
				releaseConnection();
			}
		}

		public static MemoryStream completeBuild(Projector source,
		 bool includeCommonAttributes)
		{
			MemoryStream mem = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(mem);
			ClientRecord cr = senders[source.id];
			Aircraft a = null;
			if (source is Aircraft)
				a = (Aircraft)source;
			System.Diagnostics.Trace.WriteLine("Called complete build");
			if (a != null)
				System.Diagnostics.Trace.WriteLine("with " + a.name);
			if (includeCommonAttributes && source.role == OnlineRole.bot)
				writer.Write((sbyte)3);
			else
				writer.Write((sbyte)2);
			writer.Flush();
			long pos = mem.Position;
			writer.Write((uint)0);
			writer.Write(source.id);
			writer.Flush();
			long numArgs = mem.Position;
			writer.Write((ushort)0);
			ushort size = 2;
			writer.Write((byte)Fields.damage);
			writer.Write(source.damage);
			writer.Write((byte)Fields.direction);
			writer.Write(source.direction);
			if (includeCommonAttributes)
			{
				writer.Write((byte)Fields.x);
				writer.Write(source.x);
				writer.Write((byte)Fields.y);
				writer.Write(source.y);
				writer.Write((byte)Fields.z);
				writer.Write(source.z);
				writer.Write((byte)Fields.speed);
				writer.Write(source.speed);
				size += 4;
				if (a != null)
				{ //If this is object update for aircraft, need to send
					//throttle and afterburner status also
					writer.Write((byte)Fields.throttlePosition);
					writer.Write(a.getThrottle());
					writer.Write((byte)Fields.isOnRunway);
					writer.Write(a.getRunwayStatus());
					writer.Write((byte)Fields.afterburnersActive);
					writer.Write(a.getAfterburnerStatus());
					writer.Write((byte)Fields.cloakStatus);
					writer.Write(a.getCloakStatus());
					size += 4;
				} //if sending update for aircraft
			}

			if (includeCommonAttributes)
				cr.dataCount += cr.deferredCount;
			writer.Write((short)cr.dataCount);
			if (cr.dataCount > 0)
			{
				writer.Flush();
				cr.data.WriteTo(mem);
				cr.data.SetLength(0);
				cr.dataCount = 0;
			}

			if (includeCommonAttributes && cr.deferredCount > 0)
			{
				writer.Flush();
				cr.deferred.WriteTo(mem);
				cr.deferred.SetLength(0);
				cr.deferredCount = 0;
			}
			mem.Position = numArgs;
			writer.Write(size);
			mem.Position = pos;
			writer.Write((uint)mem.Length);
			writer.Flush();
			mem.Position = 0;
			return mem;
		}

		private static void addData(Aircraft.Action command, MemoryStream stream, params Object[] args)
		{
			BinaryWriter writer = new BinaryWriter(stream);
			writer.Write((byte)command);
			if (args.Length > 0)
			{
				foreach (Object s in args)
				{
					if (s is byte)
						writer.Write((byte)s);
					else if (s is String)
						writer.Write((String)s);
					else
						throw new ArrayTypeMismatchException("The supplied value is not of a supported type.");
				}
			} //if args
			writer.Flush();
		}

		/// <summary>
		/// Adds object data to the given ID's catalog.
		/// </summary>
		/// <param name="command">The action to add.</param>
		/// <param name="id">The ID of the object to catalog.</param>
		/// <param name="args">A list of arguments.</param>
		public static void addData(Aircraft.Action command, String id, params Object[] args)
		{
			ClientRecord cr = senders[id];
			addData(command, cr.data, args);
			cr.dataCount++;
		}

		public static void addDeferred(Aircraft.Action command, String id, params Object[] args)
		{
			ClientRecord cr = senders[id];
			addData(command, cr.deferred, args);
			cr.deferredCount++;
		}

		public static void sendData(MemoryStream data)
		{
			lock (dataLocker)
				CSCommon.sendData(client, data);
		}

		public static void sendData(String data)
		{
			lock (dataLocker)
				CSCommon.sendData(client, data);
		}

		public static MemoryStream getData(int ms)
		{
			MemoryStream data = null;
			while (data == null)
				data = CSCommon.getData(client, ms, true);
			return data;
		}

		public static void closeConnection()
		{
			live = false;
		}

		private static void releaseConnection()
		{
			if (client.Connected)
			{
				client.GetStream().Close();
				client.Close();
				client = null;
			}
			DSound.unloadSound(ref chatEnterSound);
			DSound.unloadSound(ref chatLeaveSound);
			DSound.unloadSound(ref privateMessageSound);
			DSound.unloadSound(ref chatSound);
			DSound.unloadSound(ref serverMessageSound);
			chatMessages.Clear();
			chatPointer = 0;
			if (log)
				theFile.Close();
			Common.mainGUI.leaveChat();
		}
		/// <summary>
		/// Use this method for calls that require immediate responses from the server, such as an
		/// acknowledgement of success or another condition.
		/// This method will send the command to the server and wait for a response. It will also stop the monitoring thread from consuming the response.
		/// </summary>
		/// <param name="cmd">The command stream to send</param>
		/// <returns>The response from the server</returns>
		public static BinaryReader getResponse(MemoryStream cmd)
		{
			CSCommon.sendData(client, cmd);
			waitingForResponse.WaitOne();
			MemoryStream stream = new MemoryStream(responseStream);
			stream.Position = 0;
			BinaryReader reader = new BinaryReader(stream);
			return reader;
		}

		/// <summary>
		/// Use this method for calls that require immediate responses from the server, such as an
		/// acknowledgement of success or another condition.
		/// This method will send the specified string to the server and wait for a response. It will also stop the monitoring thread from consuming the response.
		/// </summary>
		/// <param name="cmd">The command string to send</param>
		/// <returns>The response from the server</returns>
		public static BinaryReader getResponse(String cmd)
		{
			return getResponse(new MemoryStream(Encoding.ASCII.GetBytes(cmd)));
		}


		private static void endConnect()
		{
			try
			{
				client.EndConnect(null);
			}
			catch
			{
			}
		}

		/// <summary>
		/// Signals whether or not a new player has joined this game. This flag can be used to determine
		/// whether or not to send initialization data so the newly joined player can sync with this object.
		/// </summary>
		public static void setNewJoin()
		{
			//newJoin = true;
		}

		/// <summary>
		/// Sends a chat message to the room.
		/// </summary>
		public static void sendChatMessage()
		{
			SapiSpeech.speak("Enter message", SapiSpeech.SpeakFlag.interruptable);
			String chatMsg = Common.mainGUI.receiveInput();
			if (!chatMsg.Equals(""))
				sendPublicChatMessage(chatMsg);
			else //canceled chat
				SapiSpeech.speak("Canceled", SapiSpeech.SpeakFlag.interruptable);
		}

		public static void sendPublicChatMessage(String message)
		{
			if (!String.IsNullOrEmpty(message))
			{
				SapiSpeech.speak("You say: " + message, SapiSpeech.SpeakFlag.interruptable);
				sendCommand(CSCommon.cmd_chat, false, message);
			}
		}

		/// <summary>
		/// Sends a chat message to the specified tag.
		/// </summary>
		/// <param name="tag">The ID of the player to send the message to</param>
		public static void sendChatMessage(String tag)
		{
			SapiSpeech.speak("Enter message", SapiSpeech.SpeakFlag.interruptable);
			String chatMsg = Common.mainGUI.receiveInput();
			if (!chatMsg.Equals(""))
			{
				SapiSpeech.speak("You say: " + chatMsg, SapiSpeech.SpeakFlag.interruptable);
				sendCommand(CSCommon.cmd_chat, true, tag, chatMsg);
			}
			else //canceled chat
				SapiSpeech.speak("Canceled", SapiSpeech.SpeakFlag.interruptable);
		}

		/// <summary>
		/// Joins FFA. If the player lost and confirmed that they want spectator mode,
		/// we essentially reenter them into the game with the spectator flag.
		/// </summary>
		public static void joinFFA()
		{
			int entryModeFFA = getEntryMode();
			if (entryModeFFA == -1)
				return;
			sendCommand(CSCommon.cmd_joinFreeForAll, entryModeFFA);
			Options.mode = Options.Modes.freeForAll;
			Interaction.inOnlineGame = true;
		}

		/// <summary>
		///  Gets the desired entry mode such as Pilot or Spectator.
		/// </summary>
		/// <returns>The entry mode.</returns>
		public static int getEntryMode()
		{
			if (spectatorPending)
				return Options.entryMode = 1;
			int s = Common.returnSvOrSr(() => Common.sVGenerateMenu("", new String[] { "menuc_2_1.wav", "menuc_2_2.wav" }), () => Common.GenerateMenu("", new string[] { "Be a pilot", "Act as spectator" }), Options.menuVoiceMode);
			if (s == -1)
				return -1;
			Options.entryMode = s;
			return s;
		}

		private static AddOnArgs[] processAddOns(BinaryReader reader)
		{
			short addOnLength = reader.ReadInt16();
			if (addOnLength == 0)
				return null;

			AddOnArgs[] addOnsCol = new AddOnArgs[addOnLength];
			for (int addOnIndex = 0; addOnIndex < addOnsCol.Length; addOnIndex++)
				addOnsCol[addOnIndex] = new AddOnArgs(reader.ReadInt32(), reader.ReadInt32());
			return addOnsCol;
		}

		/// <summary>
		/// Adds a catalog for the given id.
		/// </summary>
		/// <param name="id">The id to add</param>
		public static void addSender(String id)
		{
			senders[id] = new ClientRecord();
		}

		/// <summary>
		/// Sends a create bot command to the server.
		/// </summary>
		public static void addBot()
		{
			if (Options.mode == Options.Modes.teamDeath)
				return;
			Client.sendData(CSCommon.buildCMDString(CSCommon.cmd_createBot));
		}

		/// <summary>
		/// Sends a remove bot command to the server.
		/// </summary>
		public static void removeBot()
		{
			if (Options.mode == Options.Modes.teamDeath)
				return;
			Client.sendData(CSCommon.buildCMDString(CSCommon.cmd_removeBot));
		}

		public static void clearServerData()
		{
			gameHost = false;
			hostStartedGame = false;
			if (senders != null)
				senders.Clear();
		}

		public static LoginMessages getMessages()
		{
			return m_messages;
		}

		public static void whoIs()
		{
			String[] ids = null;
			String[] names = null;
			using (BinaryReader r = getResponse(CSCommon.buildCMDString(CSCommon.cmd_whois)))
			{
				ids = new String[r.ReadInt16()];
				System.Diagnostics.Trace.WriteLine("Doing whois");
				System.Diagnostics.Trace.WriteLine(ids.Length);
				names = new String[ids.Length];
				for (int i = 0; i < ids.Length; i++)
				{
					System.Diagnostics.Trace.WriteLine("Iteration " + i);
					ids[i] = r.ReadString();
					System.Diagnostics.Trace.WriteLine(ids[i]);
					names[i] = r.ReadString();
					System.Diagnostics.Trace.WriteLine(names[i]);
				}
			} //using
			int choice = Common.GenerateMenu("Press ENTER on a user to send a private message to them", names, Common.getIncDecVol());
			if (choice == -1)
				return;
			if (ids[choice].Equals(serverTag))
				SapiSpeech.speak("The first sign of insanity is talking to yourself. Sorry, we can't let you do that!", SapiSpeech.SpeakFlag.interruptable);
			else
				sendChatMessage(ids[choice]);
		}

		public static void adminMenu()
		{
			bool isAdmin = false;
			using (BinaryReader r = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_requestAdmin)))
			{
				isAdmin = r.ReadBoolean();
			}
			if (!isAdmin)
				return;

			int choice = Common.GenerateMenu(null, new String[] { "Set message of the day", "Reboot server" });
			switch (choice)
			{
				case 0:
					String msg = Common.mainGUI.receiveInput();
					if (String.IsNullOrEmpty(msg))
						break;
					sendCommand(CSCommon.cmd_setMessage, msg);
					break;

				case 1:
					sendCommand(CSCommon.cmd_reboot);
					break;

				default:
					return;
			}
		}

		public static void prevMessage()
		{
			lock (chatLocker)
			{
				if (chatMessages.Count == 0)
					return;
				SapiSpeech.speak(chatMessages[(chatPointer == 0) ? 0 : --chatPointer], SapiSpeech.SpeakFlag.interruptableButStop);
			}
		}
		public static void nextMessage()
		{
			lock (chatLocker)
			{
				if (chatMessages.Count == 0)
					return;
				SapiSpeech.speak(chatMessages[(chatPointer == chatMessages.Count - 1) ? chatPointer : ++chatPointer], SapiSpeech.SpeakFlag.interruptableButStop);
			}
		}

		public static void copyMessage()
		{
			if (chatMessages.Count == 0)
				return;
			Common.mainGUI.copyMessage();
			SapiSpeech.speak("Copied message " + chatMessages[chatPointer], SapiSpeech.SpeakFlag.interruptableButStop);
		}

		public static String getCurrentMessage()
		{
			return chatMessages[chatPointer];
		}

		/// <summary>
		/// Ask the player if they want to enter spectator mode
		/// </summary>
		/// <returns>True if they do, false otherwise</returns>
		public static bool askForSpectator()
		{
			if (spectatorPending)
			{
				int choice = Common.sVGenerateMenu("aspec.wav", new String[] { "kd4.wav", "kd3.wav" });
				spectatorPending = choice == 0;
			}
			return spectatorPending;
		}

		public static void addChatMessage(String message)
		{
			lock (chatLocker)
				chatMessages.Add(message);
		}

		public static void commitMembers()
		{
			Common.mainGUI.commit(members.ToArray());
		}

		/// <summary>
		/// Adds a member to the chat room.
		/// </summary>
		/// <param name="id">The id of the member to add</param>
		/// <param name="name">The name of the member</param>
		public static void addMember(String id, String name)
		{
			ChatRoomMember member = new ChatRoomMember(id, name);
			members.Add(member);
			Common.mainGUI.add(member);
		}

		/// <summary>
		/// Removes the member with the specified id.
		/// </summary>
		/// <param name="id">The id to remove</param>
		public static void removeMember(String id)
		{
			ChatRoomMember m = new ChatRoomMember(id, null);
			members.Remove(m);
			Common.mainGUI.remove(m);
		}


	} //class
} //namespace
