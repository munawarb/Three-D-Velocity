/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
using SharpDX.DirectSound;
namespace TDV
{
	public abstract class Projector : Common.Hittable
	{
		public enum TeamColors
		{
			blue,
			green,
			red,
			yellow
		}

		#region addOnVariables
		public int extraFuelTanks, extraCruiseMissiles, interceptors;
		public bool flyingCruiseMissile;
		protected bool dirty;
		#endregion
		private OnlineRole m_role;
		protected bool requestingSpectator, requestingCancelSpectator;
		protected bool autoPlayTarget;
		protected bool isMuted;

		protected float curDir;
		protected Object dataLocker;
		private int m_bankAngle;
		private int m_rudderTurnRate; //expressed in degrees per second, this is when turning on rudders only.
		private TeamColors m_team;

		public OnlineRole role
		{
			get { return m_role; }
			set { m_role = value; }
		}

		protected int rudderTurnRate
		{
			get { return m_rudderTurnRate; }
			set { m_rudderTurnRate = value; }
		}
		protected int bankAngle
		{
			get { return m_bankAngle; }
			set { m_bankAngle = value; }
		}

		public TeamColors team
		{
			get { return m_team; }
			set { m_team = value; Interaction.incrementTeam(value); }
		}

		protected BinaryReader queue;
		protected MemoryStream stream;
		public bool unlisted;
		private List<SecondarySoundBuffer> ctrlVolume;
		private List<int> volumes;
		private List<bool> looping;

		private double m_engineRadius;
		private int m_engineDamagePoints;

		protected int engineDamagePoints
		{
			get { return m_engineDamagePoints; }
			set { m_engineDamagePoints = value; }
		}
		private int m_maxEngineDamagePoints;

		protected int maxEngineDamagePoints
		{
			get { return m_maxEngineDamagePoints; }
			set { m_maxEngineDamagePoints = value; }
		}
		private double m_radius;
		private bool m_forceStareo, doneDestroyedBy;


		public bool showInList = true;
		public bool requestedLand;
		public bool isRequestedTerminated;
		private bool m_isProjectorStopped;
		private int m_maxWeight;
		protected float m_fuelWeight;
		protected float m_maxFuelWeight;
		private double m_height;

		private String m_id;
		private bool m_isAI;
		private Interaction.Cause m_cause;
		////amount of degrees a vehicle will turn on each tick.
		private byte m_turnSpeed = 2;
		private string m_name;
		private int m_damage;
		private int m_maxDamagePoints;
		public double x;
		public double y;
		public double z;
		private double m_speed; //total speed output of aircraft
		//represents airspeed
		private int m_rpm; //engine rpm, can go up to
		//Projector.maxRPM
		private static int m_maxRPM = 40000;
		private static int m_idleRPM = 20000;
		private double m_maxSpeed;
		private double m_matchSpeed;
		private double m_totalDistance;
		private int m_direction;
		private int m_noseAngle;
		////Acceleration (in miles/second)
		private byte m_accelerationSpeed;
		////amount of speed a jet will lose if decelerating (miles/second)
		private byte m_decelerationSpeed;
		private bool m_isObject; //determines if aircraft or object

		public double radius
		{
			get { return m_radius; }
			set { m_radius = value; }
		}

		public bool isObject
		{
			get { return (m_isObject); }
			set { m_isObject = value; }
		}

		public static int maxRPM
		{
			get { return (m_maxRPM); }
		}

		public static int idleRPM
		{
			get { return (m_idleRPM); }
		}

		public double engineRadius
		{
			get { return (m_engineRadius); }
			set { m_engineRadius = value; }
		}

		//current rpm.
		//engineSpeed will return the current speed output of engine
		public int rpm
		{
			get { return (m_rpm); }
			set { m_rpm = value; }
		}


		public bool isProjectorStopped
		{
			get { return (m_isProjectorStopped); }
			set { m_isProjectorStopped = value; }
		}
		public double height
		{
			get { return (m_height); }
			set { m_height = value; }
		}

		public byte turnSpeed
		{
			get { return (m_turnSpeed); }
			set { m_turnSpeed = value; }
		}
		public bool isTerminated
		{
			get { return (isProjectorStopped); }
		}

		//This ID is obtained when this projector is added to the Interaction.theArray ArrayList.
		public string id
		{
			get { return (m_id); }
		}

