/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
#define DEBUG
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Text;
using System.Net; //for IPAddress
using System.Net.Sockets; //for TcpClient and listener
using System.Collections; //for ArrayList
using System.Collections.Generic;
using System.IO;

namespace TDVServer {
	[Flags]
	public enum LoginMessages {
		none = 0,
		noCallSign = 1,
		demo = 2,
		wrongCredentials = 4,
		unauthorized = 8,
		serverAssignedTag = 16,
		badVersion = 32
	}

	public enum MessageType : byte {
		normal,
		enterRoom,
		leaveRoom,
		critical,
		privateMessage
	}
	public enum Points {
		wins,
		losses,
		valor
	}

	public enum TeamColors {
		none = -1,
		blue = 0,
		green = 1,
		red = 2,
		yellow = 3
	}

	public enum RoomTypes {
		Open,
		closed,
		password
	}
	public class ChatRoom : IComparable {
		private List<String> ids;

		public String id {
			get;
			set;
		}

		public RoomTypes type {
			get;
			set;
		}

		public String password {
			get;
			set;
		}

		public String friendlyName {
			get;
			set;
		}
		private bool alwaysOpen = true;


		/// <summary>
		/// Creates a new chat room.
		/// </summary>
		/// <param name="id">The id of the chat room.</param>
		/// <param name="friendlyName">The user-friendly name of the chat room.</param>
		/// <param name="id1">The id of the host</param>
		/// <param name="id2">The second participant in a two-person, closed chat, or null. </param>
		/// <param name="password">The room's password, or null for no password.</param>
		public ChatRoom(String id, String friendlyName, String id1, String id2, String password) {
			this.id = id;
			this.friendlyName = friendlyName;
			ids = new List<string>();
			if (id1 != null) {
				alwaysOpen = false;
				ids.Add(id1);
			}
			if (id2 != null) {
				ids.Add(id2);
				type = RoomTypes.closed;
			}
			if (password != null) {
				this.password = password;
				type = RoomTypes.password;
			}
		}


		public void add(String tag) {
			ids.Add(tag);
		}

		public bool remove(String tag) {
			ids.Remove(tag);
			return !alwaysOpen && ids.Count == 0;
		}

		public List<String> getIds() {
			return ids;
		}

		public override bool Equals(object obj) {
			return id.Equals((String)obj);
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		public int CompareTo(object obj) {
			return id.CompareTo(obj);
		}
	}



	public class Player {
		public DateTime logOnTime {
			get;
			set;
		}

		public bool admin {
			get;
			set;
		}
		public bool firstTick {
			get;
			set;
		}

		public String chatID {
			get;
			set;
		}

		public int valor {
			get;
			set;
		}

		public float power {
			get { return wins / (float)losses; }
		}

		public int wins {
			get;
			set;
		}

		public int losses {
			get;
			set;
		}

		public int entryMode {
			get;
			set;
		}

		public TcpClient client {
			get;
			set;
		}

		public String name {
			get;
			set;
		}

		public TeamColors team {
			get;
			set;
		}
		private MemoryStream m_addOnStream;

		public MemoryStream addOnStream {
			get { return m_addOnStream; }
			set { m_addOnStream = value; }
		}
		private bool m_host;

		public bool host {
			get { return m_host; }
			set { m_host = value; }
		}

		public String tag {
			get;
			set;
		}


		/// <summary>
		/// Creates a new Player
		/// </summary>
		/// <param name="tag">The global unique identifier for this player.</param>
		/// <param name="name">The player's call sign</param>
		/// <param name="expired">Whether the account is expired or not</param>
		/// <param name="admin">The admin flag</param>
		public Player(String tag, String name, bool admin) {
			this.logOnTime = DateTime.Now;
			this.tag = tag;
			this.name = name;
			this.admin = admin;
			this.client = null;
			this.entryMode = 0;
			this.team = TeamColors.none;
			m_host = false;
			firstTick = true;
		}


		/// <summary>
		/// Creates a new Player using the specified TcpClient object.
		/// </summary>
		/// <param name="client">The TcpClient representing the connection</param>
		public Player(String tag, String name, bool admin, TcpClient client)
			: this(tag, name, admin) {
			this.client = client;
		}

		/// <summary>
		/// Updates the specified points value.
		/// </summary>
		/// <param name="p">The points field to update.</param>
		/// <param name="value">The value to update the points by. Passing a negative value will decrease the points, and passing a positive value will increase them.</param>
		/// <returns>The new points value</returns>
		public int updatePoints(Points p, int value) {
			if (p == Points.wins)
				return wins = wins + value;
			else if (p == Points.losses)
				return losses = losses + value;
			else
				return valor = valor + value;
		}

