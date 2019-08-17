/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using BPCSharedComponent.ExtendedAudio;

namespace TDV
{
	public class Options
	{
		public enum VoiceModes: byte
		{
			none,
			selfVoice,
			screenReader
		}
		public enum Device : byte
		{
			keyboard,
			gameController
		}
		public enum difficulties : byte
		{
			easy = 1,
			hard = 2
		}
		public enum Modes : byte
		{
			none = 0,
			racing,
			deathMatch,
			training,
			mission,
			autoPlay,
			multiplayer,
			freeForAll = 8,
			oneOnOne,
			teamDeath,
			testing
		}
		public static VoiceModes menuVoiceMode
		{
			get;
			set;
		}
		public static VoiceModes statusVoiceMode
		{
			get;
			set;
		}
		public static int hour
		{
			get;
			set;
		}
		public static int day
		{
			get;
			set;
		}
		public static int year
		{
			get;
			set;
		}

		// We need to make these explicit declarations so we can initialize their values. Otherwise,
		// writing to the options file for the first time will throw an error because these values will be null.
		private static bool m_playRIO = true;
		private static String m_ipOrDomain = "";
		private static String m_callSign = "";
		public static bool playRIO
		{
			get { return m_playRIO; }
			set { m_playRIO = value; }
		}
		public static String ipOrDomain
		{
			get { return m_ipOrDomain; }
			set { m_ipOrDomain = value; }
		}
		public static String callSign
		{
			get { return m_callSign; }
			set { m_callSign = value; }
		}
		private static bool m_initializingLoad;
		private static bool m_loadedFromMainMenu;

		public static bool loadedFromMainMenu
		{
			get { return Options.m_loadedFromMainMenu; }
			set { Options.m_loadedFromMainMenu = value; }
		}

		public static bool initializingLoad
		{
			get { return Options.m_initializingLoad; }
			set { Options.m_initializingLoad = value; }
		}

		private static int m_entryMode;

		public static int entryMode
		{
			get { return m_entryMode; }
			set { m_entryMode = value; }
		}
		private static Projector.TeamColors m_team;
		public static Projector.TeamColors team
		{
			get { return m_team; }
			set { m_team = value; }
		}
		public static bool autoPlay;
		private static int m_launchCount;
		public static int launchCount
		{
			get { return m_launchCount; }
			set { m_launchCount = value; }
		}

		private static Device m_enabled = Device.keyboard;
		private static bool m_isPlayingOnline;
		private static bool m_announceVerticalRange = true;
		private static bool m_announceCourseChange = true;
		private static bool m_RPAutoTrigger = false;
		private static bool m_requestedShutdown;
		private static bool m_abortGame;
		private static bool m_serverEndedGame;
		private static byte m_laps = 3;
		private static Modes m_mode = Modes.none;
		////current track to be racing
		private static string m_track;
		private static bool m_isPaused;
		private static difficulties m_d;
		private static bool m_isMM1 = true;
		private static bool m_isMM2 = true;
		private static bool m_isMM3 = true;
		private static bool m_isMM4 = true;
		private static int m_verticalRangeAnnounceTime = 10000;
		private static bool m_demoExpired;
		private static bool m_isLoading;
		private static bool m_mutingThroughExternalThread;
		private static bool m_isDemo = true;

		public static bool isDemo
		{
			get { return Options.m_isDemo; }
			set { Options.m_isDemo = value; }
		}

		public static bool preorder
		{
			get;
			set;
		}

		public static bool RPAutoTrigger
		{
			get { return m_RPAutoTrigger; }
			set { m_RPAutoTrigger = value; }
		}

		public static bool mutingThroughExternalThread
		{
			get { return Options.m_mutingThroughExternalThread; }
			set { Options.m_mutingThroughExternalThread = value; }
		}

		public static Device enabled
		{
			get { return m_enabled; }
			set { m_enabled = value; }
		}

		public static bool isLoading
		{
			get { return (m_isLoading); }
			set { m_isLoading = value; }
		}


		public static bool isPlayingOnline
		{
			get { return (m_isPlayingOnline); }
			set { m_isPlayingOnline = value; }
		}

		public static bool demoExpired
		{
			get { return (m_demoExpired); }
			set { m_demoExpired = value; }
		}

		public static bool abortGame
		{
			get { return (m_abortGame); }
			set { m_abortGame = value; }
		}

		public static bool serverEndedGame
		{
			get { return m_serverEndedGame; }
			set { m_serverEndedGame = value; }
		}

		public static int verticalRangeAnnounceTime
		{
			get { return (m_verticalRangeAnnounceTime); }
			set { m_verticalRangeAnnounceTime = value; }
		}

