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
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
namespace TDV
{
	#region delegates
	//strike is fired if a weapon contacts target, but doesn't destroy
	//destroy is fired if target.damage <= 0
	public delegate void readyToDisposeHandler(WeaponBase sender);
	public delegate void hitHandler(WeaponBase sender, Projector target, int damageAmount, WeaponTypes type);
	public delegate void strikeEventHandler(bool isLCS);
	public delegate void destroyEventHandler(Projector target);
	#endregion
	#region interfaces
	public enum WeaponTypes : byte
	{
		guns,
		missile,
		laserCannonSystem,
		cruiseMissile,
		landingBeaconLock,
		samMissile,
		explosiveMissile,
		missileInterceptor,
		tankMissile,
		battleShipGuns
	}
	public interface Explosive
	{
		String id
		{
			get;
		}
		Projector origTarget
		{
			get;
		}
		void lockOn(Projector target);
		void save(BinaryWriter w);
		bool load();
		void use();
		void serverSideHit(Projector target, int remainingDamage);
		void onTick();
		void free();
		void initRange(Range r); //This method is implemented through WeaponsBase and inherited by all explosives.
																											//this event will be raised by a weapon that has done its work and is ready to be deleted from memory
		event readyToDisposeHandler readyToDispose;
		//this event is implemented by all weapons to notify the launcher craft that a target has been struck
		event hitHandler eventHit;
	}
	#endregion

	public class Weapons
	{
		public event strikeEventHandler strike;
		public event destroyEventHandler destroy;

		/// <summary>
		/// Set this property to true if on the next tick, weapons should stop themselves.
		/// </summary>
		public static bool requestedClear
		{
			get;
			set;
		}

		private List<WeaponBase> validWeaponsArray;
		private long chunkPos;
		private long numArgsPos = 0;
		private const int infiniteAmmunition = 2000000000;
		private long next;
		private short weaponEvents; //number of weapon events in this tick
		private String m_nextID;
		private MemoryStream serverData;
		private BinaryWriter writer;
		private MemoryStream incomingData; //holds info about an update sent by server
		private bool isWaitingForLoad;
		private bool executingServerData;
		private List<WeaponTypes> validIndecies;
		private byte m_LTSCount;
		private Projector m_creator;
		private WeaponTypes m_weaponIndex;
		private int currentIndex;
		private string m_lockIndex;
		private int m_lastMaxIndex;
		private int[] m_ammunition;
		private Projector m_LCSTarget;
		private static ExtendedAudioBuffer lockSound, lockAlertSound;
		private ExtendedAudioBuffer gunSound;
		private Projector m_target;
		private bool explosiveDeleted; //is true if a weapon was deleted
																																	//on its call to Explosive.onTick()
		private Range m_radarRange;
		private string[] locks;
		private int nextPos;
		private bool m_stoppedStrafe;

		/// <summary>
		/// Signals to the creator to send a stop strafe command to the server on the next go-around.
		/// We don't send the command here ourselves since stop strafe will be run on a separate thread for player input, and will crash the stream
		/// when it writes to a closed stream, or it will
		/// yield unpredictable results since two processes will be writing to
		/// the same stream.
		/// </summary>
		public bool stoppedStrafe
		{
			get { return m_stoppedStrafe; }
			set { m_stoppedStrafe = value; }
		}

		private List<WeaponBase> validWeapons
		{
			get { return (validWeaponsArray); }
		}

		private WeaponBase weaponAt(int index)
		{
			return validWeaponsArray[index];
		}

		private int maxIndex
		{
			get { return (validWeapons.Count - 1); }
		}

		/// <summary>
		/// Checks whether this Weapons class is still operating projectiles.
		/// </summary>
		/// <returns>True if there are still live projectiles, false otherwise.</returns>
		public bool isUsing()
		{
			lock (validWeapons)
				return (validWeapons.Count > 0);
		}

		/// <summary>
		/// This property should be set with the ID to assign to the next created projectile.
		/// If no ID is specified, the class will use nextPosition + next(String) as the ID.
		/// </summary>
		public String nextID
		{
			get { return m_nextID; }
			set { m_nextID = value; }
		}

		public static int maxHRange
		{
			get { return (2000000); }
		}

		public static int maxVRange
		{
			get { return (2000000); }
		}


