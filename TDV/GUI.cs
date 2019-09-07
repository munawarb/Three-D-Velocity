/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.Input;
using SharpDX.DirectInput;
using System.Collections;
using System.Reflection;
using System.Globalization;
using System.Security.Authentication;
using System.Runtime.InteropServices;

namespace TDV
{
	public class GUI : System.Windows.Forms.Form
	{
		private WebClient webClient;
		private const int totalSize = 252000000;
		private int lastProgress = 0;
		private String chatTitle;
		private String history;
		private String defaultText;
		public delegate void getInputHandler();
		public delegate void commitMembersHandler(ChatRoomMember[] members);
		public delegate void addMemberHandler(ChatRoomMember member);
		public delegate void removeMemberHandler(ChatRoomMember member);
		public delegate void waitCursorStartHandler();
		public delegate void waitCursorStopHandler();
		private string input;
		private Object lockObject;
		private bool inChat = false;
		private bool wasPaused;
		private bool completedDownload;
		private bool error = false;
		private bool shutDownAndInstall = false;
		private bool pressedEnter;
		private bool pwd = false;
		private TableLayoutPanel tableLayoutPanel1;
		private Button BtnLeave;
		private Button BtnSend;
		private TextBox TxtHistory;
		private TextBox TxtChat;
		private ListBox lstWho;
		private ProgressBar progressBar1;
		private Thread runner;
		public bool inputBoxEnabled
		{
			get { return textBox1.Enabled; }
			set { textBox1.Enabled = value; }
		}

		#region " Windows Form Designer generated code "

		public GUI()
			: base()
		{
			//This call is required by the Windows Form Designer.
			InitializeComponent();
			UseWaitCursor = true;
			Name = " Three-D Velocity";
			lockObject = new Object();
			if (!Common.isValidLicense())
				Name += " - Demonstration";
			else
				Name += " - registered by " + Common.getLicensedName();
			Text = Name;
			this.Show();
			this.BringToFront();
			Common.gameHasFocus = true;
			Common.mainGUI = this;
			Common.guiHandle = this.Handle;
			DSound.initialize(Addendums.File.commonAppPath);
			DXInput.DInputInit(this.Handle);
			this.Deactivate += new EventHandler(GUI_Deactivate);
			this.Activated += new EventHandler(GUI_Activated);
			this.FormClosing += new FormClosingEventHandler(GUI_FormClosing);
			textBox1.KeyDown += new KeyEventHandler(textBox1_KeyDown);

			runner = new Thread(startGame);
			UseWaitCursor = false;
			runner.Start();
			//start everything on a new thread so the form can
			//run its message loop
		}

