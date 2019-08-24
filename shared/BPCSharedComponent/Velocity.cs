/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
namespace BPCSharedComponent.VectorCalculation
{
	/// <summary>
	/// Describes a velocity vector.
	/// </summary>
	public struct Velocity
	{
		//Speed per unit time
		private float m_speed;
		private int m_course;
		public float speed
		{
			get { return (m_speed); }
			set { m_speed = value; }
		}

		//Direction of the velocity vector
		public int direction
		{
			get { return (m_course); }
			set { m_course = value; }
		}

		public Velocity(int d, float s)
		{
			m_course = d;
			m_speed = s;
			direction = d;
			speed = s;
		}


	}
}