		/// <summary>
		/// Returns the weapon index in the specified array slot
		/// </summary>
		/// <param name="i">Array index</param>
		/// <returns>The weapon index</returns>
		public WeaponTypes getWeaponAt(int i)
		{
			return validIndecies[currentIndex = i];
		}

		////determines if an LTS is currently projecting toward its target.
		////used to limit number of LTS that are active.
		private byte LTSCount
		{
			get { return (m_LTSCount); }
			set { m_LTSCount = value; }
		}

		//returns true if the cruise missile is active (viz. switched to) and a tag exists,
		//false otherwise
		public bool cruiseMissileLocked()
		{
			return (lCSTarget != null && isValidLock(lCSTarget));
			//lcsTarget could have a lock but the target could have been  destroyed,
			//in which case the cruise missile would fire at a dead object.
			//This method only determines if a cruise can be fired.
		}

		////An object should call this method if it is wanting to cycle through a list of weapons.
		////This method will update the weaponIndex and match it with the validIndex method.
		////Therefore, an update by this method will never fail.
		//returns new weaponIndex
		public WeaponTypes increaseWeaponIndex()
		{
			int wMIndex = getMaxWeaponIndex() + 1;
			weaponIndex = validIndecies[currentIndex = (currentIndex + 1) % wMIndex];
			return weaponIndex;
		}

		private int getMaxWeaponIndex()
		{
			return validIndecies.Count - 1;
		}

		public Projector lCSTarget
		{
			get
			{
				if (m_LCSTarget == null) {
					return (null);
				} else if (m_LCSTarget.isTerminated) {
					//short circuit evaluation does not work :(
					return (null);
				}
				return (m_LCSTarget);
			}
			set { m_LCSTarget = value; }
		}


		public void setAmmunitionFor(WeaponTypes w, int value)
		{
			m_ammunition[(int)w] = value;
		}

		public void setInfiniteAmmunition()
		{
			for (int i = 0; i < getMaxWeaponIndex(); i++)
				setAmmunitionFor((WeaponTypes)i, infiniteAmmunition);
		}
		public void decreaseAmmunitionFor(WeaponTypes w)
		{
			if (Options.isLoading)
				return; //Since load already registers proper ammo count don't allow decrease.
			if (m_ammunition[(int)w] == infiniteAmmunition)
				return;
			m_ammunition[(int)w]--;
		}

		public int ammunitionFor(WeaponTypes w)
		{
			return m_ammunition[(int)w];
		}

		//Returns a Range structure populated with data to define the locking range.
		//This is the range at which a radar can detect and lock onto an object.
		//Note: this value does not necessarily represent
		//the distance from which a weapon can be fired.
		//See firingRange for this value.
		public Range lockingRange
		{
			get { return (m_radarRange); }
		}
		public Range radarRange
		{
			get
			{
				if (creator is Aircraft)
					return (new Range(creator.z / 10000.0 * 20.0, maxVRange));
				else
					return (m_radarRange);
			}
		}

		//Returns a Range structure populated with the range
		//from which a weapon can be fired.
		//Note: this value does not necessarily represent the range
		//from which an object can lock onto another object.
		//See lockingRange for this value.
		public Range firingRange
		{
			get
			{
				Range r = new Range(0.0, 0.0);
				switch (weaponIndex) {
					case WeaponTypes.missile:
					case WeaponTypes.explosiveMissile:
						r = new Range(15.0, (creator is GuardTower) ? 50000.0 : 300.0);
						break;
					case WeaponTypes.guns:
						r = new Range(4.0, 300.0);
						break;
					case WeaponTypes.laserCannonSystem:
						r = new Range(10.0, 300.0);
						break;
					case WeaponTypes.cruiseMissile:
					case WeaponTypes.missileInterceptor:
						r = new Range(30.0, 1000.0);
						break;
					case WeaponTypes.tankMissile:
						r = new Range(30.0, 50000.0);
						break;
					case WeaponTypes.battleShipGuns:
						r = new Range(5.0, 50000.0);
						break;
					case WeaponTypes.samMissile:
						r = new Range(5.0, 50000.0);
						break;
					case WeaponTypes.landingBeaconLock:
						return new Range(maxHRange, maxVRange);
				} //switch

				//Next, if firing at object
				//increase v range so we can drop missiles
				if (Options.mode == Options.Modes.mission
								&& isValidLock()
								&& getLockedTarget().isObject)
					r.verticalDistance = 50000.0;
				return r;
			}
		}

