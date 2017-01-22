/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using SlimDX.DirectSound;
using BPCSharedComponent.ExtendedAudio;
using SlimDX.DirectInput;
using System.Collections;

namespace TDV
{
	public static class Starter
	{
		private static TextBox textBox1;
		public static void startGame(bool firstTimeLoad)
		{
			if (!firstTimeLoad)
				return;

			try
			{
				textBox1 = new TextBox();
				textBox1.Location = new System.Drawing.Point(0, 0);
				textBox1.Name = "textBox1";
				textBox1.ReadOnly = true;
				textBox1.Size = new System.Drawing.Size(100, 20);
				textBox1.TabIndex = 0;

				SapiSpeech.initialize();
				DSound.initialize(textBox1.Handle);
				dxInput.DInputInit(textBox1);

				DSound.initializeOgg();
				Options.readFromFile();
				KeyMap.initialize();
				//this.Deactivate += new EventHandler(GUI_Deactivate);
				//this.Activated += new EventHandler(GUI_Activated);

				if (!Common.isRegistered())
					MessageBox.Show("This game is not registered. Please contact"
						+ " BPCPrograms SD to obtain a license.",
						"Registration");
				register();
				OggBuffer introSound = DSound.playOgg(DSound.SoundPath + "\\i.ogg");
				while ((introSound.isPlaying() && !dxInput.isKeyHeldDown()))
					Thread.Sleep(5);
				introSound.stopOgg();
				introSound = null;

			}
			catch (Exception err)
			{
				MessageBox.Show(err.Message + " " + err.StackTrace, "Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				Environment.Exit(0);
			}
		}

		public static void startGame()
		{
			if (!KeyMap.readFromFile())
			{
				OggBuffer kError = DSound.playOgg(DSound.NSoundPath + "\\kme.ogg");
				while (kError.isPlaying())
				{
					if (dxInput.isKeyHeldDown())
						break;
					Thread.Sleep(10);
				} //while
				kError.stopOgg();
				kError = null;
			}
			Options.setDifficulty(Options.difficulties.easy);
			Interaction.clearData();
			mainMenu();
			if (!Options.requestedShutdown)
			{
				Common.startGame();
				if (Common.firstTimeLoad)
				{
					Common.firstTimeLoad = false;
					while (!Common.threadNotifier)
						Thread.Sleep(6000);
				}
			}
		}


		private static void writeTrack()
		{
			BinaryWriter s = new BinaryWriter(new FileStream("tracks\\track1.trk", FileMode.Create));
			s.Write("short run");
			s.Write((byte)2);
			s.Write((short)45);
			s.Write(3.0);
			s.Write((byte)0);

			s.Write((short)90);
			s.Write(5.0);
			s.Write((byte)0);

			s.Flush();
			s.Close();
		}

		public static void mainMenu()
		{
			Common.music = DSound.playOgg(DSound.SoundPath + "\\ms1.ogg", true);
			string[] m1 = { "mainmenu_1.wav", "mainmenu_2.wav", "mainmenu_3.wav", "mainmenu_5.wav", "mainmenu_4.wav" };
			bool exitMenu = false;
			while (!exitMenu)
			{
				short index = Common.sVGenerateMenu("mainmenu_i.wav", m1);
				switch (index)
				{
					case 0:
						////start game
						if (Options.mode == Options.Modes.racing)
						{
							if (selectTrack())
								exitMenu = true;
						}
						if (Options.mode == Options.Modes.deathMatch)
							exitMenu = true;
						if (Options.mode == Options.Modes.mission)
							exitMenu = true;

						break;
					case 1:
						////select racing mode
						string[] modeOptions = { "mainmenu_2_1.wav", "mainmenu_2_2.wav", "mainmenu_2_3.wav" };
						short modeIndex = Common.sVGenerateMenu("", modeOptions);
						if (modeIndex == -1)
							break;

						Options.mode = (Options.Modes)(modeIndex + 1);
						if (Options.mode == Options.Modes.mission)
						{
							Mission.isMission = true;
						}
						else
						{
							Mission.isMission = false;
							////reset in case coming
							////out of mission mode
						}

						break;
					case 2:
						////speaker test
						OggBuffer speakerTest = DSound.playOgg(DSound.SoundPath + "\\speakertest.ogg");
						while (speakerTest.isPlaying())
							Application.DoEvents();
						speakerTest = null;
						break;

					case 3:
						////options
						string[] oArray = { "mainmenu_5_1.wav", "mainmenu_5_2.wav" };

						short oIndex = 0;
						while (oIndex != -1)
						{
							oIndex = Common.sVGenerateMenu("mainmenu_5_i.wav", oArray);
							switch (oIndex)
							{
								case 0:
									////mapkeys
									buildKeyMapMenu();
									break;
								case 1:
									////switch input device
									showDevices();
									break;
							}
						}

						break;

					case 4:
						////exit
						Options.requestedShutdown = true;
						exitMenu = true;
						break;
				}
			}
			Common.fadeMusic();

			if (Options.requestedShutdown)
			{
				Options.requestedShutdown = false;
				Common.shutdown();
				return;
			}
		}

		private static void buildKeyMapMenu()
		{
			string[] devices = {"mainmenu_5_1_1.wav",
                      "mainmenu_5_1_2.wav"};
			short dO = Common.sVGenerateMenu("mainmenu_5_1_i.wav",
				devices);
			if (dO == -1)
				return;

			bool mapKeyboard = false;
			bool mapJoystick = false;
			if (dO == 0)
				mapKeyboard = true;
			else
				mapJoystick = true;

			short index = -1;
			Key[] r = null;
			Key[] m = null;
			////modifiers will only be assigned if keyboard is used
			int[] jsKey = null;
			while (index != 0)
			{
				r = null;
				m = null;
				jsKey = null;
				string[] strKeys = null;
				if (mapKeyboard)
				{
					strKeys = KeyMap.getKeyStrings(null);
				}
				else
				{
					strKeys = KeyMap.getKeyStrings(Vehicle.Action.throttleUp,
						Vehicle.Action.throttleDown,
						Vehicle.Action.turnLeft,
						Vehicle.Action.turnRight,
						Vehicle.Action.bankLeft,
						Vehicle.Action.bankRight,
						Vehicle.Action.rollLeft,
						Vehicle.Action.rollRight,
						Vehicle.Action.brakeLeft,
						Vehicle.Action.brakeRight,
					Vehicle.Action.highGTurn,
					Vehicle.Action.ascend,
					Vehicle.Action.descend,
					Vehicle.Action.level,
					Vehicle.Action.togglePointOfView,
					Vehicle.Action.increaseMusicVolume,
					Vehicle.Action.decreaseMusicVolume);
				}
				index = (short)(Common.sVGenerateMenu("ki.wav",
													strKeys) + 1);
				if (index > 0)
				{
					if (mapKeyboard)
					{
						OggBuffer prompt = DSound.playOgg(DSound.NSoundPath + "\\kmp1.ogg");
						while (dxInput.isKeyHeldDown())
						{
							////wait till the user lets up on enter.
							Application.DoEvents();
						}
						while (m == null)
						{
							m = dxInput.getKeys();
						}
						prompt.stopOgg();
						prompt = null;

						prompt = DSound.playOgg(DSound.NSoundPath + "\\kmp2.ogg");
						while ((dxInput.isKeyHeldDown()))
						{
							////wait till the user lets up on enter.
							Application.DoEvents();
						}
						////Next, get a key
						while (r == null)
						{
							r = dxInput.getKeys();
						}
						prompt.stopOgg();
						prompt = null;
						if (m[0] == Key.Return)
						{
							KeyMap.addKey((Vehicle.Action)index, r[0]);
						}
						else
						{
							KeyMap.addKey((Vehicle.Action)index, m[0], r[0]);
						}
					}
					else
					{
						////joystick
						OggBuffer prompt = DSound.playOgg(DSound.NSoundPath + "\\kmp3.ogg");
						while (dxInput.isJSButtonHeldDown())
						{
							Application.DoEvents();
						}
						while (jsKey == null)
						{
							jsKey = dxInput.getJSKeys();
							Application.DoEvents();
						}
						prompt.stopOgg();
						prompt = null;
						KeyMap.addKey((Vehicle.Action)index, dxInput.getJSKeys()[0]);
					}
					////joystick
				}
				////index>=0
			}

			////Since the parent loop has been broken, the user has pressed escape.
			////We need to save the new keymap data to a file now so that it will be restored when the game loads
			KeyMap.saveToFile();
		}

		public static bool selectTrack()
		{
			string[] tracks = Directory.GetFiles(Common.trackDirectory);
			string[] trackNames = new string[tracks.Length];
			int i = 0;
			for (i = 0; i <= tracks.Length - 1; i++)
			{
				trackNames[i] = Area.getName(Common.trackDirectory + '\\' + tracks[i].Split('\\')[tracks[i].Split('\\').Length - 1]);
				////getFiles() returns tracks\filename,
				////and we want only file name. So splitting the return value is necessary.
			}
			short trackIndex = 0;
			if (Mission.isMission)
			{
				trackIndex = (short)Common.getRandom(trackNames.Length - 1);
			}
			else
			{
				trackIndex = Common.GenerateMenu("Select a track", trackNames);
			}
			if ((trackIndex > -1))
			{
				Options.currentTrack = tracks[trackIndex].Split('\\')[tracks[trackIndex].Split('\\').Length - 1];
				////remove the leading relative directory name from the track filename
				return (true);
			}
			return (false);
		}

		private static void showDevices()
		{
			InputDeviceCollection dList = null;
			////holds keyboards
			InputDeviceCollection dList2 = null;
			////holds game controllers
			dList = DirectInput.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AttachedOnly);
			////enumerator for keyboards
			dList2 = DirectInput.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly);
			////enumerator for all game controllers

