/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.Input;
using SharpDX.DirectSound;
using SharpDX.DirectInput;

namespace TDV
{
	public class Interaction
	{
		public enum FightType : byte
		{
			betrayal,
			lastFight1,
			lastFight2,
			lastFight3
		}
		//flag for what object state is requested
		public enum RangeFlag : byte
		{
			existing = 0,
			existingButTerminated = 1,
			nonexistent = 2
		}
		//This enumerator defines in what context a Projector called the kill() method of this class.
		//For instance, if the Projector was shot down, the destroyedByWeapon value would be supplied to the appropriate method.
		public enum Cause : byte
		{
			none,
			finishedRace,
			lostRace,
			destroyedByWeapon,
			overWeight,
			destroyedByImpact,
			destroyedByClient,
			engineDestroyed,
			stalled,
			selfDestructed,
			sentForTermination,
			sentForLanding,
			quitGame,
			demoExpired,
			successfulLanding
		}
		public static Person player;
		private static Thread spectatorThread, playersThread;
		private static bool m_determiningKillConditions;

		/// <summary>
		/// If an object is going through .kill(), this flag will be set to prevent other objects
		/// from prematurely determining and end state.
		/// </summary>
		public static bool determiningKillConditions
		{
			get { return Interaction.m_determiningKillConditions; }
			set { Interaction.m_determiningKillConditions = value; }
		}

		public static bool inChat
		{
			get;
			set;
		}

		private static bool m_inOnlineGame; //denotes whether an online game is currently in session. used to block hangar loop.

		public static bool inOnlineGame
		{
			get { return m_inOnlineGame; }
			set { m_inOnlineGame = value; }
		}
		public static Dictionary<String, Projector> theArray;
		public static int bColor, gColor, yColor, rColor;
		private static int m_objectCount;
		private static String currentTrack;
		private static String[] sectorRows = {"a", "b", "c", "d",
	"e","f","g","h","i","j","k","l","m","n","o","p","q",
									 "r", "s", "t", "u", "v", "w", "x", "y", "z"};

		public static ArrayList holderArray;

		private static Object lockObject = new Object();
		private static List<Projector> deadArray;
		//Represents user-friendly name of player in an online game.
		private static string m_playerName;
		public static String playerName
		{
			get { return m_playerName; }
			set { m_playerName = value; }
		}

		public static bool isReadingDestroyed;

		private static bool requestedReadStop;
		private static string dName;
		private static List<Projector> terminatedArray;
		private static List<Aircraft> rankArray;
		private static double nextX;
		private static Int32 m_completeCount;
		private static int m_numberOfNonlisted;
		private static OggBuffer miscSound;

		public static int numberOfNonlisted
		{
			get { return (m_numberOfNonlisted); }
			set { m_numberOfNonlisted = value; }
		}

		public static double nextXIncrement
		{
			get { return (0.2); }
		}


		public static Int32 completedCount
		{
			get { return (m_completeCount); }
			set { m_completeCount = value; }
		}
		public static String addToObjectTable(String id, Projector p)
		{
			lock (theArray)
			{
				if (theArray.ContainsKey(id))
					throw new ArgumentException("The key " + id + " already exists, "
						+ "with object name " + theArray[id].name + ".");
				theArray.Add(id, p);
				if (!(p is WeaponBase))
					Interlocked.Increment(ref m_objectCount);
				return id;
			}
		}
		public static String addToObjectTable(Projector p)
		{
			return addToObjectTable(getID(), p);
		}
		public static void removeFromObjectTable(String id)
		{
			lock (theArray)
			{
				Projector p = objectAt(id);
				if (p == null)
					return;
				theArray.Remove(id);
				System.Diagnostics.Trace.WriteLineIf(p is WeaponBase, p.id + " removed.");
				if (!(p is WeaponBase))
					Interlocked.Decrement(ref m_objectCount);
			}
		}

		public static void addToArray(ref Projector[] suppliedArray, ref Projector ac)
		{
			Monitor.Enter(theArray);
			if ((suppliedArray == null))
			{
				suppliedArray = new Projector[1];
			}
			if ((suppliedArray.GetUpperBound(0) == 0))
			{
				if ((suppliedArray[0] == null))
				{
					suppliedArray[0] = ac;
					Monitor.Exit(theArray);
					return;
				}
			}

			Array.Resize(ref suppliedArray, suppliedArray.Length + 1);
			////add one more slot
			suppliedArray[suppliedArray.GetUpperBound(0)] = ac;
			Monitor.Exit(theArray);
		}

		public static int length
		{
			get { return m_objectCount; }
		}

		public static int maxIndex
		{
			get { return (length - 1); }
		}

		public static List<Projector> getOtherObjects(Projector exclude)
		{
			lock (theArray)
			{
				List<Projector> objects = null;
				foreach (String i in getAllIDs())
				{
					Projector p = objectAt(i);
					if (p is WeaponBase)
						continue;
					if (!exclude.Equals(p))
					{
						if (objects == null)
							objects = new List<Projector>();
						objects.Add(p);
					}
				}

				return objects;
			}
		}
		public static List<Projector> getProjectiles(WeaponBase exclude, Projector target)
		{
			lock (theArray)
			{
				List<Projector> objects = null;
				foreach (String i in getAllIDs())
				{
					Projector p = objectAt(i);
					if (!(p is WeaponBase))
						continue;
					if (exclude != null && exclude.Equals(p))
						continue;
					WeaponBase w = (WeaponBase)p;
					if (target != null)
					{
						if (w.origTarget != null && target.Equals(w.origTarget))
						{
							if (objects == null)
								objects = new List<Projector>();
							objects.Add(p);
							continue;
						}
						if (w.target != null && target.Equals(w.target))
						{
							if (objects == null)
								objects = new List<Projector>();
							objects.Add(p);
							continue;
						}
					}
					else
					{ //target == null, so add anyway
						if (objects == null)
							objects = new List<Projector>();
						objects.Add(p);
					}
				} //foreach

				return objects;
			}
		}
		public static List<Projector> getProjectiles(Projector target)
		{
			return getProjectiles(null, target);
		}

		public static void addToDeadArray(Projector v)
		{
			lock (deadArray)
			{
				deadArray.Add(v);
			}
		}

		//The method below gets all alive objects
		//Warning: just because an isTerminated flag is true, does not mean
		//that the object is dead.
		public static List<Projector> getAliveObjects()
		{
			lock (theArray)
			{
				List<Projector> objects = null;

				foreach (String i in getAllIDs())
				{
					Projector p = objectAt(i);
					if (p is WeaponBase)
						continue;

					if (p.damage > 0)
					{
						if (objects == null)
							objects = new List<Projector>();
						objects.Add(p);
					} //if found
				} //foreach
				return objects;
			} //lock
		}