		/// <summary>
		/// Records a win and loss.
		/// </summary>
		/// <param name="loser">The player that lost</param>
		/// <returns>The amount of valor points the winner received</returns>
		public int recordWin(Player loser) {
			int amount = 10;
			updatePoints(Points.valor, amount);
			updatePoints(Points.wins, 1);
			loser.updatePoints(Points.losses, 1);
			return amount;
		}
	}

	public static class Server {
		public delegate void gameFinishedHandler(Game sender);
		private static List<Player> returns;
		private static bool testing = false;
		private static bool crash = false;
		private static bool rebooting = false;
		private static DateTime totalRebootTime;
		private static int elapsedRebootTime = -1;
		private static int nextDemoNumber;
		private static DateTime serverStartTime;
		private static DateTime currentDay;
		private static String dayMsg = null;
		private static int[] ports = null;
		private static bool modifiedClientList;
		private static StreamWriter theFile, theChatFile;
		private static Dictionary<String, Player> clientList;
		private static Thread checkThread;
		private static Object lockObject, fileLock, chatFileLocker, returnLock;
		private static Dictionary<String, Game> gameList;
		private static Dictionary<String, ChatRoom> chatRooms;
		private static TcpListener[] connections;


		public static void Main(String[] args) {
			try {
				ports = ((testing) ? new int[] { 31111 } : new int[] { 4444, 4445, 4567, 6969, 60385, 32000 });
				//ports = new int[]{4445});
				connections = new TcpListener[ports.Length];
				lockObject = new object();
				fileLock = new Object();
				chatFileLocker = new Object();
				returnLock = new Object();
				returns = new List<Player>();
				serverStartTime = DateTime.Now;
				currentDay = DateTime.Now;
				createLogs();
				output("Initializing...");
				clientList = new Dictionary<String, Player>();
				gameList = new Dictionary<String, Game>();
				chatRooms = new Dictionary<String, ChatRoom>();
				createFFA();
				createChatRoom("Foo Bar");
				createChatRoom("For Your Kids");
				createChatRoom("Admins", null, "adminsrule");
				checkThread = new Thread(startMonitoringForData);
				for (int i = 0; i < ports.Length; i++) {
					connections[i] = new TcpListener(IPAddress.Parse("0.0.0.0"),
					 ports[i]);
					connections[i].Start();
					output("Server listening on port " + ports[i]);
					connections[i].BeginAcceptTcpClient(new AsyncCallback(whenConnectionMade),
					 connections[i]);
				}
				checkThread.Start();
			} catch (Exception e) {
				output(e.Message + e.StackTrace);
			}
		} //startServer

		/// <summary>
		/// This method will fire as a callback to TCPListener.BeginAcceptTCPClient(),
		/// and will fire when someone connects.
		/// So we'll add their TCPClient to the connectedClients' list,
		/// and tell the caller we just accepted someone.
		/// </summary>
		/// <param name="result">The async callback object</param>
		private static void whenConnectionMade(IAsyncResult result) {
			TcpListener listener = (TcpListener)result.AsyncState;
			TcpClient c = listener.EndAcceptTcpClient(result);
			bool error = false;
			String callSign = null;
			try {
				output("Client connected!");
				//Next, client will send call sign to server.
				//Wait for it.
				output("Waiting for call sign.");
				try {
					using (BinaryReader signReader = new BinaryReader(CSCommon.getData(c, 10000, true))) {
						callSign = signReader.ReadString();
					} //using
				} catch (System.TimeoutException e) {
					output("Client never sent sign!" + e.GetBaseException() + " Closed connection.", true);
					error = true;
					return;
				} catch (System.Net.Sockets.SocketException e) {
					output(e.Message + " Closing connection.");
					return;
				} //try/catch
				output("Call sign is " + callSign + ".");
				//Custom logic to determine admin flag goes here
				bool admin = true;
				output("Read", true);

				output("Before chat", true);
				sendChatMessage(null, callSign + " has logged on.", MessageType.enterRoom, true);
				output("After chat", true);
				String serverTag = Guid.NewGuid().ToString();
				sendConnectResponse(c, LoginMessages.serverAssignedTag, serverTag);
				output("Lock returnlock", true);
				lock (returnLock) {
					output("success", true);
					Player p = null;
					returns.Add(p = new Player(serverTag, callSign, admin, c));
				} //lock
				output("After add.", true);
			} catch (Exception e) {
				output(e.Message + e.StackTrace);
			} finally {
				if (error) {
					c.Close();
					output("Closed due to error.");
				} else {
					output("ok");
				} //if !error
				clientConnected(listener);
			}
		}

