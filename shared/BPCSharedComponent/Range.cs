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
	/// Encapsulates a horizontal and vertical component--these can be used to define a locking range, or to obtain a component vector.
	/// </summary>
	public struct Range
	{
		private double m_h;
		private double m_v;

		//The x component of a vector, typically obtained by taking the cosine of the vector.
		public double horizontalDistance
		{
			get { return (m_h); }
			set { m_h = value; }
		}

		//The y component of a vector, typically obtained by taking the sine of the vector.
		public double verticalDistance
		{
			get { return (m_v); }
			set { m_v = value; }
		}


		public Range(double h, double v)
		{
			m_h = h;
			m_v = v;
		}
	}
}
