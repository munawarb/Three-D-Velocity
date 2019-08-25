/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using BPCSharedComponent.VectorCalculation;
using System;
namespace TDV
{
	public class Chopper : Aircraft
	{
		public Chopper(float x, float y)
			: base(0, 1000, "c", true, new Track(Options.currentTrack))
		{
			showInList = true;
			this.x = x;
			this.y = y;
			maxProbability = 30;
			weapon = new Weapons(this,
						 WeaponTypes.guns, WeaponTypes.explosiveMissile);
			weapon.setAmmunitionFor(WeaponTypes.explosiveMissile, 50);
			startAtHeight(15000f);
			if (!Options.isLoading)
				Mission.chopperCount++;
			setDamagePoints(2500);
			liftSpeed = 50;
		}

		public Chopper()
			: this(0f, 0f)
		{ }


		protected override void loadSounds()
		{
			base.loadSounds();
			engine = loadSound(soundPath + "e6.wav");
			explodeSound = loadSound(soundPath + "d1.wav");
		}

		protected override void muteEngines()
		{
			engine.stop();
			base.muteEngines();
		}

		public override void move()
		{
			if (readyToTerminate())
			{
				isProjectorStopped = true;
				return;
			}
			if (firstMove)
				z = Mission.player.z - 1000f;
			base.move();
			if (Mission.player.z > minAltitude
					&& Mission.player.z < maxAltitude - 5000f)
			{
				if (Math.Abs(z - Mission.player.z) <= 550f)
					z = Mission.player.z;
			} //if player z > 1000
			if (z <= 1f)
				z = 1000f;
			if (Degrees.getDistanceBetween(x, y, Mission.player.x, Mission.player.y) >= 20f)
			{
				x = Common.getRandom((int)Math.Min(x, Mission.player.x),
					(int)Math.Max(x, Mission.player.x));
				y = Common.getRandom((int)Math.Min(y, Mission.player.y),
					(int)Math.Max(y, Mission.player.y));
				if (x == Mission.player.x)
				{
					if (Math.Max(x, Mission.player.x) == x)
						x += 3f;
					else
						x -= 3f;
				} //if we fell on player
				if (y == Mission.player.y)
				{
					if (Math.Max(y, Mission.player.y) == y)
						y += 3f;
					else
						y -= 3f;
				} //if we fell on player
			} //if chopper running away
		}



		public override void revive()
		{
			base.revive();
			z = Mission.player.z - 1000f;
			if (z < 1000f)
				z = 1000f;
		}
	}
}