		/// <summary>
		/// Used during Racing Mode
		///   The method below gets all Projectors whose isTerminated flag is set to true.
		///Warning: just because isTerminated is true, the Projector may still be alive. To ensure that the Projector is destroyed, use the getDeadObjects() method instead.
		/// </summary>
		/// <returns>The terminated array</returns>
		public static List<Projector> getTerminatedObjects()
		{
			lock (terminatedArray)
			{
				if (terminatedArray.Count == 0)
					return null;
				return terminatedArray;
			}
		}

		/// <summary>
		/// Used during Racing Mode
		/// Gets the "dead array,"
		/// or the array where objects go once destroyed.
		/// IE: this array cotnains all objects that didn't finish the race.
		/// </summary>
		/// <returns>The losers</returns>
		public static List<Projector> getDeadObjects()
		{
			lock (deadArray)
			{
				if (deadArray.Count == 0)
					return null;
				return deadArray;
			}
		}

		public static double getAndUpdateX()
		{
			nextX += nextXIncrement;
			return (nextX);
		}

		///<summary>
		///The method below sorts the rankArray in terms of place.
		///the first place value is at the front of the Array
		///eg. getRanks()[0]
		///</summary>   
		///<returns>An array of Aircraft in rank order with the highest rank (first place) being at the upper bounds. Returns null if nothing has been ranked yet.</returns>
		public static List<Aircraft> getRanks()
		{
			if (rankArray.Count == 0)
				return null;
			return rankArray;
		}

		private static void announceRaceEnd()
		{
			List<Aircraft> v = getRanks();
			if (v == null)
				return;

			int i = 0;
			Aircraft o = null;
			SelfVoice.nStop = false;
			for (i = 0; i < v.Count; i++)
			{
				o = v[i];
				SelfVoice.resetPath();
				if (v.Count == 1
					|| i < v.Count - 1)
					SelfVoice.NLS(DSound.NSoundPath + "\\o" + o.name + ".wav&"
						+ DSound.NSoundPath + "\\p" + Common.convertToWordNumber(i + 1)
						+ ".wav");
				else if (i == v.Count - 1)
					SelfVoice.NLS(DSound.NSoundPath + "\\o" + o.name + ".wav&"
						+ DSound.NSoundPath + "\\plast"
						+ Common.getRandom(1, 3)
						+ ".wav");
			}
			SelfVoice.nStop = false;
			if (deadArray.Count > 0)
			{
				List<Projector> deaths = getDeadObjects();
				for (i = 0; i < deaths.Count; i++)
				{
					SelfVoice.NLS(DSound.NSoundPath + "\\o" + deaths[i].name + ".wav&"
											+ DSound.NSoundPath + "\\plast"
											+ Common.getRandom(1, 3)
											+ ".wav");
				}
			}
		}

		public static void addRank(Aircraft v)
		{
			lock (rankArray)
			{
				rankArray.Add(v);
			}
		}

		public static void kill(Projector v)
		{
			System.Diagnostics.Trace.WriteLine(v.name + " called Interaction.kill with cause " + v.cause);
			System.Diagnostics.Trace.WriteLineIf(v is Chopper, "Choppers remaining now " + Mission.chopperCount);
			Cause c = v.cause;
			if (v.damage <= 0 || c == Cause.lostRace)
				addToDeadArray(v);
			removeFromObjectTable(v.id); //All objects will be removed from table if they call kill
			addTerminated(v);
			advanceToNextMission(v);
			switch (c)
			{
				case Cause.successfulLanding:
					rankArray.Insert(0, (Aircraft)v);
					break;
				case Cause.finishedRace:
					addRank((Aircraft)v);
					break;

				case Cause.destroyedByImpact:
				case Cause.destroyedByClient:
				case Cause.stalled:
				case Cause.destroyedByWeapon:
					if (Options.mode == Options.Modes.teamDeath)
					{
						decrementTeam(v.team);
						if (isGameFinished())
							Options.entryMode = 0; //reset the possible spectator flag which will be set by this craft before it calls .kill()
					}

					if (Options.mode == Options.Modes.mission || v.cause != Cause.destroyedByWeapon)
						break;
					dName = v.name;
					Thread dThread = new Thread(readDestroyed);
					dThread.Start();
					break;
			} //switch

			System.Diagnostics.Trace.WriteLine(v.name + " evaluating isGameFinished...");
			System.Diagnostics.Trace.WriteLine("Length is " + length + ", nonlisted is " + numberOfNonlisted
				+ ". isRaceFinished is " + isRaceFinished(true));
			if (!isGameFinished())
				return;
			terminateAllProjectors(false);
			System.Diagnostics.Trace.WriteLine("Length - nonlisted is " + (length - numberOfNonlisted));
			System.Diagnostics.Trace.WriteLine(v.name + " called end game.");
			Thread t = new Thread(endGame);
			t.Start();
		}


		//In the case of an end game criteria,
		//The method below will only return if
		//all threads are stopped and have returned.
		//therefore, it is recommended
		//to call this method from a separate thread
		//rather than the main game thread
		public static void endGame()
		{
			System.Diagnostics.Trace.WriteLine("End game called.");
			//For racing and dm, no need to call terminated first since objects are terminated elsewhere
			if (Options.mode == Options.Modes.racing)
			{
				waitForAllProjectors(); //block until player lands
				if (!Options.abortGame)
				{ //don't announce ranks if exited game,
					announceRaceEnd(); //For instance, if player asked to land and pressed Escape.
					announceMissionRaceStatus();
				}
			}
			if (Options.mode == Options.Modes.deathMatch || Options.mode == Options.Modes.freeForAll || Options.mode == Options.Modes.teamDeath)
			{
				System.Diagnostics.Trace.WriteLine(Options.mode + " is complete.");
				waitForAllProjectors(); //block until player lands
				System.Diagnostics.Trace.WriteLine("Waited for all projectors.");
				if (!Options.abortGame)
					announceMissionDMStatus();
			}

			if (deadArray.ToArray().Length == length - numberOfNonlisted)
			{
				repop();
				return;
			}

			terminateAllProjectors(true);
			//we're in mission mode.
			//isMissionFinished will be true if the player just beat
			//the game.
			if (isMissionFinished())
			{
				Common.playUntilKeyPress(DSound.SoundPath + "\\o"
										+ (int)Mission.missionNumber + ".ogg");
				Common.fadeMusic(); //stop the music
				Common.playUntilKeyPress(DSound.SoundPath + "\\v1.ogg");
				if (startFight(FightType.lastFight1))
				{
					Common.playUntilKeyPress(DSound.SoundPath + "\\v4.ogg");
					Common.playUntilKeyPress(DSound.SoundPath + "\\v5.ogg");
					Common.playUntilKeyPress(DSound.SoundPath + "\\v6.ogg");
				}
				Common.playUntilKeyPress(DSound.SoundPath + "\\c.ogg");
			}
			repop();
		}