		private static void clientConnected(TcpListener listener) {
			int port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.BeginAcceptTcpClient(new AsyncCallback(whenConnectionMade),
			   listener);
			output("Listening for connection on port " + port);
		}

		/// <summary>
		/// Sends the data to all clients in the lobby, excluding the one specified in exclude, using the specified MemoryStream.
		/// </summary>
		/// <param name="data">The MemoryStream containing data to send</param>
		/// <param name="exclude">Null if all clients should get this data</param>
		private static void propogate(MemoryStream stream, TcpClient exclude) {
			foreach (Player p in clientList.Values) {
				if (p.client != exclude && p.chatID == null)
					CSCommon.sendData(p.client, stream);
			}
		}


		/// <summary>
		/// Sends the data to all clients in the lobby, excluding the one specified in exclude, using the specified bye array.
		/// </summary>
		/// <param name="data">The byte array containing data to send</param>
		/// <param name="exclude">Null if all clients should get this data</param>
		private static void propogate(byte[] data, TcpClient exclude) {
			propogate(new MemoryStream(data), exclude);
		}

		/// <summary>
		/// this method will periodically tick and check if a client has sent data.
		/// It will run on its own thread, and is the main operation of the server.
		/// </summary>
		private static void startMonitoringForData() {
			bool loopedThrough = false;

			while (true) {
				if (DateTime.Now.Day != currentDay.Day) {
					currentDay = DateTime.Now;
					nextDay();
				}
				loopedThrough = false;
				//If a game kicks out a player, could cause deadlock
				if (returns.Count > 0) {
					output("Lock returns", true);
					lock (returnLock) {
						output("success", true);
						foreach (Player player in returns) {
							clientList.Add(player.tag, player);
						} //foreach
						returns.Clear();
					} //lock
					output("Exit returns", true);
				} //if modded list
				lock (lockObject) {
					while (!loopedThrough) {
						loopedThrough = true;
						modifiedClientList = false;
						try {
							if (prepareForReboot())
								crash = true;
							foreach (Player p in clientList.Values) {
								if (!CSCommon.isLiveConnection(p.client) || crash)
									removeFromServer(p.tag);
								else
									performCMDRCV(p.client, p.tag);

								//If player joined another game or disconnected themselves
								if (modifiedClientList)
									break;
							} //foreach
						} catch (Exception e) {
							output("ERROR: startMonitoringForData\n"
							   + e.Message + e.StackTrace);
							crash = true;
						}
					} //while ! loopedthrough
				} //lock
				if (!modifiedClientList)
					Thread.Sleep(100);
				if (crash && clientList.Count == 0 && returns.Count == 0) {
					cleanUp();
					return;
				}
			} //while true
		}

