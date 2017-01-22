/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using OggVorbisDecoder;
using System.Windows.Forms;
using System.Threading;
using System.IO;
namespace BPCSharedComponent.ExtendedAudio
{
	/// <summary>
	/// Buffers and plays Ogg Vorbis data.
	/// </summary>
	public class OggBuffer
	{
#if x86
		private bool is64bit = false;
#else
	 private bool is64bit = true;
#endif
		private bool preparing;
		private delegate void stopEventDelegate();
		private event stopEventDelegate stopEvent;
		private SourceVoice sourceVoice;
		private OggVorbisFileStream oggFile;
		private OggVorbisEncodedStream oggStream;
		private Object lockObject;
		private const short bitsPerSample = 16;
		private XAudio2 device;
		private int playPointer = 0; //keeps track of which file is to be
		//played next in a list of files or streams
		private byte[] byteStream = null; //current bytestream
		private ArrayList byteStreams = null;
		private String[] fileNames = null;
		private float m_volume;
		private int maxBuffers;
		private Boolean stopNow, playing, loop;
		public float volume
		{
			get { return (m_volume); }
			set { setVolume(value); }
		}

		private void setEvent()
		{
			this.stopEvent += stopEventHandler;
		}
		public OggBuffer(String[] fileNames, float volume,
			XAudio2 device, int maxBuffers)
		{
			lockObject = new object();
			this.fileNames = fileNames;
			m_volume = volume;
			this.maxBuffers = maxBuffers;
			setEvent();
			this.device = device;
		}

		public OggBuffer(String fileName, float volume,
						XAudio2 device, int maxBuffers)
		{
			lockObject = new object();
			fileNames = new String[1];
			fileNames[0] = fileName;
			m_volume = volume;
			this.maxBuffers = maxBuffers;
			setEvent();
			this.device = device;
		}

		public OggBuffer(byte[] byteStream, float volume,
			XAudio2 device, int maxBuffers)
		{
			lockObject = new object();
			byteStreams = new ArrayList();
			byteStreams.Add(byteStream);
			this.byteStream = byteStream;
			m_volume = volume;
			this.maxBuffers = maxBuffers;
			setEvent();
			this.device = device;
		}

		public OggBuffer(ArrayList byteStreams, float volume,
			XAudio2 device, int maxBuffers)
		{
			lockObject = new object();
			this.byteStreams = byteStreams;
			m_volume = volume;
			this.maxBuffers = maxBuffers;
			setEvent();
			this.device = device;
			fileNames = null;
			byteStream = (byte[])byteStreams[0];
		}

		private bool isNull()
		{
			return (sourceVoice == null);
		}
		public void setVolume(float v)
		{
			if (!isNull())
				sourceVoice.SetVolume(v);
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
			stopNow = true;
			while (isPlaying())
				Thread.Sleep(10);
		}
		public bool isPlaying()
		{
			if (preparing)
				return true;

			//signal play if preparing so we don't
			//lose resources in the middle of initialization
			return (playing);
		}

		public void play(bool loop)
		{
			if (fileNames == null)
				byteStream = ((byte[])byteStreams[playPointer]);
			this.loop = loop;
			Thread thread = null;
			if (!is64bit)
				thread = new Thread(process);
			else
				thread = new Thread(process64);
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
			if (playPointer <
				((byteStreams == null) ? (fileNames.Length - 1) : (byteStreams.Count - 1))
				&& !stopNow)
			{
				playPointer++;
				play(true);
				return;
			} //if we have additional files to play
			//if there's nothing left to play,
			//or the calling thread has requested this file to be stopped.
			this.stopEvent -= stopEventHandler;
			freeResources();
			playing = false;
			preparing = false;
		}