		public WeaponTypes weaponIndex
		{
			get { return (m_weaponIndex); }
			set { m_weaponIndex = value; }
		}
		public int lastMaxIndex
		{
			get { return (m_lastMaxIndex); }
			set { m_lastMaxIndex = value; }
		}
		public string lockIndex
		{
			get { return (m_lockIndex); }
			set
			{
				if (value == null) { //broke lock
					clearLock();
					return;
				}
				m_lockIndex = value;
				if (m_lockIndex.Equals("-")) {
					System.Diagnostics.Trace.WriteLineIf(creator is JuliusAircraft, String.Format("Break lock called from: {0}, range is {1}, {2}", (new System.Diagnostics.StackTrace()).ToString(), lockingRange.horizontalDistance, lockingRange.verticalDistance));

					return;
				}
				m_target = Interaction.objectAt(m_lockIndex);
				if (m_target == null) {
					m_lockIndex = "-";
					return;
				}
				if (Options.mode != Options.Modes.mission && !m_target.isAI) {
					if (lockSound == null) {
						lockSound = DSound.LoadSound(DSound.SoundPath + "\\danger.wav");
						lockAlertSound = DSound.LoadSound(DSound.SoundPath + "\\he111.wav");
					}
					if (!DSound.isPlaying(lockSound))
						DSound.PlaySound(lockSound, true, false);
					if (Options.playRIO && !DSound.isPlaying(lockAlertSound))
						DSound.PlaySound(lockAlertSound, true, false);
				}
			} //set
		} //property

		public void freeResources()
		{
			setStrafe(false);
			DSound.unloadSound(ref lockSound);
			DSound.unloadSound(ref lockAlertSound);
		}

		public void clearLock()
		{
			lockIndex = "-";
			m_target = null;
		}

		public Projector creator
		{
			get { return (m_creator); }
		}

		//The method below is the event handler for the explosive.readyToDispose event
		private void freeWeapon(WeaponBase sender)
		{
			if (sender is LaserCannonSystem)
				LTSCount--;
			sender.free();
			sender.readyToDispose -= freeWeapon;
			validWeapons.Remove(sender);
			Interaction.removeFromObjectTable(sender.id);
			explosiveDeleted = true;
		}
		public Weapons(Projector creator)
		{
			validWeaponsArray = new List<WeaponBase>();
			m_creator = creator;
			if (Options.isPlayingOnline)
				next = Client.next;
			m_lockIndex = "-";
			System.Diagnostics.Trace.WriteLineIf(creator is JuliusAircraft, "Mode during weapons create is " + Options.mode);
			if (creator is Aircraft
							&& (!creator.isAI || Options.mode != Options.Modes.mission))
				m_radarRange = new Range(creator.z / 10000.0 * 20.0, maxVRange);
			else
				m_radarRange = new Range(10.0, maxVRange);
			weaponIndex = WeaponTypes.guns;
			validIndecies = new List<WeaponTypes>();
			m_ammunition = new int[Enum.GetValues(typeof(WeaponTypes)).Length];
			arm();
			//finally, add this weapon to the weapons holder
			//so it can be threaded.
			Interaction.holderAt(1).add(this);
			if (Options.isPlayingOnline) {
				serverData = new MemoryStream();
				incomingData = new MemoryStream();
			}
			nextID = null;
		}

		//the overloaded constructor below expects an array (or parameter list) containing all the valid weapons the projector defined by
		//creator can toggle to.
		public Weapons(Projector creator, params WeaponTypes[] vw) : this(creator)
		{
			foreach (WeaponTypes t in vw)
				validIndecies.Add(t);
		}

		public void arm()
		{
			setAmmunitionFor(WeaponTypes.guns, 7000);
			setAmmunitionFor(WeaponTypes.missile, 8);
			setAmmunitionFor(WeaponTypes.laserCannonSystem, 20);
			setAmmunitionFor(WeaponTypes.cruiseMissile, 4 + creator.extraCruiseMissiles);
			setAmmunitionFor(WeaponTypes.missileInterceptor, 2 + creator.interceptors);
			setAmmunitionFor(WeaponTypes.samMissile, 30);
			setAmmunitionFor(WeaponTypes.explosiveMissile, 8);
			setAmmunitionFor(WeaponTypes.tankMissile, 10);
			setAmmunitionFor(WeaponTypes.battleShipGuns, 50);
		}

