using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.Threading;
using Microsoft.DirectX.DirectSound;

namespace TG.Sound
{
	#region Enumerations code
	public enum OggSampleSize {EightBits, SixteenBits}
	#endregion

	////[Description(1, "Delegate and event-args class for event notification when PlayOggFile method completes.")] 
    public delegate void PlayOggFileEventHandler(object sender, PlayOggFileEventArgs e);

	////[Description(1, "Delegate for event notification when client wishes to interrupt playback.")] 
	public delegate void StopOggFileEventHandler(object sender, PlayOggFileEventArgs e);

	////[Description(1, "Event-args class for event notification when PlayOggFile method completes.")] 
	public sealed class PlayOggFileEventArgs : EventArgs
	{
		#region Members and events code
		private bool success;
		private string reasonForFailure; // if !Success then this is the explanation for the failure
		private int playId;	// Value of the playID parameter when PlayOggFile() was called
        
		//NOTE: The Ogg Vorbis decoder may have encountered errors while decoding the Ogg Vorbis data.
		//		These two error counts are for information purposes only, since if Success the
		//		created waveform data was played, but it may not have sounded as intended if either of 
		//		these two counts are nonzero.
		public int ErrorHoleCount, // Count of encountered OV_HOLE errors during decoding
								   // indicates there was an interruption in the data.
				ErrorBadLinkCount; // Count of encountered OV_EBADLINK errors during decoding
								   // indicates that an invalid stream section was supplied to libvorbisfile, 
								   // or the requested link is corrupt.
		#endregion

		#region Properties code	
		public bool Success 
		{
			get { return success; }
			set { success = value; }
		}

		public string ReasonForFailure 
		{
			get { return reasonForFailure; }
			set { reasonForFailure = value; }
		}

		public int PlayId 
		{
			get { return playId; }
			set { playId = value; }
		}
		#endregion

		#region Initialization code
		public PlayOggFileEventArgs(int Id)
		{
			this.PlayId = Id;
		}

		public PlayOggFileEventArgs(bool success, string reason, int Id, int errorHoleCount, int errorBadLinkCount)
		{
			this.Success = success;
			this.ReasonForFailure = reason;
			this.PlayId = Id;
			this.ErrorBadLinkCount = errorBadLinkCount;
			this.ErrorHoleCount = errorHoleCount;
		}
		#endregion
	}

	//[Description(1, "OggPlay class responsible for playing an Ogg sound file.")] 
	public class OggPlay : IDisposable
	{
		#region Members and events code
		private const int WaitTimeout = 5000;
		private Device directSoundDevice;
		private OggSampleSize oggFileSampleSize;
		private bool disposed;
		
		public event PlayOggFileEventHandler PlayOggFileResult;
		public event StopOggFileEventHandler StopOggFileNow;
		#endregion

		#region Properties code	
		public Device DirectSoundDevice 
		{
			get { return directSoundDevice; }
			set { directSoundDevice = value; }
		}

		public OggSampleSize OggFileSampleSize
		{
			get { return oggFileSampleSize; }
			set { oggFileSampleSize = value; }
		}
		#endregion

		#region Intialization code
		/*[Description(1, "Constructor. Creates a new DirectX Sound device, sets the cooperative level and sample size. " +
		"owner is used by SetCooperativeLevel(), which defines its owner parameter as: 'The System.Windows.Forms.Control" +
		"of the application that is using the Device object.'  This should just be the applications main window. " +
		"wantedOggSampleSize is either EightBits or SixteenBits."),
		Remarks("8-bit sample size has lower quality but is faster and takes less memory than 16-bit sample size." +
		"If your game's ogg files are encoded with 8-bit sample size, then choose 8 (you can also choose 16, but that is " +
		"wasteful and gains nothing if the ogg sources are only 8-bit).  If your game's ogg files are encoded with " +
		"16-bit sample size, then choose 16 to get the full sound quality during playback, or choose 8 (or give the user " +
		"the option of choosing 8) if you want to minimize playback resource requirements. If your game's ogg files " +
		"are a mixture (some are encoded with 8-bit sample size and others are encoded with 16-bit sample size), then " +
		"choose whichever you think is best (either setting, EightBits or SixteenBits, will play all the ogg files).")]
		*/public OggPlay(Control owner, OggSampleSize wantedOggSampleSize)
		{
			// Set DirectSoundDevice
			DirectSoundDevice = new Device();

			// NOTE: The DirectSound documentation recommend CooperativeLevel.Priority for games
			DirectSoundDevice.SetCooperativeLevel(owner, CooperativeLevel.Priority);

			// Set OggSampleSize
			OggFileSampleSize = wantedOggSampleSize;
		}
		#endregion