		//Returns true if this instance of Projector is controlled by AI, false otherwise
		public bool isAI
		{
			get { return (m_isAI); }
			set { m_isAI = value; }
		}
		public string name
		{
			get { return (m_name); }
			set { m_name = value; }
		}

		//Returns or sets the maximum speed of the engines, in miles/hour.
		//Note: This value does not represent the actual speed at which the object will be traveling;
		//it only represents the speed that should be achieved by the engine thrust, neglecting nose angle.
		//If an object should not accelerate and should maintain maxspeed,
		// a class should call the neutralizeSpeed() method, passing to it the neutralization speed
		public virtual double maxSpeed
		{
			get { return (m_maxSpeed); }
			set { m_maxSpeed = value; }
		}

		//gets or sets the speed the projector is to match
		//The maxspeed determines the maximum possible speed of the projector, whereas this property only determines the new speed for throttle up/down
		public virtual double matchSpeed
		{
			get { return (m_matchSpeed); }
			set { m_matchSpeed = value; }
		}

		//Represents angular velocity of engine
		//Viz. rpm.
		public double speed
		{
			get { return (m_speed); }
			set { m_speed = value; }
		}

		//Returns or sets the speed of the engines, in miles/hour.
		//Note: This value does not represent the actual speed at which the object will be traveling;
		//it only represents the speed that should be achieved by the engine thrust, neglecting nose angle.
		public double engineSpeed
		{
			get
			{
				return ((double)rpm * Degrees.getCircumference(engineRadius));
			}
		}

		//Acceleration in miles per second
		public virtual byte accelerationSpeed
		{
			get { return (m_accelerationSpeed); }
			set { m_accelerationSpeed = value; }
		}

		////deceleration speed in miles/second
		public virtual byte decelerationSpeed
		{
			get { return (m_decelerationSpeed); }
			set { m_decelerationSpeed = value; }
		}

		////The total horizontal distance in miles a class has traveled since its instantiation
		public double totalDistance
		{
			get { return (m_totalDistance); }
			set { m_totalDistance = value; }
		}

		public int direction
		{
			get { return (m_direction); }
			set { m_direction = value; }
		}

		//The nose angle of the object viz. upward or downward tilt
		//This value is expressed in degree value, where 0 is assumed to be level, and an integer from 1 to 90 is an upward angle of the nose,
		//retrieved from the angle between the stern of the object and the ground,
		//which is assumed to be a straight, horizontal line.
		//Note: if the object is facing downward, the nose angle will be read from 270 (straight downward) to 0, where all values inbetween==from 270 to 359--
		//Are gradual downward slopes of the craft.
		public int noseAngle
		{
			get { return (Degrees.getDegreeValue(virtualNoseAngle)); }
		}
		//The nose angle of the object viz. upward or downward tilt
		//This value is expressed in degree value, where 0 is assumed to be level, and a positive integer is an upward angle of the nose,
		//and a negative value is a downward tilt.
		//retrieved from the angle between the stern of the object and the ground,
		//which is assumed to be a straight, horizontal line.
		public int virtualNoseAngle
		{
			get { return (m_noseAngle); }
			set { m_noseAngle = value; }
		}
		public int playerFriendlyAOA
		{
			get
			{
				if (virtualNoseAngle >= 0 && virtualNoseAngle <= 90)
					return virtualNoseAngle;
				else
					return 180 - (-virtualNoseAngle);
				//VNA is negative
			}
		}


		public Projector(string name)
		{
			this.name = name;
			//If playing online, projector's ID will be set
			//by some other, external operation,
			//because it will be server-specific.
			//Weapons will set their own IDs.
			if (!Options.isPlayingOnline && !Options.isLoading && !(this is WeaponBase))
				setID();
			volumes = null;
			ctrlVolume = null;
			if (Options.isPlayingOnline)
			{
				queue = new BinaryReader(stream = new MemoryStream()); //queued data to be processed
				dataLocker = new object();
			}
			m_rudderTurnRate = 3;
			m_cause = Interaction.Cause.none;
		}
		public Projector(Int16 direction, Int16 maxSpeed, string name, bool isAI)
			: this(name)
		{
			this.direction = direction;
			this.maxSpeed = maxSpeed;

			this.isAI = isAI;

			double circumference = (double)maxSpeed / (double)maxRPM;
			//circumference is 2PIR, so get the radius
			double r = circumference / Math.PI; //diameter
			r /= 2.0;
			//this is the radius that we need to put the engine at
			//to achieve maxSpeed at maxRPM.
			engineRadius = r;
			setDamagePoints(100);
		}