		public static bool isRaceFinished(bool checkPlayer)
		{
			if (Options.mode != Options.Modes.racing)
				return false;

			return length - numberOfNonlisted == 0; //no more objects racing
		}

		/// <summary>
		/// Determines if the player finished the race.
		///This should not be a substitute for isRaceFinished, since it only validates
		///the player's state.
		///Method was implemented to eliminate wait times if player finished before all craft
		///were done racing.
		/// </summary>
		/// <returns>True if player finished, false otherwise</returns>
		public static bool playerFinishedRace()
		{
			if (Options.mode != Options.Modes.racing || Mission.player.damage <= 0)
				return false;
			if (Mission.player.isTerminated)
				return true;
			List<Projector> obj = getTerminatedObjects();
			if (obj == null)
				return false;
			//If the player finished the race, no need to wait for other
			//craft to finish racing.
			foreach (Projector p in obj)
			{
				if (p.Equals(Mission.player))
					return true;
			} //foreach
			return false;
		}

		public static bool isFFAFinished()
		{
			if (Options.mode != Options.Modes.freeForAll)
				return false;
			if (Mission.player == null) //if Spectator, player will be NULL
				return false; //Don't check entryMode specifically since it may be reset while objects are still terminating.
			return Mission.player.damage <= 0;
		}

		public static bool isTeamDeathFinished()
		{
			if (Options.mode != Options.Modes.teamDeath)
				return false;
			return bColor == 0 || gColor == 0 || rColor == 0 || yColor == 0;
		}

		public static bool isDeathMatchFinished()
		{
			if (Options.mode != Options.Modes.deathMatch && Options.mode != Options.Modes.oneOnOne)
				return false;
			if (Mission.player.requestedLand)
				return false;
			List<Projector> obj = getDeadObjects();
			if (obj == null)
				return false;
			if (obj.Contains(Mission.player))
				return true;
			if (length == 2 || Mission.player.cause == Cause.successfulLanding)
				return true;
			return false;
		}

		public static void startAllThreads()
		{
			for (int i = 0; i < holderArray.Count; i++) {
				holderAt(i).startThread();
				System.Diagnostics.Trace.WriteLine("Started holder " + i);
			}
		}


		//Returns an array that is populated with all objects in the specified range of the Projector requesting the data.
		//The requester parameter is casted down to Projector to allow for flexibility
		//since all objects that can move inherit the Projector class.
		public static List<Projector> getObjectsInRange(Projector requester, Range theRange,
				  RangeFlag flag, bool ordered)
		{
			List<Projector> obj = null;
			if (flag == RangeFlag.existing)
				obj = getNonterminatedObjects();
			if (obj == null)
				return null;

			int i = 0;
			do
			{
				if (!obj[i].showInList
					|| !Weapons.inRange(requester, obj[i], theRange)
					|| requester.Equals(obj[i]))
					obj.RemoveAt(i--);
			} while ((++i) < obj.Count);
			if (obj.Count == 0)
				return null;
			if (!ordered) //don't want it sorted
				return obj;
			List<Projector> projectorArray = new List<Projector>(obj.Count);
			int nextAddIndex = 0;
			int nearestObject = -1, tracker = 0;
			while (obj.Count > 0)
			{
				foreach (Projector p in obj)
				{
					if (nearestObject == -1
						|| Degrees.getDistanceBetween(requester.x, requester.y,
						p.x, p.y) <= Degrees.getDistanceBetween(requester.x, requester.y,
						obj[nearestObject].x, obj[nearestObject].y))
						nearestObject = tracker;
					tracker++;
				} //forEach
				//now nearestObject holds new index of nearest object to requester.
				projectorArray.Add(obj[nearestObject]);
				obj.RemoveAt(nearestObject);
				nextAddIndex++;
				nearestObject = -1;
				tracker = 0;
			} //while
			return (projectorArray);
		}

		//Overloaded. Does not sort objects based on
		//nearness to requester.
		public static List<Projector> getObjectsInRange(Projector requester, Range theRange, RangeFlag flag)
		{
			return (getObjectsInRange(requester, theRange, flag, false));
		}

		public static List<Projector> getNonterminatedObjects()
		{
			List<Projector> rangeArray = null;
			lock (theArray)
			{
				foreach (String i in getAllIDs())
				{
					Projector p = objectAt(i);
					if (p is WeaponBase)
						continue;

					if (!p.unlisted && !p.isRequestedTerminated)
					{
						if (rangeArray == null)
							rangeArray = new List<Projector>();
						rangeArray.Add(p);
					}
				}
			} //lock
			return rangeArray;
		}

		public static void clearData(bool unloadMusic)
		{
			DXInput.unloadEffect2(false); //stop effects that should not be stopped in game.
			clearServerData();
			terminateAllProjectors(true);
			Options.demoExpired = false;
			if (unloadMusic && !Common.failedConnect)
			{ //Don't fade music if failed connect since main menu music still playing
				if (Common.music != null)
					Common.fadeMusic();
				DSound.masterMusicVolume = Common.maxMusicVol;
			}
			Mission.reset(Options.Modes.mission);
			Common.exitMenus = false;
			Options.entryMode = 0;
			currentTrack = null;
			rankArray = new List<Aircraft>();
			DSound.setListener(); //reset the listener, or create it
			nextX = 0.0;
			m_completeCount = 0;
			resetObjectCount();
			theArray = null;
			theArray = new Dictionary<String, Projector>();
			holderArray = null;
			holderArray = new ArrayList();
			registerHolder();
			registerHolder(new WeaponsHolder());
			deadArray = new List<Projector>();
			terminatedArray = new List<Projector>();
			Options.abortGame = false;
			playerName = "";
			if (Options.isPlayingOnline)
				Options.mode = Options.Modes.none;
		}

		public static void clearData()
		{
			clearData(true);
		}

		public static void resetObjectCount()
		{
			m_objectCount = 0;
			numberOfNonlisted = 0;
		}

		//stops all projectors.
		public static void terminateAllProjectors(bool wait)
		{
			if (theArray == null)
				return;
			System.Diagnostics.Trace.WriteLine("Terminating all holders...");
			foreach (Holder h in holderArray)
				h.stopNow();

			if (wait)
				waitForAllProjectors();
			System.Diagnostics.Trace.WriteLine("Done terminating, moving on");
		}


		private static void repop()
		{
			if (isReadingDestroyed)
			{
				while (isReadingDestroyed)
					Thread.Sleep(10);
			}

			terminateAllProjectors(false);
			Common.repop();
		}

