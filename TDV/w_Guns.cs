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
using BPCSharedComponent.Input;
namespace TDV
{
	public class Guns : WeaponBase
	{

		private SecondarySoundBuffer gunHitSound;
		public Guns(Weapons w)
			: base(w, "p" + (int)WeaponTypes.guns)
		{
			type = WeaponTypes.guns;
			weapon.decreaseAmmunitionFor(WeaponTypes.guns);
			setSpan(200.0, 0.1);
			neutralizeSpeed(2000.0);
		}
		public override void lockOn(Projector target)
		{
			origTarget = target;
		}
		public override void use()
		{
			direction = weapon.creator.direction;
			if (!isAI)
				DXInput.startFireEffect();
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
				if (gunHitSound != null)
					performing = DSound.isPlaying(gunHitSound);
				return;
			}

			performing = true;
			base.onTick();
			if (inFiringRange())
			{
				gunHitSound = target.loadSound(target.soundPath + "gun1-" + Common.getRandom(1, 5) + ".wav");
				target.playSound(gunHitSound, true, false);
				fireHitEvent(target, 10);
				finished = true;
				return;
			}

			if (totalDistance >= 4)
			{
				finished = true;
				performing = false;
			}
		}

		public override void serverSideHit(Projector target, int damageAmount)
		{
			gunHitSound = target.loadSound(target.soundPath + "gun1-" + Common.getRandom(1, 5) + ".wav");
			target.playSound(gunHitSound, true, false);
			fireHitEvent(target, damageAmount);
			finished = true;
		}

		public override void free()
		{
			DSound.unloadSound(ref gunHitSound);
		}
	}
}
