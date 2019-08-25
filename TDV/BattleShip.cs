/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Threading;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.ExtendedAudio;
namespace TDV
{
	public class BattleShip : MissionObjectBase
	{
		private ExtendedAudioBuffer moveSound;
		private bool firstLoad;
		public BattleShip(float x, float y, Instructions i)
			: base("bs", i)
		{
			firstLoad = true;
			this.x = x;
			this.y = y;
			setSpan(0.5f, 0.5f);
			neutralizeSpeed(50f);
			weapon.weaponIndex = WeaponTypes.battleShipGuns;
			moveSound = loadSound(soundPath + "e7.wav");
			explodeString = soundPath + "d3.wav";
		}
		public BattleShip()
			: this(0f, 0f, new Instructions())
		{ }

		public override void move()
		{
			if (firstLoad)
			{
				playSound(moveSound, true, true);
				firstLoad = false;
			}
			performDeaths();
			if (readyToTerminate())
			{
				moveSound.stop();
				isProjectorStopped = true;
				return;
			}
			if (!isRequestedTerminated)
			{
				base.move();
				playSound(moveSound, false, true);
				base.updateTotalDistance();
				registerLock();
				fireWeapon();
				processRoute();
			}
		}

		protected override void performDeaths()
		{
			if (hit())
			{
				moveSound.stop();
				base.performDeaths();
			}
		}

		public override void freeResources()
		{
			base.freeResources();
			DSound.unloadSound(ref moveSound);
		}

	}
}