		/// <summary>
		/// Stops weapons thread.
		/// </summary>
		/// <param name="waitTillHaulted">If true, method will block until weapons are stopped. If false, method will send halt command but will not block.</param>
		private static void stopWeapons(bool waitTillHaulted)
		{
			holderAt(1).changeHaultStatus(true);
			if (!waitTillHaulted)
				return;
			while (!holderAt(1).haulted) ;
		}

		public static void resumeWeapons()
		{
			//The condition below is necessary since when a game loses focus, a pause command is sent to every holder.
			//However, an event could be executing that will resume
			//all holders after it is done.
			//To prevent that event from resuming holders while the game does not have focus,
			//we will block below until we are sure the game has focus.
			while (!Common.gameHasFocus)
				Thread.Sleep(1000);
			holderAt(1).changeHaultStatus(false);
		}

		private static void playNextMissionObjective()
		{
			stopWeapons(true);
			if (Options.demoExpired)
			{
				resumeWeapons();
				repop();
				return;
			}
			muteAllObjects(Mission.missionNumber == Mission.Stage.discovery || Mission.missionNumber == Mission.Stage.juliusBattle);
			Common.fadeMusic(Mission.missionNumber == Mission.Stage.discovery
				|| Mission.missionNumber == Mission.Stage.juliusBattle);
			System.Diagnostics.Trace.WriteLine("Made it past suspend threads");
			if (Mission.juliusDieCount == 0)
				Common.playUntilKeyPress(DSound.SoundPath + "\\o" + (int)Mission.missionNumber + ".ogg");
			else if (Mission.juliusDieCount < Mission.maxJuliusDieCount)
				Common.playUntilKeyPress(DSound.SoundPath + "\\j5-" + Mission.juliusDieCount + ".ogg");

			if (Mission.missionNumber == Mission.Stage.discovery)
			{
				if (!startFight(FightType.betrayal))
				{
					endGame();
					return;
				}
			}

			Mission.Stage prevMission = Mission.missionNumber;
			if (!Mission.isJuliusFight)
				Mission.missionNumber++;
			if (Mission.missionNumber == Mission.Stage.powerPlant ||
				Mission.missionNumber == Mission.Stage.chopperFight ||
				Mission.isJuliusFight)
			{
				Common.fadeMusic();
				Common.startMusic();
			}
			if (Mission.missionNumber != Mission.Stage.gameEnd)
			{
				Common.restoreMusic(DSound.masterMusicVolume);
				resumeAndUnmute();
			}
			if (!Common.isValidLicense()
							&& prevMission == Mission.Stage.missileHit)
			{
				((Aircraft)Mission.player).endDemo();
				return;
			}
		}

		public static void advanceToNextMission(Projector v)
		{
			if (isGameFinished())
				return; //player could access this method if destroyed
			if (Options.mode == Options.Modes.mission)
			{
				if (v.Equals(Mission.player))
				{
					playNextMissionObjective();
					if (Mission.missionNumber == Mission.Stage.radarTowers)
						Mission.setUpBridge();
				}

				//Below, radar towers carry out missile attacks,
				//and also must be destroyed
				if (v == Mission.player || v is RadarTower)
				{
					if (Mission.isDestroyingRadar)
					{
						if (Mission.radarCount == 0)
						{
							Mission.isDestroyingRadar = false;
							Mission.createNewJuliusAircraft(208.0, 210.0);
							Mission.isJuliusFight = true;
							Mission.refueler.setJuliusHoverPosition();
							playNextMissionObjective();
						}
					} //if destroying radar
				} //if radar tower

				if (v is TrainingCamp)
				{
					playNextMissionObjective();
					Mission.setUpAirbase();
				}

				if (v is Bridge)
				{
					playNextMissionObjective();
					Mission.isDestroyingRadar = true;
					Mission.setUpRadarStations();
				}

				if (v is PowerPlant)
				{
					playNextMissionObjective();
					holderAt(0).add(new Chopper(300, 190));
					holderAt(0).add(new Chopper(313, 190));
					holderAt(0).add(new Chopper(308, 170));
					holderAt(0).add(new Chopper(308, 200));
					Mission.isSwarm = true;
				}

				if (v is AirBase)
				{
					playNextMissionObjective();
					for (int i = 1; i <= 2; i++)
						Mission.createNewFighter(Mission.airbase.x, Mission.airbase.y);
					Mission.setUpPowerPlant();
				}
				if (v is Chopper)
				{
					if (Mission.isJuliusFight)
						return;
					Mission.chopperCount--;
					if (Mission.chopperCount == 0)
					{
						Mission.isSwarm = false;
						playNextMissionObjective();
					}
				}

				if (v is JuliusAircraft)
				{
					//JuliusAircraft will update dieCount when it explodes, so no need to do it here.
					if (Mission.juliusDieCount == Mission.maxJuliusDieCount)
						Mission.isJuliusFight = false;
					//Need to negate juliusFight flag before calling playNextObjective, otherwise it will play
					//A julius reincarnation message.
					playNextMissionObjective();
				} //if juliusAircraft
			} //if mode is mission
		}

		private static void announceMissionDMStatus()
		{
			if (Mission.isMission)
			{
				SelfVoice.nStop = false;
				SelfVoice.NLS(DSound.NSoundPath + "\\deathmatch.wav&#" + (++Mission.deathMatchesComplete));
				List<Projector> d = getDeadObjects();

				if (d != null
								&& !d.Contains(Mission.player))
				{
					Mission.deathMatchScore += Mission.pointsWorth;
					miscSound = DSound.loadOgg(DSound.SoundPath + "\\win.ogg");
					miscSound.play();
				}
				else
				{
					miscSound = DSound.loadOgg(DSound.SoundPath + "\\dl" + Common.getRandom(1, 6) + ".ogg");
					miscSound.play();
				} //if player won

				while (miscSound.isPlaying())
					Thread.Sleep(10);
				SelfVoice.NLS(DSound.NSoundPath + "\\score.wav&#" + Mission.deathMatchScore + "&" + DSound.NSoundPath + "\\outof.wav&#" + Mission.passingDeathMatchScore);

				if (Mission.deathMatchScore >= Mission.passingDeathMatchScore)
				{
					miscSound = DSound.loadOgg(DSound.SoundPath + "\\dv.ogg");
					Common.fadeMusic();
					miscSound.play();
					while (miscSound.isPlaying())
						Thread.Sleep(10);
				} //if pass

				SelfVoice.nStop = true;
				Mission.writeToFile();
			} //if isMission

			if (Options.mode == Options.Modes.teamDeath)
			{
				//Find out which team value has > 0 players, this is winner
				int t = Math.Max(bColor, Math.Max(gColor, Math.Max(rColor, yColor)));
				Projector.TeamColors winningTeam;
				if (t == bColor)
					winningTeam = Projector.TeamColors.blue;
				else if (t == gColor)
					winningTeam = Projector.TeamColors.green;
				else if (t == rColor)
					winningTeam = Projector.TeamColors.red;
				else
					winningTeam = Projector.TeamColors.yellow;
				int winningTeamNum = (int)winningTeam;
				SelfVoice.NLS("t" + winningTeamNum + ".wav");
				OggBuffer teamWin = DSound.loadOgg(DSound.NSoundPath + "\\tw" + Common.getRandom(1, 2) + ".ogg");
				teamWin.play();
				while (teamWin.isPlaying())
					Thread.Sleep(10);
			} //if team death
		}