		/// <summary>
		/// Called by the aircraft to update this weapons class.
		/// </summary>
		/// <param name="data">The byte array representing the update, excluding all prefixes.</param>
		public void sendUpdate(byte[] data)
		{
			lock (incomingData) {
				long p = incomingData.Position;
				incomingData.Position = incomingData.Length;
				incomingData.Write(data, 0, data.Length);
				incomingData.Position = p;
			}
		}

		///<summary>
		///Call this method before going through the normal use() procedure.
		///In case we have commands sent by the server.
		///Caller should check to see if incomingData is null before calling.
		///If it isn't, we have some server updates.
		///</summary>
		private void executeServerUpdate()
		{
			lock (incomingData) {
				executingServerData = true;
				BinaryReader reader = new BinaryReader(incomingData);
				String projectileId = null;
				Projector target = null;
				WeaponTypes type;
				bool found = false;

				int numArgs = 0;
				while (incomingData.Length > incomingData.Position) {
					//info will hold each weapon's information: the targetId, the ID for the projectile to which this hit belongs, type of weapon
					System.Diagnostics.Trace.WriteLine("in serverupdate, length is " + incomingData.Length + " and position is " + incomingData.Position);

					numArgs = reader.ReadInt16();
					for (int i = 1; i <= numArgs; i++) {
						//crawl each projectile info here
						projectileId = creator.id + reader.ReadString();
						target = Interaction.objectAt(reader.ReadString());
						type = (WeaponTypes)reader.ReadByte();
						if (target == null)
							continue;

						found = false;
						foreach (Explosive w in validWeapons) {
							if (w.id.Equals(projectileId)) { //this is our target
								found = true;
								w.serverSideHit(target, 0);
								Application.DoEvents();
								break; //we found the target id, so no need to continue
							}
						} //for all projectiles

						//Next, say the projectile hit finish before it saw the HIT command sent by the server.
						//In this case, we want to still register the hit.
						if (!found) {
							eventHit(null, target, 0, type);
							Application.DoEvents();
						} //if weapon not found.
					} //for each projectile in this command
				} //for each commands
				incomingData.SetLength(0);
				executingServerData = false;
			} //lock
		}

		/// <summary>
		///Returns true if the target is still a valid lock, false otherwise
		///Note: this method does not account for whether the object to which this class belongs has locked yet. Rather, it matches the number of objects with the number of objects it registered last time a lock was invoked.
		///Therefore, if no lock has been invoked previously, this method returns false because the last number of objects registered was zero.
		///</summary>
		///<returns>True if in range, false otherwise.</returns>
		public bool isTargetInRange()
		{
			if (!isValidLock())
				return false; //Prevents invalid cast error
			return (inRange(creator, Interaction.objectAt(lockIndex), lockingRange));
		}
		public bool inFiringRange()
		{
			if (!isValidLock())
				return (false);
			return (inRange(creator, Interaction.objectAt(lockIndex), firingRange));
		}

		/// <summary>
		/// returns true if the class has obtained a track.
		///note: does not discriminate between isValidLock. IE: This method will return true even if
		///the locked target no longer exists and has been deleted from the object table.
		///for those specifics, use isValidLock instead.
		/// </summary>
		/// <returns>True if a target is registered, false otherwise</returns>
		private bool isTargetRegistered()
		{
			return !lockIndex.Equals("-");
		}

		/// <summary>
		/// Returns true if this class has obtained a lock,
		///and the locked target has not been destroyed.
		///Unlike isTargetRegistered(), this method accounts for whether or not an object has been deleted from the object table.
		/// </summary>
		/// <returns>True if there is a valid target, false otherwise</returns>
		public bool isValidLock()
		{
			System.Diagnostics.Trace.WriteLineIf(creator is JuliusAircraft, String.Format("targetRegistered: {0}, objectAt[{1}] = {2}", isTargetRegistered(), lockIndex, Interaction.objectAt(lockIndex)));
			if (!isTargetRegistered() || Interaction.objectAt(lockIndex) == null)
				return false;
			return isValidLock(getLockedTarget());
		}

		public static bool isValidLock(Projector target)
		{
			//isTerminated gets precedence over isRequestedTerminated
			//because a target could signal terminate if a match is over,
			//in which case requestedTerminated is not set.
			if (target.isTerminated || target.isRequestedTerminated)
				return false; //this target can't be locked
			return true;
		}

