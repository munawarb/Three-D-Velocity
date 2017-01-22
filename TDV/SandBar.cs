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
using SharpDX.DirectSound;

namespace TDV
{
	public class SandBar : Furniture
	{
		/// <summary>
		/// Creates a new sandbar
		/// </summary>
		/// <param name="x">The mid X value.</param>
		/// <param name="y">The mid y value.</param>
		/// <param name="damage">The damage points</param>
		/// <param name="length">The length</param>
		/// <param name="width">The width</param>
		public SandBar(int x, int y, int damage, int length, int width)
			: base(x, y, damage, length, width)
		{
			hitFile = "fsandbar.wav";
		}
		public override bool hit(int max)
		{
			return false;
		}

		public override string ToString()
		{
			return "" + (x - width) + ", " + (x + width) + ", " + (y - length) + ", " + (y + length);
		}
	}



}