		private void process()
		{
			lock (lockObject)
				preparing = true;
			byte[] outBuffer = new Byte[4096];
			if (byteStream == null)
				oggFile = new OggVorbisFileStream(fileNames[playPointer]);
			else
				oggStream = new OggVorbisEncodedStream(byteStream);


			MemoryStream PcmStream = null;
			int PcmBytes = -1;

			PcmStream = new MemoryStream();
			WaveFormat waveFormat = new WaveFormat();

			AudioBuffer[] theBuffers = new AudioBuffer[maxBuffers];
			int nextBuffer = 0;
			bool firstLoop = true;
			bool startedSourceVoice = false;
			// Decode the Ogg Vorbis data into its PCM data
			while (PcmBytes != 0)
			{
				// Get the next chunk of PCM data, pin these so the GC can't 
				while (true)
				{
					PcmBytes = (oggStream == null) ? oggFile.Read(outBuffer, 0, outBuffer.Length)
						: oggStream.Read(outBuffer, 0, outBuffer.Length);


					if (PcmBytes == 0) //Reached the end
						break;
					PcmStream.Flush();
					PcmStream.Position = 0;
					PcmStream.Write(outBuffer, 0, PcmBytes);
					PcmStream.Position = 0;
					if (theBuffers[nextBuffer] != null)
					{
						theBuffers[nextBuffer].Stream.Dispose();
						theBuffers[nextBuffer] = null;
					}

					theBuffers[nextBuffer] = new AudioBuffer(SharpDX.DataStream.Create<byte>(PcmStream.ToArray(), true, false));
					theBuffers[nextBuffer].AudioBytes = PcmBytes;
					theBuffers[nextBuffer].LoopCount = 0;
					if (firstLoop)
					{
						VorbisInfo info = (oggStream == null) ? oggFile.Info : oggStream.Info;
						//BlockAlign = info.Channels * (bitsPerSample / 8);
						//AverageBytesPerSecond = info.Rate * BlockAlign;

						//waveFormat.AverageBytesPerSecond = AverageBytesPerSecond;
						//waveFormat.BitsPerSample = (short)bitsPerSample;
						//waveFormat.BlockAlignment = (short)BlockAlign;
						//waveFormat.Channels = (short)info.Channels;
						//waveFormat.SamplesPerSecond = info.Rate;
						waveFormat = new WaveFormat(info.Rate, bitsPerSample, info.Channels);
						//waveFormat.Encoding= WaveFormatEncoding.Pcm;

						sourceVoice = new SourceVoice(device, waveFormat);

						sourceVoice.SetVolume(volume);
					} //if first time looping, create sourcevoice

					sourceVoice.SubmitSourceBuffer(theBuffers[nextBuffer], null);
					if (nextBuffer == theBuffers.Length - 1)
						nextBuffer = 0;
					else
						nextBuffer++;
					//If we're done filling the buffer for the first time
					if (!startedSourceVoice
						&& sourceVoice.State.BuffersQueued
						== maxBuffers)
					{
						sourceVoice.Start();
						startedSourceVoice = true;
						lock (lockObject)
						{
							playing = true;
							preparing = false;
						} //lock
					}
					firstLoop = false;
					if (startedSourceVoice)
					{
						while (sourceVoice.State.BuffersQueued
							> maxBuffers - 1)
						{
							if (stopNow)
								break;
							Thread.Sleep(5);
						}
					} //if started source voice
					if (stopNow)
						break;
				}//while
				if (stopNow)
					break;
				//We don't have any more data but file could still be playing the remaining data.
				if (PcmBytes == 0 /*&& !loop*/)
				{
					if (!stopNow)
					{
						while (sourceVoice.State.BuffersQueued > 0
							&& !stopNow)
							Thread.Sleep(10);
					} //if doesn't want to stop ogg
					if (!loop)
						break; //exit the loop since we ran out of data and don't want to loop back
				} //if we ran out of data
				if (PcmBytes == 0 && loop)
				{
					PcmBytes = -1;
					if (oggFile != null)
						oggFile.Position = 0;
					if (oggStream != null)
						oggStream.Position = 0;
				} //if we ran out of data but want to loop back
			} //while more data

			//Done playing, or file requested stop,
			//so clean up and tell calling thread that
			//buffer has stopped and cleaned up.
			//calling thread doesn't know buffer has stopped until we clean things up
			//so we don't lose memory

			//Clean up the resources
			if (sourceVoice != null)
			{
				sourceVoice.ExitLoop(); //stop looping if looping
				sourceVoice.Stop();
			}
			sourceVoice.Dispose();
			sourceVoice = null;
			if (oggFile != null)
			{
				oggFile.Close();
				oggFile = null;
			}
			outBuffer = null;
			for (int i = 0; i < theBuffers.Length; i++)
			{
				if (theBuffers[i] != null)
				{
					theBuffers[i].Stream.Dispose();
					theBuffers[i] = null;
				}
			}
			theBuffers = null;
			if (oggStream != null)
			{
				oggStream.Close();
				oggStream = null;
			}
			PcmStream.Dispose();
			PcmStream = null;
			if (stopEvent != null)
				stopEvent();
		} //method

		private void freeResources()
		{
			if (byteStreams != null)
			{
				byteStreams.Clear();
				byteStreams = null;
			}
			byteStream = null;
			fileNames = null;
			stopEvent -= stopEventHandler;
		}