		#region IDispose implementation
		//[Description(1, "You will want to call Dispose() when done using the OggPlay class instance.")]
		protected virtual void Dispose(bool disposing)
		{
			lock(this)
			{
				// Do nothing if the object has already been disposed of
				if (disposed)
					return;

				if (disposing)
				{
					// Release disposable objects used by this instance here

					// Cleanup DirectSound Device
					if (DirectSoundDevice != null)
					{
						DirectSoundDevice.Dispose();
						DirectSoundDevice = null;
					}
				}

				// Release unmanaged resource here. Don't access reference type fields

				// Remember that the object has been disposed of
				disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);

			// Unregister object for finalization
			GC.SuppressFinalize(this);
		}

		~OggPlay()
		{
			Dispose(false);
		}
		#endregion

		#region Ogg playback commands code
		/*[Description(1, "Plays the specified Ogg Vorbis file. It accepts the file name of the sound file to play, " + 
		"and an arbitrary Id value (determined by the caller) as parameters. The Id is returned in the raised event " +
		"so your handler code can use it to identify which specific PlayOggFile() call resulted in that handled event."),
		Remarks("Add your event handler to the public event PlayOggFileResult. Also see overloaded version of this " +
		"method if you want to play an Ogg Vorbis file from memory instead."),
		ReturnValue("Returns immediately, since decoding and playback are done in a separate thread.")]
		*/public void PlayOggFile(string fileName, int playId)
		{
			// Create an event argument class identified by the playId
			PlayOggFileEventArgs EventArgs = new PlayOggFileEventArgs(playId);

			// Decode the Ogg Vorbis file in a separate thread
			PlayOggFileThreadInfo pofInfo = new PlayOggFileThreadInfo(
				EventArgs, fileName, null, OggFileSampleSize == OggSampleSize.EightBits ? 8 : 16,
				DirectSoundDevice, this);

			Thread PlaybackThread = new Thread(new ThreadStart(pofInfo.PlayOggFileThreadProc));
			PlaybackThread.Start();
		}

		//[Description(1, "Plays the specified Ogg Vorbis data from the specified memory stream instead of a file."),
		//Remarks("See the overloaded PlayOggFile method for more details."),
		//ReturnValue("See the overloaded PlayOggFile method for more details.")]
		public void PlayOggFile(byte[] data, int playId)
		{			
			// Create an event argument class identified by the playId
			PlayOggFileEventArgs EventArgs = new PlayOggFileEventArgs(playId);

			// Decode the Ogg Vorbis memory stream in a separate thread
			PlayOggFileThreadInfo pofInfo = new PlayOggFileThreadInfo(
				EventArgs, null, data, OggFileSampleSize == OggSampleSize.EightBits ? 8 : 16,
				DirectSoundDevice, this);

			Thread PlaybackThread = new Thread(new ThreadStart(pofInfo.PlayOggFileThreadProc));
			PlaybackThread.Start();
		}

		//[Description(1, "Stops the specified Ogg file. playId is the Id of the ogg file to stop playback on. ")]
		public void StopOggFile(int playId)
		{
			PlayOggFileEventArgs EventArgs = new PlayOggFileEventArgs(playId);
			StopOggFileNow(this, EventArgs);	
		}
		#endregion

		//[Description(1, "Class for playback thread created in PlayOggFile method of OggPlay class.")]
		class PlayOggFileThreadInfo
		{
			#region Members and events code
			private string fileName;
			private byte[] memFile;
			private int bitsPerSample;  // either 8 or 16
			private bool stopNow;
			private PlayOggFileEventArgs eventArgs;
			private Device directSoundDevice;
			private OggPlay oggPlay;
			#endregion

