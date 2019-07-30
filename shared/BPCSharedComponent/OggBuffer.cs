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
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
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
		public enum Status
		{
			stopped,
			playing
		}
		private const short bitsPerSample = 16;
		private XAudio2 device;
		private SourceVoice sourceVoice;
		private String[] fileNames = null;
		private float m_volume;
		private Boolean stopNow;
		private Status m_status;
		public Status status
		{
			get { return m_status; }
		}
		public float volume
		{
			get { return (m_volume); }
			set { setVolume(value); }
		}

		public OggBuffer(String[] fileNames, float volume, XAudio2 device)
		{
			this.fileNames = fileNames;
			m_volume = volume;
			this.device = device;
		}

		public OggBuffer(String fileName, float volume, XAudio2 device) : this(new string[] { fileName }, volume, device)
		{

		}
		public void setVolume(float v)
		{
			// XAudio has a min volume of 0.0 and a max volume of 1.0.
			if (v > 1.0f)
				v = 1.0f;
			else if (v < 0.0f)
				v = 0.0f;
			m_volume = v;
			sourceVoice.SetVolume(v);
		}

		public void stopOgg()
		{
			if (stopNow)
				return;
			stopNow = true;
			while (isPlaying())
				Thread.Sleep(0);
		}
		public bool isPlaying()
		{
			return status == Status.playing;
		}

		public void play(bool loop)
		{
			m_status = Status.playing;
			process(0, loop);
		}
		public void play()
		{
			play(false);
		}

		private void process(int playPointer, bool loop)
		{
			VorbisReader vorbis = new VorbisReader(fileNames[playPointer]);
			if (fileNames.Length > 1) {
				if (playPointer < fileNames.Length - 1)
					loop = false;
				else
					loop = true;
			}
			float[] outBuffer = new float[vorbis.Channels*vorbis.SampleRate/5];
			// If this is a consecutive track, we've already initialized the sourceVoice so we don't need to do it again.
			// We can just fill the already-playing voice with data from the new track.
			if (playPointer == 0) {
				WaveFormat waveFormat = new WaveFormat(vorbis.SampleRate, bitsPerSample, vorbis.Channels);
				sourceVoice = new SourceVoice(device, waveFormat, true);
				sourceVoice.SetVolume(m_volume);
			}
			int rescaleFactor = 32767;
			Func<int, List<DataStream>> getAtLeast = howMany =>
			{
				List<DataStream> samples = new List<DataStream>();
				int PcmBytes = 0;
				int howManySoFar = 0;
				while ((PcmBytes = vorbis.ReadSamples(outBuffer, 0, outBuffer.Length)) > 0) {
					short[] intData = new short[PcmBytes];
					byte[] data = new byte[PcmBytes * 2];
					for (int index = 0; index < PcmBytes; index++) {
						intData[index] = (short)(outBuffer[index] * rescaleFactor);
						byte[] b = BitConverter.GetBytes(intData[index]);
						b.CopyTo(data, index * 2);
					}
					samples.Add(DataStream.Create<byte>(data, true, true));
					if (++howManySoFar > howMany)
						break;
				}
				return samples;
			};
			Func<List<DataStream>, List<AudioBuffer>> convertToAudioBuffers = dataStreams =>
			{
				List<AudioBuffer> audioBuffers = new List<AudioBuffer>();
				foreach (DataStream s in dataStreams)
					audioBuffers.Add(new AudioBuffer(s));
				return audioBuffers;
			};
			Action<List<AudioBuffer>> submitToSourceVoice = (audioBuffers) =>
			{
				foreach (AudioBuffer a in audioBuffers)
					sourceVoice.SubmitSourceBuffer(a, null);
			};
			List<DataStream> streams = getAtLeast(5);
			List<AudioBuffer> buffers = convertToAudioBuffers(streams);
			submitToSourceVoice(buffers);
			// If this isn't the first consecutive track, we've already started playing this sourceVoice and are just filling it with data from the new track.
			if (playPointer == 0)
				sourceVoice.Start();
			new Thread(() =>
			{
				VoiceState state;
				while (true) {
					if (stopNow)
						break;
					state = sourceVoice.State;
					if (state.BuffersQueued < 5) {
						// Fill the source with more samples since we're running low.
						List<DataStream> moreStreams = getAtLeast(5);
						if (moreStreams.Count < 5 && loop)
							vorbis.DecodedPosition = 0;
						if (state.BuffersQueued == 0 && moreStreams.Count == 0)
							break; // Nothing remaining to fill the source with and we've played everything.
						List<AudioBuffer> moreBuffers = convertToAudioBuffers(moreStreams);
						submitToSourceVoice(moreBuffers);
					}
					Thread.Sleep(10);
				}
				// If we're transitioning to the next track and haven't received a stop signal.
				if (!stopNow && playPointer < (fileNames.Length - 1)) {
					process(playPointer + 1, loop);
					return;
				}
				sourceVoice.Stop();
				m_status = Status.stopped;
			}).Start();
		}
	}
}
