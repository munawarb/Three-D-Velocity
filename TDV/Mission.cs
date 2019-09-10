/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using System.Windows.Forms;
using BPCSharedComponent.ExtendedAudio;
using System.Threading;

namespace TDV
{
	public class Mission
	{
		public enum Stage
		{
			takingOff,
			missileHit,
			aboveIsland,
			trainingGrounds,
			airbase,
			discovery,
			powerPlant,
			chopperFight,
			juliusRadioIntercept,
			radarTowers,
			juliusBattle,
			gameEnd
		}
		public static Interaction.FightType fightType;
		private static int nextFighterNumber; // The 0-based offset of the next fighter, used to mitigate duplicate fighter names on the map.
		public static bool trainingFinished
		{
			get;
			set;
		}

		private static bool m_hasSentAircraft;

		public static bool hasSentAircraft
		{
			get { return Mission.m_hasSentAircraft; }
			set { Mission.m_hasSentAircraft = value; }
		}
		private static bool m_hasAnnouncedFighters;

		public static bool hasAnnouncedFighters
		{
			get { return Mission.m_hasAnnouncedFighters; }
			set { Mission.m_hasAnnouncedFighters = value; }
		}
		private static int m_attackCounter;

		public static int attackCounter
		{
			get { return Mission.m_attackCounter; }
			set { Mission.m_attackCounter = value; }
		}
		private static MissionObjectBase m_player;
		private static bool m_isJuliusFight;
		private static byte m_juliusDieCount;
		public static MissionObjectBase island;
		public static MissionObjectBase airbase;
		public static MidAirRefueler refueler;
		public static AircraftCarrier carrier;
		public static JuliusAircraft darkBlaze;
		private static int m_radarCount;
		private static int m_missileCount;
		private static bool m_isDestroyingRadar;
		private static int m_chopperCount;
		private static bool m_isMission;
		private static byte m_racingScore;
		private static byte m_racesComplete;
		private static byte m_deathMatchScore;
		private static byte m_deathMatchesComplete;
		private static Stage m_missionNumber;
		public static int locks;
		private static bool m_isMissileAttack;
		private static bool m_isSwarm;
		private static byte m_pointsWorth;
		private static LandingBeacon m_landingBeacon;
		private static int m_refuelCount;

		public static int refuelCount
		{
			get { return m_refuelCount; }
			set { m_refuelCount = value; }
		}

		public static byte maxJuliusDieCount
		{
			get { return (3); }
		}

		public static bool isJuliusFight
		{
			get { return (m_isJuliusFight); }
			set { m_isJuliusFight = value; }
		}
		public static byte juliusDieCount
		{
			get { return (m_juliusDieCount); }
			set { m_juliusDieCount = value; }
		}
		public static LandingBeacon landingBeacon
		{
			get { return (m_landingBeacon); }
			set { m_landingBeacon = value; }
		}

		public static byte pointsWorth
		{
			get { return (m_pointsWorth); }
			set { m_pointsWorth = value; }
		}

		public static int radarCount
		{
			get { return (m_radarCount); }
			set { m_radarCount = value; }
		}

		public static int missileCount
		{
			get { return (m_missileCount); }
			set { m_missileCount = value; }
		}

		public static int chopperCount
		{
			get { return (m_chopperCount); }
			set { m_chopperCount = value; }
		}
		public static bool isSwarm
		{
			get { return (m_isSwarm); }
			set { m_isSwarm = value; }
		}

		public static bool isDestroyingRadar
		{
			get { return (m_isDestroyingRadar); }
			set { m_isDestroyingRadar = value; }
		}

		public static bool isMissileAttack
		{
			get { return (m_isMissileAttack); }
			set { m_isMissileAttack = value; }
		}

		public static bool isMission
		{
			get { return (m_isMission); }
			set { m_isMission = value; }
		}
		////number of points gained for acing mode.
		public static byte racingScore
		{
			get { return (m_racingScore); }
			set { m_racingScore = value; }
		}

		////number of races already completed.
		public static byte racesComplete
		{
			get { return (m_racesComplete); }
			set { m_racesComplete = value; }
		}

		////score for death match mode.
		public static byte deathMatchScore
		{
			get { return (m_deathMatchScore); }
			set { m_deathMatchScore = value; }
		}

		////Number of death matches already completed.
		public static byte deathMatchesComplete
		{
			get { return (m_deathMatchesComplete); }
			set { m_deathMatchesComplete = value; }
		}

