/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Text;
using System.Windows.Forms;
using BPCSharedComponent.ExtendedAudio;
using Microsoft.VisualBasic;
using System.Threading;
using BPCSharedComponent.ExtendedAudio;
using System.Collections.Generic;

namespace TDV
{

	public class SelfVoice
	{
		private static String[] digits = new string[] { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
		private static String[] tens = new string[] { "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
		private static String[] thousands = new string[] { "thousand", "million", "billion" };
		private static int globalCounter;
		private static string tempPath;
		private static bool usingTempPath;
		public static bool nStop;
		public static long CurrentFreq = 44100;
		private static string gSFilename;
		private static bool gNWait;
		private static float gNumber;
		private static Thread numThread = null;
		private static StringBuilder files;
		private static ExtendedAudioBuffer[] soundFiles;
		private static bool filesPlaying;
		private static Object locker = new object();
		private static bool threadRunning = false;

		public static void NumWait()
		{
			while (DSound.isPlaying(soundFiles[globalCounter]))
			{
				if (nStop)
					return;
				Thread.Sleep(0);
			}
		}
		private static void stopLastFile()
		{
			if (soundFiles != null
	&& soundFiles.Length > globalCounter
	&& soundFiles[globalCounter] != null)
			{
				if (DSound.isPlaying(soundFiles[globalCounter]))
					soundFiles[globalCounter].stop();
			}
		}
		public static void VoiceNumber(float number)
		{
			NLS("#" + number);
		}
		private static void processNumber(String number)
		{
			string SM = null;
			long smn = 0;
			string[] smd = null;
			SM = number;
			smn = SM.Length;
			smd = SM.Split('.');
			SM = smd[0];
			if (SM.Contains("-"))
			{
				SM = SM.Substring(1, SM.Length - 1);
				files.Append(numPath + "-.wav&");
			}
			smn = Strings.Len(SM);
			if (smn == 1) v1(SM);
			if (smn == 2) v2(SM);
			if (smn == 3) v3(SM);
			if (smn == 4) v4(SM);
			if (smn == 5) v5(SM);
			if (smn == 6) v6(SM);
			if (smn == 7) v7(SM);
			if (smn == 8) v8(SM);
			if (smn == 9) v9(SM);
			if (smd.Length - 1 > 0)
			{
				files.Append(numPath + "point.wav&");
				VPoint(smd[1]);
			}
		}

		//starts number voicing in new thread
		public static void VoiceNumber(float n, bool newThread)
		{
			//if something is already being voiced, stop it before starting this thread.
			lock (locker)
			{
				stopThread();
				threadRunning = true;
				//Next, initialize global parameter
				gNumber = n;
				//Finally, start new thread.
				numThread = new Thread(VoiceNumber);
				numThread.Start();
			}
		}

		//method that does the numbering in new thread
		//since this method will be called in anew thread,
		//its parameters will be initialized through global variables.
		//Therefore it is assumed that
		//gNumber is set before
		//this method is called.
		private static void VoiceNumber()
		{
			VoiceNumber(gNumber);
			purge();
			threadRunning = false;
		}

		private static void v1(string STRNumber)
		{
			files.Append(numPath + STRNumber + ".wav&");
		}
		private static void v2(string STRNumber)
		{
			//for teens
			if (Conversion.Val(STRNumber) >= 1 && Conversion.Val(STRNumber) <= 20)
				files.Append(numPath + Conversion.Val(STRNumber) + ".wav&");
			else
			{
				//leading 0s are dropped out, so it's got to be over 20
				if (Strings.Mid(STRNumber, 1, 1) != "0")
					files.Append(numPath + Strings.Mid(STRNumber, 1, 1) + "0.wav&");
				if (Strings.Mid(STRNumber, 2, 1) != "0")
					files.Append(numPath + Strings.Mid(STRNumber, 2, 1) + ".wav&");
			}
		}
		private static void v3(string STRNumber)
		{
			if (Conversion.Val(Strings.Mid(STRNumber, 1, 1)) > 0)
			{
				files.Append(numPath + Strings.Mid(STRNumber, 1, 1) + ".wav&");
				files.Append(numPath + "100.wav&");
				v2(Strings.Mid(STRNumber, 2, 2));
			}
			else
			{
				v2(Strings.Mid(STRNumber, 2, 2));
			}
		}
		private static void v4(string STRNumber)
		{
			if (Conversion.Val(Strings.Mid(STRNumber, 1, 1)) >= 1)
			{
				files.Append(numPath + Strings.Mid(STRNumber, 1, 1) + ".wav&");
				files.Append(numPath + "1000.wav&");
				v3(Strings.Mid(STRNumber, 2, 3));
			}
			else
			{
				v3(Strings.Mid(STRNumber, 2, 3));
			}
		}
		private static void v5(string STRNumber)
		{
			v2(Strings.Mid(STRNumber, 1, 2));
			files.Append(numPath + "1000.wav&");
			v3(Strings.Mid(STRNumber, 3, 3));
		}
		private static void v6(string STRNumber)
		{
			v3(Strings.Mid(STRNumber, 1, 3));
			if (Strings.Mid(STRNumber, 1, 3) != "000")
				files.Append(numPath + "1000.wav&");
			v3(Strings.Mid(STRNumber, 4, 3));
		}
		private static void v7(string STRNumber)
		{
			if (Strings.Mid(STRNumber, 1, 1) != "0")
			{
				files.Append(numPath + Strings.Mid(STRNumber, 1, 1) + ".wav&");
				files.Append(numPath + "1000000.wav&");
			}
			v6(Strings.Mid(STRNumber, 2, 6));
		}
		private static void v8(string STRNumber)
		{
			v2(Strings.Mid(STRNumber, 1, 2));
			files.Append(numPath + "1000000.wav&");
			v6(Strings.Mid(STRNumber, 3, 6));
		}
		private static void v9(string STRNumber)
		{
			v3(Strings.Mid(STRNumber, 1, 3));
			files.Append(numPath + "1000000.wav&");
			v6(Strings.Mid(STRNumber, 4, 6));
		}
		private static void VPoint(string STRNum)
		{
			short i = 0;
			for (i = 1; i <= Strings.Len(STRNum); i++)
			{
				files.Append(numPath + Strings.Mid(STRNum, i, 1) + ".wav&");
			}
		}

		public static void NLS(string sFilename, bool NWait, long TheRate)
		{
			try
			{
				stopLastFile();
				filesPlaying = true;
				files = new StringBuilder();
				String[] SFiles = sFilename.Split('&');
				for (int i = 0; i < SFiles.Length; i++)
				{
					sFilename = SFiles[i];
					//if # is in front of array element author wants that element to be num voiced,
					//and it is not a .wav file.
					if (sFilename.Contains("#"))
						processNumber(
							sFilename.Substring(
							sFilename.IndexOf("#") + 1
							));
					else
					{
						//if reg .wav file
						sFilename = processFileName(sFilename);
						files.Append(sFilename);
						if (i != SFiles.Length - 1)
							files.Append("&");
					} //if not number
				} //loop

				//Now we have all the files to play.
				String[] finalFiles = files.ToString().Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
				soundFiles = new ExtendedAudioBuffer[finalFiles.Length];
				bool firstLoop = true;
				for (globalCounter = 0; globalCounter < soundFiles.Length; globalCounter++)
				{
					if (firstLoop)
						soundFiles[globalCounter] = DSound.LoadSoundAlwaysLoud(finalFiles[globalCounter]);
					DSound.PlaySound(soundFiles[globalCounter], true, false);
					if (globalCounter < finalFiles.Length - 1)
						soundFiles[globalCounter + 1] = DSound.LoadSoundAlwaysLoud(finalFiles[globalCounter + 1]);
					firstLoop = false;
					//Next, if only playing one file, return since we're probably in a menu.
					if (soundFiles.Length != 1 || NWait)
						NumWait();

					if (nStop)
					{
						filesPlaying = false;
						return;
					}
				}

				//next, global counter may be == length
				//of array since we completed the loop
				//so back it up to stop properly.
				if (globalCounter == soundFiles.Length)
					globalCounter--;
				filesPlaying = false;
			}
			catch (Exception e)
			{
				Common.handleError(e);
			}
		}

		////Specifies filename, defaults NWait to true, and rate to 0--file rate
		public static void NLS(string sFilename)
		{
			NLS(sFilename, true, 0);
		}

		////Specifies filename, NWait, and efaults rate to 0--file rate
		public static void NLS(string sFilename, bool NWait)
		{
			NLS(sFilename, NWait, 0);
		}

		//plays self voice in new thread.
		public static void NLS(string sFilename, bool NWait, bool nThread, bool interrupt)
		{
			//first check to see if a thread is already running.
			//if it is, wait till it stops by setting the interrupt flag.
			lock (locker)
			{
				if (interrupt)
					stopThread();
				else
				{
					if (isThreadRunning())
						return;
				}

				threadRunning = true;
				//first initialize global parameters for
				//nls thread method.
				gSFilename = sFilename;
				gNWait = NWait;

				//now start new thread,
				//and nls()will read parameters from global values.
				numThread = new Thread(NLS);
				numThread.Start();
			}
		}

		public static void NLS(string sFilename, bool NWait, bool nThread)
		{
			NLS(sFilename, NWait, nThread, true);
		}


		//since this method will be called in anew thread,
		//its parameters will be initialized through global variables.
		//Therefore it is assumed that
		//gSFilename and gNWait are set before
		//this method is called.
		private static void NLS()
		{
			NLS(gSFilename, gNWait);
			purge();
			threadRunning = false;
		}

		public static void purge(bool waitForThread)
		{
			nStop = true; //let a new sound play properly without having to reset NStop to false.
			if (waitForThread)
				stopThread();
			while (filesPlaying)
				Thread.Sleep(5);
			if (!waitForThread)
			{ //stopthread() waits for thread to stop, so purge is already called and resources are cleaned up.
				stopLastFile();
				cleanUp(ref soundFiles);
			}
			usingTempPath = false;
			//have to wait for numThread to stop before resetting
			//nstop
			nStop = false;
		}

		public static void purge()
		{
			purge(false);
		}

		private static string numPath
		{
			get
			{
				if (usingTempPath)
					return (DSound.NumPath + "\\" + tempPath);
				return (DSound.NumPath + "\\");
			}
		}

		public static bool setPathTo(string p, bool interrupt)
		{
			//First wait for current thread to exit.
			//that way we don't inadvertently change its path
			//in the middle of its execution!
			if (!interrupt && isThreadRunning())
				return false;
			purge(true);
			usingTempPath = true;
			tempPath = p;
			return true;
		}

		public static bool setPathTo(string p)
		{
			return setPathTo(p, true);
		}


		public static void resetPath()
		{
			purge(true);
			usingTempPath = false;
			tempPath = null;
		}

		private static void stopThread()
		{
			//guarantee: when thread stops, numThread==null.
			/*
			if (numThread != null) {
				nStop = true;
				while (numThread != null)
					Thread.Sleep(5);
				//nStop will be reset by the thread's termination call to purge()
			} else
			 * */
			nStop = true;
			while (threadRunning)
				Thread.Sleep(5);
			nStop = false; //let another soundplay
		}

		private static String processFileName(String sFileName)
		{
			if (sFileName.IndexOf("\\") != -1)
				return (sFileName); //already chose path
			else
			{
				String newPath = "";
				newPath = DSound.NSoundPath + "\\";
				newPath += sFileName;
				return (newPath);
			}
		}

		//returns true if a sound is playing in a thread,
		//false otherwise
		public static bool isThreadRunning()
		{
			return threadRunning;
		}

		private static void cleanUp(ref ExtendedAudioBuffer[] sounds)
		{
			if (sounds == null)
				return;
			for (int i = 0; i < sounds.Length; i++)
				DSound.unloadSound(ref sounds[i]);
			sounds = null;
		}

		private static String getWordDigit(String number)
		{
			if (number.Equals("0"))
				return "zero";
			return digits[int.Parse(number) - 1];
		}

		private static String getWordTens(String number)
		{
			return tens[int.Parse(number) - 2];
		}

		private static String getThousand(int n)
		{
			return thousands[n - 1];
		}

		private static String getTripple(String number)
		{
			if (number.Equals("0"))
				return "zero";
			String res = "";
			int n = number.Length;
			String d = "";
			if (n == 3) {
				d = number.Substring(0, 1);
				if (d != "0") {
					d = getWordDigit(d);
					res += d + " hundred";
				}
			}
			if (n >= 2) {
				d = number.Substring(n - 2, 1);
				if (d != "0") {
					if (d != "1")
						d = getWordTens(d);
					else
						d = getWordDigit(number.Substring(n - 2, 2));
					if (!res.Equals(""))
						res += " ";
					res += d;
				}
			}
			if (n == 1 || number.Substring(n - 2, 1) != "1") { // Make sure the tens column isn't a teen, otherwise we'll get numbers like 19 9 or 10 0
				d = number.Substring(n - 1, 1);
				if (d != "0") {
					d = getWordDigit(d);
					if (!res.Equals(""))
						res += " ";
					res += d;
				}
			}
			return res;
		}

		public static String convertToWords(String value)
		{
			int thePoint = value.IndexOf(".");
			String number = (thePoint > -1)?value.Substring(0, thePoint):value;
			String fraction = (thePoint > -1) ? value.Substring(thePoint + 1) : "";
			int triplets = number.Length / 3;
			int mostSig = number.Length % 3;
			String res = "";
			String triplet = "";
			for (int i = 0; i < triplets; i++) {
				triplet = number.Substring(number.Length - 3 * i - 3, 3);
				triplet = getTripple(triplet);
				if (i > 0) {
					triplet += " " + getThousand(i);
				}
				if (!res.Equals(""))
					res = " " + res;
				res = triplet + res;
			}
			if (mostSig > 0) {
				triplet = number.Substring(0, mostSig);
				triplet = getTripple(triplet);
				if (triplets > 0) {
					triplet += " " + getThousand(triplets);
				}
				if (!res.Equals(""))
					res = " " + res;
				res = triplet + res;
			}
			if (thePoint > -1) {
				if (!res.Equals(""))
					res += " ";
				res += "point";
				for (int i = 0, n = fraction.Length; i < n; i++)
					res += " " + getWordDigit(fraction[i].ToString());
			}
			return res;
		}

	}
}