		/// <summary>
		/// Gets data from the TCPClient passed,
		/// and does a command based on the given data.
		/// This command could result in information being passed to other TCPClient objects,
		/// for instance if we've recieved information about an aircraft's state that needs to be propogated.
		/// </summary>
		/// <param name="client">The TcpClient representing a client's connection.</param>
		/// <param name="tag">The server tag of the client.</param>
		private static void performCMDRCV(TcpClient client, String tag) {
			if (!CSCommon.isLiveConnection(client))
				return;
			MemoryStream stream = CSCommon.getData(client);
			if (clientList[tag].firstTick && DateTime.Now.Subtract(clientList[tag].logOnTime).Seconds >= 10) {
				sendMessageOfTheDay(tag);
				clientList[tag].firstTick = false;
			}

			if (stream == null)
				return;
			BinaryReader rcvData = new BinaryReader(stream);
			//rcvData is the list of serverCommands.
			//so we'll crawl through each one.
			//We're given them as separate commands since getData()
			//already split them.
			sbyte c;
			while (rcvData.BaseStream.Length > rcvData.BaseStream.Position) {
				c = rcvData.ReadSByte();
				if (c == 1) {
					byte command = rcvData.ReadByte();
					switch (command) {
						case CSCommon.cmd_requestAdmin:
							CSCommon.sendResponse(client, clientList[tag].admin);
							break;

						case CSCommon.cmd_setMessage:
							String msgOfTheDay = rcvData.ReadString();
							if (!clientList[tag].admin)
								break;
							setMessage(msgOfTheDay);
							break;

						case CSCommon.cmd_reboot:
							if (!clientList[tag].admin)
								break;
							totalRebootTime = DateTime.Now;
							rebooting = true;
							break;

						case CSCommon.cmd_viewChatRooms:
							int numRooms = 0;
							using (BinaryWriter wChats = new BinaryWriter(new MemoryStream())) {
								wChats.Write((short)0);
								foreach (ChatRoom chatRoom in chatRooms.Values) {
									if (chatRoom.type != RoomTypes.closed) {
										wChats.Write(chatRoom.id);
										wChats.Write(chatRoom.friendlyName);
										wChats.Write(chatRoom.type == RoomTypes.password);
										numRooms++;
									}
								} //foreach
								wChats.BaseStream.Position = 0;
								wChats.Write((short)numRooms);
								CSCommon.sendResponse(client, wChats);
							}
							break;

						case CSCommon.cmd_joinChatRoom:
							String joinChatId = rcvData.ReadString();
							String joinPassword = (isPassworded(joinChatId)) ? rcvData.ReadString() : null;
							bool joinedChat = joinChatRoom(tag, joinChatId, joinPassword);
							using (BinaryWriter joinChatW = new BinaryWriter(new MemoryStream())) {
								joinChatW.Write(joinedChat);
								if (joinedChat) {
									ChatRoom roomToJoin = chatRooms[joinChatId];
									joinChatW.Write((short)(roomToJoin.getIds().Count - 1));
									foreach (String playerID in roomToJoin.getIds()) {
										if (playerID.Equals(tag))
											continue;
										joinChatW.Write(playerID);
										joinChatW.Write(clientList[playerID].name);
									} //foreach
								} //if joined chat room
								CSCommon.sendResponse(client, (MemoryStream)joinChatW.BaseStream);
							} //using
							break;

						case CSCommon.cmd_leaveChatRoom:
							leaveRoom(tag, true);
							break;

						case CSCommon.cmd_createChatRoom:
							int createArg = rcvData.ReadByte();
							if (createArg == 0)
								createChatRoom(rcvData.ReadString(), tag, null);
							else
								createChatRoom(rcvData.ReadString(), tag, rcvData.ReadString());
							break;

						case CSCommon.cmd_getStats:
							using (BinaryWriter statsWriter = new BinaryWriter(new MemoryStream())) {
								Player p = clientList[tag];
								statsWriter.Write(p.valor);
								statsWriter.Write(p.power);
								statsWriter.Write(p.wins);
								statsWriter.Write(p.losses);
								CSCommon.sendResponse(p.client, statsWriter);
							} //using
							break;

						case CSCommon.cmd_whois:
							using (BinaryWriter whoWriter = new BinaryWriter(new MemoryStream())) {
								whoWriter.Write((short)clientList.Count);
								foreach (Player p in clientList.Values) {
									whoWriter.Write(p.tag);
									whoWriter.Write(p.name + ((p.admin) ? " (GM)" : ""));
								}
								CSCommon.sendResponse(client, whoWriter);
							} //using
							break;

						case CSCommon.cmd_createGame:
							int gameListArgs = rcvData.ReadByte();
							String nc = clientList[tag].name;
							Game createdGame = null;
							Game.GameType gameType = (Game.GameType)rcvData.ReadInt32();
							if (gameListArgs == 1)
								clientList[tag].team = TeamColors.none;
							else
								clientList[tag].team = (TeamColors)rcvData.ReadInt32();
							createdGame = createNewGame(tag, gameType);
							sendMessage(nc + " Has created a new " + createdGame.getTitle() + ". The game is open to new players.", client);
							break;

						case CSCommon.cmd_requestGameList:
							using (BinaryWriter gLWriter = new BinaryWriter(new MemoryStream())) {
								int numberOfGames = 0;
								gLWriter.Write((byte)0);
								gLWriter.Write((byte)0);
								foreach (Game g in gameList.Values) {
									if (g.type == Game.GameType.freeForAll || !g.isOpen(tag, 0))
										continue;
									numberOfGames++;
									gLWriter.Write(g.id);
									gLWriter.Write(g.ToString());
									gLWriter.Write((int)g.type);
								}
								gLWriter.Flush();
								gLWriter.BaseStream.Position = 0;
								gLWriter.Write((short)numberOfGames);
								CSCommon.sendResponse(client, gLWriter);
							} //using
							break;

						case CSCommon.cmd_joinFreeForAll:
							clientList[tag].entryMode = rcvData.ReadInt32();
							sendMessage(clientList[tag].name + " has joined F F A", client);
							joinFFA(tag);
							output("success", true);
							break;

						case CSCommon.cmd_joinGame:
							int joinLen = rcvData.ReadByte();
							String n = clientList[tag].name;
							String joinId = rcvData.ReadString();
							clientList[tag].team = ((joinLen == 2) ? TeamColors.none : (TeamColors)rcvData.ReadInt32());
							clientList[tag].entryMode = rcvData.ReadInt32();
							String gameName = joinGame(tag, joinId); //will send success flag to player.
							if (gameName != null)
								sendMessage(n + " has joined a " + gameName, client);
							break;

						case CSCommon.cmd_serverMessage:
							propogate(CSCommon.buildCMDString(command, rcvData.ReadString()), client);
							break;

						case CSCommon.cmd_chat:
							bool isPrivate = rcvData.ReadBoolean();
							String chatMsg = null;
							if (isPrivate) {
								String recipient = rcvData.ReadString();
								chatMsg = clientList[tag].name + " (private): " + rcvData.ReadString();
								sendPrivateChatMessage(recipient, chatMsg);
							} else {
								chatMsg = rcvData.ReadString();
								sendChatMessage(tag, chatMsg, MessageType.normal, false);
							}
							break;

						case CSCommon.cmd_disconnectMe:
							closeClientConnection(tag);
							break;
					} //switch
				} //if explicit command
			} //foreach serverCommand
		}

