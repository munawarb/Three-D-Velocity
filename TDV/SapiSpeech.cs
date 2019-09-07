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
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.Input;
using System.Threading;

namespace TDV
{
	/// <summary>
	/// Output speech using a common screen reader or SAPI. Both 32-bit and 64-bit environments are supported with respect to the NVDA screen-reader.
	/// </summary>
	public class SapiSpeech
	{
		// For the 32-bit versions, we'll keep the function names the same as in the headers.
		[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Unicode)]
		static extern int nvdaController_speakText(String text);
		[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Unicode)]
		static extern int nvdaController_cancelSpeech();
		[DllImport("nvdaControllerClient32.dll", CharSet = CharSet.Unicode)]
		static extern int nvdaController_testIfRunning();

		// For the 64-bit counterparts, we'll use the nvdaController64 prefix.
		[DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_speakText", CharSet = CharSet.Unicode)]
		static extern int nvdaController64_speakText(String text);
		[DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_cancelSpeech", CharSet = CharSet.Unicode)]
		static extern int nvdaController64_cancelSpeech();
		[DllImport("nvdaControllerClient64.dll", EntryPoint = "nvdaController_testIfRunning", CharSet = CharSet.Unicode)]
		static extern int nvdaController64_testIfRunning();


		/// <summary>
		/// Describes speaking methods.
		/// </summary>
		public enum SpeakFlag
		{
			/// <summary>
			/// Start the message and immediately return.
			/// </summary>
			none,
			/// <summary>
			/// The thread blocks until the message finishes speaking.
			/// </summary>
			noInterrupt,
			/// <summary>
			/// The thread blocks until the message finishes speaking, and the currently speaking message, if any, will be flushed.
			/// </summary>
			noInterruptButStop,
			/// <summary>
			/// The thread blocks until a key is pressed or the message finishes speaking.
			/// </summary>
			interruptable,
			/// <summary>
			/// The thread blocks until a key is pressed or the message finishes speaking, and the currently speaking message, if any, will be flushed.
			/// </summary>
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
		private static Stopwatch watch = new Stopwatch();
		public static float screenReaderRate = 0f; // How many ms to speak one word.
		private static long timeRequiredToSpeak = 0; // How many ms will be required to speak the current string of text?
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
			else {
				initializeJAWS();
				initializeWinEyes(); //if they have both JAWS and winEyes installed, load both since
																									//they could unload JAWS at any time.
			}
		}

		public static void speak(String sayString)
		{
			speak(sayString, SpeakFlag.none);
		}

		/// <summary>
		/// Expands a string so that numbers are written out in their word equivalents. This function is used to give the API an idea of how many words are in a number in the given string so that speech delays can be properly assessed. This function does not insert hyphens between number words, since we use white space to count the number of words in a string. So fifty-five is returned as fifty five.
		/// </summary>
		/// <param name="text">The string to process.</param>
		/// <returns>The expanded string. For example, 'there are 55 lines to go' would be expanded to 'there are fifty five lines to go'</returns>
		private static String processText(String text)
		{
			String res = "";
			String[] splits = text.Split(' ');
			String s = "";
			float f = 0f;
			for (int i = 0, n = splits.Length; i < n; i++) {
				s = splits[i];
				if (float.TryParse(s, out f))
					s = SelfVoice.convertToWords(s);
				res += s + " ";
			}
			return res;
		}

		/// <summary>
		/// Says something through the speech source.
		/// </summary>
		/// <param name="sayString">The string to speak</param>
		/// <param name="flag">The way the string should be spoken. For example, if noInterrupt is passed, then the thread is halted until the message is finished speaking. See the flags enum for a description of each flag.</param>
		public static void speak(string sayString, SpeakFlag flag)
		{
			if (flag == SpeakFlag.interruptableButStop || flag == SpeakFlag.noInterruptButStop) {
				purge();
			}
			sayString = processText(sayString);
			lastSpokenString = sayString;
			if (source == SpeechSource.auto) {
				if (initializeJAWS() && sayThroughJAWS(sayString, flag == SpeakFlag.interruptableButStop || flag == SpeakFlag.noInterruptButStop)) {
					startSpeakTimerOn(sayString);
				} else if (((Environment.Is64BitProcess) ? nvdaController64_speakText(sayString) : nvdaController_speakText(sayString)) == 0) {
					startSpeakTimerOn(sayString);
				}
			} else {
				// if we got down here, none of the other screen readers are loaded or source is SAPI
				initialize();
				voice.SpeakAsync(sayString);
				// Wait for SAPI to actually begin speaking, otherwise we'll get a race condition where a query to isSpeaking will return false if
				// SAPI takes a little while to start speaking the string.
				while (!isSpeaking())
					Thread.Sleep(0);
			}
			if (flag == SpeakFlag.none)
				return;
			if (flag == SpeakFlag.interruptable || flag == SpeakFlag.interruptableButStop) {
				while (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown()) { // So the loop doesn't break if we hold ENTER for too long.
					if (!isSpeaking())
						break;
					Thread.Sleep(0);
				}
			}
			while (isSpeaking()) {
				if (flag == SpeakFlag.interruptable || flag == SpeakFlag.interruptableButStop) {
					if (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown())
						break;
				}
				Thread.Sleep(10);
			}
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
			if (watch.IsRunning)
				return watch.ElapsedMilliseconds < timeRequiredToSpeak;
			return voice != null && voice.State == SynthesizerState.Speaking;
		}

		/// <summary>
		/// Stops speech. This method guarantees that when it returns, both SAPI and the screen reader will be stopped. We achieve this by blocking on the asyncCancel function for SAPI.
		/// </summary>
		public static void purge()
		{
			watch.Reset();
			timeRequiredToSpeak = 0;
			if (voice != null) {
				try {
					voice.SpeakAsyncCancelAll();
				} catch (System.OperationCanceledException) {
				}
				// Block here so we don't get a race condition while canceling SAPI.
				while (isSpeaking())
					Thread.Sleep(0);
			}
			if (JAWS != null) {
				stopJAWSSpeech();
			}
			// NVDA fails silently so we can safely make these calls even with a system that has no NVDA installed.
			if (Environment.Is64BitProcess)
				nvdaController64_cancelSpeech();
			else
				nvdaController_cancelSpeech();
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
			try {
				if (winEyes != null)
					return true;
				winEyes = Type.GetTypeFromProgID("GWSpeak.Speak");
				if (winEyes == null)
					return false;
				oWinEyes = Activator.CreateInstance(winEyes);
				return true;
			} catch {
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

		public static void playOrSpeakMenu(String soundFile, String text)
		{
			Common.executeSvOrSr(() => DSound.playAndWait(soundFile), () => speak(text, SpeakFlag.noInterrupt), Options.menuVoiceMode);
		}

		public static void playOrSpeakStatus(String soundFile, String text)
		{
			Common.executeSvOrSr(() => DSound.playAndWait(soundFile), () => speak(text, SpeakFlag.noInterrupt), Options.statusVoiceMode);
		}

		private static void startSpeakTimerOn(String text)
		{
			timeRequiredToSpeak = (long)(text.Split(' ').Length * screenReaderRate);
			watch.Reset();
			watch.Start();
		}

	}
}