		private static void announceMissionRaceStatus()
		{
			System.Diagnostics.Trace.WriteLine("Checking announce race");
			if (Mission.isMission)
			{
				System.Diagnostics.Trace.WriteLine("Called announce race");
				SelfVoice.nStop = false;
				SelfVoice.NLS(DSound.NSoundPath + "\\race.wav&#" + (++Mission.racesComplete));
				List<Aircraft> obj = getRanks();
				if (obj == null)
				{
					miscSound = DSound.loadOgg(DSound.SoundPath + "\\rl" + Common.getRandom(1, 6) + ".ogg");
					miscSound.play();
				} //if player died and no crafts one the race
				else
				{
					if (Mission.player.Equals((Projector)obj[0]))
					{
						miscSound = DSound.loadOgg(DSound.SoundPath + "\\win.ogg");
						miscSound.play();
					}
					else
					{ //if race done but player lost
						miscSound = DSound.loadOgg(DSound.SoundPath + "\\rl" + Common.getRandom(1, 6) + ".ogg");
						miscSound.play();
					} //if player won race
				} //if getRank is null
				while (miscSound.isPlaying())
				{
					Thread.Sleep(10);
				}
				System.Diagnostics.Trace.WriteLine("Done playing misc sound");
				SelfVoice.NLS(DSound.NSoundPath + "\\score.wav&#" + Mission.racingScore + "&" + DSound.NSoundPath + "\\outof.wav&#" + Mission.passingRacingScore);
				if ((Mission.racingScore >= Mission.passingRacingScore))
				{ //if won race sim.
					miscSound = DSound.loadOgg(DSound.SoundPath + "\\rv.ogg");
					miscSound.play();
					while (miscSound.isPlaying())
					{
						Thread.Sleep(10);
					}
				} //if pass

				SelfVoice.nStop = true;
				Mission.writeToFile();
			} //if isMission
		}

		////This method haults the calling thread until
		////until all Projectors have signaled terminated.
		////This method is usually used to stop any moving Projectors
		////at game end.
		private static void waitForAllProjectors()
		{
			System.Diagnostics.Trace.WriteLine("Waiting for all objects to stop...");
			if (holderArray != null)
			{
				if (holderArray.Count > 0)
				{
					////if objects could still be accessing the array,
					////don't clear it until they have stopped.
					bool allClear = false;
					while (!allClear)
					{
						allClear = true;
						foreach (Holder h in holderArray)
						{
							System.Diagnostics.Trace.WriteLine(h.stopped);
							if (!h.stopped)
								allClear = false;
						} //for
						Thread.Sleep(10);
					} //while !allClear
				} //if things in holderArray
			} //if holderArray!=null
			System.Diagnostics.Trace.WriteLine("Done waiting, moving on.");
		}

		private static void waitForProjector(Projector p)
		{
			while (!p.isTerminated)
			{
				Thread.Sleep(Common.intervalMS);
			}
		}


		public static bool isGameFinished(bool checkPlayer)
		{
			if (Options.entryMode == 1)
				checkPlayer = false;
			return Options.requestedShutdown
	  || (Options.isPlayingOnline && Client.closed) //server down
			|| Options.abortGame
			|| Options.serverEndedGame
			|| isRaceFinished(checkPlayer)
					  || isDeathMatchFinished()
					  || isMissionFinished()
					  || isTrainingFinished()
					  || isTeamDeathFinished()
					  || (checkPlayer && isFFAFinished())
					  || (checkPlayer && Mission.player.damage <= 0)
					  || (Options.mode == Options.Modes.mission
					  && Mission.player.isRequestedTerminated)
					  || Options.demoExpired;
		}

		public static bool isGameFinished()
		{
			return isGameFinished(true);
		}

		public static void addTerminated(Projector p)
		{
			lock (terminatedArray)
			{
				terminatedArray.Add(p);
			}
		}
		public static bool isMissionFinished()
		{
			if (Options.mode != Options.Modes.mission)
				return false;
			if (Mission.missionNumber == Mission.Stage.gameEnd)
				return true;
			//lets you fight julius
			return false;
		}

		public static Projector getNearestObject(Projector requester, params Projector[] objects)
		{
			if (objects == null)
			{
				return (null);
			}
			RelativePosition[] r = new RelativePosition[objects.Length];
			RelativePosition[] rSorted = new RelativePosition[objects.Length];
			int i = 0;
			double x = requester.x;
			double y = requester.y;
			double z = requester.z;
			int direction = requester.direction;

			for (i = 0; i <= r.Length - 1; i++)
			{
				double tX = objects[i].x;
				double tY = objects[i].y;
				double tZ = objects[i].z;
				int tDirection = objects[i].direction;
				r[i] = Degrees.getPosition(x, y, z, direction, tX, tY, tZ, tDirection);
			}
			Array.Copy(r, rSorted, r.Length);
			Array.Sort(rSorted);
			for (i = 0; i <= r.Length - 1; i++)
			{
				if (rSorted[0].Equals(r[i]))
				{
					rSorted = null;
					r = null;
					return (objects[i]);
				}
			}
			return (null);
		}

		/// <summary>
		/// Retrieves the Projector with the specified tag.
		/// </summary>
		/// <param name="id">The ID or tag of the object to retrieve.</param>
		/// <returns>The object if it was found, or NULL otherwise. This method uses TryGetValue since it is possible to test for tags that don't exist, especially in an online game.</returns>
		public static Projector objectAt(String id)
		{
			Projector p = null;
			return (!theArray.TryGetValue(id, out p)) ? null : p;
		}

		private static void readDestroyed()
		{
			if (Options.mode == Options.Modes.training)
				return;
			if (!Options.isPlayingOnline)
			{
				if (isReadingDestroyed)
				{
					requestedReadStop = true;
					while (isReadingDestroyed)
						Thread.Sleep(5);
				}
				isReadingDestroyed = true;
				SecondarySoundBuffer n = DSound.LoadSound(DSound.NSoundPath + "\\o"
				 + dName + ".wav");
				DSound.PlaySound(n, true, false);
				while (DSound.isPlaying(n))
				{
					if (requestedReadStop)
						break;
					Thread.Sleep(5);
				}
				DSound.unloadSound(ref n);

				if (!requestedReadStop)
				{
					n = DSound.LoadSound(DSound.NSoundPath + "\\d"
					 + Common.getRandom(1, 4) + ".wav");
					DSound.PlaySound(n, true, false);
					while (DSound.isPlaying(n))
					{
						if (requestedReadStop)
							break;
						Thread.Sleep(5);
					}
					DSound.unloadSound(ref n);
				} //if ! readstop
			} // if !online
			//Every call to this method is guaranteed to come across this block of code.
			requestedReadStop = false;
			isReadingDestroyed = false;
		}