		/// <summary>
		/// Puts this player back in the lobby after they have returned from a game.
		/// </summary>
		/// <param name="tag">The player's GUID</param>
		/// <param name="p">The player</param>
		/// <param name="keepOnServer">If true, the player will be put in the lobby, otherwise the player instance will be deleted and the connection closed</param>
		public static void returnFromGame(String tag, Player p, bool keepOnServer) {
			try {
				if (keepOnServer) {
					p.host = false;
					output("Lock returns in removve", true);
					lock (returnLock) {
						output("success", true);
						returns.Add(p);
					}
					output("unlock returns", true);
					modifiedClientList = true;
					if (!crash)
						sendMessage(p.name + " Has returned from a game.", p.client);
				}
			} catch (Exception e) {
				output("ERROR: removeFromGame:\n"
				+ e.Message + e.StackTrace);
				crash = true;
			}
		}

		/// <summary>
		/// Removes the specified player from the server.
		/// </summary>
		/// <param name="tag">The tag of the player to remove</param>
		private static void removeFromServer(String tag) {
			Player p = getPlayerByID(tag);
			if (p == null)
				return;
			String name = p.name;
			TcpClient c = p.client;
			if (c.Connected) {
				c.GetStream().Close();
				c.Close();
			}
			leaveRoom(tag, false);
			clientList.Remove(tag);
			sendChatMessage(null, name + " has left the server.", MessageType.leaveRoom, true);
			output(name + " disconnected");
			modifiedClientList = true;
		}

		/// <summary>
		/// Creates a new game.
		/// </summary>
		/// <param name="tag">The GUID of the player creating the game. If null, the method will create an FFA game.</param>
		/// <param name="type">The type of game to create</param>
		/// <returns>The created game instance</returns>
		private static Game createNewGame(String tag, Game.GameType type) {
			if (tag != null)
				output("creating game at request of " + tag);
			else
				output("Creating FFA.");
			String id = getID(gameList);
			Game g = new Game(id, type);
			g.gameFinished += gameFinishedEvent;
			gameList.Add(id, g);
			if (tag != null) {
				g.add(clientList[tag]);
				clientList.Remove(tag);
				modifiedClientList = true;
			}
			output("ok");
			return g;
		}

		/// <summary>
		/// Creates the FFA game.
		/// </summary>
		private static void createFFA() {
			createNewGame(null, Game.GameType.freeForAll);
		}

		/// <summary>
		/// An event to notify the server that a game has finished and should be flushed.
		/// </summary>
		/// <param name="sender">The game instance to flush</param>
		private static void gameFinishedEvent(Game sender) {
			gameList.Remove(sender.id);
			output("Game " + sender.id + " ended.");
			sender.gameFinished -= gameFinishedEvent;
		}

		/// <summary>
		/// Adds the client to the specified game.
		/// </summary>
		/// <param name="tag">The GUID of the client to add</param>
		/// <param name="id">The ID of the game to add to.</param>
		/// <returns>If the player could be added, returns the name of the game. else NULL.</returns>
		private static String joinGame(String tag, String id) {
			output("Joining game " + id + " using client " + tag);
			if (!gameList.ContainsKey(id)) {
				output("ERROR: id " + id + " doesn't exist.");
				return null;
			}
			String name = gameList[id].ToString();
			if (!gameList[id].isOpen(tag, clientList[tag].entryMode))
				return null;
			CSCommon.sendData(clientList[tag].client, (byte)1);
			gameList[id].add(clientList[tag]);
			clientList.Remove(tag);
			modifiedClientList = true;
			output("ok");
			return name;
		}

