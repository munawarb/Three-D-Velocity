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
	public abstract class Area
	{
		public enum MapType : byte
		{
			mission = 1,
			track
		}
		private float m_runwayLength;
		private string m_name;
		private Int16 m_harddeck = 10000;
		public Int16 harddeck
		{
			get { return (m_harddeck); }
			set { m_harddeck = value; }
		}
		public string name
		{
			get { return (m_name); }
			set { m_name = value; }
		}
		public float runwayLength
		{
			get { return (m_runwayLength); }
			set { m_runwayLength = value; }
		}
		public Area()
		{
			name = null;
		}
		public static string getName(string fileName)
		{
			BinaryReader s = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
			string tName = s.ReadString();
			s.Close();
			return (tName);
		}

		public static void buildMap(string filename, ref Track t)
		{
			BinaryReader s = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read));
			s.ReadString();
			MapType type = (MapType)s.ReadByte();
			t = buildTrack(ref filename);
			s.Close();
		}

		private static Track buildTrack(ref string filename)
		{
			return (new Track(filename));
		}

	}
}
