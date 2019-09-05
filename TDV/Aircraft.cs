/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using BPCSharedComponent;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.Input;
using BPCSharedComponent.VectorCalculation;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace TDV
{
	/// <summary>
	/// Represents the different positions of the throttle.
	/// </summary>
	public enum OpenPositions
	{
		closed,
		oneQuarter,
		oneHalf,
		threeQuarters,
		full
	}
	public enum Notifications
	{
		strafe,
		missileLaunch,
		goingUp,
		goingDown
	}

	/// <summary>
	/// Represents a generic Aircraft implementation. The IComparable interface is used to rank the aircraft in a race,
	/// and MissionObjectBase inherits projector so this class has movement capabilities.
	/// </summary>
	public class Aircraft : MissionObjectBase, IComparable
	{
		private enum TrainingStages : byte
		{
			turnedOnAfterburners,
			turnedOffAfterburners,
			hudSpeed,
			takeoff,
			levelOut,
			throttleUp,
			stopAccelerating,
			putInGear,
			increaseAOA,
			aboveHardDeck,
			levelOutAboveHardDeck,
			throttleTo0,
			throttleBackUp,
			checkSpeed,
			backOffAfterburners,
			courseTo30,
			bankRightTo30,
			checkTurnRate,
			brake,
			checkTurnRate2,
			levelOut2,
			barrelRoll,
			comeOutOfBarrelRoll,
			splitS,
			loop, //only done by joystick users
			hudCourse,
			lockOnFighter1,
			autoElevateToFighter1,
			matchFighter1Altitude,
			solidToneOnFighter1,
			killFighter1,
			lockOnFighter2,
			distanceBetweenFighter2,
			splitS2,
			killFighter2,
			killFighter3,
			descendTo5000,
			levelOut3,
			threeMilesAway,
			slowToStallWarning,
			descendTo1000,
			takeOutGear,
			land,
			goodLanding,
			badLanding
		}
		private enum RollState
		{
			none,
			barrelRoll,
			aileronRoll,
			loop
		}
		private enum FacingState
		{
			upright,
			inverted
		}
		private enum TargetState
		{
			ascending,
			descending,
			level
		}
		/// <summary>
		/// Actions that can be done by an aircraft.
		/// </summary>
		public enum Action : byte
		{
			throttleUp = 1,
			throttleDown,
			ascend,
			descend,
			level,
			turnLeft,
			turnRight,
			bankLeft,
			bankRight,
			leftBarrelRoll,
			rightBarrelRoll,
			splitS,
			brake,
			retractLandingGear,
			registerLock,
			fireWeapon,
			switchWeapon,
			autoelevation,
			sectorNav,
			activateAfterburners,
			requestRefuel,
			weaponsRadar,
			togglePointOfView,
			pauseGame,
			stopSAPI,
			increaseMusicVolume,
			decreaseMusicVolume,
			optionsMenu,
			whoIs,
			chat,
			prevMessage,
			nextMessage,
			copyMessage,
			exitGame,
			switchToWeapon1,
			switchToWeapon2,
			switchToWeapon3,
			switchToWeapon4,
			switchToWeapon5,
			admin,
			addBot,
			removeBot,
			endStrafe,
			cloak,
			deCloak
		}


		public enum PointOfView : byte
		{
			interior,
			exterior
		}

		private enum Status
		{
			none,
			missionObjective,
			target,
			course,
			speedometer,
			altimeter,
			angleOfAttack,
			rank,
			ammunition,
			lap,
			integrity,
			targetIntegrity,
			engineIntegrity,
			fuel,
			refuelerCount,
			distance,
			sector,
			altitudeRate,
			bankAngle,
			turnRate,
			turnRadius,
			facing,
			loadPercentage
		}

		//sounds
		private ExtendedAudioBuffer throttleClickSound;
		private ExtendedAudioBuffer courseClickSound;
		private ExtendedAudioBuffer selfDestAlarm;
		private ExtendedAudioBuffer ltsHit;
		private ExtendedAudioBuffer catapultSound;
		private ExtendedAudioBuffer lowFuelAlarm;
		private ExtendedAudioBuffer windSound;
		private ExtendedAudioBuffer aileronRollSound, barrelRollSound;
		private ExtendedAudioBuffer radarSound;
		private ExtendedAudioBuffer lockBrokenSound;
		private ExtendedAudioBuffer enginesLaunch;
		private ExtendedAudioBuffer enginesLand;
		private ExtendedAudioBuffer lockAlertSound;
		private ExtendedAudioBuffer afStart;
		private ExtendedAudioBuffer afFlame;
		private ExtendedAudioBuffer afEnd;
		private ExtendedAudioBuffer fallAlarm;
		private OggBuffer ATCMessage;
		private ExtendedAudioBuffer RIOMessage;
		private ExtendedAudioBuffer pilotMessage;
		private ExtendedAudioBuffer lowSpeedAlarm;
		protected ExtendedAudioBuffer engine;
		private ExtendedAudioBuffer jetRumble;
		private ExtendedAudioBuffer altitudeWarningAlarm;
		private ExtendedAudioBuffer landingGear;
		private ExtendedAudioBuffer landingGearOut;
		private ExtendedAudioBuffer turnSignal;
		private ExtendedAudioBuffer destroyed;
		private ExtendedAudioBuffer targetSolutionSound;
		private ExtendedAudioBuffer targetSolutionSound3;

		// The volume at which fade-in will no longer occur.
		private const float engineFadeInThreshold = 0.6f;
		// The amount by which to increase the volume of the jet rumble sound.
		// Since the jet sound starts at engineFadeInThreshold, we should only account for the upper part of the interval.
		private const float rumbleVolumeIncrement = (1f - engineFadeInThreshold) / 1500f;
		private const float windThreshold = 0.2f; // Wind will never drop below this value
		private const float windVolumeIncrement = (1f - windThreshold) / 1500f;
		private const float windFreqIncrement = 0.0005f;
		// Since the front of the aircraft is considered to be from -80 to 80, we'll have the target solution lower in pitch by semitones according to this range.
		private const float targetSolutionFreqCoefficient = -5 / 80f;
		private const float retractGearAltitude = 1000f;
		protected const float minAltitude = 10000f; // Expressed in feet.
		protected const float maxAltitude = 90000f; // Expressed in feet.
		private int sectorX, sectorY;
		private int lastDirection;
		private OpenPositions openPosition, lastOpenPosition;
		private float origThrottleClickFreq;
		private int rollTime; //Time before roll can be executed
		private int gunFireCount, gunWaitTime;
		private int maxGunFireCount, maxGunWaitTime;
		private int fireTimeStamp;
		private int cruiseFire;
		private const int barrelRate = 10; //degrees per second
		private int[] volumeArray;
		private const byte numVolumes = 11;
		private String prevSector;
		private String lastTrainingFile; //the last file played in training mode
		private String lockTag; //used to specify the tag that this object
								//should lock on to if we've received
								//a lock command from the server.
		private string fireTag; //Used to specify ID information about a fire command,
								//sent from the server.
		private int throttlePosition;
		private int m_maxThrottlePosition;
		private int m_minThrottlePosition;
		private const int throttleSpan = 200;
		private List<Action> actionsArray;
		private bool readingInput;
		private List<Action> acHistory;
		private String[] trainingNames = null;
		private bool completedTrainingStage, kf, incorrectCommand;
		private bool playAlarms = true;
		private bool saidInstructions;
		private bool trainer1, trainer2, trainer3 = false;
		private TrainingStages currentStage;
		private Status lastStatusCommand;
		private bool inSplitS;
		private bool inBarrelRoll;
		private RollState rollState;
		private FacingState facingState;
		private bool hasNotifiedRecharge;
		private int rechargeNotifyTime;
		private bool isBraking;
		private byte brakeTick;
		private bool RIOHealthWarned;
		private bool RIOEngineWarned;
		private string input;
		private bool sayRelative;
		//two vars below used to stop messages from replaying
		//once aircraft unmutes and alarms start playing
		private bool loopedThrough;
		private bool actionsThreadRunning;
		private bool stopInput;
		private bool inputPause;
		private Thread inputThread;
		private bool playedSonicBoom;
		private bool enginesOff;
		private bool callingRefueler;
		private bool announcedRefueler;
		private bool landingOnCarrier;
		private bool played0; //since missions are played in new thread,
		private bool played1; //these vars block this projector from calling playXMissionObjectiveX twice.
		private bool played2;
		private bool played3;
		private bool played6;
		private bool firstLaunch;
		private bool iteratingActions;
		protected bool firstMove;
		private bool afterburnersActive;
		//Time for which this craft has been racing. At the beginning of the race, it holds the tickCount
		private long missionCounter;
		private int missileWarnTime;
		private bool hasMissileWarned;
		private long m_totalTime;
		private int m_speakVDTime;
		private PointOfView m_POV;
		//total time elapsed before self destruct will initiate.
		private long m_selfDestructTime;
		//when the race first starts. When the craft is done racing this value will be minused from the time stamp when the craft finishes racing
		private float origEngineFreq;
		private Weapons m_weapon;
		private bool isFalling;
		private float fallRate = 0.0f; //in meters/s
		private bool m_isLeveling;
		private float ctz;
		private TargetState targetState;

		/// <summary>
		/// The range of the degree value the aircraft needs to be in to clear a turn.
		/// </summary>
		private const byte minClearDeg = 5;
		private bool m_isLandingGearRetracted;
		private int gearWarningTime, maxGearWarningTime;
		private int weightWarningTime, maxWeightWarningTime;
		private bool m_isOnRunway;
		private byte m_lap;
		private bool m_isTurning;
		private float m_currentDistance;
		private bool m_isElevating;
		private float elevationAltitude;
		private bool checkForAltitude;
		private bool m_isLifting;

		//The speed at which the nose angle will change viz. value of 5 represents 5 degrees every tick
		private int m_liftSpeed = 1;


		//-----
		//Variables for Straightaway and Track information
		private Track m_currentTrack;
		private Straightaway m_currentStraightaway;

		/// <summary>
		///  Denotes whether auto elevating.
		/// </summary>
		private bool isElevating
		{
			get { return (m_isElevating); }
			set { m_isElevating = value; }
		}

		private int maxThrottlePosition
		{
			get { return (m_maxThrottlePosition); }
			set { m_maxThrottlePosition = value; }
		}

		private int minThrottlePosition
		{
			get { return (m_minThrottlePosition); }
			set { m_minThrottlePosition = value; }
		}

		private int speakVDTime
		{
			get { return m_speakVDTime; }
			set { m_speakVDTime = value; }
		}

		private PointOfView pov
		{
			get { return (m_POV); }
			set { m_POV = value; }
		}

		private long selfDestructTime
		{
			get { return (m_selfDestructTime); }
			set { m_selfDestructTime = value; }
		}

		public long totalTime
		{
			get { return (m_totalTime); }
			set { m_totalTime = value; }
		}

		private bool isLeveling
		{
			get { return (m_isLeveling); }
			set { m_isLeveling = value; }
		}
		public override Weapons weapon
		{
			get { return (m_weapon); }
			set
			{
				m_weapon = value;
			}
		}

		private int soundDecayFactor
		{
			get { return (-1000); }
		}
		private bool isLandingGearRetracted
		{
			get { return (m_isLandingGearRetracted); }
			set { m_isLandingGearRetracted = value; }
		}

		//Flag to determine if has just taken off
		private bool isLifting
		{
			get { return (m_isLifting); }
			set { m_isLifting = value; }
		}
		// If aircraft is on a runway, returns true; otherwise, false
		public bool isOnRunway
		{
			get { return (m_isOnRunway); }
			set { m_isOnRunway = value; }
		}

		private bool isInTurn
		{
			get { return (m_isTurning); }
			set { m_isTurning = value; }
		}

		//Distance traveled on current Straightaway.
		private float currentDistance
		{
			get { return (m_currentDistance); }
			set { m_currentDistance = value; }
		}
		private Track currentTrack
		{
			get { return (m_currentTrack); }
			set { m_currentTrack = value; }
		}
		private Straightaway currentStraightaway
		{
			get { return (m_currentStraightaway); }
			set { m_currentStraightaway = value; }
		}

		private byte lap
		{
			get { return (m_lap); }
			set { m_lap = value; }
		}

		public override byte accelerationSpeed
		{
			get
			{
				if (afterburnersActive)
					return ((byte)(base.accelerationSpeed * 6));

				if (isOnRunway)
					return (100);
				else
					return (base.accelerationSpeed);
			}
			set { base.accelerationSpeed = value; }
		}

		public override byte decelerationSpeed
		{
			get
			{
				if (isOnRunway)
					return (100);
				return (base.decelerationSpeed);
			}
			set { base.decelerationSpeed = value; }
		}

		/// <summary>
		/// maximum achievable rpm for aircraft
		/// </summary>
		public override float maxSpeed
		{
			get
			{
				if (isOnRunway)
					return 200f;
				else
					return ((!isLandingGearRetracted) ? 530f : base.maxSpeed) - getMaxSpeedPercentage();
			}
			set { base.maxSpeed = value; }
		}


		protected int liftSpeed
		{
			get { return (m_liftSpeed); }
			set { m_liftSpeed = value; }
		}


		//--------
		/// <summary>
		/// Creates a new aircraft.
		/// </summary>
		/// <param name="direction">The initial course</param>
		/// <param name="maxSpeed">The speed this Aircraft can reach</param>
		/// <param name="name">The name of the craft. Fighter 1 to fighter 6 should be f1 through f1; racers should be r1 to r6. Other names are also defined.</param>
		/// <param name="isAI">True if controlled by AI, false otherwise.</param>
		/// <param name="currentTrack">If Racing Mode, should define the racing track. Otherwise null</param>
		public Aircraft(Int16 direction, Int16 maxSpeed, string name, bool isAI, Track currentTrack)
			: base(direction, maxSpeed, name, isAI, null) //initialize the underlying projector. 
		{ //This will also give this object its ID
			if (!isAI && Options.mode == Options.Modes.training) {
				playAlarms = false;
				trainingNames = new String[] {"toa1-", "toa2-", "hs", "to", "lo1-", "tu", "sa", "pig", "iaoa", "ahd",
					"loahd", "tt0", "tbu", "cs", "boa",
					"ct30", "brt30", "ctr1-", "b",
					"ctr2-", "lo2-", "br", "coobr", "ss1", "l1",
					"hc", "lof1-", "aetf1-", "mf1a",
					"stof1-", "kf1", "stof2-", "dbf2",
					"ss2", "kf2", "kf3", "dt5000", "lo3-",
					"", "stsw", "dt1000", "tog", "land",
					"gland", "bland"};
			}
			lastTrainingFile = null;
			lastStatusCommand = Status.none;
			facingState = FacingState.upright;
			//let maxthrottleposition be rpm at which 800 mph is achieved
			maxThrottlePosition = (int)(800.0 / Degrees.getCircumference(engineRadius));
			maxThrottlePosition /= throttleSpan;
			//next, let min throttle position be rpm/throttleSpan at which 30 mph is achieved
			//this way, jet will idle at this rpm and can slowly crawl forward if the player doesn't
			//press the brakes
			minThrottlePosition = (int)(30.0 / Degrees.getCircumference(engineRadius));
			minThrottlePosition /= throttleSpan;
			if (Options.autoPlay && name.Equals("o"))
				autoPlayTarget = true;
			maxWeightWarningTime = 30000;
			if (!isAI) {
				setEngineDamagePoints(3000);
				if (DXInput.JSDevice != null) {
					DXInput.setJSSliderRange(minThrottlePosition, maxThrottlePosition);
					DXInput.setJSZRange(minThrottlePosition, maxThrottlePosition);
				}
			}

			throttleDown();
			firstMove = true;
			liftSpeed = 200;
			isObject = false;
			actionsArray = new List<Action>();
			volumeArray = new int[numVolumes];
			isOnRunway = true;
			firstLaunch = true;
			setDamagePoints(
				(isMissionFighter()) ? 2500 : 2000);
			if (Common.ACBMode) {
				if (autoPlayTarget)
					setDamagePoints(15000);
				else
					setDamagePoints(Common.getRandom(3000, 5000));
			}
			setSpan(10f, 0.5f);
			accelerationSpeed = 30;
			decelerationSpeed = 20;
			setWeight(71250, 27000, 37000);
			sectorX = -1;
			sectorY = -1;
			prevSector = "";
			missionCounter = -1;
			this.currentTrack = currentTrack;
			this.currentStraightaway = this.currentTrack.getStraightaway(0);
			lap = 1;
			createWeaponInstance();
			if (isAI) {
				x = Interaction.getAndUpdateX();
				setStrafeTime(10, 10);
			}
			loadSounds();
			engine.setFrequency(freqInterval());
			if (!isAI && !Options.loadedFromMainMenu) {
				Common.playUntilKeyPress(DSound.SoundPath + "\\su.ogg");
			}
			if (!isMissionFighter() && !isInherited() && !Options.initializingLoad) //don't start chopper motor
				playSound(engine, false, true);
			if (!isAI) {
				if (Options.mode == Options.Modes.training)
					acHistory = new List<Action>();
				if (!Options.loadedFromMainMenu && Options.mode != Options.Modes.training) {
					if (!Options.isPlayingOnline) {
						Common.playUntilKeyPress(DSound.SoundPath + "\\cc"
							+ ((Mission.isMission) ? Common.getRandom(3, 4) : Common.getRandom(1, 2)) + ".ogg", 1);
					} //if !online
					else if (Options.mode != Options.Modes.freeForAll && !Client.gameHost)
						DSound.playAndWait(DSound.SoundPath + "\\cc5.wav");
				} //if not initializing load
				inputThread = new Thread(processInput);
				startInput();
			} //if !AI
			totalTime = Environment.TickCount;
			if (Mission.isMission && Options.isDemo)
				totalTime = 0;

			if (isMissionFighter()) {
				startAtHeight(15000f);
				weapon.setInfiniteAmmunition();
			} //if missionFighter
		}

		public Aircraft(String name)
			: this(0, 1500, name, true, new Track(Options.currentTrack)) { }
		public Aircraft(bool isAI)
			: this(0, 1500, (!isAI) ? "o" : "f1", isAI, new Track(Options.currentTrack)) { }

		/// <summary>
		/// Instantiates an Aircraft for training mode.
		/// </summary>
		/// <param name="t1">If this is a stage 1 craft</param>
		/// <param name="t2">If this is a stage 2 craft</param>
		/// <param name="t3">If this is a stage 3 craft</param>
		public Aircraft(Int16 direction, Int16 maxSpeed, string name, bool t1, bool t2, bool t3)
			: this(direction, maxSpeed, name, true, new Track(Options.currentTrack))
		{
			trainer1 = t1;
			trainer2 = t2;
			trainer3 = t3;
			if (t1) {
				if (Mission.player.z + 10000.0 >= 50000.0)
					startAtHeight(Mission.player.z - 10000f);
				else
					startAtHeight(Mission.player.z + 10000f);
				liftSpeed = 0; //don't let them follow player.
			}
			if (t3)
				startAtHeight(Mission.player.z);
			setDamagePoints(500);
		}

		/// <summary>
		///  This method is a callback to Weapons.strike. It will be called whenever this object's weapon hits another object.
		/// </summary>
		/// <param name="isLCS">If true, the weapon that hit was an LTS</param>
		private void strike(bool isLCS)
		{
			if (!isLCS)
				playRIO(soundPath + "he5" + Common.getRandom(1, 4) + ".wav", false);
			else
				playRIO(soundPath + "he6" + Common.getRandom(1, 2)
					+ ".wav", false);
		}

		//event method for Weapons.destroy event
		private void destroy(Projector target)
		{
			if (Options.mode == Options.Modes.training && (currentStage == TrainingStages.killFighter1 || currentStage == TrainingStages.killFighter2 || currentStage == TrainingStages.killFighter3))
				kf = true;
			if (Options.mode != Options.Modes.mission || Options.mode == Options.Modes.training)
				return;
			if (target is MidAirRefueler || target is AircraftCarrier) {
				// We need to set the self-destruct time here since this is a cross thread operation.
				// Once we set the selfdest time, the aircraft loop will pick up that this object is ready to be self-destructed and the pilot will go rogue.
				// The previous implementation was to call selfDestruct() directly but this was resulting in a race condition since the player's damage was dropping to 0, so all objects were terminating. This meant that sometimes the self destruct message would not play.
				selfDestructTime = 2 * 60 * 1000;
			} else
				playRIO(soundPath + "he7"
				+ Common.getRandom(1, 5)
					+ ".wav", false);
		}

		public override void move()
		{
			System.Diagnostics.Trace.WriteLine("Before hit check for " + name);
			if (!hit())
				performDeaths(); //Hit may become true here
			if (hit() && !isRequestedTerminated) {
				die(cause); //isRequestedTerminated is set here
				return;
			}
			System.Diagnostics.Trace.WriteLine("Before readyToTerminate check for " + name);
			if (readyToTerminate()) {
				freeResources();
				if (!isAI)
					terminateInput();
				isProjectorStopped = true;
				return;
			}
			if (isRequestedTerminated)
				return;
			System.Diagnostics.Trace.WriteLine("Before first move check for " + name);
			if (firstMove) {
				if (Options.mode == Options.Modes.mission) {
					System.Diagnostics.Trace.WriteLine("First move, and mission for " + name);
					playSound(engine, true, true);
					firstMove = false;
				} else {
					System.Diagnostics.Trace.WriteLine("First move, and !mission for " + name);
					playSound(engine, true, true);
					firstMove = false;
				}
			} //if firstmove
			System.Diagnostics.Trace.WriteLine("Passed first move check, now checking spectator, with requestingSpectator set to " + requestingSpectator + " for " + name);
			if (requestingSpectator)
				enterSpectatorMode();
			else if (requestingCancelSpectator)
				exitSpectatorMode();

			System.Diagnostics.Trace.WriteLine("Before interact in " + name);
			interact();
			System.Diagnostics.Trace.WriteLine("After interact in " + name);

			if (waitingForHost())
				return; //This way, only clean up code gets called, in case we pressed escape.
			if (!enginesOff) {
				fadeEngines();
				playSound(engine, false, true);
				if (DSound.isPlaying(enginesLaunch))
					playSound(enginesLaunch, false, false);
				if (!isInherited()) {
					if (DSound.isPlaying(jetRumble))
						playSound(jetRumble, false, true);
					if (!isOnRunway && !DSound.isPlaying(jetRumble)) //if it was stopped restart it
						playSound(jetRumble, true, true);
				} //if !chopper
			} //if !enginesOff

			//Update Aircraft position
			if (!isInTurn) {
				if (!isOnRunway)
					currentDistance += Common.convertToTickDistance(speed);
				updateTotalDistance();
			}
			if (!(trainer1 || trainer2 || !isAI && (currentStage == TrainingStages.killFighter1 || currentStage == TrainingStages.solidToneOnFighter1 || currentStage == TrainingStages.killFighter2)))
				base.move();
			if (z < 0.0)
				z = 0f; //stop from going to negative altitude
			if (!isFalling && isStallCondition())
				isFalling = true;

			if (Options.mode == Options.Modes.racing && !requestedLand) {
				if ((currentDistance >= currentStraightaway.length)
					&& (!isInTurn))
					isInTurn = true;
			}
			engine.setFrequency(freqInterval());

			throttle();
			if (!isStallCondition()) {
				if (!isBankStallConditions())
					bank();
				roll();
			}
			afterBurners();
			useFuel();
			if (inSplitS)
				splitS();

			//code below used to implement delay from speed brake recovery
			if (isBraking) {
				brakeTick++;
				if (brakeTick == 3)
					isBraking = false;
			}
			if (!weapon.lockIndex.Equals("-"))
				notifyLock();
			if (isLeveling) {
				if (bankAngle != 0)
					bankAngle = 0;
				if (virtualNoseAngle < 0)
					virtualNoseAngle++;
				else if (virtualNoseAngle > 0)
					virtualNoseAngle--;
				else
					levelOut();
			}
			if (isFalling) {
				if (!isStallCondition()) { //if pulled out of stall
					isFalling = false;
					fallAlarm.stop();
					fallRate = 0.0f;
				} else //still stalling
					fall();
			}
			updateOpenPosition();
			if (!isAI || autoPlayTarget) {
				updateListener();
				playThrottleClick();
				playCourseClick();
				if (playAlarms) {
					soundLowFuelAlarm();
					soundLowSpeedAlarm();
					altitudeWarning();
				}
				targetSolution();
				autoelevate();
				playWindSound();
				changeRumbleVolume();
				startSelfDestruct();
				missileWarn();
				playRollSound();
				if (Mission.isJuliusFight)
					rechargeNotification();
				speakVDistanceToTarget();
				updateTurnSignal();
				announceNewSector();
				repairEngine();
				warnLandingGear();
				if (Options.mode == Options.Modes.training)
					doTraining();
				if (Options.mode == Options.Modes.training && !isAI)
					acHistory.Clear();

				if (Options.mode == Options.Modes.mission) {
					startFighterSwarm();
					if (Mission.missionNumber > Mission.Stage.chopperFight
						&& !Mission.isJuliusFight)
						Mission.attackCounter += Common.intervalMS;
					randomAttack();
					landOnCarrier();
					if (callingRefueler) {
						announceRefueler();
						refuelCommands();
					} //if calling ref

					if ((!played0)
						&& (Mission.missionNumber == Mission.Stage.takingOff)
						&& (z >= minAltitude))
						playMissionObjective0();

					if ((!played1)
						&& (Mission.missionNumber == Mission.Stage.aboveIsland)
						&& (x >= 110.0)
					&& (y >= 110.0)
					&& (x <= 310.0)
					&& (y <= 310.0))
						playMissionObjective1();
					if (!played2
						&& Mission.missionNumber == Mission.Stage.missileHit
						&& x >= 100.0
					&& y >= 100.0
					&& x < 310.0
					&& y < 310.0)
						playMissionObjective2();
					if (!played3 && Mission.missionNumber == Mission.Stage.discovery)
						playMissionObjective3();
					if (!played6)
						tickMissionCounter();
				} //if mission mode
				if (firstLaunch && z > minAltitude)
					firstLaunch = false;
			} //if not AI
			else { //if AI
				if (afterburnersActive && !conditionForAfterburners())
					deactivateAfterburners();
				tickGunWaitTimer();
			}

			if (isMissionFighter()) {
				if (cruiseFire != -1) //if -1,
					cruiseFire += Common.intervalMS; //we've already fired.
				fireCruiseMissile();
			} //if mission fighter
		}

		private void interact()
		{
			lastOpenPosition = openPosition;
			lastDirection = direction;
			if (!isAI) {
				DXInput.updateJSState();
				if (!Options.isPaused)
					executeStatusCommand();
				/*
				if (DXInput.isFirstPress(Key.D))
				{
					Mission.missionNumber = Mission.Stage.gameEnd;
					Thread t = new Thread(Interaction.endGame);
					t.Start();
					Common.fadeMusic();
					return;
				}
				*/

				/*
				if (DXInput.isFirstPress(Key.D)) {
					Mission.missionNumber = Mission.Stage.powerPlant;
					Mission.setUpPowerPlant();
					x = 308;
					y = 180;
				}
				 */

				/*
				if (DXInput.isFirstPress(Key.D)) {
					Mission.missionNumber = Mission.Stage.juliusBattle;
					Mission.isDestroyingRadar = true;
					Mission.setUpRadarStations();
					played6 = true;
					x = 210;
					y = 200;
				}
				*/
				/*
				if (DXInput.isFirstPress(Key.D)) {
					Mission.missionNumber = Mission.Stage.discovery;
				}
				*/


				/*
				if (DXInput.isFirstPress(Key.D)) {
					byte[] blah = null; ;
					using (BinaryWriter bw = new BinaryWriter(new MemoryStream())) {
						bw.Write((sbyte)1);
						SapiSpeech.speak("" + bw.BaseStream.Position, SapiSpeech.SpeakFlag.interruptable);
						SapiSpeech.speak("Length: " + bw.BaseStream.Length, SapiSpeech.SpeakFlag.interruptable);
						bw.Write("Hi there!");
						SapiSpeech.speak("Length: " + bw.BaseStream.Length, SapiSpeech.SpeakFlag.interruptable);
						blah = ((MemoryStream)bw.BaseStream).ToArray();
						foreach (byte bb in blah)
							SapiSpeech.speak("" + bb, SapiSpeech.SpeakFlag.interruptable);
					}
				} //key.d
				*/
				/*
				if (DXInput.isFirstPress(Key.D)) {
					Mission.missionNumber = Mission.Stage.discovery;
					Interaction.advanceToNextMission(this);
					return;
				}
				*/
				/*
				if (DXInput.isFirstPress(Key.D)) {
					List<Projector> a = Interaction.getAliveObjects();
					if (a == null)
						return;
					int mynum = Common.getRandom(0, a.Count - 1);
					if (!a[mynum].name.Equals(name))
						a[mynum].hit(a[mynum].damage, Interaction.Cause.destroyedByWeapon);
				}
				*/
			}

			System.Diagnostics.Trace.WriteLine("Before online check for " + name);
			if (Options.isPlayingOnline)
				receiveData();
			System.Diagnostics.Trace.WriteLine("Got all buffered online data for " + name);
			//If not AI, actions will be handled by processInput. This method will run on separate thread.
			if (isAI) {
				//if this is an AI craft meant to be controlled by a player across the internet, dont' use local getActions.
				if (Options.isPlayingOnline && !isBot()) {
					if (lockTag != null)
						actionsArray.Add(Action.registerLock);
					//Firetag contains types of weapons to fire.
					if (fireTag != null) {
						String fireInfo = getNextParameterToken(ref fireTag);
						String[] fireArgs = fireInfo.Split(',');
						string wIndex = fireArgs[0];
						weapon.nextID = id + fireArgs[1]; //weapons class should assign the given ID to the next created projectile
						switchWeapon((WeaponTypes)Convert.ToByte(wIndex));
						actionsArray.Add(Action.fireWeapon);
					} //if fireTag != NULL
				} else //if not playing online, or is bot
					getActions();
				iterateActions();
			} else { //if not AI
				iterateActions();
				resetActions(); //Previous implementation locked the actions array. However, this would create race condition. Since boolean  flags already denote if actions are being read or processed.
			} //if !AI
		}

		private void getActions()
		{
			resetActions();
			//Exiting is handled by the spectator loop itself,
			//but spectated objects are autotargeted so we get stareo sounds.
			if (autoPlayTarget && Options.entryMode != 1 && DXInput.isFirstPress(Key.Escape)) {
				exitGame();
				if (cause == Interaction.Cause.quitGame)
					return;
			} //if autoplay and pressed escape
			if (maxThrottlePosition - throttlePosition > 1)
				actionsArray.Add(Action.throttleUp);
			if (conditionForAfterburners())
				actionsArray.Add(Action.activateAfterburners);
			if (isInTurn && !inSplitS) {
				if (Degrees.getShortestRotation(direction, currentStraightaway.direction)
					== Degrees.Rotation.clockwise)
					actionsArray.Add(Action.turnRight);
				else if (Degrees.getShortestRotation(direction, currentStraightaway.direction)
== Degrees.Rotation.counterClockwise)
					actionsArray.Add(Action.turnLeft);
			} //if in turn

			if (!inSplitS) {
				if (!isOnRunway) {
					if (Options.mode != Options.Modes.racing && weapon.isValidLock()) {
						int d = targetDegrees();
						Degrees.Rotation r = Degrees.getShortestRotation(direction, targetDegrees());
						RelativePosition w = getPosition(weapon.getLockedTarget());

						if (z > minAltitude) {
							if (!w.isBehind) {
								if (Degrees.getDifference(direction, d) > 45
									&& Common.getRandom(100) <= (this is JuliusAircraft ? 20 : 80)) {
									if (speed >= 600.0)
										actionsArray.Add(Action.brake); //tighter turn
									if (Degrees.getShortestRotation(direction, d) == Degrees.Rotation.counterClockwise)
										bankAngle = -60;
									else if (Degrees.getShortestRotation(direction, d) == Degrees.Rotation.clockwise)
										bankAngle = 60;
								} //if bank condition
								else if (Degrees.getDifference(direction, targetDegrees()) < 45
													&& Common.getRandom(1, 100) <= (this is JuliusAircraft ? 20 : 80)) {
									if (r == Degrees.Rotation.clockwise)
										actionsArray.Add(Action.turnRight);
									else if (r == Degrees.Rotation.counterClockwise)
										actionsArray.Add(Action.turnLeft);
								}
							} //if target not behind

							if (w.isBehind && !inSplitS && z > minAltitude && Common.getRandom(100) <= (this is JuliusAircraft ? 30 : 10))
								actionsArray.Add(Action.splitS);
						} //if validlock
						else if (Options.mode == Options.Modes.racing
							&& weapon.isValidLock()
												&& Degrees.getDifference(direction, targetDegrees()) <= 5) {
							if (Degrees.getShortestRotation(direction, targetDegrees()) == Degrees.Rotation.clockwise)
								actionsArray.Add(Action.turnRight);
							else if (Degrees.getShortestRotation(direction, targetDegrees()) == Degrees.Rotation.counterClockwise)
								actionsArray.Add(Action.turnLeft);
						} //if racing
					} //if z > minAltitude

					if (gunFireCount != 0
		||
		 Common.getRandom(1, 100) <= maxProbability
						&& ((Options.mode != Options.Modes.mission
						&& z > minAltitude
						&& weapon.isValidLock()
						&& weapon.getLockedTarget().z > minAltitude)
						||
						(Options.mode == Options.Modes.mission
						&& weapon.isValidLock())
						))
						actionsArray.Add(Action.fireWeapon);
				} //if !onRunway

				//insplits
				//During the simulators,
				//all aircraft will follow general combat rules
				//IE: no fighting below the hard deck.
				//In mission mode, this restriction is lifted,
				//mainly to prevent the player from flying low and avoiding fighters.
				if (Options.mode != Options.Modes.mission) {
					if (z <= minAltitude + 2000.0
						|| z > minAltitude && weapon.isValidLock()
						&& weapon.getLockedTarget().z > minAltitude
						&& weapon.getLockedTarget().z <= 50000.0
						&& z < weapon.getLockedTarget().z)
						actionsArray.Add(facingState == FacingState.inverted ? Action.descend : Action.ascend);
				} else { //mission mode
					if (weapon.isValidLock()
						&& (weapon.getLockedTarget().z <= 50000.0 || this is JuliusAircraft)
						//julius will follow player no matter at what height
						&& z < weapon.getLockedTarget().z)
						actionsArray.Add(facingState == FacingState.inverted ? Action.descend : Action.ascend);
				} //if mission mode

				//During the simulators,
				//all aircraft will follow general combat rules
				//IE: no fighting below the hard deck.
				//In mission mode, this restriction is lifted,
				//mainly to prevent the player from flying low and avoiding fighters.
				if (weapon.isValidLock()
					&& (Options.mode != Options.Modes.mission
					&& weapon.getLockedTarget().z > minAltitude
					&& z > weapon.getLockedTarget().z
					|| Options.mode == Options.Modes.mission
					&& z > weapon.getLockedTarget().z))
					actionsArray.Add(facingState == FacingState.inverted ? Action.ascend : Action.descend);
				if (z > minAltitude + 2000.0 && noseAngle > 0 && !isLeveling)
					actionsArray.Add(Action.level);
			} //if !inSplitS


			if (!isLandingGearRetracted)
				actionsArray.Add(Action.retractLandingGear);
			if (!weapon.isTargetInRange())
				actionsArray.Add(Action.registerLock);

			//See iterateActions and switchWeapon method for complete implementation of condition below.
			if (gunFireCount == 0 && Common.getRandom(100) <= 5)
				actionsArray.Add(Action.switchWeapon);
		}

		/// <summary>
		/// Checks to see if the action passed can be performed.
		/// This method assumes the state of the primary input device has been updated outside this method.
		/// </summary>
		/// <param name="theAction">The action to perform.</param>
		/// <param name="keyOnce">True if the key should be pressed only once, false if it can be rapidly fired (I.E. held down.)</param>
		/// <returns>True if the action passed, false otherwise.</returns>
		private bool checkCommand(Action theAction, bool keyOnce)
		{
			if (DXInput.JSDevice != null) {
				long[] jsAssignment = KeyMap.getKey(theAction, true);
				if (jsAssignment != null) {
					if (jsAssignment[1] == -1)
						return JSCheck(theAction);

					if (keyOnce && DXInput.isFirstPressJSB((int)jsAssignment[1]))
						return true;
					//a joystick action has been registered,
					//so don't process keyboard.
					else if (!keyOnce && DXInput.isJSButtonHeldDown((int)jsAssignment[1]))
						return true; //a joystick action has been registered, so don't process keyboard.
				} //if jsAssignment != null
			} //if JS

			long[] assignment = KeyMap.getKey(theAction);
			if (assignment == null)
				return false;
			if (keyOnce) {
				if ((assignment[0] == -1
					|| DXInput.isKeyHeldDown((Key)(assignment[0]), false)
					) && DXInput.isFirstPress((Key)(assignment[1]), false))
					return true; //key once
			} else { //if not keyOnce
				if (assignment[0] == -1) {
					if (DXInput.isKeyHeldDown((Key)assignment[1], false)) {
						KeyData[] possibleHolds =
							KeyMap.getAllAssignmentsFor(assignment[1],
							theAction); //get all other commands that have this key assigned
						if (possibleHolds != null) {
							foreach (KeyData kd in possibleHolds) {
								if (kd.modifier != -1
									&& DXInput.isKeyHeldDown((Key)kd.modifier, false))
									return false; //don't assign anything,
												  //since another modifier for this key is being held down and we're going to
												  //register another action
							} //for
						} //if possibleHolds != null
						return true;
					} //if the key is held down
				} else { //if modifier defined
					if ((DXInput.isKeyHeldDown((Key)assignment[0], false))
						&& (DXInput.isKeyHeldDown((Key)assignment[1], false)))
						return true; //if both key and modifier held
				} //if modifier defined
			} //if not keyOnce
			return false;
		}

		//Check keys with canRepeat set to true.
		//eg. this key can be rapidfired
		private bool checkCommand(Action theAction)
		{
			return check(theAction, false);
		}

		private bool check(Action theAction, bool keyOnce)
		{
			if (checkCommand(theAction, keyOnce)) {
				actionsArray.Add(theAction);
				return true;
			}
			return false;
		}

		private bool check(Action theAction)
		{
			return check(theAction, false);
		}



		//returns true if a joystick action was done,
		//false otherwise
		private bool JSCheck(Action theAction)
		{
			switch (theAction) {
				case Action.bankRight: //No need to do bank left, will perform same function
					bankAngle = DXInput.JSXAxis() * 18; //js x axis scaled from -5 to 5
					break;
				case Action.decreaseMusicVolume:
					return DXInput.JSDirectionalPad() == DXInput.DirectionalPadPositions.down;
				case Action.increaseMusicVolume:
					return DXInput.JSDirectionalPad() == DXInput.DirectionalPadPositions.up;
				case Action.turnLeft:
					return DXInput.JSRZAxis() < 0;
				case Action.turnRight:
					return DXInput.JSRZAxis() > 0;
				case Action.level:
					return DXInput.JSYAxisIsCenter();
				case Action.ascend:
					return DXInput.JSYAxis() > 0;
				case Action.descend:
					return DXInput.JSYAxis() < 0;
				case Action.togglePointOfView:
					return DXInput.isFirstPressJSDP(DXInput.DirectionalPadPositions.left) || DXInput.isFirstPressJSDP(DXInput.DirectionalPadPositions.right);
			}
			return false;
		}

		/// <summary>
		/// Determines if the specified action was performed during this tick.
		/// </summary>
		/// <param name="a">The Action to check</param>
		/// <returns>True if the Action was performed; false otherwise.</returns>
		private bool didAction(Action a)
		{
			return acHistory.Contains(a);
		}

		/*
		private void actionsArray.Add(ExtendedAudioBuffer[] theArray, ExtendedAudioBuffer ac)
		{
			if ((theArray.GetUpperBound(0) == 0)) {
				if ((theArray[0] == null)) {
					theArray[0] = ac;
					return;
				}
			}

			if (theArray[theArray.Length - 1] != null) //if the whole thing is filled
				Array.Resize(ref theArray, theArray.Length + 1);
			int newPos = theArray.Length - 1;
			while (newPos >= 0 && theArray[newPos] == null) //look for the next available slot
				newPos--;
			newPos++; //move forward one slot so we don't override
			theArray[newPos] = ac;
		}
		*/



		////--------
		////The following methods are invoked when an action is performed.
		private void ascend(bool transferred)
		{
			if (!transferred && facingState == FacingState.inverted) {
				descend(true);
				return;
			}
			takeOff();
			if (isReceiver())
				return; //z is updated through Client.completeBuild()
			if (isOnRunway)
				return;
			if (isAI) {
				z += liftSpeed;
				if (weapon.isValidLock()
					&& weapon.getLockedTarget().z > minAltitude
					&& Math.Abs(z - weapon.getLockedTarget().z) <= liftSpeed + 50.0)
					z = weapon.getLockedTarget().z;

				if (z >= maxAltitude)
					z = maxAltitude - 1000f;
				if (virtualNoseAngle == 0)
					levelOut();
				return;
			}
			if (DXInput.JSDevice == null) {
				increaseNoseAngle();
				if (virtualNoseAngle == 0)
					levelOut();
			} else
				virtualNoseAngle = DXInput.JSYAxis() * (facingState == FacingState.inverted ? -10 : 10);
		}

		private void descend(bool transferred)
		{
			if (!transferred && facingState == FacingState.inverted) {
				ascend(true);
				return;
			}
			if (isOnRunway)
				return;
			if (isAI) {
				z -= liftSpeed;
				if (weapon.isValidLock()
					&& Math.Abs(z - weapon.getLockedTarget().z)
					<= liftSpeed + 50f)
					z = weapon.getLockedTarget().z;
				if (z < 1000f)
					z = 1000f;
				if (virtualNoseAngle == 0)
					levelOut();
				return;
			}
			if (DXInput.JSDevice == null) {
				decreaseNoseAngle();
				if (virtualNoseAngle == 0)
					levelOut();
			} else
				virtualNoseAngle = DXInput.JSYAxis() * (facingState == FacingState.inverted ? -10 : 10);
		}


		protected virtual void loadSounds()
		{
			if (engine == null) {
				engine = loadSound(soundPath + "e1.wav");
			}
			System.Diagnostics.Trace.WriteLine("Engine is " + ((engine == null) ? "nulll" : "not null") + " for " + name);
			if (jetRumble == null)
				jetRumble = loadSound(soundPath + "e2.wav");
			if (afStart == null)
				afStart = loadSound(soundPath + "a1.wav", true, false); // Load with notifications.
			if (afFlame == null)
				afFlame = loadSound(soundPath + "a2.wav");
			afStart.setOnEnd(() =>
			{
				if (this.afFlame != null)
					playSound(this.afFlame, true, true);
			});
			if (afEnd == null)
				afEnd = loadSound(soundPath + "a3.wav");
			if (enginesLaunch == null)
				enginesLaunch = loadSound(soundPath + "e31.wav");
			if (enginesLand == null)
				enginesLand = loadSound(soundPath + "e3.wav");
			//if AI, engines will be stopped but not faded because 3d sound modifies volume and it could cause conflicts.
			System.Diagnostics.Trace.WriteLine("IsAI is " + isAI + " for " + name);
			if (!isAI)
				jetRumble.setVolume(0.5f);
			System.Diagnostics.Trace.WriteLine("Passed set jetRumble volume for " + name);
			origEngineFreq = engine.getFrequency();
			System.Diagnostics.Trace.WriteLine("Passed set engine frequency for " + name);
			if (landingGear == null)
				landingGear = loadSound(soundPath + "l.wav");
			if (landingGearOut == null)
				landingGearOut = loadSound(soundPath + "l2.wav");


			if (!isAI || autoPlayTarget) {
				throttleClickSound = loadSound(soundPath + "thrpos.wav");
				origThrottleClickFreq = throttleClickSound.getFrequency();
				courseClickSound = DSound.LoadSound(soundPath + "coupos.wav");
				if (ltsHit == null)
					ltsHit = loadSound(soundPath + "ca1-1.wav");
				if (aileronRollSound == null)
					aileronRollSound = loadSound(soundPath + "wind1.wav");
				if (barrelRollSound == null)
					barrelRollSound = loadSound(soundPath + "wind2.wav");
				if (catapultSound == null)
					catapultSound = loadSound(soundPath + "m1.wav");
				if (lowFuelAlarm == null)
					lowFuelAlarm = loadSound(soundPath + "alarm10.wav");
				if (windSound == null)
					windSound = loadSound(soundPath + "e6.wav");
				if (radarSound == null)
					radarSound = loadSound(soundPath + "rad.wav");
				if (lockBrokenSound == null)
					lockBrokenSound = loadSound(soundPath + "rad2.wav");
				if (lockAlertSound == null)
					lockAlertSound = loadSound(soundPath + "alarm8.wav");
				if (fallAlarm == null)
					fallAlarm = loadSound(soundPath + "alarm6.wav");
				if (lowSpeedAlarm == null)
					lowSpeedAlarm = loadSound(soundPath + "alarm4.wav");
				if (targetSolutionSound == null)
					targetSolutionSound = DSound.LoadSound(DSound.SoundPath + "\\alarm5.wav");
				if (targetSolutionSound3 == null)
					targetSolutionSound3 = loadSound(soundPath + "alarm7.wav");
				if (altitudeWarningAlarm == null)
					altitudeWarningAlarm = loadSound(soundPath + "alarm1.wav");
				if (turnSignal == null)
					turnSignal = DSound.LoadSound(soundPath + "alarm3.wav");
				if (selfDestAlarm == null)
					selfDestAlarm = DSound.LoadSound(soundPath + "alarm9.wav");
			}
		}


		/// <summary>
		/// Will move the Aircraft up one straightaway,
		/// or wrap around to the beginning,
		/// or end race for this Aircraft
		/// </summary>
		private void moveToNextStraightaway()
		{
			if (currentTrack.isEnd(currentStraightaway)) {
				if ((lap >= Options.laps)) {
					hit(0, Interaction.Cause.finishedRace);
				} else {
					////if laps < options.laps
					resetAircraft();
					lap += 1;
				}
			} else {
				////if hasn't reached end of track
				currentDistance = 0f;
				////reset distance on straightaway
				currentStraightaway = currentTrack.getStraightaway(currentStraightaway.id + 1);
			}
		}
		private void resetAircraft()
		{
			x = 0f;
			y = 0f;
			direction = 0;
			currentDistance = 0f;
			currentStraightaway = currentTrack.getStraightaway(0);
		}

		private void takeOff()
		{
			/*If we send the ascend command,
			 * it means the player has launched, so launch the receiver unconditionally.
			 * */
			if ((isOnRunway && speed >= 150.0)
	|| isReceiver()) {
				isOnRunway = false;
				if (!isAI)
					virtualNoseAngle = 5;
				z += 2f;
				playSound(enginesLaunch, true, false);
				playSound(jetRumble, true, true);
				//Only send ascend command if we're about to take off.
				//Else this is redundant since we send height anyway.
				if (isSender())
					Client.addData(Action.ascend, id);
			}
		}

		private void retractLandingGear()
		{
			if (!isOnRunway) {
				if (!isLandingGearRetracted) {
					if (z > retractGearAltitude) {
						if (!isAI)
							landingGearOut.stop();
						playSound(landingGear, true, false);
						isLandingGearRetracted = true;
					}
					////if z>100
				} else {
					////if  landingGearRetracted
					if (z < retractGearAltitude) {
						landingGear.stop();
						playSound(landingGearOut, true, false);
						isLandingGearRetracted = false;
					}
					////if z<100
				}
				////if gear not retracted
			}
			////if on runway
		}

		private void altitudeWarning()
		{

			if (!isOnRunway) {
				if (z <= minAltitude || z > 50000.0) {
					if (z <= minAltitude) {
						if (!DSound.isPlaying(altitudeWarningAlarm)) {
							if (Options.mode != Options.Modes.mission
										&& !firstLaunch
								&& !requestedLand && !landingOnCarrier && !isElevating) {
								if (ATCMessage != null)
									ATCMessage.stopOgg();
								ATCMessage = DSound.loadOgg(soundPath + "alarm1m.ogg");
								ATCMessage.play();
							}
						} //if alarm not looping
					}

					DSound.PlaySound(altitudeWarningAlarm, false, true);
				} else {
					////if z>minAltitude
					if (DSound.isPlaying(altitudeWarningAlarm)) {
						altitudeWarningAlarm.stop();
					}
					////if alarm.status.looping
				}
				////if z<minAltitude
			}
			////if not isOnRunway
		}


		private void updateTurnSignal()
		{
			if (!isInTurn) {
				if (DSound.isPlaying(turnSignal))
					turnSignal.stop();
				return;
			}
			float x = 0;
			float y = 0;
			float z = 0;
			x = this.x;
			y = this.y;
			z = this.z;
			Degrees.moveObject(ref x, ref y, currentStraightaway.direction, 1f, 3f);
			DSound.PlaySound3d(turnSignal, false, true, x, z, y);
		}
		private void updateListener()
		{
			DSound.setOrientation(velocity.X, 0f,velocity.Y, 0f, (facingState == FacingState.upright)? 1f:-1f, 0f);
			if (pov == PointOfView.interior)
				DSound.SetCoordinates(this.x, this.z, this.y);
			else
				DSound.SetCoordinates(this.x, 0, this.y);
			DSound.setVelocity(velocity.X, 0, velocity.Y);
		}


		private void speak(string theString, SapiSpeech.SpeakFlag flags)
		{
			SapiSpeech.speak(theString, flags);
		}

		private void speak(string theString)
		{
			speak(theString, SapiSpeech.SpeakFlag.interruptable);
		}

		////to sort this vehicle based on rank,
		////the IComparable.compareTo method must be implemented.
		////When an array of this type is sorted,
		////the lowest rank will be at index 0, and the vehicle in the lead will be at the highest index.
		/*This method will alternate under two conditions:
		1. if the game is still in progress, the compareTo method
		 * will return a value based on the aircraft's distance relative to obj.
		 * This is because the aircraft that is further ahead is in a higher rank.
		 2. If Interaction.isGameFinished()==true, then
		 * this method will return a value based on the total time it took
		 * the aircrafts to complete the match. This is because the
		 * aircraft with the smallest time value is in first place.
		 * 
		 * In other words, when isgamefinished()==false, the object at the end of the sorted array is in first place,
		 * whereas the opposite is true when isgamefinished()==true.
		 */
		public int CompareTo(object obj)
		{
			Aircraft otherVehicle = (Aircraft)obj;
			if (!Interaction.isGameFinished()) {
				if (totalDistance < otherVehicle.totalDistance)
					return (-1);
				else if (totalDistance > otherVehicle.totalDistance)
					return (1);
				else
					return (0);
			}
			if (totalTime < otherVehicle.totalTime)
				return (-1);
			if (totalTime > otherVehicle.totalTime)
				return (1);
			return (0);
		}

		public short getRank()
		{
			lock (Interaction.theArray) {
				Aircraft[] theArray = new
					Aircraft[Interaction.length - Interaction.numberOfNonlisted];
				int i = 0;
				foreach (String s in Interaction.getAllIDs()) {
					if (Interaction.objectAt(s).unlisted || !(Interaction.objectAt(s) is Aircraft))
						continue;
					theArray[i++] = (Aircraft)Interaction.objectAt(s);
				}
				Array.Sort(theArray);
				short rank = 0;
				int index = i - 1;
				while (index >= 0) {
					rank++;
					if (theArray[index].Equals(this))
						return (rank);
					index--;
				}
				return (-1);
			}
		}

		/// <summary>
		/// Gets the total time (in minutes:seconds) that this Aircraft has been racing on the track.
		/// </summary>
		/// <returns>String in x minutes y seconds format</returns>
		public string getTimeElapsed()
		{
			int seconds = (int)totalTime / 1000;
			int minutes = (int)Math.Floor((float)seconds / 60);
			seconds -= minutes * 60;
			string msgstr = "";
			if ((minutes != 0)) {
				if ((minutes == 1)) {
					msgstr = minutes + " minute";
				} else {
					msgstr = minutes + " minutes";
				}
			}

			////tack on seconds
			if ((seconds != 0)) {
				if ((!msgstr.Equals(""))) {
					////if we have minutes,
					////add "and"
					msgstr += " and ";
				}
				if ((seconds == 1)) {
					msgstr += seconds + " second";
				} else {
					msgstr += seconds + " seconds";
				}
			}
			return (msgstr);
		}

		//Returns true if obtained a lock, false otherwise
		//Used to tell server code to send off the weaponIndex, or disregard the lock command.
		protected override bool registerLock()
		{
			//If lockTag is not null, we've received a lock command from the server,
			//so just lock the object in question and return since we know what object to lock already.
			if (lockTag != null) {
				weapon.lockIndex = getNextParameterToken(ref lockTag);
				return true;
			}

			if (!isAI)
				playSound(radarSound, true, false);
			if (isOnRunway)
				return false;
			if (isInherited() || isMissionFighter()) {
				base.registerLock();
				return true;
			}
			if (lockOnPlayer())
				return true;

			int index = 0;
			List<Projector> vArray = Interaction.getObjectsInRange(this,
			weapon.radarRange,
			Interaction.RangeFlag.existing, true); //order the objects so that the first one is nearest to this aircraft

			//if there's nothing in range, or the thing in range is not supposed to be locked onto then just return.
			if (vArray == null
			|| (vArray.Count == 1 && !vArray[0].showInList))
				return false;

			String id = null;
			if (!isAI) {
				string[] strArray = new string[(Options.mode == Options.Modes.mission && !isAI) ? (vArray.Count + 1) : (vArray.Count)];
				int i = 0;
				for (i = 0; i < strArray.Length; i++) {
					if (!isAI && Options.mode == Options.Modes.mission && i == strArray.Length - 1) {
						if (isAboveIsland())
							strArray[i] = Common.returnSvOrSr(() => "above.wav&i.wav", () => "Above Island", Options.menuVoiceMode);
						else {
							Projector proj = Mission.island;
							RelativePosition ispos = getPosition(proj);
							strArray[i] = Common.returnSvOrSr(() => proj.name + ".wav&at.wav&" + ispos.clockMark + "o.wav&#" + Common.cultureNeutralRound(ispos.distance, 1) + "&mc.wav", () => $"Island at {ispos.clockMark} o'clock, {Common.cultureNeutralRound(ispos.distance, 1)} miles closure", Options.menuVoiceMode);
						} //if we're not above the island
						continue;
					}
					strArray[i] = Common.returnSvOrSr(() =>
					{
						return ((vArray[i].showInList) ? (vArray[i].ToString()
						+ ".wav") : "") + "&"
						+ getPosition(vArray[i]);
					}, () =>
					{
						RelativePosition pos = getPosition(vArray[i]);
						pos.sapiMode = true;
						return ((vArray[i].showInList) ? Common.getFriendlyNameOf(vArray[i].ToString())
						: "") + " "
						+ pos;
					}, (Options.isPlayingOnline) ? Options.VoiceModes.screenReader : Options.statusVoiceMode); // If we're playing online, we'll be using SAPI for output in the lock menu, always.
				}
				System.Diagnostics.Trace.WriteLine("Got beforepause input");
				pauseInput();
				System.Diagnostics.Trace.WriteLine("got after input");
				Interaction.stopAndMute(false);
				index = Common.returnSvOrSr(() => Common.sVGenerateMenu(null, strArray, 0, "n"), () => Common.GenerateMenu("Lock on", strArray), (Options.isPlayingOnline) ? Options.VoiceModes.screenReader : Options.statusVoiceMode); // If we're playing online, we'll be using SAPI for output in the lock menu, always.
				Interaction.resumeAndUnmute();
				resumeInput();

				if (index == -1 || (Options.mode == Options.Modes.mission && index == strArray.Length - 1))
					return false;
				id = vArray[index].id;
			} else { //if AI
				int selectedID = 0;
				do {
					id = vArray[selectedID = Common.getRandom(vArray.Count - 1)].id;
				} while (!vArray[selectedID].showInList);
			} //if  AI
			weapon.lockIndex = id;
			if (isAI)
				return true; //don't play lock announcement
			playRIO(soundPath + "lk" + Common.getRandom(0, 1)
+ ".wav", true);
			return true;
		}

		public override void hit(int decrementValue, Interaction.Cause cause)
		{
			//don't let the object die if this is racing mode.
			if (Options.mode == Options.Modes.racing && cause == Interaction.Cause.destroyedByWeapon) {
				decelerate(decrementValue);
				return;
			}
			//Next, we need to capture the state just before the player dies,
			//otherwise we'll get a read of 0 object since other objects will start ending themselves
			//after player's health drops
			//Also after the first spectatorPending run the method may check again, so block it from reevaluating the condition
			if (Options.isPlayingOnline && !isAI && damage - decrementValue <= 0 && !Client.spectatorPending)
				askForSpectatorMode();
			if (Options.mode == Options.Modes.training && !isAI && cause == Interaction.Cause.destroyedByWeapon)
				return; //make player invincible
			base.hit(decrementValue, cause);
			System.Diagnostics.Trace.WriteLineIf(!isAI, "After base.hit, cause is " + this.cause + " and damage is " + damage);
			//Even though RIOPlay checks for flags below, we put them here to decrease processing overhead. Otherwise, every object that inherits this class will
			//go through the conditions below, and we don't want AI to check for these conditions since they don't play RIO anyway.
			if (!isAI || autoPlayTarget) {
				if (cause == Interaction.Cause.destroyedByWeapon) {
					if (!RIOHealthWarned && getHealthPercent() <= 50) {
						//If fighting Julius, he will taunt player.
						//Otherwise the RIO will tell player of health.
						if (!Mission.isJuliusFight)
							playRIO(soundPath + "hb.wav");
						else
							Mission.darkBlaze.warnPlayer();
						RIOHealthWarned = true;
					} else if (getHealthPercent() > 50)//probably rose above 50% again
						RIOHealthWarned = false;

					//line below will only play if we're not warning for health,
					//since it's force play flag is set to false.
					playRIO(soundPath + "h" + Common.getRandom(1, 2) + ".wav", false);
				} //if hit by weapon
				DXInput.startHitEffect();
			} //if not AI or this is autoplay target
		}

		private void die(Interaction.Cause cause)
		{
			System.Diagnostics.Trace.WriteLine(name + " called die...");
			if (cause == Interaction.Cause.none)
				cause = Interaction.Cause.destroyedByClient;
			//don't reaccount time of projector if landing
			if (!requestedLand && !Options.isDemo)
				totalTime = (long)Environment.TickCount - totalTime;
			if (!isAI)
				stopSAPI();
			stopMessages();
			weapon.setStrafe(false);
			isFalling = false;
			if (!isAI)
				DXInput.unacquireJoystick(false);
			muteAlarms();
			base.hit(0,
	(isReceiver()) ? Interaction.Cause.destroyedByClient : cause);
			//register the cause through the hit() method
			notifyLock();
			String sMessage = "";
			if (Mission.isMission && cause == Interaction.Cause.quitGame)
				Mission.pointsWorth = 0; //otherwise player will earn match's total points by escaping out of landing.
			if (cause == Interaction.Cause.quitGame)
				Options.abortGame = true;
			else if (cause == Interaction.Cause.demoExpired) {
				Options.demoExpired = true;
				playFinalOgg(DSound.SoundPath + "\\demo.ogg");
			} else if (cause == Interaction.Cause.finishedRace) {
				if (!isAI) {
					speak("You have completed the race with a time of " + getTimeElapsed(), SapiSpeech.SpeakFlag.interruptable);
					if (Mission.isMission && Interaction.getRanks() == null)
						Mission.racingScore += Mission.pointsWorth;
				}
			} else if (cause == Interaction.Cause.destroyedByClient
				 || cause == Interaction.Cause.engineDestroyed
				 || cause == Interaction.Cause.destroyedByWeapon) {
				if (isSender()) {
					if (cause == Interaction.Cause.engineDestroyed) {
						speak("Your engine exploded.");
						sMessage = name + "'s engine exploded.";
					}
				} //if sender
				// So that this sound doesn't get muted by playFinalOgg, we'll load it on the alwaysLoud device.
				explodeSound = loadSound(soundPath + "d1.wav", false, true);
				playSound(explodeSound, true, false);
				if (!isAI) {
					DXInput.startExplodeEffect();
					terminateInput();
					if (Options.mode == Options.Modes.mission) {
						Common.fadeMusic();
						playFinalOgg(DSound.SoundPath + "\\gm.ogg");
					}
				} //if !AI
			} else if (cause == Interaction.Cause.destroyedByImpact
				|| cause == Interaction.Cause.stalled) {
				if (!isAI)
					terminateInput();
				if (isSender()) {
					speak("You made a pilot error");
					sMessage = name + " made a pilot error";
				}
				if (!isAI
					&& Options.mode == Options.Modes.mission && isAboveOcean()) {
					Common.fadeMusic();
					playFinalOgg(DSound.SoundPath + "\\d5.ogg");
				} else { //If not above ocean
					destroyed = loadSound(soundPath + "d2.wav");
					playSound(destroyed, true, false);
					while (DSound.isPlaying(destroyed))
						Thread.Sleep(10);
					playDeathMessage();
				}

				//Water crash has own scene.
				if (!isAI
					&& Options.mode == Options.Modes.mission && !isAboveOcean()) {
					Common.fadeMusic();
					playFinalOgg(DSound.SoundPath + "\\gm.ogg");
				}
				if (Options.mode == Options.Modes.training && !isAI)
					doTraining();
			} else if (cause == Interaction.Cause.successfulLanding) {
				isOnRunway = true;
				playSound(enginesLand, true, false);
				if (Mission.isMission) {
					if (Options.mode == Options.Modes.racing)
						Mission.racingScore += 3;
					else if (Options.mode == Options.Modes.deathMatch)
						Mission.deathMatchScore += 3;
				}
				while (DSound.isPlaying(enginesLand))
					Thread.Sleep(5);
				if (Options.mode == Options.Modes.training && !isAI)
					doTraining();
			} else if (cause == Interaction.Cause.selfDestructed) {
				if (ATCMessage != null)
					ATCMessage.stopOgg();
				playFinalOgg(DSound.SoundPath + "\\sd2.ogg");
			} //else if
			if (isSender()) {
				sendFinalObjectUpdate();
				if (!sMessage.Equals("")) {
					Client.sendCommand(CSCommon.cmd_serverMessage, sMessage);
					//destroyed is already sent by projector through weapons.eventHit
				}
			}
			if (!blockKill()) {
				requestedLand = false;
				isRequestedTerminated = true;
				Interaction.kill((Projector)this);
			}
		}

		protected override String fireWeapon()
		{
			String weaponID = null;
			/* We must put this condition here since when
			 * gunWaitTime is first set below, we could register another fire
			 * before switchWeapon properly sets in.
			 * For details, see getActions() to see how
			 * strafing works.
			 * This is a similar case.
			 * */
			if (weapon.weaponIndex == WeaponTypes.guns && gunWaitTime > 0)
				return null;
			/*
			 * Julius and the choppers will only fire if
			 * they are in perfect alignment with the player.
			 * */
			if (weapon.weaponIndex == WeaponTypes.laserCannonSystem || isInherited()) {
				System.Diagnostics.Trace.WriteLineIf(this is JuliusAircraft, String.Format("Firing range: {0}, validLock {1}", weapon.inFiringRange(), weapon.isValidLock()));
				if (!weapon.inFiringRange())
					return null;
				if (!weapon.isValidLock())
					return null;
				//target could be destroyed,
				//so don't let fire at nonexistent target

				RelativePosition p = getPosition(weapon.getLockedTarget());
				if (p.degreesDifference <= 5) {
					if (p.isAhead) {
						if (this is Chopper
							&& (fireTimeStamp += Common.intervalMS) / 1000 < 5
							&& weapon.weaponIndex == WeaponTypes.explosiveMissile)
							return null;
						weaponID = weapon.use(weapon.weaponIndex, weapon.getLockedTarget(),
							weapon.firingRange);
						fireTimeStamp = 0;
					}
				} //if in degree range
				return weaponID;
			} //if lts or inherited

			if (weapon.weaponIndex != WeaponTypes.cruiseMissile) {
				//only vertically align the weapon if
				//we're in vertical range,
				//else make it a free floating projectile
				//If the AI is strafing, fire the guns, otherwise wait
				//for the strafe to register below. This way they don't do single shot fires.
				if (!isAI || (isAI && weapon.weaponIndex != WeaponTypes.guns) || (isAI && gunFireCount != 0) || isReceiver())
					weaponID = weapon.use(weapon.weaponIndex,
(weapon.isValidLock()
&& Math.Abs(z - weapon.getLockedTarget().z) <= weapon.firingRange.verticalDistance ?
weapon.getLockedTarget() : null),
weapon.firingRange);
				//Since the player could be aiming at a target, take that into account.
				//But still make the weapon targetless.
				//So, if they have a projector tracked, assume they want to target it.
				//But still allow the weapon to hit anything that blocks it.

				//Next, make sure the AI strafe!
				if (!isReceiver()) {
					if (isAI && weapon.weaponIndex == WeaponTypes.guns && gunFireCount == 0)
						gunFireCount = 1;
					else if (gunFireCount > 0 && ++gunFireCount >= maxGunFireCount) {
						gunFireCount = 0;
						gunWaitTime = 1;
					} //If gun fire time is over && strafing
				} //if !online
				return weaponID;
			} else { //if cruise selected,
				if (weapon.inFiringRange() && weapon.cruiseMissileLocked())
					return weapon.use(weapon.weaponIndex, weapon.lCSTarget, weapon.firingRange);
			} //if cruise selected
			return null;
		}

		public override String ToString()
		{
			return (name);
		}
		private float freqInterval()
		{
			float coef = 5 / base.maxSpeed; // one semitone per speed change, clamped at 5 semitones.
			return coef * engineSpeed;
		}

		private void level()
		{
			if (!isAI) {
				if (DXInput.JSDevice != null) {
					if ((virtualNoseAngle != 0)
						&& (!isElevating)) //don't level if autoelevating
						levelOut(); //is the plane coming to a level position?
					return; //do something different if using joystick since there is instant level
				}
			}
			isLeveling = true;
			playRIO(soundPath + "lv1.wav");
			inSplitS = false;
			inBarrelRoll = false;
			rollState = RollState.none;
		}
		private void levelOut()
		{
			virtualNoseAngle = 0;
			isLeveling = false;
			inSplitS = false;
			inBarrelRoll = false;
			rollState = RollState.none;
			if (!isAI) //stop repeated level out messages
				playRIO(soundPath + "lv2.wav"); //if this is autoplay target
		}

		private void playDeathMessage()
		{
			String deathMessage = null;
			if (isAI || Options.mode == Options.Modes.training || Mission.isMission)
				return;

			stopMessages();
			int n = Common.getRandom(1, 6);
			//dl5 is for stalled
			if (n >= 5)
				n = 6;
			if (cause == Interaction.Cause.destroyedByImpact ||
				cause == Interaction.Cause.stalled)
				deathMessage = DSound.SoundPath + "\\dl5.ogg";
			if (cause == Interaction.Cause.destroyedByWeapon)
				deathMessage = DSound.SoundPath + "\\dl" + (n) + ".ogg";

			if (deathMessage == null)
				return;

			playFinalOgg(deathMessage);
		}


		private void switchWeapon()
		{
			if (!requestedLand)
				switchWeapon(weapon.increaseWeaponIndex());

			//Next, if this is a general fighter during
			//Mission Mode, we'll fire the
			//lts every 30 s, so abort here since
			//firing of lts will be handled by
			//fireCruise() method.
			if (isMissionFighter()
				&& weapon.weaponIndex == WeaponTypes.laserCannonSystem)
				switchWeapon(weapon.increaseWeaponIndex());
		}

		private void switchWeapon(WeaponTypes i)
		{
			if (requestedLand)
				return;
			//If gunWaitTime > 0,
			//we just came out of a strafe and we're waiting for the timer to expire.
			if (isAI && i == WeaponTypes.guns && gunWaitTime > 0)
				i = weapon.increaseWeaponIndex();
			weapon.weaponIndex = i;
			if (isAI)
				return;
			DSound.unloadSound(ref pilotMessage);
			pilotMessage = loadSound(soundPath + "w" + (int)weapon.weaponIndex + ".wav");
			playSound(pilotMessage, true, false);
		}

		private bool statusMode(Status s)
		{
			if (Options.mode == Options.Modes.training)
				lastStatusCommand = s;
			SelfVoice.setPathTo("n");
			SapiSpeech.purge();
			switch (s) {
				case Status.missionObjective:
					if (Options.mode == Options.Modes.mission && (int)Mission.missionNumber > 0) {
						Common.playUntilKeyPress(DSound.SoundPath + "\\o" + ((int)Mission.missionNumber - 1) + ".ogg");
						return true;
					}
					return false;
				case Status.facing:
					Common.executeSvOrSr(() => SelfVoice.NLS("i" + (int)facingState + ".wav", true, true), () => SapiSpeech.speak(facingState.ToString()), Options.statusVoiceMode);
					return true;
				case Status.turnRadius:
					if (bankAngle != 0 && !isBankStallConditions())
						Common.executeSvOrSr(() => SelfVoice.NLS("#" + (int)getTurnRadius() + "&feet.wav", true, true), () => SapiSpeech.speak($"{getTurnRadius()} feet"), Options.statusVoiceMode);
					else
						Common.executeSvOrSr(() => SelfVoice.NLS("unknown.wav", true, true), () => SapiSpeech.speak("Unknown"), Options.statusVoiceMode);
					return true;
				case Status.turnRate:
					int rt = (int)Math.Floor(getRateOfTurn());
					Common.executeSvOrSr(() => SelfVoice.NLS("#" + rt + "&" + (rt == 1 ? "dps.wav" : "dsps.wav"), true, true), () => SapiSpeech.speak($"{rt} degree{(rt == 1 ? "":"s")} per second"), Options.statusVoiceMode);
					return true;
				case Status.bankAngle:
					Common.executeSvOrSr(() => SelfVoice.VoiceNumber(bankAngle, true), () => SapiSpeech.speak(""+bankAngle), Options.statusVoiceMode);
					return true;
				case Status.altitudeRate:
					int tr = (int)((fallRate == 0.0f) ? (getVerticalSpeed() / 60) : (fallRate * 60 / 3));
					Common.executeSvOrSr(() => SelfVoice.NLS("#" + tr + "&fpm.wav", true, true), () => SapiSpeech.speak($"{tr} feet per minute"), Options.statusVoiceMode);
					return true;
				case Status.ammunition:
					Common.executeSvOrSr(() => SelfVoice.VoiceNumber(weapon.ammunitionFor(weapon.weaponIndex), true), () => SapiSpeech.speak("" + weapon.ammunitionFor(weapon.weaponIndex)), Options.statusVoiceMode);
					return true;
				case Status.fuel:
					Common.executeSvOrSr(() => SelfVoice.NLS("#" + (int)m_fuelWeight + "&pof.wav", true, true), () => SapiSpeech.speak($"{(int)m_fuelWeight} pounds of fuel"), Options.statusVoiceMode);
					return true;
				case Status.targetIntegrity:
					if (weapon.isValidLock() && !(weapon.getLockedTarget() is LandingBeacon)) {
						Common.executeSvOrSr(() => SelfVoice.NLS("#" + weapon.getLockedTarget().getHealthPercent() + "&p.wav", true, true), () => SapiSpeech.speak($"{weapon.getLockedTarget().getHealthPercent()} percent"), Options.statusVoiceMode);
						return true;
					} else
						return false;
				case Status.integrity:
					Common.executeSvOrSr(() => SelfVoice.NLS("#" + getHealthPercent() + "&p.wav", true, true), () => SapiSpeech.speak($"{getHealthPercent()} percent"), Options.statusVoiceMode);
					return true;
				case Status.engineIntegrity:
					Common.executeSvOrSr(() => SelfVoice.NLS("#" + getEngineDamagePercent() + "&p.wav", true, true), () => SapiSpeech.speak($"{getEngineDamagePercent()} percent"), Options.statusVoiceMode);
					return true;
				case Status.sector:
					Common.executeSvOrSr(() => SelfVoice.NLS(Interaction.getSector((Projector)this), true, true), () => SapiSpeech.speak(Interaction.getSector((Projector)this, false)), Options.statusVoiceMode);
					return true;
				case Status.refuelerCount:
					Common.executeSvOrSr(() => SelfVoice.VoiceNumber(Mission.refuelCount, true), () => SapiSpeech.speak("" + Mission.refuelCount), Options.statusVoiceMode);
					return true;
				case Status.lap:
					if (Options.mode == Options.Modes.racing)
						Common.executeSvOrSr(() => SelfVoice.VoiceNumber(lap, true), () => SapiSpeech.speak(""+lap), Options.statusVoiceMode);
					return true;
				case Status.distance:
					string msgstr = "";
					int ds = (int)Math.Floor(currentDistance);
					Common.executeSvOrSr(() =>
					{
						msgstr += "#" + ds + "&";
						if (ds == 1)
							msgstr += "mi.wav";
						else
							msgstr += "mis.wav";
						SelfVoice.NLS(msgstr, true, true);
					}, () => SapiSpeech.speak($"{ds} mile{((ds==1)?"":"s")}"), Options.statusVoiceMode);
					return true;
				case Status.altimeter:
					String alt = Common.cultureNeutralRound(z, 1);
					Common.executeSvOrSr(() => SelfVoice.NLS("#" + alt + "&" + "feet.wav", true, true), () => SapiSpeech.speak($"{alt} feet"), Options.statusVoiceMode);
					sayRelative = false;
					return true;
				case Status.course:
					Common.executeSvOrSr(() => SelfVoice.VoiceNumber(direction, true), () => SapiSpeech.speak("" + direction), Options.statusVoiceMode);
					sayRelative = false;
					return true;
				case Status.rank:
					if (Options.mode == Options.Modes.racing && cause != Interaction.Cause.sentForLanding) {
						Common.executeSvOrSr(() => SelfVoice.NLS("yai.wav" + "&" + Common.convertToWordNumber(getRank()) + ".wav" + "&" + "pl.wav", true, true), () => SapiSpeech.speak($"You are in {Common.convertToWordNumber(getRank()) } place"), Options.statusVoiceMode);
						return true;
					} else
						return false;
				case Status.speedometer:
					String airspeed = Common.cultureNeutralRound(Common.convertToKNOTS(getHorizontalSpeed()), 1);
					Common.executeSvOrSr(() =>
					{
						SelfVoice.NLS("as.wav&#" + airspeed
						+ "&knots.wav"
						+ "&es.wav&#"
						+ rpm + "&rpm.wav",
						true, true);
					}, () => SapiSpeech.speak($"Airspeed: {airspeed} knots; Engine speed: {rpm} rpm"), Options.statusVoiceMode);
					return true;
				case Status.angleOfAttack:
					Common.executeSvOrSr(() => SelfVoice.VoiceNumber(virtualNoseAngle, true), () => SapiSpeech.speak("" + virtualNoseAngle), Options.statusVoiceMode);
					return true;
				case Status.target:
					if (weapon.isValidLock()) {
						RelativePosition position = getPosition(weapon.getLockedTarget());
						Common.executeSvOrSr(() =>
						{
							string n = (Options.isPlayingOnline) ? "ta.wav&" : (weapon.getLockedTarget().name + ".wav&");
							SelfVoice.NLS(n + ((Options.isPlayingOnline) ? "" : "at.wav&") + position.ToString()
							+ "&" + Interaction.getSector(weapon.getLockedTarget()),
							true, true);
						}, () =>
						{
							position.sapiMode = true;
							SapiSpeech.speak($"{Common.getFriendlyNameOf(weapon.getLockedTarget().ToString())} {position}, sector: {Interaction.getSector(weapon.getLockedTarget(), false)}");
						}, Options.statusVoiceMode);
						if (Options.RPAutoTrigger)
							sayRelative = true;
						return true;
					} //if validLock
					return false;
			} //switch
			return false;
		}

		private void targetSolution()
		{
			if (!weapon.isValidLock() || isOnRunway) {
				if (DSound.isPlaying(targetSolutionSound))
					targetSolutionSound.stop();
				if (DSound.isPlaying(targetSolutionSound3))
					targetSolutionSound3.stop();
				return;
			}

			if (weapon.weaponIndex == WeaponTypes.missileInterceptor) {
				targetSolutionSound.stop();
				if (weapon.getInterceptorLock() != null)
					playSound(targetSolutionSound3, false, true);
				else
					targetSolutionSound3.stop();
				return;
			}

			if (weapon.weaponIndex == WeaponTypes.cruiseMissile) {
				targetSolutionSound.stop();
				if (weapon.cruiseMissileLocked()) {
					if (weapon.inFiringRange()) {
						playSound(targetSolutionSound3, false, true);
						if (Options.mode == Options.Modes.training && currentStage == TrainingStages.solidToneOnFighter1)
							completedTrainingStage = true;
					} else
						targetSolutionSound3.stop();
				} else //if toggled to cruise missile but not locked
					targetSolutionSound3.stop();
				return;
			} //if cruise missile

			if (weapon.isValidLock()) {
				if (!weapon.inFiringRange()) {
					targetSolutionSound.stop();
					targetSolutionSound3.stop();
					return;
				}

				Projector t = weapon.getLockedTarget();
				RelativePosition p = getPosition(t);
				if (p.isAhead) {
					if (p.degreesDifference > 5) {
						targetSolutionSound3.stop();
						float tx = x;
						float tY = y;
						Degrees.moveObject(ref tx, ref tY, p.degrees, 1f, 1f);
						targetSolutionSound.setFrequency(targetSolutionFreqCoefficient*p.degreesDifference);
						DSound.PlaySound3d(targetSolutionSound, false, true, tx, (pov == PointOfView.interior) ? z : 0f, tY, flags: SharpDX.X3DAudio.CalculateFlags.Matrix);
					} else { //if degree difference==0
						targetSolutionSound.stop();
						playSound(targetSolutionSound3, false, true);
						if (Options.mode == Options.Modes.training && currentStage == TrainingStages.solidToneOnFighter1)
							completedTrainingStage = true;
					} //if degrees difference=0
				} else { //if ! ahead
					targetSolutionSound.stop();
					targetSolutionSound3.stop();
				} //if ahead
			} //if valid lock
		}

		private void soundLowSpeedAlarm()
		{
			if (isPartialStallCondition() && !isFalling) {
				if (!firstLaunch && !DSound.isPlaying(lowSpeedAlarm)) {
					playRIO(soundPath + "alarm4m"
						+ Common.getRandom(1, 2) + ".wav", false);
				} //if lowspeed alarm not playing
				if (!DSound.isPlaying(lowSpeedAlarm))
					playSound(lowSpeedAlarm, false, true);
			} else { // if not dangerously low
				lowSpeedAlarm.stop();
			} //if gear retracted and below harddeck
		}

		private void fall()
		{
			z += fallRate;
			fallRate -= (float)(9.8 * Common.intervalMS / 1000);
			if (!isAI) {
				muteAlarms(false); //don't stop stall alarm!
				if (!DSound.isPlaying(fallAlarm)) {
					playRIO(soundPath + "hc.wav");
					playSound(fallAlarm, false, true);
				} //if not looping
			} //if not AI
		}

		protected virtual void muteEngines()
		{
			if (engine != null)
				engine.stop();
			if (jetRumble != null)
				jetRumble.stop();
		}
		private void muteAlarms(bool stopStallAlarm)
		{
			if (isAI && !autoPlayTarget) {
				return;
			}
			if (!isFalling) altitudeWarningAlarm.stop();
			lowSpeedAlarm.stop();
			targetSolutionSound.stop();
			targetSolutionSound3.stop();
			if (stopStallAlarm)
				fallAlarm.stop();
			turnSignal.stop();
			lockAlertSound.stop();
			lowFuelAlarm.stop();
			selfDestAlarm.stop();
		}

		private void muteAlarms()
		{
			muteAlarms(true);
		}

		//Defines the target degrees for a lock
		//Calling method MUST check to see if we have valid lock first.
		private int targetDegrees()
		{
			RelativePosition p = getPosition(weapon.getLockedTarget());
			return (p.degrees);
		}

		private bool successfulLanding(bool aircraftCarrier)
		{
			if (facingState == FacingState.inverted)
				return false;
			if (!aircraftCarrier
				&& speed <= 400.0 && virtualNoseAngle >= -30 && !isLandingGearRetracted)
				return true;
			if (aircraftCarrier
				&& speed <= 700.0 && !isLandingGearRetracted)
				return true;
			return false;
		}

		protected override void performDeaths()
		{
			if (!isAI) {
				if (selfDestructTime / 1000 / 60 >= 2) {
					selfDestruct();
					return;
				}
				if (tickDemoTimer())
					return;
				if (damageEngine()) {
					hit(damage, Interaction.Cause.engineDestroyed);
					return;
				}
			} //if! AI

			//If abortGame is true, don't die() since player has already hooked kill method and is waiting for end game.
			if (isAI && !Options.abortGame && Interaction.playerFinishedRace()) {
				hit(0, Interaction.Cause.lostRace);
				System.Diagnostics.Trace.WriteLine("Race finished by player, " + name + " terminating.");
				return;
			}

			if (z >= maxAltitude) {
				hit(damage, Interaction.Cause.engineDestroyed);
				return;
			}

			if (landingOnCarrier
					&& !successfulLanding(true) && collidesWith(Mission.carrier))
				hit(damage, Interaction.Cause.destroyedByWeapon); //fireball explosion

			if (z <= 0.0 && !isOnRunway) {
				if (isFalling) {
					isFalling = false;
					hit(damage, Interaction.Cause.stalled);
					return;
				}

				if (successfulLanding(false)) {
					if (!requestedLand) {
						hit(damage, Interaction.Cause.destroyedByImpact);
						return;
					}

					if (Degrees.getDistanceBetween(x, y,
						Mission.landingBeacon.x, Mission.landingBeacon.y) <= 5.0) {
						isOnRunway = true;
						hit(0, Interaction.Cause.successfulLanding);
					} else //if more than 5 miles from beacon
						hit(damage, Interaction.Cause.destroyedByImpact);
				} //successful landing
				else //if doesn't even appear to be landing...
					hit(damage, Interaction.Cause.destroyedByImpact);
			} //if on ground.

			if (isInTurn) {
				if (Degrees.getDifference(currentStraightaway.direction,
					direction) <= minClearDeg) {
					isInTurn = false;
					moveToNextStraightaway();
				} //if cleared turn
			}//if in turn

			if (outOfFuel())
				turnOffEngine();
			if (enginesOff && !outOfFuel() && bankAngle != 90)
				turnOnEngine();
		}

		private void fadeEngines()
		{
			if (!isOnRunway) {
				float v = jetRumble.getVolume();
				if (v < engineFadeInThreshold) {
					jetRumble.setVolume(v + 0.01f);
				}
			}
		}



		/// <summary>
		/// The following method overrides accelerate()
		///in the Projector class.
		///This is necessary to implement jet engine starting sounds.
		/// </summary>
		/// <param name="accelerationSpeed">The value to accelerate by</param>
		/// <returns></returns>
		public override bool accelerate(int accelerationSpeed)
		{
			if (isBraking)
				return false;
			return base.accelerate(accelerationSpeed);
		}

		/// <summary>
		/// Overloaded. Uses default acceleration value.
		/// </summary>
		/// <returns></returns>
		public override bool accelerate()
		{
			return accelerate(accelerationSpeed);
		}

		/// <summary>
		///  Lets the player know that an enemy has a full or partial lock on them.
		/// </summary>
		public void lockAlert()
		{
			if (isAI)
				return;
			if (isTerminated)
				return;
			lock (lockAlertSound) {
				if (!DSound.isPlaying(lockAlertSound)) {
					if (Common.getRandom(1, 100) < 5) {
						playRIO(soundPath + "danger1.wav", false);
					}
					playSound(lockAlertSound, true, true);
				}
			}
		}

		/// <summary>
		///  Stops the lock alert.
		/// </summary>
		public void stopLockAlert()
		{
			if (isTerminated) {
				return;
			}
			if ((lockAlertSound != null)) {
				lock (lockAlertSound) {
					lockAlertSound.stop();
				}
			}
		}

		/// <summary>
		///  Notifies the player object that a lock has been obtained.
		/// </summary>
		private void notifyLock()
		{
			if (Options.mode == Options.Modes.training)
				return;
			if (weapon.isValidLock()) {
				if (!(weapon.getLockedTarget() is Aircraft)) {
					return;
				}
				//no need to set lock alerts if the target
				//isn't an aircraft the player
				//can fly.

				if (damage <= 0 || isTerminated) {
					((Aircraft)weapon.getLockedTarget()).stopLockAlert();
					//fixes bug where if this craft was destroyed,
					//lock alert alarm would not stop. This was because it's up to the locking craft to issue the alarm and to quiet it.
					return;
				}
				if (getPosition(weapon.getLockedTarget()).isAhead && weapon.inFiringRange()) {
					((Aircraft)weapon.getLockedTarget()).lockAlert();
				} else {
					((Aircraft)weapon.getLockedTarget()).stopLockAlert();
				}
			}
		}

		private void afterBurners()
		{
			if (!afterburnersActive)
				return;
			if (!isAI)
				DXInput.startAfterburnerEffect();
			if (!enginesOff) {
				if (DSound.isPlaying(afStart)) {
					// Shift it in 3d space.
					playSound(afStart, false, false);
				} //if afflame!looping
				else {
					if (DSound.isPlaying(afFlame)) {
						playSound(afFlame, false, true); //shift it
					}
				}
				if (isAI)
					return;
				//AI will deactivate afterburners through the
				//deactivateAfterburners method.
				if ((throttlePosition == maxThrottlePosition)
					|| (DXInput.isJSButtonHeldDown((int)KeyMap.getKey(Action.activateAfterburners, true)[1])
					|| (DXInput.isKeyHeldDown((Key)KeyMap.getKey(Action.activateAfterburners)[1]))
					))
					return; //keep the afterburners going.
			} //if !enginesOff

			afStart.stop();
			afFlame.stop();
			playSound(afEnd, true, false);
			afterburnersActive = false;
			if (!isAI)
				DXInput.stopAfterburnerEffect();
		}

		private void activateAfterburners()
		{
			if (afterburnersActive)
				return;
			if (enginesOff)
				return;
			if (DSound.isPlaying(afEnd))
				afEnd.stop();
			playSound(afStart, true, false);
			afterburnersActive = true;
			if (!isAI)
				DXInput.startAfterburnerEffect();
		}

		private void deactivateAfterburners()
		{
			afterburnersActive = false;
			afStart.stop();
			afFlame.stop();
			playSound(afEnd, true, false);
		}

		private void turnLeft(bool transfer)
		{
			if (!transfer && facingState == FacingState.inverted) {
				turnRight(true);
				return;
			}
			if (navigatingToSector())
				stopSectorNav();
			if (isAI)
				turnLeft();
			if (!isAI) {
				int pDir = direction;
				if (DXInput.JSDevice == null)
					turnLeft();
				else
					turn((facingState == FacingState.inverted) ? -DXInput.JSRZAxis() : DXInput.JSRZAxis());
				if (pDir != direction)
					speakCourseChange();
			}
			if (isInTurn && Degrees.getShortestRotation(this.direction, currentStraightaway.direction) != Degrees.Rotation.counterClockwise)
				brake();
		}
		private void turnRight(bool transfer)
		{
			if (!transfer && facingState == FacingState.inverted) {
				turnLeft(true);
				return;
			}
			if (navigatingToSector())
				stopSectorNav();
			if (isAI)
				turnRight();
			if (!isAI) {
				int pDir = direction;
				if (DXInput.JSDevice == null)
					turnRight();
				else
					turn(facingState == FacingState.inverted ? -DXInput.JSRZAxis() : DXInput.JSRZAxis());
				if (pDir != direction)
					speakCourseChange();
			}
			if (isInTurn && Degrees.getShortestRotation(this.direction, currentStraightaway.direction) != Degrees.Rotation.clockwise)
				brake();
		}

		public override void bankLeft()
		{
			if (navigatingToSector())
				stopSectorNav();
			base.bankLeft();
			if (Options.mode == Options.Modes.training && currentStage != TrainingStages.splitS && currentStage != TrainingStages.splitS2 && currentStage != TrainingStages.barrelRoll && currentStage != TrainingStages.loop && currentStage != TrainingStages.killFighter3 && bankAngle < -50)
				bankAngle = -50;
		}

		public override void bankRight()
		{
			if (navigatingToSector())
				stopSectorNav();
			base.bankRight();
			if (Options.mode == Options.Modes.training && currentStage != TrainingStages.splitS && currentStage != TrainingStages.splitS2 && currentStage != TrainingStages.barrelRoll && currentStage != TrainingStages.loop && currentStage != TrainingStages.killFighter3 && bankAngle > 50)
				bankAngle = 50;
		}

		//Will accelerate or decelerate the craft
		//based on the position of the throttle
		private void throttle()
		{
			if (!enginesOff) {
				if (!isAI && DXInput.JSDevice != null) {
					if (DXInput.useSlider)
						throttlePosition = DXInput.JSSlider();
					else
						throttlePosition = DXInput.JSZAxis();
					if (throttlePosition < minThrottlePosition)
						throttlePosition = minThrottlePosition;
					else if (throttlePosition > maxThrottlePosition)
						throttlePosition = maxThrottlePosition;
				} //if !AI and joystick enabled

				//if the burners are active, increase engine
				//rpm to maximum rpm
				if (afterburnersActive) {
					if (rpm < Projector.maxRPM)
						rpm += 100; //increase engine sspeed
									//which will increase airspeed
				} else {
					if (rpm > throttlePosition * throttleSpan)
						rpm -= 100;
					else
						rpm = throttlePosition * throttleSpan;
				}
			} //if enginesOff            
			matchSpeed = engineSpeed;
			if (matchSpeed > maxSpeed) {
				matchSpeed = maxSpeed;
			}
			if (matchSpeed < 0) {
				matchSpeed = 0;
			}

			if (speed < matchSpeed) {
				accelerate();
				if (speed > matchSpeed)
					speed = matchSpeed;
			}
			if (speed > matchSpeed) {
				decelerate();
				if (speed < matchSpeed)
					speed = matchSpeed;
			}
		}

		private void resetActions()
		{
			actionsArray.Clear();
		}

		private void startSelfDestruct()
		{
			if (Options.mode == Options.Modes.mission) {
				bool uncharted = Interaction.getSector(this).Equals("sec.wav");
				if ((uncharted && !weapon.isValidLock())
					|| (uncharted && weapon.isValidLock()
					&& getPosition(weapon.getLockedTarget()).distance >= 10.0)) {
					if (selfDestructTime == 0) { //if first time
						if (ATCMessage != null)
							ATCMessage.stopOgg();
						ATCMessage = DSound.loadOgg(DSound.SoundPath + "\\sd1.ogg");
						ATCMessage.play();
						selfDestructTime = 1; //start it
					} else //already started, so just tick the timer
						selfDestructTime += Common.intervalMS;
				} else //if not in uncharted
					selfDestructTime = 0; //stop and reset timer

				if (selfDestructTime >= 1
					&& !DSound.isPlaying(selfDestAlarm))
					playSound(selfDestAlarm, false, true);
				else if (selfDestructTime == 0
					&& DSound.isPlaying(selfDestAlarm))
					selfDestAlarm.stop();
			} //if mission mode
		}

		private void selfDestruct()
		{
			hit(damage, Interaction.Cause.selfDestructed);
		}

		public void startAtHeight(float z)
		{
			this.z = z;
			isOnRunway = false;
			isLandingGearRetracted = true;
			speed = 450;
		}

		private void togglePointOfView()
		{
			if (pov == PointOfView.interior) {
				pov = PointOfView.exterior;
			} else {
				pov = PointOfView.interior;
			}
			DSound.unloadSound(ref pilotMessage);
			pilotMessage = loadSound(soundPath + "p" + (int)pov + ".wav");
			playSound(pilotMessage, true, false);
		}
		private void playMissionObjective0()
		{
			played0 = true;
			Interaction.advanceToNextMission(this);
		}

		private void playMissionObjective1()
		{
			played1 = true;
			Interaction.advanceToNextMission(this);
		}

		private void playMissionObjective2()
		{
			played2 = true;
			Interaction.advanceToNextMission(this);
		}

		private void playMissionObjective3()
		{
			missileWarnTime += Common.intervalMS;
			if (missileWarnTime / 1000 >= 10) {
				played3 = true;
				missileWarnTime = 0; //In case another objective wants to use it.
				Interaction.advanceToNextMission(this);
			}
		}


		private void playMissionObjective6()
		{
			played6 = true;
			Interaction.advanceToNextMission(this);
		}

		private void tickMissionCounter()
		{
			if (Options.mode == Options.Modes.mission && Mission.missionNumber == Mission.Stage.juliusRadioIntercept) {
				if (missionCounter == -1)
					missionCounter = Environment.TickCount;

				if (Environment.TickCount - missionCounter >= 30000)
					playMissionObjective6();
			}
		}

		public override bool readyToTerminate()
		{
			if (requestedLand)
				return false; //don't terminate if landing.
			if (destroyed != null && DSound.isPlaying(destroyed))
				return false;
			System.Diagnostics.Trace.WriteLine($"ready to terminate for {name} is {base.readyToTerminate()}");
			return base.readyToTerminate();
		}

		/// <summary>
		/// Pauses the game.
		/// </summary>
		private void pauseGame()
		{
			if (Options.isPlayingOnline)
				return;
			Options.isPaused = true;
			Common.music.stopOgg();
			Common.startMusic(DSound.SoundPath + "\\ms5.ogg");
			Interaction.stopAndMute(true, true);
			while (!checkCommand(Action.pauseGame, true)) {
				DXInput.updateKeyboardState();
				Thread.Sleep(100);
			}
			Common.music.stopOgg();
			Common.startMusic();
			Options.isPaused = false;
			Interaction.resumeAndUnmute();
		}

		private bool lockOnPlayer()
		{
			if (!isAI)
				return false;
			if (weapon.isValidLock())
				return true;
			if (Mission.isMission) {
				if (Options.mode == Options.Modes.racing && Mission.racingScore >= Mission.passingRacingScore - 10 && Weapons.isValidLock(Mission.player)) {
					weapon.lockIndex = Mission.player.id;
					return true;
				}

				if (Options.mode == Options.Modes.deathMatch && Mission.deathMatchScore >= Mission.passingDeathMatchScore - 10 && Weapons.isValidLock(Mission.player)) {
					weapon.lockIndex = Mission.player.id;
					return true;
				}
			} //if Mission
			return false;
		}

		private void stopSAPI()
		{
			SelfVoice.purge(true);
		}

		private void speakVDistanceToTarget()
		{
			if (Options.verticalRangeAnnounceTime == 0 || virtualNoseAngle == 0)
				return;
			if (Environment.TickCount - speakVDTime
				>= Options.verticalRangeAnnounceTime) {
				if (sayRelative && weapon.isValidLock()) {
					RelativePosition r = getPosition(weapon.getLockedTarget());
					String name = weapon.getLockedTarget().name;
					float hd = r.vDistance;
					string w = null;
					if (hd < 0.0)
						w = (Options.isPlayingOnline) ? "abt" : "above";
					else if (hd > 0.0)
						w = (Options.isPlayingOnline) ? "bt" : "below";
					else
						return;
					if (!SelfVoice.setPathTo("n", false))
						return;
					w += ".wav";
					SelfVoice.NLS("#"
					 + Math.Abs((int)hd)
					 + "&feet.wav&"
										  + w + ((Options.isPlayingOnline) ? "" : ("&" + name + ".wav")),
					 true, true);
				} //if validlock and saypos
				else {
					if (!SelfVoice.setPathTo("n", false))
						return;
					Common.executeSvOrSr(() =>
					{
						SelfVoice.NLS("#" + (int)z
						+ "&"
						+ "feet.wav",
						true, true);
					}, () =>
					{
						SapiSpeech.speak($"{(int)z} feet");
					}, Options.statusVoiceMode);
				}
				speakVDTime = Environment.TickCount;
			} //if time to say something
		}

		private void unloadSounds()
		{
			System.Diagnostics.Trace.WriteLine("Unloading engine for " + name);
			DSound.unloadSound(ref engine);
			DSound.unloadSound(ref windSound);
			DSound.unloadSound(ref destroyed);
			DSound.unloadSound(ref pilotMessage);
			DSound.unloadSound(ref RIOMessage);
			DSound.unloadSound(ref jetRumble);
			DSound.unloadSound(ref afStart);
			DSound.unloadSound(ref afFlame);
			DSound.unloadSound(ref afEnd);
			DSound.unloadSound(ref enginesLaunch);
			DSound.unloadSound(ref enginesLand);
			DSound.unloadSound(ref landingGear);
			DSound.unloadSound(ref landingGearOut);
			DSound.unloadSound(ref ltsHit);
			DSound.unloadSound(ref aileronRollSound);
			DSound.unloadSound(ref barrelRollSound);
			DSound.unloadSound(ref catapultSound);
			DSound.unloadSound(ref lowFuelAlarm);
			DSound.unloadSound(ref radarSound);
			DSound.unloadSound(ref lockBrokenSound);
			DSound.unloadSound(ref lockAlertSound);
			DSound.unloadSound(ref fallAlarm);
			DSound.unloadSound(ref lowSpeedAlarm);
			DSound.unloadSound(ref targetSolutionSound);
			DSound.unloadSound(ref targetSolutionSound3);
			DSound.unloadSound(ref altitudeWarningAlarm);
			DSound.unloadSound(ref turnSignal);
			DSound.unloadSound(ref selfDestAlarm);
			DSound.unloadSound(ref throttleClickSound);
			DSound.unloadSound(ref courseClickSound);
		}

		public override void freeResources()
		{
			//If requestingSpectator is true, this means that this craft is requested by someone else to be watched,
			// so in this case we shouldn't wait for termination.
			if (!requestingSpectator) {
				while (!readyToTerminate())
					Thread.Sleep(10);
			}

			base.freeResources();
			unloadSounds();
			weapon.freeResources();
			if (!isAI && !Options.initializingLoad)
				terminateInput();
			if (!requestingSpectator)
				isProjectorStopped = true;
		}

		//if force is false, the course will only be spoken
		//if it is divisible by ten
		//else, it will be spoken no matter what.
		//for instance, when turning the course ia nnounced
		//even ten degrees.
		//however when banking it will be announced after every bank
		private void speakCourseChange(bool force)
		{
			if (!Options.announceCourseChange)
				return;

			if ((direction % 10 == 0)
				|| (force)) {
				if (sayRelative && weapon.isValidLock()) {
					if (!SelfVoice.setPathTo("n", false))
						return;
					SelfVoice.NLS(
						getPosition(
						weapon.getLockedTarget()
						).clockMark
						+ "o.wav",
						true, true);
				} else {
					if (!SelfVoice.setPathTo("n", false))
						return;
					SelfVoice.VoiceNumber(direction, true);
				} //if don't speak relative
			} //if not force
		}

		private void speakCourseChange()
		{
			speakCourseChange(false);
		}

		private void optionsMenu()
		{
			pauseInput();
			Interaction.stopAndMute(false);
			bool exitCode = false;
			string[] options = Common.returnSvOrSr(() => new string[]{ "o_1.wav",
				"o_2.wav",
				"o_3.wav",
				"o_7.wav",
				(Options.mode != Options.Modes.mission || Options.isDemo) ?
				"" : "o_4.wav",
				(Options.mode != Options.Modes.mission || Options.isDemo) ?
				"" : "o_5.wav",
				"o_6.wav"
			}, () => new string[]{"Vertical range announcements",
				"Course change announcements",
				"Relative position auto trigger",
				"RIO announcements",
				(Options.mode != Options.Modes.mission || Options.isDemo) ?
				"" : "Save game",
				(Options.mode != Options.Modes.mission || Options.isDemo) ?
				"" : "Load game",
				"Exit"
			}, Options.menuVoiceMode);
			int mIndex = 0;
			while (!exitCode) {
				mIndex = Common.returnSvOrSr(() => Common.sVGenerateMenu("o_i.wav", options, mIndex), () => Common.GenerateMenu("Ok Orion, here are your options.", options, mIndex), Options.menuVoiceMode);
				switch (mIndex) {
					case -1:
						exitCode = true;
						break;
					case 0: //interval for vertical announce
						string[] iOptions = Common.returnSvOrSr(() => new string[]{ "off.wav",
							"o_1_5.wav",
							"o_1_10.wav",
							"o_1_15.wav",
							"o_1_20.wav",
							"o_1_25.wav",
							"o_1_30.wav"
						}, () => new String[]{"Off",
							"5 seconds",
							"10 seconds",
							"15 seconds",
							"20 seconds",
							"25 seconds",
							"30 seconds"
						}, Options.menuVoiceMode);
						int nIndex = Common.returnSvOrSr(() => Common.sVGenerateMenu("", iOptions, Options.verticalRangeAnnounceTime / 5 / 1000), () => Common.GenerateMenu(null, iOptions, Options.verticalRangeAnnounceTime / 5 / 1000), Options.menuVoiceMode);
						if (nIndex == -1)
							break;
						Options.verticalRangeAnnounceTime = nIndex * 5 * 1000;
						break;

					case 1: //set courseChange announcement
						Common.executeSvOrSr(() =>
						{
							SelfVoice.NLS(DSound.NSoundPath + "\\"
							+ Common.getOnOffStatus(Options.announceCourseChange = !Options.announceCourseChange)
							+ ".wav", true);
						}, () =>
						{
							SapiSpeech.speak(Common.getOnOffStatus(Options.announceCourseChange = !Options.announceCourseChange));
						}, Options.menuVoiceMode);
						break;

					case 2:
						Common.executeSvOrSr(() =>
						{
							SelfVoice.NLS(DSound.NSoundPath + "\\"
							+ Common.getOnOffStatus(Options.RPAutoTrigger = !Options.RPAutoTrigger)
							+ ".wav", true);
						}, () =>
						{
							SapiSpeech.speak(Common.getOnOffStatus(Options.RPAutoTrigger = !Options.RPAutoTrigger), SapiSpeech.SpeakFlag.interruptable);
						}, Options.menuVoiceMode);
						break;
					case 3: // Toggle RIO announcements
						Common.executeSvOrSr(() =>
						{
							SelfVoice.NLS(DSound.NSoundPath + "\\"
							+ Common.getOnOffStatus(Options.playRIO = !Options.playRIO)
							+ ".wav", true);
						}, () =>
						{
							SapiSpeech.speak(Common.getOnOffStatus(Options.playRIO = !Options.playRIO), SapiSpeech.SpeakFlag.interruptable);
						}, Options.menuVoiceMode);
						break;

					case 4: //save
						int slotSave = Common.returnSvOrSr(() => Common.sVGenerateMenu("", new String[] {DSound.NumPath + "\\1.wav", DSound.NumPath + "\\2.wav", DSound.NumPath + "\\3.wav"}), () => Common.GenerateMenu("", new string[] { "1", "2", "3" }), Options.menuVoiceMode)
							+ 1;
						if (slotSave == 0)
							break;
						Common.saveGame(slotSave);
						break;

					case 5: //load game
						int slotLoad = Common.returnSvOrSr(() => Common.sVGenerateMenu("", new String[] {DSound.NumPath + "\\1.wav", DSound.NumPath + "\\2.wav", DSound.NumPath + "\\3.wav"}), () => Common.GenerateMenu("", new string[] { "1", "2", "3" }), Options.menuVoiceMode)
							+ 1;
						if (slotLoad == 0)
							break;
						Common.loadGame(slotLoad);
						exitCode = true;
						break;

					default: //exit
						exitCode = true;
						break;
				} //switch
			} //while
			Options.writeToFile();
			SapiSpeech.playOrSpeakMenu(DSound.NSoundPath + "\\o_c.wav", "Settings updated");
			Interaction.resumeAndUnmute();
			resumeInput();
		}

		private void updateOpenPosition()
		{
			int cPercent = (int)((throttlePosition / (double)maxThrottlePosition) * 100);
			if (cPercent >= 25 && cPercent < 100) {
				if (cPercent <= 50)
					openPosition = OpenPositions.oneQuarter;
				else if (cPercent <= 75)
					openPosition = OpenPositions.oneHalf;
				else if (cPercent < 100)
					openPosition = OpenPositions.threeQuarters;
			} else {
				if (cPercent == 100)
					openPosition = OpenPositions.full;
				else
					openPosition = OpenPositions.closed;
			}
		}

		private void playThrottleClick()
		{
			if (lastOpenPosition!=openPosition) {
				float freq = (float)openPosition;
				throttleClickSound.setFrequency(freq);
				playSound(throttleClickSound, true, false);
			}
		}

		private void throttleUp()
		{
			if (throttlePosition == maxThrottlePosition)
				return;
			throttlePosition++;
			if (throttlePosition > maxThrottlePosition)
				throttlePosition = maxThrottlePosition;
		}

		private void throttleDown()
		{
			if (throttlePosition == minThrottlePosition)
				return;
			throttlePosition--;
			if (throttlePosition < minThrottlePosition)
				throttlePosition = minThrottlePosition;
		}

		//Will be called by player-initiated autoelevate action.
		private void autoelevation()
		{
			if (isElevating) {
				isElevating = false;
				levelOut();
				checkForAltitude = false;
				return;
			}
			if (!weapon.isValidLock())
				return;
			elevationAltitude = getPosition(weapon.getLockedTarget()).vDistance;
			if (elevationAltitude == 0.0)
				return;

			if (elevationAltitude < 0.0)
				virtualNoseAngle = -45;
			else
				virtualNoseAngle = 45;

			isElevating = true;
			checkForAltitude = true;
			playRIO(soundPath + "ae.wav");
		}

		//This method will be called on every tick to align the altitude.
		private void autoelevate()
		{
			if (!isElevating)
				return;
			if (!weapon.isValidLock()) {
				isElevating = false;
				checkForAltitude = false;
				levelOut();
				return;
			}
			Projector t = weapon.getLockedTarget();
			//this method has been run before.
			//and the craft has ticked and moved.
			//so check if it is close enough to align it.
			if (checkForAltitude) {
				checkForAltitude = false;
				if (elevationAltitude < 0.0
					&& z < t.z) //craft just passed target.
					z = t.z;
				if (elevationAltitude > 0.0
					&& z > t.z) //craft just passed target.
					z = t.z;
				if (z == t.z) {
					isElevating = false;
					levelOut();
					return;
				} //if aligned to target
			} //if checkfor
			elevationAltitude = getPosition(weapon.getLockedTarget()).vDistance;
			if (!checkForAltitude)
				checkForAltitude = true;
		}

		private void playWindSound()
		{
			if (speed >= 150.0) {
				if (!DSound.isPlaying(windSound)) {
					playSound(windSound, false, true);
				} //if !looping
				windSound.setFrequency(windFreqInterval());
				float v = windThreshold + (float)speed * windVolumeIncrement; 
				if (v > 1.0f)
					v = 1.0f;
				windSound.setVolume(v);
			} //if speed>=150
			else { //if speed <150
				windSound.stop();
			}
		}

		private float windFreqInterval()
		{
			return (float)speed * windFreqIncrement;
		}

		private bool isStallCondition()
		{
			if (isAI || isOnRunway)
				return false;
			if (Math.Abs(virtualNoseAngle) >= 85 && speed < 500.0)
				return true;
			if (Math.Abs(virtualNoseAngle) > 50 && speed <= 400.0)
				return true;
			if (speed <= 120.0)
				return true;
			return false;
		}
		private bool isBankStallConditions()
		{
			return ((Math.Abs(bankAngle) > 80 && speed < 400.0) || getHorizontalSpeed() <= 50.0);
		}

		private bool isPartialStallCondition()
		{
			if (isStallCondition())
				return false;
			if (isAI || isOnRunway)
				return false;
			//if climbing too steeply and too slowly which won't produce enough lift,
			//or diving too steeply and too slowly which won't produce enough lift,
			if (Math.Abs(virtualNoseAngle) > 35 && speed <= 400.0)
				return true;
			if (Math.Abs(bankAngle) > 60 && speed < 400.0)
				return true;
			if (speed <= 300.0)
				return true;
			return false;
		}

		private void stopMessages()
		{
			if (ATCMessage != null)
				ATCMessage.stopOgg();
			if (RIOMessage != null)
				RIOMessage.stop();
			if (pilotMessage != null)
				pilotMessage.stop();
		}

		private void changeRumbleVolume()
		{
			float vol = jetRumble.getVolume();
			if (vol < engineFadeInThreshold)
				return; //still fading in.
			vol = engineFadeInThreshold + (float)speed * rumbleVolumeIncrement;
			jetRumble.setVolume(vol);
		}

		private void moveToSector()
		{
			pauseInput();
			String letter = "";
			String number = "";
			SelfVoice.setPathTo("n");
			Interaction.stopAndMute(false);
			while (DXInput.isKeyHeldDown())
				Thread.Sleep(5);
			SelfVoice.NLS("ens.wav", false);
			String s = Common.mainGUI.receiveInput();
			if (s.Equals("")) {
				Interaction.resumeAndUnmute();
				resumeInput();
				return;
			}
			char[] sectorChars = s.ToCharArray();
			int index = 0;
			char prev = '\0';
			while (
				!(sectorChars[index] < '9'
				&& sectorChars[index] > '0')) {
				letter += sectorChars[index];
				if (prev != '\0') {
					if (prev != 'z' && sectorChars[index] == 'z') {
						SelfVoice.NLS("is.wav", false);
						Interaction.resumeAndUnmute();
						resumeInput();
						return;
					} //if prev char is not z but this one is
				} //if we have a prev char
				  //ie: not first loop
				prev = sectorChars[index];
				index++;
				if (index >= sectorChars.Length) {
					//if we didn't find any numbers
					SelfVoice.NLS("is.wav", false);
					Interaction.resumeAndUnmute();
					resumeInput();
					return;
				}
			} //crawl letters

			//Next, if player didn't enter any letters...
			if (prev == '\0') {
				SelfVoice.NLS("is.wav", false);
				Interaction.resumeAndUnmute();
				resumeInput();
				return;
			}

			letter = letter.ToLower();
			for (; index < sectorChars.Length; index++) {
				//if player entered sector like this:
				//za9e, error.
				if ((sectorChars[index] < '0')
					|| (sectorChars[index] > '9')) {
					SelfVoice.NLS("is.wav", false);
					Interaction.resumeAndUnmute();
					resumeInput();
					return;
				}
				number += sectorChars[index];
			}
			char[] letters = letter.ToCharArray();
			//next, get the coordinates of each designation
			int hs = 0;
			int vs = 0;

			String sec = Common.sectorToString(letter + "," + number);
			//first, the horizontal coordinate
			//since each sector is 10 miles wide, each letter represents
			//a ten mile span
			foreach (char c in letters)
				hs += (26 - ('z' - c)) * 10;
			//the vertical value is simply a multiple of ten
			vs = Convert.ToInt32(number) * 10;
			if (vs < 0) {
				SelfVoice.NLS("is.wav", false);
				Interaction.resumeAndUnmute();
				resumeInput();
				return;
			}
			sectorX = hs;
			sectorY = vs;
			direction = Degrees.GetDegreesBetween(x, y, sectorX, sectorY);
			SelfVoice.NLS("vs.wav&"
			 + sec, true, true);
			Interaction.resumeAndUnmute();
			resumeInput();
		}

		private bool navigatingToSector()
		{
			return (sectorX != -1);
		}

		/// <summary>
		/// Plays a RIO message.
		/// </summary>
		/// <param name="file">The file to play.</param>
		/// <param name="forcePlay">If true, the current RIO message will be stopped. If false, the current RIO message will continue playing and the new RIO message will be ignored.</param>
		private void playRIO(String file, bool forcePlay)
		{
			if (!Options.playRIO)
				return;
			if (isAI && !autoPlayTarget)
				return;
			if (!forcePlay && RIOMessage != null && DSound.isPlaying(RIOMessage))
				return; //it's already playing, don't stop it
			DSound.unloadSound(ref RIOMessage);
			RIOMessage = loadSound(file);
			playSound(RIOMessage, true, false);
		}

		private void playRIO(String file)
		{
			playRIO(file, true);
		}

		public override void save(BinaryWriter w)
		{
			base.save(w);
			w.Write(currentDistance);
			w.Write(isOnRunway);
			w.Write(speed);
			w.Write(throttlePosition);
			w.Write(missileWarnTime);
			w.Write(hasMissileWarned);
			w.Write(isLandingGearRetracted);
			w.Write(firstLaunch);
			w.Write(isLeveling);
			w.Write(played0);
			w.Write(played1);
			w.Write(played2);
			w.Write(played3);
			w.Write(played6);
			w.Write(enginesOff);
			w.Write((byte)pov);
			w.Write(m_fuelWeight);
			w.Write(playedSonicBoom);
			w.Write(callingRefueler);
			w.Write(announcedRefueler);
			w.Write(landingOnCarrier);
			w.Write(engineDamagePoints);
			w.Write(maxEngineDamagePoints);
			w.Write(gunFireCount);
			w.Write(maxGunFireCount);
			w.Write(gunWaitTime);
			w.Write(maxGunWaitTime);
			w.Write(hasNotifiedRecharge);
		}

		public override bool load()
		{
			//Player is not re-instantiated, so create a new weapon instance manually
			//since the last one was destroyed (see Common, loadGame function)
			if (!isAI) {
				loadSounds();
				createWeaponInstance();
			}
			if (!base.load())
				return false;
			BinaryReader r = Common.inFile;
			currentDistance = r.ReadSingle();
			isOnRunway = r.ReadBoolean();
			speed = r.ReadSingle();
			throttlePosition = r.ReadInt32();
			missileWarnTime = r.ReadInt32();
			hasMissileWarned = r.ReadBoolean();
			isLandingGearRetracted = r.ReadBoolean();
			firstLaunch = r.ReadBoolean();
			isLeveling = r.ReadBoolean();
			played0 = r.ReadBoolean();
			played1 = r.ReadBoolean();
			played2 = r.ReadBoolean();
			played3 = r.ReadBoolean();
			played6 = r.ReadBoolean();
			enginesOff = r.ReadBoolean();
			pov = (PointOfView)r.ReadByte();
			m_fuelWeight = r.ReadSingle();
			playedSonicBoom = r.ReadBoolean();
			callingRefueler = r.ReadBoolean();
			announcedRefueler = r.ReadBoolean();
			landingOnCarrier = r.ReadBoolean();
			engineDamagePoints = r.ReadInt32();
			maxEngineDamagePoints = r.ReadInt32();
			gunFireCount = r.ReadInt32();
			maxGunFireCount = r.ReadInt32();
			gunWaitTime = r.ReadInt32();
			maxGunWaitTime = r.ReadInt32();
			hasNotifiedRecharge = r.ReadBoolean();
			return true;
		}

		private void missileWarn()
		{
			if (!Mission.isSwarm || hasMissileWarned)
				return;
			missileWarnTime += Common.intervalMS;
			if (missileWarnTime / 1000 >= 60) {
				if (ATCMessage != null)
					ATCMessage.stopOgg();
				ATCMessage = DSound.loadOgg(soundPath + "m1-1.ogg");
				Interaction.stopAndMute(true);
				ATCMessage.play();
				while (ATCMessage.isPlaying())
					Thread.Sleep(100);
				Interaction.resumeAndUnmute();
				hasMissileWarned = true;
			}

		}

		private void rechargeNotification()
		{
			if (hasNotifiedRecharge)
				return;
			rechargeNotifyTime += Common.intervalMS;
			if (rechargeNotifyTime / 1000 >= 5) {
				if (ATCMessage != null)
					ATCMessage.stopOgg();
				ATCMessage = DSound.loadOgg(soundPath + "hf.ogg");
				Interaction.stopAndMute(false);
				ATCMessage.play();
				while (ATCMessage.isPlaying())
					Thread.Sleep(100);
				Interaction.resumeAndUnmute();
				hasNotifiedRecharge = true;
			}
		}


		private void announceNewSector()
		{
			String currentSector = Interaction.getSector(this, false);
			if (!prevSector.Equals(currentSector)) {
				prevSector = currentSector;
				if (!SelfVoice.setPathTo("n", false))
					return;
				SelfVoice.NLS(
				 Common.sectorToString(currentSector),
				 true, true);
			} //if changed sectors
		}

		private void stopSectorNav()
		{
			sectorX = -1;
			sectorY = -1;
			playSound(lockBrokenSound, true, false);
		}

		private void turnOffEngine()
		{
			enginesOff = true;
			muteEngines();
			throttlePosition = 0;
			rpm = 0;
			throttle();
			afterBurners();
		}

		//This method will also be called by the
		//midair refueler and aircraft carrier
		//after the land scenes are done to break the lock.
		public void requestRefuel(bool refuelerRanOutOfFuel)
		{
			if (Options.mode != Options.Modes.mission)
				return;
			if (refuelerRanOutOfFuel) {
				ExtendedAudioBuffer msg = DSound.LoadSoundAlwaysLoud(DSound.SoundPath + "\\rf13.wav");
				DSound.PlaySound(msg, true, false);
			}
			if (callingRefueler) {
				Mission.refueler.uncall();
				callingRefueler = false;
				announcedRefueler = false;
				playSound(lockBrokenSound, true, false);
				if (weapon.isValidLock()
					&& weapon.getLockedTarget().Equals(Mission.refueler))
					weapon.clearLock();
				return;
			}
			if (landingOnCarrier) {
				playSound(lockBrokenSound, true, false);
				weapon.clearLock();
				landingOnCarrier = false;
				Mission.carrier.abort();
				return;
			}
			Interaction.stopAndMute(false);
			pauseInput();
			String[] oArray = { (Mission.refuelCount < 5) ? "r.wav" : "", "ac.wav" };
			int index = Common.sVGenerateMenu(null, oArray);
			if (index == 0) {
				callingRefueler = true;
				Mission.refueler.call();
			}
			if (index == 1) {
				landingOnCarrier = true;
				weapon.lockIndex = Mission.carrier.id;
				Mission.carrier.playerRequestLand();
			}
			resumeInput();
			Interaction.resumeAndUnmute();
		}

		public void requestRefuel()
		{
			requestRefuel(false);
		}

		private void announceRefueler()
		{
			if (!announcedRefueler
				&& Weapons.inRange(this, Mission.refueler, weapon.radarRange)) {
				announcedRefueler = true;
				if (Mission.refueler.message != null && DSound.isPlaying(Mission.refueler.message))
					return; //don't play anything
				waitTillOggDone(soundPath + "\\rf11.ogg", true);
			}
		}

		private void waitTillOggDone(String ogg, bool allowForSkip)
		{
			Interaction.stopAndMute(false);
			OggBuffer o = DSound.loadOgg(ogg);
			o.play();
			long mark = Environment.TickCount;
			while (o.isPlaying()) {
				if (allowForSkip && (Environment.TickCount - mark) / 1000 >= 3
					&& (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown()))
					break;
				Thread.Sleep(100);
			}
			o.stopOgg();
			Interaction.resumeAndUnmute();
			o = null;
		}

		/*
		private void playSonicBoom()
		{
			if (!playedSonicBoom && speed <= 760.0) {
				int num = Common.getRandom(0, sonicBoom.Length - 1);
				bool playing = false;
				for (int i = 0; i < sonicBoom.Length; i++) {
					if (DSound.isPlaying(sonicBoom[i])) {
						playing = true;
						break;
					} //if playing
				} //loop

				if (!playing)
					playSound(sonicBoom[num], true, false);
				playedSonicBoom = true;
			} else if (speed > 760.0)
				playedSonicBoom = false;
		}
		*/
		private bool isInherited()
		{
			return (this is Chopper || this is JuliusAircraft);
		}

		private bool isMissionFighter()
		{
			if (isInherited())
				return false;
			if (isAI && Options.mode == Options.Modes.mission)
				return true;
			return false;
		}

		public void rearm()
		{
			m_fuelWeight = m_maxFuelWeight;
			weapon.arm();
		}

		private void landOnCarrier()
		{
			if (landingOnCarrier
				&& successfulLanding(true) && collidesWith(Mission.carrier)) {
				z = 100f;
				if (isSender()) {
					cloak(); //queues cloak command to send to server
					sendObjectUpdate();
				}
				Mission.carrier.landPlayer(); //method will take care of muting player, and unmuting before catapult
				if (isSender()) {
					deCloak(); //Queues decloak command to send to server
					sendObjectUpdate();
				}
			}
		}

		private void processInput()
		{
			actionsThreadRunning = true;
			while (!stopInput) {
				//Can't use isTerminate dhere since this projector will only terminate
				//after this thread stops receiving input.
				while (inputPause || Interaction.holderAt(0).haulted) {
					//System.Diagnostics.Trace.WriteLine("Requested pause");
					resetActions(); //clear what actions were already registered
					//System.Diagnostics.Trace.WriteLine("Reset actions for " + name);
					weapon.setStrafe(false); //stop strafe sound so it doesn't play over lock menus, etc.
					//System.Diagnostics.Trace.WriteLine("Strafe now false");
					//in case we have a split second case of the player pressing a key
					//but we're supposed to stop receiving input.
					actionsThreadRunning = false;
					Thread.Sleep(10);
					if (stopInput)
						break;
				} //if want to pause input
				actionsThreadRunning = true;
				while (iteratingActions) {
					System.Diagnostics.Trace.WriteLine("Insite iterating actions condition");
					if (stopInput || inputPause)
						break; //prevent deadlock here.
							   //since pauseInput() will wait for this thread to hault, but if it gets stuck here because iteratingActions is true, there will be a deadlock.
					Thread.Sleep(0);
				}
				readingInput = true;
				DXInput.updateKeyboardState();
				if (!Options.isPaused) {
					check(Action.exitGame, true);
					check(Action.decreaseMusicVolume);
					check(Action.increaseMusicVolume);
					if (Options.isPlayingOnline) {
						check(Action.chat, true);
						check(Action.whoIs, true);
						check(Action.addBot, true);
						check(Action.removeBot, true);
						check(Action.prevMessage, true);
						check(Action.nextMessage, true);
						check(Action.copyMessage, true);
						if (waitingForHost()) {
							startGame();
						}
					}

					if (!waitingForHost()) { //Only let escape process if waiting for host, so objects can clean up.
						if (throttlePosition == maxThrottlePosition)
							actionsArray.Add(Action.activateAfterburners);
						check(Action.throttleUp);
						check(Action.switchToWeapon1, true);
						check(Action.switchToWeapon2, true);
						check(Action.switchToWeapon3, true);
						check(Action.switchToWeapon4, true);
						check(Action.switchToWeapon5, true);
						check(Action.weaponsRadar, true);
						if (!isOnRunway) {
							check(Action.weaponsRadar, true);
							check(Action.turnLeft);
							check(Action.turnRight);
							check(Action.bankLeft);
							check(Action.bankRight);
							if (!(check(Action.leftBarrelRoll) || check(Action.rightBarrelRoll)) && inBarrelRoll) {
								inBarrelRoll = false;
								virtualNoseAngle = 0;
								bankAngle = 0;
							}
							check(Action.splitS, true);
							weapon.setStrafe(check(Action.fireWeapon, weapon.weaponIndex != WeaponTypes.guns) && weapon.weaponIndex == WeaponTypes.guns);
							check(Action.autoelevation, true);
							check(Action.sectorNav, true);
							check(Action.requestRefuel, true);
						} //If not on runway
						check(Action.brake);
						check(Action.throttleDown);
						check(Action.level, true);
						check(Action.ascend);
						check(Action.descend);
						check(Action.retractLandingGear, true);
						check(Action.registerLock, true);
						check(Action.switchWeapon, true);
						check(Action.activateAfterburners, true);
						check(Action.togglePointOfView, true);
						check(Action.optionsMenu, true);
					} //If !waiting for host
				} //if !paused
				check(Action.pauseGame, true);
				check(Action.stopSAPI, true);
				readingInput = false;
				Thread.Sleep(50);
			} //while
			readingInput = false;
			actionsThreadRunning = false;
		}

		private void iterateActions()
		{
			while (readingInput)
				Thread.Sleep(0);
			iteratingActions = true;
			bool continueStrafe = false;
			if (isSender() && !weapon.stoppedStrafe) {
				weapon.stoppedStrafe = true;
				Client.addData(Action.endStrafe, id);
			}
			int count = actionsArray.Count;
			foreach (Action action in actionsArray) {
				if (Options.mode == Options.Modes.training && !isAI)
					acHistory.Add(action);
				switch (action) {
					case Action.addBot:
						Client.addBot();
						break;

					case Action.removeBot:
						Client.removeBot();
						break;

					case Action.leftBarrelRoll:
						leftBarrelRoll();
						break;

					case Action.rightBarrelRoll:
						rightBarrelRoll();
						break;

					case Action.splitS:
						System.Diagnostics.Debug.WriteLine("Entered split s");
						splitS();
						break;

					case Action.chat:
						Client.sendChatMessage();
						break;

					case Action.copyMessage:
						Client.copyMessage();
						break;

					case Action.prevMessage:
						Client.prevMessage();
						break;

					case Action.nextMessage:
						Client.nextMessage();
						break;

					case Action.whoIs:
						Client.whoIs();
						break;

					case Action.requestRefuel:
						requestRefuel();
						while (DXInput.isKeyHeldDown(Key.Escape) || DXInput.isJSButtonHeldDown())
							Thread.Sleep(10);
						break;
					case Action.throttleUp:
						throttleUp(); //will only activate with keyboard.
						break;
					case Action.throttleDown:
						throttleDown(); //will only activate with keyboard.
						break;
					//For turn left and turn right, we send heading anyway, no need
					//to implement these on receiver end.
					case Action.turnLeft:
						turnLeft(false);
						break;
					case Action.turnRight:
						turnRight(false);
						break;
					//We send bank left and right so other players will get audible feedback.
					//IE: sound of this player's afterburners activating.
					case Action.bankLeft:
						bankLeft();
						if (isSender())
							Client.addData(action, id);
						break;
					case Action.bankRight:
						bankRight();
						if (isSender())
							Client.addData(action, id);
						break;
					case Action.brake:
						brake();
						break;
					case Action.ascend:
						//Ascend command will be sent by takeoff()
						ascend(false);
						break;
					case Action.descend:
						descend(false);
						break;
					case Action.retractLandingGear:
						retractLandingGear();
						if (isSender())
							Client.addData(action, id);
						break;
					case Action.registerLock:
						//don't break lock if AI, otherwise we'll get a break lock break lock break lock cycle
						if (!isAI) {
							//Landing beacon could be null if we're playing online.
							if (weapon.isValidLock()) {
								// Don't allow to break a lock with the landing beacon or aircraft carrier, since these locks can't be re-obtained.
								if ((Mission.landingBeacon != null && weapon.getLockedTarget().Equals(Mission.landingBeacon)) || (Mission.carrier != null && weapon.getLockedTarget().Equals(Mission.carrier)))
									break;
								weapon.clearLock();
								playSound(lockBrokenSound, true, false);
							} else {
								bool gotLock = registerLock();
								while (DXInput.isKeyHeldDown(Key.Escape) || DXInput.isJSButtonHeldDown())
									Thread.Sleep(5);
								if (gotLock && isSender())
									Client.addData(Action.registerLock, id, weapon.lockIndex);
							} //if doesn't have lock
							break;
						} //if !AI
						registerLock();

						break;
					case Action.fireWeapon:
						if (trainer1 || trainer2)
							break;
						if (weapon.weaponIndex == WeaponTypes.guns)
							continueStrafe = true;
						String weaponID = fireWeapon();
						if (isSender() && weaponID != null) {
							Client.addDeferred(action, id,
							 (byte)weapon.weaponIndex, weaponID);
						}
						break;
					case Action.level:
						level();
						break;
					case Action.switchWeapon:
						/* See getActions method for first implementation of this condition.
						 * We need to put the gunFire condition here since
						 * all actions are filled in one method, and then iterated in another.
						 * So, getActions may not know that we're strafing before fireWeapon()
						 * is executed for the first time with the guns.
						 * After fireWeapon is executed for the first time, getActions will take care of making sure weapons
						 * don't switch.
						 * */
						if (gunFireCount == 0) {
							switchWeapon();
							if (isSender())
								Client.addData(Action.switchWeapon, id);
						}
						break;
					case Action.switchToWeapon1:
						switchWeapon(weapon.getWeaponAt(0));
						break;
					case Action.switchToWeapon2:
						switchWeapon(weapon.getWeaponAt(1));
						break;
					case Action.switchToWeapon3:
						switchWeapon(weapon.getWeaponAt(2));
						break;
					case Action.switchToWeapon4:
						switchWeapon(weapon.getWeaponAt(3));
						break;
					case Action.switchToWeapon5:
						switchWeapon(weapon.getWeaponAt(4));
						break;
					case Action.activateAfterburners:
						activateAfterburners();
						if (isSender())
							Client.addData(action, id);
						break;
					case Action.autoelevation:
						autoelevation();
						break;
					case Action.sectorNav:
						if (!navigatingToSector())
							moveToSector();
						else
							stopSectorNav();
						break;
					case Action.weaponsRadar:
						weaponsRadar();
						break;
					case Action.togglePointOfView:
						togglePointOfView();
						break;
					case Action.pauseGame:
						pauseGame();
						break;
					case Action.stopSAPI:
						stopSAPI();
						break;
					case Action.decreaseMusicVolume:
						Common.decreaseMusicVolume();
						break;
					case Action.increaseMusicVolume:
						Common.increaseMusicVolume();
						break;
					case Action.optionsMenu:
						optionsMenu();
						while (DXInput.isKeyHeldDown(Key.Escape) || DXInput.isJSButtonHeldDown())
							Thread.Sleep(10);
						break;
					case Action.exitGame:
						exitGame();
						break;
					case Action.endStrafe: //Action only exists in online play
						weapon.setStrafe(false);
						break;
				} //switch
				if (count != actionsArray.Count)
					break; //We've issued a terminate input command
			} //for

			//continueStrafe will be honored if this is an AI craft in offline play, or if this
			//is a bot in online play and this is the actual copy of the bot.
			if (!continueStrafe && ((!Options.isPlayingOnline && isAI) || isBot()))
				weapon.setStrafe(false);
			sendObjectUpdate();
			iteratingActions = false;
		}

		private void soundLowFuelAlarm()
		{
			if (m_fuelWeight > 1000.0) {
				if (DSound.isPlaying(lowFuelAlarm))
					lowFuelAlarm.stop();
				return;
			}
			if (!DSound.isPlaying(lowFuelAlarm))
				playSound(lowFuelAlarm, false, true);
		}

		protected override void useFuel()
		{
			if (afterburnersActive)
				m_fuelWeight -= 10.0f;

			base.useFuel();
		}

		private void startInput()
		{
			stopInput = false;
			inputThread.Start();
		}

		private void resumeInput()
		{
			inputPause = false;
		}

		private void terminateInput()
		{
			stopInput = true;
			while (actionsThreadRunning)
				Thread.Sleep(0);
			resetActions();
			weapon.setStrafe(false);
		}

		private void pauseInput()
		{
			inputPause = true;
			while (actionsThreadRunning)
				Thread.Sleep(0);
		}

		private bool isAboveIsland()
		{
			return (x >= 110.0
				&& x <= 310.0
				&& y >= 110.0
				&& y <= 310.0);
		}

		private bool isAboveOcean()
		{
			return !isAboveIsland();
		}

		public void restartEngine(bool initSpeed)
		{
			if (!enginesOff)
				return;
			if (initSpeed)
				speed = 450f;
			enginesOff = false;
			throttleDown();
		}

		public void catapult()
		{
			playSound(catapultSound, true, false);
			z = 1000f;
			restartEngine(true);
			throttlePosition = 1;
			throttleDown();
			virtualNoseAngle = 10;
		}

		private void playFinalOgg(String file)
		{
			Interaction.muteAllObjects(true); //Weapons will mute and clear themselves, so we just need to mute regular objects.
			Common.playUntilKeyPress(file);
		}
		public void restoreDamage(int restorationAmount)
		{
			if (restorationAmount == 0
				|| damage + restorationAmount > maxDamagePoints) {
				damage = maxDamagePoints;
				return;
			}
			damage += restorationAmount;
		}

		private void finishedInputEvent(String input)
		{
			this.input = input;
		}

		private bool tickDemoTimer()
		{
			if (Mission.isMission && Options.isDemo) {
				totalTime += Common.intervalMS;
				if (totalTime / 60000 >= 15 || Mission.missionNumber == Mission.Stage.aboveIsland) {
					endDemo();
					return true;
				}
			}
			return false;
		}

		private void exitGame()
		{
			System.Diagnostics.Trace.WriteLine("entered exit game");
			pauseInput();
			Interaction.stopAndMute(false);
			int conf = Common.returnSvOrSr(() => Common.sVGenerateMenu("q.wav", new String[] { "kd3.wav", "kd4.wav" }), () => Common.GenerateMenu("Do you really want to quit the game?", new string[] { "No", "Yes" }), Options.menuVoiceMode);
			if (conf == 1) {
				hit(0, Interaction.Cause.quitGame);
				Interaction.resumeWeapons();
				return;
			}
			Interaction.resumeAndUnmute();
			resumeInput();
		}

		public void endDemo()
		{
			hit(0, Interaction.Cause.demoExpired);
		}

		private void refuelCommands()
		{
			if (Mission.refueler.isConnecting
				&& Weapons.inRange(this,
				Mission.refueler,
				Mission.refueler.connectRange)) {
				Mission.refueler.connect();
				return; //don't fall through
			}
			if (!Mission.refueler.isConnecting
				&& Weapons.inRange(this,
				Mission.refueler,
				Mission.refueler.startConnectManeuverRange))
				Mission.refueler.connectManeuver();
		}

		public override void revive()
		{
			base.revive();
			firstMove = true;
			showInList = true;
		}

		public void revive(float x, float y)
		{
			revive();
			this.x = x;
			this.y = y;
			z = Mission.player.z -
				((Common.getRandom(1, 2) == 1) ? -3000 : 1000);
			//Either 3000 feet above player or 1000 feet below.
			if (z < 1000f)
				z = 1000f;
			if (z > 50000f)
				z = 45000f;
		}

		private void startFighterSwarm()
		{
			if (!Mission.hasSentAircraft
				&& Mission.missionNumber == Mission.Stage.powerPlant
				&& x >= 210.0) {
				if (ATCMessage != null)
					ATCMessage.stopOgg();
				ATCMessage = DSound.loadOgg(DSound.SoundPath + "\\co1.ogg");
				Interaction.stopAndMute(false);
				ATCMessage.play();
				long mark = Environment.TickCount;
				while (ATCMessage.isPlaying()) {
					if ((Environment.TickCount - mark) / 1000 >= 3 && (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown()))
						break;
					Thread.Sleep(10);
				}
				ATCMessage.stopOgg();
				Interaction.resumeAndUnmute();
				for (int i = 3; i <= 6; i++)
					Mission.createNewFighter(i, 15);
				Mission.hasSentAircraft = true;
			} //if hasn't sent swarm aircraft
		}

		private void randomAttack()
		{
			if (!Mission.hasAnnouncedFighters && Mission.attackCounter / 1000 >= 270) {
				Mission.hasAnnouncedFighters = true;
				if (ATCMessage != null)
					ATCMessage.stopOgg();
				ATCMessage = DSound.loadOgg(DSound.SoundPath + "\\co2.ogg");
				ATCMessage.play();
				return;
			}
			if (Mission.attackCounter / 1000 >= 300) {
				Mission.attackCounter = 0;
				Mission.hasAnnouncedFighters = false;
				int numFighters = Common.getRandom(1, 2);
				for (int i = 1; i <= numFighters; i++)
					Mission.createNewFighter(Common.getRandom(1, 6), 15);
			} //if time to attack
		}

		private void fireCruiseMissile()
		{
			if (cruiseFire / 1000 >= 30) {
				int fire = Common.getRandom(1, 3);
				if (fire == 1) {
					cruiseFire = 0;
					return;
				}
				cruiseFire = -1;
				switchWeapon(WeaponTypes.laserCannonSystem);
				fireWeapon();
			}
		}

		//Checks whether AI should turn on its afterburners
		private bool conditionForAfterburners()
		{
			if (!weapon.isValidLock()
				|| this is Chopper) //Choppers dont' have afterburners
				return false;
			float distance = getPosition(weapon.getLockedTarget()).distance;
			if (distance >= 8f)
				return true;
			else
				return false;
		}

		private void setEngineDamagePoints(int v)
		{
			engineDamagePoints = v;
			maxEngineDamagePoints = v;
		}

		private bool damageEngine()
		{
			if (z > 50000.0)
				engineDamagePoints -= 2;

			if (!RIOEngineWarned && getEngineDamagePercent() <= 50) {
				playRIO(soundPath + "hd.wav");
				RIOEngineWarned = true;
			}
			if (engineDamagePoints < 0)
				engineDamagePoints = 0;
			if (engineDamagePoints == 0)
				return true;
			else
				return false;
		}

		private void repairEngine()
		{
			if (engineDamagePoints < maxEngineDamagePoints && z < 50000.0) {
				engineDamagePoints++;
				if (engineDamagePoints > maxEngineDamagePoints)
					engineDamagePoints = maxEngineDamagePoints;
				if (!RIOEngineWarned && getEngineDamagePercent() > 50)
					RIOEngineWarned = false;
			} //if repairing engine
		}

		public override void brake()
		{
			base.brake();
			isBraking = true;
			brakeTick = 1;
		}

		public override void hit(bool lts)
		{
			if (!isAI || autoPlayTarget) {
				playSound(ltsHit, true, false);
				playRIO(soundPath + "he41.wav", false);
			}
		}

		public void announceRecharging()
		{
			int x = Common.getRandom(1, 2);
			if (x == 1) {
				if (ATCMessage != null)
					ATCMessage.stopOgg();
				ATCMessage = DSound.loadOgg(DSound.SoundPath + "\\hg1.ogg");
				ATCMessage.play();
			} else  //if RIO
				playRIO(soundPath + "hg2.wav", true);
		}

		public void announceDoneCharging()
		{
			int x = Common.getRandom(1, 2);
			if (x == 1) {
				if (ATCMessage != null)
					ATCMessage.stopOgg();
				ATCMessage = DSound.loadOgg(DSound.SoundPath + "\\hh1.ogg");
				ATCMessage.play();
			} else
				playRIO(soundPath + "hh" + Common.getRandom(2, 3) + ".wav", true);
		}

		//sets number of rounds aircraft should fire when strafing gun
		protected void setStrafeTime(int strafeRounds, int waitSeconds)
		{
			maxGunFireCount = strafeRounds;
			maxGunWaitTime = waitSeconds;
		}

		private void tickGunWaitTimer()
		{
			if (gunWaitTime > 0) {
				gunWaitTime += Common.intervalMS;
				if (gunWaitTime / 1000 >= maxGunWaitTime)
					gunWaitTime = 0;
			}
		}

		public void notifyOf(Notifications n, bool inRange)
		{
			if (inRange) {
				if (n == Notifications.strafe)
					playRIO(soundPath + "he1" + Common.getRandom(1, 2) + ".wav", false);
				else if (n == Notifications.missileLaunch)
					playRIO(soundPath + "he2" + Common.getRandom(1, 2) + ".wav", false);
			}
		}

		/// <summary>
		/// Receives data from the server. This method will then parse the data and
		/// change this object's state accordingly.
		/// </summary>
		private void receiveData()
		{
			if (isAI)
				resetActions();
			lock (dataLocker) {
				if (queue.BaseStream.Length == 0) //we have no commands from the server
					return;
				Client.Fields uC; //current field, such as 2 for damage.
				System.Diagnostics.Trace.WriteLine(name + " received data");
				while (queue.BaseStream.Length > queue.BaseStream.Position) {
					System.Diagnostics.Trace.WriteLine("Positions: " + queue.BaseStream.Position + ", " + queue.BaseStream.Length);

					//See plans/objects.txt for information on each field.
					int maxArgs = queue.ReadInt16();
					System.Diagnostics.Trace.WriteLine(maxArgs);
					for (int numArgs = 1; numArgs <= maxArgs; numArgs++) {
						uC = (Client.Fields)queue.ReadByte();
						System.Diagnostics.Trace.WriteLine(uC);
						switch (uC) {
							case Client.Fields.damage:
								damage = queue.ReadInt32();
								break;
							case Client.Fields.direction:
								direction = queue.ReadInt32();
								break;
							case Client.Fields.x:
								x = queue.ReadSingle();
								break;
							case Client.Fields.y:
								y = queue.ReadSingle();
								break;
							case Client.Fields.z:
								z = queue.ReadSingle();
								break;
							case Client.Fields.speed:
								speed = queue.ReadSingle();
								break;
							case Client.Fields.throttlePosition:
								throttlePosition = queue.ReadInt32();
								break;
							case Client.Fields.isOnRunway:
								isOnRunway = queue.ReadBoolean();
								break;
							case Client.Fields.afterburnersActive:
								afterburnersActive = queue.ReadBoolean();
								break;
							case Client.Fields.cloakStatus:
								setCloakStatus(queue.ReadBoolean());
								break;
						} //switch
					} //each attribute

					//Next, we have custom attributes.
					//This is array of attributes in form c:co, args[arg1, arg2,  etc.]
					if ((maxArgs = queue.ReadInt16()) == 0)
						continue; //didn't pass any custom attributes.
					System.Diagnostics.Trace.WriteLine("Custom arg size " + maxArgs);
					Action a;
					for (int numArgs = 1; numArgs <= maxArgs; numArgs++) {
						a = (Action)queue.ReadByte();
						System.Diagnostics.Trace.WriteLine(a);
						if (a != Action.registerLock && a != Action.fireWeapon)
							actionsArray.Add(a);
						else {
							if (a == Action.registerLock) {
								//If multiple locks occurred in one chunk, execute them in order
								if (lockTag == null)
									lockTag = "";
								lockTag += queue.ReadString() + ".";
							}
							if (a == Action.fireWeapon) {
								/* If we got multiple fire actions in one data chunk,
								 * make sure they're only executed every tick.
								 * This way, the duplicate stays in sync with the player.
								 * */
								if (fireTag == null)
									fireTag = "";
								fireTag += queue.ReadByte() + "," + queue.ReadString() + ".";
							} //if fire tag
						} //if we have parameters
					} //every command
				} //for index = 0
				stream.Position = 0;
				stream.SetLength(0);
			} //lock
		}

		/* Gets the next token in a parameter list.
		 * For instance, if multiple lock commands occur in one instance, the tags of
		 * theobjects which should be locked will be separated by a '.'.
		 * This method will return the next tag, so each tag can be crawled through.
		 * */
		private string getNextParameterToken(ref String args)
		{
			if (args == null)
				return null;
			String[] argList = args.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			args = "";
			String result = argList[0];
			for (int i = 1; i < argList.Length; i++)
				args += argList[i] + ".";
			if (argList.Length == 1)
				args = null; //There are no more tokens.
			return result;
		}

		public void createWeaponInstance()
		{
			if (!isInherited()) { //chopper will consruct own weapon
				weapon = new Weapons((Projector)this,
				 WeaponTypes.guns, WeaponTypes.missile,
				 WeaponTypes.laserCannonSystem, WeaponTypes.cruiseMissile);
				if ((!isAI) || autoPlayTarget) {
					weapon.strike += strike;
					weapon.destroy += destroy;
					weapon.addValidIndex(WeaponTypes.missileInterceptor);
				}
			} //if !inherited
		}


		private void weaponsRadar()
		{
			List<Projector> w = null;
			string[] menuStr = null;
			String name = "";
			if ((w = Interaction.getProjectiles(this)) != null) {
				menuStr = new String[w.Count];
				for (int i = 0; i < w.Count; i++) {
					if (w[i] is CruiseMissile)
						name = Common.returnSvOrSr(() => "wcm.wav", () => "Cruise Missile", Options.menuVoiceMode);
					else
						name = Common.returnSvOrSr(() => "wm.wav", () => "Missile", Options.menuVoiceMode);
					menuStr[i] = name + Common.returnSvOrSr(() => "&" + getPosition(w[i]).ToString(), () =>
					{
						RelativePosition p = getPosition(w[i]);
						p.sapiMode = true;
						return p.ToString();
					}, Options.menuVoiceMode);
				} //for
				pauseInput();
				Interaction.stopAndMute(false);
				Common.executeSvOrSr(() => Common.sVGenerateMenu(null, menuStr, 0, "n"), () => Common.GenerateMenu(null, menuStr, 0), Options.menuVoiceMode);
				Interaction.resumeAndUnmute();
				resumeInput();
			} //if have projectiles
		}

		private void executeStatusCommand()
		{
			if (waitingForHost())
				return; //No status commands when game hasn't started.
			bool e = false, t = false;
			if (DXInput.IsShift()) {
				if (DXInput.isFirstPress(Key.F1, false))
					e = statusMode(Status.sector);
				else if (DXInput.isFirstPress(Key.F2, false))
					e = statusMode(Status.lap);
				else if (DXInput.isFirstPress(Key.F3, false))
					e = statusMode(Status.distance);
				else if (DXInput.isFirstPress(Key.F4, false))
					e = statusMode(Status.course);
				else if (DXInput.isFirstPress(Key.F5, false))
					e = statusMode(Status.targetIntegrity);
			} else if (DXInput.IsAlt()) {
				if (DXInput.isFirstPress(Key.F1, false))
					e = statusMode(Status.fuel);
				else if (DXInput.isFirstPress(Key.F2, false))
					e = statusMode(Status.ammunition);
				else if (DXInput.isFirstPress(Key.F3, false))
					e = statusMode(Status.rank);
				else if (DXInput.isFirstPress(Key.F5, false))
					e = statusMode(Status.engineIntegrity);
			} else {
				if (DXInput.isFirstPress(Key.F1, false))
					e = statusMode(Status.target);
				else if (DXInput.isFirstPress(Key.F2, false))
					e = statusMode(Status.speedometer);
				else if (DXInput.isFirstPress(Key.F3, false))
					e = statusMode(Status.altimeter);
				else if (DXInput.isFirstPress(Key.F4, false))
					e = statusMode(Status.angleOfAttack);
				else if (DXInput.isFirstPress(Key.F5, false))
					e = statusMode(Status.integrity);
				else if (DXInput.isKeyHeldDown(Key.Tab, false)) {
					t = executeTABStatusCommand();
					if (!t) {
						Interaction.resumeAndUnmute();
						resumeInput();
						return;
					}

				}
			} //if not alt or shift

			if (e) {
				pauseInput();
				Interaction.stopAndMute(false);
			}
			if (e || t) {
				while (DXInput.isKeyHeldDown() && !hit())
					Thread.Sleep(0);
				while (SelfVoice.isThreadRunning()) {
					if (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown() || hit())
						break;
					Thread.Sleep(5);
				}
				SelfVoice.purge(true);
				Interaction.resumeAndUnmute();
				resumeInput();
			} //if status command
		}

		private bool executeTABStatusCommand()
		{
			if (DXInput.isKeyHeldDown(Key.Tab)) {
				Key k = Key.Unknown;
				bool target = false; //true if wants opponent's info
				pauseInput();
				Interaction.stopAndMute(false);
				while (DXInput.isKeyHeldDown() && !hit())
					Thread.Sleep(5);
				Common.executeSvOrSr(() =>
				{
					SelfVoice.setPathTo("n");
					SelfVoice.NLS("st.wav", true, true);
				}, () =>
				{
					SapiSpeech.purge();
					SapiSpeech.speak("Status");
				}, Options.statusVoiceMode);
				while (DXInput.isKeyHeldDown(Key.Tab))
					Thread.Sleep(5);
				while ((k = DXInput.getKeyPressed()) == Key.Unknown && !hit())
					Thread.Sleep(0);
				if (DXInput.isKeyHeldDown(Key.Tab)) {
					if (!weapon.isValidLock()) {
						//Make sure we don't fall through to another status mode issueance.
						while (DXInput.isKeyHeldDown(Key.Tab))
							Thread.Sleep(5);
						return false;
					}
					Common.executeSvOrSr(() =>
					{
						SelfVoice.NLS(weapon.getLockedTarget().name + ".wav", true, true);
					}, () =>
					{
						SapiSpeech.speak(Common.getFriendlyNameOf(weapon.getLockedTarget().name));
					}, (Options.isPlayingOnline)?Options.VoiceModes.screenReader:Options.statusVoiceMode);
					target = true;
					while (DXInput.isKeyHeldDown(Key.Tab))
						Thread.Sleep(5);
					while (DXInput.isKeyHeldDown() && !hit())
						Thread.Sleep(5);
					while ((k = DXInput.getKeyPressed()) == Key.Unknown && !hit())
						Thread.Sleep(0);
				} //if TAB second time

				switch (k) {
					case Key.LeftShift:
					case Key.RightShift:
						Key theKey = Key.Unknown;
						while (theKey == Key.Unknown) {
							if (DXInput.isKeyHeldDown(Key.F))
								theKey = Key.F;
							if (!DXInput.IsShift())
								break;
							Thread.Sleep(5);
						}
						if (theKey == Key.F)
							return statusMode(Status.refuelerCount);
						return false;
					case Key.O:
						return statusMode(Status.missionObjective);
					case Key.T:
						return statusMode(Status.target);
					case Key.W:
						return statusMode(Status.loadPercentage);
					case Key.S:
						return statusMode(Status.speedometer);
					case Key.A:
						return statusMode(Status.altimeter);
					case Key.N:
						return statusMode(Status.angleOfAttack);
					case Key.H:
						return statusMode((target) ? Status.targetIntegrity : Status.integrity);
					case Key.F:
						return statusMode(Status.fuel);
					case Key.M:
						return statusMode(Status.ammunition);
					case Key.P:
						return statusMode(Status.rank);
					case Key.I:
						return statusMode(Status.engineIntegrity);
					case Key.X:
						return statusMode(Status.sector);
					case Key.L:
						return statusMode(Status.lap);
					case Key.C:
						return statusMode(Status.course);
					case Key.D:
						return statusMode(Status.distance);
					case Key.R:
						return statusMode(Status.altitudeRate);
					case Key.B:
						return statusMode(Status.bankAngle);
					case Key.U:
						return statusMode(Status.turnRadius);
					case Key.E:
						return statusMode(Status.turnRate);
					case Key.Z:
						return statusMode(Status.facing);
				} //switch
			} //if tab
			return false;
		}

		public override void setAddOns(AddOnArgs[] addOns)
		{
			base.setAddOns(addOns);
			m_maxFuelWeight += extraFuelTanks * 5000.0f;
			m_fuelWeight = m_maxFuelWeight;
			weapon.arm();
			if (interceptors > 0)
				weapon.addValidIndex(WeaponTypes.missileInterceptor);
		}

		public int getThrottle()
		{
			return throttlePosition;
		}
		public byte getRunwayStatus()
		{
			return Convert.ToByte(isOnRunway);
		}
		public bool getAfterburnerStatus()
		{
			return afterburnersActive;
		}

		private void askForSpectatorMode()
		{
			System.Diagnostics.Trace.WriteLine("Asked for spectinator in Aircraft");
			//If we're in a Team Death or OOO, if there is one object remaining,
			//no spectator since game has ended.
			//But if this is FFA, then that one object is player, so if we're in FFA,
			//2 or more objects need to be alive
			Client.spectatorPending = (Options.mode != Options.Modes.freeForAll && !Interaction.areAtMost(1)) ||
				(Options.mode == Options.Modes.freeForAll && !Interaction.areAtMost(1));
			System.Diagnostics.Trace.WriteLine(Options.mode == Options.Modes.freeForAll && !Interaction.areAtMost(0));
			System.Diagnostics.Trace.WriteLine(Options.mode != Options.Modes.freeForAll && !Interaction.areAtMost(1));
		}

		private void enterSpectatorMode()
		{
			System.Diagnostics.Trace.WriteLine("In enterSpectator, freeing resources for " + name);
			freeResources();
			isAI = true;
			autoPlayTarget = true;
			System.Diagnostics.Trace.WriteLine("In enterSpectator, Now loading sounds for " + name);
			loadSounds();
			requestingSpectator = false;
			System.Diagnostics.Trace.WriteLine("Initialized for spectator");
		}

		private void exitSpectatorMode()
		{
			requestingCancelSpectator = false;
			freeResources();
			autoPlayTarget = false;
			loadSounds();
		}

		/// <summary>
		/// Holders will tick even if host has not started game yet. So that if the host exits the game, waitForProjectors will not hang and objects will clean up.
		/// </summary>
		/// <returns>True if the host has not started the game yet, false otherwise.</returns>
		private bool waitingForHost()
		{
			if (!Options.isPlayingOnline)
				return false;
			if (Options.mode == Options.Modes.freeForAll)
				return false;
			//This is a game with a host.
			return !Client.hostStartedGame;
		}

		/// <summary>
		/// If this person is the host, will start the game if the proper conditions exist.
		/// </summary>
		private void startGame()
		{
			BinaryReader resp = null;
			if (Client.gameHost && DXInput.isFirstPress(Key.Return)) {
				resp = Client.getResponse(CSCommon.buildCMDString(CSCommon.cmd_requestStartGame));
				if (resp.ReadByte() == (byte)1)
					Client.hostStartedGame = true;
				else
					DSound.playAndWait(DSound.NSoundPath + "\\cn.wav");
			} //if pressed ENTER
		}

		private void warnLandingGear()
		{
			if (!isLandingGearRetracted && z > retractGearAltitude + 1000.0 && isNextRoundtime(ref gearWarningTime, maxGearWarningTime)) {
				playRIO(soundPath + "he3" + Common.getRandom(1, 2) + ".wav");
				maxGearWarningTime = Common.getRandom(15000, 60000);
			}
		}

		private void announceTargetVertical()
		{
			if (!weapon.isValidLock())
				return;
			Projector p = weapon.getLockedTarget();
			if (targetState != TargetState.ascending && p.z > z && p.z > ctz) {
				playRIO(soundPath + "he91.wav");
				targetState = TargetState.ascending;
				ctz = p.z;
			} else if (targetState != TargetState.descending && p.z < z && p.z < ctz) {
				playRIO(soundPath + "he8" + Common.getRandom(1, 2) + ".wav");
				targetState = TargetState.descending;
				ctz = p.z;
			}
		}

		private void bank()
		{
			try {
				if (bankAngle == 0)
					return;
				if (Math.Abs(bankAngle) == 90) {
					if (speed <= 400.0)
						turnOffEngine();
					return;
				} //90 completely vertical
				int pDir = direction;
				float turnRate = getRateOfTurn();
				if (turnRate >= 0.0f && turnRate <= 1.0f)
					turnRate = 1.0f;
				turn(turnRate);
				if (!isAI && pDir != direction)
					speakCourseChange();
			}
			catch (Exception e) {
				Common.handleError(e, "" + getRateOfTurn() + ", " + bankAngle + ", " + getHorizontalSpeed());
			}
		}

		private void turnOnEngine()
		{
			enginesOff = false;
		}


		/// <summary>
		/// Determines if the aircraft can roll, based on the angles. If the aircraft is rolling both
		/// vertically and horizontally, this method returns false.
		/// </summary>
		/// <returns>True if it can roll, false otherwise.</returns>
		private bool isRollConditions()
		{
			return virtualNoseAngle == -90 ^ Math.Abs(bankAngle) == 90;
		}
		private void roll()
		{
			if (!isRollConditions()) {
				rollTime = 0;
				if (barrelRoll())
					rollState = RollState.barrelRoll;
				else
					rollState = RollState.none;
				return;
			}
			if (Math.Abs(virtualNoseAngle) == 90)
				rollState = RollState.loop;
			else
				rollState = RollState.aileronRoll;
			if ((rollTime += Common.intervalMS) > 1000) {
				if (rollState == RollState.loop) {
					changeDirectionTo((int)Degrees.getDegreeValue(direction + 180));
					if (!inSplitS) //split-S needs angle to be -90 so it knows the loop is done; will adjust to 0 on its own
						virtualNoseAngle = 0;
				}
				if (facingState == FacingState.upright) {
					facingState = FacingState.inverted;
					if (Options.mode == Options.Modes.training && rollState == RollState.aileronRoll && currentStage == TrainingStages.splitS)
						completedTrainingStage = true; //move to TrainingStages.loop
				} else {
					facingState = FacingState.upright;
					if (Options.mode == Options.Modes.training && rollState == RollState.loop && (currentStage == TrainingStages.loop || currentStage == TrainingStages.splitS2))
						completedTrainingStage = true; //move to TrainingStages.HudCourse or killFighter2
				}
				rollTime = 0;
				if (!isAI)
					SelfVoice.NLS("i" + (int)facingState + ".wav", true, true);
			}
		}

		private bool conditionForBarrelRoll()
		{
			if (inSplitS || isLeveling)
				return false;
			int v = Math.Abs(virtualNoseAngle);
			int b = Math.Abs(bankAngle);
			return v >= 30 && v < 80 && b >= 42 && b < 90;
		}

		private bool barrelRoll()
		{
			if (!conditionForBarrelRoll())
				return false;
			rollState = RollState.barrelRoll;
			int pDir = direction;
			try {
				turn(getRateOfTurn());
			}
			catch (OverflowException e) {
				throw new OverflowException(e.Message + " Bank angle: " + bankAngle + " AOA: " + virtualNoseAngle + " rate of turn: " + getRateOfTurn());
			}
			if (!isAI && pDir != direction)
				speakCourseChange(true);
			return true;
		}

		protected override float getRateOfTurn()
		{
			float turnRate = base.getRateOfTurn();
			if (turnRate < 0.0f)
				return turnRate + (conditionForBarrelRoll() && facingState != FacingState.inverted ? -barrelRate : 0.0f);
			else
				return turnRate + (conditionForBarrelRoll() && facingState != FacingState.inverted ? barrelRate : 0.0f);
		}

		private void splitS()
		{
			if (inBarrelRoll || isLeveling)
				return;
			if (!inSplitS) {
				bankAngle = 90;
				System.Diagnostics.Debug.WriteLine("bank angle: " + bankAngle);
				inSplitS = true;
			}
			if (rollTime % 1000 == 0)
				System.Diagnostics.Debug.WriteLine("Roll time " + rollTime);
			if (facingState == FacingState.inverted) {
				bankAngle = 0;
				virtualNoseAngle = -90;
			}
			if (virtualNoseAngle == -90 && facingState == FacingState.upright) {
				virtualNoseAngle = 0;
				inSplitS = false;
				System.Diagnostics.Debug.WriteLine("Came out of split s.");
			}
		}

		private void playRollSound()
		{
			if (rollState == RollState.none) {
				if (DSound.isPlaying(barrelRollSound))
					barrelRollSound.stop();
				if (DSound.isPlaying(aileronRollSound))
					aileronRollSound.stop();
				return;
			}
			ExtendedAudioBuffer rollSound = (rollState == RollState.barrelRoll ? barrelRollSound : aileronRollSound);
			if (DSound.isPlaying(barrelRollSound) && rollState != RollState.barrelRoll)
				barrelRollSound.stop();
			if (DSound.isPlaying(aileronRollSound) && (rollState != RollState.aileronRoll || rollState == RollState.loop))
				aileronRollSound.stop();
			if (rollState == RollState.aileronRoll || rollState == RollState.barrelRoll) { //banking
				if (bankAngle > 0)
					DSound.setPan(rollSound, facingState == FacingState.upright ? 1.0f : -1.0f);
				else
					DSound.setPan(rollSound, facingState == FacingState.upright ? -1.0f : 1.0f);
			} else //loop
				DSound.setPan(rollSound, 0.0f);
			playSound(rollSound, false, true);
		}

		public override void sendWeaponUpdate(byte[] data)
		{
			weapon.sendUpdate(data);
		}

		protected override int getWeight()
		{
			return base.getWeight() + weapon.getWeight();
		}

		private void leftBarrelRoll()
		{
			if (navigatingToSector())
				stopSectorNav();
			if (conditionForBarrelRoll() || inSplitS)
				return;
			virtualNoseAngle = 30;
			bankAngle = -50;
			inBarrelRoll = true;
		}

		private void rightBarrelRoll()
		{
			if (navigatingToSector())
				stopSectorNav();
			if (conditionForBarrelRoll() || inSplitS)
				return;
			virtualNoseAngle = 30;
			bankAngle = 50;
			inBarrelRoll = true;
		}

		private void doTraining()
		{
			if (currentStage == TrainingStages.turnedOnAfterburners) {
				completedTrainingStage = DSound.isPlaying(afFlame);
			}

			if (currentStage == TrainingStages.turnedOffAfterburners) {
				completedTrainingStage = !afterburnersActive;
			}

			if (currentStage == TrainingStages.hudSpeed) {
				completedTrainingStage = lastStatusCommand == Status.speedometer;
				if (!completedTrainingStage && lastStatusCommand != Status.none)
					playIncorrectTrainingCommand();
				incorrectCommand = false;
				lastStatusCommand = Status.none;
			}

			if (currentStage == TrainingStages.takeoff) {
				completedTrainingStage = !isOnRunway;
			}

			if (currentStage == TrainingStages.levelOut) {
				completedTrainingStage = virtualNoseAngle == 0;
			}

			if (currentStage == TrainingStages.throttleUp) {
				completedTrainingStage = throttlePosition >= 7;
			}

			if (currentStage == TrainingStages.stopAccelerating) {
				completedTrainingStage = speed == matchSpeed;
			}

			if (currentStage == TrainingStages.putInGear) {
				if (z < 2000f)
					z = 2000f;
				completedTrainingStage = isLandingGearRetracted;
			}

			if (currentStage == TrainingStages.increaseAOA) {
				playAlarms = true;
				completedTrainingStage = z > minAltitude + 500f;
			}

			if (currentStage == TrainingStages.aboveHardDeck) {
				completedTrainingStage = z > minAltitude + 500f;
			}

			if (currentStage == TrainingStages.levelOutAboveHardDeck) {
				completedTrainingStage = virtualNoseAngle == 0;
			}

			if (currentStage == TrainingStages.throttleTo0) {
				completedTrainingStage = throttlePosition == minThrottlePosition;
			}

			if (currentStage == TrainingStages.throttleBackUp) {
				completedTrainingStage = DSound.isPlaying(afFlame) && speed == maxSpeed;
			}

			if (currentStage == TrainingStages.checkSpeed) {
				completedTrainingStage = lastStatusCommand == Status.speedometer;
				if (completedTrainingStage)
					lastStatusCommand = Status.none;
			}

			if (currentStage == TrainingStages.backOffAfterburners) {
				completedTrainingStage = !afterburnersActive;
			}

			if (currentStage == TrainingStages.courseTo30) {
				if (!saidInstructions)
					direction = 0;
				completedTrainingStage = direction == 30;
			}

			if (currentStage == TrainingStages.bankRightTo30) {
				completedTrainingStage = bankAngle >= 30;
				if (!completedTrainingStage && (didAction(Action.bankLeft) || didAction(Action.leftBarrelRoll) || didAction(Action.rightBarrelRoll)))
					playIncorrectTrainingCommand();
				else
					incorrectCommand = false;
			}

			if (currentStage == TrainingStages.checkTurnRate) {
				completedTrainingStage = lastStatusCommand == Status.turnRate;
				if (!completedTrainingStage && lastStatusCommand != Status.none)
					playIncorrectTrainingCommand();
				incorrectCommand = false;
				lastStatusCommand = Status.none;
			}

			if (currentStage == TrainingStages.brake) {
				completedTrainingStage = didAction(Action.brake);
			}

			if (currentStage == TrainingStages.checkTurnRate2) {
				completedTrainingStage = lastStatusCommand == Status.turnRate;
				if (!completedTrainingStage && lastStatusCommand != Status.none)
					playIncorrectTrainingCommand();
				incorrectCommand = false;
				lastStatusCommand = Status.none;
			}

			if (currentStage == TrainingStages.levelOut2) {
				completedTrainingStage = virtualNoseAngle == 0 && bankAngle == 0;
			}

			if (currentStage == TrainingStages.barrelRoll) {
				completedTrainingStage = DSound.isPlaying(barrelRollSound) && bankAngle > 0;
				if (!completedTrainingStage && (bankAngle < 0 || rollState == RollState.aileronRoll || rollState == RollState.loop))
					playIncorrectTrainingCommand();
				else
					incorrectCommand = false;
			}

			if (currentStage == TrainingStages.comeOutOfBarrelRoll) {
				completedTrainingStage = !DSound.isPlaying(barrelRollSound);
			}

			if (currentStage == TrainingStages.splitS) {
				//Stages are handled by roll()
			}

			if (currentStage == TrainingStages.loop) {
				if (DXInput.isJSEnabled())
					playTrainingMessage();
				//...
			}

			if (currentStage == TrainingStages.hudCourse) {
				completedTrainingStage = lastStatusCommand == Status.course;
				if (!completedTrainingStage && lastStatusCommand != Status.none)
					playIncorrectTrainingCommand();
				incorrectCommand = false;
				lastStatusCommand = Status.none;
			}

			if (currentStage == TrainingStages.lockOnFighter1) {
				if (saidInstructions) {
					int dir = Degrees.getDegreeValue(direction + 10);
					float px = x;
					float py = y;
					Degrees.moveObject(ref px, ref py, dir, 1f, 3f);
					Mission.trainer.x = px;
					Mission.trainer.y = py;
				}
				completedTrainingStage = weapon.isValidLock() && weapon.getLockedTarget().name.Equals("f1");
			}

			if (currentStage == TrainingStages.autoElevateToFighter1) {
				completedTrainingStage = isElevating;
			}

			if (currentStage == TrainingStages.matchFighter1Altitude) {
				int dir = Degrees.getDegreeValue(direction + 10);
				float px = x;
				float py = y;
				Degrees.moveObject(ref px, ref py, dir, 1f, 3f);
				Mission.trainer.x = px;
				Mission.trainer.y = py;
				completedTrainingStage = weapon.isValidLock() && z == weapon.getLockedTarget().z && DSound.isPlaying(targetSolutionSound); //otherwise trainer talks before solution kicks in.
			}

			if (currentStage == TrainingStages.solidToneOnFighter1) {
				if (!saidInstructions) {
					int dir = Degrees.getDegreeValue(direction + 10);
					float px = x;
					float py = y;
					Degrees.moveObject(ref px, ref py, dir, 1f, 3f);
					Mission.trainer.x = px;
					Mission.trainer.y = py;
					Mission.trainer.z = z;
				}
				//...
			}

			if (currentStage == TrainingStages.killFighter1) {
				completedTrainingStage = kf;
				if (completedTrainingStage)
					kf = false;
			}

			if (currentStage == TrainingStages.lockOnFighter2) {
				if (saidInstructions) {
					float px = x, py = y;
					int dir = Degrees.getDegreeValue(direction + 180);
					Degrees.moveObject(ref px, ref py, dir, 1f, 2f);
					Mission.trainer.x = px;
					Mission.trainer.y = py;
					Mission.trainer.direction = direction;
				}
				completedTrainingStage = weapon.isValidLock() && weapon.getLockedTarget().name.Equals("f2");
			}

			if (currentStage == TrainingStages.distanceBetweenFighter2) {
				completedTrainingStage = weapon.isValidLock() && getPosition(weapon.getLockedTarget()).distance >= 6.0;
			}

			if (currentStage == TrainingStages.splitS2) {
				completedTrainingStage = weapon.isValidLock() && getPosition(weapon.getLockedTarget()).isAhead;
				if (completedTrainingStage) {
					float px = x, py = y;
					int dir = Degrees.getDegreeValue(direction);
					Degrees.moveObject(ref px, ref py, dir, 1f, 2f);
					Mission.trainer.x = px;
					Mission.trainer.y = py;
				}
			}

			if (currentStage == TrainingStages.killFighter2) {
				completedTrainingStage = kf;
				if (completedTrainingStage)
					kf = false;
			}

			if (currentStage == TrainingStages.killFighter3) {
				completedTrainingStage = kf;
				if (completedTrainingStage)
					kf = false;
			}

			if (currentStage == TrainingStages.descendTo5000) {
				if (!saidInstructions) {
					requestLand();
					x = 0f;
					y = 10f;
					direction = getPosition(Mission.landingBeacon).degrees;
				}
				completedTrainingStage = z <= 5000.0;
			}

			if (currentStage == TrainingStages.levelOut3) {
				completedTrainingStage = virtualNoseAngle == 0;
			}

			if (currentStage == TrainingStages.threeMilesAway) {
				completedTrainingStage = getPosition(Mission.landingBeacon).distance <= 5f;
			}

			if (currentStage == TrainingStages.slowToStallWarning) {
				completedTrainingStage = speed <= 300f;
			}

			if (currentStage == TrainingStages.descendTo1000) {
				completedTrainingStage = z <= 1000f;
			}

			if (currentStage == TrainingStages.takeOutGear) {
				completedTrainingStage = !isLandingGearRetracted;
			}

			if (currentStage == TrainingStages.land) {
				completedTrainingStage = cause == Interaction.Cause.successfulLanding;
				if (completedTrainingStage) {
					currentStage = TrainingStages.goodLanding;
					Mission.trainingFinished = true;
					saidInstructions = false;
					playTrainingMessage();
				}
				if (cause == Interaction.Cause.destroyedByImpact) {
					saidInstructions = false;
					completedTrainingStage = true;
					currentStage = TrainingStages.badLanding;
					Mission.trainingFinished = true;
					playTrainingMessage();
				}
			}

			if (!completedTrainingStage) {
				if (currentStage != TrainingStages.loop) //loop only plays if js active
					playTrainingMessage();
				if (!saidInstructions && currentStage == TrainingStages.lockOnFighter1)
					Mission.createNewTrainer(Mission.trainer = new Aircraft(0, 1000, "f1", true, false, false));
				if (!saidInstructions && currentStage == TrainingStages.lockOnFighter2)
					Mission.createNewTrainer(Mission.trainer = new Aircraft(0, 1000, "f2", false, true, false));
				if (!saidInstructions && currentStage == TrainingStages.killFighter3) {
					Mission.createNewTrainer(Mission.trainer = new Aircraft(0, 1500, "f3", false, false, true));
					float px = x;
					float py = y;
					Degrees.moveObject(ref px, ref py, Degrees.getDegreeValue(direction - 30), 1f, 7f);
					Mission.trainer.x = px;
					Mission.trainer.y = py;
				}

				saidInstructions = true;
			} else {
				if (currentStage == TrainingStages.hudCourse || currentStage == TrainingStages.killFighter2)
					aileronRollSound.stop();
				currentStage++;
				saidInstructions = false;
				completedTrainingStage = false;
			}
		}

		/// <summary>
		/// Plays the training message.
		/// </summary>
		/// <param name="file">The file name to play. It should be an Ogg file.</param>
		private void playTrainingMessage(String file)
		{
			lastTrainingFile = file;
			Common.playUntilKeyPress(file);
		}

		private void playTrainingMessage()
		{
			if (saidInstructions)
				return;
			if (String.IsNullOrEmpty(trainingNames[(int)currentStage]))
				return;
			String mask = trainingNames[(int)currentStage] + "*.ogg";
			Regex regEx = new Regex(trainingNames[(int)currentStage] + "[0-9]+", RegexOptions.Compiled);
			String[] files = Directory.GetFiles(DSound.SoundPath, mask);
			Array.Sort(files);
			List<String> filtered = new List<string>();
			for (int i = 0; i < files.Length; i++) {
				if (regEx.IsMatch(files[i]))
					filtered.Add(files[i]);
			}
			foreach (String file in filtered) {
				if (!file.Contains("_") || (file.Contains("_k") && !DXInput.isJSEnabled()) || (file.Contains("_j") && DXInput.isJSEnabled()))
					playTrainingMessage(file);
			}
		}

		private void playIncorrectTrainingCommand()
		{
			if (incorrectCommand)
				return;
			Common.playUntilKeyPress(DSound.SoundPath + "\\tir.ogg");
			playTrainingMessage(DSound.SoundPath + String.Format("\\i_{0}_{1}.ogg", trainingNames[(int)currentStage], (DXInput.isJSEnabled()) ? "j" : "k"));
			incorrectCommand = true;
		}

		private bool blockKill()
		{
			if (requestedLand)
				return false;
			if (Options.mode == Options.Modes.racing) {
				if (cause == Interaction.Cause.successfulLanding || cause == Interaction.Cause.quitGame || damage <= 0 || Options.autoPlay || Common.ACBMode || !(Common.getRandom(1, 3) == 1))
					return false;
				if (!isAI && Interaction.getRanks() == null) {
					OggBuffer l = DSound.loadOgg(DSound.NSoundPath + "\\ld1.ogg");
					l.play();
					while (l.isPlaying())
						Thread.Sleep(100);
					l.stopOgg();
					l = null;
					((Aircraft)Mission.player).requestLand();
					return true;
				} else
					return false;
			} //if racing mode
			else if (Options.mode == Options.Modes.deathMatch) { //Death match land will not block kill on this craft.
				if (isAI && !Options.autoPlay && !Common.ACBMode && Interaction.length == 3 && Common.getRandom(1, 3) == 1) {
					while (Interaction.isReadingDestroyed)
						Thread.Sleep(5);
					OggBuffer l = DSound.loadOgg(DSound.NSoundPath + "\\ld1.ogg");
					l.play();
					while (l.isPlaying())
						Thread.Sleep(100);
					l.stopOgg();
					l = null;
					((Aircraft)Mission.player).requestLand();
				}
			}
			return false;
		}

		public void requestLand()
		{
			switchWeapon(WeaponTypes.landingBeaconLock);
			weapon.lockIndex = Mission.landingBeacon.id;
			requestedLand = true;
			cause = Interaction.Cause.none;
		}

		private void playCourseClick()
		{
			if (direction != lastDirection) {
				float x = 0;
				float y = 0;
				float z = 0;
				x = this.x;
				y = this.y;
				z = this.z;
				Degrees.moveObject(ref x, ref y, 0, 1f, 3f);
				DSound.PlaySound3d(courseClickSound, true, false, x, z, y);
			}
		}


	} //class
} //namespace