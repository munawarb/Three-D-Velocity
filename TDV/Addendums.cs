/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace TDV.Addendums
{
	public class File
	{
		public static String appPath
		{
			get
			{
				return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\data";
			}
		}
		public static String commonAppPath
		{
			get
			{
				return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\data";
			}
		}
		/// <summary>
		/// The following property returns the version of the running executable.
		/// </summary>
		public static string FileVersion
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}
		}
	} //class

	public class System
	{
		/// <summary>
		/// <summary>
		/// The following method pauses the program for the specified number of milliseconds.
		/// </summary>
		/// <param name="TimeToWait">Time in ms</param>
		public static void pause(long TimeToWait)
		{
			long M1 = Environment.TickCount;
			while (((Environment.TickCount - M1) < TimeToWait))
			{
				Application.DoEvents();
			}
		}
	}
}
