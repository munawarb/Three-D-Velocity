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
using BPCSharedComponent.Security;


namespace BPCSharedComponent.ExtendedAudio
{
	public class DSound
	{
		private static String rootDir;
		private static Object playLock;
		private static int m_maxMusicVol;
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
		public static DirectSound objDS = null;
		private static SoundListener3D DSBListener = null;
		private static PrimarySoundBuffer primaryBuffer;
		public static int masterMusicVolume;
		//the listener used for 3d sound
		//used to hold sounds path
		public static string SoundPath;
		//used to hold narratives
		public static string NSoundPath;
		//used to hold numbers
		public static string NumPath;

		/// <summary>
		/// Gets the maximum volume of background music.
		/// </summary>
		public static int maxMusicVol
		{
			get { return (m_maxMusicVol); }
			set { m_maxMusicVol = value; }
		}

		/// <summary>
		/// Initializes DirectSound for playback.
		/// </summary>
		/// <param name="WinHandle">A pointer to the main form of this program.</param>
		/// <param name="root">The root directory of the sounds.</param>
		public static void initialize(IntPtr WinHandle, String root)
		{
			playLock = new object();
			maxMusicVol = 0;
			setRootDirectory(root);
			SoundPath = "s";
			NSoundPath = SoundPath + "\\n";
			NumPath = NSoundPath + "\\ns";
			objDS = new DirectSound();
			//if this object is destroyed, all sounds created with it will also be flushed.
			objDS.SetCooperativeLevel(WinHandle, CooperativeLevel.Priority);
			//WinHandle must be passed from the main form
			//            SpeakerConfiguration sConfig;
			//SpeakerGeometry sGeo;
			//objDS.GetSpeakerConfiguration(sConfig, sGeo);
			//objDS.SetSpeakerConfiguration(sConfig, SpeakerGeometry.None);
			//get the listener:
			setListener();
		}

		/// <summary>
		/// Assumes the sounds that will be loaded are encrypted.
		/// </summary>
		/// <param name="handle">The form handle.</param>
		/// <param name="m">True if sounds are encrypted, false otherwise.</param>
		/// <param name="root">The root directory of the sounds.</param>
		public static void initialize(IntPtr handle, bool m, String root)
		{
			initialize(handle, root);
			pass = "TDV123";
			m_isFromResource = m;
		}

		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		public static SecondarySoundBuffer LoadSound(string FileName)
		{
			if (isFromResource)
				FileName = FileName.Split('.')[0];
			if (!File.Exists(FileName)) {
				throw (new ArgumentException("The sound " + FileName + " could not be found."));
			}
			SoundBufferDescription BufferDesc = new SoundBufferDescription();
			//enable volume changes on all buffers created with this function.
			BufferDesc.Flags = SharpDX.DirectSound.BufferFlags.ControlVolume
				| SharpDX.DirectSound.BufferFlags.ControlFrequency
				//| SharpDX.DirectSound.BufferFlags.StickyFocus
				| SharpDX.DirectSound.BufferFlags.ControlPan;
			//load wave file into DirectSound buffer
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
			return (theBuffer);
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

		////Main playOgg function
		////Base method for most overloaded methods.
		public static OggBuffer loadOgg(string fileName, int v)
		{
			if (isFromResource)
				fileName = fileName.Split('.')[0];
			if (!File.Exists(fileName))
				throw (new ArgumentException("The sound " + fileName + " could not be found."));


			return (new OggBuffer(fileName, v, objDS));
		}

		//sets filename, but leaves other parameters
		//as defaults
		public static OggBuffer loadOgg(string fileName)
		{
			return (loadOgg(fileName, maxMusicVol));
		}

		//Used to play consecutive buffers.
		//Expects balance, volume, and list of files to play
		public static OggBuffer loadOgg(int v, params string[] fileNames)
		{
			for (int i = 0; i < fileNames.Length; i++) {
				if (isFromResource)
					fileNames[i] = fileNames[i].Split('.')[0];
				if (!File.Exists(fileNames[i]))
					throw (new ArgumentException("The sound " + fileNames[i] + " could not be found."));
			}
			return (new OggBuffer(fileNames, v, objDS));
		}

		/// <summary>
		/// Unloads the sound from memory. The memory will be freed and the object reference will be set to NULL. The sound will also be stopped if it is playing.
		/// </summary>
		/// <param name="sound">The sound to unload.</param>
		[MethodImplAttribute(MethodImplOptions.Synchronized)]
		public static void unloadSound(ref SecondarySoundBuffer sound)
		{
			System.Diagnostics.Trace.WriteLine("In unloadSound, sound is " + ((sound == null) ? "null" : " not nulll"));
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