		//Returns the index at which new holder was added
		public static int registerHolder()
		{
			return (holderArray.Add(new Holder()));
		}

		public static int registerHolder(Holder h)
		{
			return (holderArray.Add(h));
		}

		public static Holder holderAt(int index)
		{
			if (holderArray == null)
				return (null);
			return (
				(Holder)holderArray[index]
				);
		}

		public static void disposeAllProjectors()
		{
			if (theArray == null)
				return;
			foreach (Projector p in theArray.Values)
				p.Dispose();
		}

		public static String getSector(Projector p, bool returnFormatted)
		{
			int r = (int)p.x / 10;
			int c = (int)p.y / 10 + 1;
			if (r < 0
				|| c < 1
				|| c > sectorRows.Length * 10)
				return ((!returnFormatted) ? ""
					: "sec.wav"); //just sector with no designation
			StringBuilder row = new StringBuilder();
			if (r >= 26)
			{
				for (int i = 1; i <= r / 26; i++)
					row.Append(sectorRows[25]);
				if (r % 26 != 0)
					row.Append(sectorRows[
						(r % 26) - 1
						]);
			}
			else
				row.Append(sectorRows[r]);
			int col = c;
			String sec = row.ToString() + "," + col;
			if (!returnFormatted)
				return (sec);
			return (Common.sectorToString(sec));
		}

		//returns a self-voice formatted representation of sector
		public static String getSector(Projector p)
		{
			return getSector(p, true);
		}


		private static String getID()
		{
			bool validID = false;
			String theID = null;
			while (!validID)
			{
				char[] chars = new char[Common.getRandom(10, 30)];
				int n = chars.Length;
				for (int i = 0; i < n; i++)
				{
					//If set, we will select a number 0-9,
					//else a letter
					bool selectNumber =
					 Common.getRandom(1, 2) == 2;
					if (selectNumber)
						chars[i] = (char)Common.getRandom('0', '9');
					else
						chars[i] = (char)Common.getRandom('A', 'Z');
				}
				theID = new String(chars);
				validID = !theArray.ContainsKey(theID);
			} //while
			return theID;
		}

		/// <summary>
		///  Gets all IDs in the object table.
		///  This includes projectiles. You can enumerate over the collection using
		///  type String.
		/// </summary>
		/// <returns>KeyCollection containing all IDS</returns>
		public static Dictionary<String, Projector>.KeyCollection getAllIDs()
		{
			return theArray.Keys;
		}

		public static String[] getAllNonWeaponIDs()
		{
			String[] ids = new String[m_objectCount];
			int i = 0;
			foreach (String s in getAllIDs())
			{
				if (objectAt(s) is WeaponBase)
					continue;
				ids[i++] = s;
			}
			return ids;
		}

		public static void clearLocks(String id)
		{
			foreach (String s in getAllIDs())
			{
				if (objectAt(s) is MissionObjectBase)
					((MissionObjectBase)objectAt(s)).removeLockIf(id);
			}
		}

		public static void incrementTeam(Projector.TeamColors c)
		{
			if (c == Projector.TeamColors.blue && ++bColor == 0)
				bColor++;
			else if (c == Projector.TeamColors.green && ++gColor == 0)
				gColor++;
			else if (c == Projector.TeamColors.red && ++rColor == 0)
				rColor++;
			else if (c == Projector.TeamColors.yellow && ++yColor == 0)
				yColor++;
		}
		public static void decrementTeam(Projector.TeamColors c)
		{
			if (c == Projector.TeamColors.blue)
				bColor--;
			else if (c == Projector.TeamColors.green)
				gColor--;
			else if (c == Projector.TeamColors.red)
				rColor--;
			else if (c == Projector.TeamColors.yellow)
				yColor--;
		}

		public static void clearServerData()
		{
			bColor = -1; rColor = -1; gColor = -1; yColor = -1;
			Options.serverEndedGame = false;
			Client.clearServerData();
		}

		/// <summary>
		///  Starts spectator mode. This method creates a new thread so the calling emthod doesn't block.
		/// </summary>
		public static void startSpectatorMode()
		{
			spectatorThread = new Thread(startSpectatorModeThread);
			spectatorThread.Start();
		}
		private static void startSpectatorModeThread()
		{
			bool endSpectatorMode = false;
			bool dirtyTrack = true; //Pick random player to spectate when first entering mode.
			if (m_objectCount == 0)
				SapiSpeech.speak("There are no players currently in the game. You can wait for players or press ESCAPE to exit.", SapiSpeech.SpeakFlag.interruptable);
			while (!endSpectatorMode)
			{
				if (m_objectCount > 0 //in case spectator has entered game with 0 players, IE: empty FFA
					&& (DXInput.isFirstPress(Key.L) || dirtyTrack))
				{
					String[] names = new String[m_objectCount];
					String[] ids = new String[names.Length];
					int i = 0;
					foreach (String s in getAllNonWeaponIDs())
					{
						//Make current id tracked a blank option so it can't be selected.
						names[i] = (!String.IsNullOrEmpty(currentTrack) && currentTrack.Equals(s)) ? "" : objectAt(s).name;
						ids[i++] = s;
					} //foreach
					if (m_objectCount == 1 && !string.IsNullOrEmpty(currentTrack) && currentTrack.Equals(ids[0]))
						continue;			//No need to present menu if we're spectating a craft and its the only craft in the game.

					int choice = (dirtyTrack) ? Common.getRandomItem(names) : Common.GenerateMenu(null, names);
					if (choice == -1)
						continue;
					if (!String.IsNullOrEmpty(currentTrack) && !dirtyTrack)
						objectAt(currentTrack).removeSpectator();
					currentTrack = ids[choice];
					objectAt(ids[choice]).setSpectator();
					SapiSpeech.speak("You are now tracking " + names[choice], SapiSpeech.SpeakFlag.interruptable);
					dirtyTrack = false;
				} //if l
				dirtyTrack = !String.IsNullOrEmpty(currentTrack) && objectAt(currentTrack) == null;
				if (DXInput.isFirstPress(Key.Escape) || isGameFinished())
					endSpectatorMode = true;
				Common.executeExtraCommands(Common.getServerItems());
				Thread.Sleep(50);
			} //while !endSpectatorMode
			if (Options.requestedShutdown)
				return;
			Options.abortGame = true; //force local game to end and send disconnectMe command to server (see Common.repop)
			endGame();
		}