		public void updateTotalDistance()
		{
			totalDistance += getHorizontalSpeed(Common.intervalMS);
		}
		public virtual void move()
		{
			Degrees.moveObject(ref x, ref y, ref z,
						 direction,
						 new Range(getHorizontalSpeed(1.0), getVerticalSpeed(1.0)), Common.intervalMS);
		}

		//Below, accelerate and decelerate cast the acceleration speed value to miles per hour--
		//The original value is in miles/second, but it must be in miles per hour to get an accurate per tick acceleration,
		//because convertToTick() assumes that the value passed to it is in a units/hour ratio.
		public virtual bool accelerate(int accelerationSpeed)
		{
			bool speedChanged = speed < maxSpeed;
			if ((speed < maxSpeed))
			{
				speed += Common.convertToTickDistance(accelerationSpeed * 60.0 * 60.0);
				if ((speed > maxSpeed))
				{
					speed = maxSpeed;
				}
			}
			return (speedChanged);
		}

		////Accelerates with default acceleration speed
		public virtual bool accelerate()
		{
			return (accelerate(accelerationSpeed));
		}

		public virtual bool decelerate(int decelerationSpeed)
		{
			bool speedChanged = speed > 0;
			if ((speed > 0))
			{
				speed -= Common.convertToTickDistance(decelerationSpeed * 60.0 * 60.0);
				if ((speed < 0))
				{
					speed = 0;
				}
			}
			return (speedChanged);
		}

		////Calls decelerate with default deceleration speed
		public virtual bool decelerate()
		{
			return (decelerate(decelerationSpeed));
		}

		////This method should be called with the speed of an object if
		////the object should not accelerate or decelerate
		////If the speed property is modified after this call, the object will remain stationary
		public void neutralizeSpeed(double n)
		{
			speed = n;
			maxSpeed = speed;
			accelerationSpeed = 0;
			decelerationSpeed = 0;
		}

