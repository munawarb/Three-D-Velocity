/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.DirectSound;
using SharpDX.XAudio2;
using BPCSharedComponent.Security;


namespace BPCSharedComponent.ExtendedAudio
{
	public class DSound
	{
		private static String rootDir;
		private static Object playLock;
		private static string pass;
		private static bool m_isFromResource;
		public static bool isFromResource
		{
			get { return m_isFromResource; }
		}


		public static string SFileName;
		//used to store all sounds for cleanup
		public static ArrayList Sounds = new ArrayList();
		//main soundcard object
		private static XAudio2 mainSoundDevice;
		private static MasteringVoice mainMasteringVoice;
		private static SoundListener3D DSBListener = null;
		private static XAudio2 musicDevice;
		private static MasteringVoice musicMasteringVoice;
		public static float masterMusicVolume;
		//the listener used for 3d sound
		//used to hold sounds path
		public static string SoundPath;
		//used to hold narratives
		public static string NSoundPath;
		//used to hold numbers
		public static string NumPath;

		/// <summary>
		/// Initializes DirectSound for playback.
		/// </summary>
		/// <param name="WinHandle">A pointer to the main form of this program.</param>
		/// <param name="root">The root directory of the sounds.</param>
		public static void initialize(IntPtr WinHandle, String root)
		{
			playLock = new object();
			setRootDirectory(root);
			SoundPath = "s";
			NSoundPath = SoundPath + "\\n";
			NumPath = NSoundPath + "\\ns";
			mainSoundDevice = new XAudio2();
			mainMasteringVoice = new MasteringVoice(mainSoundDevice);
			musicDevice = new XAudio2();
			musicMasteringVoice = new MasteringVoice(musicMasteringVoice);
			//get the listener:
			setListener();
		}

		/// <summary>
		/// Loads a wave file into a SourceVoice.
		/// </summary>
		/// <param name="FileName">The path of the file to load.</param>
		/// <returns>A populated SourceVoice.</returns>
		public static SourceVoice LoadSound(string FileName)
		{
			if (!File.Exists(FileName)) {
				throw (new ArgumentException("The sound " + FileName + " could not be found."));
			}
			SoundStream stream = new SoundStream(File.OpenRead(FileName));
			WaveFormat format = stream.Format; // So we don't lose reference to it when we close the stream.
			AudioBuffer buffer = new AudioBuffer { Stream = stream.ToDataStream(), AudioBytes = (int)stream.Length, Flags = SharpDX.XAudio2.BufferFlags.EndOfStream };
			// We can now safely close the stream.
			stream.Close();
			SourceVoice sv = new SourceVoice(mainSoundDevice, format, true);
			sv.SubmitSourceBuffer(buffer, null); // We don't have WMA data.
			return sv;
		}
		public static Vector3 Get3DVector(double X, double Y, double Z)
		{
			return (new Vector3((float)X, (float)Y, (float)Z));
		}

		public static void setListener(double X1, double Y1, double Z1,
				  double X2, double Y2, double Z2)
		{
			//if this is the first time calling this method,
			//instantiate the listener,
			//else just reset its position
			if (DSBListener == null) {
				SoundBufferDescription BufferDesc = new SoundBufferDescription();
				BufferDesc.Flags = SharpDX.DirectSound.BufferFlags.PrimaryBuffer
					| SharpDX.DirectSound.BufferFlags.Control3D;
				primaryBuffer = new PrimarySoundBuffer(objDS, BufferDesc);
				//Finally, instantiate the listener using the PrimaryBuffer object
				DSBListener = new SoundListener3D(primaryBuffer);
				DSBListener.RolloffFactor = 1.0f; //Apply rolloff
												  //according to realism.
				DSBListener.DistanceFactor = 0.3048f;
			}
			//To set orientation, a listener3DOrientation object must be passed which contains values for front.x,y,z, Etc.
			DSBListener.Position = Get3DVector(0.0, 0.0, 0.0);
			setOrientation(DSBListener,
				X1, Y1, Z1, X2, Y2, Z2);
		}

		public static void setListener()
		{
			setListener(0, 0, 1, 0, 1, 0);
		}