		public static bool inRange(Projector weapon, Projector target, Range r)
		{
			return ((Degrees.getDistanceBetween(weapon.x, weapon.y,
																target.x, target.y) <= r.horizontalDistance)
																&& (Math.Abs(weapon.z - target.z) <= r.verticalDistance));
		}

		public void use()
		{
			lock (validWeapons) {
				if (isWaitingForLoad && !Weapons.requestedClear) {
					lockIndex = m_lockIndex;
					for (int i = 0; i <= maxIndex; i++) {
						//if locks i is -1, this weapon
						//is targetless.
						if (!locks[i].Equals("-"))
							validWeapons[i].lockOn(
											Interaction.objectAt(locks[i]));
					}
					isWaitingForLoad = false;
				}
				if (validWeapons.Count >= 1) {
					int i = 0;
					if (incomingData != null && incomingData.Length > 0L) //We have server updates.
						executeServerUpdate();
					long length = 0;
					if (serverData != null && serverData.Length == 0) {
						if (writer == null)
							writer = new BinaryWriter(serverData);
						writer.Write((sbyte)4);
						writer.Flush();
						chunkPos = serverData.Position;
						writer.Write((int)0);
						writer.Write(creator.id);
						writer.Flush();
						numArgsPos = serverData.Position;
						writer.Write((short)0);
						writer.Flush();
					}
					if (serverData != null)
						length = serverData.Length; //if the serverData grows beyond this,
																																		//it means we added data.

					while (i <= maxIndex) {
						validWeapons[i].onTick();
						if (!explosiveDeleted)
							i++;
						explosiveDeleted = false; //reset the flag since we've already taken action on it
						Application.DoEvents();
					}
					if (serverData != null && serverData.Length > length) {
						writer.Flush();
						serverData.Position = chunkPos;
						writer.Write((int)serverData.Length);
						writer.Flush();
						serverData.Position = numArgsPos;
						writer.Write((short)weaponEvents);
						writer.Flush();
						weaponEvents = 0;
						Client.sendData(serverData);
						serverData.SetLength(0);
					} //if we added data to the commandstring to send
				} //if projectiles to iterate
			} //SyncLock
		}

		public string use(WeaponTypes type, Projector target, Range firingRange)
		{
			if (isWaitingForLoad)
				return null;
			lock (validWeapons) {
				if (target != null && target.isLanded()
								&& target.Equals(Mission.player))
					return null;
				//don't let fire at player if player on ground
				if (type == WeaponTypes.landingBeaconLock)
					return null;
				WeaponBase e = null;
				int c = 0;
				String p = null;
				bool first = true;
				do {
					if (c > 0) //Only guns will increase c
						setStrafe(true); //Object may fire a few more rounds before it is suspended so don't restart strafe sound.
					e = createNewWeapon(type, target, firingRange);
					if (e == null) {
						setStrafe(false); //turn off strafing in case player ran out of ammo.
						return null;
					}
					if (first)
						p = initializeWeapon(e);
					if (weaponIndex != WeaponTypes.guns)
						return p;
					first = false;
				} while (++c <= 10);
				return p;
			} //lock
		}

		private String initializeWeapon(WeaponBase e)
		{
			e.use();
			return (creator.isSender()) ? e.id.Replace(creator.id, null) : null;
		}

