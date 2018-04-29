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
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
using SharpDX.DirectSound;
namespace TDV
{
	public class MissionObjectBase : Projector
	{
		private int serverSendTicks;
		private Instructions m_inst;
		private Weapons m_weapon;
		private bool m_mustKill;
		private bool m_noKill;
		protected SecondarySoundBuffer explodeSound;
		private String m_explodeString;
		private String m_serverTag;
		private int m_maxProbability;

		public String serverTag
		{
			get { return m_serverTag; }
			set { m_serverTag = value; }
		}

		//The higher this number, the greater chance an object has of
		//firing at the player
		//100 means that the object will fire at the player
		//every tick.
		protected int maxProbability
		{
			get { return (m_maxProbability); }
			set { m_maxProbability = value; }
		}

		//allows to specify
		//what sound will be played when object is destroyed
		//this class's performDeaths method will load the sound
		//when the object is destroyed.
		protected String explodeString
		{
			get { return (m_explodeString); }
			set { m_explodeString = value; }
		}


		public InstructionNode currentNode()
		{
			return (instructions.current());
		}
		public Projector player
		{
			get { return (Mission.player); }
		}
		public bool mustKill
		{
			get { return (m_mustKill); }
			set { m_mustKill = value; }
		}
		public bool noKill
		{
			get { return (m_noKill); }
			set { m_noKill = value; }
		}
		public Instructions instructions
		{
			get { return (m_inst); }
			set { m_inst = value; }
		}
		public virtual Weapons weapon
		{
			get { return (m_weapon); }
			set { m_weapon = value; }
		}
		public MissionObjectBase(string n, Instructions inst)
			: base(0, 0, n, true)
		{
			m_inst = inst;
			m_weapon = new Weapons(this);
			maxProbability = 5;
			isObject = true;
			showInList = true;
		}
		public MissionObjectBase(Int16 d, Int16 maxspeed, string name, Instructions inst)
			: base(d, maxspeed, name, true)
		{
			m_inst = inst;
			m_weapon = new Weapons(this);
			maxProbability = 5;
			isObject = true;
		}

		public MissionObjectBase(Int16 d, Int16 maxspeed, string name, bool isAI, Instructions inst)
			: base(d, maxspeed, name, isAI)
		{
			m_inst = inst;
			m_weapon = new Weapons(this);
			maxProbability = 5;
			isObject = true;
		}

		public MissionObjectBase(Int16 d, Int16 maxspeed, string name, bool isAI)
			: base(d, maxspeed, name, isAI)
		{
			m_weapon = new Weapons(this);
			maxProbability = 5;
			isObject = true;
		}

		//sets attributes for this object
		public void setAttributes(bool mustKill, bool noKill)
		{
			this.mustKill = mustKill;
			this.noKill = noKill;
			if (mustKill)
			{
				Instructions.incrementMaxMustKill();
			}
		}
		protected virtual bool registerLock()
		{
			if (Interaction.isGameFinished())
				return false;
			if (Weapons.inRange(this, Mission.player, weapon.lockingRange)
				&& !weapon.isValidLock())
			{
				weapon.lockIndex = Mission.player.id;
				Interlocked.Increment(ref Mission.locks);
				return true;
			}
			else if (!Weapons.inRange(this, Mission.player, weapon.lockingRange)
						  && weapon.isValidLock())
			{
				weapon.lockIndex = "-";
				Interlocked.Decrement(ref Mission.locks);
				return false;
			}
			return false; //code won't get here anyway
		}

		public void processRoute()
		{
			if (currentNode().target)
			{
				direction = Degrees.GetDegreesBetween(x, y, currentNode().x, currentNode().y);
			}
		}

		public void moveNext()
		{
			if (instructions.current().target)
			{
				if (Degrees.getDistanceBetween(x, y, currentNode().x, currentNode().y) <= 1.0)
				{
					instructions.moveNext();
				}
			}
		}

		protected virtual String fireWeapon()
		{
			if (Mission.isJuliusFight)
				return null;
			if (weapon.inFiringRange())
			{
				if (Common.getRandom(1, 100) <= maxProbability)
					weapon.use(weapon.weaponIndex, weapon.getLockedTarget(), weapon.firingRange);
			}
			return null;
		}

		protected virtual void performDeaths()
		{
			if (isRequestedTerminated
				|| cause == Interaction.Cause.sentForTermination)
				return;
			if (hit())
			{
				explodeSound = loadSound(explodeString);
				playSound(explodeSound, true, false);
				isRequestedTerminated = true;
				Interaction.kill(this);
			}
		}

		/// <summary>
		/// Checks to see if this object can be disposed.
		///</summary>
		///<returns>True if the object can be disposed, false otherwise.</returns>
		public override bool readyToTerminate()
		{
			if (isProjectorStopped || dirty)
				return true;

			if (!DSound.isPlaying(explodeSound))
			{ //game hasn't ended, or explode sound is playing
				if (Interaction.isGameFinished(true))
					System.Diagnostics.Trace.WriteLine(String.Format("shutdown {0}, demo expired {1}, abort {2}, player damage is {3}", Options.requestedShutdown, Options.demoExpired, Options.abortGame, (Mission.player == null)?0:Mission.player.damage));
				return isRequestedTerminated || Interaction.isGameFinished(true);
			}
			return false;
		}

		public override void cleanUp()
		{
			weapon.use(); //will stop any weapons if it's end of match
		}

		public override void save(BinaryWriter w)
		{
			base.save(w);
			weapon.save(w);
			if (instructions != null)
				instructions.save(w);
		}

		public override bool load()
		{
			if (!base.load())
				return false;
			weapon.load();
			if (this is BattleShip
				|| this is Tank)
				instructions.load();
			return true;
		}

		public void removeLockIf(String id)
		{
			if (weapon.lockIndex.Equals(id))
				weapon.clearLock();
		}

		public override void freeResources()
		{
			base.freeResources();
			DSound.unloadSound(ref explodeSound);
		}
		protected void sendObjectUpdate()
		{
			if (isSender())
			{
				//Only send x and y and z and ... values on every tenth tick, or every 1 second.
				//Will minimize server load.
				Client.sendObjectUpdate(Client.completeBuild(this, (++serverSendTicks) == 10), id);
				if (serverSendTicks == 10)
					serverSendTicks = 1;
			} //if isSender
		}

		protected void sendFinalObjectUpdate()
		{
			Client.sendObjectUpdate(Client.completeBuild(this, true),
					id, true); //send last object state.
		}

	}
}