		public static void setOrientation(SoundListener3D l,
				  double x1, double y1, double z1, double x2, double y2, double z2)
		{
			Vector3 front = new Vector3((float)x1, (float)y1, (float)z1);
			Vector3 top = new Vector3((float)x2, (float)y2, (float)z2);
			if (l == null) {
				DSBListener.FrontOrientation = front;
				DSBListener.TopOrientation = top;
			} else {
				l.FrontOrientation = front;
				l.TopOrientation = top;
			}
		}

		//sets orientation on the default listener
		public static void setOrientation(double x1, double y1, double z1,
			double x2, double y2, double z2)
		{
			setOrientation(null,
				x1, y1, z1,
				x2, y2, z2);
		}
		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		public static SecondarySoundBuffer LoadSound3d(string FileName, double MDistance, double MaxDistance, bool useFull3DEmulation)
		{
			if (isFromResource)
				FileName = FileName.Split('.')[0];
			SoundBufferDescription BufferDesc = new SoundBufferDescription();
			if (!File.Exists(FileName)) {
				throw new ArgumentException("The sound " + FileName + " could not be found.");
			}


			BufferDesc.Flags = SharpDX.DirectSound.BufferFlags.Control3D
						 | SharpDX.DirectSound.BufferFlags.ControlVolume
						 | SharpDX.DirectSound.BufferFlags.ControlFrequency
						 //| SharpDX.DirectSound.BufferFlags.StickyFocus
						 | SharpDX.DirectSound.BufferFlags.Mute3DAtMaxDistance;
			// if (useFull3DEmulation)
			//BufferDesc.AlgorithmFor3D = DirectSound3DAlgorithmGuid.FullHrt3DAlgorithm;
			SecondarySoundBuffer theBuffer = null;
			if (!isFromResource) {
				AudioFile wFile = new AudioFile(new FileStream(FileName, FileMode.Open));
				byte[] final = wFile.getRawWaveData();
				BufferDesc.Format = wFile.format();
				BufferDesc.BufferBytes = final.Length;
				theBuffer = new SecondarySoundBuffer(objDS, BufferDesc);
				theBuffer.Write(final, 0, LockFlags.EntireBuffer);
				wFile.close();
			} else {
				byte[] data = Encrypter.getData(FileName, pass);
				AudioFile wFile = new AudioFile(data);
				byte[] final = wFile.getRawWaveData();
				BufferDesc.Format = wFile.format();
				BufferDesc.BufferBytes = final.Length;
				theBuffer = new SecondarySoundBuffer(objDS, BufferDesc);
				theBuffer.Write(final, 0, LockFlags.EntireBuffer);
				wFile.close();
			}

			SoundBuffer3D DS3DBuffer = new SoundBuffer3D(theBuffer);
			DS3DBuffer.MinDistance = (float)MDistance;
			DS3DBuffer.MaxDistance = (float)MaxDistance;
			DS3DBuffer.Dispose();
			return (theBuffer);
		}

		public static SecondarySoundBuffer LoadSound3d(string FileName)
		{
			return (LoadSound3d(FileName, 3.0, 15.0, true));
		}

		public static SecondarySoundBuffer LoadSound3d(string FileName, bool useFull3DEmulation)
		{
			return (LoadSound3d(FileName, 3.0, 15.0, useFull3DEmulation));
		}

		public static void PlaySound(SecondarySoundBuffer Sound, bool bCloseFirst, bool bLoopSound)
		{
			//stop currently playing waves?
			if (bCloseFirst) {
				Sound.Stop();
				Sound.CurrentPosition = 0;
			}
			//loop the sound?
			if (bLoopSound) {
				Sound.Play(0, SharpDX.DirectSound.PlayFlags.Looping);
			} else {
				Sound.Play(0, SharpDX.DirectSound.PlayFlags.None);
			}
		}

