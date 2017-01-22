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
using SharpDX;

namespace TDV
{
	public struct FightObjectFeeler : IComparable
	{
		public Furniture furniture { get; set; }
		public int x { get; set; }
		public int y { get; set; }
		public int distance { get; set; }

		public FightObjectFeeler(Furniture f, int lx, int ly, int d)
			: this()
		{
			furniture = f;
			x = lx;
			y = ly;
			distance = d;
		}

		public int CompareTo(object obj)
		{
			FightObjectFeeler f = (FightObjectFeeler)obj;
			return distance - f.distance;
		}
	}
}
