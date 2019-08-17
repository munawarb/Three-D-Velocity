/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using System.Collections.Generic;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
namespace TDV
{
	public class ExplosiveMissile : WeaponBase
	{
		private ExtendedAudioBuffer missileLaunchSound;
		private ExtendedAudioBuffer missileSound;
		private ExtendedAudioBuffer missileHitSound;
		private ExtendedAudioBuffer missileExplodeSound;

		public ExplosiveMissile(Weapons w)
			: base(w, "p" + (int)WeaponTypes.explosiveMissile)
		{
			weapon.decreaseAmmunitionFor(WeaponTypes.explosiveMissile);
			missileLaunchSound = loadSound(soundPath + "m1.wav");
			missileSound = DSound.LoadSound(DSound.SoundPath + "\\m2.wav");
			neutralizeSpeed(1500.0);
			setSpan(0.1, 0.1);
			followTarget = false;
		}

		public override void lockOn(Projector target)
		{
			origTarget = target;
			z = origTarget.z;
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
				//The weapon is done doing what it needs to do, but a sound is still playing.
				//Do not free this weapon until the sound is done playing or sound will get cut.
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
				target.hit(Common.getRandom(101, 200), Interaction.Cause.destroyedByWeapon);
				fireHitEvent(target, target.damage);
				finished = true;
				return;
			}
			if (totalDistance > 15.0)
			{
				missileSound.stop();
				explode();
				finished = true;
				performing = (missileHitSound != null && DSound.isPlaying(missileHitSound)) || (expl != null && DSound.isPlaying(expl));
			}
		}

		public override void use()
		{
			direction = weapon.creator.direction;
			z = origTarget.z;
		}

		public override void free()
		{
			DSound.unloadSound(ref missileLaunchSound);
			DSound.unloadSound(ref missileSound);
			DSound.unloadSound(ref missileHitSound);
			DSound.unloadSound(ref missileExplodeSound);
		}



		public override void serverSideHit(Projector target, int remainingDamage)
		{
			throw new NotImplementedException("This weapon is not available online.");
		}

	}
}
