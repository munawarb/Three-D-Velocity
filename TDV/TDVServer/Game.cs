/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
#define SERVER
using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TDVServer
{
	public class Game
	{
		public enum GameType
		{
			freeForAll = 8,
			oneOnOne,
			teamDeath
		}
		private enum DisconnectMethod
		{
			gameEnded,
			midGame
		}
		public enum ObjectType : byte
		{
			aircraft,
			carrierBlue,
			carrierGreen,
			carrierRed,
			carrierYellow
		}

		private Object returnsLock;
		private List<Player> returns;
		private List<Player> unlockedReturns;
		public event Server.gameFinishedHandler gameFinished;
		private bool modifiedClientList;
		private bool requestPause;
		private bool paused;
		private bool canEvaluateGameEnd; //if set, conditions to end the game will be evaluated.
										 //This flag is set either if this is an FFA game, or when the first player is added to a non-FFA game, since end-of-game
										 //is most often determined by if there are zero players remaining in the game.
		private bool forceGameEnd; //if true, this game has been forced to end.
								   //This may happen if a player in Team Death disconnects, giving the others an unfair advantage (used to thwart cheating!)
		private String reason; //If forceGameEnd is set, contains the reason why (optional)
		private string serverMessage; //used to send critical server messages
		private long next;
		private Dictionary<String, Player> clientList;
		private List<BotInfo> bots;
		private int bColor, rColor, gColor, yColor;
		private bool gameStarted;
		private Object lockObject;
		private String m_id;
		private Thread ticker;
		private GameType m_type;


		public GameType type
		{
			get { return m_type; }
			set { m_type = value; }
		}


		/// <summary>
		/// Unique identifier for this game. This is what clients will use to specify what game to connect to
		/// </summary>
		public string id
		{
			get { return m_id; }
			set { m_id = value; }
		}

		public Game(String id, GameType type)
		{
			returnsLock = new Object();
			lockObject = new object();
			returns = new List<Player>();
			unlockedReturns = new List<Player>();
			serverMessage = "";
			next = 1;
			this.type = type;
			this.id = id;
			clientList = new Dictionary<String, Player>();
			bots = new List<BotInfo>();
			bColor = -1; gColor = -1; rColor = -1; yColor = -1;
			if (type == GameType.freeForAll)
				canEvaluateGameEnd = true;
			ticker = new Thread(startMonitoringForData);
			ticker.Start();
		}

		public bool add(Player p)
		{
			lock (returnsLock) {
				returns.Add(p);
			}
			return true;
		}

		private void enterPendingPlayers()
		{
			lock (returnsLock) {
				foreach (Player player in returns)
					unlockedReturns.Add(player);
				returns.Clear();
			}

			foreach (Player player in unlockedReturns)
				add(player.tag, player);
			unlockedReturns.Clear();
		}

		/// <summary>
		/// This method will add a new player to this game.
		/// </summary>
		/// <param name="tag">The server tag of the player being added</param>
		/// <param name="p">The Player object representing the connection.</param>
		private bool add(String tag, Player p)
		{
			try {
				//pauseProcessing();
				output(LoggingLevels.debug, "Adding player " + tag + "...");
				lock (lockObject) {
					if (clientList.Count == 0 && type != GameType.freeForAll) //This is first player being added, so this is the host.
						p.host = true;
					//Create local player.
					if (p.entryMode != 1) {
						int maxWeight = 0;
						CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_position, next++, (short)0));
						CSCommon.sendData(p.client,
						CSCommon.buildCMDString(CSCommon.cmd_requestCreate, p.name, maxWeight));

						//Give this player's info to all players.
						output(LoggingLevels.debug, "Propogating connection...");
						if (type != GameType.teamDeath) {
							propogate(CSCommon.buildCMDString(CSCommon.cmd_distributeServerTag, tag, p.name, (byte)ObjectType.aircraft, (short)0),
							 p.client);
						} else { //if team death
							propogate(CSCommon.buildCMDString(CSCommon.cmd_distributeServerTag, tag, p.name, (byte)ObjectType.aircraft, (int)p.team, (short)0),
					  p.client);
						}
					} //if entryMode != 1

					//Give this player info about all other players.
					output(LoggingLevels.debug, "Sending info about other clients to this client...");
					foreach (Player player in clientList.Values) {
						if (player.entryMode == 1)  //spectator
							continue;
						if (type != GameType.teamDeath) {
							CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_distributeServerTag, player.tag, player.name, (byte)ObjectType.aircraft, (short)0));
						} else { //if team death
							CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_distributeServerTag, player.tag, player.name, (byte)ObjectType.aircraft,
								(int)player.team, (short)0));
						}
					} //foreach connected player

					foreach (BotInfo info in bots) {
						if (info.creator == null) {
							info.creator = tag;
							CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_createBot, info.id, info.name, (byte)info.objectType));
							CSCommon.sendData(p.client, new MemoryStream(info.data));
							output(LoggingLevels.debug, "Created bot " + info.id);
						} else
							CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_distributeServerTag, info.id, info.name, (byte)info.objectType,
								(short)0));
					}
					if (type == GameType.freeForAll) {
						output(LoggingLevels.debug, "FFA, Starting local player...");
						CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_startGame));
					} //if FFA
					clientList[tag] = p;
					if (type == GameType.teamDeath)
						incrementTeam(tag, p.team);

					String entMessage = ".";
					if (type == GameType.teamDeath)
						entMessage = " for the " + p.team + " team";
					sendMessage(clientList[tag].name +
						String.Format(" has joined this game{0}",
						(p.entryMode == 1) ? " as a spectator" : entMessage),
					   p.client);
				} //lock
				if (type != GameType.freeForAll && !canEvaluateGameEnd
					&& (type == GameType.teamDeath && getNumberOfTeams() >= 2 || type != GameType.teamDeath))
					canEvaluateGameEnd = true;
				//resumeProcessing();
				return true;
			}
			catch (Exception e) {
				output(LoggingLevels.error, e.Message + Environment.NewLine + e.StackTrace);
			}
			return false;
		}

		[Conditional("DEBUG")]
		private void output(LoggingLevels l, String message)
		{
			Server.output(l, "Game " + id + ":" + message);
		}

		/// <summary>
		/// This method will periodically tick and check if a client has sent data.
		/// It will run on its own thread and is the main operation of the server.
		/// </summary>
		private void startMonitoringForData()
		{
			const int waitTime = 10;
			while (true) {
				try {
					if (returns.Count > 0)
						enterPendingPlayers();
					//pause();
					lock (lockObject) {
						modifiedClientList = false;
						foreach (String s in clientList.Keys) {
							if (!CSCommon.isLiveConnection(clientList[s].client)) {
								String name = clientList[s].name;
								removeFromGame(s, false, DisconnectMethod.midGame);
								sendMessage(name + " has been unexpectedly disconnected.", null);
							} else
								performCMDRCV(s, clientList[s].client);
							//In case player wanted to be disconnected
							if (modifiedClientList)
								break;
						}  //for
					} //lock
					if (!modifiedClientList)
						Thread.Sleep(waitTime);
					if (isGameEnd()) {
						output(LoggingLevels.debug, "Doing game ended routine");
						if (!forceGameEnd) //don't give points if team death player disconnected
							allocatePoints();
						else {
							if (reason == null)
								sendMessage("The game has ended.", null);
							else
								sendMessage("The game has ended because " + reason, null);
							propogate(CSCommon.buildCMDString(CSCommon.cmd_gameEnded), null);
							while (clientList.Count != 0) {
								foreach (String s in clientList.Keys) {
									removeFromGame(s, true, DisconnectMethod.midGame); //remove clients one by one
									break; //release enumerator
								} //foreach
							} //while
						} //if forced to end the game by server-side event.

						if (gameFinished != null)
							gameFinished(this);
						return;
					} //if game ended
					sendCriticalMessage();
				}
				catch (Exception e) {
					output(LoggingLevels.error, e.Message + e.StackTrace);
					setForceGameEnd("there was a problem with the game.");
				}
			} //while
		}

		/// <summary>
		/// gets data from the TCPClient passed and does a command based on the given data. This command could result in information being passed to other TCPClient objects, for instance if we've recieved information about an aircraft's state that needs to be propogated.
		/// </summary>
		/// <param name="tag">The GUID of the player to perform commands on</param>
		/// <param name="client">The player's TcpClient object</param>
		private void performCMDRCV(String tag, TcpClient client)
		{
			if (!CSCommon.isLiveConnection(client))
				return;
			MemoryStream stream = CSCommon.getData(client);
			if (stream == null)
				return;

			BinaryReader rcvData = new BinaryReader(stream);
			//rcvData is the list of serverCommands.
			sbyte c = 0;
			long start = 0L;
			byte command = 0;
			try {
				while (rcvData.BaseStream.Length > rcvData.BaseStream.Position) {
					start = rcvData.BaseStream.Position;
					c = rcvData.ReadSByte();
					if (c > 4)
						return;
					if (c == 1) {
						command = rcvData.ReadByte();

						switch (command) {
							case CSCommon.cmd_test:
								int testAmount = recordWin("6SRKJ695G", "ABCDEFGHI");
								CSCommon.sendData(client, CSCommon.buildCMDString(CSCommon.cmd_newval, testAmount));
								break;

							case CSCommon.cmd_createBot:
								//The player who creates the bot will spawn a thread to control them.
								createBot(tag, ObjectType.aircraft);
								break;

							case CSCommon.cmd_removeBot:
								String rBotId = removeBot(0);
								if (rBotId == null)
									break;
								sendMessage(rBotId + " has been dropped from the server", null);
								propogate(CSCommon.buildCMDString(CSCommon.cmd_forceDisconnect, rBotId), null);
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

							case CSCommon.cmd_requestStartGame:
								if (type == GameType.oneOnOne && (clientList.Count + bots.Count) > 1)
									gameStarted = true;
								if (type == GameType.teamDeath) {
									if (getNumberOfTeams() >= 2)
										gameStarted = true;
								} //if team death
								CSCommon.sendResponse(client, gameStarted);
								if (gameStarted) {
									propogate(CSCommon.buildCMDString(CSCommon.cmd_startGame), client);
								}
								break;

							case CSCommon.cmd_updatePoints:
								Player winner = getPlayerByID(rcvData.ReadString());
								if (winner == null) //winner logged off; kill doesn't count!
									break;
								//The client sending this command is the loser.
								int amount = recordWin(winner.tag, tag);
								CSCommon.sendData(winner.client, CSCommon.buildCMDString(CSCommon.cmd_newval, amount));
								break;

							case CSCommon.cmd_startGame:
								//Send cmd_startGame to all clients so they can
								//signal start locally.
								propogate(CSCommon.buildCMDString(CSCommon.cmd_startGame), client);
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
									sendChatMessage(tag, chatMsg, MessageType.normal);
								}
								break;

							case CSCommon.cmd_serverMessage:
								sendMessage(rcvData.ReadString(), null);
								break;

							case CSCommon.cmd_disconnectMe:
								String name = clientList[tag].name;
								removeFromGame(tag, true, DisconnectMethod.midGame);
								sendMessage(name + " has left the game.", null);
								return; //no need to process things further.

							case CSCommon.cmd_deleteFromGame: //silent exit
								removeFromGame(tag, true, DisconnectMethod.gameEnded); //delete will only be sent on successful game end
								return; //no need to process data further.
						} //switch
					} //if explicit command
					else {
						byte[] buffer = new byte[rcvData.ReadInt32()];
						string id = rcvData.ReadString();
						bool updateBot = false;
						int botIndex = 0;
						if (c == 3) {
							if ((botIndex = getBot(id)) > -1) {
								rcvData.ReadInt16(); //passed numArgs
								while (rcvData.ReadSByte() != 1) ;
								if (rcvData.ReadInt32() > 0)
									updateBot = true;
								else
									removeBot(botIndex);
							} //if bot exists
						} //if this is a bot update
						rcvData.BaseStream.Position = start;
						rcvData.BaseStream.Read(buffer, 0, buffer.Length);
						if (updateBot)
							bots[botIndex].data = buffer;
						propogate(buffer, client);
					} //if something else besides cmd_command.
				} //foreach serverCommand
			}
			catch (Exception e) {
				output(LoggingLevels.error, "Error while reading data from " + tag + ". Char = " + c + Environment.NewLine + "Last command: " + command + Environment.NewLine + "Stack trace: " + e.Message + e.StackTrace);
			} //catch
		}

		/// <summary>
		/// Sends the specified string to all clients, excluding the one specified in exclude, using the specified MemoryStream.
		/// </summary>
		/// <param name="data">The MemoryStream containing data to send</param>
		/// <param name="exclude">Null if all clients should get this data</param>
		private void propogate(MemoryStream stream, TcpClient exclude)
		{
			foreach (Player p in clientList.Values) {
				if (p.client != exclude)
					CSCommon.sendData(p.client, stream);
			}
		}

		/// <summary>
		/// Sends the specified string to all clients, excluding the one specified in exclude, using the specified bye array.
		/// </summary>
		/// <param name="data">The byte array containing data to send</param>
		/// <param name="exclude">Null if all clients should get this data</param>
		private void propogate(byte[] data, TcpClient exclude)
		{
			propogate(new MemoryStream(data), exclude);
		}

		/// <summary>
		///    Sends a server message to all clients. To include everyone, send NULL to the exclude parameter.
		/// </summary>
		/// <param name="message">The message to send.</param>
		/// <param name="exclude">The client to exclude.</param>
		private void sendMessage(String message, TcpClient exclude)
		{
			propogate(CSCommon.buildCMDString(CSCommon.cmd_serverMessage, message),
			 exclude);
			output(LoggingLevels.chat, message);
		}

		/// <summary>
		/// Removes a player from this game. if disconnectFromServer == true, we'll assume player
		/// either did hard shutdown, or was dropped forcibly by the game because their connection timed out.
		/// In this case, we send cmd_forceDisconnect command to each client so they can get rid of that player's object.  Otherwise, we'll just put them back in the hangar.
		/// </summary>
		/// <param name="tag">The tag of the player to remove.</param>
		/// <param name="keepOnServer">If true, this player will not be dropped from the server.</param>
		/// <param name="d">The method by which this player is being disconnected. If the player was disconnected because they lost,
		/// this flag should be set to gameEnd. Otherwise, if the player hit escape or was dropped, this flag should be set to midGame.</param>
		private void removeFromGame(String tag, bool keepOnServer, DisconnectMethod d)
		{
			if (clientList[tag].host) //If this is the game host,
				forceGameEnd = true; //If they drop or disconnect, end the game.
			Server.returnFromGame(tag, clientList[tag], keepOnServer);
			clientList.Remove(tag);
			//In case this is a case where the client was disconnected without an in-game event,
			//IE: a connection drop, tell all clients that this object has disconnected so they can
			//clean it up.
			propogate(CSCommon.buildCMDString(CSCommon.cmd_forceDisconnect, tag), null);
			if (clientList.Count == 0)
				canEvaluateGameEnd = true;
			modifiedClientList = true;
			if (type == GameType.freeForAll)
				clearBotsFromPlayer(tag);
			//now game is unbalanced, so call it quit since players may cheat this way,
			//by starting a team death, disconnecting, and the other team gets all the points.
			//Or if there's a death match and only one player remains after this disconnect, just end the game since one player in a deth match is pointless!
			if (type == GameType.teamDeath && d == DisconnectMethod.midGame || type == GameType.oneOnOne && clientList.Count == 1)
				forceGameEnd = true;
		}

		/// <summary>
		/// Returns the user-friendly description for this game.
		/// </summary>
		/// <returns>The type of the game as well as the players.</returns>
		public override string ToString()
		{
			String r = "";
			if (type == GameType.oneOnOne)
				r += "One-on-one death match with ";
			else
				r += "Team death with ";
			if (clientList.Count == 0)
				r += "0 participants";
			else {
				foreach (Player s in clientList.Values)
					r += s.name + ", ";
			}
			return r;
		}

		private bool isGameEnd()
		{
			if (forceGameEnd)
				return true;
			if (!canEvaluateGameEnd)
				return false;
			if (type == GameType.freeForAll)
				return false;
			if (clientList.Count == 0)
				return true;
			if (type == GameType.teamDeath)
				return bColor > 0 ^ gColor > 0 ^ rColor > 0 ^ yColor > 0;
			return false;
		}

		/// <summary>
		///  Allocates points to winning players after a match.
		/// </summary>
		private void allocatePoints()
		{
			if (type == GameType.teamDeath) {
				//Find out which team value has > 0 players, this is winner
				int t = Math.Max(bColor, Math.Max(gColor, Math.Max(rColor, yColor)));
				TeamColors winningTeam;
				if (t == bColor)
					winningTeam = TeamColors.blue;
				else if (t == gColor)
					winningTeam = TeamColors.green;
				else if (t == rColor)
					winningTeam = TeamColors.red;
				else
					winningTeam = TeamColors.yellow;
				foreach (KeyValuePair<String, Player> ent in clientList) {
					if (ent.Value.team == winningTeam)
						ent.Value.updatePoints(Points.valor, 10);
				} //foreach
				sendMessage(winningTeam.ToString() + " has won this match!", null);
			}//team death
		}

		/// <summary>
		/// Increments the available participatns on a given team, and also spawns the team's aircraft carrier.
		/// </summary>
		/// <param name="tag">The tag of the player just added.</param>
		/// <param name="c">The color of the team.</param>
		private void incrementTeam(String tag, TeamColors c)
		{
			if (c == TeamColors.blue && ++bColor == 0) {
				bColor++;
				createBot(tag, ObjectType.carrierBlue);
			} else if (c == TeamColors.green && ++gColor == 0) {
				gColor++;
				createBot(tag, ObjectType.carrierGreen);
			} else if (c == TeamColors.red && ++rColor == 0) {
				rColor++;
				createBot(tag, ObjectType.carrierRed);
			} else if (c == TeamColors.yellow && ++yColor == 0) {
				yColor++;
				createBot(tag, ObjectType.carrierYellow);
			}
		}

		private void decrementTeam(TeamColors c)
		{
			if (c == TeamColors.blue)
				bColor--;
			else if (c == TeamColors.green)
				gColor--;
			else if (c == TeamColors.red)
				rColor--;
			else if (c == TeamColors.yellow)
				yColor--;
		}

		/// <summary>
		/// Checks to see if players are allowed to connect or not
		/// </summary>
		/// <param name="tag">The tag of the player attempting to connect.</param>
		/// <param name="entryMode">An entry mode value.</param>
		/// <returns>True on success, false on failure.</returns>
		public bool isOpen(String tag, int entryMode)
		{
			if (entryMode == 1)
				return true;
			if (gameStarted)
				return false;
			if (type == GameType.oneOnOne && clientList.Count == 2)
				return false;
			return true;
		}

		private int getNumberOfTeams()
		{
			int numTeams = 0;
			if (bColor > 0)
				numTeams++;
			if (gColor > 0)
				numTeams++;
			if (rColor > 0)
				numTeams++;
			if (yColor > 0)
				numTeams++;
			return numTeams;
		}

		/// <summary>
		/// Requests the game to stop processing incoming data.
		/// </summary>
		private void pauseProcessing()
		{
			requestPause = true;
			while (!paused)
				Thread.Sleep(0);
		}

		/// <summary>
		/// Resumes processing of incoming data.
		/// </summary>
		private void resumeProcessing()
		{
			requestPause = false;
		}

		/// <summary>
		/// Interrupts processing of incoming data.
		/// </summary>
		private void pause()
		{
			if (requestPause) {
				paused = true;
				while (requestPause)
					Thread.Sleep(50);
				paused = false;
			}
		}

		/// <summary>
		/// Gets the user-friendly title of this game without player count and listing.
		/// </summary>
		/// <returns>The title of this game such as Team Death.</returns>
		public String getTitle()
		{
			if (type == GameType.oneOnOne)
				return "one-on-one death match";
			else if (type == GameType.teamDeath)
				return "team death";
			return null;
		}

		/// <summary>
		/// Records a win, and updates points as appropriate.
		/// </summary>
		/// <param name="winner">The ID of the winning player.</param>
		/// <param name="loser">The id of the losing player.</param>
		/// <returns>The amount by which the winner's valor points were updated.</returns>
		private int recordWin(String winner, String loser)
		{
			int amount = clientList[winner].recordWin(clientList[loser]);
			output(LoggingLevels.info, String.Format("{0} got {1} points", winner, (int)amount));
			return amount;
		}

		/// <summary>
		/// Gets an id for a bot.
		/// </summary>
		/// <returns>The id to assign to a bot.</returns>
		private String getBotID()
		{
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
				validID = !botIDExists(theID);
			} //while
			return theID;
		}

		/// <summary>
		/// Pushes the bot to a new player, in the event the previous host was disconnected. The bots list will always be modified after this method runs. Call this method after removing the player.
		/// </summary>
		/// <param name="index">The position in bots to remove.</param>
		private void pushBot(int index)
		{
			String botID = bots[index].id;
			String botName = bots[index].name;
			ObjectType objectType = bots[index].objectType;
			if (clientList.Count == 0) {
				bots[index].creator = null;
				return;
			}

			String[] ids = clientList.Keys.ToArray();
			Random r = new Random();
			String pid = null; //ID of new host player for bot
			CSCommon.sendData(clientList[pid = ids[r.Next(0, ids.Length)]].client,
				CSCommon.buildCMDString(CSCommon.cmd_createBot, botID, botName, (byte)objectType));
			bots[index].creator = pid;
		}

		/// <summary>
		/// If a player is disconnected who is hosting bots, this method reassigns those bots to a new host.
		/// </summary>
		/// <param name="id">The id of the player who was disconnected.</param>
		private void clearBotsFromPlayer(String id)
		{
			output(LoggingLevels.debug, "Clearing bots.");
			bool clear = true;
			do {
				clear = true;
				for (int i = 0; i < bots.Count; i++) {
					if (id.Equals(bots[i].creator)) {
						pushBot(i);
						clear = false;
						break;
					} //if found player with bot
				} //for
			} while (!clear);
			output(LoggingLevels.debug, "Ok");
		}

		/// <summary>
		/// Checks to see if the given bot id exists already.
		/// </summary>
		/// <param name="id">The bot id to check</param>
		/// <returns>True if the id exists, false otherwise.</returns>
		private bool botIDExists(String id)
		{
			return getBot(id) > -1;
		}

		private bool isBotID(String id)
		{
			return id.StartsWith("B-");
		}

		/// <summary>
		/// Gets the index at which the given bot id resides.
		/// </summary>
		/// <param name="id">The bot id</param>
		/// <returns>The index in the bots array, or -1 if not found</returns>
		private int getBot(String id)
		{
			for (int i = 0; i < bots.Count; i++) {
				if (bots[i].id.Equals(id))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Drops this bot from the server.
		/// </summary>
		/// <param name="index">The index of the bot to remove</param>
		/// <returns>The id of the bot just removed</returns>
		private String removeBot(int index)
		{
			if (bots.Count == 0 || index >= bots.Count)
				return null;
			String id = bots[index].id;
			bots.RemoveAt(index);
			return id;
		}

		/// <summary>
		/// Creates a bot.
		/// </summary>
		/// <param name="creator">The tag of the player that will hold the bot's data initially.</param>
		/// <param name="objectType">The type of the bot.</param>
		private void createBot(String creator, ObjectType objectType)
		{
			String id = getBotID();
			String botName = "Bot " + id;
			String botId = "B-" + id;
			CSCommon.sendData(clientList[creator].client, CSCommon.buildCMDString(CSCommon.cmd_createBot, botId, botName, (byte)objectType));
			//Other players will just see another spawn.
			propogate(CSCommon.buildCMDString(CSCommon.cmd_distributeServerTag, botId, botName, (byte)objectType, (short)0), clientList[creator].client);
			if (objectType == ObjectType.aircraft)
				sendMessage(botName + " has been created.", null);
			bots.Add(new BotInfo(creator, botId, botName, objectType));
			bots.Sort();
			output(LoggingLevels.debug, "Bot " + botName + " created");
		}

		public void setForceGameEnd(String reason)
		{
			forceGameEnd = true;
			this.reason = reason;
		}

		public void queueCriticalMessage(String message)
		{
			lock (serverMessage)
				serverMessage = message;
		}

		private void sendCriticalMessage()
		{
			if (String.IsNullOrEmpty(serverMessage))
				return;
			lock (serverMessage) {
				foreach (Player p in clientList.Values)
					CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_chat, (byte)MessageType.critical, serverMessage));
				serverMessage = "";
			}
		}

		/// <summary>
		/// Sends a private message.
		/// </summary>
		/// <param name="target">The recipient of the message</param>
		/// <param name="message">The message to send</param>
		private void sendPrivateChatMessage(String target, String message)
		{
			Player p = getPlayerByID(target);
			if (p != null)
				CSCommon.sendData(p.client, CSCommon.buildCMDString(CSCommon.cmd_chat, (byte)MessageType.privateMessage, message));
		}

		/// <summary>
		/// Sends a chat message
		/// </summary>
		/// <param name="tag">The tag of the player sending a message. Can be null</param>
		/// <param name="message">The message</param>
		/// <param name="type">The message type</param>
		private void sendChatMessage(String tag, String message, MessageType type)
		{
			if (tag != null)
				message = clientList[tag].name + ": " + message;
			propogate(CSCommon.buildCMDString(CSCommon.cmd_chat, (byte)type, message), (tag != null) ? clientList[tag].client : null);
			output(LoggingLevels.chat, message);
		}

		/// <summary>
		/// Gets the player belonging to the specified server tag. This method uses TryGetValue as a fail-safe.
		/// </summary>
		/// <param name="tag">The server tag.</param>
		/// <returns>The player object associated with the tag, or null if not found.</returns>
		private Player getPlayerByID(String tag)
		{
			Player p = null;
			clientList.TryGetValue(tag, out p);
			return p;
		}
	} //class
} //namespace