		public static void PlaySound3d(SecondarySoundBuffer Sound, bool bCloseFirst, bool bLoopSound, double x, double y, double z)
		{
			lock (playLock) {
				SoundBuffer3D DS3DBuffer = new SoundBuffer3D(Sound);
				//stop currently playing waves?
				if (bCloseFirst) {
					Sound.Stop();
					Sound.CurrentPosition = 0;
				}
				//set the position
				DS3DBuffer.Position = Get3DVector(x, y, z);

				//loop the sound?
				if (bLoopSound) {
					Sound.Play(0, SharpDX.DirectSound.PlayFlags.Looping);
				} else {
					Sound.Play(0, SharpDX.DirectSound.PlayFlags.None);
				}
				DS3DBuffer.Dispose();
			} //lock
		}

		public static void SetCoordinates(double x, double y, double z)
		{
			DSBListener.Position = Get3DVector(x, y, z);
		}

		/// <summary>
		/// Loads an ogg file into memory.
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <param name="v">The starting volume.</param>
		/// <returns>An ogg buffer ready to be played.</returns>
		public static OggBuffer loadOgg(string fileName, float v)
		{
			if (isFromResource)
				fileName = fileName.Split('.')[0];
			if (!File.Exists(fileName))
				throw (new ArgumentException("The sound " + fileName + " could not be found."));


			return (new OggBuffer(fileName, v, objXA));
		}

		public static OggBuffer loadOgg(string fileName)
		{
			return (loadOgg(fileName, 1.0f));
		}

		/// <summary>
		/// Used to create a playing chain. The last files will be looped indefinitely and the files before it will only play once, in order.
		/// </summary>
		/// <param name="v">The starting volume.</param>
		/// <param name="fileNames">A list of file names to play, where the last one is looped indefinitely.</param>
		/// <returns>An ogg buffer that is ready to be played.</returns>
		public static OggBuffer loadOgg(float v, params string[] fileNames)
		{
			for (int i = 0; i < fileNames.Length; i++) {
				if (isFromResource)
					fileNames[i] = fileNames[i].Split('.')[0];
				if (!File.Exists(fileNames[i]))
					throw (new ArgumentException("The sound " + fileNames[i] + " could not be found."));
			}
			return (new OggBuffer(fileNames, v, objXA));
		}

		/// <summary>
		/// Unloads the sound from memory. The memory will be freed and the object reference will be set to NULL. The sound will also be stopped if it is playing.
		/// </summary>
		/// <param name="sound">The sound to unload.</param>
		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		public static void unloadSound(ref SecondarySoundBuffer sound)
		{
			if (sound == null || sound.IsDisposed) {
				sound = null;
				return;
			}
			sound.Stop();
			sound.Dispose();
			sound = null;
		}

		/// <summary>
		///  Checks to see if a sound is playing.
		/// </summary>
		/// <param name="s">The sound to check</param>
		/// <returns>True if the sound is playing, false if either s is NULL or is not playing.</returns>
		public static bool isPlaying(SecondarySoundBuffer s)
		{
			if (s == null || s.IsDisposed)
				return false;
			if ((s.Status & (int)BufferStatus.Playing) == (int)BufferStatus.Playing)
				return true;
			return false;
		}

		public static bool isLooping(SecondarySoundBuffer s)
		{
			if (s == null || s.IsDisposed)
				return false;
			if ((s.Status & (int)BufferStatus.Looping) == (int)BufferStatus.Looping)
				return true;
			return false;
		}

		public static void getWaveData(string filename)
		{
			FileStream s = new FileStream(filename, FileMode.Open,
				 FileAccess.Read, FileShare.Read);

		}

		/// <summary>
		///  Loads and plays the specified wave file, and disposes it after it is done playing.
		/// </summary>
		/// <param name="fn">The name of the file to play.</param>
		public static void playAndWait(String fn)
		{
			SecondarySoundBuffer s = LoadSound(fn);
			PlaySound(s, true, false);
			while (isPlaying(s))
				Thread.Sleep(100);
			s.Dispose();
			s = null;
		}

		public static void cleanUp()
		{
			DSBListener.Dispose();
			primaryBuffer.Dispose();
			objDS.Dispose();
		}

		/// <summary>
		/// Sets the root directory for sounds.
		/// </summary>
		/// <param name="root">The path of the root directory.</param>
		public static void setRootDirectory(String root)
		{
			rootDir = root;
		}

	}
}