		/// <summary>
		/// Allows a pleyr to join the FFA game.
		/// </summary>
		/// <param name="tag">The GUID of the player to add to FFA</param>
		/// <returns>True on success, false on failure</returns>
		private static bool joinFFA(String tag) {
			foreach (Game g in gameList.Values) {
				if (g.type == Game.GameType.freeForAll) {
					g.add(clientList[tag]);
					clientList.Remove(tag);
					modifiedClientList = true;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Closes the specified client and removes the player from the server.
		/// </summary>
		/// <param name="tag">The GUID of the player to remove</param>
		private static void closeClientConnection(String tag) {
			Player p = getPlayerByID(tag);
			if (p == null)
				return;
			TcpClient client = p.client;
			client.GetStream().Close();
			client.Close();
			removeFromServer(tag);
		}

		public static void output(String text) {
			System.Console.WriteLine(text + "");
			lock (fileLock) {
				theFile.Write(text + Environment.NewLine);
				theFile.Flush();
			}
		}

		public static void outputChat(String text) {
			lock (chatFileLocker) {
				theChatFile.WriteLine(DateTime.Now.ToString("MMMM/d/yyyy"));
				theChatFile.WriteLine(text);
				theChatFile.WriteLine();
				theChatFile.Flush();
			}
		}

		[Conditional("DEBUG")]
		public static void output(String text, bool b) {
			System.Console.WriteLine(text + "");
			lock (fileLock) {
				theFile.Write(text + Environment.NewLine);
				theFile.Flush();
			}
		}


		/// <summary>
		/// Sends the message to all clients except the one specified.
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <param name="exclude">The TcpClient to exclude in the message sending.</param>
		private static void sendMessage(String message, TcpClient exclude) {
			output("Sending message: " + message);
			MemoryStream sendStream = CSCommon.buildCMDString(CSCommon.cmd_serverMessage, message);
			propogate(sendStream, exclude);
			output("Success", true);
		}

		/// <summary>
		/// Sends the message to all clients.
		/// </summary>
		/// <param name="message">The message to send.</param>
		private static void sendMessage(String message) {
			sendMessage(message, null);
		}

		/// <summary>
		/// Gets a random character String.
		/// </summary>
		/// <param name="d">The collection to check against to make sure this ID doesn't exist</param>
		/// <returns>An unused ID</returns>
		private static String getID(IDictionary d) {
			Random r = new Random();
			bool validID = false;
			String theID = null;
			while (!validID) {
				char[] chars = new char[r.Next(1, 10)];
				int nextIndex = 0;
				do {
					//If set, we will select a number 0-9,
					//else a letter
					bool selectNumber =
						r.Next(1, 3) == 2;
					if (selectNumber)
						chars[nextIndex] = (char)r.Next('0', '9' + 1);
					else
						chars[nextIndex] = (char)r.Next('A', 'Z' + 1);
					nextIndex++;
				} while (nextIndex < chars.Length);
				theID = new String(chars);
				validID = !d.Contains(theID);
			} //while
			return theID;
		}

		/// <summary>
		/// Gets the Power Ratio for the given Client ID.
		/// </summary>
		/// <param name="tag">The GUID of the player to get the power ratio for</param>
		/// <returns>The power ratio.</returns>
		public static double getPowerRatio(String tag) {
			return clientList[tag].power;
		}

		/// <summary>
		/// Sends a private message.
		/// </summary>
		/// <param name="target">The GUID of the player who is to receive the message</param>
		/// <param name="message">The message to send</param>
		private static void sendPrivateChatMessage(String target, String message) {
			Player p = getPlayerByID(target);
			if (p != null)
				CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_chat, (byte)MessageType.privateMessage, message));
		}

		/// <summary>
		/// Sends a chat message to everyone in the room to which the player belongs.
		/// </summary>
		/// <param name="sender">The tag of the sender. If this message is from the server, this parameter should contain the id of the chat room to which to send the message</param>
		/// <param name="message">The message to send</param>
		/// <param name="type">The message type</param>
		/// <param name="fromServer">True if the message is generated by the server (with no name prefix), and false otherwise</param>
		private static void sendChatMessage(String sender, String message, MessageType type, bool fromServer) {
			ChatRoom room = null;
			String chatId = null;
			Player p = null;
			if (fromServer)
				chatId = sender;
			else {
				p = getPlayerByID(sender);
				chatId = p.chatID;
			}
			if (!fromServer)
				message = p.name + ": " + message;

			if (chatId == null)
				propogate(CSCommon.buildCMDString(CSCommon.cmd_chat, (byte)type, message), (p == null) ? null : p.client);
			else {
				if (!chatRooms.TryGetValue(chatId, out room)) {
					if (p != null)
						CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_leaveChatRoom));
					return;
				} //if the chat room no longer exists.
				foreach (String id in room.getIds()) {
					if (!fromServer && sender.Equals(id))
						continue; //so sender doesn't get their own message
					CSCommon.sendData(clientList[id].client, CSCommon.buildCMDString(CSCommon.cmd_chat, (byte)type, message));
				}
			}
			String writeMsg = "";
			if (p != null)
				writeMsg = p.name + Environment.NewLine + p.tag;
			if (!String.IsNullOrEmpty(writeMsg))
				writeMsg += Environment.NewLine;
			if (room == null)
				writeMsg += "Lobby";
			else
				writeMsg += room.friendlyName;
			writeMsg += Environment.NewLine + message;
			outputChat(writeMsg);
		}