		////Mission area number.
		public static Stage missionNumber
		{
			get { return (m_missionNumber); }
			set { m_missionNumber = value; }
		}

		public static byte passingRacingScore
		{
			get { return (20); }
		}
		public static byte passingDeathMatchScore
		{
			get { return (20); }
		}
		public static string missionFile
		{
			get { return (Addendums.File.appPath + "\\mission.tdv"); }
		}
		public static MissionObjectBase player
		{
			get { return (m_player); }
			set { m_player = value; }
		}
		public static Aircraft trainer
		{
			get;
			set;
		}
		public static void readFromFile()
		{
			if (File.Exists(missionFile))
			{
				BinaryReader s = new BinaryReader(
								new FileStream(missionFile, FileMode.Open, FileAccess.Read));
				racingScore = s.ReadByte();
				racesComplete = s.ReadByte();
				deathMatchScore = s.ReadByte();
				deathMatchesComplete = s.ReadByte();
			} //if file exists
		}

		public static void writeToFile()
		{
			BinaryWriter s = new BinaryWriter(new FileStream(missionFile, FileMode.Create, FileAccess.Write));
			/*
		 s.Write((byte)50);
		 s.Write((byte)50);
		 s.Write((byte)50);
		 s.Write((byte)50);
		 s.Close();
		 if (1 == 1)
			 return;
			 */
			s.Write(racingScore);
			s.Write(racesComplete);
			s.Write(deathMatchScore);
			s.Write(deathMatchesComplete);
			s.Close();
		}
		private static Options.Modes getCurrentMode()
		{
			if (Options.isDemo || Options.loadedFromMainMenu)
				return Options.Modes.mission;
			if (racingScore < passingRacingScore)
			{
				return (Options.Modes.racing);
			}
			if (deathMatchScore < passingDeathMatchScore)
			{
				return (Options.Modes.deathMatch);
			}
			return (Options.Modes.mission);
		}

		public static void enterMissionMode()
		{
			pointsWorth = 0; //reset the point value
			if (!Options.isDemo)
				readFromFile();
			racingScore = deathMatchScore = 200;
			bool rComplete = (Options.loadedFromMainMenu) ? true : (racingScore >= passingRacingScore);
			bool dComplete = (Options.loadedFromMainMenu) ? true : (deathMatchScore >= passingDeathMatchScore);
			if (!Options.loadedFromMainMenu)
				Common.playUntilKeyPress(DSound.SoundPath + "\\mi1.ogg", 0);
			if (!dComplete)
				Common.playUntilKeyPress(DSound.SoundPath + "\\ri.ogg", 0);
			if (!Options.loadedFromMainMenu && dComplete)
				Common.playUntilKeyPress(DSound.SoundPath + "\\mi2.ogg", 0);

			Options.mode = getCurrentMode();
			if (Options.loadedFromMainMenu)
				return;
			if (Options.mode == Options.Modes.mission)
			{
				Common.mainGUI.startWaitCursor();
				Holder h = Interaction.holderAt(0);
				Instructions i = null;

				MissionObjectBase MOB = null;
				int j = 0;
				refueler = new MidAirRefueler(200f, 200f);
				carrier = new AircraftCarrier(200f, 200f);
				h.add(refueler);
				h.add(carrier);
				for (j = 40; j <= 50; j += 5)
				{
					MOB = new SAM(40f, j);
					h.add(MOB);
				}
				MOB = new SAM(100f, 110f);
				h.add(MOB);

				MOB = new SAM(100f, 115f);
				h.add(MOB);

				MOB = new SAM(110f, 120f);
				h.add(MOB);

				MOB = new SAM(120f, 115f);
				h.add(MOB);

				for (j = 170; j <= 190; j += 10)
				{
					int k = 0;
					for (k = 160; k <= 180; k += 10)
					{
						MOB = new SAM(j, k);
						h.add(MOB);
					}
				}


				i = new Instructions();
				i.addNode(true, 20, 20);
				i.addNode(true, 20, 0);
				i.addNode(true, 0, 20);
				i.addNode(true, 15, 15);
				MOB = new BattleShip(20f, 20f, i);
				h.add(MOB);
				MOB = new Island(110f, 110f);
				h.add(MOB);
				island = MOB;

				h.add(new GuardTower(110, 110));
				////southwest corner
				h.add(new GuardTower(160, 110));
				////south edge middle
				h.add(new GuardTower(200, 110));
				h.add(new GuardTower(110, 210));
				h.add(new GuardTower(160, 210));
				h.add(new GuardTower(210, 210));
				h.add(new GuardTower(110, 310));
				h.add(new GuardTower(160, 310));
				h.add(new GuardTower(210, 310));

				MOB = new TrainingCamp(120, 190);
				//west coast of island, sector m19
				MOB.setAttributes(true, false);
				h.add(MOB);
				//Set up defenses around training grounds
				h.add(new GuardTower(120, 188));
				h.add(new GuardTower(120, 192));
				h.add(new GuardTower(122, 190));
				h.add(new GuardTower(118, 190));

				//set up defenses:
				//to left and right we will have sams,
				//while to northwest and southeast corners will be guard towers.
				h.add(new SAM(113, 210));
				h.add(new SAM(111, 210));
				h.add(new GuardTower(111, 209));
				h.add(new GuardTower(113, 212));

				h.add(new GuardTower(302, 190));
				h.add(new GuardTower(306, 190));
				h.add(new GuardTower(310, 190));
				h.add(new GuardTower(308, 192));
				i = new Instructions();
				i.addNode(true, 306, 190);
				i.addNode(true, 310, 192);
				i.addNode(true, 308, 188);
				h.add(new Tank(308, 190, i));
				i = new Instructions();
				i.addNode(true, 206, 210);
				i.addNode(true, 206, 212);
				i.addNode(true, 214, 210);
				i.addNode(true, 211, 208);
				h.add(new Tank(211, 210, i));

				h.add(new SAM(210, 275));
				h.add(new SAM(210, 285));
				i = new Instructions();
				i.addNode(true, 215, 281);
				i.addNode(true, 208, 279);
				h.add(new Tank(210, 280, i));
				i = new Instructions();
				i.addNode(true, 205, 290);
				i.addNode(true, 215, 290);
				i.addNode(true, 205, 270);
				i.addNode(true, 215, 270);
				h.add(new Tank(210, 280, i));

				//h.add(new RadarTower(208, 278, true));
				//h.add(new RadarTower(208, 282, true));
				Common.mainGUI.stopWaitCursor();
			} //if mission
		}

