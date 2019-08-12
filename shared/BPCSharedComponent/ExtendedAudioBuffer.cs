/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using SharpDX.XAudio2;


namespace BPCSharedComponent.ExtendedAudio
{
	public class ExtendedAudioBuffer
	{
		private AudioBuffer buffer;
		private SourceVoice voice;
		public ExtendedAudioBuffer(AudioBuffer buffer, SourceVoice voice)
		{
			this.buffer = buffer;
			this.voice = voice;
		}

		public void play(bool stop, bool loop)
		{
			if (stop) {
				voice.Stop();
				voice.FlushSourceBuffers();
				buffer.Stream.Position = 0;
			}
			if (loop) {
				buffer.LoopCount = AudioBuffer.LoopInfinite;
			}
			voice.SubmitSourceBuffer(buffer, null);
			voice.Start();
		}

		public void stop()
		{
			voice.Stop();
		}
	}
}
