/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using SharpDX.DirectSound;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
namespace TDV
{
	public class LaserCannonSystem : WeaponBase
	{
		private SecondarySoundBuffer cannonLaunch;

		public LaserCannonSystem(Weapons w)
			: base(w, "p" + (int)WeaponTypes.laserCannonSystem)
		{
			type = WeaponTypes.laserCannonSystem;
			weapon.decreaseAmmunitionFor(WeaponTypes.laserCannonSystem);
			neutralizeSpeed(3000.0);
			cannonLaunch = loadSound(DSound.SoundPath + "\\ca1.wav");
			setSpan(0.001, 0.001);
		}

		public override void free()
		{
			DSound.unloadSound(ref cannonLaunch);
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
				////The weapon is done doing what it needs to do, but a sound is still playing.
				////Do not free this weapon until the sound is done playing or the program will act up.
				performing = DSound.isPlaying(cannonLaunch);
				return;
			}

			performing = true;
			if (!inVerticalRange(10))
			{
				if (z < origTarget.z) z += 10;
				if (z > origTarget.z) z -= 10;
			}
			base.onTick();

			x = origTarget.x;
			y = origTarget.y;
			z = origTarget.z;

			if (inFiringRange())
			{
				cannonLaunch.Stop();
				origTarget.hit(true);
				fireHitEvent(origTarget, 0);
				weapon.lCSTarget = origTarget;
				finished = true;
				return;
			}
			if (!DSound.isPlaying(cannonLaunch))
			{
				performing = false;
				finished = true;
			}
		}

		public override void serverSideHit(Projector target, int remainingDamage)
		{
			cannonLaunch.Stop();
			fireHitEvent(target, remainingDamage);
			weapon.lCSTarget = target;
			finished = true;
		}

		public override void use()
		{
			direction = weapon.creator.direction;
			x = origTarget.x;
			y = origTarget.y;
			z = origTarget.z;
			playSound(cannonLaunch, true, false);
		}
	}
}