		public static bool announceVerticalRange
		{
			get { return (m_announceVerticalRange); }
			set { m_announceVerticalRange = value; }
		}

		public static bool announceCourseChange
		{
			get { return (m_announceCourseChange); }
			set { m_announceCourseChange = value; }
		}

		////is game shutting down?
		public static bool requestedShutdown
		{
			get { return (m_requestedShutdown); }
			set { m_requestedShutdown = value; }
		}
		////first racing mode flag
		public static bool isMM1
		{
			get { return (m_isMM1); }
			set { m_isMM1 = value; }
		}
		////first death match mode flag
		public static bool isMM2
		{
			get { return (m_isMM2); }
			set { m_isMM2 = value; }
		}
		////first mission mode flag
		public static bool isMM3
		{
			get { return (m_isMM3); }
			set { m_isMM3 = value; }
		}
		//First startup sound flag
		public static bool isMM4
		{
			get { return m_isMM4; }
			set { m_isMM4 = value; }
		}

		public static bool isPaused
		{
			//syncLock m_isPaused
			// end syncLock
			get { return (m_isPaused); }
			//syncLock m_isPaused
			// end syncLock
			set { m_isPaused = value; }
		}
		public static difficulties difficulty
		{
			get { return (m_d); }
			set { m_d = value; }
		}
		public static byte laps
		{
			get { return (m_laps); }
		}
		public static Modes mode
		{
			get { return (m_mode); }
			set { m_mode = value; }
		}

		public static string currentTrack
		{
			get { return (Common.trackDirectory + "\\" + m_track); }
			set { m_track = value; }
		}
		public static void setDifficulty(difficulties o)
		{
			difficulty = o;
		}
		public static difficulties getDifficulty()
		{
			return (difficulty);
		}
		public static void setMode(Modes m)
		{
			mode = m;
		}
		public static Modes getMode()
		{
			return (mode);
		}

		public static void writeToFile()
		{
			BinaryWriter s = new BinaryWriter(new FileStream(Addendums.File.appPath + "\\settings.tdv", FileMode.Create));
			s.Write(Common.musicVolume);
			s.Write(Common.cutSceneVolume);
			s.Write(announceCourseChange);
			s.Write(RPAutoTrigger);
			s.Write(verticalRangeAnnounceTime);
			s.Write((byte)enabled);
			s.Write(launchCount);
			s.Write(Client.port);
			s.Write((int)SapiSpeech.source);
			s.Write(hour);
			s.Write(day);
			s.Write(year);
			s.Write(ipOrDomain);
			s.Write(callSign);
			s.Write(playRIO);
			s.Write((byte)menuVoiceMode);
			s.Write((byte)statusVoiceMode);
			s.Flush();
			s.Close();
		}
		public static void readFromFile()
		{
			if (!File.Exists(Addendums.File.appPath + "\\settings.tdv"))
				return;

			BinaryReader s = null;
			try
			{
				s = new BinaryReader(new FileStream(Addendums.File.appPath + "\\settings.tdv", FileMode.Open));
				//dump file data into dummy vars so if there's an error, the game options won't contain strange data
				//caused by the stream erroring out unexpectedly.
				//if player is running outdated config, don't error out, just ignore the rest
				float musicVol = s.ReadSingle();
				Common.musicVolume = musicVol;
				musicVol = s.ReadSingle();
				Common.cutSceneVolume = musicVol;
				bool announceCourse = s.ReadBoolean();
				bool rp = s.ReadBoolean();
				int announceV = s.ReadInt32();
				byte en = s.ReadByte();
				int launchC = s.ReadInt32();
				int port = s.ReadInt32();
				int speechSource = s.ReadInt32();
				int ho = s.ReadInt32();
				int da = s.ReadInt32();
				int ye = s.ReadInt32();
				String ip = s.ReadString();
				String callSign = s.ReadString();
				announceCourseChange = announceCourse;
				RPAutoTrigger = rp;
				verticalRangeAnnounceTime = announceV;
				enabled = (Device)en;
				launchCount = launchC;
				Client.port = port;
				if (speechSource > 2)
					speechSource = (int)SapiSpeech.SpeechSource.auto;
				SapiSpeech.setSource((SapiSpeech.SpeechSource)speechSource);
				hour = ho;
				day = da;
				year = ye;
				Options.ipOrDomain = ip;
				Options.callSign = callSign;
				bool pr = s.ReadBoolean();
				playRIO = pr;
				byte sv = s.ReadByte();
				menuVoiceMode = (VoiceModes)sv;
				sv = s.ReadByte();
				statusVoiceMode = (VoiceModes)sv;
			}
			catch (Exception e)
			{
				//Ignore the error
			}
			finally
			{
				s.Close();
			}
		}

	}
}
