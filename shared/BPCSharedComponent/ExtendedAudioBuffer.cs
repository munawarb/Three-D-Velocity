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
			voice.StreamEnd += () =>
			{
				m_state = State.stopped;
			};
		}

		/// <summary>
		/// Plays the audio.
		/// </summary>
		/// <param name="stop">If true, will stop the sound and return its position to 0 before playing it. Passing false will have the effect of resuming the sound from the last position it was stopped at.</param>
		/// <param name="loop">Whether or not to loop the sound.</param>
		public void play(bool stop, bool loop)
		{
			if (stop) {
				voice.Stop();
				voice.FlushSourceBuffers();
				buffer.Stream.Position = 0;
				voice.SubmitSourceBuffer(buffer, null);
			}
			if (loop) {
				buffer.LoopCount = AudioBuffer.LoopInfinite;
			}
			m_state = State.playing;
			voice.Start();
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