		//Form overrides dispose to clean up the component list.
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
			}
			base.Dispose(disposing);
		}



		//Required by the Windows Form Designer
		public TextBox textBox1;
		//NOTE: The following procedure is required by the Windows Form Designer
		//It can be modified using the Windows Form Designer.  
		//Do not modify it using the code editor.
		[System.Diagnostics.DebuggerStepThrough()]
		private void InitializeComponent()
		{
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.lstWho = new System.Windows.Forms.ListBox();
			this.BtnLeave = new System.Windows.Forms.Button();
			this.BtnSend = new System.Windows.Forms.Button();
			this.TxtHistory = new System.Windows.Forms.TextBox();
			this.TxtChat = new System.Windows.Forms.TextBox();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// textBox1
			// 
			this.textBox1.AcceptsReturn = true;
			this.textBox1.CausesValidation = false;
			this.textBox1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.textBox1.Location = new System.Drawing.Point(270, 128);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(362, 20);
			this.textBox1.TabIndex = 0;
			this.textBox1.TabStop = false;
			this.textBox1.Visible = false;
			this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.CausesValidation = false;
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.Controls.Add(this.lstWho);
			this.tableLayoutPanel1.Controls.Add(this.BtnLeave);
			this.tableLayoutPanel1.Controls.Add(this.BtnSend);
			this.tableLayoutPanel1.Controls.Add(this.TxtHistory);
			this.tableLayoutPanel1.Controls.Add(this.TxtChat);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(16, 16);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(248, 251);
			this.tableLayoutPanel1.TabIndex = 4;
			// 
			// lstWho
			// 
			this.lstWho.AccessibleName = "Participants";
			this.lstWho.FormattingEnabled = true;
			this.lstWho.Location = new System.Drawing.Point(3, 3);
			this.lstWho.Name = "lstWho";
			this.lstWho.Size = new System.Drawing.Size(94, 30);
			this.lstWho.Sorted = true;
			this.lstWho.TabIndex = 6;
			this.lstWho.Visible = false;
			// 
			// BtnLeave
			// 
			this.BtnLeave.CausesValidation = false;
			this.BtnLeave.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.BtnLeave.Location = new System.Drawing.Point(127, 3);
			this.BtnLeave.Name = "BtnLeave";
			this.BtnLeave.Size = new System.Drawing.Size(75, 14);
			this.BtnLeave.TabIndex = 7;
			this.BtnLeave.TabStop = false;
			this.BtnLeave.Text = "Leave";
			this.BtnLeave.UseVisualStyleBackColor = true;
			this.BtnLeave.Visible = false;
			this.BtnLeave.Click += new System.EventHandler(this.BtnLeave_Click);
			// 
			// BtnSend
			// 
			this.BtnSend.AccessibleDescription = "Send the chat message";
			this.BtnSend.AccessibleRole = System.Windows.Forms.AccessibleRole.PushButton;
			this.BtnSend.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.BtnSend.Location = new System.Drawing.Point(3, 118);
			this.BtnSend.Name = "BtnSend";
			this.BtnSend.Size = new System.Drawing.Size(75, 14);
			this.BtnSend.TabIndex = 6;
			this.BtnSend.TabStop = false;
			this.BtnSend.Text = "Send";
			this.BtnSend.UseVisualStyleBackColor = true;
			this.BtnSend.Visible = false;
			this.BtnSend.Click += new System.EventHandler(this.BtnSend_Click);
			// 
			// TxtHistory
			// 
			this.TxtHistory.CausesValidation = false;
			this.TxtHistory.Location = new System.Drawing.Point(127, 118);
			this.TxtHistory.Multiline = true;
			this.TxtHistory.Name = "TxtHistory";
			this.TxtHistory.ReadOnly = true;
			this.TxtHistory.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.TxtHistory.Size = new System.Drawing.Size(94, 20);
			this.TxtHistory.TabIndex = 5;
			this.TxtHistory.Visible = false;
			// 
			// TxtChat
			// 
			this.TxtChat.AccessibleDescription = "";
			this.TxtChat.AccessibleName = "Message";
			this.TxtChat.Location = new System.Drawing.Point(3, 233);
			this.TxtChat.Name = "TxtChat";
			this.TxtChat.Size = new System.Drawing.Size(94, 20);
			this.TxtChat.TabIndex = 4;
			this.TxtChat.Visible = false;
			// 
			// progressBar1
			// 
			this.progressBar1.Location = new System.Drawing.Point(19, 273);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(872, 63);
			this.progressBar1.TabIndex = 5;
			this.progressBar1.Visible = false;
			// 
			// GUI
			// 
			this.AcceptButton = this.BtnSend;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
			this.CancelButton = this.BtnLeave;
			this.CausesValidation = false;
			this.ClientSize = new System.Drawing.Size(916, 367);
			this.ControlBox = false;
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.textBox1);
			this.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.ImeMode = System.Windows.Forms.ImeMode.On;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "GUI";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		public void startGame(bool firstTimeLoad)
		{
			if (!firstTimeLoad)
				return;

			try {
				Common.menuNotifier = new AutoResetEvent(false);
				Common.onlineMenuNotifier = new AutoResetEvent(false);
				Options.readFromFile();
				DSound.setVolumeOfMusic(Common.musicVolume);
				if (isUpdating()) {
					while (!completedDownload)
						Thread.Sleep(500);
					completedDownload = false;

					if (!error) {
						MessageBox.Show("The update download is complete. Click OK to begin installing. TDV will restart once the update is complete.", "Download Complete", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						System.Diagnostics.Process.Start("Updater.exe", "tdv.exe");
						SapiSpeech.enableJAWSHook();
						Environment.Exit(0);
					} else
						Common.GenerateMenu("Error downloading update. Press ENTER to continue.", new String[] { "Ok" });
				} //if getting update

				// If they haven't selected self-voicing options yet or are using an older version with no speech delay, we need to reconfigure them.
				if (Options.menuVoiceMode == Options.VoiceModes.none || ((Options.menuVoiceMode == Options.VoiceModes.screenReader || Options.statusVoiceMode == Options.VoiceModes.screenReader) && SapiSpeech.screenReaderRate == 0f))
					setSVMode();

				if (SapiSpeech.source == SapiSpeech.SpeechSource.notSet)
					SapiSpeech.setSource(SapiSpeech.SpeechSource.auto);
				// This might throw MissingMethodException. It won't harm anything, so ignore it.
				try {SapiSpeech.disableJAWSHook();} catch(MissingMethodException) {}


				if (Options.enabled == Options.Device.gameController)
					showDevices(true);

				KeyMap.initialize();
				register();
				OggBuffer introSound = null;

				introSound = DSound.loadOgg(DSound.SoundPath + "\\tdvl.ogg");
				introSound.play();
				while (introSound.isPlaying() && !DXInput.isKeyHeldDown() && !DXInput.isJSButtonHeldDown() && DXInput.JSDirectionalPadIsCenter())
					Thread.Sleep(100);
				introSound.stopOgg();
				DateTime now = DateTime.Now;
				Options.hour = now.Hour;
				Options.day = now.Day;
				Options.year = now.Year;
				Options.writeToFile();
			}
			catch (Exception err) {
				if (err.Message.Contains("REGDB_E_CLASSNOTREG"))
					MessageBox.Show("You do not have the latest DirectX components installed. " +
						"You can download them from the following address:\r\n" +
						"http://www.bpcprograms.com/programs/utilities/," +
					" or directly from Microsoft at the following address: \r\nhttp://www.microsoft.com/directx.",
						"Prerequisites",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error);
				else
					Common.handleError(err);
			}
		}

		public void startGame()
		{
			if (Common.firstTimeLoad)
				startGame(true);
			while (true) {
				try {
					Interaction.unmuteAllObjects();
					if (!KeyMap.readFromFile() && Common.firstTimeLoad) {
						OggBuffer kError = DSound.loadOgg(DSound.NSoundPath + "\\kme.ogg");
						kError.play();
						while (DXInput.isKeyHeldDown())
							Thread.Sleep(10);
						while (kError.isPlaying()) {
							if (DXInput.isKeyHeldDown())
								break;
							Thread.Sleep(10);
						} //while
						kError.stopOgg();
						kError = null;
					}

					Options.setDifficulty(Options.difficulties.easy);
					Interaction.clearData();
					if (Common.ACBMode) {
						OggBuffer introSound = DSound.loadOgg(DSound.SoundPath + "\\tdvl.ogg");
						introSound.play();
						while (introSound.isPlaying()) {
							if (DXInput.isKeyHeldDown())
								break;
							Thread.Sleep(100);
						}
						introSound.stopOgg();
					} //if ACB mode
					mainMenu();
					if (!Options.requestedShutdown) {
						Common.startGame();
						if (Common.firstTimeLoad)
							Common.firstTimeLoad = false;
						Common.menuNotifier.WaitOne();
						//If the player pressed ALT+F4, requestedShutdown will be true and this wait handle will be set.
						if (Options.requestedShutdown)
							return;
					} //if not requested shutdown eg. player did not choose exit from main menu
					else { //if requested shutdown
						Options.requestedShutdown = false;
						Common.shutdown();
						return; //don't loop over
					} //if shutdown
				}
				catch (Exception e) {
					Common.handleError(e);
				}
			} //while
		}

		private void writeTrack()
		{
			BinaryWriter s = new BinaryWriter(new FileStream("tracks\\track1.trk", FileMode.Create));
			s.Write("short run");
			s.Write((byte)2);
			s.Write((short)5);
			s.Write(3.0);
			s.Write((byte)0);

			s.Write((short)20);
			s.Write(5.0);
			s.Write((byte)0);

			s.Flush();
			s.Close();
		}

		public void mainMenu()
		{
			//writeTrack();
			//If we tried to connect and the connection failed, then the main menu music will still be playing
			if (!Common.failedConnect) {
				Common.startMusic(DSound.SoundPath + "\\ms1.ogg");
			}
			Common.failedConnect = false;

			if (Common.ACBMode) {
				//If we're setting up the environment for the first time,
				//let us pick the game controller.
				if (Options.enabled != Options.Device.gameController)
					showDevices();
				long time = Environment.TickCount;
				bool a = false;
				while ((Environment.TickCount - time) / 1000 < 60) {
					if (DXInput.isKeyHeldDown(Key.Escape)) {
						Options.requestedShutdown = true;
						a = true;
						break;
					}
					if (DXInput.isKeyHeldDown(Key.T)) {
						a = true;
						Options.autoPlay = false;
						Options.mode = Options.Modes.testing;
						break;
					}
					if (DXInput.isKeyHeldDown(Key.P)) {
						a = true;
						Options.autoPlay = false;
						Options.mode = Options.Modes.deathMatch;
						break;
					}
					if (DXInput.isKeyHeldDown(Key.J))
						showDevices();
					Thread.Sleep(100);
				} //while
				if (!a) {
					Options.autoPlay = true;
					Options.mode = Options.Modes.deathMatch;
				} //if we didn't get a specific action to perform
			} else { //if not ACB
				string[] m1 = Common.returnSvOrSr(() => new string[] { "mainmenu_1.wav", "mainmenu_7.wav", "mainmenu_2.wav", "mainmenu_3.wav", "mainmenu_5.wav", "mainmenu_6.wav", "mainmenu_4.wav" },
				() => new string[] { "Start a new game", "Load a saved game", "Mode selection", "Test speakers", "Settings", "Sound description", "Exit" }, Options.menuVoiceMode);
				bool exitMenu = false;
				int index = 0;
				while (!exitMenu) {
					index = Common.returnSvOrSr(() => Common.sVGenerateMenu(DXInput.JSDevice == null ? "mainmenu_i.wav" : "mainmenu_ijs.wav", m1, index, Common.getIncDecVol()), () => Common.GenerateMenu(DXInput.JSDevice == null ? "Main Menu. Use your arrow keys to navigate the options. Press ESCAPE to back out of any menu. Pressing HOME or END will move you to the top or bottom of a menu." : "Main Menu. Press down or right on the View Finder to move forward, and up or left to move backward through a menu. The fire button will select a choice, and the switch weapons button will back out of any menu.", m1, index, Common.getIncDecVol()), Options.menuVoiceMode);
					switch (index) {
						case 0: //start game
							if (Options.mode == Options.Modes.none) { //start flight with no mode selected
								if (!selectMode())
									break; //escaped out of mode selection
							}
							if (Options.mode == Options.Modes.racing) {
								selectTrack();
								exitMenu = true;
							}
							if (Options.mode == Options.Modes.deathMatch)
								exitMenu = true;
							if (Options.mode == Options.Modes.mission)
								exitMenu = true;
							if (Options.mode == Options.Modes.training)
								exitMenu = true;
							if (Options.mode == Options.Modes.multiplayer)
								exitMenu = true;
							break;
						case 1: //loadGame
							if (loadGameMenu())
								exitMenu = true;
							break;
						case 2:
							selectMode();
							break;
						case 3: //speaker test
							Common.playUntilKeyPress(DSound.SoundPath + "\\speakertest.ogg");
							break;
						case 4: //options
							string[] oArray = Common.returnSvOrSr(() => new string[] { "mainmenu_5_1.wav", "mainmenu_5_2.wav", "mainmenu_5_3.wav", "mainmenu_5_4.wav" }, () => new string[] { "Map keys", "Change input device", "Change performance options", "Select screen reader" }, Options.menuVoiceMode);

							int oIndex = 0;
							while (oIndex != -1) {
								oIndex = Common.returnSvOrSr(() => Common.sVGenerateMenu(null, oArray, oIndex, Common.getIncDecVol()), () => Common.GenerateMenu(null, oArray, oIndex, Common.getIncDecVol()), Options.menuVoiceMode);
								switch (oIndex) {
									case 0: //map keys
										buildKeyMapMenu();
										break;

									case 1: //switch device
										showDevices();
										break;

									case 2: //performance options
										changePerformance();
										break;

									case 3: //switch screen reader
										switchScreenReader();
										break;
								}
							}
							break;
						case 5: //sound desc
							soundDescriptionMenu();
							break;
						case -1: //Exit
						default: //exit
							Options.requestedShutdown = true;
							exitMenu = true;
							break;
					}
				}
			} //if not ACB
			if (Options.mode != Options.Modes.multiplayer)
				Common.fadeMusic();
		}

		private void switchScreenReader()
		{
			int choice = Common.returnSvOrSr(() => Common.sVGenerateMenu("mainmenu_5_4_i.wav", new String[] { "mainmenu_5_4_1.wav", "mainmenu_5_4_2.wav" }, (int)(SapiSpeech.source - 1), Common.getIncDecVol()), () => Common.GenerateMenu("Select a speech output source to use.", new String[] { "Microsoft SAPI", "Auto detect" }, (int)(SapiSpeech.source - 1), Common.getIncDecVol()), Options.menuVoiceMode);
			if (choice == -1)
				return;
			switch (choice) {
				case 0:
					SapiSpeech.setSource(SapiSpeech.SpeechSource.SAPI);
					break;
				default:
					SapiSpeech.setSource(SapiSpeech.SpeechSource.auto);
					break;
			}
			Options.writeToFile();
		}

		private void changePerformance()
		{
			String[] options = Common.returnSvOrSr(() => new string[] { "mainmenu_5_3_1.wav", "mainmenu_5_3_2.wav", "mainmenu_5_3_3.wav" }, () => new string[] { "Yes, always", "Occasionally", "No, there is no stuttering" }, Options.menuVoiceMode);
			String intro = Common.returnSvOrSr(() => "mainmenu_5_3_i.wav", () => "Are you experiencing audio stuttering?", Options.menuVoiceMode);
			int choice = Common.returnSvOrSr(() => Common.sVGenerateMenu(intro, options, Common.getIncDecVol()), () => Common.GenerateMenu(intro, options, Common.getIncDecVol()), Options.menuVoiceMode);
			if (choice == -1)
				return;
			Options.writeToFile();
			Common.fadeMusic(true);
			Common.startMusic(DSound.SoundPath + "\\ms1.ogg");
		}

		private void buildKeyMapMenu()
		{
			bool deletedMap = false;
			int dO = 0; //keyboard by default
						//Only display the menu below if we have a joystick connected,
						//else skip it and assume keyboard mapping.
			if (DXInput.JSDevice != null) {
				string[] devices = Common.returnSvOrSr(() => new string[] { "mainmenu_5_1_1.wav", "mainmenu_5_1_2.wav" }, () => new string[] { "Keyboard", "Joystick or flight simulation controller" }, Options.menuVoiceMode);
				dO = Common.returnSvOrSr(() => Common.sVGenerateMenu("mainmenu_5_1_i.wav", devices, Common.getIncDecVol()), () => Common.GenerateMenu("What device would you like to map?", devices, Common.getIncDecVol()), Options.menuVoiceMode);
				if (dO == -1)
					return;
			} //if a joystick is connected

			bool mapKeyboard = false;
			if (dO == 0)
				mapKeyboard = true;

			int index = -1;
			Key[] r = null;
			Key[] m = null;
			//modifiers will only be assigned if keyboard is used
			int[] jsKey = null;
			bool canceledCurrent = false;
			int assigned = 0;
			while (index != 0) {
				r = null;
				m = null;
				jsKey = null;
				string[] strKeys = new string[KeyMap.getNumberOfKeys() + 1];
				canceledCurrent = false;
				assigned = 0;
				if (mapKeyboard)
					KeyMap.getKeyStrings(Aircraft.Action.exitGame, Aircraft.Action.switchWeapon,
						Aircraft.Action.switchToWeapon1,
						Aircraft.Action.switchToWeapon2, Aircraft.Action.switchToWeapon3,
						Aircraft.Action.switchToWeapon4, Aircraft.Action.switchToWeapon5,
						Aircraft.Action.admin, Aircraft.Action.addBot, Aircraft.Action.removeBot,
						Aircraft.Action.endStrafe, Aircraft.Action.cloak, Aircraft.Action.deCloak
					).CopyTo(strKeys, 0);
				else
					KeyMap.getKeyStrings(Aircraft.Action.throttleUp, Aircraft.Action.throttleDown, Aircraft.Action.turnLeft, Aircraft.Action.turnRight,
						Aircraft.Action.bankLeft, Aircraft.Action.bankRight, Aircraft.Action.leftBarrelRoll, Aircraft.Action.rightBarrelRoll, Aircraft.Action.splitS,
						Aircraft.Action.ascend, Aircraft.Action.descend, Aircraft.Action.level, Aircraft.Action.togglePointOfView,
						Aircraft.Action.increaseMusicVolume, Aircraft.Action.decreaseMusicVolume, Aircraft.Action.exitGame, Aircraft.Action.switchToWeapon1, Aircraft.Action.switchToWeapon2, Aircraft.Action.switchToWeapon3, Aircraft.Action.switchToWeapon4, Aircraft.Action.switchToWeapon5, Aircraft.Action.admin, Aircraft.Action.endStrafe, Aircraft.Action.removeBot, Aircraft.Action.addBot, Aircraft.Action.cloak, Aircraft.Action.deCloak
					).CopyTo(strKeys, 0);
				strKeys[strKeys.Length - 1] = Common.returnSvOrSr(() => "kd1.wav", () => "Restore default keymap", Options.menuVoiceMode);
				index = Common.returnSvOrSr(() => Common.sVGenerateMenu("ki.wav", strKeys, (index == -1) ? 0 : (index - 1), Common.getIncDecVol()), () => Common.GenerateMenu("Select a key. Press ESCAPE, or button 2 to save your changes.", strKeys, (index == -1) ? 0 : (index - 1), Common.getIncDecVol()), Options.menuVoiceMode) + 1;
				if (index > 0) {
					if (index < strKeys.Length) {
						if (mapKeyboard) {
							m = null;
							r = null;
							OggBuffer prompt = null;
							Common.executeSvOrSr(() =>
							{
								DSound.loadOgg(DSound.NSoundPath + "\\kmp1.ogg");
								prompt.play();
							}, () => SapiSpeech.speak("Press the modifier you would like to assign to this action; press ENTER for none.", SapiSpeech.SpeakFlag.interruptable), Options.menuVoiceMode);
							while (DXInput.isKeyHeldDown()) {
								//wait till the user lets up on enter.
								Application.DoEvents();
							}
							while (m == null) {
								if (DXInput.isKeyHeldDown(Key.Escape)) {
									canceledCurrent = true;
									break;
								}
								m = DXInput.getKeys();
								Application.DoEvents();
							}
							Common.executeSvOrSr(() =>
							{
								prompt.stopOgg();
								prompt = null;
							}, () => SapiSpeech.purge(), Options.menuVoiceMode);
							if (!canceledCurrent) {
								Common.executeSvOrSr(() =>
								{
									prompt = DSound.loadOgg(DSound.NSoundPath + "\\kmp2.ogg");
									prompt.play();
								}, () => SapiSpeech.speak("Press the key you would like to assign to this action.", SapiSpeech.SpeakFlag.interruptable), Options.menuVoiceMode);
								while (DXInput.isKeyHeldDown()) {
									//wait till the user lets up on enter.
									Application.DoEvents();
								}
								//Next, get a key
								while (r == null) {
									r = DXInput.getKeys();
									Application.DoEvents();
								}
								Common.executeSvOrSr(() =>
								{
									prompt.stopOgg();
									prompt = null;
								}, () => SapiSpeech.purge(), Options.menuVoiceMode);
								bool noModifier = m[0] == Key.Return;
								if (noModifier)
									assigned = KeyMap.alreadyAssignedTo(r[0]);
								else
									assigned = KeyMap.alreadyAssignedTo(m[0], r[0]);
								if (KeyMap.isReserved(m[0], r[0])) {
									Common.executeSvOrSr(() => DSound.playAndWait(DSound.NSoundPath + "\\kd5.wav"), () => SapiSpeech.speak("This key combination is already reserved.", SapiSpeech.SpeakFlag.noInterrupt), Options.menuVoiceMode);
									break;
								}
								if (assigned != 0) {
									int overConf = 0;
									Common.executeSvOrSr(() =>
									{
										DSound.playAndWait(DSound.NSoundPath + "\\kp1.wav");
										DSound.playAndWait(DSound.NSoundPath + "\\" + strKeys[assigned - 1]);
										overConf = Common.sVGenerateMenu("kp2.wav", new String[] { "kd3.wav", "kd4.wav" }, Common.getIncDecVol());
									}, () => overConf = Common.GenerateMenu($"This command is already assigned to {strKeys[assigned - 1]}. In order to assign this command, you will have to reassign the mentioned command. Do you want to continue and assign this action?", new String[] { "No", "Yes" }, Common.getIncDecVol()), Options.menuVoiceMode);
									if (overConf == 0 || overConf == -1)
										break; //loop over, doesn't want to modify assignment
								}
								if (noModifier)
									KeyMap.addKey((Aircraft.Action)index, r[0]);
								else
									KeyMap.addKey((Aircraft.Action)index, m[0], r[0]);
								if (assigned != 0)
									KeyMap.clearKeyboardAssignment((Aircraft.Action)assigned);
							} //if !canceledCurrent
						} else {
							//joystick
							OggBuffer prompt = null;
							Common.executeSvOrSr(() =>
							{
								prompt = DSound.loadOgg(DSound.NSoundPath + "\\kmp3.ogg");
								prompt.play();
							}, () => SapiSpeech.speak("Press the joystick button you would like to assign to this action."), Options.menuVoiceMode);
							while (DXInput.isJSButtonHeldDown()) {
								Application.DoEvents();
							}
							while (jsKey == null) {
								jsKey = DXInput.getJSKeys();
								Application.DoEvents();
							}
							Common.executeSvOrSr(() =>
							{
								prompt.stopOgg();
								prompt = null;
							}, () => SapiSpeech.purge(), Options.menuVoiceMode);
							KeyMap.addKey((Aircraft.Action)index, DXInput.getJSKeys()[0]);
						} //joystick
					} //if index < .length
					else { //if we want to delete keymap
						int yesNo = Common.returnSvOrSr(() => Common.sVGenerateMenu("kd2.wav", new String[] { "kd3.wav", "kd4.wav" }, Common.getIncDecVol()), () => Common.GenerateMenu("Selecting Yes will restore the keymap for the currently selected device back to default. Are you sure you wish to do this?", new String[] { "No", "Yes" }, Common.getIncDecVol()), Options.menuVoiceMode);
						if (yesNo == 1) {
							KeyMap.deleteKeymap((mapKeyboard) ? KeyMap.Device.keyboard : KeyMap.Device.joystick);
							index = 0; //exit keymap
							deletedMap = true;
						} //if answered YES to delete keymap
					} //if want to delte keymap
				} //index>=0
			}

			//Since the parent loop has been broken, the user has pressed escape.
			//We need to save the new keymap data to a file now so that it will be restored when the game loads
			if (!deletedMap)
				KeyMap.saveToFile();
		}

		public void selectTrack()
		{
			string[] tracks = Directory.GetFiles(Common.trackDirectory);
			string[] trackNames = new string[tracks.Length];
			int i = 0;
			for (i = 0; i <= tracks.Length - 1; i++) {
				trackNames[i] = Area.getName(Common.trackDirectory + '\\' + tracks[i].Split('\\')[tracks[i].Split('\\').Length - 1]);
				////getFiles() returns tracks\filename,
				////and we want only file name. So splitting the return value is necessary.
			}
			short trackIndex = 0;
			//if (Mission.isMission) {
			trackIndex = (short)Common.getRandom(trackNames.Length - 1);
			//}
			/*else {
			 trackIndex = Common.GenerateMenu("Select a track", trackNames);
			}
			 * */
			if (trackIndex > -1) {
				Options.currentTrack = tracks[trackIndex].Split('\\')[tracks[trackIndex].Split('\\').Length - 1];
				////remove the leading relative directory name from the track filename
			}
		}

		private void showDevices(bool silentMode)
		{
			IEnumerable dList = null; //holds keyboards
			IEnumerable dList2 = null; //holds game controllers
			dList = DXInput.input.GetDevices(DeviceClass.Keyboard, DeviceEnumerationFlags.AttachedOnly); //enumerator for keyboards
			dList2 = DXInput.input.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly); //enumerator for all game controllers
			//Check to see if we have any controlers attached.
			//dList2 itself will not be null, but the enumerator could be null which means no joysticks.
			DeviceInstance o = null;
			foreach (DeviceInstance t in dList2)
				o = t;
			if (o == null)
				dList2 = null;
			if (dList2 == null && silentMode)
				return;

			DeviceInstance[] devList = null;
			devList = (DeviceInstance[])(Array.CreateInstance(typeof(DeviceInstance),
				(dList2 == null) ? 1 : 2
			));
			foreach (DeviceInstance d in dList) {
				devList[0] = d;
				break;
			}
			if (dList2 != null) {
				foreach (DeviceInstance d in dList2) {
					devList[1] = d;
					break;
				}
			}
			string[] devListSTR = new string[(dList2 == null) ? 1 : 2];
			devListSTR[0] = Common.returnSvOrSr(() => "mainmenu_5_1_1.wav", () => "Keyboard", Options.menuVoiceMode);
			if (dList2 != null)
				devListSTR[1] = Common.returnSvOrSr(() => "mainmenu_5_1_2.wav", () => "Joystick or flight simulation controller", Options.menuVoiceMode);
			int mindex = (silentMode) ? 1
				: Common.returnSvOrSr(() => Common.sVGenerateMenu(null, devListSTR, Common.getIncDecVol()), () => Common.GenerateMenu(null, devListSTR, Common.getIncDecVol()), Options.menuVoiceMode);
			if (mindex == -1)
				return;
			if (mindex > 0) {
				//chose joystick
				//so config it
				bool success = configureJS(devList[mindex].InstanceGuid, silentMode);
				KeyMap.readFromFile();
				Options.enabled = Options.Device.gameController;
				if (success && !silentMode)
					DSound.playAndWait(DSound.NSoundPath + "\\gce.wav");
			} else {
				DXInput.unacquireJoystick(true);
				KeyMap.readFromFile();
				Options.enabled = Options.Device.keyboard;
			} //if chose keyboard
			Options.writeToFile();
		}

		private void showDevices()
		{
			showDevices(false);
		}

		private bool configureJS(Guid guid, bool silentMode)
		{
			//Don't acquire joystick in case we find out
			//that it was never configged below.
			//silentMode means that the device was configged but this is the first time loading the game
			//so we'll automatically enable the joystick
			if (!silentMode) {
				DXInput.ForceFeedbackStatus status=  DXInput.DInputInit(Common.guiHandle, guid);
				if (status == DXInput.ForceFeedbackStatus.couldNotInitialize) {
					Common.playUntilKeyPress(DSound.SoundPath + "\\ffbd.ogg");
				}
			}

			if (!File.Exists(Addendums.File.appPath
						 + "\\dev_" + guid.ToString() + ".tdv")) {
				if (silentMode)
					return false;
				DXInput.JSXCenter = DXInput.JSState.X;
				DXInput.JSYCenter = DXInput.JSState.Y;
				DXInput.JSZCenter = DXInput.JSState.Z;
				DXInput.JSRZCenter = DXInput.JSState.RotationZ;
				BinaryWriter s = new BinaryWriter(
								new FileStream(Addendums.File.appPath
								+ "\\dev_" + guid.ToString() + ".tdv",
								FileMode.Create));
				s.Write(DXInput.JSXCenter);
				s.Write(DXInput.JSYCenter);
				s.Write(DXInput.JSZCenter);
				s.Write(DXInput.JSRZCenter);
				s.Close();
			} else {
				BinaryReader s = new BinaryReader(new FileStream(Addendums.File.appPath
								+ "\\dev_" + guid.ToString() + ".tdv", FileMode.Open));
				DXInput.JSXCenter = s.ReadInt32();
				DXInput.JSYCenter = s.ReadInt32();
				DXInput.JSZCenter = s.ReadInt32();
				DXInput.JSRZCenter = s.ReadInt32();
				s.Close();
				//If we're in silent mode the joystick object wasn't created yet,
				//since we told it not to unless we're sure it's been configged before.
				if (silentMode) {
					DXInput.ForceFeedbackStatus status = DXInput.DInputInit(Common.guiHandle, guid);
					if (status == DXInput.ForceFeedbackStatus.couldNotInitialize) {
						Common.playUntilKeyPress(DSound.SoundPath + "\\ffbd.ogg");
					}
				}
			}
			return true;
		}

		public void GUI_Activated(Object sender, EventArgs e)
		{
			Common.gameHasFocus = true;
			if (!Common.error)
				try { SapiSpeech.disableJAWSHook(); } catch (MissingMethodException) { }
			if (!wasPaused) //We paused the game when we lost focus, it wasn't an in game event.
				Options.isPaused = false;
			wasPaused = false;
			if (Common.music != null)
				Common.restoreMusic();
			int index = 0;
			Thread thread = null;
			if (Interaction.holderAt(0) == null)
				return;
			while (index < Interaction.holderArray.Count) {
				if (!Interaction.holderAt(index).isEmpty()) {
					thread = Interaction.holderAt(index).getAssociatedThread();
					if (thread != null && thread.ThreadState == ThreadState.Running)
						thread.Priority = ThreadPriority.Normal;
				}
				index++;
			}
		}

		public void GUI_Deactivate(Object sender, EventArgs e)
		{
			if (Options.requestedShutdown) //so both Common.shutdown() and this handler don't try to clean
				return; //up resources simultaneously.
			Common.gameHasFocus = false;
			SapiSpeech.enableJAWSHook();
			wasPaused = Options.isPaused;
			//if this flag is not set, we won't unpause the game
			//when the game regains focus.
			//wasPaused indicates whether the game was already paused before we lost focus.
			if (!wasPaused)
				Options.isPaused = true;

			if (Common.music != null)
				Common.fadeMusic(false);
			int index = 0;
			Thread thread = null;
			if (Interaction.holderAt(0) == null)
				return;
			while (index < Interaction.holderArray.Count) {
				if (!Interaction.holderAt(index).isEmpty()) {
					thread = Interaction.holderAt(index).getAssociatedThread();
					if (thread != null && thread.ThreadState == ThreadState.Running)
						thread.Priority = ThreadPriority.Lowest;
				}
				index++;
			}
		}


		private void register()
		{
			if (Common.isValidLicense()) {
				Options.isDemo = false;
				return;
			}
			if (Common.isRegistered()) {
				if (!Common.isValidLicense())
					Common.playUntilKeyPress(DSound.NSoundPath + "\\reg2.ogg");
			} else { //if not license at all
				if (!Common.ACBMode)
					Common.playUntilKeyPress(DSound.NSoundPath + "\\reg1.ogg");
			}
		}

		private bool selectMode(bool resetMode)
		{
			if (resetMode) {
				Options.mode = Options.Modes.none;
				Options.autoPlay = false;
				Options.isPlayingOnline = false;
			}
			string[] modeOptions = Common.returnSvOrSr(() => new string[]{ "mainmenu_2_1.wav",
				"mainmenu_2_2.wav",
				(Options.mode != Options.Modes.autoPlay) ? "mainmenu_2_3.wav" : "",
				(Options.mode != Options.Modes.autoPlay) ? "mainmenu_2_4.wav" : "",
				(Options.mode != Options.Modes.autoPlay) ? "mainmenu_2_5.wav" : "",
				"mainmenu_2_6.wav"
			}, () => new string[] {"Racing mode", "Death match mode",
				(Options.mode != Options.Modes.autoPlay) ? "Training mode" : "",
				(Options.mode != Options.Modes.autoPlay) ? "Mission mode" : "",
				(Options.mode != Options.Modes.autoPlay) ? "Autoplay mode" : "",
				"Multiplayer mode"
			}, Options.menuVoiceMode);
			int modeIndex = Common.returnSvOrSr(() => Common.sVGenerateMenu("", modeOptions, Common.getIncDecVol()), () => Common.GenerateMenu("", modeOptions, Common.getIncDecVol()), Options.menuVoiceMode);
			if (modeIndex == -1)
				return false;
			Options.mode = (Options.Modes)(modeIndex + 1);
			if (Options.mode == Options.Modes.mission)
				Mission.isMission = true;
			else
				Mission.isMission = false;
			//reset in case coming out of mission mode
			if (Options.mode == Options.Modes.multiplayer)
				Options.isPlayingOnline = true;
			else
				Options.isPlayingOnline = false;
			if (Options.mode == Options.Modes.autoPlay) { //player selected autoplay, so next they need to select racing or dm to autoplay
				if (!selectMode(false)) {
					Options.mode = Options.Modes.none;
					return false;
				} else { //selected an autoplay mode
					Options.autoPlay = true;
					return true;
				} //if selected *an* autoplay mode
			} //if selected Modes.autoplay
			return true;
		}

		private bool selectMode()
		{
			return selectMode(true);
		}

		public void startWaitCursor()
		{
			this.Invoke(new waitCursorStartHandler(this.startCursor));
		}

		public void stopWaitCursor()
		{
			this.Invoke(new waitCursorStopHandler(this.stopCursor));
		}

		private void startCursor()
		{
			UseWaitCursor = true;
			Cursor = Cursors.WaitCursor;
		}

		private void stopCursor()
		{
			UseWaitCursor = false;
			Cursor = Cursors.Arrow;
		}

		private void getInput()
		{
			this.AcceptButton = null;
			this.CancelButton = null;
			if (defaultText == null)
				defaultText = "";
			if (pwd)
				this.textBox1.PasswordChar = '*';
			this.textBox1.Text = defaultText;
			textBox1.Visible = true;
			textBox1.Focus();
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{

		}

		private void textBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == Keys.Enter || e.KeyData == Keys.Escape) {
				this.AcceptButton = BtnSend;
				this.CancelButton = BtnLeave;
				input = (e.KeyData == Keys.Enter) ? textBox1.Text : "";
				textBox1.PasswordChar = '\0';
				textBox1.Clear();
				textBox1.Visible = false;
				this.Focus();
				defaultText = "";
				pressedEnter = true;
			} else
				e.Handled = false;
		}

		private void showChat()
		{
			inChat = true;
			TxtHistory.Visible = true;
			BtnSend.Visible = true;
			TxtChat.Visible = true;
			BtnLeave.Visible = true;
			lstWho.Visible = true;
			Text = chatTitle;
			TxtChat.Focus();
		}

		private void hideChat()
		{
			if (!TxtChat.Visible)
				return;
			inChat = false;
			TxtHistory.Visible = false;
			BtnSend.Visible = false;
			TxtChat.Visible = false;
			BtnLeave.Visible = false;
			lstWho.Items.Clear();
			lstWho.Visible = false;
			Text = Name;
			Focus();
			Common.repop();
			try { SapiSpeech.disableJAWSHook(); } catch (MissingMethodException) { }
		}

		public void startChat(String caption)
		{
			chatTitle = caption;
			textBox1.Invoke(new getInputHandler(this.showChat));
		}
		public void leaveChat()
		{
			textBox1.Invoke(new getInputHandler(this.hideChat));
		}

		/// <summary>
		/// Gets input from the user.
		/// </summary>
		/// <param name="password">True if the user is entering a password, false otherwise</param>
		/// <returns>The string entered, empty or null if nothing was entered</returns>
		public String receiveInput(String defaultText, bool password)
		{
			this.defaultText = defaultText;
			pwd = password;
			SapiSpeech.enableJAWSHook();
			pressedEnter = false;
			DXInput.diDev.Unacquire();

			textBox1.Invoke(new getInputHandler(this.getInput));
			while (!pressedEnter) {
				Application.DoEvents();
				Thread.Sleep(10);
			}
			try { SapiSpeech.disableJAWSHook(); } catch (MissingMethodException) { }
			DXInput.diDev.Acquire();
			while (DXInput.isKeyHeldDown(Key.Escape))
				Thread.Sleep(5);
			return input;
		}

		public String receiveInput()
		{
			return receiveInput("", false);
		}

		public void addToHistory(String message)
		{
			if (!inChat)
				return;
			history = message;
			TxtHistory.Invoke(new getInputHandler(this.addToHistoryBox));
			history = null;
		}
		private void addToHistoryBox()
		{
			TxtHistory.AppendText(history + Environment.NewLine);
		}
		private String
				getPageContent(String url)
		{
			try {
				if (webClient == null)
					webClient = new WebClient();
				byte[] output = webClient.DownloadData(url);
				StringBuilder str = new StringBuilder();
				foreach (byte b in output)
					str.Append(
						(char)b);
				return str.ToString();
			}
			catch (System.Net.WebException) {
				return "failed";
			}
		}

		/// <summary>
		/// Downloads an update
		/// </summary>
		/// <param name="url">The URL of the file to download. The domain is prepended</param>
		/// <param name="localPath">The file name to store the update in. It will be housed in the program data directory</param>
		private void downloadUpdate(String url, String localPath)
		{
			if (webClient == null)
				webClient = new WebClient();
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(downloadComplete);
			webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(progressUpdated);
			System.Diagnostics.Trace.WriteLine("Downloading from " + url);
			System.Diagnostics.Trace.WriteLine("and saving to " + localPath);
			webClient.DownloadFileAsync(new Uri(url), localPath);
		}

		private void progressUpdated(Object sender, DownloadProgressChangedEventArgs e)
		{
			int progressPercentage = (int)((float)e.BytesReceived / totalSize* 100);
			if (progressPercentage != lastProgress) {
				lastProgress = progressPercentage;
				progressBar1.Invoke((MethodInvoker)delegate { progressBar1.Value = lastProgress; });
			}
		}

		private void downloadComplete(Object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			Common.fadeMusic();
			progressBar1.Invoke((MethodInvoker)delegate { progressBar1.Visible = false; });
			if (e.Error != null)
				error = true;
			completedDownload = true;
		}

		public static ArrayList getJSGuid()
		{
			IEnumerable dList2 = null;

			dList2 = DXInput.input.GetDevices(DeviceClass.GameControl,
			DeviceEnumerationFlags.AttachedOnly);
			ArrayList d = new ArrayList();
			//enumerator for all game controllers
			//Check to see if we have any controlers attached.
			//dList2 itself will not be null, but the enumerator could be null which means no joysticks.
			foreach (DeviceInstance t in dList2)
				d.Add(t.InstanceGuid);
			if (d.Count == 0)
				return null;
			else
				return d;
		}

		private void GUI_FormClosing(Object sender, FormClosingEventArgs e)
		{
			Common.shutdown();
		}

		private void soundDescriptionMenu()
		{
			String[] s = {"alarm5.wav","alarm7.wav",
								 "alarm2.wav", "alarm3.wav",
								 "alarm1.wav", "alarm4.wav",
								 "alarm6.wav", "ca1-1.wav",
								 "m2.wav", "bsg2.wav", "cr2.wav",
							 "alarm8.wav", "rad2.wav", "alarm10.wav",
							 "alarm9.wav"};

			String[] choices = null;
			String intro = null;
			int choice = 0;
			Common.executeSvOrSr(() =>
			{
				choices = new string[15];
				for (int i = 0; i < choices.Length; i++)
					choices[i] = "s" + (i + 1) + ".wav";
				intro = (DXInput.JSDevice == null) ? "mainmenu_6_i.wav" : "mainmenu_6_ij.wav";
			}, () =>
			{
				choices = new string[] {"Target solution",
					"Solid lock tone",
					"Within vertical range of target",
					"Turn signal",
					"Altitude too low or too high",
					"Stall warning",
					"Stalling",
					"Enemy has tagged you",
					"Semi-radar-guided missile",
					"Projectile",
					"Active-radar-guided missile",
					"Enemy has partial or full lock on you",
					"Player canceled lock",
					"Fuel warning",
					"Rogue warning"
				};
				intro = (DXInput.JSDevice == null) ? "Press ENTER on a sound to hear it" : "Press the fire button on a sound to hear it";
			}, Options.menuVoiceMode);
			ExtendedAudioBuffer clip = null;
			do {
				choice = Common.returnSvOrSr(() => Common.sVGenerateMenu(intro, choices, choice, Common.getIncDecVol()), () => Common.GenerateMenu(intro, choices, choice, Common.getIncDecVol()), Options.menuVoiceMode);
				intro = null;
				if (choice != -1) {
					clip = DSound.LoadSound(DSound.SoundPath + "\\" + s[choice]);
					DSound.PlaySound(clip, true, false);
					while (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown())
						Thread.Sleep(50);
					while (DSound.isPlaying(clip)) {
						if (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown())
							break;
						Thread.Sleep(50);
					}
				} //if choice != -1
				DSound.unloadSound(ref clip);
			} while (choice != -1);
		}

		private bool loadGameMenu()
		{
			String[] ops = Common.returnSvOrSr(() => new String[] { "l_1.wav", "l_2.wav", "l_3.wav" }, () => new string[] { "1", "2", "3" }, Options.menuVoiceMode);
			int slot = Common.returnSvOrSr(() => Common.sVGenerateMenu("mainmenu_7_i.wav", ops, Common.getIncDecVol()), () => Common.GenerateMenu("Select a slot from which to load", ops, Common.getIncDecVol()), Options.menuVoiceMode) + 1;
			if (slot == 0)
				return false;
			Options.loadedFromMainMenu = true; //so loadGame knows we're loading from the main screen.
			Options.Modes oldMode = Options.mode;
			bool oldIsPlayingOnline = Options.isPlayingOnline;
			Options.isPlayingOnline = false;
			Options.mode = Options.Modes.mission; //Some loading code like weapons needs this to be set.
			if (Common.loadGame(slot)) {
				Mission.isMission = true;
				return true;
			}
			DSound.playAndWait(DSound.NSoundPath + "\\ldne.wav");
			Options.loadedFromMainMenu = false;
			Options.mode = oldMode;
			Options.isPlayingOnline = oldIsPlayingOnline;
			return false;
		}

		private void BtnSend_Click(object sender, EventArgs e)
		{
			Client.sendPublicChatMessage(TxtChat.Text);
			addToHistory("You say " + TxtChat.Text);
			TxtChat.Clear();
		}

		private void BtnLeave_Click(object sender, EventArgs e)
		{
			while (DXInput.isKeyHeldDown(Key.Escape))
				Thread.Sleep(5);
			hideChat();
			Client.sendCommand(CSCommon.cmd_leaveChatRoom);
		}

		/// <summary>
		/// Indicates whether this version is updating or not. If it is, the file will have already started
		/// downloading when this method returns.
		/// </summary>
		/// <returns>True if updating, false otherwise</returns>
		private bool isUpdating()
		{
			DateTime now = DateTime.Now;
			if (now.Hour != Options.hour || now.Day != Options.day || now.Year != Options.year) {
				String updatever = getPageContent("https://raw.githubusercontent.com/munawarb/Three-D-Velocity/master/version");
				System.Diagnostics.Trace.WriteLine(updatever);
				if (updatever.Equals("failed"))
					return false;
				float newVersion = float.Parse(updatever, CultureInfo.InvariantCulture.NumberFormat);
				float oldVersion = float.Parse(Common.applicationVersion, CultureInfo.InvariantCulture.NumberFormat);
				if (oldVersion < newVersion) {
					DialogResult download = MessageBox.Show("Three-D Velocity version " + newVersion + " is available. You are running version " + oldVersion + ". Would you like to download version " + newVersion + " now?", "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
					if (download == DialogResult.Yes) {
						updateTo(Common.applicationVersion, updatever, null);
						return true;
					} else
						return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Asks the user to confirm an update.
		/// </summary>
		/// <param name="from">The currently running version</param>
		/// <param name="to">The version to which the user will be updated</param>
		/// <param name="comments">Any comments such as update history. Can be null</param>
		private void updateTo(String from, String to, String comments)
		{
			this.Invoke((MethodInvoker) delegate {
				this.Text = "Downloading update, please wait...";
				this.progressBar1.Visible = true;
			});
			Common.startMusic(DSound.SoundPath + "\\ms5.ogg", 0.5f);
			downloadUpdate("https://github.com/munawarb/Three-D-Velocity-Binaries/archive/master.zip", "Three-D-Velocity-Binaries-master.zip");
		}

		public void copyMessage()
		{
			textBox1.Invoke(new getInputHandler(this.copyToClipboard));
		}

		private void copyToClipboard()
		{
			Clipboard.SetText(Client.getCurrentMessage());
		}

		private void commitMembers(ChatRoomMember[] members)
		{
			lstWho.Items.Clear();
			foreach (ChatRoomMember member in members)
				lstWho.Items.Add(member);
			lstWho.SelectedIndex = 0;
		}

		private void addMember(ChatRoomMember member)
		{
			lstWho.Items.Add(member);
		}

		private void removeMember(ChatRoomMember member)
		{
			lstWho.Items.Remove(member);
		}

		public void commit(ChatRoomMember[] members)
		{
			lstWho.Invoke(new commitMembersHandler(this.commitMembers), members);
		}

		public void add(ChatRoomMember member)
		{
			lstWho.Invoke(new addMemberHandler(this.addMember), member);
		}

		public void remove(ChatRoomMember member)
		{
			lstWho.Invoke(new removeMemberHandler(this.removeMember), member);
		}

		private static void setSVMode()
		{
			int choice = 0;
			while (choice == 0)
				choice = Common.GenerateMenu("Choose a self-voicing option for menus", new string[] { "Self-voicing: menus will be announced using recorded speech", "Screen-reader: Menus will be read using your screen-reader" }) + 1;
			Options.menuVoiceMode = (Options.VoiceModes)choice;
			// Don't commit to the options file until the second choice is also selected,
			// otherwise the user could exit the game before selecting the second choice, leaving the options in an undefined state.
			// So we either commit everything or nothing at all.
			int choice2 = 0;
			while (choice2 == 0)
				choice2 = Common.GenerateMenu("Choose a self-voicing option for status messages. These are in-game announcements such as fuel readouts and speed", new string[] { "Self-voicing: Status messages will be announced using recorded speech", "Screen-reader: Status messages will be read using your screen-reader" }) + 1;
			Options.statusVoiceMode = (Options.VoiceModes)choice2;
			if (Options.menuVoiceMode == Options.VoiceModes.screenReader || Options.statusVoiceMode == Options.VoiceModes.screenReader) {
				// Next, switch to the user's screen reader for the speed test.
				SapiSpeech.setSource(SapiSpeech.SpeechSource.auto);
				Common.GenerateMenu("Since you've opted to use a screen-reader, you'll be presented with a short piece of text on the next screen. Press ENTER when your screen-reader finishes speaking it.", new string[] { "Ok" });
				System.Diagnostics.Stopwatch w = System.Diagnostics.Stopwatch.StartNew();
				String testString = "I really have nothing interesting to put here not even the secret to life except this really long run on sentence that is probably the most boring thing you have ever read but that will help me get an idea of how fast your screen reader is speaking.";
				float wordCount = testString.Split().Length;
				Common.GenerateMenu(testString, new string[] { "Ok" });
				SapiSpeech.screenReaderRate = w.ElapsedMilliseconds / wordCount;
			}
			Options.writeToFile();
		}
	}
}