		public override bool Equals(object obj)
		{
			Projector obj2 = (Projector)obj;
			return (this.id == obj2.id);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		////Returns the Velocity structure associated with the current respective course:
		////viz. noseAngle for verticalVelocity and direction for horizontalVelocity,
		////and the engine speed, in miles per hour for the horizontal velocity, and feet per hour for vertical velocity.
		public Velocity getHorizontalEngineVelocity()
		{
			return (new Velocity(direction, engineSpeed));
		}
		public Velocity getVerticalEngineVelocity()
		{
			return (new Velocity(noseAngle, speed));
		}
		////Returns the horizontal speed in miles/hour at the current nose angle
		////Note: if a value is supplied to  the parameter ms, the function will return the speed over the millisecond interval viz. speed per ms milliseconds
		public double getHorizontalSpeed(double ms)
		{
			double milesPerHour = (Degrees.getHVSpeed(getVerticalEngineVelocity()).horizontalDistance);
			////return horizontal speed in miles/hour
			if (ms > 0.0) milesPerHour = Common.convertToTickDistance(milesPerHour, ms);
			return (milesPerHour);
		}

		public double getHorizontalSpeed()
		{
			return (getHorizontalSpeed(0.0));
		}

		////Returns the vertical speed in feet/hour at the current nose angle
		////Note: if a value is supplied to  the parameter ms, the function will return the speed over the millisecond interval viz. speed per ms milliseconds
		public double getVerticalSpeed(double ms)
		{
			double feetPerHour = Common.convertToFeet(Degrees.getHVSpeed(getVerticalEngineVelocity()).verticalDistance);
			if (ms > 0.0) feetPerHour = Common.convertToTickDistance(feetPerHour, ms);
			return (feetPerHour);
		}

		public double getVerticalSpeed()
		{
			return (getVerticalSpeed(0.0));
		}

		////hittable implementation
		public Interaction.Cause cause
		{
			get { return (m_cause); }
			set { m_cause = value; }
		}

		public bool forceStareo
		{
			get { return (m_forceStareo); }
			set { m_forceStareo = value; }
		}

		public int maxDamagePoints
		{
			get { return (m_maxDamagePoints); }
			set { m_maxDamagePoints = value; }
		}
		public int damage
		{
			get { return (m_damage); }
			set { m_damage = value; }
		}
		public bool hit()
		{
			return damage <= 0 || cause != Interaction.Cause.none;
		}

		public virtual void hit(int decrementValue, Interaction.Cause cause)
		{
			damage -= decrementValue;
			System.Diagnostics.Trace.WriteLineIf(!isAI, String.Format("Hit: decrement {0}, damage: {1}, called from: {2}", decrementValue, damage, (new System.Diagnostics.StackTrace()).ToString()));
			if (decrementValue == 0 || damage <= 0)
				m_cause = cause;
		}

		//Should only be called by lts
		public virtual void hit(bool lts)
		{

		}

		//The below methods are mask methods for directX
		//All projectors will have the ability to play sounds from their perspectives
		public void playSound(SecondarySoundBuffer theSound, bool stopFlag, bool loopFlag)
		{
			lock (this)
			{
				if (isAI && !autoPlayTarget)
				{
					if (!forceStareo)
						DSound.PlaySound3d(theSound, stopFlag, loopFlag, x, z, y);
					else
						DSound.PlaySound(theSound, stopFlag, loopFlag);
				}
				else
				{
					DSound.PlaySound(theSound, stopFlag, loopFlag);
				}
			}
		}
		public SecondarySoundBuffer loadSound(string filename)
		{
			SecondarySoundBuffer s = null;
			if (isAI && !autoPlayTarget)
			{
				if (!forceStareo)
					s = DSound.LoadSound3d(filename);
				else
					s = DSound.LoadSound(filename);
			}
			else //Either this is not AI or this is autoPlayTarget
				s = DSound.LoadSound(filename);
			return s;
		}

		private string prepend
		{
			get
			{
				if (isAI && !autoPlayTarget)
					return ("a_");
				else
					return ("");
			}
		}

		////Returns soundPath+prepended characters for mono sound if necessary
		public string soundPath
		{
			get { return (DSound.SoundPath + "\\" + prepend); }
		}

		public override string ToString()
		{
			return name;
		}
		public virtual void increaseNoseAngle()
		{
			if ((virtualNoseAngle += 2) > 90)
				virtualNoseAngle = 90;
		}
		public virtual void decreaseNoseAngle()
		{
			if ((virtualNoseAngle -= 2) < -90)
				virtualNoseAngle = -90;
		}
		/// <summary>
		/// Turns the object based on the sign of the rate parameter.
		/// </summary>
		/// <param name="rate">The degrees per second rate of turn. If negative, the object will turn left, else it will turn right.</param>
		protected void turn(float rate)
		{
			curDir += (float)(rate * Common.intervalMS / 1000);
			curDir = Degrees.getDegreeValue(curDir);
			Interlocked.Exchange(ref m_direction, (int)Math.Floor(curDir));
		}
		protected virtual void turnLeft()
		{
			turn(-m_rudderTurnRate);
		}
		protected virtual void turnRight()
		{
			turn(m_rudderTurnRate);
		}
		public virtual void brake()
		{
			speed -= speed / 4.0;
			if (speed <= 0.0)
				speed = 0.0;
		}
		public virtual void bankLeft()
		{
			if ((bankAngle -= 5) < -90)
				bankAngle = -90;
		}
		public virtual void bankRight()
		{
			if ((bankAngle += 5) > 90)
				bankAngle = 90;
		}
		public virtual void brakeLeft()
		{
			brake();
			bankLeft();
		}
		public virtual void brakeRight()
		{
			brake();
			bankRight();
		}

		/// <summary>
		/// Signals this projector that it needs to stop, and some threads are waiting for it to stop. This method does not actually terminate the object.
		/// </summary>
		public virtual void requestingTerminate()
		{
			isRequestedTerminated = true;
		}

		/// <summary>
		/// Cancels a request to terminate an object. This method only has an effect if the object in question is not terminated yet.
		/// To check if an object has been terminated, call isTerminated.
		/// </summary>

		public void stopRequestingTerminate()
		{
			isRequestedTerminated = false;
			isProjectorStopped = false;
		}

		//Sets the radius and height of the object.
		//These values are double values and pertain to the standard unit of measurement for the game.
		//For instance, a height of 1.0 signifies that this object spans 1 foot out from center in each direction
		public void setSpan(double h, double r)
		{
			height = h;
			radius = r;
		}
		public bool isTerminating()
		{
			return (isRequestedTerminated);
		}

		////This method is called by
		////Interaction.Kill to allow the
		////object to do any last cleanup.
		public virtual void cleanUp()
		{

		}

		////masked method for dSound
		////.unloadSound method
		public void unloadSound(SecondarySoundBuffer s)
		{
			DSound.unloadSound(ref s);
		}
		public virtual void Dispose()
		{
			////stub
		}

		public virtual bool readyToTerminate()
		{
			return (isProjectorStopped);
		}

		//Used to consume fuel from projector.
		//Objects should override this method for their own implementation.
		protected virtual void useFuel()
		{
			m_fuelWeight -= (float)Common.convertToTickDistance(speed * 3.525);
			if (m_fuelWeight < 0.0f)
				m_fuelWeight = 0.0f;
		}

		public void setDamagePoints(int maxDamage)
		{
			damage = maxDamage;
			maxDamagePoints = maxDamage;
		}

		public bool isLanded()
		{
			return (z <= 0.0);
		}

		public virtual void mute(bool hardMute)
		{
			if (volumes == null || isMuted)
				return; //object may not have defined any sounds to be muted.
			//Or we've already muted.
			/*Holders will keep calling mute()
			 * Even though they may already have called it.
			 * This way, objects that are inserted into the holders after the mute operation will also be muted.
			 * */
			for (int i = 0; i < ctrlVolume.Count; i++)
			{
				volumes.Add(ctrlVolume[i].Volume);
				if (ctrlVolume[i].Volume > -8000)
					ctrlVolume[i].Volume = -8000;
				if (hardMute)
				{
					looping.Add(DSound.isLooping(ctrlVolume[i]));
					ctrlVolume[i].Stop();
				}
			}
			isMuted = true;
		}

		public virtual void mute()
		{
			mute(false);
		}

		public virtual void unmute()
		{
			if (volumes == null || !isMuted || ctrlVolume.Count != volumes.Count)
				return;
			for (int i = 0; i < ctrlVolume.Count; i++)
			{
				ctrlVolume[i].Volume = volumes[i];
				if (looping.Count > 0 && looping[i])
					ctrlVolume[i].Play(0, PlayFlags.Looping);
			}
			volumes.Clear();
			looping.Clear();
			isMuted = false;
		}

		/// <summary>
		///  Saves this projector's data.
		/// </summary>
		/// <param name="w">The BinaryWriter to save to.</param>
		/// <param name="saveName">If true, the name will be saved before the ID is saved. Otherwise, the name will not be saved.</param>
		public virtual void save(BinaryWriter w, bool saveName)
		{
			if (saveName)
				w.Write(name);
			w.Write(id);
			w.Write(isRequestedTerminated);
			w.Write(isProjectorStopped);
			w.Write(showInList);
			w.Write(x);
			w.Write(y);
			w.Write(z);
			w.Write(direction);
			w.Write(virtualNoseAngle);
			w.Write(damage);
			w.Write(maxDamagePoints);
			w.Write(totalDistance);
		}

		/// <summary>
		///  Saves this projector's data with the name.
		/// </summary>
		/// <param name="w">The BinaryWriter to save to.</param>
		public virtual void save(BinaryWriter w)
		{
			save(w, true);
		}


		public virtual bool load()
		{
			BinaryReader r = Common.inFile;
			setID(r.ReadString());
			isRequestedTerminated = r.ReadBoolean();
			isProjectorStopped = r.ReadBoolean();
			showInList = r.ReadBoolean();
			x = r.ReadDouble();
			y = r.ReadDouble();
			z = r.ReadDouble();
			direction = r.ReadInt32();
			virtualNoseAngle = r.ReadInt32();
			damage = r.ReadInt32();
			maxDamagePoints = r.ReadInt32();
			totalDistance = r.ReadDouble();
			return true;
		}

		/// <summary>
		/// Gets the relative position for a target.
		/// </summary>
		/// <param name="target">The object whose position needs to be obtained</param>
		/// <returns>A RelativePosition structure</returns>
		protected RelativePosition getPosition(Projector target)
		{
			return Degrees.getPosition(x, y, z, direction,
				target.x, target.y, target.z, target.direction,
				target.isObject);
		}

		protected bool outOfFuel()
		{
			if (m_fuelWeight <= 1.0f)
				return true;
			return false;
		}

		public virtual void revive()
		{
			isRequestedTerminated = false;
			isProjectorStopped = false;
			cause = Interaction.Cause.none;
			setDamagePoints(maxDamagePoints);
		}

		//Gets the damage percentage
		//of this projector.
		public int getHealthPercent()
		{
			return (int)
				((double)damage / (double)maxDamagePoints * 100.0);
		}

		public int getEngineDamagePercent()
		{
			return (int)
					((double)engineDamagePoints / (double)maxEngineDamagePoints * 100.0);
		}

		protected void addVolume(SecondarySoundBuffer buffer)
		{
			if (ctrlVolume == null)
			{
				ctrlVolume = new List<SecondarySoundBuffer>();
				volumes = new List<int>();
				looping = new List<bool>();
			}
			ctrlVolume.Add(buffer);
		}

		public bool collidesWith(Projector target)
		{
			double d1 = Math.Pow(x - target.x, 2);
			double d2 = Math.Pow(y - target.y, 2);
			double s = Math.Sqrt(d1 + d2);
			//If s < r1 + r2 
			//and the distance between the z values is < the heights of these objects,
			//then these two spheres
			//have collided.
			return s < (radius + target.radius)
				&& Math.Abs(z - target.z) < height + target.height;
		}

		public void setID(String id, bool addToObjectTable)
		{
			if (addToObjectTable)
				m_id = Interaction.addToObjectTable(id, this);
			else
				m_id = id;
		}

		public void setID(String id)
		{
			setID(id, true);
		}

		public void setID()
		{
			m_id = Interaction.addToObjectTable(this);
		}

		public void queueData(sbyte type, byte[] data)
		{
			if (type == 4)
			{
				sendWeaponUpdate(data);
				return;
			}
			lock (dataLocker)
			{
				long p = queue.BaseStream.Position;
				stream.Position = stream.Length;
				stream.Write(data, 0, data.Length);
				stream.Position = p;
			}
		}

		/* Determines if this object is to send
		   * data to the server.
		   * Methods should check this method before initiating an object update command to the server.
		   * Only ONE object will be a sender, this will be the
		   * object the player is controlling.
		   * All other objects will receive data from the server.
		   * See client-server.txt in ../plans/ for details on client-server interaction.
		   * */
		public bool isSender()
		{
			return Options.isPlayingOnline && (role == OnlineRole.sender || role == OnlineRole.bot);
		}
		public bool isReceiver()
		{
			return Options.isPlayingOnline && role == OnlineRole.receiver;
		}

		public virtual void freeResources()
		{
			if (ctrlVolume != null)
			{
				SecondarySoundBuffer b = null;
				for (int i = 0; i < ctrlVolume.Count; i++)
				{
					b = ctrlVolume[i];
					DSound.unloadSound(ref b);
				}
				ctrlVolume.Clear();
				volumes.Clear();
				looping.Clear();
			}
			isMuted = false;
		}

		/// <summary>
		/// Applies add-ons. Add-ons are encapsulated in the AddOnArgs class.
		/// </summary>
		/// <param name="addOns">The add-on array.</param>
		public virtual void setAddOns(AddOnArgs[] addOns)
		{
			foreach (AddOnArgs s in addOns)
			{
				switch (s.getAddOn())
				{
					case CSCommon.AddOns.extraFuel:
						extraFuelTanks = s.getArg();
						break;

					case CSCommon.AddOns.extraCruiseMissiles:
						extraCruiseMissiles = s.getArg();
						break;

					case CSCommon.AddOns.flyingCruiseMissile:
						flyingCruiseMissile = true;
						break;
					case CSCommon.AddOns.missileInterceptor:
						interceptors = s.getArg();
						break;
				} //switch
			} //foreach addOn
		}

		public void setSpectator()
		{
			requestingSpectator = true;
		}
		public void removeSpectator()
		{
			requestingSpectator = false;
			requestingCancelSpectator = true;
		}

		protected float getTotalWeight()
		{
			return getWeight() + m_fuelWeight;
		}

		protected float getMaxTotalWeight()
		{
			return m_maxWeight + m_maxFuelWeight;
		}

		/// <summary>
		/// Gets the current weight of this object. Each child class should implements its own getWeight() method to make sure all weights are totaled.
		/// </summary>
		/// <returns>The weight of the object, not accounting fuel.</returns>
		protected virtual int getWeight()
		{
			//filler
			return 0;
		}

		public int getMaxWeight()
		{
			return m_maxWeight;
		}

		public float getFuelWeight()
		{
			return m_fuelWeight;
		}

		public float getMaxFuelWeight()
		{
			return m_maxFuelWeight;
		}

		/// <summary>
		///  Sets the weight attributes of this object.
		/// </summary>
		/// <param name="maxWeight">The maximum frame weight capacity. This does not account for fuel weight.</param>
		public void setWeight(int maxWeight)
		{
			m_maxWeight += maxWeight;
		}

		/// <summary>
		/// Sets the frame and fuel weight attributes of this projector.
		/// </summary>
		/// <param name="fuelWeight">The current fuel weight.</param>
		/// <param name="maxFuelWeight">The maximum fuel capacity.</param>
		public void setWeight(int maxWeight, float fuelWeight, float maxFuelWeight)
		{
			setWeight(maxWeight);
			m_fuelWeight += fuelWeight;
			m_maxFuelWeight += maxFuelWeight;
		}

		/// <summary>
		/// This method marks this projector as "dirty," meaning that it should be
		/// removed from the object table. This will happen if a player
		/// unexpectedly disconnects from the server.
		/// </summary>
		public void declareAsDirty()
		{
			dirty = true;
		}

		protected double getMaxSpeedPercentage()
		{
			return 0.0;
		}

		/// <summary>
		/// Gest the rate of turn for an object, using the bank angle and horizontal velocity.
		/// </summary>
		/// <returns>The rate of turn in degrees per second.</returns>
		protected virtual float getRateOfTurn()
		{
			return (float)(Math.Tan(Degrees.toRadians(bankAngle)) * 1091 / Common.convertToKNOTS(getHorizontalSpeed()));
		}

		/// <summary>
		/// Gets the radius of a turn.
		/// </summary>
		/// <returns>The radius in feet.</returns>
		protected float getTurnRadius()
		{
			return (float)(Math.Pow(Common.convertToKNOTS(getHorizontalSpeed()), 2) / (11.26 * Math.Tan(Degrees.toRadians(bankAngle))));
		}

		/// <summary>
		/// Changes the heading of the object in a thread-safe manner.
		/// </summary>
		/// <param name="d">The new heading. This value should already represent a correct degree marking.</param>
		protected void changeDirectionTo(int d)
		{
			curDir = d;
			Interlocked.Exchange(ref m_direction, d);
		}

		/*
		protected void addData(Aircraft.Action command, params String[] args)
		{
			Client.addData(command, id, args);
		}
		*/
		/// <summary>
		/// Commits a weapon update received by queueData
		/// </summary>
		/// <param name="data">The byte array containing the data to send.</param>
		public virtual void sendWeaponUpdate(byte[] data)
		{
			//filler
		}

		public bool isBot()
		{
			return role == OnlineRole.bot;
		}

		protected bool isNextRoundtime(ref int currentTime, int maxTime)
		{
			currentTime += Common.intervalMS;
			if (currentTime > maxTime)
			{
				currentTime = 0;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Hides the object from view.
		/// </summary>
		public void cloak()
		{
			showInList = false;
			if (isSender())
				Client.addData(Aircraft.Action.cloak, id);
		}

		/// <summary>
		/// Shows an object.
		/// </summary>
		public void deCloak()
		{
			showInList = true;
			if (isSender())
				Client.addData(Aircraft.Action.deCloak, id);
		}

		public bool getCloakStatus()
		{
			return showInList;
		}

		protected void setCloakStatus(bool status)
		{
			showInList = status;
		}

		/// <summary>
		/// Sends destroyed indication to server
		/// </summary>
		/// <param name="name">The name of the killer</param>
		/// <param name="tag">The tag of the killer</param>
		public void indicateDestroyedBy(String name, String tag)
		{
			if (doneDestroyedBy)
				return;
			doneDestroyedBy = true;
			if (isSender())
			{
				if (!id.StartsWith("B-", StringComparison.OrdinalIgnoreCase))
					SapiSpeech.speak("You were destroyed by " + name, SapiSpeech.SpeakFlag.interruptable);
				Client.sendCommand(CSCommon.cmd_serverMessage, this.name + " was poned by " + name);
				if (!isBot() && tag != null)
					Client.sendCommand(CSCommon.cmd_updatePoints, tag);
			}
		}

	}
}