		//if a player's score isn't high enough, call to this method will cause them to start over.
		public static void reset(Options.Modes mode)
		{
			pointsWorth = 0;
			if (mode == Options.Modes.racing)
			{
				racesComplete = 0;
				racingScore = 0;
			}
			if (mode == Options.Modes.deathMatch)
			{
				deathMatchesComplete = 0;
				deathMatchScore = 0;
			}
			if (mode == Options.Modes.mission)
			{
				missionNumber = 0;
				isSwarm = false;
				chopperCount = 0;
				isMissileAttack = false;
				radarCount = 0;
				locks = 0;
				juliusDieCount = 0;
				isJuliusFight = false;
				isDestroyingRadar = false;
				refuelCount = 0;
				hasAnnouncedFighters = false;
				attackCounter = 0;
			}
		}

		public static void save(BinaryWriter w)
		{
			w.Write((int)missionNumber);
			w.Write(isSwarm);
			w.Write(chopperCount);
			w.Write(isMissileAttack);
			w.Write(missileCount);
			w.Write(isDestroyingRadar);
			w.Write(radarCount);
			w.Write(isJuliusFight);
			w.Write(juliusDieCount);
			w.Write(hasSentAircraft);
			w.Write(refuelCount);
			w.Write(hasAnnouncedFighters);
			w.Write(attackCounter);
		}

		public static void load()
		{
			BinaryReader r = Common.inFile;
			missionNumber = (Stage)r.ReadInt32();
			isSwarm = r.ReadBoolean();
			chopperCount = r.ReadInt32();
			isMissileAttack = r.ReadBoolean();
			missileCount = r.ReadInt32();
			isDestroyingRadar = r.ReadBoolean();
			radarCount = r.ReadInt32();
			isJuliusFight = r.ReadBoolean();
			juliusDieCount = r.ReadByte();
			//Two entries below don't exist in versions
			//below 1.1
			if (Common.version >= 1.1f)
			{
				hasSentAircraft = r.ReadBoolean();
				Mission.refuelCount = r.ReadInt32();
			}
			//Two options below won't even exist if this is
			//a previous version
			if (Common.version >= 1.2f)
			{
				hasAnnouncedFighters = r.ReadBoolean();
				attackCounter = r.ReadInt32();
			}
		}