			DeviceInstance[] devList = null;
			devList = (DeviceInstance[])(Array.CreateInstance(typeof(DeviceInstance),
				(dList2 == null) ? 1 : 2));
			foreach (DeviceInstance d in dList)
			{
				devList[0] = d;
				break;
			}
			if (dList2 != null)
			{
				foreach (DeviceInstance d in dList2)
				{
					devList[1] = d;
					break;
				}
			}
			string[] devListSTR = new string[(dList2 == null) ? 1 : 2];
			devListSTR[0] = "mainmenu_5_1_1.wav";
			if (dList2 != null)
				devListSTR[1] = "mainmenu_5_1_2.wav";
			int mindex = Common.sVGenerateMenu(null, devListSTR);
			if (mindex == -1)
				return;
			////exit menu
			if (mindex > 0)
			{
				////chose joystick
				////so config it
				dxInput.DInputInit(Common.mainGUI.Handle, devList[mindex].InstanceGuid);
				configureJS(devList[mindex].InstanceGuid);
				KeyMap.readFromFile();
				SecondarySoundBuffer confirm = DSound.LoadSound(DSound.NSoundPath + "\\gce.wav");
				DSound.PlaySound(confirm, true, false);
				while (DSound.isPlaying(confirm))
					Thread.Sleep(10);
				DSound.unloadSound(confirm);
				confirm = null;
			}
			else
			{
				if (dxInput.JSDevice != null)
				{
					dxInput.JSDevice.Unacquire();
					dxInput.JSDevice = null;
				} //if !null
				KeyMap.readFromFile();
			} //if chose keyboard
		}

