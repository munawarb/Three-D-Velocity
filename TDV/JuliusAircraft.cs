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
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
namespace TDV
{
	public class JuliusAircraft : Aircraft
	{
		private ExtendedAudioBuffer taunt;
		private ExtendedAudioBuffer startChargeSound;
		private ExtendedAudioBuffer chargeSound;
		private ExtendedAudioBuffer endChargeSound;
		private bool incarnatedChoppers;
		private bool incarnatedFighters;
		private bool incarnatedFighters2;
		//Two values below are measured in milliseconds
		private int rechargeTime;
		private int fireTime;
		//Two values below are measured in seconds
		private int maxRechargeTime;
		private int maxFireTime;
		public JuliusAircraft(double x, double y)
			: base(0, 1000, "db", true, new Track(Options.currentTrack))
		{
			showInList = true;
			this.x = x;
			this.y = y;
			maxProbability = 30;
			weapon = new Weapons(this,
			 WeaponTypes.guns,
			 WeaponTypes.missile,
			 WeaponTypes.laserCannonSystem,
			 WeaponTypes.cruiseMissile,
			 WeaponTypes.explosiveMissile);
			weapon.setInfiniteAmmunition();
			startAtHeight(1000.0);
			setDamagePoints(10000);
			setStrafeTime(20, 10);
			liftSpeed = 200;
			//set the two values below to initial defaults so he starts firing
			//when the match starts
			fireTime = 1;
			maxFireTime = 30;
		}

		public JuliusAircraft()
			: this(0.0, 0.0)
		{ }


		protected override void loadSounds()
		{
			base.loadSounds();
			engine = loadSound(soundPath + "e9.wav");
			explodeSound = loadSound(soundPath + "d1.wav");
			startChargeSound = loadSound(soundPath + "cs.wav");
			chargeSound = loadSound(soundPath + "cl.wav");
			endChargeSound = loadSound(soundPath + "ce.wav");
		}

		protected override void muteEngines()
		{
			engine.stop();
			chargeSound.stop();
			endChargeSound.stop();
			startChargeSound.stop();
			base.muteEngines();
		}


		public override void move()
		{
			if (readyToTerminate())
			{
				muteAllSounds();
				isProjectorStopped = true;
				return;
			}
			if (!incarnatedChoppers && firstMove)
			{
				Chopper chopper = null;
				for (int i = 1; i <= 4; i++)
				{
					chopper = Mission.createNewChopper();
					chopper.x = this.x + (4 - (2 * i));
					chopper.y = this.y;
					if (chopper.x == this.x)
						chopper.x = this.x - 2;
				}
				incarnatedChoppers = true;
			}

			if (!incarnatedFighters && Mission.juliusDieCount == 1)
			{
				for (int i = 1; i <= 2; i++)
					Mission.createNewFighter(i, 10);
				incarnatedFighters = true;
			} //if !incarnated

			if (!incarnatedFighters2 && Mission.juliusDieCount == 2)
			{
				for (int i = 1; i <= 6; i++)
					Mission.createNewFighter(i, 5);
				incarnatedFighters2 = true;
			} //if !incarnated

			tickWeaponTimer();
			if (rechargeTime != 0)
				playSound(chargeSound, false, true); //move the sound in 3d space
			if (fireTime != 0)
			{
				if (Common.getRandom(1, 300) == 150)
					teleport();
				if (Common.getRandom(1, 300) == 5)
					specialMove();
				if (getPosition(Mission.player).distance >= 15.0)
				{
					x = Mission.player.x - 3.0;
					y = Mission.player.y + 3.0;
					playTaunt(soundPath + "j2-"
					 + Common.getRandom(1, 3)
					 + ".wav");
				}
			} //if can fire
			regenerate();
			base.move();
		}

		/// <summary>
		/// If julius has been shot down, will regenerate, else will take no action,
		/// and the code will fall through to calling Aircraft.die() on Julius.
		/// </summary>
		private void regenerate()
		{
			if (hit() && (++Mission.juliusDieCount) < Mission.maxJuliusDieCount)
			{ //if need to reincarnate,
				stopCharge(false);
				Interaction.advanceToNextMission(this);
				cause = Interaction.Cause.none;
				strengthenArmor(Mission.juliusDieCount);
			}
		}