			#region Properties code	
			public PlayOggFileEventArgs EventArgs 
			{
				get { return eventArgs; }
				set { eventArgs = value; }
			}

			public string FileName
			{
				get { return fileName; }
				set { fileName = value; }
			}

			public byte[] MemFile
			{
				get { return memFile; }
				set { memFile = value; }
			}

			public int BitsPerSample
			{
				get { return bitsPerSample; }
				set { bitsPerSample = value; }
			}

			public Device DirectSoundDevice 
			{
				get { return directSoundDevice; }
				set { directSoundDevice = value; }
			}

			public OggPlay oplay
			{
				get { return oggPlay; }
				set { oggPlay = value; }
			}
			#endregion

			#region Initialization code
			public PlayOggFileThreadInfo(PlayOggFileEventArgs eventArgs, string fileName, 
				byte[] memFile, int bitsPerSample, Device directSound, OggPlay oPlay)
			{
				// Verify parameters
				if (null == eventArgs || null == directSound || 
					null == oPlay || (null == fileName && null == memFile))
					throw new System.ArgumentNullException();
				else if (null == fileName && null == memFile)
					throw new System.ArgumentException(("Either a file name parameter or a memory stream parameter must be specified, but not both."));
							
				// Initialize this objects data members
				this.EventArgs = eventArgs;
				this.FileName = fileName;
				this.MemFile = memFile;
				this.BitsPerSample = bitsPerSample;
				this.DirectSoundDevice = directSound;
				this.oplay = oPlay;

				// Add the interrupt event handler
				oplay.StopOggFileNow += new StopOggFileEventHandler(InterruptOggFilePlayback);
			}
			#endregion

			#region Interrupt thread handler code
			//[Description(1, "Stops thread playback thread immediately.")]
			private void InterruptOggFilePlayback(object sender, PlayOggFileEventArgs e)
			{
				if (e.PlayId == EventArgs.PlayId)
				{
					stopNow = true;
				}
			}
			#endregion

