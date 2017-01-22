/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;

namespace TDV
{
	public class Track : Area
	{
		private Straightaway[] m_track;

/// <summary>
/// Constructs a new Track with the given filename.
/// </summary>
/// <param name="fileName">The file name from which to construct the Track</param>
		public Track(string fileName)
		{
			byte trackLength = 0;
			if (Options.mode != Options.Modes.racing)
			{
				trackLength = 1;
				name = "generic area";
				m_track = (Straightaway[])(Array.CreateInstance(typeof(Straightaway), 1));
				setStraightaway(0, new Straightaway(0, 0, 0, 0));
			}
			else
			{
				//track file exists, which means a racing track has been selected.
				BinaryReader s = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
				name = s.ReadString();
				////track name
				trackLength = s.ReadByte();
				////Number of straightaways in the track
				m_track = (Straightaway[])(Array.CreateInstance(typeof(Straightaway), trackLength));
				////resize the straightaway array so that
				////we can fill in straightaways and start building the track...finally!
				byte counter = 0;
				for (counter = 1; counter <= trackLength; counter++)
				{
					setStraightaway(counter - 1, new Straightaway(s.ReadInt16(), s.ReadDouble(), s.ReadByte(), (byte)(counter - 1)));
				}
				s.Close();
			}
		}


		////--------
		////Property to return length of track in miles
		public double length
		{
			get
			{
				byte i = 0;
				double l = 0;
				for (i = 0; i <= m_track.Length - 1; i++)
				{
					l += getStraightaway(i).length;
				}
				return (l);
			}
		}
		private byte totalStraightaways
		{
			get { return (((byte)m_track.Length)); }
		}
		private byte maxStraightawayPosition
		{
			get { return (byte)(totalStraightaways - 1); }
		}

		////Returns true if the Straightaway supplied is the end of the track,
		////false otherwise.
		public bool isEnd(Straightaway s)
		{
			return (s.id >= maxStraightawayPosition);
		}
		public Straightaway getStraightaway(int i)
		{
			return (m_track[i]);
		}
		public void setStraightaway(int i, Straightaway s)
		{
			m_track[i] = s;
		}
	}
}