		/// <summary>
		/// Gets the player belonging to the specified server tag. This method uses TryGetValue as a fail-safe.
		/// </summary>
		/// <param name="tag">The server tag.</param>
		/// <returns>The player object associated with the tag, or null if not found.</returns>
		private static Player getPlayerByID(String tag) {
			Player p = null;
			clientList.TryGetValue(tag, out p);
			return p;
		}

		private static void createChatRoom(String friendlyName, String tag1, String tag2, String password) {
			String id = getID(chatRooms);
			chatRooms.Add(id, new ChatRoom(id, friendlyName, tag1, tag2, password));
			if (tag2 != null)
				clientList[tag2].chatID = id;
			if (tag1 != null) {
				clientList[tag1].chatID = id;
				sendChatMessage(id, "Room created", MessageType.enterRoom, true);
			}
		}

		/// <summary>
		/// Creates a new chat room with no participants. This room will always be open.
		/// </summary>
		/// <param name="friendlyName">The display name</param>
		private static void createChatRoom(String friendlyName) {
			createChatRoom(friendlyName, null, null, null);
		}

		/// <summary>
		/// </summary>
		/// <param name="tag1">The person who created the room</param>
		/// <param name="tag2">The other participant in a two-way closed chat</param>
		private static void createChatRoom(String tag1, String tag2) {
			createChatRoom(null, tag1, tag2, null);
		}

		///<summary>
		///Creates a chat room with the friendly name, host (null for no host), and password (null for no password).
		///</summary>
		/// <param name="password">The password, or null for open</param>
		private static void createChatRoom(String friendlyName, String tag, String password) {
			createChatRoom(friendlyName, tag, null, password);
		}

		/// <summary>
		/// Joins a chat room.
		/// </summary>
		/// <param name="tag">The GUID of the player to join</param>
		/// <param name="id">The id of the room to join</param>
		/// <param name="password">Any password, null for no password. If the chatroom requires no password this parameter will be ignored</param>
		/// <returns>True if the join was successful, false otherwise</returns>
		private static bool joinChatRoom(String tag, String id, String password) {
			ChatRoom room = null;
			if (!chatRooms.TryGetValue(id, out room))
				return false;
			if (room.password != null && !String.Equals(password, room.password))
				return false;
			sendChatMessage(id, clientList[tag].name + " has joined the room!", MessageType.enterRoom, true);
			sendToRoom(id, CSCommon.buildCMDString(CSCommon.cmd_addMember, tag, clientList[tag].name));
			room.add(tag);
			clientList[tag].chatID = id;
			return true;
		}

