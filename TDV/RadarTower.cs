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

namespace TDV
{
	public class RadarTower : MissionObjectBase
	{
		private bool missileAttacker;
		public RadarTower(double x, double y, bool missileAttacker)
			: base("rs", null)
		{
			this.missileAttacker = missileAttacker;
			this.x = x;
			this.y = y;
			setSpan(0.05, 0.05);
			weapon.weaponIndex = WeaponTypes.missile;
			if (!Options.isLoading)
				incrementRadarCount();
			explodeString = soundPath + "d7.wav";
		}

		public RadarTower()
			: this(0.0, 0.0, false)
		{ }

		private void incrementRadarCount()
		{
			if (!missileAttacker)
				Mission.radarCount++;
			else
				Mission.missileCount++;
		}

		public override void save(BinaryWriter w)
		{
			base.save(w);
			w.Write(missileAttacker);
		}

		public override bool load()
		{
			if (!base.load())
				return false;
			BinaryReader r = Common.inFile;
			missileAttacker = r.ReadBoolean();
			return true;
		}

		public override void move()
		{

			performDeaths();
			if (readyToTerminate())
			{
				isProjectorStopped = true;
				return;
			}

			if (!isRequestedTerminated)
			{
				//if ((Mission.isDestroyingRadar && !missileAttacker)
				//|| (Mission.isMissileAttack && missileAttacker)){
				registerLock();
				fireWeapon();
			}
		}

		protected override void performDeaths()
		{
			if (isRequestedTerminated) //don't perform deaths if already performed
				return;
			if (hit())
			{
				if (!missileAttacker)
					Mission.radarCount--;
				else
					Mission.missileCount--;
			}
			base.performDeaths();
		}

	}
}