		private void process64()
		{
			byte[] outBuffer = null;
			MemoryStream PcmStream = null;
			int PcmBytes = 0;
			WaveFormat waveFormat;
			AudioBuffer[] theBuffers = null;
			int nextBuffer = 0;
			bool firstLoop = false;
			bool startedSourceVoice = false;

			while (true)
			{
				//This is the outer loop
				//which controls looping in
				//64-bit ogg decoding.
				lock (lockObject)
					preparing = true;
				outBuffer = new Byte[4096];
				if (byteStream == null)
					oggFile = new OggVorbisFileStream(fileNames[playPointer]);
				else
					oggStream = new OggVorbisEncodedStream(byteStream);
				PcmBytes = -1;
				//AverageBytesPerSecond = 0;
				//BlockAlign = 0;
				PcmStream = new MemoryStream();
				waveFormat = new WaveFormat();
				theBuffers = new AudioBuffer[maxBuffers];
				nextBuffer = 0;
				firstLoop = true;
				startedSourceVoice = false;
				// Decode the Ogg Vorbis data into its PCM data
				while (PcmBytes != 0)
				{
					// Get the next chunk of PCM data, pin these so the GC can't 
					while (true)
					{
						PcmBytes = (oggStream == null) ? oggFile.Read(outBuffer, 0, outBuffer.Length)
							: oggStream.Read(outBuffer, 0, outBuffer.Length);


						if (PcmBytes == 0) //Reached the end
							break;
						PcmStream.Flush();
						PcmStream.Position = 0;
						PcmStream.Write(outBuffer, 0, PcmBytes);
						PcmStream.Position = 0;
						if (theBuffers[nextBuffer] != null)
						{
							theBuffers[nextBuffer].Stream.Dispose();
							theBuffers[nextBuffer] = null;
						}
						theBuffers[nextBuffer] = new AudioBuffer(SharpDX.DataStream.Create<byte>(PcmStream.ToArray(), true, true));
						theBuffers[nextBuffer].AudioBytes = PcmBytes;
						theBuffers[nextBuffer].LoopCount = 0;
						if (firstLoop)
						{
							VorbisInfo info = (oggStream == null) ? oggFile.Info : oggStream.Info;
							//BlockAlign = info.Channels * (bitsPerSample / 8);
							//AverageBytesPerSecond = info.Rate * BlockAlign;

							//waveFormat.AverageBytesPerSecond = AverageBytesPerSecond;
							//waveFormat.BitsPerSample = (short)bitsPerSample;
							//waveFormat.BlockAlignment = (short)BlockAlign;
							//waveFormat.Channels = (short)info.Channels;
							//waveFormat.SamplesPerSecond = info.Rate;
							waveFormat = new WaveFormat(info.Rate, bitsPerSample, info.Channels);
							//waveFormat.Encoding= WaveFormatEncoding.Pcm;

							sourceVoice = new SourceVoice(device, waveFormat);

							sourceVoice.SetVolume(volume);
						} //if first time looping, create sourcevoice

						sourceVoice.SubmitSourceBuffer(theBuffers[nextBuffer], null);
						if (nextBuffer == theBuffers.Length - 1)
							nextBuffer = 0;
						else
							nextBuffer++;
						//If we're done filling the buffer for the first time
						if (!startedSourceVoice
							&& sourceVoice.State.BuffersQueued == maxBuffers)
						{
							sourceVoice.Start();
							startedSourceVoice = true;
							lock (lockObject)
							{
								playing = true;
								preparing = false;
							} //lock
						}
						firstLoop = false;
						if (startedSourceVoice)
						{
							while (sourceVoice.State.BuffersQueued > maxBuffers - 1)
							{
								if (stopNow)
									break;
								Thread.Sleep(5);
							}
						} //if started source voice
						if (stopNow)
							break;
					}//while
					if (stopNow)
						break;
					//We don't have any more data but file could still be playing the remaining data.
					if (PcmBytes == 0 /*&& !loop*/)
					{
						if (!stopNow)
						{
							while (sourceVoice.State.BuffersQueued > 0
								&& !stopNow)
								Thread.Sleep(10);
						} //if doesn't want to stop ogg
						break; //exit the loop since we ran out of data and don't want to loop back
					} //if we ran out of data
				} //while more data

				//Clean everything up for another loop.
				//Must do clean up here since in 64-bit implementation,
				//we need to recreate all the objects.
				if (sourceVoice != null)
				{
					sourceVoice.ExitLoop(); //stop looping if looping
					sourceVoice.Stop();
				}
				sourceVoice.Dispose();
				sourceVoice = null;
				if (oggFile != null)
				{
					oggFile.Close();
					oggFile = null;
				}
				outBuffer = null;
				for (int i = 0; i < theBuffers.Length; i++)
				{
					if (theBuffers[i] != null)
					{
						theBuffers[i].Stream.Dispose();
						theBuffers[i] = null;
					}
				}
				theBuffers = null;
				if (oggStream != null)
				{
					oggStream.Close();
					oggStream = null;
				}
				PcmStream.Dispose();
				PcmStream = null;

				//We must loop this way,
				//since unlike the 32-bit implementation,
				//64-bit does not support native seek.
				if (PcmBytes == 0 && loop)
				{
					if (!stopNow)
						continue;
					else
						break;
				}
				else
				{ //if we're not looping
					break;
				} //if we ran out of data but want to loop back
			} //outer loop to control
			//loop of file on 64-bit implementation

			//Finally, notify calling thread
			//that we're done playing.
			if (stopEvent != null)
				stopEvent();
		} //method


	}
}
