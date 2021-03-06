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
		private const int minimumNumberOfBuffers = 5;
		private const short bitsPerSample = 16;
		private bool started;
		private XAudio2 device;
		private SourceVoice sourceVoice;
		private String[] fileNames = null;
		private bool stopNow;
		private List<AudioBuffer> memories; // Used to hold reference to AudioBuffer objects so we don't lose these references until XAudio2 is done playing the buffers.
		private Status m_status;
		public Status status
		{
			get { return m_status; }
		}

		public OggBuffer(XAudio2 device, String[] fileNames)
		{
			memories = new List<AudioBuffer>();
			this.fileNames = fileNames;
			this.device = device;
		}

		public void stopOgg()
		{
			if (stopNow)
				return;
			stopNow = true;
			while (isPlaying())
				Thread.Sleep(10);
		}
		public bool isPlaying()
		{
			return status == Status.playing;
		}

		public void play(bool loop)
		{
			m_status = Status.playing;
			process(0, loop);
			while (!started)
				Thread.Sleep(10);
		}
		public void play()
		{
			play(false);
		}

		private void process(int playPointer, bool loop)
		{
			bool noMoreData = false;
			VorbisReader vorbis = new VorbisReader(fileNames[playPointer]);
			if (fileNames.Length > 1) {
				if (playPointer < fileNames.Length - 1)
					loop = false;
				else
					loop = true;
			}
			float[] outBuffer = new float[vorbis.Channels * vorbis.SampleRate / 5];
			// If this is a consecutive track, we've already initialized the sourceVoice so we don't need to do it again.
			// We can just fill the already-playing voice with data from the new track.
			if (playPointer == 0) {
				WaveFormat waveFormat = new WaveFormat(vorbis.SampleRate, bitsPerSample, vorbis.Channels);
				sourceVoice = new SourceVoice(device, waveFormat, false);
			}
			const int rescaleFactor = 32767;
			Func<int, List<DataStream>> getAtLeast = howMany =>
			{
				List<DataStream> samples = new List<DataStream>();
				if (noMoreData)
					return samples;
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
					samples.Add(DataStream.Create<byte>(data, true, false));
					if (++howManySoFar == howMany)
						break;
				}
				if (howManySoFar < howMany)
					noMoreData = true;
				return samples;
			};
			Func<List<DataStream>, List<AudioBuffer>> convertToAudioBuffers = dataStreams =>
			{
				List<AudioBuffer> audioBuffers = new List<AudioBuffer>();
				foreach (DataStream s in dataStreams) {
					audioBuffers.Add(new AudioBuffer { Stream = s, Flags = BufferFlags.None, AudioBytes = (int)s.Length });
				}
				return audioBuffers;
			};
			Action<List<AudioBuffer>> submitToSourceVoice = (audioBuffers) =>
			{
				foreach (AudioBuffer a in audioBuffers) {
					sourceVoice.SubmitSourceBuffer(a, null);
				}
			};
			new Thread(() =>
			{
				VoiceState state;
				List<DataStream> streams = getAtLeast(minimumNumberOfBuffers);
				List<AudioBuffer> buffers = convertToAudioBuffers(streams);
				memories.AddRange(buffers);
				submitToSourceVoice(buffers);
				// If this isn't the first consecutive track, we've already started playing this sourceVoice and are just filling it with data from the new track.
				if (playPointer == 0)
					sourceVoice.Start();
				started = true;
				while (true) {
					if (stopNow)
						break;
					state = sourceVoice.State;
					if (state.BuffersQueued < minimumNumberOfBuffers) {
						// Fill the source with more samples since we're running low.
						List<DataStream> moreStreams = getAtLeast(minimumNumberOfBuffers);
						if (moreStreams.Count < minimumNumberOfBuffers && loop) {
							vorbis.DecodedPosition = 0;
							noMoreData = false;
						}
						if (state.BuffersQueued == 0 && moreStreams.Count == 0)
							break; // Nothing remaining to fill the source with and we've played everything.
						List<AudioBuffer> moreBuffers = convertToAudioBuffers(moreStreams);
						// The buffers that are already played can now be removed.
						for (int i = 0; i < memories.Count - state.BuffersQueued; i++)
							memories[i].Stream.Close();
						memories.RemoveRange(0, memories.Count - state.BuffersQueued);
						memories.AddRange(moreBuffers);
						submitToSourceVoice(moreBuffers);
					}
					Thread.Sleep(10);
				}
				// If we're transitioning to the next track and haven't received a stop signal.
				if (!stopNow && playPointer < (fileNames.Length - 1)) {
					process(playPointer + 1, loop);
					vorbis.Dispose();
					return;
				}
				sourceVoice.Stop();
				sourceVoice.FlushSourceBuffers();
				for (int i = 0; i < memories.Count; i++)
					memories[i].Stream.Close();
				memories.Clear();
				vorbis.Dispose();
				m_status = Status.stopped;
			}).Start();
		}
	}
}
