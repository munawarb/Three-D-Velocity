/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Globalization;
namespace BPCSharedComponent.VectorCalculation
{
	/// <summary>
	/// Defines the position of an object relative to another.
	/// </summary>
	public struct RelativePosition : IComparable
	{
		private bool m_sapiMode;
		private bool m_isBehind;
		private bool m_isAhead;
		private bool m_noseFacing;
		private bool m_tailFacing;
		private bool m_wingFacing;
		private int m_degreeDifference;
		private int m_relativeMark;
		private int m_degrees;
		private double m_distance;
		private double m_vDistance;
		private int m_clockMark;

		public bool sapiMode
		{
			get { return m_sapiMode; }
			set { m_sapiMode = value; }
		}

		/*
		 * Returns the clock mark of this projector
		 * relative to the other projector.
		 * */
		public int clockMark
		{
			get { return m_clockMark; }
			set { m_clockMark = value; }
		}

		//Determines how many feet above or below
		//the object in question this object is.
		//If this value is negative,
		//this object is below the other object, else it is above.
		public double vDistance
		{
			get { return (m_vDistance); }
			set { m_vDistance = value; }
		}
		//Determines if this projector is behind the other projector.
		public bool isBehind
		{
			get { return (m_isBehind); }
			set { m_isBehind = value; }
		}

		//Determines if this projector is in front of the other projector.
		public bool isAhead
		{
			get { return (m_isAhead); }
			set { m_isAhead = value; }
		}

		//Determines if this projector has a frontal view of the other projector.
		public bool isNoseFacing
		{
			get { return (m_noseFacing); }
			set { m_noseFacing = value; }
		}

		//Determines if this projector is facing away from the other projector.
		public bool isTailFacing
		{
			get { return (m_tailFacing); }
			set { m_tailFacing = value; }
		}

		//Determines if this projector has its side facing to the other projector.
		public bool isWingFacing
		{
			get { return (m_wingFacing); }
			set { m_wingFacing = value; }
		}

		//Gets or sets the difference between this projector's degree mark, and the other projector's degree mark.
		//For instance, if the other projector was facing 000, and this projector was at 358 mark, this property would hold 2.
		public int degreesDifference
		{
			get { return (m_degreeDifference); }
			set { m_degreeDifference = value; }
		}

		//Holds this projector's relative markig to the other projector.
		//For instance, if this projector was at mark 30 and the other projector was facing 32, this property will return 358.
		public int relativeDegrees
		{
			get { return (m_relativeMark); }
			set { m_relativeMark = value; }
		}

		////Gets or sets the mark of this projector, such that the other projector would have to face this mark to be on a collision path with this projector.
		public int degrees
		{
			get { return (m_degrees); }
			set { m_degrees = value; }
		}

		////Gets or sets the closure of this projector to the other projector.
		public double distance
		{
			get { return (m_distance); }
			set { m_distance = value; }
		}

		/// <summary>
		/// Returns a string representation of this structure. If this position structure will be spoken through SAPI, set sapiMode to true before calling this method.
		/// </summary>
		/// <returns>A string formatted for wave playback or to be read by SAPI.</returns>
		public override string ToString()
		{
			if (!sapiMode)
			{
				string nSoundPath = "s\\n";
				double vD = Math.Round(vDistance, 1);
				string dir = nSoundPath + "\\";
				if (vD > 0.0)
					dir += "above.wav";
				else if (vD < 0.0)
					dir += "below.wav";
				else if (vD == 0.0)
					dir = "";

				String msgString = clockMark
				 + "o.wav" //clock mark
				 + "&#" + Convert.ToString(Math.Round(distance, 1),
				 CultureInfo.InvariantCulture)
				 + "&mc.wav";
				if (isNoseFacing)
					msgString += "&nov.wav";
				else if (isTailFacing)
					msgString += "&tov.wav";
				else if (isWingFacing)
					msgString += "&wov.wav";

				//otherwise this is an object, so don't specify the view

				if (!dir.Equals(""))
					msgString += "&and.wav&#"
					+ Convert.ToString(Math.Abs(vD),
					CultureInfo.InvariantCulture)
					+ "&feet.wav&"
					+ dir;
				return (msgString);
			} // if !sapiMode

			double vDS = Math.Round(vDistance, 1);
			string dirS = "";
			if (vDS > 0.0)
				dirS = "above";
			else if (vDS < 0.0)
				dirS = "below";
			else if (vDS == 0.0)
				dirS = "";

			String msgS = clockMark + " o'clock, "
			 + Convert.ToString(Math.Round(distance, 1),
			 CultureInfo.InvariantCulture)
			 + " miles closure. ";
			if (isNoseFacing)
				msgS += "Nose on, ";
			else if (isTailFacing)
				msgS += "tail on, ";
			else if (isWingFacing)
				msgS += "wing on, ";

			if (!dirS.Equals(""))
				msgS += "and " + Convert.ToString(Math.Abs(vDS),
				CultureInfo.InvariantCulture) + " feet "
				+ dirS;
			return msgS;
		}

		/// <summary>
		/// Sorts structures by distance. Both vertical and horizontal distances are accounted for.
		/// </summary>
		/// <param name="obj">The other RelativePosition object to compare with.</param>
		/// <returns>-1 if this structure is less than obj, 0 if they are equal, and 1 if obj is greater than this structure.</returns>
		public int CompareTo(object obj)
		{
			RelativePosition r = (RelativePosition)obj;
			double instanceDistance = distance + vDistance;
			double objDistance = r.distance + r.vDistance;
			if (instanceDistance > objDistance)
			{
				return 1;
			}
			if (instanceDistance < objDistance)
			{
				return -1;
			}
			return 0;
		}
	}
}
