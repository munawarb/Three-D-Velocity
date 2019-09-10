/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;

namespace TDV
{
	public class PersonMissile
	{
		private int x, y;
		private ExtendedAudioBuffer moveSound, explodeSound;
		private int speed, maxDamage;
		private DateTime lastMoveTime, startTime;
		private Person target;

		/// <summary>
		/// Creates a new missile.
		/// </summary>
		/// <param name="thrower">The Person throwing the missile.</param>
		/// <param name="target">The target Person.</param>
		/// <param name="speed">The number of milliseconds to elapse between each move of the missile.</param>
		/// <param name="maxDamage">The maximum damage the missile can cause.</param>
		public PersonMissile(Person thrower, Person target, int speed, int maxDamage)
		{
			x = thrower.x;
			y = thrower.y;
			this.speed = speed;
			this.maxDamage = maxDamage;
			this.target = target;
			startTime = DateTime.Now;
			moveSound = DSound.LoadSoundAlwaysLoud(DSound.SoundPath + "\\a_mmove.wav");
			explodeSound = DSound.LoadSoundAlwaysLoud(DSound.SoundPath + "\\a_mexpl.wav");
			DSound.PlaySound3d(moveSound, true, true, x, 0, y);
		}

		public void move()
		{
			if (isFinished() || DSound.isPlaying(explodeSound))
				return;
			if ((DateTime.Now - lastMoveTime).TotalMilliseconds >= speed)
			{
				lastMoveTime = DateTime.Now;
				System.Diagnostics.Trace.WriteLine("Missile moved");
				if (target.x < x)
					x--;
				else if (target.x > x)
					x++;
				else if (target.y < y)
					y--;
				else if (target.y > y)
					y++;
				DSound.PlaySound3d(moveSound, false, true, x, 0, y);
			}
			if ((DateTime.Now - startTime).Seconds >= 10)
			{
				moveSound.stop();
				DSound.PlaySound3d(explodeSound, true, false, x, 0, y);
				if (Degrees.getDistanceBetween(x, y, target.x, target.y) <= 1)
					target.hitAndGrunt(maxDamage);
			}
		}

		public bool isFinished()
		{
			if (moveSound == null) //clean up has occurred
				return true;
			if (DSound.isPlaying(moveSound))
				return false;
			return !DSound.isPlaying(explodeSound);
		}
		public void cleanUp()
		{
			DSound.unloadSound(ref moveSound);
			DSound.unloadSound(ref explodeSound);
		}
	}
}
