/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.Input;
using SharpDX.DirectInput;
using SharpDX.XAudio2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TDV
{

	public class Common
	{
		public static String applicationVersion = Assembly.GetExecutingAssembly().GetName().Version.Major + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor;
		/// <summary>
		/// The time between frame updates, measured in seconds.
		/// </summary>
		public const float interval = 0.1f;
		/// <summary>
		/// The time between frame updates, expressed in milliseconds.
		/// </summary>
		public const int intervalMS = (int)(interval * 1000);
		public const string trackDirectory = "tracks";
		/// <summary>
		/// The length of one nautical mile.
		/// </summary>
		public const float KNOT = 0.868976242f;
		//Measurement of one mach.
		public const float mach = 761.2f;
		/// <summary>
		/// The length of a sector in miles. 
		/// </summary>
		public const float sectorLength = 0.1f;
		private static Random random = new Random();
		private static Dictionary<String, String> friendlyNames = new Dictionary<String, String>()
		{
			{"r1", "Racer 1" },
			{"r2", "Racer 2" },
			{"r3", "Racer 3" },
			{"r4", "Racer 4" },
			{"r5", "Racer 5" },
			{"r6", "Racer 6" },
			{"f1", "Fighter 1" },
			{"f2", "Fighter 2" },
			{"f3", "Fighter 3" },
			{"f4", "Fighter 4" },
			{"f5", "Fighter 5" },
			{"f6", "Fighter 6" },
			{"ab", "Airbase" },
			{"ac", "Aircraft Carrier" },
			{"bs", "Battleship" },
			{"b", "Bridge" },
			{ "c", "Chopper" },
			{"gt", "Guard Tower" },
			{"i", "Island" },
			{"db", "Dark Blaze" },
			{"lb", "Landing Beacon" },
			{"r", "Refueler" },
			{"pp", "Powerplant" },
			{"rs", "Radar Station" },
			{"sb", "SAM Battery" },
			{"t", "Tank" },
			{"tg", "Training Grounds" }
		};
		public static float volumeIncrementValue { get { return 0.25f; } }
		public static float volumeFadeValue{get{return 0.05f; } }
		public static bool error
		{
			get;
			set;
		}
		private static int nextErr = 0;
		private static bool loadedReg;
		public static bool ACBMode = false;
		public static bool failedConnect;
		public static ExtraItem[] musicExtraItem;
		private static ExtraItem[] serverExtraItem;
		public static IntPtr guiHandle;
		public static String input;
		public static String cmdLine;
		public interface Hittable
		{
			Interaction.Cause cause
			{
				get;
			}
			int maxDamagePoints
			{
				get;
				set;
			}

			//if this value is less than or equal to 0, the object is destroyed
			int damage
			{
				get;
				set;
			}
			////an object that is striking another object will call this method to give the object damage
			void hit(int damage, Interaction.Cause cause);
			bool hit();
			////Returns true if this object is destroyed, false otherwise
		}
		private const float defaultMusicVol = 0.5f;
		public static float musicVolume = defaultMusicVol, cutSceneVolume = 1.0f;
		private static ExtendedAudioBuffer menuWrapSound, menuMoveSound, menuSelectSound;
		private static bool m_previousFileVersion;

		public static bool previousFileVersion
		{
			get { return m_previousFileVersion; }
			set { m_previousFileVersion = value; }
		}
		private static float m_version = 0.0f;

		public static float version
		{
			get { return Common.m_version; }
			set { Common.m_version = value; }
		}
		private static bool m_gameHasFocus;
		public static BinaryReader inFile; //used for loading
		private static Thread sThread;
		public static OggBuffer music;
		public static AutoResetEvent musicNotifier, menuNotifier, onlineMenuNotifier;
		public static bool firstTimeLoad = true;
		public static GUI mainGUI;
		private static bool m_exitMenus;

		public static bool exitMenus
		{
			get { return m_exitMenus; }
			set { m_exitMenus = value; }
		}

		public static bool gameHasFocus
		{
			get { return (m_gameHasFocus); }
			set { m_gameHasFocus = value; }
		}

		//returns a random integer from 0 to  max.
		public static int getRandom(int max)
		{
			////Since next returns a random number from 0 to 1 less than the supplied value,
			////increment supplied value so that it will be from 0 to max.
			return (new Random().Next(max + 1));
		}
		public static int getRandom(int min, int max)
		{
			return random.Next(min, max + 1);
		}




		[STAThread]
		public static void Main(String[] args)
		{
			//System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Addendums.File.appPath + "\\trace.log"));
			/*
			Mission.writeToFile();
			if (1 == 1)
				return;
			 */
			try {
				//SharpDX.Configuration.EnableObjectTracking = true;
				if (args != null && args.Length == 1)

					cmdLine = args[0];
				if (Common.cmdLine != null && Common.cmdLine.Equals("reset")) {
					DialogResult r =
						MessageBox.Show(@"Would you like to remove your mission data? Answering 'Yes' will not remove your saved game; it will only remove your simulation records.

If you do this, you will have to start over from Racing Mode. This process is NOT reversible.",
																							  "Remove Mission Data",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question);
					if (r == DialogResult.Yes)
						File.Delete(Mission.missionFile);

					r =
						MessageBox.Show(@"Would you like to delete your configuration settings? Answering 'Yes' will delete settings set through the 'Options' menu during game play.

Answering 'Yes' will also delete your joystick calibration data if you have your joystick connected.",
																 "Remove Settings",
																 MessageBoxButtons.YesNo,
																 MessageBoxIcon.Question);
					if (r == DialogResult.Yes) {
						File.Delete(Addendums.File.appPath + "\\settings.tdv");
						String[] files = Directory.GetFiles(Addendums.File.appPath);
						foreach (String f in files) {
							if (f.Contains("dev_"))
								File.Delete(f);
						}
					} //if yes

					MessageBox.Show("This program will now exit.",
						"Finished",
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
					Application.Exit();
					return;
				} //if reset
				else {
					if (!Directory.Exists(Addendums.File.appPath))
						Directory.CreateDirectory(Addendums.File.appPath);
					if (!Directory.Exists(Addendums.File.commonAppPath))
						Directory.CreateDirectory(Addendums.File.commonAppPath);
					initializeRegistration();

					musicExtraItem = new ExtraItem[] {
		  new ExtraItem(Aircraft.Action.increaseMusicVolume, new ExtraItemFunction(increaseMusicVolume)),
		  new ExtraItem(Aircraft.Action.decreaseMusicVolume, new ExtraItemFunction(decreaseMusicVolume))};
					serverExtraItem = new ExtraItem[] {
		  new ExtraItem(Aircraft.Action.increaseMusicVolume, new ExtraItemFunction(increaseMusicVolume)),
		  new ExtraItem(Aircraft.Action.decreaseMusicVolume, new ExtraItemFunction(decreaseMusicVolume)),
					new ExtraItem(Aircraft.Action.chat, new ExtraItemFunction(serverChat)),
					new ExtraItem(Aircraft.Action.whoIs, new ExtraItemFunction(Client.whoIs)),
					new ExtraItem(Aircraft.Action.admin, new ExtraItemFunction(Client.adminMenu)),
					new ExtraItem(Aircraft.Action.prevMessage, new ExtraItemFunction(Client.prevMessage)),
					new ExtraItem(Aircraft.Action.nextMessage, new ExtraItemFunction(Client.nextMessage)),
					new ExtraItem(Aircraft.Action.copyMessage, new ExtraItemFunction(Client.copyMessage))};

					Application.Run(new GUI());
				} //if no command lines
			}
			catch (Exception e) {
				handleError(e);
				System.Environment.Exit(0);
			}
		}

		public static Hittable convertToHittable(Projector obj)
		{
			return ((Hittable)obj);
		}

		////Returns the miles that an object should move on each tick (intervalMS.)
		////Expects the MPH rating of the object (viz. 100 MPH).
		public static float convertToTickDistance(float miles)
		{
			float intervalMS = Common.intervalMS;
			return (convertToTickDistance(miles, intervalMS));
		}
		////Returns the MPH rating for the millisecond interval.
		////For instance, if 100 were passed to this method,
		////It would return the MPH rating in miles per 100 milliseconds.
		////Likewise, passing 1 to this method will return the MPH rating in terms of miles per 1 millisecond.
		public static float convertToTickDistance(float miles, float tick)
		{
			return miles / 60f / 60f / 1000f * tick;
		}

		////Returns the whole number "equivalent" of the float with the specified precision.
		////For instance, if the arguments 5.57, 2 were passed to this method,
		////it would return 557.
		public static int convertToWholeNumber(float number, int precision)
		{
			return ((int)(Math.Round(number, precision) * Math.Pow(10, precision)));
		}

		public static string convertToWordNumber(int number)
		{
			string[] numbers = { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth"
	};
			return (numbers[number - 1]);
		}

		/// <summary>
		/// Generates a menu voiced by SAPI using the specified intro, menu, and key bindings.
		/// </summary>
		/// <param name="keys">Key bindings.</param>
		/// <returns>The index of the chosen option.</returns>
		public static int GenerateMenu(string intro, string[] menu, ExtraItem[] keys)
		{
			return sVGenerateMenu(intro, menu, 0, null, keys, true);
		}

		public static int GenerateMenu(string intro, string[] menu, int index, ExtraItem[] keys)
		{
			return sVGenerateMenu(intro, menu, index, null, keys, true);
		}

		public static int GenerateMenu(string intro, string[] menu, int index)
		{
			return GenerateMenu(intro, menu, index, null);
		}

		/// <summary>
		/// Generates a menu voiced by SAPI using the specified introduction and menu with no key bindings.
		/// </summary>
		/// <param name="intro">The text to use for the introduction.</param>
		/// <param name="menu">An array of menu options.</param>
		/// <returns>The index of the chosen option.</returns>
		public static int GenerateMenu(string intro, string[] menu)
		{
			return GenerateMenu(intro, menu, null);
		}

		/// <summary>
		///  Generates a self-voiced menu using the specified intro, menu options, position index, path, key bindings, and SAPI flag.
		/// </summary>
		/// <param name="intro">A string of a wave file path for the introduction, or a text prompt if this menu is voiced by SAPI.</param>
		/// <param name="menu">An array of strings resembling the menu options.</param>
		/// <param name="menuPos">The index on which the menu should start, zero-based.</param>
		/// <param name="nPath">The prepended path.</param>
		/// <param name="keys">An ExtraItem structure array populated with extra command that can be done inside a menu.</param>
		/// <param name="sapi">True if this menu should be voiced by SAPI, false otherwise.</param>
		/// <returns>The menu index on which ENTER was pressed.</returns>
		public static int sVGenerateMenu(string intro, string[] menu, int menuPos, String nPath, ExtraItem[] keys, bool sapi)
		{
			bool justEntered = true; // Used for screen reader menus so the prompt isn't cut off
			if (menuWrapSound == null) {
				menuWrapSound = DSound.LoadSoundAlwaysLoud(DSound.SoundPath + "\\menumove.wav");
				menuMoveSound = DSound.LoadSoundAlwaysLoud(DSound.SoundPath + "\\mc" + getRandom(1, 2) + ".wav");
				menuSelectSound = DSound.LoadSoundAlwaysLoud(DSound.SoundPath + "\\mc3.wav");
			}
			bool wrap = false;
			while (DXInput.isKeyHeldDown(Key.Return) || DXInput.isKeyHeldDown(Key.Escape) || DXInput.isJSButtonHeldDown(0) || DXInput.isJSButtonHeldDown(1))
				Thread.Sleep(5);

			ExtendedAudioBuffer ISound = null;
			bool HasSaid = false;
			SapiSpeech.purge();
			SelfVoice.purge(true);
			int length = menu.Length;
			int max = length - 1;
			SelfVoice.nStop = false;
			if (!string.IsNullOrEmpty(intro)) {
				if (!sapi) {
					if (intro.IndexOf("\\") >= 0)
						ISound = DSound.LoadSoundAlwaysLoud(intro);
					else
						ISound = DSound.LoadSoundAlwaysLoud(DSound.NSoundPath + "\\" + intro);
					DSound.PlaySound(ISound, true, false);
					while (DSound.isPlaying(ISound)) {
						if (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown() || !DXInput.JSDirectionalPadIsCenter())
							break;
						Thread.Sleep(5);
					}

					DSound.unloadSound(ref ISound);
				} else // if SAPI
					SapiSpeech.speak(intro, SapiSpeech.SpeakFlag.interruptable);
			} //if intro not null

			DXInput.resetJSDP();
			DXInput.resetKeys();
			DXInput.resetJSB();
			//prevents menu from starting on blank option
			while (menu[menuPos].Equals("")) {
				menuPos++;
			}

			while (!DXInput.isFirstPress(Key.Return) && !DXInput.isFirstPressJSB(0)) {
				DXInput.updateJSState();
				if (exitMenus || DXInput.isFirstPress(Key.Escape, false) || DXInput.isFirstPressJSB(1)) {
					SapiSpeech.purge();
					SelfVoice.purge(true);
					return -1;
				}
				if (DXInput.isFirstPress(Key.Up, false) || DXInput.isFirstPress(Key.Left, false) || DXInput.isFirstPressJSDP(DXInput.DirectionalPadPositions.up) || DXInput.isFirstPressJSDP(DXInput.DirectionalPadPositions.left)) {
					justEntered = false;
					menuPos--;
					if (menuPos < 0) {
						menuPos = max;
						wrap = true;
					}

					while (menu[menuPos].Equals("")) {
						if (--menuPos < 0) {
							menuPos = max;
							wrap = true;
						}
					}
					HasSaid = false;
				}

				if (DXInput.isFirstPress(Key.Down, false) || DXInput.isFirstPress(Key.Right, false) || DXInput.isFirstPressJSDP(DXInput.DirectionalPadPositions.down) || DXInput.isFirstPressJSDP(DXInput.DirectionalPadPositions.right)) {
					justEntered = false;
					wrap = (menuPos = (menuPos + 1) % length) == 0;

					while (menu[menuPos].Equals(""))
						wrap = (menuPos = (menuPos + 1) % length) == 0;
					HasSaid = false;
				}

				if (DXInput.isFirstPress(Key.Home, false)) {
					justEntered = false;
					menuPos = 0;
					while (menu[menuPos].Equals(""))
						menuPos++;
					HasSaid = false;
				}

				if (DXInput.isFirstPress(Key.End, false)) {
					justEntered = false;
					menuPos = max;
					while (menu[menuPos].Equals(""))
						menuPos--;

					HasSaid = false;
				}


				if (menuPos < 0)
					menuPos = 0;
				if (menuPos > max)
					menuPos = max;

				if (!HasSaid) {
					SelfVoice.purge(true);
					//Purge resets the alternate path, which is bad in case we're reading each menu item as several strung sound files. See Aircraft.weaponsRadar();
					if (wrap)
						DSound.PlaySound(menuWrapSound, true, false);
					wrap = false;
					DSound.PlaySound(menuMoveSound, true, false);

					if (!sapi) {
						if (nPath != null)
							SelfVoice.setPathTo(nPath);
						if (menu[menuPos].IndexOf("\\") >= 0)
							SelfVoice.NLS(menu[menuPos], true, true);
						else
							SelfVoice.NLS(DSound.NSoundPath + "\\" + menu[menuPos], true, true);
					} else { //if sapi
						if (!justEntered)
							SapiSpeech.purge();
						SapiSpeech.speak(menu[menuPos]);
					}
					justEntered = false;
					HasSaid = true;
				}

				executeExtraCommands(keys);
				Thread.Sleep(30);
			}

			if (sapi)
				SapiSpeech.purge();
			SelfVoice.purge(true);
			DSound.PlaySound(menuSelectSound, true, false);
			return menuPos;
		}

		public static int sVGenerateMenu(string intro, string[] menu, int menuPos, String nPath)
		{
			return sVGenerateMenu(intro, menu, 0, nPath, null, false);
		}


		/// <summary>
		///  Generates a self-voiced menu with the default narratives path.
		/// </summary>
		/// <param name="intro"></param>
		/// <param name="menu"></param>
		/// <param name="menuPos">If the menu should start somewhere else besides the top, indicate that position in this parameter.</param>
		/// <returns>The menu index on which ENTER was pressed.</returns>
		public static int sVGenerateMenu(string intro, string[] menu, int menuPos)
		{
			return sVGenerateMenu(intro, menu, menuPos, null, null, false);
		}

		/// <summary>
		///  Generates a self-voiced menu with the default narratives path.
		/// </summary>
		/// <param name="intro"></param>
		/// <param name="menu"></param>
		/// <param name="menuPos">If the menu should start somewhere else besides the top, indicate that position in this parameter.</param>
		/// <param name="keys">A keys structure array describing extra commands.</param>
		/// <returns>The menu index on which ENTER was pressed.</returns>
		public static int sVGenerateMenu(string intro, string[] menu, int menuPos, ExtraItem[] keys)
		{
			return sVGenerateMenu(intro, menu, menuPos, null, keys, false);
		}

		/// <summary>
		///  Generates a self-voiced menu, and assumes the index should start at the top of the list. Also uses default narratives path.
		/// </summary>
		/// <param name="intro">The sound file for the introduction. If no prompt exists, pass NULL.</param>
		/// <param name="menu">An array of strings naming the sound files for the menus. The narratives directory will be prepended automatically.</param>
		/// <returns>The menu index on which ENTER was pressed.</returns>
		public static int sVGenerateMenu(String intro, String[] menu)
		{
			return sVGenerateMenu(intro, menu, 0);
		}

		/// <summary>
		///  Generates a self-voiced menu, and assumes the index should start at the top of the list. Also uses default narratives path. Accepts extra commands.
		/// </summary>
		/// <param name="intro">The sound file for the introduction. If no prompt exists, pass NULL.</param>
		/// <param name="menu">An array of strings naming the sound files for the menus. The narratives directory will be prepended automatically.</param>
		/// <param name="keys">Extra commands.</param>
		/// <returns>The menu index on which ENTER was pressed.</returns>
		public static int sVGenerateMenu(String intro, String[] menu, ExtraItem[] keys)
		{
			return sVGenerateMenu(intro, menu, 0, keys);
		}

		public static float convertToKNOTS(float miles)
		{
			return (miles * KNOT);
		}

		////Returns a Projector array represented by the supplied arrayList.
		////Note: this method assumes that the supplied arrayList can be casted to Projector objects.
		public static Projector[] convertToProjectorArray(ArrayList objArray)
		{
			Projector[] projectors = new Projector[objArray.ToArray().Length];
			int i = 0;
			for (i = 0; i <= projectors.Length - 1; i++) {
				projectors[i] = (Projector)objArray[i];
			}
			return (projectors);
		}
		public static Aircraft[] convertToVehicleArray(ArrayList objArray)
		{
			Aircraft[] vehicles = new Aircraft[objArray.ToArray().Length];
			Aircraft[] finalArray = null;
			int nullCounter = 0;
			int i = 0;
			for (i = 0; i <= vehicles.Length - 1; i++) {
				if (objArray[i] is Aircraft)
					vehicles[i] = (Aircraft)objArray[i];
				else {
					vehicles[i] = null;
					nullCounter++;
				}
				finalArray = new Aircraft[vehicles.Length - nullCounter];
				int index = 0;
				for (i = 0; i < vehicles.Length; i++) {
					if (vehicles[i] != null) {
						finalArray[index] = vehicles[i];
						index++;
					}
				} //for
			} //lock
			return (finalArray);
		}

		public static float convertToFeet(float miles)
		{
			return miles * 5280f;
		}
		public static float convertToMiles(float feet)
		{
			return (feet / 5280f);
		}
		public static void handleError(Exception e, String msg)
		{
			error = true;
			StreamWriter theFile = File.CreateText(Addendums.File.appPath +
				String.Format("\\error{0}-{1}.log", DateTime.Now.ToString("MMM-d-yyyy h-m-s tt"), nextErr++));

			theFile.Write("Error log, created with build version {0}: {1}", Addendums.File.FileVersion, Environment.NewLine);
			theFile.Write("Error base exception: {0}{1}Error Description: {2}{3}Stack trace: {4}", e.GetBaseException(), Environment.NewLine, e.Message, Environment.NewLine, e.StackTrace);
			if (msg != null)
				theFile.Write(Environment.NewLine + "Extra information: " + msg);
			theFile.Flush();
			theFile.Close();
			SapiSpeech.speak("An error has occurred in the program. An error log has been generated in error.log located in your application data directory. Press enter to terminate the program.", SapiSpeech.SpeakFlag.interruptable);
			MessageBox.Show("An error has occured. A log has been generated.", "TDV.exe - Application Error",
						 MessageBoxButtons.OK, MessageBoxIcon.Error);
			menuNotifier.Set();
			SapiSpeech.enableJAWSHook();
			Environment.Exit(0);
		}
		public static void handleError(Exception e)
		{
			handleError(e, null);
		}
		public static void shutdown()
		{
			if (!Options.requestedShutdown) {
				Options.requestedShutdown = true;
				sThread = new Thread(shutdownThread);
				sThread.Start();
			}
		}

		private static void shutdownThread()
		{
			Interaction.terminateAllProjectors(true);
			SapiSpeech.cleanUp();
			System.Diagnostics.Trace.WriteLine("Ended all projectors.");
			if (music != null) {
				music.stopOgg();
				music = null;
				DSound.unloadSound(ref menuWrapSound);
				DSound.unloadSound(ref menuMoveSound);
				DSound.unloadSound(ref menuSelectSound);
			}
			DSound.cleanUp();
			DXInput.cleanUp();
			onlineMenuNotifier.Set();
			menuNotifier.Set();
			System.Environment.Exit(0);
		}

		/* If playing online, will return to the hangar,
		 * else will return to main menu of game.
		 * */
		public static void repop()
		{
			if (Options.isPlayingOnline) {
				onlineMenuNotifier.Set();
				if (Options.abortGame)
					Client.sendCommand(CSCommon.cmd_disconnectMe);
				else if (!Options.serverEndedGame && !Client.closed) //Don't send a command if the server shut down.
					Client.sendCommand(CSCommon.cmd_deleteFromGame); //if serverEndedGame, client has already been removed from game so no need to send delete command, or player may be removed from server since game removes player anyway.
			} else //if not playing online.
				menuNotifier.Set();
		}

		public static void startGame()
		{
			if (Options.isPlayingOnline) {
				/* We will add the player's craft here by requesting a create, but all subsequent adds will be done by Client.
				 * Once all adds are complete, client will receive a startGame command from the server.
				 * */
				bool connected = false;
				SapiSpeech.speak("Enter IP address or domain to connect to.", SapiSpeech.SpeakFlag.interruptable);
				String ip = Common.mainGUI.receiveInput(Options.ipOrDomain, false).Trim();
				if (ip.Equals("")) {
					menuNotifier.Set();
					return;
				}
				SapiSpeech.speak("Enter your call sign. This is how you'll be known on the server.", SapiSpeech.SpeakFlag.interruptable);
				String callSign = Common.mainGUI.receiveInput(Options.callSign, false).Trim();
				if (callSign.Equals("")) {
					menuNotifier.Set();
					return;
				}
				Options.ipOrDomain = ip;
				Options.callSign = callSign;
				Options.writeToFile();
				SapiSpeech.playOrSpeakMenu(DSound.NSoundPath + "\\c1.wav", "Please wait...");
				connected = Client.connect(ip, callSign, 4444);
				failedConnect = !connected;
				if (!connected) {
					if ((Client.getMessages() & Client.LoginMessages.wrongCredentials) != Client.LoginMessages.wrongCredentials)
						SapiSpeech.playOrSpeakMenu(DSound.NSoundPath + "\\c2.wav", "Could not connect. Check your connection and try again. If you are running a firewall, make sure it allows all connection attempts for Three-D Velocity.");
				} else { //connected
					fadeMusic();
					buildOnlineMenu();
				} //if connected
				if (Options.requestedShutdown)
					return;
				//If got down here, wants to exit menu or the connection failed
				if (connected) {
					Client.closeConnection();
					fadeMusic(); //hangar music
					SapiSpeech.playOrSpeakMenu(DSound.NSoundPath + "\\c4.wav", "You have left the hangar");
				}
				menuNotifier.Set();
				return;
			} //if playing online

			if (Mission.isMission)
				Mission.enterMissionMode();
			//Initialize mission mode in case we need to start some music for a mode
			startMusic();
			System.Diagnostics.Trace.WriteLine("started music");
			Track t = null;
			int i = 0;
			if (!Mission.isMission) {
				t = new Track(Options.currentTrack);
				Holder h = Interaction.holderAt(0);
				for (i = 1; i <= ((Options.mode == Options.Modes.testing || Options.mode == Options.Modes.training) ? 1 : getRandom(5, (Options.autoPlay) ? 7 : 5)); i++) {
					string name = null;
					if (i == 1)
						name = "o";
					else
						name = ((Options.mode == Options.Modes.deathMatch) ? "f" : "r") + (i - 1);
					Aircraft v = new Aircraft(0, 1500, name, (Options.autoPlay) ? true : (i > 1), t);
					if (Common.ACBMode && !Options.autoPlay)
						v.startAtHeight(15000);
					h.add(v);
					if (name == "o")
						Mission.player = (MissionObjectBase)Interaction.objectAt(v.id);
				}
			} else if (!Options.loadedFromMainMenu) { //if mission mode
				//If loading mission from main menu, all objects already exist. This code will create duplicate players otherwise.
				if (Options.mode == Options.Modes.racing) {
					SelfVoice.nStop = false;
					SelfVoice.NLS(DSound.NSoundPath + "\\race.wav&#" + (Mission.racesComplete + 1));
					SelfVoice.nStop = true;
					Common.mainGUI.selectTrack();
					t = new Track(Options.currentTrack);
					Holder h = Interaction.holderAt(0);
					for (i = 1; i <= Common.getRandom(2, 7); i++) {
						string name = null;
						if (i == 1)
							name = "o";
						else {
							name = "r" + (i - 1);
							Mission.pointsWorth++;
						}
						Projector a = null;
						h.add(a = (Projector)new Aircraft(0, 1500, name, i > 1, t));
						if (name == "o")
							Mission.player = (MissionObjectBase)Interaction.objectAt(a.id);
					} //for random projector count
				} //if mode=racing

				if (Options.mode == Options.Modes.deathMatch) {
					SelfVoice.nStop = false;
					SelfVoice.NLS(DSound.NSoundPath + "\\deathmatch.wav&#" + (Mission.deathMatchesComplete + 1));
					SelfVoice.nStop = true;
					t = new Track(Options.currentTrack);
					Holder h = Interaction.holderAt(0);
					for (i = 1; i <= Common.getRandom(2, 7); i++) {
						string name = null;
						if (i == 1)
							name = "o";
						else {
							name = "f" + (i - 1);
							Mission.pointsWorth++;
						}
						MissionObjectBase player = null;
						h.add(player = new Aircraft(0, 1500, name, i > 1, t));
						if (name == "o")
							Mission.player = (MissionObjectBase)Interaction.objectAt(player.id);
					}
				} //if deathmatch
				if (Options.mode == Options.Modes.mission)
					Interaction.holderAt(0).add(Mission.player = new Aircraft(0, 1500, "o", false, new Track(Options.currentTrack)));
			} //if mission

			if (!Options.loadedFromMainMenu)
				Interaction.holderAt(0).add(Mission.landingBeacon = new LandingBeacon());
			Options.loadedFromMainMenu = false;
			//all projectors are registered and ready to go, so start them.
			Interaction.startAllThreads();
		}

		/// <summary>
		///  Game loop for checking for key presses in online mode.
		/// </summary>
		private static void buildOnlineMenu()
		{
			String menuIntro = Common.returnSvOrSr(() => "c3.wav", () => "You're now in the hangar.", Options.menuVoiceMode);
			String[] topLevelMenu = Common.returnSvOrSr(() => new string[] { "menuc_1.wav", "menuc_2.wav", "menuc_3.wav",
				(Options.preorder) ? "menuc_4.wav":"",
				(Options.preorder) ? "menuc_5.wav":"", "menuc_6.wav", "menuc_7.wav"
			}, () => new string[] { "Connect to the free-for-all game", "Connect to an existing game", "Start a new game",
				"", "", "Join chat room", "Create chat room"
			}, Options.menuVoiceMode);
			bool exitOnline = false;
			bool startedMusic = false;
			int choice = 0;
			while (!exitOnline) {
				Interaction.clearData(false);

				while (!exitOnline) {
					if (!Client.askForSpectator()) {
						if (!startedMusic) {
							startMusic(DSound.SoundPath + "\\ms6.ogg");
							startedMusic = true;
						}
					} //if doesn't want spectator

					if (DXInput.isFirstPress(Key.Escape))
						exitOnline = true;

					if (!Client.spectatorPending)
						choice = Common.returnSvOrSr(() => sVGenerateMenu(menuIntro, topLevelMenu, getServerItems()), () => GenerateMenu(menuIntro, topLevelMenu, getServerItems()), Options.menuVoiceMode);
					else
						choice = 0; //enter FFA
					menuIntro = null; //Only say menu prompt first time user enters hangar
					switch (choice) {
						case 0: //join FFA
							Client.joinFFA();
							break;

						case 1: //Join an open game	
							BinaryReader gResp = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_requestGameList));
							int listLength = gResp.ReadInt16();
							if (listLength == 0) {
								SapiSpeech.playOrSpeakMenu(DSound.NSoundPath + "\\ng.wav", "No games available");
								break;
							}

							//for menu, we just need the description;
							//the ID is sent by the client to the server but is irrelevant to the user.
							String[] menu = new String[listLength];
							GameInfoArgs[] gamesList = new GameInfoArgs[listLength];
							for (int k = 0; k < listLength; k++) {
								gamesList[k] = new GameInfoArgs(gResp.ReadString(), gResp.ReadString(), gResp.ReadInt32());
								menu[k] = gamesList[k].getDescription();
							}
							int option = GenerateMenu("", menu, getServerItems());
							//Next, send the ID to the server.
							//This is the id of the game we want to connect to.
							if (option == -1)
								break;
							Options.mode = gamesList[option].getGameType();
							int entryMode = Client.getEntryMode();
							if (entryMode == -1)
								break;
							int team = -1;
							BinaryReader resp = null;
							if (Options.mode == Options.Modes.teamDeath) { //if not spectator
								if (entryMode != 1) { //if not spectator
									team = sVGenerateMenu("menuc_3_2_i.wav", new String[] { "menuc_3_2_1.wav", "menuc_3_2_2.wav",
						  "menuc_3_2_3.wav", "menuc_3_2_4.wav"}, getServerItems());
									if (team == -1)
										break;
									Options.team = (Projector.TeamColors)team;
								} //if ! spectator
								resp = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_joinGame, (byte)3, gamesList[option].getId(), team, entryMode));
							} else // no team death
								   // The server will return true if the player has been added to the game.
								resp = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_joinGame, (byte)2, gamesList[option].getId(), entryMode));
							if (resp.ReadBoolean())
								Interaction.inOnlineGame = true;
							break;

						case 2: //create game
							Client.gameHost = true;
							int t = Common.returnSvOrSr(() => sVGenerateMenu("", new String[] { "menuc_3_1.wav", "menuc_3_2.wav" }, getServerItems()), () => GenerateMenu("", new string[] { "Death match", "Team death" }, getServerItems()), Options.menuVoiceMode); ;
							if (t == -1)
								break;
							bool inGame = true;
							switch (t) {
								case 0: //create one-on-one game
									Options.mode = Options.Modes.oneOnOne;
									Client.sendData(CSCommon.buildCMDString(CSCommon.cmd_createGame, (byte)1, (int)Options.Modes.oneOnOne));
									break;

								case 1: //create team death
									Options.mode = Options.Modes.teamDeath;
									int color = Common.returnSvOrSr(() =>  sVGenerateMenu("menuc_3_2_i.wav", new String[] { "menuc_3_2_1.wav", "menuc_3_2_2.wav", "menuc_3_2_3.wav", "menuc_3_2_4.wav" }, getServerItems()), () => GenerateMenu("What team would you like to play on?", new string[] { "Blue team", "Green team", "Red team", "Yellow team" }, getServerItems()), Options.menuVoiceMode);
									if (color == -1) {
										inGame = false;
										break;
									}
									Options.team = (Projector.TeamColors)color;
									Client.sendData(CSCommon.buildCMDString(CSCommon.cmd_createGame, (byte)2, (int)Options.Modes.teamDeath, color));
									break;
							} //switch
							if (inGame)
								Interaction.inOnlineGame = true;
							break;

						case 3: //store
							String[] sOptions = new String[] { "menuc_4_1.wav", "menuc_4_2.wav" };
							int sIndex = 0;
							while ((sIndex = sVGenerateMenu(null, sOptions)) != -1) {
								switch (sIndex) {
									case 0: //view add-ons
										ViewAddOnArgs[] vA = null;
										String[] vA2 = null;
										String vPrompt = null;
										using (BinaryReader viewAddOns = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_viewAddOns))) {
											vA = new ViewAddOnArgs[viewAddOns.ReadInt16()];
											if (vA.Length == 0)
												break;
											vPrompt = String.Format("You have {0} valor points to spend", viewAddOns.ReadInt32());
											vA2 = new String[vA.Length];
											for (int vI = 0; vI < vA.Length; vI++) {
												vA[vI] = new ViewAddOnArgs(viewAddOns.ReadInt32(), viewAddOns.ReadString());
												vA2[vI] = vA[vI].getDescription();
											}
										} //using
										int vO = GenerateMenu(vPrompt, vA2, getServerItems());
										if (vO == -1)
											break;
										using (BinaryReader vResp = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_buyAddOn, vA[vO].getId()))) {
											if (!vResp.ReadBoolean())
												SapiSpeech.speak(vResp.ReadString(), SapiSpeech.SpeakFlag.noInterrupt);
										} //using
										break;

									case 1: //browse my add-ons
										ViewAddOnArgs[] vMA = null;
										String[] vMA2 = null;
										short myAddonsLength = 0;
										using (BinaryReader vMReader = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_viewMyAddOns))) {
											myAddonsLength = vMReader.ReadInt16();
											if (myAddonsLength == 0)
												break;

											vMA = new ViewAddOnArgs[myAddonsLength];
											vMA2 = new String[vMA.Length];
											for (int vMI = 0; vMI < vMA.Length; vMI++) {
												vMA[vMI] = new ViewAddOnArgs(vMReader.ReadInt32(), vMReader.ReadString(), vMReader.ReadBoolean(), vMReader.ReadBoolean(), vMReader.ReadBoolean(), vMReader.ReadBoolean());
												vMA2[vMI] = vMA[vMI].getDescription();
											}
										} //using
										int vMO = (myAddonsLength != 0) ? GenerateMenu(null, vMA2, getServerItems()) : -1;
										if (vMO == -1)
											break;
										int vMO2 = 0;
										String[] vMA3 = new String[] { "", "" };
										if (vMA[vMO].isEnabledOrDisabledType())
											vMA3[0] = (vMA[vMO].showEnableText()) ? "a4.wav" : "a3.wav";
										else {
											if (vMA[vMO].showDecrement())
												vMA3[0] = "a1.wav";
											if (vMA[vMO].showIncrement())
												vMA3[1] = "a2.wav";
										}
										vMO2 = sVGenerateMenu(null, vMA3, getServerItems());
										if (vMO2 == -1)
											break;
										//First, if this is a type "d" add-on and showEnabled is true then we'll enable the add-on,
										//otherwise disable it
										if (vMO2 == 0)
											Client.sendCommand((vMA[vMO].showEnableText()) ? CSCommon.cmd_incAddOn : CSCommon.cmd_decAddOn, vMA[vMO].getId());
										else
											Client.sendCommand(CSCommon.cmd_incAddOn, vMA[vMO].getId());
										break;
								} //switch
							} //while in store
							break;
						case 4: //stats
							using (BinaryReader statReader = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_getStats))) {
								if (!statReader.ReadBoolean())
									SapiSpeech.speak("No stats available.", SapiSpeech.SpeakFlag.noInterrupt);
								else {
									String stats = String.Format("You have {0} valor points. Your power ratio is {1}, with {2} wins and {3} losses.",
									statReader.ReadInt32(), Math.Round(statReader.ReadSingle(), 1), statReader.ReadInt32(), statReader.ReadInt32());
									SapiSpeech.speak(stats, SapiSpeech.SpeakFlag.noInterrupt);
									Client.addChatMessage(stats);
								}
							} //using
							break;

						case 5: //join chat room
							joinChatRoom();
							break;

						case 6: //create chat room
							createChatRoom();
							break;

						default:
							exitOnline = true;
							break;
					} //switch choice
					if (Interaction.inOnlineGame || Interaction.inChat) {
						fadeMusic();
						startMusic();
						startedMusic = false;
						if (Options.entryMode == 1) //Throw game into capturing loop--usually done by
							Interaction.waitForPlayers(); //requestCreate, but spectators don't receive this command.
						Client.spectatorPending = false;
						onlineMenuNotifier.WaitOne();
						Interaction.inOnlineGame = false;
						Interaction.inChat = false;
						fadeMusic();
						break; //Came out of game so go back to parent loop for data clearing.
					}
					if (Client.closed || Options.requestedShutdown)
						exitOnline = true;
				} //while loop for menu
				//Condition below won't be hit in the above loop if we're in a game and ALT+F4 is pressed to close it, so we need it
				//here in case the child loop breaks so we will still hit this condition.
				if (Client.closed || Options.requestedShutdown)
					exitOnline = true;
			} //parent loop to control data clearing
		}


		public static void fadeMusic(bool stop)
		{
			float v;
			while ((v = DSound.getVolumeOfMusic()) > ((stop) ? 0.0f : 0.25f)) { //don't completely fade if not stopping
				DSound.setVolumeOfMusic(v - volumeFadeValue);
				Thread.Sleep(100);
			}
			if (stop)
				music.stopOgg();
		}

		//Fades and stops music before starting new track
		public static void fadeMusic()
		{
			fadeMusic(true);
		}

		public static void restoreMusic()
		{
			float v;
			while ((v = DSound.getVolumeOfMusic()) < musicVolume) {
				DSound.setVolumeOfMusic(v + volumeFadeValue);
				Thread.Sleep(100);
			}
			DSound.setVolumeOfMusic(musicVolume);
		}


		//returns either on or off depending on the boolean value supplied
		public static string getOnOffStatus(bool value)
		{
			if (!value)
				return ("off");
			return ("on");
		}

		//Returns true if this game is a full version;
		//false otherwise
		public static bool isRegistered()
		{
			return true;
		}

		public static bool isLicensedForCurrentVersion()
		{
			return true;
		}

		public static String getLicensedName()
		{
			return "Opensource";
		}

		public static String getLicensedID()
		{
			return "";
		}

		public static bool isValidLicense()
		{
			return (isRegistered() && isLicensedForCurrentVersion());
		}



		public static void startMusic()
		{
			bool changedMusic = false;
			if (Options.mode == Options.Modes.racing) {
				music = DSound.loadMusicFile(DSound.SoundPath + "\\ms2-1.ogg", DSound.SoundPath + "\\ms2-2.ogg");
				changedMusic = true;
			}
			if (Options.mode == Options.Modes.deathMatch || Options.mode == Options.Modes.testing) {
				music = DSound.loadMusicFile(DSound.SoundPath + "\\ms3.ogg");
				changedMusic = true;
			}
			if (Options.mode == Options.Modes.freeForAll || Options.mode == Options.Modes.oneOnOne || Options.mode == Options.Modes.teamDeath) {
				music = DSound.loadMusicFile(DSound.SoundPath + "\\ms7.ogg");
				changedMusic = true;
			}
			if (Options.mode == Options.Modes.mission) {
				if (Mission.isJuliusFight) {
					if (Options.isLoading)
						fadeMusic();
					music = DSound.loadMusicFile(DSound.SoundPath + "\\ms4-3.ogg");
					changedMusic = true;
				}
				if (!changedMusic
					&& Mission.missionNumber == Mission.Stage.discovery) {
					if (Options.isLoading)
						fadeMusic();
					music = DSound.loadMusicFile(DSound.SoundPath + "\\ms8.ogg");
					changedMusic = true;
				}
				if (!changedMusic && Mission.missionNumber == Mission.Stage.gameEnd && Mission.fightType == Interaction.FightType.lastFight1) {
					music = DSound.loadMusicFile(DSound.SoundPath + "\\ms8.ogg");
					changedMusic = true;
				}

				if (!changedMusic && Mission.missionNumber == Mission.Stage.gameEnd && Mission.fightType == Interaction.FightType.lastFight2) {
					music = DSound.loadMusicFile(DSound.SoundPath + "\\ms9.ogg");
					changedMusic = true;
				}

				if (!changedMusic && Mission.missionNumber == Mission.Stage.gameEnd && Mission.fightType == Interaction.FightType.lastFight3) {
					music = DSound.loadMusicFile(DSound.SoundPath + "\\ms10.ogg");
					changedMusic = true;
				}


				if (!changedMusic
					&& Mission.missionNumber <= Mission.Stage.powerPlant) {
					if (Options.isLoading)
						fadeMusic();
					music = DSound.loadMusicFile(DSound.SoundPath + "\\ms4-1.ogg");
					changedMusic = true;
				}
				if (!changedMusic
					&& Mission.missionNumber >= Mission.Stage.chopperFight) {
					if (Options.isLoading)
						fadeMusic();
					music = DSound.loadMusicFile(DSound.SoundPath + "\\ms4-2.ogg");
					changedMusic = true;
				}
			}
			if (changedMusic) {
				DSound.setVolumeOfMusic(musicVolume);
				music.play(true);
			}
		}

		public static void startMusic(String filename, float musicVolume)
		{
			DSound.setVolumeOfMusic(musicVolume);
			if (music != null)
				music.stopOgg();
			music = DSound.loadMusicFile(filename);
			music.play(true);
		}

		public static void startMusic(String filename)
		{
			startMusic(filename, musicVolume);
		}

		public static void saveGame(int slot)
		{
			BinaryWriter w = new BinaryWriter(new FileStream(Addendums.File.appPath
			  + String.Format("\\mission_save{0}.tdv", slot), FileMode.Create));
			Holder h = Interaction.holderAt(1);
			while (!h.haulted)
				Thread.Sleep(100);
			w.Write(float.Parse(applicationVersion, CultureInfo.InvariantCulture.NumberFormat));
			Mission.save(w);
			w.Write(Interaction.length);
			//Only save objects; don't save projectiles,
			//since each object will save its own projectiles.
			foreach (String s in Interaction.getAllNonWeaponIDs())
				Interaction.objectAt(s).save(w);
			w.Flush();
			w.Close();
		}

		public static bool loadGame(int slot)
		{
			String name = null;
			String file = Addendums.File.appPath + String.Format("\\mission_save{0}.tdv", slot);
			if (!File.Exists(file))
				return false;
			Common.mainGUI.startWaitCursor();
			System.Diagnostics.Trace.WriteLine("Entering load game...");
			Options.isLoading = true;
			Options.initializingLoad = true;
			Holder h = Interaction.holderAt(1);
			if (h.isRunning()) { //could be loading from main menu
				System.Diagnostics.Trace.WriteLine("Weapons holder running");
				while (!h.haulted)
					Thread.Sleep(100);
				h.clear(); //clear all the weapons here since options menu does not wait for clear
				System.Diagnostics.Trace.WriteLine("Weapons cleared.");
			}
			Interaction.holderAt(0).clear();
			//If loading from main menu, following objects will be null
			//These objects will not be completely unloaded and reloaded, or this will break object references.
			/*
			if (Mission.player == null)
				Mission.player = new Aircraft(0, 1500, "o", false, new Track(Options.currentTrack));
			Interaction.holderAt(0).add(Mission.player);
			if (Mission.landingBeacon == null)
				Mission.landingBeacon = new LandingBeacon();
				Interaction.holderAt(0).add(Mission.landingBeacon);
			if (Mission.island == null)
				Mission.island = new Island();
				Interaction.holderAt(0).add(Mission.island);
			 * */
			if (!Options.loadedFromMainMenu)
				Mission.player.stopRequestingTerminate();
			Options.initializingLoad = false;

			inFile = new BinaryReader(new FileStream(file, FileMode.Open));
			version = inFile.ReadSingle();
			System.Diagnostics.Trace.WriteLine("Loaded version " + version);
			if (version < 2.4) // Breaking changes ,don't load.
				return false;
			Mission.load();
			System.Diagnostics.Trace.WriteLine("During load, missionNumber is " + Mission.missionNumber);

			int numObj = inFile.ReadInt32();
			System.Diagnostics.Trace.WriteLine("Objects to load is " + numObj);
			Interaction.resetObjectCount();
			for (int i = 1; i <= numObj; i++) {
				name = inFile.ReadString();
				System.Diagnostics.Trace.WriteLine("Loading " + name);
				Projector p = null;
				if (!Options.loadedFromMainMenu) {
					/*If this is an in-game load, we will not reload the player object,
					 * otherwise this will break object references since the player calls load. We don't want
					 * a ghost object.
					 * */
					if (name.Equals("o")) {
						Mission.player.load();
						p = Mission.player;
					} else //Not player
						p = Mission.createNewObject(name);
				} else //Loading from mainmenu, so we can recreate all objects safely.
					p = Mission.createNewObject(name);
				if (p == null)
					throw new NullReferenceException("Null object during load, name = " + name);
				if (name.Equals("o") && Options.loadedFromMainMenu || !name.Equals("o"))
					p.load();
				Interaction.holderAt(0).add(p);
			} //for number objects
			inFile.Close();

			if (Mission.isJuliusFight)
				Mission.refueler.setJuliusHoverPosition();
			startMusic();
			Options.isLoading = false;
			Common.mainGUI.stopWaitCursor();
			System.Diagnostics.Trace.WriteLine("Done loading.");
			return true;
		}

		//Returns formatted String representation of String sector.
		//The return value can be fed to the self-voicing API directly for processing.
		//Parameter should be in form "r,c"
		public static String sectorToString(String sec)
		{
			StringBuilder str = new StringBuilder();
			str.Append("sec.wav");
			//if given uncharted sector to translate,
			//return sec
			if (sec.Equals(""))
				return (str.ToString());
			char[] strchr = sec.ToCharArray();
			int index = 0;
			while (index < sec.Length) {
				if (strchr[index] != ',')
					str.Append("&" + DSound.NSoundPath + "\\s_"
						+ strchr[index] + ".wav");
				else
					break; //comma
				index++;
			} //while
			str.Append("&#"
	+ sec.Split(',')[1]);

			return (str.ToString());
		}

		//We load license into byte array since it
		//doesn't seem as if Reactor closes the file
		//after it is done reading.
		//This would cause occasionalc orruption.
		private static void initializeRegistration()
		{
			loadedReg = true;
		}

		public static String cultureNeutralRound(float value, int precision)
		{
			return Convert.ToString(Math.Round(value, precision), CultureInfo.InvariantCulture);
		}

		public static String cultureNeutralRound(float value)
		{
			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		public static float cultureNeutralRound(String value)
		{
			return Convert.ToSingle(value, CultureInfo.InvariantCulture);
		}


		/// <summary>
		/// Gets a random item from the array that is not a blank option.
		/// </summary>
		/// <param name="choices">An array of Strings from which to choose an item.</param>
		/// <returns>The index of the chosen item.</returns>
		public static int getRandomItem(params String[] choices)
		{
			int max = choices.Length - 1;
			String choice = null;
			int item = 0;
			do {
				choice = choices[item = getRandom(0, max)];
			} while (choice.Equals(""));
			return item;
		}

		public static void increaseMusicVolume()
		{
			DSound.setVolumeOfMusic(DSound.getVolumeOfMusic() + volumeIncrementValue);
			musicVolume = DSound.getVolumeOfMusic();
			Options.writeToFile();
		}
		public static void decreaseMusicVolume()
		{
			DSound.setVolumeOfMusic(DSound.getVolumeOfMusic() - volumeIncrementValue);
			musicVolume = DSound.getVolumeOfMusic();
			Options.writeToFile();
		}

		private static void serverChat()
		{
			Client.sendChatMessage();
		}
		/// <summary>
		/// Executes the commands defined in the array.
		/// </summary>
		/// <param name="keys">An array of ExtraItem objects representing the action and associated delegate to execute.</param>
		public static void executeExtraCommands(ExtraItem[] keys)
		{
			if (keys == null)
				return;
			long[] keyInfo = null;
			foreach (ExtraItem e in keys) {
				keyInfo = KeyMap.getKey(e.action);
				if (keyInfo == null)
					continue; //no key assigned
				if ((keyInfo[0] == -1 && DXInput.isFirstPress((Key)keyInfo[1]))
					|| (keyInfo[0] != -1 && DXInput.isFirstPress((Key)keyInfo[0]) && DXInput.isFirstPress((Key)keyInfo[1])))
					e.extraItemProc();
			}
		}

		/// <summary>
		/// Gets an array populated with increase and decrease music volume functions, since this command will be so common.
		/// </summary>
		/// <returns>An array of type ExtraItem</returns>
		public static ExtraItem[] getIncDecVol()
		{
			return musicExtraItem;
		}

		public static ExtraItem[] getServerItems()
		{
			return serverExtraItem;
		}

		/// <summary>
		/// Plays an ogg file until a key is pressed.
		/// </summary>
		/// <param name="block">If true, the method waits the specified number of seconds before letting a key press or joystick button stop playback. If false, the method waits the specified number of seconds if a key is held down before stopping playback.</param>
		public static void playUntilKeyPress(String file, int wait, bool block)
		{
			int elapsed = 0;
			bool cameInPressed = DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown();
			OggBuffer buffer = DSound.loadOgg(file);
			buffer.play();
			while (buffer.isPlaying()) {
				if (block) {
					if ((elapsed += 10) > wait && (DXInput.isKeyHeldDown(Key.Return)))
						break;
				} else {
					if (!cameInPressed && (DXInput.isKeyHeldDown(Key.Return)))
						break;
					if (cameInPressed && (elapsed += 10) / 1000 > wait)
						break;
					if (cameInPressed)
						cameInPressed = DXInput.isKeyHeldDown(Key.Return);
				}
				Thread.Sleep(10);
			}
			buffer.stopOgg();
		}

		/// <summary>
		/// Plays an ogg file until a key is pressed.
		/// If something is already held down, this method blocks for three seconds before stopping the ogg file.
		/// </summary>
		/// <param name="file">The file name to play.</param>
		public static void playUntilKeyPress(String file)
		{
			playUntilKeyPress(file, 3, false);
		}

		/// <summary>
		/// Plays an ogg file until a key is pressed.
		/// If something is already held down, this method blocks for the specified number of seconds before stopping the ogg file.
		/// </summary>
		/// <param name="wait">The number of seconds to wait if something is held down before stopping the file.</param>
		public static void playUntilKeyPress(String file, int wait)
		{
			playUntilKeyPress(file, wait, false);
		}

		private static void joinChatRoom()
		{
			ChatRoomArgs[] args = null;
			String[] rooms = null;
			using (BinaryReader r = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_viewChatRooms))) {
				short numberOfRooms = r.ReadInt16();
				if (numberOfRooms == 0)
					return;
				args = new ChatRoomArgs[numberOfRooms];
				rooms = new String[numberOfRooms];
				for (int i = 0; i < numberOfRooms; i++) {
					args[i] = new ChatRoomArgs(r.ReadString(), r.ReadString(), r.ReadBoolean());
					rooms[i] = args[i].friendlyName;
				} //for
			} //using
			int choice = GenerateMenu(null, rooms, getServerItems());
			if (choice == -1)
				return;
			BinaryReader resp = null;
			if (args[choice].passworded) {
				ExtendedAudioBuffer enterPassword = DSound.LoadSound(DSound.NSoundPath + "\\pw1.wav");
				DSound.PlaySound(enterPassword, true, false);
				String pwd = mainGUI.receiveInput();
				enterPassword.stop();
				if (pwd == null)
					return;
				resp = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_joinChatRoom, args[choice].id, pwd));
			} else
				resp = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_joinChatRoom, args[choice].id));
			using (resp) {
				bool entered = resp.ReadBoolean();
				if (entered) {
					int count = resp.ReadInt16();
					if (count > 0) {
						for (int i = 0; i < count; i++)
							Client.addMember(resp.ReadString(), resp.ReadString());
						Client.commitMembers();
					}
					enterChatRoom(args[choice].friendlyName);
				}
			}
		}

		private static void enterChatRoom(String name)
		{
			SapiSpeech.enableJAWSHook();
			Interaction.inChat = true;
			mainGUI.startChat(name);
		}

		private static void createChatRoom()
		{
			ExtendedAudioBuffer enr = DSound.LoadSound(DSound.NSoundPath + "\\enr.wav");
			DSound.PlaySound(enr, true, false);
			String name = mainGUI.receiveInput();
			enr.stop();
			if (name == null)
				return;
			String password = null;
			int pwd = sVGenerateMenu("pw3.wav", new String[] { "kd3.wav", "kd4.wav" }, getServerItems());
			if (pwd == -1)
				return;
			if (pwd == 1) {
				enr = DSound.LoadSound(DSound.NSoundPath + "\\pw1.wav");
				do {
					DSound.PlaySound(enr, true, false);
					password = mainGUI.receiveInput();
					enr.stop();
				} while (password == null);
			}
			using (BinaryWriter w = new BinaryWriter(new MemoryStream())) {
				if (password == null)
					w.Write((byte)0);
				else
					w.Write((byte)1);
				w.Write(name);
				if (password != null)
					w.Write(password);
				Client.sendData(CSCommon.buildCMDString(CSCommon.cmd_createChatRoom, (MemoryStream)w.BaseStream));
			} //using
			enterChatRoom(name);
		}

		/// <summary>
		/// Executes a statement lambda according to self-voicing switches
		/// </summary>
		/// <param name="sv">The function to execute if self-voicing is used.</param>
		/// <param name="sr">The function to execute if a screen-reader is used.</param>
		/// <param name="s">The current sv value.</param>
		public static void executeSvOrSr(Action sv, Action sr, Options.VoiceModes s)
		{
			if (s == Options.VoiceModes.selfVoice)
				sv();
			else
				sr();
		}

		/// <summary>
		/// Executes a lambda function according to self-voicing switches
		/// </summary>
		/// <param name="sv">The function to execute if self-voicing is used.</param>
		/// <param name="sr">The function to execute if a screen-reader is used.</param>
		/// <param name="s">The current sv value.</param>
		/// <returns>The result of the function.</returns>
		public static T returnSvOrSr<T>(Func<T> sv, Func<T> sr, Options.VoiceModes s)
		{
			if (s == Options.VoiceModes.selfVoice)
				return sv();
			return sr();
		}

		/// <summary>
		/// Given an object short name (IE: one that's read by the self-voicing API,) returns a friendly name for the object.
		/// </summary>
		/// <param name="name">The short name.</param>
		/// <returns>A friendly name that can be voiced by a screen-reader.</returns>
		public static String getFriendlyNameOf(String name)
		{
			if (Options.isPlayingOnline)
				return name;
			if (!friendlyNames.ContainsKey(name))
				throw new ArgumentException($"The name {name} was not found in the friendlyNames hash table.");
			return friendlyNames[name];
		}
	}
}