		public static void createNewJuliusAircraft(float x, float y)
		{
			createNewObject("db"); //will assign reference to Mission.darkBlaze
			darkBlaze.x = x; darkBlaze.y = y;
			Interaction.holderAt(0).add(darkBlaze);
		}

		private static int getNextFighterNumber()
		{
			int r = nextFighterNumber+1; // scale from 1 to 6.
			nextFighterNumber = (nextFighterNumber + 1) % 6;
			return r;
		}

		public static Aircraft createNewFighter(int type, int radius)
		{
			Aircraft ac = (Aircraft)createNewObject($"f{getNextFighterNumber()}");
			switch (type)
			{
				case 1:
					ac.x = Mission.player.x; ac.y = Mission.player.y - radius;
					break;
				case 2:
					ac.x = Mission.player.x; ac.y = Mission.player.y + radius;
					break;
				case 3:
					ac.x = Mission.player.x + radius; ac.y = Mission.player.y;
					break;
				case 4:
					ac.x = Mission.player.x - radius; ac.y = Mission.player.y;
					break;
				case 5:
					ac.x = Mission.player.x - 2f; ac.y = Mission.player.y + 2f;
					break;
				case 6:
					ac.x = Mission.player.x + 10f; ac.y = Mission.player.y - 10f;
					break;
			}
			Interaction.holderAt(0).add(ac);
			return ac;
		}

		public static Aircraft createNewFighter(float x, float y)
		{
			Aircraft ac = (Aircraft)createNewObject($"f{getNextFighterNumber()}");
			ac.x = x;
			ac.y = y;
			Interaction.holderAt(0).add(ac);
			return ac;
		}

		public static Aircraft createNewTrainer(Aircraft ac)
		{
			Interaction.holderAt(0).add(ac);
			return ac;
		}

		public static Chopper createNewChopper()
		{
			Chopper chopper = (Chopper)createNewObject("c");
			Interaction.holderAt(0).add(chopper);
			return chopper;
		}

		public static MissionObjectBase createNewObject(String name)
		{
			if (name.Equals("ab"))
				return new AirBase();
			else if (name.Equals("ac"))
				return carrier = new AircraftCarrier();
			else if (name.Equals("bs"))
				return new BattleShip();
			else if (name.Equals("b"))
				return new Bridge();
			else if (name.Equals("c"))
				return new Chopper();
			else if (name.StartsWith("f") && name.Length == 2) // f1 to fn (single digit)
				return new Aircraft(name);
			else if (name.Equals("gt"))
				return new GuardTower();
			else if (name.Equals("i"))
				return island = new Island();
			else if (name.Equals("db"))
				return darkBlaze = new JuliusAircraft();
			else if (name.Equals("r"))
				return refueler = new MidAirRefueler();
			else if (name.Equals("pp"))
				return new PowerPlant();
			else if (name.Equals("rs"))
				return new RadarTower();
			else if (name.Equals("sb"))
				return new SAM();
			else if (name.Equals("t"))
				return new Tank();
			else if (name.Equals("tg"))
				return new TrainingCamp();
			else if (name.Equals("o"))
				return player = new Aircraft(false); //create player
			else if (name.Equals("lb"))
				return landingBeacon = new LandingBeacon();
			throw new ArgumentException($"The string {name} is not a valid object name.");
		}

		#region ObjectGeneration
		public static void setUpAirbase()
		{
			Interaction.holderAt(0).add(new AirBase(112, 210)); //due north from trainig grounds
		}

		public static void setUpPowerPlant()
		{
			Interaction.holderAt(0).add(new PowerPlant(308, 190)); //across training grounds, sector ZE18
		}

		public static void setUpBridge()
		{
			Interaction.holderAt(0).add(new Bridge(210, 280)); //north of center of island, sector V28
		}

		public static void setUpRadarStations()
		{
			RadarTower MOB = new RadarTower(208, 210, false); //left of facility sector U21
			MOB.setAttributes(true, false);
			Interaction.holderAt(0).add(MOB);
			RadarTower MOB2 = new RadarTower(212, 210, false); //Right
			MOB2.setAttributes(true, false);
			Interaction.holderAt(0).add(MOB2);
		}
		#endregion

	}
}