		private static void configureJS(Guid guid)
		{
			if (!File.Exists("dev_" + guid.ToString() + ".tdv"))
			{
				SapiSpeech.speak("This joystick has not been configured yet. You will need to configure it before moving on.", SapiSpeech.SpeakFlag.noInterrupt);
				SapiSpeech.speak("We will now calibrate your joystick. First, press any button on your joystick to begin.", SapiSpeech.SpeakFlag.noInterrupt);
				while (!dxInput.isJSButtonHeldDown())
				{
					Application.DoEvents();
				}
				SapiSpeech.speak("Please make sure your joystick is centered.", SapiSpeech.SpeakFlag.noInterrupt);
				dxInput.JSXCenter = dxInput.JSState.X;
				dxInput.JSYCenter = dxInput.JSState.Y;
				dxInput.JSZCenter = dxInput.JSState.Z;
				dxInput.JSRZCenter = dxInput.JSState.RotationZ;
				BinaryWriter s = new BinaryWriter(new FileStream("dev_" + guid.ToString() + ".tdv", FileMode.Create));
				s.Write(dxInput.JSXCenter);
				s.Write(dxInput.JSYCenter);
				s.Write(dxInput.JSZCenter);
				s.Write(dxInput.JSRZCenter);
				s.Close();
				SapiSpeech.speak("Your joystick has been successfully configured.", SapiSpeech.SpeakFlag.noInterrupt);
			}
			else
			{
				BinaryReader s = new BinaryReader(new FileStream("dev_" + guid.ToString() + ".tdv", FileMode.Open));
				dxInput.JSXCenter = s.ReadInt32();
				dxInput.JSYCenter = s.ReadInt32();
				dxInput.JSZCenter = s.ReadInt32();
				dxInput.JSRZCenter = s.ReadInt32();
				s.Close();
			}
		}

		/*
		public void GUI_Activated(Object sender, EventArgs e)
		{
			SapiSpeech.speak("gained focus.", SapiSpeech.SpeakFlag.interruptable);
			int index = 0;
			Thread thread = null;
			if (Interaction.getThread(0) == null)
				return;
			thread = Interaction.getThread(0);
			do {
				if (thread.ThreadState == ThreadState.Running)
					thread.Priority = ThreadPriority.Normal;
				index++;
				thread = Interaction.getThread(index);
			}
			while (thread != null);
		}

		public void GUI_Deactivate(Object sender, EventArgs e)
		{
			SapiSpeech.speak("lost focus.", SapiSpeech.SpeakFlag.interruptable);
			int index = 0;
			Thread thread = null;
			if (Interaction.getThread(0) == null)
				return;
			thread = Interaction.getThread(0);
			do {
				if (thread.ThreadState == ThreadState.Running)
					thread.Priority = ThreadPriority.Lowest;
				index++;
				thread = Interaction.getThread(index);
			}
			while (thread != null);

		}
		*/

		private static void register()
		{
			try
			{
				if (Common.isRegistered())
				{
					SortedList data = License.Status.KeyValueList;
					int updateIndex = data.IndexOfKey("Updates");
					String[] values = data.GetByIndex(updateIndex).ToString().Split(',');
					if (Array.IndexOf(values, "1.9.5.0") == -1)
						throw new NotSupportedException("Your license is valid, but not registered for the current version of " +
							"TDV. Please contact BPCPrograms SD for a license update.");
				} //if
			} //try
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Registration",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				Environment.Exit(0);
			}
		}

	} //class
} //namespace