			//[Description(1, "Playback thread for use by the PlayOggFile method of OggPlay class.")]
			public void PlayOggFileThreadProc()
			{
				// Call the external C functions to decode the ogg file
				unsafe 
				{
					void *vf = null;
					SecondaryBuffer SecBuf = null;
					BufferDescription MyDescription = null;
					Notify MyNotify = null;

					try
					{
						// Initialize the file for Ogg Vorbis decoding using
						// data from either a file name or memory stream.
						int ErrorCode = 0;
						if (null != FileName)
							ErrorCode = NativeMethods.init_for_ogg_decode(FileName, &vf);
						else if (null != MemFile)
							ErrorCode = NativeMethods.memory_stream_for_ogg_decode(MemFile, MemFile.Length, &vf);

						// If any error occurred then set the reason for failure
						// and return it to the calling application.
						if (ErrorCode != 0)
						{
							// Build the reason string
							EventArgs.ReasonForFailure =
								"Ogg Vorbis decoder initialization for ogg file '" + FileName + "' failed: ";

							switch(ErrorCode)
							{
								case NativeMethods.ifod_err_open_failed:
									EventArgs.ReasonForFailure += "Unable to open the ogg file.";
									break;
								case NativeMethods.ifod_err_malloc_failed:
									EventArgs.ReasonForFailure += "Out of memory.";
									break;
								case NativeMethods.ifod_err_read_failed:
									EventArgs.ReasonForFailure += "A read from media returned an error.";
									break;
								case NativeMethods.ifod_err_not_vorbis_data:
									EventArgs.ReasonForFailure += "Bitstream is not Vorbis data.";
									break;
								case NativeMethods.ifod_err_vorbis_version_mismatch:
									EventArgs.ReasonForFailure += "Vorbis version mismatch.";
									break;
								case NativeMethods.ifod_err_invalid_vorbis_header:
									EventArgs.ReasonForFailure += "Invalid Vorbis bitstream header.";
									break;
								case NativeMethods.ifod_err_internal_fault:
									EventArgs.ReasonForFailure += 
										"Internal logic fault; indicates a bug or heap/stack corruption.";
									break;
								case NativeMethods.ifod_err_unspecified_error:
									EventArgs.ReasonForFailure += "Vorbis ov_open() returned an undocumented error.";
									break;
								default:
									Debug.Assert(false);
									break;
							}

							// Raise the finished play event to return status
							oplay.PlayOggFileResult(this, EventArgs);
							return;
						}

						Debug.WriteLine("OggPlayer.cs: Ogg Vorbis decoder successfully initialized.");

						byte[] PcmBuffer = new byte[4096]; // 4096 is the Vorbisfile API recommended size
						bool FirstTime, AtEOF, FormatChanged;
						int ChannelsCount = 0, SamplingRate = 0, 
							PreviousChannelCount = 0, PreviousSamplingCount = 0,
							PcmBytes;
						int AverageBytesPerSecond = 0,
							BlockAlign = 0;

						WaveFormat MyWaveFormat = new WaveFormat();

						// NOTE: DirectSound documentation recommends from 1 to 2 seconds 
						//       for buffer size, so 1.2 is an arbitrary but good choice.
						double SecBufHoldThisManySeconds = 1.2;
						int SecBufByteSize = 0,
							SecBufNextWritePosition = 0,
							SecBufPlayPositionWhenNextWritePositionSet = 0;
						AutoResetEvent 
							SecBufNotifyAtBegin     = new AutoResetEvent(false),
							SecBufNotifyAtOneThird  = new AutoResetEvent(false),
							SecBufNotifyAtTwoThirds = new AutoResetEvent(false);
						bool SecBufInitialLoad = true;

						MemoryStream PcmStream = new MemoryStream();
						int PcmStreamNextConsumPcmPosition = 0;

						WaitHandle[] SecBufWaitHandles = {SecBufNotifyAtBegin,
															SecBufNotifyAtOneThird,
															SecBufNotifyAtTwoThirds};

						Debug.WriteLine("OggPlayer.cs: Ogg Vorbis decoder playing back ogg file.");
						
						// Decode the ogg file into its PCM data
						for (FirstTime = true;;)
						{
							if (stopNow) // Client has decided to stop playback!
							{
								SecBuf.Stop();
								Debug.WriteLine("OggPlayer.cs: Ogg Vorbis decoder playback interrupted.");
								break;
							}

							// Get the next chunk of PCM data, pin these so the GC can't relocate them
							fixed(byte *buf = &PcmBuffer[0])
							{
								fixed(int *HoleCount = &EventArgs.ErrorHoleCount)
								{
									fixed(int *BadLinkCount = &EventArgs.ErrorBadLinkCount)
									{
										// NOTE: The sample size of the returned PCM data -- either 8-bit 
										//		 or 16-bit samples -- is set by BitsPerSample
										PcmBytes = NativeMethods.ogg_decode_one_vorbis_packet(
											vf, buf, PcmBuffer.Length,
											BitsPerSample,
											&ChannelsCount, &SamplingRate,
											HoleCount, BadLinkCount);
									}
								}
							}

							// Set AtEOF
							if (PcmBytes == 0)
								AtEOF = true;
							else
								AtEOF = false;

							if (FirstTime && AtEOF)
							{
								EventArgs.ReasonForFailure =
									"The Ogg Vorbis file '" + FileName + "' has no usable data.";
								oplay.PlayOggFileResult(this, EventArgs);
								return;
							}

							// We must be aware that multiple bitstream sections do not 
							// necessarily use the same number of channels or sampling rate							
							if (!FirstTime &&
								(ChannelsCount != PreviousChannelCount
								|| SamplingRate != PreviousSamplingCount))
								FormatChanged = true;
							else
								FormatChanged = false;

							// Compute format items
							if (FirstTime || FormatChanged)
							{
								BlockAlign = ChannelsCount * (BitsPerSample / 8);
								AverageBytesPerSecond = SamplingRate * BlockAlign;
							}

							// Use the PCM data
							if (FirstTime)
							{
								int HoldThisManySamples = 
									(int)(SamplingRate * SecBufHoldThisManySeconds);

								// Set the format
								MyWaveFormat.AverageBytesPerSecond = AverageBytesPerSecond;
								MyWaveFormat.BitsPerSample = (short)BitsPerSample;
								MyWaveFormat.BlockAlign = (short)BlockAlign;
								MyWaveFormat.Channels = (short)ChannelsCount;
								MyWaveFormat.SamplesPerSecond = SamplingRate;
								MyWaveFormat.FormatTag = WaveFormatTag.Pcm;

								// Set BufferDescription
								MyDescription = new BufferDescription();
								MyDescription.Format = MyWaveFormat;
								MyDescription.BufferBytes = 
									SecBufByteSize = HoldThisManySamples * BlockAlign;
								MyDescription.CanGetCurrentPosition = true;
								MyDescription.ControlPositionNotify = true;

								// Create the buffer
								SecBuf = new SecondaryBuffer(MyDescription, DirectSoundDevice);

								// Set 3 notification points, at 0, 1/3, and 2/3 SecBuf size
								MyNotify = new Notify(SecBuf);

								BufferPositionNotify[] MyBufferPositions = new BufferPositionNotify[3];

								MyBufferPositions[0].Offset = 0;
								MyBufferPositions[0].EventNotifyHandle = SecBufNotifyAtBegin.Handle;
								MyBufferPositions[1].Offset = (HoldThisManySamples / 3) * BlockAlign;
								MyBufferPositions[1].EventNotifyHandle = SecBufNotifyAtOneThird.Handle;
								MyBufferPositions[2].Offset = ((HoldThisManySamples * 2) / 3) * BlockAlign;
								MyBufferPositions[2].EventNotifyHandle = SecBufNotifyAtTwoThirds.Handle;

								MyNotify.SetNotificationPositions(MyBufferPositions);

								// Prepare for next iteration
								FirstTime = false;
								PreviousChannelCount = ChannelsCount;
								PreviousSamplingCount = SamplingRate;
							}
							else if (FormatChanged)
							{
								SecBuf.Stop();

								EventArgs.ReasonForFailure =
									"The Ogg Vorbis file '" + FileName + "' has a format change (DirectSound can't handle this).";
								oplay.PlayOggFileResult(this, EventArgs);
								return;
							}
							else if (AtEOF)
							{
								Debug.WriteLine("OggPlayer.cs: Ogg Vorbis decoder playback at end of file.");

								Debug.Assert(SecBufPlayPositionWhenNextWritePositionSet >= 0
									&& SecBufPlayPositionWhenNextWritePositionSet < SecBufByteSize
									&& SecBufNextWritePosition >= 0
									&& SecBufNextWritePosition < SecBufByteSize);

								// Start playback if there wasn't enough PCM data to fill SecBuf the first time
								if (SecBufInitialLoad)
								{
									Debug.Assert(SecBufPlayPositionWhenNextWritePositionSet == 0
										&& SecBufNextWritePosition > 0);

									// NOTE: Play() Does the playing in its own thread
									SecBuf.Play(0, BufferPlayFlags.Looping);
									Debug.WriteLine("OggPlayer.cs: Ogg Vorbis decoder starting playback.");
								}

								// Poll for end of current playback
								int LoopbackCount = 0,
									PlayPosition,
									PreviousPlayPosition = SecBufPlayPositionWhenNextWritePositionSet;

								for(;; PreviousPlayPosition = PlayPosition)
								{
									Thread.Sleep(10);  // 10 milliseconds is an arbitrary but good choice

									PlayPosition = SecBuf.PlayPosition;

									if (PlayPosition < PreviousPlayPosition)
										++LoopbackCount;

									if (SecBufPlayPositionWhenNextWritePositionSet <= SecBufNextWritePosition)
									{
										if (PlayPosition >= SecBufNextWritePosition || LoopbackCount > 0)
											break;
									}
									else
									{
										if ((PlayPosition < SecBufPlayPositionWhenNextWritePositionSet
											&& PlayPosition >= SecBufNextWritePosition) || LoopbackCount > 1)
											break;
									}
								}

								// Done playing
								Debug.WriteLine("OggPlayer.cs: Ogg Vorbis decoder finished playback.");
								SecBuf.Stop();
								break;
							}

							// Copy the new PCM data into PCM memory stream
							PcmStream.SetLength(0);
							PcmStream.Write(PcmBuffer, 0, PcmBytes);
							PcmStream.Position = 0;
							PcmStreamNextConsumPcmPosition = 0;

							Debug.Assert(PcmStream.Length == PcmBytes);

							// Initial load of secondary buffer
							if (SecBufInitialLoad)
							{
								int WriteCount = (int)Math.Min(
									PcmStream.Length,
									SecBufByteSize - SecBufNextWritePosition);

								Debug.Assert(WriteCount >= 0);

								if (WriteCount > 0)
								{
									Debug.Assert(PcmStream.Position == 0);

									SecBuf.Write(
										SecBufNextWritePosition,
										PcmStream,
										WriteCount,
										LockFlag.None);

									SecBufNextWritePosition += WriteCount;
									PcmStreamNextConsumPcmPosition += WriteCount;
								}

								if (SecBufByteSize == SecBufNextWritePosition)
								{
									// Done filling the buffer
									SecBufInitialLoad = false;
									SecBufNextWritePosition = 0;

									// So start the playback in its own thread
									SecBuf.Play(0, BufferPlayFlags.Looping);

									// Yield rest of timeslice so playback can start right away
									Thread.Sleep(0);  
								}
								else
								{
									continue;  // Get more PCM data
								}
							}

							// Exhaust the current PCM data, writing the data into SecBuf
							for(; PcmStreamNextConsumPcmPosition < PcmStream.Length;)
							{
								int WriteCount = 0,
									PlayPosition = SecBuf.PlayPosition,
									WritePosition = SecBuf.WritePosition;

								if (SecBufNextWritePosition < PlayPosition
									&& (WritePosition >= PlayPosition || WritePosition < SecBufNextWritePosition))
									WriteCount = PlayPosition - SecBufNextWritePosition;
								else if (SecBufNextWritePosition > WritePosition
									&& WritePosition >= PlayPosition)
									WriteCount = (SecBufByteSize - SecBufNextWritePosition) + PlayPosition;

								Debug.Assert(WriteCount >= 0 && WriteCount <= SecBufByteSize);
							
								if (WriteCount > 0)
								{
									WriteCount = (int)Math.Min(
										WriteCount,
										PcmStream.Length - PcmStreamNextConsumPcmPosition);

									PcmStream.Position = PcmStreamNextConsumPcmPosition;
									SecBuf.Write(
										SecBufNextWritePosition,
										PcmStream,
										WriteCount,
										LockFlag.None);

									SecBufNextWritePosition = 
										(SecBufNextWritePosition + WriteCount) % SecBufByteSize;
									SecBufPlayPositionWhenNextWritePositionSet = PlayPosition;
									PcmStreamNextConsumPcmPosition += WriteCount;
								}
								else
								{
									WaitHandle.WaitAny(SecBufWaitHandles);
								}
							}
						}

						// Finito
						EventArgs.Success = true;
						oplay.PlayOggFileResult(this, EventArgs);
					}
					catch(System.Exception ex)
					{     
						EventArgs.ReasonForFailure = ex.Message;
						oplay.PlayOggFileResult(this, EventArgs);
						return;
					}
					finally
					{
						// Cleanup vorbis decoder
						int ErrorCode = NativeMethods.final_ogg_cleanup(vf);
						Debug.Assert(ErrorCode == 0);

						// Cleanup DirectSound stuff
						if (SecBuf != null)
						{
							SecBuf.Dispose();
							SecBuf = null;
						}

						if (MyDescription != null) 
						{
							MyDescription.Dispose();
							MyDescription = null;
						}
						
						if (MyNotify != null)
						{
							MyNotify.Dispose();
							MyNotify = null;
						}

						Debug.WriteLine("OggPlayer.cs:Ogg Vorbis decoder objects cleaned up.");
					}
				}  // End unsafe  
			} 
		} 
	}
} 