		/// <summary>
		/// This method will be called in response to  the cmd_requestCreate command sent before creating the player's aircraft.
		/// This method will be called from Client and will be in response to the cmd request.
		/// It will contain position for the player's aircraft. See Server.performCMDRCV for more information.
		/// </summary>
		/// <param name="name">The name of the player</param>
		/// <param name="maxWeight">The maximum weight capacity.</param>
		public static void createPlayer(String name, int maxWeight)
		{
			playerName = name;
			Track t = new Track(Options.currentTrack);
			holderAt(0).add(Mission.player = new Aircraft(0, 1500, name,
			   false, t));
			Mission.player.setWeight(maxWeight);
			Mission.player.role = OnlineRole.sender;
			Mission.player.setID(Client.serverTag);
			waitForPlayers();
		}

		/// <summary>
		/// This method will be called by Client.processRCV,
		/// and will create a copy of an aircraft created on the server.
		/// This method will only run as the result of a cmd_distributeServerTag command from another client.
		/// </summary>
		/// <param name="id">The ID of the object to create, sent by the server</param>
		/// <param name="name">The user-friendly name of this object, as sent by the server</param>
		/// <param name="role">The role, whether receiver or sender or bot.</param>
		/// <param name="type">The type of the object to create, defined by ObjectType found in the Client class.</param>
		/// <returns>The created object</returns>
		public static Projector createObjectFromServer(String id, String name, OnlineRole role, ObjectType type)
		{
			Projector obj = Interaction.objectAt(id);
			if (obj != null)
			{
				obj.stopRequestingTerminate();
				obj.role = role;
				Client.setNewJoin();
				return obj;
			}
			Track t = new Track(Options.currentTrack);
			Projector p = null;
			if (type == ObjectType.aircraft)
				p = new Aircraft(0, 1500, name, true, t);
			else if (type == ObjectType.carrierBlue)
				p = new AircraftCarrier(5.0, 5.0);
			else if (type == ObjectType.carrierGreen)
				p = new AircraftCarrier(5.0, 10.0);
			else if (type == ObjectType.carrierRed)
				p = new AircraftCarrier(8.0, 5.0);
			else if (type == ObjectType.carrierYellow)
				p = new AircraftCarrier(8.0, 10.0);
			p.role = role;
			holderAt(0).add(p);
			p.setID(id);
			//Client.setNewJoin();
			return p;
		}

		/// <summary>
		/// This method will be called when Client receives the cmd_startGame command.
		/// </summary>
		public static void startMultiplayerGame()
		{
			startAllThreads();
		}

		public static void waitForPlayers()
		{
			playersThread = new Thread(waitForPlayersThread);
			playersThread.Start();
		}

		private static void waitForPlayersThread()
		{
			if (Client.gameHost)
			{
				DSound.playAndWait(DSound.NSoundPath + "\\j.wav");
				while (DXInput.isKeyHeldDown(Key.Return))
					Thread.Sleep(5);
			}

			while (!Client.hostStartedGame)
			{
				if (isGameFinished()) //host pressed escape and ended game.
					break;
				Thread.Sleep(50);
			} //while

			giveSignal();
			if (Options.entryMode == 1)
			{
				startMultiplayerGame(); //Since spectators never receive a requestCreate command, see Client class for case CSCommon.cmd_requestCreate
				startSpectatorMode();
			}
		}

		private static void giveSignal()
		{
			if (Options.mode != Options.Modes.freeForAll && Options.entryMode != 1 && !Client.gameHost)
			{
				SecondarySoundBuffer go = DSound.LoadSound(DSound.SoundPath + "\\cc6.wav");
				DSound.PlaySound(go, true, false);
				while (DSound.isPlaying(go))
				{
					if (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown())
						break;
					Thread.Sleep(10);
				}
				DSound.unloadSound(ref go);
			}
		}

		/// <summary>
		/// Checks to see if at most num objects remain.
		/// </summary>
		/// <param name="num">The number of objects that are considered maximum.</param>
		/// <returns>True if the number of objects alive is at most num, false otherwise.</returns>
		public static bool areAtMost(int num)
		{
			foreach (Projector p in theArray.Values)
				System.Diagnostics.Trace.WriteLine(p.name);

			System.Diagnostics.Trace.WriteLine("Are at most...");
			System.Diagnostics.Trace.WriteLine(m_objectCount);
			return m_objectCount <= num;

		}

		private static bool isTrainingFinished()
		{
			return Mission.trainingFinished;
		}