		private void teleport()
		{
			switch (Common.getRandom(1, 4))
			{
				case 1:
					x = Mission.player.x - 2;
					y = Mission.player.y - 2;
					direction = getPosition(Mission.player).degrees;
					break;
				case 2:
					//julius will end up in front of player
					x = Mission.player.x;
					y = Mission.player.y + 2;
					direction = Mission.player.direction;
					break;
				case 3:
					x = Mission.player.x + 2;
					y = Mission.player.y - 2;
					break;
				case 4:
					x = Mission.player.x + 20;
					y = Mission.player.y + 10;
					break;
			} //switch

			playTaunt(soundPath + "j" + Common.getRandom(3, 4) + ".wav");
		}

		private void specialMove()
		{
			playTaunt(soundPath + "j1-1.wav");
			weapon.weaponIndex = WeaponTypes.guns;
			do
			{
				fireWeapon();
			} while (weapon.increaseWeaponIndex() != WeaponTypes.guns);
		}

		private void playTaunt(String t)
		{
			if (taunt != null && DSound.isPlaying(taunt))
				return;
			taunt = DSound.LoadSound(t);
			DSound.PlaySound(taunt, true, false);
		}

		public void warnPlayer()
		{
			if (taunt != null)
				taunt.stop();
			playTaunt(soundPath + "j1-2.wav");
		}

		private void strengthenArmor(byte stage)
		{
			if (stage == 1)
			{
				setDamagePoints(25000);
				maxProbability = 40;
				maxFireTime = 60;
				setStrafeTime(30, 10);
			}
			if (stage == 2)
			{
				setDamagePoints(30000);
				maxProbability = 70;
				maxFireTime = 120;
				setStrafeTime(50, 30);
			}
		}

		public override void save(BinaryWriter w)
		{
			base.save(w);
			w.Write(maxProbability);
			w.Write(incarnatedChoppers);
			w.Write(incarnatedFighters);
			w.Write(incarnatedFighters2);
			w.Write(rechargeTime);
			w.Write(maxRechargeTime);
			w.Write(fireTime);
			w.Write(maxFireTime);
		}
		public override bool load()
		{
			if (!base.load())
				return false;
			BinaryReader w = Common.inFile;
			maxProbability = w.ReadInt32();
			incarnatedChoppers = w.ReadBoolean();
			if (Common.version >= 1.1f)
			{
				incarnatedFighters = w.ReadBoolean();
				incarnatedFighters2 = w.ReadBoolean();
			}
			if (Common.version >= 1.3f)
			{
				rechargeTime = w.ReadInt32();
				maxRechargeTime = w.ReadInt32();
				fireTime = w.ReadInt32();
				maxFireTime = w.ReadInt32();
			}
			return true;
		}

		private void tickWeaponTimer()
		{
			if (fireTime == 0)
			{
				rechargeTime += Common.intervalMS;
				if (rechargeTime / 1000 >= maxRechargeTime)
				{
					stopCharge(true);
					rechargeTime = 0;
					fireTime = 1;
					maxFireTime = Common.getRandom(10, 30);
				}
			}
			else
			{ //if fire time counting
				fireTime += Common.intervalMS;
				if (fireTime / 1000 >= maxFireTime)
				{
					fireTime = 0;
					rechargeTime = 1;
					startCharge();
					maxRechargeTime = Common.getRandom(5, 50);
				} //if firetime exceeded
			} //if firetime counting
		}

		protected override string fireWeapon()
		{
			if (fireTime == 0)
				return null;
			return base.fireWeapon();
		}

		private void startCharge()
		{
			playSound(startChargeSound, true, false);
			playSound(chargeSound, true, true);
			((Aircraft)Mission.player).announceRecharging();
		}

		private void stopCharge(bool justStopSounds)
		{
			if (rechargeTime == 0)
				return;
			playSound(endChargeSound, true, false);
			chargeSound.stop();
			startChargeSound.stop();
			if (justStopSounds)
				((Aircraft)Mission.player).announceDoneCharging();
			else
			{
				rechargeTime = 0;
				fireTime = 1;
			}
		}
		public override void muteAllSounds()
		{
			if (taunt != null)
				taunt.stop();
			base.muteAllSounds();
		}

		public override void freeResources()
		{
			base.freeResources();
			DSound.unloadSound(ref engine);
			DSound.unloadSound(ref startChargeSound);
			DSound.unloadSound(ref chargeSound);
			DSound.unloadSound(ref endChargeSound);
			DSound.unloadSound(ref taunt);
		}

	}
}