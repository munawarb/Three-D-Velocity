/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Speech.Synthesis;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
namespace TDV
{
	/// <summary>
	/// For this class to work as expected, a 32-bit application should define a conditional variable named x86.
	/// In addition, 32-bit applications should reference JFWAPICTRLLib, and 64-bit applications should reference FSAPILib.
	/// This can be done by adding the COM references under the "References" node in the project solution.
	/// Also, the NVDA API should exist in the same directory as the executable. 32-bit applications should use nvdaControllerClient32.dll and 64-bit applications should use nvdaControllerClient64.dll.
	/// </summary>
	public class SapiSpeech
	{
		[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Unicode)]
		static extern int nvdaController_speakText(String text);
		[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Unicode)]
		static extern int nvdaController_cancelSpeech();
		[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Unicode)]
		static extern int nvdaController_testIfRunning();


		/// <summary>
		/// Describes speaking methods. The ones that hault the program are only applicable to SAPI.
		/// </summary>
		public enum SpeakFlag
		{
			noInterrupt,
			noInterruptButStop,
			interruptable,
			interruptableButStop
		}

		/// <summary>
		/// Defines what screen reader to use.
		/// </summary>
		public enum SpeechSource
		{
			notSet,
			SAPI,
			auto
		}
		private static SpeechSynthesizer voice = null;
		private static string lastSpokenString = "";
		private static Type JAWS, winEyes;
		private static Object oJAWS, oWinEyes;
		private static SpeechSource m_source;
		public static SpeechSource source
		{
			get { return m_source; }
		}

		/// <summary>
		/// Initializes SAPI. This step is optional, but it's recommended to always have
		/// SAPI initialized at least as a fallback.
		/// The method also removes the JAWS keyboard hook.
		/// </summary>
		private static void initialize()
		{
			if (voice == null)
				voice = new SpeechSynthesizer();
		}

		/// <summary>
		/// Sets the screen reader to use.
		/// </summary>
		/// <param name="source">The speaking source, such as SpeechSource.JAWS</param>
		public static void setSource(SpeechSource source)
		{
			m_source = source;
			System.Diagnostics.Trace.WriteLine("Set speech source to " + source);
			if (source == SpeechSource.SAPI)
				initialize();
			else
			{
				initializeJAWS();
				initializeWinEyes(); //if they have both JAWS and winEyes installed, load both since
				//they could unload JAWS at any time.
			}
		}

		/// <summary>
		/// Says something through the speech source.
		/// </summary>
		/// <param name="sayString">The string to speak</param>
		/// <param name="flag">The way the string should be spoken. For example, if noInterrupt was passed, then SAPI would hault the executing thread until it was done speaking. Typically, the flag
		/// only affects SpeechSource.SAPI.</param>
		public static void speak(string sayString, SpeakFlag flag)
		{
			lastSpokenString = sayString;
			if (flag == SpeakFlag.interruptableButStop || flag == SpeakFlag.noInterruptButStop)
			{
				System.Diagnostics.Trace.WriteLine("Stopping speech...");
				if (voice != null)
					voice.SpeakAsyncCancelAll();
				//JAWS' stop speech does nothing!
				//wineyes fails silently
				stopWinEyesSpeech();
				//nvda fails silently
				nvdaController_cancelSpeech();
			} //if stop flag

			if (source == SpeechSource.auto)
			{
				if (initializeJAWS() && sayThroughJAWS(sayString, flag == SpeakFlag.interruptableButStop || flag == SpeakFlag.noInterruptButStop))
					return;
				if (initializeWinEyes() && sayThroughWinEyes(sayString))
					return;

				if (nvdaController_speakText(sayString) == 0)
					return;
			}

			// if we got down here, none of the other screen readers are loaded or source is SAPI
			initialize();
			if (flag == SpeakFlag.interruptable || flag == SpeakFlag.interruptableButStop)
				voice.SpeakAsync(sayString);
			if (flag == SpeakFlag.noInterrupt || flag == SpeakFlag.noInterruptButStop)
				voice.Speak(sayString);
		}

		/// <summary>
		/// Gets the last spoken string.
		/// </summary>
		/// <returns>The last string to be spoken by the speech source</returns>
		public static string getRepeat()
		{
			return lastSpokenString;
		}

		/// <summary>
		/// Applicable to SpeechSource.SAPI only.
		/// </summary>
		/// <returns>True if the TTS is speaking, false otherwise</returns>
		public static bool isSpeaking()
		{
			return voice != null && voice.State == SynthesizerState.Speaking;
		}

		/// <summary>
		/// Stops speech.
		/// </summary>
		public static void purge()
		{
			try
			{
				if (voice != null)
					voice.SpeakAsyncCancelAll();
				else if (JAWS != null)
					stopJAWSSpeech();
				else //nvda fails silently
					nvdaController_cancelSpeech();
			}
			catch (System.OperationCanceledException)
			{
			}
		}


		/// <summary>
		/// This method should be run just before the program shuts down. It will reinstate the JAWS keyboard hook.
		/// </summary>
		public static void cleanUp()
		{
			enableJAWSHook();
		}

		/// <summary>
		/// Reinstates the JAWS keyboard hook.
		/// </summary>
		public static void enableJAWSHook()
		{
			if (JAWS == null)
				return;
			invokeJAWSMethod("Enable", new Object[] { true });
		}

		/// <summary>
		/// Disables the JAWS keyboard hook.
		/// </summary>
		public static void disableJAWSHook()
		{
			if (!initializeJAWS())
				return;
			invokeJAWSMethod("Disable");
		}

		private static bool initializeJAWS()
		{
			if (JAWS != null)
				return true;
			JAWS = Type.GetTypeFromProgID("FreedomSci.JawsApi");
			if (JAWS == null)
				return false;
			oJAWS = Activator.CreateInstance(JAWS);
			return true;
		}
		private static bool initializeWinEyes()
		{
			try
			{
				if (winEyes != null)
					return true;
				winEyes = Type.GetTypeFromProgID("GWSpeak.Speak");
				if (winEyes == null)
					return false;
				oWinEyes = Activator.CreateInstance(winEyes);
				return true;
			}
			catch
			{
				winEyes = null;
			}
			return false;
		}

		private static bool invokeJAWSMethod(String methodName, params Object[] args)
		{
			if (JAWS == null)
				return false;
			Object res = JAWS.InvokeMember(methodName, System.Reflection.BindingFlags.InvokeMethod, null, oJAWS, args);
			if (res == null)
				return false;
			return (bool)res;
		}

		private static bool invokeWinEyesMethod(String methodName, params Object[] args)
		{
			if (winEyes == null)
				return false;
			Process[] names = Process.GetProcessesByName("wineyes");
			if (names.Length == 0)
				return false;
			winEyes.InvokeMember(methodName, System.Reflection.BindingFlags.InvokeMethod, null, oWinEyes, args);
			return true;
		}

		private static bool sayThroughJAWS(String text, bool stop)
		{
			return invokeJAWSMethod("SayString", new Object[] { text, stop });
		}
		private static void stopJAWSSpeech()
		{
			invokeJAWSMethod("StopSpeech");
		}
		private static bool sayThroughWinEyes(String text)
		{
			return invokeWinEyesMethod("SpeakString", new Object[] { text });
		}
		private static void stopWinEyesSpeech()
		{
			invokeWinEyesMethod("Silence");
		}

	}
}