		/// <summary>
		/// Causes player to exit room.
		/// </summary>
		/// <param name="tag">The tag of the player wanting to leave</param>
		/// <param name="playerAlive">True if the player is still on the server, false otherwise</param>
		private static void leaveRoom(String tag, bool playerAlive) {
			ChatRoom room = null;
			Player p = getPlayerByID(tag);
			if (p == null)
				return;
			if (p.chatID == null)
				return; //protect against potential multiple presses of "leave" button
			if (!chatRooms.TryGetValue(p.chatID, out room))
				return;
			String id = room.id;
			if (room.remove(tag)) //return true if this is the last member to be removed.
				chatRooms.Remove(room.id);
			if (playerAlive)
				CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_leaveChatRoom));
			p.chatID = null;
			sendChatMessage(id, p.name + " has left the room!", MessageType.leaveRoom, true);
			sendToRoom(id, CSCommon.buildCMDString(CSCommon.cmd_removeMember, tag));
		}

		/// <summary>
		/// Sends the data to the specified room.
		/// </summary>
		/// <param name="id">The id of the room</param>
		/// <param name="data">The MemoryStream containing the data</param>
		private static void sendToRoom(String id, MemoryStream data) {
			ChatRoom c;
			Player p;
			if (chatRooms.TryGetValue(id, out c)) {
				foreach (String s in c.getIds()) {
					if ((p = getPlayerByID(s)) != null)
						CSCommon.sendData(p.client, data);
				}
			}
			data.Close();
		}

		/// <summary>
		/// Determines if the given chat room is password protected.
		/// </summary>
		/// <param name="id">The id to check</param>
		/// <returns>True if protected, false otherwise</returns>
		private static bool isPassworded(String id) {
			ChatRoom room = null;
			if (!chatRooms.TryGetValue(id, out room))
				return false;
			return room.type == RoomTypes.password;
		}

		private static void cleanUp() {
			try {
				output("Cleaning up...");
				foreach (Game g in gameList.Values)
					g.setForceGameEnd((rebooting) ? null : "there was a problem with the server.");
				output("Sent forceGameEnd");
				while (gameList.Count != 0)
					Thread.Sleep(0);
				output("All games ended.");
				for (int i = 0; i < connections.Length; i++)
					connections[i].Stop();
				output("Connections closed.");

				output("Cleaned up, ending server process.");
				if (theFile != null) {
					theFile.Flush();
					theFile.Close();
				}
				if (theChatFile != null) {
					theChatFile.Flush();
					theChatFile.Close();
				}
			} catch (Exception e) {
				output(e.Message + e.StackTrace);
			}
		}

		private static bool prepareForReboot() {
			if (rebooting) {
				if (elapsedRebootTime == -1) {
					elapsedRebootTime = 0;
					return false;
				}
				DateTime d = DateTime.Now;
				int current = d.Subtract(totalRebootTime).Minutes;
				if (current < 5 && current != elapsedRebootTime) {
					sendCriticalMessage("Server rebooting in " + (5 - current) + " minutes");
					elapsedRebootTime = current;
				}
				return current == 5;
			}
			return false;
		}

		/// <summary>
		/// Sends the critical server message to everyone.
		/// </summary>
		/// <param name="message">The message to send</param>
		private static void sendCriticalMessage(String message) {
			sendChatMessage(null, message, MessageType.critical, true);
			foreach (ChatRoom room in chatRooms.Values)
				sendChatMessage(room.id, message, MessageType.critical, true);
			foreach (Game g in gameList.Values)
				g.queueCriticalMessage(message);
		}

		public static void createLogs() {
			if (theFile != null) {
				lock (fileLock) {
					theFile.Flush();
					theFile.Close();
					theFile = new StreamWriter(String.Format("log{0}.log", currentDay.ToString("MMMM-d-yyyy")), true);
				}
				lock (chatFileLocker) {
					theChatFile.Flush();
					theChatFile.Close();
					theChatFile = new StreamWriter(String.Format("chat{0}.log", currentDay.ToString("MMMM-d-yyyy")));
				}
			} else { //if file handles are null, meaning no files have been created yet
				theFile = new StreamWriter(String.Format("log{0}.log", currentDay.ToString("MMMM-d-yyyy")), true);
				theChatFile = new StreamWriter(String.Format("chat{0}.log", currentDay.ToString("MMMM-d-yyyy")));
			}
			outputChat("New chat log created.");
			output("New log created on " + DateTime.Now.ToString("MMMM/d/yyyy"));
		}

		private static void nextDay() {
			createLogs();
			clearDayMsg();
		}

		private static void setMessage(String msg) {
			dayMsg = msg;
		}

		private static void sendMessageOfTheDay(String tag) {
			if (dayMsg != null)
				CSCommon.sendData(clientList[tag].client, CSCommon.buildCMDString(CSCommon.cmd_chat, (byte)MessageType.normal, "[Message of the day]: " + dayMsg));
		}

		private static void clearDayMsg() {
			dayMsg = null;
		}

		private static void sendConnectResponse(TcpClient c, LoginMessages l, String serverTag) {
			if ((l & LoginMessages.unauthorized) != LoginMessages.unauthorized) {
				using (BinaryWriter writer = new BinaryWriter(new MemoryStream())) {
					c.NoDelay = true;
					writer.Write((int)l);
					if ((l & LoginMessages.serverAssignedTag) == LoginMessages.serverAssignedTag)
						writer.Write(serverTag);
					output("sending data", true);
					CSCommon.sendData(c, writer);
					if ((l & LoginMessages.wrongCredentials) == LoginMessages.wrongCredentials
						|| (l & LoginMessages.badVersion) == LoginMessages.badVersion)
						c.Close();
					c.NoDelay = false;
				} //using
			} else //Invalid connection
				c.Close();
		}


	} 	 //class
} //namespace
