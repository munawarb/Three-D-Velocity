/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using System.Threading;

namespace BPCSharedComponent.ExtendedAudio
{
	public class ExtendedAudioBuffer: IDisposable
	{
		/// <summary>
		/// Represents the state of our buffer. We have to implement our own states since there's no way to query
		/// the SourceVoice and ask it about its state without attaching to its handles.
		/// </summary>
		public enum State
		{
			playing,
			stopped
		}
		private AudioBuffer buffer;
		private SourceVoice voice;
		private State m_state;
		private bool hasNeverPlayed;
		private Action onEnd;

		/// <summary>
		///  The state of the audio. It will begin in the stopped state.
		/// </summary>
		public State state
		{
			get { return m_state; }
		}

		/// <summary>
		///  Constructs a new ExtendedAudioBuffer.
		/// </summary>
		/// <param name="buffer">The AudioBuffer with which to fill the SourceVoice. The SourceVoice can either be filled beforehand or filled by calling play(true, false).</param>
		/// <param name="voice">The SourceVoice that represents the pipeline of the supplied Audio Buffer. The SourceVoice must be instantiated with delegate support enabled.</param>
		public ExtendedAudioBuffer(AudioBuffer buffer, SourceVoice voice)
		{
			this.buffer = buffer;
			this.voice = voice;
			m_state = State.stopped;
			hasNeverPlayed = true;
			voice.StreamEnd += () =>
			{
				m_state = State.stopped;
				if (onEnd != null)
					new Thread(() => onEnd()).Start();
			};
		}

		/// <summary>
		/// Sets a callback to be executed when this sound is done playing. Querying state during the callback will return State.stopped.
		/// </summary>
		/// <param name="onEnd">A function to execute when this sound stops playing.</param>
		public void setOnEnd(Action onEnd)
		{
			this.onEnd = onEnd;
		}

		/// <summary>
		/// Plays the audio.
		/// </summary>
		/// <param name="stop">If true, will stop the sound and return its position to 0 before playing it. Passing false will have the effect of resuming the sound from the last position it was stopped at.</param>
		/// <param name="loop">Whether or not to loop the sound.</param>
		public void play(bool stop, bool loop)
		{
			if (loop) {
				buffer.LoopCount = AudioBuffer.LoopInfinite;
			}
			// We'll start the buffer from the beginning if we've never played this buffer before so that the sound can be loaded.
			// Otherwise, the sound might start from a random position in the buffer.
			if (stop || hasNeverPlayed) {
				hasNeverPlayed = false;
				voice.Stop();
				voice.FlushSourceBuffers();
				buffer.Stream.Position = 0;
				voice.SubmitSourceBuffer(buffer, null);
			}
			voice.Start();
			m_state = State.playing;
		}

		/// <summary>
		/// Stops playback of the sound.
		/// </summary>
		public void stop()
		{
			voice.Stop();
			m_state = State.stopped;
		}

		/// <summary>
		/// Applies 3-D settings represented by the supplied settings object to the sound.
		/// </summary>
		/// <param name="settings">The DspSettings object that represents changes that should be made to this sound.</param>
		public void apply3D(DspSettings settings)
		{
			voice.SetOutputMatrix(1, 2, settings.MatrixCoefficients);
			voice.SetFrequencyRatio(settings.DopplerFactor);
		}

		/// <summary>
		/// Gets the frequency of the sound expressed in semitones.
		/// </summary>
		/// <returns>The semitones.</returns>
		public float getFrequency()
		{
			voice.GetFrequencyRatio(out float freq);
			return XAudio2.FrequencyRatioToSemitones(freq);
		}

		/// <summary>
		/// Sets the frequency of the sound.
		/// </summary>
		/// <param name="f">The frequency ratio of the sound expressed in semitones.</param>
		public void setFrequency(float f)
		{
			voice.SetFrequencyRatio(XAudio2.SemitonesToFrequencyRatio(f));
		}

		/// <summary>
		/// Gets the volume of the sound.
		/// </summary>
		/// <returns>The volume setting.</returns>
		public float getVolume()
		{
			voice.GetVolume(out float volume);
			return volume;
		}

		/// <summary>
		/// Sets the volume fo the sound.
		/// </summary>
		/// <param name="v">The volume to set the sound at.</param>
		public void setVolume(float v)
		{
			voice.SetVolume(v);
		}

		/// <summary>
		/// Gets the VoiceDetails for the sound.
		/// </summary>
		/// <returns>The VoiceDetails struct.</returns>
		public VoiceDetails getVoiceDetails()
		{
			return voice.VoiceDetails;
		}

		/// <summary>
		/// Applies a level matrix to a sound.
		/// </summary>
		/// <param name="sourceChannels">The channel count for the voice.</param>
		/// <param name="destinationChannels">The channel count for the destination, which is the mastering voice through which this sound is mixed, in most cases.</param>
		/// <param name="levelMatrixRef">The matrix representing levels.</param>
		public void setOutputMatrix(int sourceChannels, int destinationChannels, float[] levelMatrixRef)
		{
			voice.SetOutputMatrix(sourceChannels, destinationChannels, levelMatrixRef);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue) {
				if (disposing) {
					voice.Dispose();
					buffer.Stream.Dispose();
				}
				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~ExtendedAudioBuffer() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