		/// <summary>
		/// Creates a new projectile.
		/// </summary>
		/// <param name="type">The type (Guns, Missile, LTS, etc) of the weapon</param>
		/// <param name="target">The projector at which this projectile is being fired</param>
		/// <param name="reachRange">The range of how wide this projectile will consider itself a hit</param>
		/// <returns>The projectile as type Explosive. It will also have been given an ID.</returns>
		private WeaponBase createNewWeapon(WeaponTypes type, Projector target,
Range reachRange)
		{
			//if isLoading is true, ammo count could be 0 but we still have to load weapon
			//since ammo is decreased when weapon is created.
			WeaponBase w = null;
			if (ammunitionFor(type) != 0 || Options.isLoading) {
				switch (type) {
					case WeaponTypes.guns:
						validWeapons.Add(w = new Guns(this));
						break;
					case WeaponTypes.missile:
						validWeapons.Add(w = new Missile(this));
						break;
					case WeaponTypes.laserCannonSystem:
						if ((lCSTarget == null && LTSCount < 1) || Options.isLoading) {
							//ltsCount will not increment if loaded in from file
							LTSCount++; //so increment it if we're loading it from a file as well
							validWeapons.Add(w = new LaserCannonSystem(this));
						}
						break;
					case WeaponTypes.cruiseMissile:
						validWeapons.Add(w = new CruiseMissile(this));
						break;
					case WeaponTypes.missileInterceptor:
						Projector iLock = getInterceptorLock();
						if (iLock == null)
							break;
						target = iLock;
						validWeapons.Add(w = new MissileInterceptor(this));
						break;
					case WeaponTypes.samMissile:
						validWeapons.Add(w = new SAMMissile(this));
						break;
					case WeaponTypes.explosiveMissile:
						validWeapons.Add(w = new ExplosiveMissile(this));
						break;
					case WeaponTypes.tankMissile:
						validWeapons.Add(w = new TankMissile(this));
						break;
					case WeaponTypes.battleShipGuns:
						validWeapons.Add(w = new BattleShipGuns(this));
						break;
				}
			} //if ammo remaining
			if (w != null) {
				// Only the client that fired the actual projectile will send info about its fired projectiles.
				// Other clients will just duplicate this projectile, and wait for instructions from this client.
				String id = creator.id + "_" + next
								+ "_" + (nextPos++);
				if (nextID != null) { //received weapon duplicate command from server
					w.setID(nextID); // command includes assigned ID.
																						/*When firing with guns, only one creation ID will be sent to the server,
																							* signaling the ID of the first shot. We parse this ID to get the IDs of the remaining shots since nextID will be null after this.
																							* See use() to see how gunshots are fired together.
																							* */
					if (weaponIndex == WeaponTypes.guns) {
						String[] idParts = nextID.Split(new char[] { '_' });
						next = Convert.ToInt32(idParts[1]);
						nextPos = Convert.ToInt32(idParts[2]) + 1;
					} //if guns
					nextID = null;
				} else { //If nextID == null IE: not server weapon
					if (!Options.isLoading) //weapon's load method will add to object table
						w.setID(id, !(w is Guns)); //don't add bullets to object table
				}

				w.readyToDispose += freeWeapon; //Notify this class when the weapon has completed its execution
				w.eventHit += eventHit;
				if (target != null)
					w.lockOn(target);
				w.initRange(reachRange);
				System.Diagnostics.Trace.WriteLineIf(w != null, "WCreated weapon " + w.id);
				if (w.id != null)
					System.Diagnostics.Trace.WriteLine("WCreated id " + w.id);
				return w;
			}
			return null;
		}

		/// <summary>
		/// Use this method of creating weapons when loading a game. It will create the new weapon
		/// without locking, since the locked object may not have been created yet.
		/// </summary>
		/// <param name="type">The type of the weapon</param>
		/// <param name="id">The lock ID. This is the ID the weapon will lock on to.</param>
		/// <param name="index">The array index to insert the lock tag into. This is a parallel array and is used to associated lock IDs with projectiles.</param>
		/// <param name="reachRange">The reaching range</param>
		/// <returns>The new projectile as type Explosive</returns>
		private WeaponBase createNewWeapon(WeaponTypes type, string id, int index, Range reachRange)
		{
			locks[index] = id;
			return createNewWeapon(type, null, reachRange);
		}

		/* Fires when a weapon hits a target.
   * If sender == NULL, this mean we pseudofired this event from
   * executeServerCommand().
   * IE: what happened is that we had a hit on the server side but by the time it reached here to propogate,
   * this local Weapons class had disposed of the local copy of the projectile.
   * However, we still need to propogate the hit.
   * */
		public void eventHit(Explosive sender,
												Projector target,
												int damageAmount, //0 if from server or LTS
			WeaponTypes type) //Used if weapon has been deleted by the time hit event registers on server
		{
			if (sender != null)
				sender.eventHit -= eventHit;

			bool killed = target.damage - damageAmount <= 0;
			if (killed)
				target.indicateDestroyedBy(creator.name, creator.id);
			if (!executingServerData && target.isSender()) {
				writer.Write(sender.id.Replace(creator.id, null));
				writer.Write(target.id);
				writer.Write((byte)type);
				writer.Flush();
				weaponEvents++;
			}
			if (!executingServerData && !(sender is LaserCannonSystem)) //we already get new damage value from server.
				target.hit(damageAmount, Interaction.Cause.destroyedByWeapon);
			if (killed) {
				if (destroy != null)
					destroy(target);
				return;
			}
			if (strike != null)
				strike(type == WeaponTypes.laserCannonSystem);
			if (sender == null)
				doNullSender(type, target);
		}
		private void doNullSender(WeaponTypes type, Projector target)
		{
			if (type == WeaponTypes.laserCannonSystem)
				lCSTarget = target;
		}
		public Projector getLockedTarget()
		{
			return (Interaction.objectAt(lockIndex));
		}

