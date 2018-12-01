/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using SharpDX.DirectSound;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.Input;
namespace TDV
{
	public class MissileInterceptor : WeaponBase
	{
		private SecondarySoundBuffer launchSound;
		private SecondarySoundBuffer missileSound;
		private SecondarySoundBuffer hitSound;


		public MissileInterceptor(Weapons w)
			: base(w, "p" + (int)WeaponTypes.cruiseMissile)
		{
			type = WeaponTypes.missileInterceptor;
			weapon.decreaseAmmunitionFor(WeaponTypes.missileInterceptor);
			neutralizeSpeed(900.0);
			setSpan(0.10, 0.25);
			launchSound = loadSound(soundPath + "ci1.wav");
			missileSound = DSound.LoadSound3d(DSound.SoundPath + "\\ci2.wav");
			addVolume(missileSound);
		}

		public override void free()
		{
			base.free();
			DSound.unloadSound(ref launchSound);
			DSound.unloadSound(ref missileSound);
			DSound.unloadSound(ref hitSound);
		}

		public override void lockOn(Projector target)
		{
			origTarget = target;
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
				//Do not free this weapon until the sound is done playing or the program will act up.
				if (hitSound != null)
					performing = DSound.isPlaying(hitSound);
				return;
			}

			performing = true;
			playSound3d(missileSound, false, true);
			if (!inVerticalRange(10))
			{
				if (z > origTarget.z)
					z -= 10;
				if (z < origTarget.z)
					z += 10;
				if (Math.Abs(z - origTarget.z) <= 10)
					z = origTarget.z;
			}
			direction = Degrees.GetDegreesBetween(x, y, origTarget.x, origTarget.y);
			base.onTick();
			if (inFiringRange())
			{
				missileSound.Stop();
				hitSound = target.loadSound(target.soundPath + "m3-" + Common.getRandom(1, 2) + ".wav");
				target.playSound(hitSound, true, false);
				target.hit(target.damage, Interaction.Cause.destroyedByWeapon);
				fireHitEvent(target, target.damage);
				finished = true;
				return;
			}
			if (totalDistance > 30.0 || !Weapons.isValidLock(origTarget) || finished)
			{
				missileSound.Stop();
				finished = true;
				performing = false;
			}
		}

		public override void serverSideHit(Projector target, int remainingDamage)
		{
			missileSound.Stop();
			hitSound = target.loadSound(target.soundPath + "m3-" + Common.getRandom(1, 2) + ".wav");
			target.playSound(hitSound, true, false);
			fireHitEvent(target, remainingDamage);
			finished = true;
		}

		public override void use()
		{
			direction = Degrees.GetDegreesBetween(x, y, origTarget.x, origTarget.y);
			z = origTarget.z;
			playSound(launchSound, true, false);
			if (!isAI)
				DXInput.startFireEffect();
		}

	}
}
