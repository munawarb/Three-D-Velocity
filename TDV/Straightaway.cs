/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;

namespace TDV
{
	public class Straightaway
	{
		private Int16 m_degrees;
		////length of this straightaway in miles.
		private double m_length;
		////obsticle type of this straightaway
		private byte m_terrain;
		private byte m_ID;


		////Returns this straightaway's length in miles
		public double length
		{
			get { return (m_length); }
		}

		////Returns degrees this straightaway is facing
		public Int16 direction
		{
			get { return (m_degrees); }
		}
		public byte id
		{
			get { return (m_ID); }
		}
		public byte terrain
		{
			get { return (m_terrain); }
		}

		public Straightaway(Int16 degrees, double length, byte terrain, byte id)
		{
			m_degrees = degrees;
			m_length = length;
			m_terrain = terrain;
			m_ID = id;
		}
	}
}