		public void save(BinaryWriter w)
		{
			w.Write((int)weaponIndex);
			w.Write(lockIndex);
			foreach (WeaponTypes i in Enum.GetValues(typeof(WeaponTypes)))
				w.Write(ammunitionFor(i));
			w.Write(maxIndex);
			foreach (WeaponBase o in validWeapons)
				o.save(w);
			w.Write(nextPos);
		}

		public void load()
		{
			BinaryReader r = Common.inFile;
			weaponIndex = (WeaponTypes)r.ReadInt32();
			m_lockIndex = r.ReadString();
			//first handful of numbers are ammo counts.
			foreach (WeaponTypes i in Enum.GetValues(typeof(WeaponTypes)))
				setAmmunitionFor(i, r.ReadInt32());

			//next, data after this are weapon data.
			//totalWeapons determines how many weapons there are.
			//If 0 weapons were saved, this value will be -1.
			int index = 0, maxIndex = r.ReadInt32(), type = 0;
			String lockId = null;
			if (maxIndex != -1)
				locks = new String[maxIndex + 1];
			String n = null;
			while (index <= maxIndex) {
				n = r.ReadString(); //get weapon name
				n = n.Substring(1); //first char is "p", but we just need weapon id
				type = Convert.ToInt32(n); //get the weapon id so we can create the weapon below
																															//Next, string is lock id of weapon to be loaded.
				lockId = r.ReadString();
				//The next two numbers are doubles and
				//will define the reach Range for the weapon.
				//The objects this weapon tracks will only be initialized in the Explosive.onTick method for the Explosive however,
				//so this call just sets up the weapon to execute onTick properly.
				//NOTE: onTick will *NOT* execute until all weapons are done loading.
				createNewWeapon((WeaponTypes)type, lockId, index++,
					new Range(r.ReadDouble(), r.ReadDouble())).load();
			} //while more weapons to load
			if (maxIndex > -1) //loaded weapons
				isWaitingForLoad = true;
			nextPos = r.ReadInt32();
		}

		/// <summary>
		/// Turns strafing on or off. If strafing is on, the gun sound is looped and
		/// use() will fire off ten shots. Else it will stop playing the strafing sound. So it will call this method to start the gun sound and let fly ten.
		/// </summary>
		/// <param name="status">True to turn strafing on or leave it on, false to turn it off or leave it off.</param>
		public void setStrafe(bool status)
		{
			if (status) {
				if (gunSound == null)
					gunSound = creator.loadSound(creator.soundPath + "gun1.wav");
				creator.playSound(gunSound, false, true);
				if (isValidLock() && getLockedTarget() is Aircraft)
					((Aircraft)getLockedTarget()).notifyOf(Notifications.strafe, inFiringRange());
			} else {
				if (creator.isSender())
					stoppedStrafe = false;
				DSound.unloadSound(ref gunSound);
			}
		}


		public void addValidIndex(WeaponTypes w)
		{
			validIndecies.Add(w);
		}

		/// <summary>
		/// Gest a cruise missile that an interceptor can lock onto.
		/// </summary>
		/// <returns>The cruise missile, or null if there are none.</returns>
		public Projector getInterceptorLock()
		{
			List<Projector> allProjectiles = Interaction.getProjectiles(creator);
			if (allProjectiles == null)
				return null;
			foreach (Projector p in allProjectiles) {
				if (p is WeaponBase && p is CruiseMissile)
					return p;
			}
			return null;
		}

		public int getWeight()
		{
			return ammunitionFor(WeaponTypes.guns) / 1000 * 100
				+ ammunitionFor(WeaponTypes.missile) * 50
				+ ammunitionFor(WeaponTypes.missileInterceptor) * 50
				+ ammunitionFor(WeaponTypes.cruiseMissile) * 500;
		}

	}
}
