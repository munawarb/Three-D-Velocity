/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections;
using System.Collections.Generic;
using SharpDX.Multimedia;
using SharpDX.DirectSound;
using SharpDX.XAudio2;
using OggVorbisDecoder;
using System.Threading;
using System.IO;
using NVorbis;
namespace BPCSharedComponent.ExtendedAudio
{
	/// <summary>
	/// Buffers and plays Ogg Vorbis data.
	/// </summary>
	public class OggBuffer
	{
		private bool preparing;
		private delegate void stopEventDelegate();
		private OggVorbisFileStream oggFile = null;
		private Object lockObject;
		private const short bitsPerSample = 16;
		private XAudio2 device;
		private int playPointer = 0; //keeps track of which file is to be played next in a list of files or streams
		private String[] fileNames = null;
		private int m_volume;
		private Boolean stopNow, loop;
		private List<SecondarySoundBuffer> soundBuffers;
		private AutoResetEvent stoppedSignal;
		public int volume
		{
			get { return (m_volume); }
			set { setVolume(value); }
		}

		public OggBuffer(String[] fileNames, int volume, DirectSound device)
		{
			lockObject = new object();
			this.fileNames = fileNames;
			soundBuffers = new List<SecondarySoundBuffer>();
			m_volume = volume;
			this.device = device;
		}

		public OggBuffer(String fileName, int volume, DirectSound device) : this(new string[] { fileName }, volume, device)
		{

		}
		public void setVolume(int v)
		{
			// DirectSound buffers have max volumes of 0 and min volumes of -10000.
			if (v > 0)
				v = 0;
			else if (v < -10000)
				v = -10000;
			lock (lockObject) {
				if (soundBuffers.Count > 0 && soundBuffers[playPointer] != null)
					soundBuffers[playPointer].Volume = v;
			}
			m_volume = v;
		}

		public void stopOgg()
		{
			lock (lockObject) {
				if (stopNow)
					return; // Expected behavior: multiple calls should have no affect.
				stopNow = true;
				stoppedSignal.Set();
				preparing = false;
				if (soundBuffers.Count > 0 && soundBuffers[playPointer] != null)
					soundBuffers[playPointer].Stop();
				freeResources();
			}
		}
		public bool isPlaying()
		{
			lock (lockObject) {
				if (stopNow)
					return false;
				if (preparing)
					return false;
				if (soundBuffers.Count < 1)
					return false;
				return DSound.isPlaying(soundBuffers[playPointer]) || DSound.isLooping(soundBuffers[playPointer]);
			}
		}

		public void play(bool loop)
		{
			this.loop = loop;
			Thread thread = null;
			thread = new Thread(process);
			thread.Start();
			while (!isPlaying())
				Thread.Sleep(5);
		}
		public void play()
		{
			play(false);
		}


		//Will be called by ogg player thread when a file is done playing.
		//This event sets the play flag to false.
		//Only then can the program assume the file is done playing,
		//since this flag is unset last and it means
		//all resources have been cleaned up and nothing is lost.
		public void stopEventHandler()
		{
			stoppedSignal.WaitOne();
			if (!stopNow) {
				if (playPointer < fileNames.Length - 1) {
					SecondarySoundBuffer s = soundBuffers[playPointer];
					DSound.unloadSound(ref s);
					playPointer++;
					play(loop);
				}
				if (!loop) // If the file is explicitly stopped, the stopOgg method will call free resources so we don't have to do it here; instead, this is the case when the file is done playing naturally, so stopOgg might never be called.
					freeResources();
			}
		}

		private void process()
		{
			process(false);
		}

		private void process(bool initializeNextTrack)
		{
			preparing = true;
			SecondarySoundBuffer sBuff = null;
			int p = playPointer;
			if (initializeNextTrack)
				p++; // Point to the next track which we will initialize.
			MemoryStream PcmStream = null;
			PlayFlags f = PlayFlags.None;
			if (p > soundBuffers.Count - 1) {
				SoundBufferDescription desc = new SoundBufferDescription();
				desc.Flags = BufferFlags.ControlPositionNotify | BufferFlags.ControlVolume | BufferFlags.GetCurrentPosition2 | BufferFlags.GlobalFocus;
				VorbisReader vorbis = new VorbisReader(fileNames[playPointer]);
				float[] outBuffer = new float[vorbis.Channels*vorbis.SampleRate/5];
				PcmStream = new MemoryStream();
				int PcmBytes = 0;
				WaveFormat waveFormat = new WaveFormat(vorbis.SampleRate, bitsPerSample, vorbis.Channels);

				// Decode the Ogg Vorbis data into its PCM data
				int rescaleFactor = 32767;
				while ((PcmBytes = vorbis.ReadSamples(outBuffer, 0, outBuffer.Length)) > 0) {
					short[] intData = new short[PcmBytes];
					byte[] data = new byte[PcmBytes* 2];
					for(int index = 0; index < PcmBytes; index++) {
						intData[index] = (short)(outBuffer[index] * rescaleFactor);
						byte[] b = BitConverter.GetBytes(intData[index]);
						b.CopyTo(data, index * 2);
					}
					PcmStream.Write(data, 0, data.Length);
				}
				desc.Format = waveFormat;
				desc.BufferBytes = (int)PcmStream.Length;
				lock (lockObject) // So we don't lose a simultaneous volume change.
					soundBuffers.Add(sBuff = new SecondarySoundBuffer(device, desc));
				PcmStream.Position = 0;
				sBuff.Write(PcmStream.ToArray(), 0, LockFlags.EntireBuffer);
				// In a multi-wave playback, only loop the last track. The preceeding tracks are intros.
				// Next, if we have a multi-file situation, we need to wait for the current file to stop playing before starting the next one.
				// This handler will also fire when a sound is done playing by default so we can explicitly dispose of the soundBuffer.
				stoppedSignal = new AutoResetEvent(false);
				NotificationPosition[] n = { new NotificationPosition() { Offset = (int)PcmStream.Length - 1, WaitHandle = new AutoResetEvent(false) } };
				stoppedSignal = (AutoResetEvent)(n[0].WaitHandle);
				sBuff.SetNotificationPositions(n);
			} else {  // If this buffer has already been initialized ahead of time
				sBuff = soundBuffers[p];
			}
			if (!initializeNextTrack) {
				Thread t = new Thread(stopEventHandler);
				t.Start();
				sBuff.Volume = m_volume;
				f = (loop && p == fileNames.Length - 1) ? PlayFlags.Looping : PlayFlags.None;
				sBuff.Play(0, f);
			}
			/*
			if (PcmStream != null) {
				oggFile.Close();
				oggFile.Dispose();
				PcmStream.Close();
				PcmStream.Dispose();
			}
			*/
			if (!initializeNextTrack && playPointer < fileNames.Length - 1) // Prepare the next track.
				process(true);
			preparing = false;
		}

		private void freeResources()
		{
			lock (lockObject) {
				if (stopNow) // If explicitly stopped, freeResources has already been called.
					return;
				stopNow = true;
				fileNames = null;
				SecondarySoundBuffer s = null;
				foreach (SecondarySoundBuffer buffer in soundBuffers) {
					s = buffer;
					DSound.unloadSound(ref s);
				}
			}
		}

	}
}