		private static bool startFight(FightType fightType)
		{
			Mission.fightType = fightType;
			Common.startMusic();
			bool won = false;
			bool lost = false;
			List<Person> people = new List<Person>();
			player = new Person(3, 3, 1000, false);
			if (fightType == FightType.betrayal)
			{
				player.initializePunchedSound("fp", 4, "fp1", 4);
				player.initializeSwingSound("fsw", 3, "fsw1", 3);
				player.initializeStepSound("fo", 2);
				player.initializeThrowSounds("ft", 3, "ft1", 2);
			}
			if (fightType == FightType.lastFight1)
			{
				player.initializePunchedSound("fp", 4, "fp-2", 4);
				player.initializeSwingSound("fsw", 3, "fsw-2", 3);
				player.initializeStepSound("fi", 4);
				player.initializeThrowSounds("fg", 1, "ft2", 1);
			}
			if (fightType == FightType.lastFight2)
			{
				player.initializePunchedSound("fp", 4, "fp-2", 4);
				player.initializeSwingSound("fsw", 3, "fsw-2", 3);
				player.initializeStepSound("fl", 4);
				player.initializeThrowSounds("ft", 3, "ft2", 1);
			}
			if (fightType == FightType.lastFight3)
			{
				player.initializePunchedSound("fp", 4, "fp-2", 4);
				player.initializeSwingSound("fsw", 3, "fsw-2", 3);
				player.initializeStepSound("fo", 2);
				player.initializeThrowSounds("ft", 3, "ft2", 1);
			}

			people.Add(player);
			Person opp = new Person(3, 15, 3000, true);
			people.Add(opp);
			if (fightType == FightType.betrayal)
			{
				opp.initializePunchedSound("fp", 4, "fp2", 2);
				opp.initializeSwingSound("fsw", 3, "fsw2", 3);
				opp.initializeStepSound("fo", 2);
				opp.initializeThrowSounds("ft", 3, "ft1", 2);
			}
			if (fightType == FightType.lastFight1)
			{
				opp.initializePunchedSound("fp", 4, "fp-2", 5);
				opp.initializeSwingSound("fsw", 3, "fsw-2", 3);
				opp.initializeStepSound("fi", 4);
				opp.initializeThrowSounds("fg", 1, "ft2", 3);
			}
			if (fightType == FightType.lastFight2)
			{
				opp.initializeStepSound("fl", 4);
				opp.initializePunchedSound("fp", 4, "fp-2", 5);
				opp.initializeSwingSound("fsw", 3, "fsw-2", 3);
				opp.initializeThrowSounds("ft", 3, "ft2", 3);
			}
			if (fightType == FightType.lastFight3)
			{
				opp.initializePunchedSound("fp", 4, "fp-2", 5);
				opp.initializeSwingSound("fsw", 3, "fsw-2", 3);
				opp.initializeStepSound("fo", 2);
				opp.initializeThrowSounds("ft", 3, "ft2", 3);
			}


			List<Furniture> furnishing = new List<Furniture>();
			if (fightType == FightType.betrayal)
			{
				furnishing.Add(new Wall(10, 10, 1000, 10, 0));
				furnishing.Add(new Wall(1, 10, 1000, 10, 0));
				furnishing.Add(new Wall(10, 1, 1000, 0, 10));
				furnishing.Add(new Wall(10, 20, 1000, 0, 10));
				furnishing.Add(new Shelf(3, 19, 1000, 0, 1));
				furnishing.Add(new Shelf(7, 19, 1000, 0, 1));
				furnishing.Add(new Desk(5, 5, 500, 1, 1));
				MapNode.buildMap(furnishing, 10, 20);
				MapNode.loadAStarFromFile("maps\\map1.tdv");
			}
			else if (fightType == FightType.lastFight1)
			{
				furnishing.Add(new SandBar(10, 10, 1000, 10, 0));
				furnishing.Add(new SandBar(1, 10, 1000, 10, 0));
				furnishing.Add(new SandBar(10, 1, 1000, 0, 10));
				furnishing.Add(new SandBar(10, 20, 1000, 0, 10));
				furnishing.Add(new BurningWreckage(5, 5, 1, 1, 1));
				MapNode.buildMap(furnishing, 10, 20);
				MapNode.loadAStarFromFile("maps\\map2.tdv");
			}
			else if (fightType == FightType.lastFight2)
			{
				furnishing.Add(new Wall(10, 10, 1000, 10, 0));
				furnishing.Add(new Wall(1, 10, 1000, 10, 0));
				furnishing.Add(new Wall(10, 1, 1000, 0, 10));
				furnishing.Add(new Wall(10, 20, 1000, 0, 10));
				furnishing.Add(new Shelf(6, 19, 1000, 0, 5));
				MapNode.buildMap(furnishing, 10, 20);
				MapNode.loadAStarFromFile("maps\\map3.tdv");
			}
			else if (fightType == FightType.lastFight3)
			{
				furnishing.Add(new Wall(10, 10, 1000, 10, 0));
				furnishing.Add(new Wall(1, 10, 1000, 10, 0));
				furnishing.Add(new Wall(10, 1, 1000, 0, 10));
				furnishing.Add(new Wall(10, 20, 1000, 0, 10));
				furnishing.Add(new Shelf(3, 19, 1000, 0, 1));
				furnishing.Add(new Shelf(7, 19, 1000, 0, 1));
				furnishing.Add(new Desk(5, 5, 500, 1, 1));
				MapNode.buildMap(furnishing, 10, 20);
				MapNode.loadAStarFromFile("maps\\map4.tdv");
			}

			DSound.setOrientation(0.0, 0.0, 1.0, 0.0, 1.0, 0.0);
			while (!won && !lost)
			{
				foreach (Person person in people)
				{
					person.things.Clear();
					foreach (Person p in people)
						person.things.Add(p);
					foreach (Furniture f in furnishing)
						person.things.Add(f);
				}
				foreach (Person p in people)
					p.move();
				lost = player.damage <= 0;
				if (!lost)
					won = opp.damage <= 0;
				if (DXInput.isFirstPress(Key.H, false))
					SelfVoice.NLS("#" + player.getHealthPercentage() + "&p.wav", true, true);
				if (DXInput.isFirstPress(Key.T, false))
					SelfVoice.NLS("#" + opp.getHealthPercentage() + "&p.wav", true, true);
				Thread.Sleep(5);
			} //while
			Common.fadeMusic();
			player.cleanUp();
			opp.cleanUp();
			foreach (Furniture f in furnishing)
				f.cleanUp();

			if (fightType == FightType.betrayal)
			{
				if (won)
				{
					Common.playUntilKeyPress(DSound.SoundPath + "\\o5-1.ogg", 5, true);
					return true;
				}
				else
				{
					Common.playUntilKeyPress(DSound.SoundPath + "\\o5-2.ogg", 5, true);
					return false;
				}
			}

			if (fightType >= FightType.lastFight1 && fightType <= FightType.lastFight3)
			{
				if (lost)
				{
					Common.playUntilKeyPress(DSound.SoundPath + "\\vl1.ogg");
					Common.playUntilKeyPress(DSound.SoundPath + "\\vl2.ogg");
					return false;
				}
				DSound.playAndWait(DSound.SoundPath + "\\fdie" + ((int)fightType) + ".wav");
				if (fightType == FightType.lastFight3)
					return true;
				Common.playUntilKeyPress(DSound.SoundPath + "\\v" + ((int)(++fightType)) + ".ogg", 5, true);
				return startFight(fightType);
			}
			return false;
		}

		/// <summary>
		/// Mutes everything, including weapons.
		/// </summary>
		/// <param name="hardMute">If true, an aggressive mute is performed. If false, just volume is lowered.</param>
		public static void muteAllObjects(bool hardMute)
		{
			holderAt(1).mute(hardMute);
			holderAt(0).mute(hardMute);
		}

		/// <summary>
		/// Unmutes everything, including weapons.
		/// </summary>
		private static void unmuteAllObjects()
		{
			holderAt(0).unmute();
			holderAt(1).unmute();
		}

		/// <summary>
		/// Stops weapons and mutes everything.
		/// </summary>
		/// <param name="hardMute">If true, sounds are stopped. If false, sounds are just lowered in volume.</param>
		public static void stopAndMute(bool waitUntilHalted, bool hardMute)
		{
			stopWeapons(waitUntilHalted);
			muteAllObjects(hardMute);
		}

		/// <summary>
		/// Stops weapons and mutes everything. This method implements a non-aggressive mute.
		/// </summary>
		/// <param name="waitUntilHalted">If true, method blocks until weapons are stopped; if false, method will not block, but halt command will be sent and method will return immediately.</param>
		public static void stopAndMute(bool waitUntilHalted)
		{
			stopAndMute(waitUntilHalted, false);
		}

		public static void resumeAndUnmute()
		{
			unmuteAllObjects();
			resumeWeapons();
		}

	} //class
}
