/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Threading;

namespace TDV
{
	public class Island : MissionObjectBase
	{
		public Island(float x, float y)
			: base("i", null)
		{
			this.x = x;
			this.y = y;
			setSpan(1f, 100f);
			forceStareo = true;
			explodeString = soundPath + "d8.wav";
			showInList = false;
		}

		public Island()
			: this(0f, 0f)
		{ }

		public override void move()
		{
			performDeaths();
			if (readyToTerminate())
			{
				isProjectorStopped = true;
				return;
			}
		}
	}
}
