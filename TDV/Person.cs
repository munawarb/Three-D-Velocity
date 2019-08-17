/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.Input;
using BPCSharedComponent.VectorCalculation;
using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TDV
{
	public class Person : FightObject
	{
		public enum MovementDirection : byte
		{
			none,
			north,
			east,
			south,
			west
		}
		#region sounds
		private ExtendedAudioBuffer hitSound, impactSound, throwSound;
		private ExtendedAudioBuffer blockSound;
		private ExtendedAudioBuffer[] swingSound, swingVoice, punchedSound, stepSound, gruntSound, tossSound, tossVoice;
		private ExtendedAudioBuffer lastStepSound;
		private ExtendedAudioBuffer crashSound;
		private ExtendedAudioBuffer stunSound;
		private ExtendedAudioBuffer lockSound, shootSound;
		#endregion

		private PersonMissile[] missiles;
		private DateTime lastPunchTime, startSeekTime, startSwingTime, lastGrabTime, lastStunTime, lastMissileTime, lastShootTime;
		private int stunTime, missileTime, shootTime, maxShootTime;
		private int shootX, shootY;
		private bool beingGrabbed, swinging, waitingToShoot;
		private Person grabTarget, grabber; //Who we've got a hold of, and who we're being held by
		private bool isBlocking, isInPanicMode;
		private bool seeking;
		private int seekX, seekY;
		private FightObject target;
		private FightObject punchTarget;
		private int stopPunchTime, maxPunchDamage, stopGrabTime;
		private List<MapNode> path;
		private int pathIndex = 0;
		private Person shootTarget;


		/// <summary>
		/// Creates a new person
		/// </summary>
		/// <param name="x">Starting x coordinate</param>
		/// <param name="y">Starting y coordinate</param>
		/// <param name="damage">Starting hit points; max damage will also be set to this value</param>
		/// <param name="isAI">True if computer controlled, false otherwise</param>
		public Person(int x, int y, int damage, bool isAI)
			: base(x, y, damage, isAI)
		{
			this.isAI = isAI;
			if (juliusLevel() == 2)
				missiles = new PersonMissile[1];
			else if (juliusLevel() == 3)
				missiles = new PersonMissile[4];
			if (juliusLevel() == 3) {
				shootSound = loadSound("l3s.wav");
				lockSound = loadSound("l3a.wav");
			}
			lastStunTime = lastPunchTime = lastMissileTime = DateTime.Now;
			stopPunchTime = Common.getRandom(1, 10);
			startSeekTime = DateTime.Now;
			blockSound = loadSound("fb.wav");
			stunSound = loadSound("fstun.wav");
			if (!isAI)
				DSound.SetCoordinates(x, 0.0, y);
		}

		/// <summary>
		/// Loads swing sounds and exhale sounds.
		/// </summary>
		/// <param name="filename">The template of swing sounds, played when the character makes a punch move.</param>
		/// <param name="n">The number of files to load.</param>
		/// <param name="filename2">The template of exhale sounds.</param>
		/// <param name="n2">The number of files to load.</param>
		public void initializeSwingSound(String filename, int n, String filename2, int n2)
		{
			swingSound = loadSoundArray(filename, n);
			swingVoice = loadSoundArray(filename2, n2);
		}

		public void initializeStepSound(String filename, int n)
		{
			stepSound = loadSoundArray(filename, n);
		}

		/// <summary>
		/// Loads throw voices. Throw sounds will be loaded as well.
		/// </summary>
		/// <param name="filename">The body impact sounds for throwing.</param>
		/// <param name="n">The number of files to load.</param>
		/// <param name="filename2">The template of files containing voices.</param>
		/// <param name="n2">The number of files to load.</param>
		public void initializeThrowSounds(String filename, int n, String filename2, int n2)
		{
			tossSound = loadSoundArray(filename, n);
			tossVoice = loadSoundArray(filename2, n2);
		}

		/// <summary>
		/// Sets up punch sounds and grunt sounds
		/// </summary>
		/// <param name="filename">The template of body punch sounds, played when this character is hit.</param>
		/// <param name="n">The number of files to load.</param>
		/// <param name="filename2">The template of grunt voices, played when this character is hit.</param>
		/// <param name="n2">The number of files to load.</param>
		public void initializePunchedSound(String filename, int n, String filename2, int n2)
		{
			punchedSound = loadSoundArray(filename, n);
			gruntSound = loadSoundArray(filename2, n2);
		}

		public void initializeGruntSound(String filename, int n)
		{
			gruntSound = loadSoundArray(filename, n);
		}

		/// <summary>
		/// Makes this person punch someone.
		/// </summary>
		/// <param name="f">The object to punch or null to select one.</param>
		/// <param name="max">The max damage.</param>
		public void punchSomeone(FightObject f, int max)
		{
			if (swinging || grabTarget != null)
				return;
			if (isAI && (DateTime.Now - lastPunchTime).TotalSeconds < stopPunchTime)
				return;
			lastPunchTime = DateTime.Now;
			stopPunchTime = Common.getRandom(1, 3);
			playSound(swingSound[Common.getRandom(0, 1)], true, false);
			playSound(swingVoice[Common.getRandom(0, swingVoice.Length - 1)], true, false);
			startSwingTime = DateTime.Now;
			maxPunchDamage = max;
			swinging = true;
			punchTarget = f;
		}

		/// <summary>
		/// Tosses this person.
		/// </summary>
		/// <param name="max">The max damage to subtract</param>
		/// <returns>True if dead, false otherwise</returns>
		public bool toss(int max)
		{
			List<FightObjectFeeler> l = getFurnitureInRange(3);
			Furniture furniture = null;
			bool dest = false;
			if (l != null) {
				furniture = l[0].furniture;
				dest = furniture.hit(500);
				updateCoordinates(l[0].x, l[0].y);
				playGrunt();
				furniture.playDestroy(isAI);
				if (dest)
					hit(200);
				else
					hit(100);
			}
			DateTime fallTime = DateTime.Now;
			while ((DateTime.Now - fallTime).TotalMilliseconds < 100) ;
			playSound(tossSound[Common.getRandom(0, tossSound.Length - 1)], true, false);
			playGrunt();
			setStunTime(4, (dest) ? 12 : 7);
			return hit(max);
		}

		private void tossTarget()
		{
			playSound(tossVoice[Common.getRandom(0, tossVoice.Length - 1)], true, false);
			DateTime tossTime = DateTime.Now;
			while ((DateTime.Now - tossTime).TotalMilliseconds < 500) ;
			grabTarget.toss(200);
			letGoOfTarget();
			setStunTime(2, 5);
		}

		/// <summary>
		/// Grabs this person.
		/// </summary>
		/// <param name="grabber">The person who's grabbing this person.</param>
		public void grab(Person grabber)
		{
			beingGrabbed = true;
			this.grabber = grabber;
		}

		/// <summary>
		/// Let's go of this person.
		/// </summary>
		public void letGo()
		{
			beingGrabbed = false;
			grabber = null;
		}

		public void block()
		{
			isBlocking = true;
		}

		public void stopBlocking()
		{
			isBlocking = false;
		}


		public override bool punch(int max)
		{
			if (isBlocking)
				max /= 2;
			playSound((isBlocking) ? blockSound : punchedSound[Common.getRandom(0, punchedSound.Length - 1)], true, false);
			if (!isBlocking)
				playGrunt();
			return base.punch(max);
		}

		public bool inactive()
		{
			return beingGrabbed;
		}

		/// <summary>
		/// Moves the person.
		/// </summary>
		/// <param name="movementDirection">The direction to move in.</param>
		/// <param name="force">True if the person should move regardless of whether the step sound is playing or not; false otherwise.</param>
		public void move(MovementDirection movementDirection, bool force)
		{
			if (lastStepSound != null) {
				if (!force && DSound.isPlaying(lastStepSound))
					return;
				lastStepSound.stop();
			}

			int lx = x;
			int ly = y;
			if (movementDirection == MovementDirection.north)
				ly++;
			else if (movementDirection == MovementDirection.east)
				lx++;
			else if (movementDirection == MovementDirection.south)
				ly--;
			else
				lx--;
			foreach (FightObject o in things) {
				if (o.isBlocked(lx, ly)) {
					o.playCrashSound(lx, ly);
					return;
				}
			}
			if (grabTarget != null)
				grabTarget.updateCoordinates(x, y);
			updateCoordinates(lx, ly);
			playSound(lastStepSound = stepSound[Common.getRandom(0, stepSound.Length - 1)], true, false);
		}

		public void move()
		{
			if (isAI) {
				throwMissile(Interaction.player);
				lockOnPerson(Interaction.player);
				if (shootTarget != null)
					shootPerson(shootTarget);
				moveMissiles();
			}
			if (beingGrabbed)
				return;
			if (isStunned()) {
				playSound(stunSound, false, true);
				return;
			} else if (DSound.isPlaying(stunSound))
				stunSound.stop();
			if (isAI) {
				if (!isInPanicMode && (double)(damage / maxDamage) * 100 <= 40)
					isInPanicMode = true;
				if (isInPanicMode) {
					if (Common.getRandom(1, 100) <= 2)
						seekRandomCoordinates();
				}
				if (isInRange(Interaction.player)) {
					if (grabTarget == null) {
						punchSomeone(Interaction.player, 50);
						if (!seeking && !Interaction.player.isStunned()) {
							if (grabSomeone(Interaction.player))
								seekRandomFurniture();
						}
					} else if (!seeking) { //If someone's already being held.
						if (Common.getRandom(1, 100) < 30)
							letGoOfTarget();
						else {
							tossTarget();
							if (Common.getRandom(1, 2) == 1)
								seekRandomCoordinates();
						}
					}
				} else //if not in range
					startSeeking(Interaction.player);
				seek();
			} else { //if not AI
				if (DXInput.isFirstPress(Key.Space))
					punchSomeone(null, 100);

				if (DXInput.isKeyHeldDown(Key.LeftShift) || DXInput.isKeyHeldDown(Key.RightShift))
					block();
				else
					stopBlocking();

				if (DXInput.isKeyHeldDown(Key.LeftControl) || DXInput.isKeyHeldDown(Key.RightControl))
					grabSomeone(null);
				else if (grabTarget != null)
					tossTarget();

				if (DXInput.isKeyHeldDown(Key.Up))
					move(Person.MovementDirection.north, DXInput.isFirstPress(Key.Up));
				else if (DXInput.isKeyHeldDown(Key.Right))
					move(Person.MovementDirection.east, DXInput.isFirstPress(Key.Right));
				else if (DXInput.isKeyHeldDown(Key.Down))
					move(Person.MovementDirection.south, DXInput.isFirstPress(Key.Down));
				else if (DXInput.isKeyHeldDown(Key.Left))
					move(Person.MovementDirection.west, DXInput.isFirstPress(Key.Left));
				if (DXInput.isFirstPress(Key.F6))
					Common.decreaseMusicVolume();
				if (DXInput.isFirstPress(Key.F7))
					Common.increaseMusicVolume();
			}
			if (swinging && (DateTime.Now - startSwingTime).TotalMilliseconds > 200)
				doPunch();
		}

		public override string ToString()
		{
			return x + ", " + y;
		}

		/// <summary>
		/// Starts seeking the FightObject.
		/// </summary>
		/// <param name="target">The target to seek.</param>
		public void startSeeking(FightObject target)
		{
			if (!seeking) {
				seeking = true;
				startSeekTime = DateTime.Now;
				path = MapNode.getCachedPathTo(new Vector2(x, y), (target is Furniture) ? ((Furniture)target).getClosestCorner(x, y) : new Vector2(target.x, target.y));
				pathIndex = 0;
				this.target = target;
				seekX = target.x;
				seekY = target.y;
			}
		}

		/// <summary>
		/// Starts seeking the specified coordinates. The AI will travel to the node nearest these coordinates but may not end up on these coordinates exactly.
		/// </summary>
		/// <param name="x">The x coordinate to seek.</param>
		/// <param name="y">The y coordinate to seek.</param>
		public void startSeeking(int x, int y)
		{
			if (!seeking) {
				seeking = true;
				target = null;
				seekX = x;
				seekY = y;
				path = MapNode.getCachedPathTo(new Vector2(this.x, this.y), new Vector2(x, y));
				startSeekTime = DateTime.Now;
				pathIndex = 0;
			}
		}

		private void seek()
		{
			if (!seeking)
				return;
			if ((DateTime.Now - startSeekTime).TotalMilliseconds < 600)
				return;
			if (pathIndex == path.Count) {
				if (target != null && !(target is Furniture))
					move(seek(target), false);
			} else {
				MovementDirection m = seek(path[pathIndex]);
				if (m != MovementDirection.none)
					move(m, false);
				else
					pathIndex++;
			}
			if ((target != null && !(target is Furniture) && isInRange(target)) || pathIndex == path.Count) {
				seeking = false;
				target = null;
			}
		}

		private void doPunch()
		{
			FightObject f = punchTarget;
			if (punchTarget == null) {
				List<FightObject> fs = getObjectInRange();
				if (fs == null) {
					swinging = false;
					return;
				}
				f = fs[0];
			}
			f.punch(maxPunchDamage);
			swinging = false;
			punchTarget = null;
			//Next, there are cases where there is quick punch and grab in succession.
			//So we block grabs for x amount of seconds after a punch.
			if (isAI) {
				lastGrabTime = DateTime.Now;
				stopGrabTime = 2;
			}
		}

		/// <summary>
		/// Grabs someone.
		/// </summary>
		/// <param name="f">The person to grab, or null for an automated selection.</param>
		///<returns>True if the grab was successful, false otherwise.</returns>
		private bool grabSomeone(Person f)
		{
			if (grabTarget != null)
				return false;
			if (missiles != null) {
				foreach (PersonMissile m in missiles) {
					if (m != null && !m.isFinished())
						return false;
				}
			}
			if (isAI && (DateTime.Now - lastGrabTime).TotalSeconds < stopGrabTime)
				return false;

			if (f == null) {
				List<Person> fs = getObjectInRangeByType<Person>();
				if (fs == null)
					return false;
				f = fs[0];
			}
			playSound(swingVoice[Common.getRandom(0, swingVoice.Length - 1)], true, false);
			grabTarget = f;
			f.grab(this);
			return true;
		}

		private void letGoOfTarget()
		{
			grabTarget.letGo();
			grabTarget = null;
			stopGrabTime = Common.getRandom(5, 10);
			lastGrabTime = DateTime.Now;
		}

		/// <summary>
		/// Updates this person's coordinates and also takes care of updating the listener if necessary.
		/// </summary>
		/// <param name="x">The X coordinate of the object.</param>
		/// <param name="y">The Y coordinate of the object.</param>
		public void updateCoordinates(int x, int y)
		{
			this.x = x;
			this.y = y;
			if (!isAI)
				DSound.SetCoordinates(x, 0.0, y);
		}

		public override bool isBlocked(int x, int y)
		{
			if (beingGrabbed)
				return false;
			return base.isBlocked(x, y);
		}

		private void seekRandomCoordinates()
		{
			int x = 0, y = 0;
			do {
				x = Common.getRandom(0, MapNode.maxX);
				y = Common.getRandom(0, MapNode.maxY);
			} while (isBlockedByObject(x, y));
			startSeeking(x, y);
		}

		private void seekRandomFurniture()
		{
			FightObject f = null;
			do {
				f = things[Common.getRandom(0, things.Count - 1)];
			} while (!(f is Furniture));
			startSeeking(f);
		}

		public override void playCrashSound(int x, int y)
		{
			if (crashSound == null)
				crashSound = DSound.LoadSound(DSound.SoundPath + "\\a_fwall.wav");
			DSound.PlaySound3d(crashSound, true, false, x, y, 0);
		}

		/// <summary>
		/// Sets stun time. The person is hindered from doing anything during this time.
		/// </summary>
		/// <param name="min">The minimum value to set the stun time to.</param>
		/// <param name="max">The maximum value to set the stun time to.</param>
		private void setStunTime(int min, int max)
		{
			stunTime = Common.getRandom(min, max);
			lastStunTime = DateTime.Now;
		}

		public bool isStunned()
		{
			return (DateTime.Now - lastStunTime).TotalSeconds < stunTime;
		}

		private int juliusLevel()
		{
			if (!isAI)
				return 0;
			if (Mission.fightType == Interaction.FightType.lastFight1)
				return 1;
			else if (Mission.fightType == Interaction.FightType.lastFight2)
				return 2;
			else if (Mission.fightType == Interaction.FightType.lastFight3)
				return 3;
			return 0;
		}

		private void throwMissile(Person target)
		{
			if (missiles == null)
				return;
			int speed = 0;
			if (juliusLevel() == 2)
				speed = Common.getRandom(1000, 2000);
			else if (juliusLevel() == 3)
				speed = Common.getRandom(1000, 1400);
			if ((DateTime.Now - lastMissileTime).TotalSeconds >= missileTime) {
				lastMissileTime = DateTime.Now;
				missileTime = Common.getRandom(10, 60);
				for (int i = 0; i < missiles.Length; i++) {
					if (missiles[i] == null || missiles[i].isFinished()) {
						missiles[i] = new PersonMissile(this, target, speed, 100);
						break;
					}
				}
			}
		}

		public override void cleanUp()
		{
			if (missiles != null) {
				foreach (PersonMissile p in missiles) {
					if (p == null)
						continue;
					p.cleanUp();
				}
			}
			if (stepSound != null) {
				for (int i = 0; i < stepSound.Length; i++)
					DSound.unloadSound(ref stepSound[i]);
			}
			if (swingSound != null) {
				for (int i = 0; i < swingSound.Length; i++)
					DSound.unloadSound(ref swingSound[i]);
			}
			if (swingVoice != null) {
				for (int i = 0; i < swingVoice.Length; i++)
					DSound.unloadSound(ref swingVoice[i]);
			}
			if (punchedSound != null) {
				for (int i = 0; i < punchedSound.Length; i++)
					DSound.unloadSound(ref punchedSound[i]);
			}
			if (gruntSound != null) {
				for (int i = 0; i < gruntSound.Length; i++)
					DSound.unloadSound(ref gruntSound[i]);
			}
			if (tossSound != null) {
				for (int i = 0; i < tossSound.Length; i++)
					DSound.unloadSound(ref tossSound[i]);
			}
			if (tossVoice != null) {
				for (int i = 0; i < tossVoice.Length; i++)
					DSound.unloadSound(ref tossVoice[i]);
			}
			DSound.unloadSound(ref throwSound);
			DSound.unloadSound(ref impactSound);
			DSound.unloadSound(ref hitSound);
			DSound.unloadSound(ref blockSound);
			DSound.unloadSound(ref crashSound);
			DSound.unloadSound(ref lastStepSound);
			DSound.unloadSound(ref stunSound);
			DSound.unloadSound(ref shootSound);
			DSound.unloadSound(ref lockSound);
		}

		private void moveMissiles()
		{
			if (missiles == null)
				return;
			System.Diagnostics.Trace.WriteLine("Missiles not null; traversing.");
			foreach (PersonMissile m in missiles) {
				if (m != null)
					m.move();
			}
		}

		public void playGrunt()
		{
			playSound(gruntSound[Common.getRandom(0, gruntSound.Length - 1)], true, false);
		}

		public bool hitAndGrunt(int maxDamage)
		{
			playGrunt();
			return hit(maxDamage);
		}

		private void lockOnPerson(Person target)
		{
			System.Diagnostics.Trace.WriteLine("Shoot method: entered lock method");
			if (juliusLevel() != 3)
				return;
			if (waitingToShoot || (DateTime.Now - lastShootTime).TotalSeconds < maxShootTime)
				return;
			DSound.PlaySound3d(lockSound, true, false, shootX = target.x, 0, shootY = target.y);
			shootTarget = target;
			waitingToShoot = true;
			//shoot time is set in shoot method because we need to wait for locksound to stop first.
			shootTime = 0;
		}

		private void shootPerson(Person target)
		{
			System.Diagnostics.Trace.WriteLine("Shoot method: entered shoot method");
			if (!waitingToShoot)
				return;
			if (DSound.isPlaying(lockSound))
				return;
			if (shootTime == 0) {
				lastShootTime = DateTime.Now;
				shootTime = Common.getRandom(1, 3000);
			}
			System.Diagnostics.Trace.WriteLine("Shoot: Got passed init");
			System.Diagnostics.Trace.WriteLine(String.Format("Shoot: shootTime {0}; diff {1}", shootTime, (DateTime.Now - lastShootTime).TotalMilliseconds));
			if ((DateTime.Now - lastShootTime).TotalMilliseconds > shootTime) {
				DSound.PlaySound3d(shootSound, true, false, shootX, 0, shootY);
				if (Degrees.getDistanceBetween(shootX, shootY, shootTarget.x, shootTarget.y) <= 2)
					shootTarget.hitAndGrunt(300);
				waitingToShoot = false;
				lastShootTime = DateTime.Now;
				maxShootTime = Common.getRandom(1, 10);
				shootTime = 0;
				shootTarget = null;
			}
		}
	}
}
