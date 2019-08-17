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
using BPCSharedComponent.Input;
namespace TDV
{
	public class Missile : WeaponBase
	{
		private ExtendedAudioBuffer missileLaunchSound;
		private ExtendedAudioBuffer missileSound;
		private ExtendedAudioBuffer missileHitSound;
		private ExtendedAudioBuffer fox;
		private double tz;

		public Missile(Weapons w)
			: base(w, "p" + (int)WeaponTypes.missile)
		{
			type = WeaponTypes.missile;
			weapon.decreaseAmmunitionFor(WeaponTypes.missile);
			missileLaunchSound = loadSound(soundPath + "m1.wav");
			missileSound = DSound.LoadSound(DSound.SoundPath + "\\m2.wav");
			neutralizeSpeed(1500.0);
			setSpan(0.1, 0.1);
		}

		public override void lockOn(Projector target)
		{
			origTarget = target;
			if (!Options.isLoading)
				tz = origTarget.z;
		}

		public override void onTick()
		{
			if (isFinished())
			{
				fireDisposeEvent();
				return;
			}

			if (finished && performing)
			{
				performing = (missileHitSound != null && DSound.isPlaying(missileHitSound)) || (expl != null && DSound.isPlaying(expl));
				return;
			}

			performing = true;
			playSound3d(missileSound, false, true);
			base.onTick();
			if (inFiringRange())
			{
				missileSound.stop();
				missileHitSound = target.loadSound(target.soundPath + "m3-" + Common.getRandom(1, 3) + ".wav");
				target.playSound(missileHitSound, true, false);
				fireHitEvent(target, Common.getRandom(300));
				finished = true;
				return;
			}
			if (totalDistance > 15.0 || finished)
			{
				missileSound.stop();
				finished = true;
				performing = (missileHitSound != null && DSound.isPlaying(missileHitSound)) || (expl != null && DSound.isPlaying(expl));
			}
		}

		public override void serverSideHit(Projector target, int damageAmount)
		{
			missileSound.stop();
			missileHitSound = target.loadSound(target.soundPath + "m3-" + Common.getRandom(1, 3) + ".wav");
			target.playSound(missileHitSound, true, false);
			fireHitEvent(target, damageAmount);
			finished = true;
		}

		public override void use()
		{
			direction = weapon.creator.direction;
			int ammunitionNumber = weapon.ammunitionFor(WeaponTypes.missile) + 1;
			if (!isAI)
			{
				if (ammunitionNumber % 2 != 0) //odd number means fire from right
					DSound.setPan(missileLaunchSound, (ammunitionNumber + 1) / 2 * 0.25f); //give 1 to ammunition number so we can divide properly; adding 1 will make it evenly divisible by 2.
				else
					DSound.setPan(missileLaunchSound, ammunitionNumber / 2 * -0.25f);
				fox = loadSound(soundPath + "fox2.wav");
				playSound(fox, true, false);
				DXInput.startFireEffect();
			} //if !AI
			playSound(missileLaunchSound, true, false);
			if (origTarget != null)
			{
				z = origTarget.z;
				if (isAI && origTarget is Aircraft)
					((Aircraft)origTarget).notifyOf(Notifications.missileLaunch, Degrees.getDistanceBetween(x, y, origTarget.x, origTarget.y) <= 15);
			}
		}

		public override void free()
		{
			base.free();
			DSound.unloadSound(ref missileLaunchSound);
			DSound.unloadSound(ref missileSound);
			DSound.unloadSound(ref missileHitSound);
			DSound.unloadSound(ref fox);
		}

		public override void save(BinaryWriter w)
		{
			base.save(w);
			w.Write(tz);
		}

		public override bool load()
		{
			if (!base.load())
				return false;
			BinaryReader r = Common.inFile;
			tz = r.ReadDouble();
			return true;
		}

	}
}
