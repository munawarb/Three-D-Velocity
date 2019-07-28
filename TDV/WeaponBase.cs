/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
using SharpDX.DirectSound;
namespace TDV
{
	public abstract class WeaponBase : Projector, Explosive
	{
		public event readyToDisposeHandler readyToDispose;

		public event hitHandler eventHit;

		private bool m_finished;
		private List<Projector> vArray; //all the objects we are close to
		private Projector m_target;
		private Range reachRange;
		private int m_ticks;
		private bool m_performing;
		private Weapons m_weapon;
		private Projector m_origTarget;
		private bool m_followTarget;
		protected SecondarySoundBuffer expl;
		protected WeaponTypes type;

		//Will determine if during mission mode,
		//if this weapon should raise itself to the
		//target object's altitude if
		//the target's altitude changes.
		protected bool followTarget
		{
			get { return m_followTarget; }
			set { m_followTarget = value; }
		}

		public Projector origTarget
		{
			get { return m_origTarget; }
			set { m_origTarget = value; }
		}

		public bool finished
		{
			get { return (m_finished); }
			set { m_finished = value; }
		}
		public Projector target
		{
			get { return (m_target); }
			set { m_target = value; }
		}

		//Defines the range a projectile must be to the target in order for
		//it to consider itself a hit.
		/*
  public Range fireRange {
   get { return (m_fireRange); }
   set { m_fireRange = value; }
  }
        */
		public int ticks
		{
			get { return (m_ticks); }
			set { m_ticks = value; }
		}

		public bool performing
		{
			get { return (m_performing); }
			set { m_performing = value; }
		}

		public Weapons weapon
		{
			get { return (m_weapon); }
			set { m_weapon = value; }
		}

		/// <summary>
		/// Creates a new weapon.
		/// </summary>
		/// <param name="weapon">The Weapons class of the creator of this weapon.</param>
		/// <param name="n">The name of this weapon.</param>
		public WeaponBase(Weapons weapon, String n) : base(n)
		{
			//Assign the name of this instance of weapon to its id so that it can be matched and deleted later
			this.weapon = weapon;
			x = weapon.creator.x;
			y = weapon.creator.y;
			z = weapon.creator.z;
			isAI = weapon.creator.isAI; //So loadSound and playSound work properly.
			followTarget = true;
			expl = null;
			setDamagePoints(1);
		}
		public bool isPerforming()
		{
			return (performing);
		}

		public bool isFinished()
		{
			return finished && !performing || Weapons.requestedClear;
		}

		public Projector getLockedTarget()
		{
			return (target);
		}

		public virtual void onTick()
		{
			//First, need to set what object sare in range
			//since this is the first ontick method.
			if (vArray == null)
				vArray = Interaction.getObjectsInRange(this,
				   reachRange,
				   Interaction.RangeFlag.existing);
			updateTotalDistance();
			move();
			if (Options.mode == Options.Modes.mission
				&& followTarget
				&& origTarget != null)
				z = origTarget.z;
			if (damage < 1) {
				expl = DSound.LoadSound3d(DSound.SoundPath + "\\m4-1.wav");
				DSound.PlaySound(expl, true, false);
				finished = true;
			}
		}

		//Determines if this weapon is close enough to consider itself a hit
		//If it is, this method returns the projector that should be hit.
		//If no projectors are in range, this method
		//returns null.
		//NOTE: This method does NOT modify the target variable.
		//Therefore, target can still be used as normal.
		public bool inFiringRange()
		{
			if (vArray != null) {
				foreach (Projector p in vArray) {
					if (p.Equals(weapon.creator)
						|| !p.showInList
						|| ((Mission.refueler != null && p.Equals(Mission.refueler)
						|| Mission.carrier != null && p.Equals(Mission.carrier))
						&& !weapon.creator.Equals(Mission.player))
						)
						continue; //don't let weapon hit the launcher.
								  //Also don't let it hit the refuelr or carrier if it's not the player firing
					if (Weapons.isValidLock(p) && collidesWith(p)
					 && (!Options.isPlayingOnline || p.isSender())) {
						target = p;
						return true;
					}
				} //foreach
			} //if vArray != null
			else {
				//vArray is null,
				//meaning there is a defined target to hit,
				//Or a targetless weapon just didn't find any projectors in range
				if (origTarget != null
					&& collidesWith(origTarget) && Weapons.isValidLock(origTarget)) {
					target = origTarget;
					return true;
				}
				return false;
			} //vArray == null
			return false;
		}

		//Determines if the current projectile is vertically aligned with tz.
		public bool inVerticalRange(double tz)
		{
			if (origTarget == null)
				throw new ArgumentException("Orig Target was null "
			+ "during vertical range alignment.");
			return (Math.Abs(z - origTarget.z) <= tz);
		}

		public override bool Equals(object o)
		{
			return (object.ReferenceEquals((object)this, o));
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		//based on the neutralized speed of this projectile,
		//this emthod returns what horizontal distance is achievable closest to the target.
		protected Range getIdealFireRange()
		{
			return (new Range((speed / 60.0 / 60.0 / 1000.0 * (double)Common.intervalMS) + 0.20, 3.0));
		}

		public override void save(BinaryWriter w)
		{
			w.Write(name); //used so load knows what weapon to create
			if (origTarget != null)
				w.Write(origTarget.id);
			else
				w.Write("-");
			//OrigTarget could be null if no specific target was specified.
			w.Write(reachRange.horizontalDistance);
			w.Write(reachRange.verticalDistance);
			base.save(w, false); //Don't save name,
								 //since we already saved it above.
			w.Write(finished);
		}

		public override bool load()
		{
			if (!base.load())
				return false;
			BinaryReader r = Common.inFile;
			finished = r.ReadBoolean();
			System.Diagnostics.Trace.WriteLine("W2Create " + id);
			return true;
		}

		public virtual void initRange(Range reachRange)
		{
			this.reachRange = reachRange;
		}

		/// <summary>
		/// Plays a sound in 3D desipte setting of isAI.
		/// </summary>
		/// <param name="s">The SecondarySoundBuffer to play.</param>
		/// <param name="stopFlag">True if this sound should be stopped before it is played or replayed.</param>
		/// <param name="loopFlag">True if this sound should loop.</param>
		protected void playSound3d(SecondarySoundBuffer s, bool stopFlag, bool loopFlag)
		{
			DSound.PlaySound3d(s, stopFlag, loopFlag, x, z, y);
		}

		protected void fireHitEvent(Projector target, int damageAmount)
		{
			if (eventHit != null)
				eventHit(this, target, damageAmount, type);
		}
		protected void fireDisposeEvent()
		{
			if (readyToDispose != null)
				readyToDispose(this);
		}
		abstract public void lockOn(Projector target);

		abstract public void use();

		abstract public void serverSideHit(Projector target, int remainingDamage);

		public virtual void free()
		{
			DSound.unloadSound(ref expl);
		}

		protected void explode()
		{
			expl = DSound.LoadSound3d(DSound.SoundPath + "\\m4-1.wav");
			playSound3d(expl, true, false);
			List<Projector> hits = Interaction.getObjectsInRange(this, new Range(2.0, 1000.0), Interaction.RangeFlag.existing);
			if (hits == null)
				return;
			foreach (Projector hit in hits) {
				hit.hit(10, Interaction.Cause.destroyedByWeapon);
				fireHitEvent(hit, hit.damage);
			}
		}

	}
}