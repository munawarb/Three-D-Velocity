/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections;
using SharpDX.Multimedia;
using SharpDX.DirectSound;
using OggVorbisDecoder;
using System.Threading;
using System.IO;
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
		private DirectSound device;
		private int playPointer = 0; //keeps track of which file is to be played next in a list of files or streams
		private String[] fileNames = null;
		private int m_volume;
		private Boolean stopNow, loop;
		private SecondarySoundBuffer soundBuffer;
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
				if (soundBuffer != null)
					soundBuffer.Volume = v;
			}
			m_volume = v;
		}

		//stops the ogg file,
		//and blocks the thread until the
		//player returns.
		//Therefore, this method guarantees that
		//the ogg file is done playing when it passes
		//control back to the calling method.
		public void stopOgg()
		{
			stoppedSignal.Set();
			stopNow = true;
			preparing = false;
			if (soundBuffer != null)
				soundBuffer.Stop();
			freeResources();
		}
		public bool isPlaying()
		{
			if (soundBuffer == null)
				return false;
			return DSound.isPlaying(soundBuffer) || DSound.isLooping(soundBuffer);
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
					playPointer++;
					DSound.unloadSound(ref soundBuffer);
					play(loop);
				}
				if (!loop) // If the file is explicitly stopped, the stopOgg method will call free resources so we don't have to do it here; instead, this is the case when the file is done playing naturally, so stopOgg is never called.
					freeResources();
			}
		}

		private void process()
		{
			preparing = true;
			SoundBufferDescription desc = new SoundBufferDescription();
			desc.Flags = BufferFlags.ControlPositionNotify | BufferFlags.ControlVolume;
			byte[] outBuffer = new Byte[4096];
			oggFile = new OggVorbisFileStream(fileNames[playPointer]);
			MemoryStream PcmStream = new MemoryStream();
			int PcmBytes = -1;
			WaveFormat waveFormat = new WaveFormat();
			// Decode the Ogg Vorbis data into its PCM data
			while (PcmBytes != 0) {
				PcmBytes = oggFile.Read(outBuffer, 0, outBuffer.Length);
				PcmStream.Write(outBuffer, 0, PcmBytes);
			}
			VorbisInfo info = oggFile.Info;
			//BlockAlign = info.Channels * (bitsPerSample / 8);
			//AverageBytesPerSecond = info.Rate * BlockAlign;

			//waveFormat.AverageBytesPerSecond = AverageBytesPerSecond;
			//waveFormat.BitsPerSample = (short)bitsPerSample;
			//waveFormat.BlockAlignment = (short)BlockAlign;
			//waveFormat.Channels = (short)info.Channels;
			//waveFormat.SamplesPerSecond = info.Rate;
			waveFormat = new WaveFormat(info.Rate, bitsPerSample, info.Channels);
			//waveFormat.Encoding= WaveFormatEncoding.Pcm;
			desc.Format = waveFormat;
			desc.BufferBytes = (int)PcmStream.Length;
			lock(lockObject) // So we don't lose a simultaneous volume change.
				soundBuffer = new SecondarySoundBuffer(device, desc);
			soundBuffer.Volume = m_volume;
			soundBuffer.Write(PcmStream.ToArray(), 0, LockFlags.EntireBuffer);
			// In a multi-wave playback, only loop the last track. The preceeding tracks are intros.
			PlayFlags f = (loop && playPointer == fileNames.Length - 1) ? PlayFlags.Looping : PlayFlags.None;
			// Next, if we have a multi-file situation, we need to wait for the current file to stop playing before starting the next one.
			// This handler will also fire when a sound is done playing by default so we can explicitly dispose of the soundBuffer.
			stoppedSignal = new AutoResetEvent(false);
			NotificationPosition[] n = { new NotificationPosition() { Offset = (int)PcmStream.Length - 1, WaitHandle = stoppedSignal } };
			soundBuffer.SetNotificationPositions(n);
			Thread t = new Thread(stopEventHandler);
			t.Start();
			soundBuffer.Play(0, f);
			oggFile.Close();
			oggFile.Dispose();
			PcmStream.Close();
			PcmStream.Dispose();
			preparing = false;
		}

		private void freeResources()
		{
			fileNames = null;
			DSound.unloadSound(ref soundBuffer);
		}

	}
}
