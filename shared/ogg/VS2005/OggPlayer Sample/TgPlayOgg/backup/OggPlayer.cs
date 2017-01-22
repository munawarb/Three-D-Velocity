// Copyright (c) 2003-2006 TrayGames, LLC 
// All rights reserved. Reproduction or transmission of this file, or a portion
// thereof, is forbidden without prior written permission of TrayGames, LLC.
//
// Author: Perry L. Marchant
// Date: June 2 2005

using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using SlimDX;
using SlimDX.DirectSound;
using SlimDX.Multimedia;

namespace TG.Sound
{
    //[Description(1, "Class for managing the playback of an Ogg Vorbis encoded sound file."),
    //Remarks("This class requires Managed DirectX to be installed because it uses a DirectSound " +
    //"device for audio playback during Ogg Vorbis decoding."),
    //Example("http://developer.traygames.com/Docs/?doc=OggLib")]
	public class OggPlayManager : IDisposable
	{
		#region Members and events code

		private const int WaitTimeout = 5000;
		private DirectSound directSoundDevice;
		private SampleSize oggFileSampleSize;
        private bool globalFocus;
        private bool disposed;
        private List<Thread> playbackThreads;

        //[Description(1, "Describes the two possible sample rates that can be used for playback.")] 
        public enum SampleSize { EightBits, SixteenBits }
        //[Description(1, "Event notification when the client wishes to interrupt playback.")]
        public event EventHandler<OggPlayEventArgs> StopOggFileNow;

		#endregion

		#region Properties code	

        //[Description(1, "Indicates the requested sample size for the audio data."),
        //Remarks("8-bit sample size has lower quality but is faster and takes less memory than 16-bit sample size." +
        //"If your games ogg files are encoded with an 8-bit sample size, then choose 8 (you can also choose 16, but that is " +
        //"wasteful and gains nothing if the Ogg sources are only 8-bit). If your game's Ogg files are encoded with a " +
        //"16-bit sample size, then choose 16 to get the full sound quality during playback, or choose 8 (or give the user " +
        //"the option of choosing 8) if you want to minimize playback resource requirements. If your game's Ogg files " +
        //"are a mixture (some are encoded with 8-bit sample size and others are encoded with 16-bit sample size), then " +
        //"choose whichever you think is best (either setting, EightBits or SixteenBits, will play all the ogg files).")]        
        public SampleSize OggFileSampleSize
		{
			get { return oggFileSampleSize; }
			set { oggFileSampleSize = value; }
		}

        //[Description(1, "Indicates whether or not to play buffer on loss of focus.")]
        public bool GlobalFocus
        {
            get { return globalFocus; }
            set { globalFocus = value; }
        }

        #endregion

		#region Intialization code

        //[Description(1, "Constructor. Creates a new DirectX Sound device, sets the cooperative level, global focus, " +
        //"and sample size. <b>owner</b> is used by the DirectSound SetCooperativeLevel method, which defines this parameter " +
        //"as the <b>System.Windows.Forms.Control</b> of the application that is using the Device object. This should be " +
        //"your applications main window. <b>globalFocus</b> will allow the buffer to play even if the owner loses focus. " +
        //"<b>wantedOggSampleSize</b> is either EightBits or SixteenBits, the sample rate you desire."),
        //Remarks("8-bit sample size has lower quality but is faster and takes less memory than 16-bit sample size." +
        //"If your game's ogg files are encoded with an 8-bit sample size, then choose 8 (you can also choose 16, but that is " +
        //"wasteful and gains nothing if the Ogg sources are only 8-bit). If your game's Ogg files are encoded with a " +
        //"16-bit sample size, then choose 16 to get the full sound quality during playback, or choose 8 (or give the user " +
        //"the option of choosing 8) if you want to minimize playback resource requirements. If your game's Ogg files " +
        //"are a mixture (some are encoded with 8-bit sample size and others are encoded with 16-bit sample size), then " +
        //"choose whichever you think is best (either setting, EightBits or SixteenBits, will play all the ogg files)." +
        //"IMPORTANT: To stay compatible with software decoders you should encode your ogg file at 44khz or lower!")]
        public OggPlayManager(IntPtr owner, bool wantGlobalFocus, SampleSize wantedOggSampleSize)
        {
            // NOTE: You will get the following warning when targeting the .NET 2.0
            // platform: 'Managed Debugging Assistant 'LoaderLock' has detected a 
            // problem in 'TGOggPlayer.vshost.exe'. This seems to be an issue in the 
            // current Managed DirectX library and will have to be ignored for now.

            directSoundDevice = new DirectSound();

            // DirectSound documentation recommends Priority for games
            this.directSoundDevice.SetCooperativeLevel(owner, 
CooperativeLevel.Priority);

            // Set OggSampleSize 8 or 16 bit, see description for details
            this.oggFileSampleSize = wantedOggSampleSize;

            // Set whether or not to play buffer on loss of focus
            this.globalFocus = wantGlobalFocus;

            // Create a new list to hold thread objects
            if (null == (playbackThreads = new List<Thread>()))
            {
                throw new Exception("Unable to create playback thread array list.");
            }
        }

//Allows to specify the DSDevice
        public OggPlayManager(DirectSound DirectSoundDevice, bool wantGlobalFocus, SampleSize wantedOggSampleSize)
        {
            // NOTE: You will get the following warning when targeting the .NET 2.0
            // platform: 'Managed Debugging Assistant 'LoaderLock' has detected a 
            // problem in 'TGOggPlayer.vshost.exe'. This seems to be an issue in the 
            // current Managed DirectX library and will have to be ignored for now.
directSoundDevice=DirectSoundDevice;
            // Set OggSampleSize 8 or 16 bit, see description for details
            this.oggFileSampleSize = wantedOggSampleSize;

            // Set whether or not to play buffer on loss of focus
            this.globalFocus = wantGlobalFocus;

            // Create a new list to hold thread objects
            if (null == (playbackThreads = new List<Thread>()))
            {
                throw new Exception("Unable to create playback thread array list.");
            }
        }


        //[Description(1, "Overloaded constructor. Defaults <b>globalFocus</b> to false and the <b>wantedSampleSize</b> to 16-bit. " +
        //"You provide your applications main window for the <b>owner</b> parameter.")]
        public OggPlayManager(IntPtr owner)
            : this(owner, false, SampleSize.SixteenBits)
        {
        }

        //[Description(1, "Overloaded constructor. Defaults <b>globalFocus</b> to false. " +
        //"You provide your applications main window for the owner parameter and desired sample size.")]
        public OggPlayManager(IntPtr owner, SampleSize wantedOggSampleSize)
            : this(owner, false, wantedOggSampleSize)
        {
        }

        #endregion

		#region IDisposable implementation

		protected virtual void Dispose(bool disposing)
		{
			lock(this)
			{
				// Do nothing if the object has already been disposed of
				if (disposed)
					return;

                Debug.WriteLine("OggPlayer.cs: Disposing object");

                // If disposing equals true, dispose all managed 
                // and unmanaged resources.

				if (disposing)
				{
					// Release disposable objects used by this instance here

					// Cleanup DirectSound Device
					if (directSoundDevice != null)
					{
						directSoundDevice.Dispose();
						directSoundDevice = null;
					}
				}

				// Release unmanaged resource here. Don't access reference type fields

				// Remember that the object has been disposed of
				disposed = true;
			}
		}

        //[Description(1, "You must call this method when you are done using the class " +
        //"instance, it will dispose the DirectSound device and other resources.")]
		public void Dispose()
		{
            // A derived class should not be able to override this method.
            Dispose(true);

            // Take yourself off of the finalization queue to prevent 
            // finalization code for this object from executing a second time.
            GC.SuppressFinalize(this);
		}

		#endregion

		#region Ogg playback commands code

        //[Description(1, "Plays the specified Ogg Vorbis file. Accepts the name of the sound file to play and an " +
        //"arbitrary Id value (caller determined)."),
        //Remarks("See the overloaded <b>PlayOggFile</b> method for more details."),
        //ReturnValue("Returns immediately, decoding and playback are done in a separate thread.")]
        public OggPlayThreadInfo PlayOggFile(string fileName, int playId)
        {
        	return(PlayOggFile(fileName, playId, 0, 0));
        }

        //[Description(1, "Plays the specified Ogg Vorbis file. Accepts the <b>fileName</b> of the sound file to play, an arbitrary " +
        //"Id value (determined by the caller), volume level and balance as parameters. The <b>playId</b> is returned " +
        //"in the raised event so your handler code can use it to identify which specific <b>PlayOggFile</b> call resulted " +
        //"in that handled event. The <b>volumeLevel</b> can be set in a range from 0 (full volume) to -10,000 (silent)." +
        //"Unlike volume, <b>balance</b> ranges from -10,000 (full left) to +10,000 (full right), with zero being center."),
        //Remarks("Add your event handler to the public event <b>PlayOggFileResult</b>. Also see the overloaded version of " +
		//"this method if you want to play an Ogg Vorbis file from memory instead."),
		//ReturnValue("Returns immediately, decoding and playback are done in a separate thread.")]
		public OggPlayThreadInfo PlayOggFile(string fileName, int playId, int volumeLevel, int balance)
		{
			// Create an event argument class identified by the playId
			OggPlayEventArgs OggPlayArgs = new OggPlayEventArgs(playId, fileName, volumeLevel, balance);

			// Decode the Ogg Vorbis file in a separate thread
            OggPlayThreadInfo OggPlayThread = new OggPlayThreadInfo(
				OggPlayArgs, fileName, OggFileSampleSize == SampleSize.EightBits ? 8 : 16,
				globalFocus, directSoundDevice, this);

            // Start the thread
            Thread PlaybackThread = new Thread(new ThreadStart(OggPlayThread.PlayOggDecodeThreadProc));
            if (null != PlaybackThread)
            {
                playbackThreads.Add(PlaybackThread);
                PlaybackThread.Start();
            }
            return OggPlayThread;
		}


		public OggPlayThreadInfo PlayOggFile(string fileName, int playId, int volumeLevel, int balance, bool lf)
		{
			// Create an event argument class identified by the playId
			OggPlayEventArgs OggPlayArgs = new OggPlayEventArgs(playId, fileName, volumeLevel, balance);
OggPlayArgs.LoopFlag = lf;
			// Decode the Ogg Vorbis file in a separate thread
            OggPlayThreadInfo OggPlayThread = new OggPlayThreadInfo(
				OggPlayArgs, fileName, OggFileSampleSize == SampleSize.EightBits ? 8 : 16,
				globalFocus, directSoundDevice, this);

            // Start the thread
            Thread PlaybackThread = new Thread(new ThreadStart(OggPlayThread.PlayOggDecodeThreadProc));
            if (null != PlaybackThread)
            {
                playbackThreads.Add(PlaybackThread);
                PlaybackThread.Start();
            }
            return OggPlayThread;
		}

        //[Description(1, "Plays the specified Ogg Vorbis data from the specified memory stream instead of a file."),
        //Remarks("See the overloaded <b>PlayOggFile</b> method for more details."),
        //ReturnValue("Returns immediately, decoding and playback are done in a separate thread.")]
        public OggPlayThreadInfo PlayOggFile(byte[] data, int playId, int volumeLevel, int balance)
		{
			// Create an event argument class identified by the playId
			OggPlayEventArgs OggPlayArgs = new OggPlayEventArgs(playId, volumeLevel, balance);

			// Decode the Ogg Vorbis memory stream in a separate thread
			OggPlayThreadInfo OggPlayThread = new OggPlayThreadInfo(
				OggPlayArgs, data, OggFileSampleSize == SampleSize.EightBits ? 8 : 16,
				globalFocus, directSoundDevice, this);

            // Start the thread
			Thread PlaybackThread = new Thread(new ThreadStart(OggPlayThread.PlayOggDecodeThreadProc));
            if (null != PlaybackThread)
            {
                playbackThreads.Add(PlaybackThread);
                PlaybackThread.Start();
            }
return OggPlayThread; //give the caller access to the SBuffer
        }


        public OggPlayThreadInfo PlayOggFile(byte[] data, int playId, int volumeLevel, int balance, bool lf)
		{
			// Create an event argument class identified by the playId
			OggPlayEventArgs OggPlayArgs = new OggPlayEventArgs(playId, volumeLevel, balance);
OggPlayArgs.LoopFlag = lf;
			// Decode the Ogg Vorbis memory stream in a separate thread
			OggPlayThreadInfo OggPlayThread = new OggPlayThreadInfo(
				OggPlayArgs, data, OggFileSampleSize == SampleSize.EightBits ? 8 : 16,
				globalFocus, directSoundDevice, this);

            // Start the thread
			Thread PlaybackThread = new Thread(new ThreadStart(OggPlayThread.PlayOggDecodeThreadProc));
            if (null != PlaybackThread)
            {
                playbackThreads.Add(PlaybackThread);
                PlaybackThread.Start();
            }
return OggPlayThread; //give the caller access to the SBuffer
        }

        //[Description(1, "Blocks the calling thread until all outstanding playback threads terminate, while " +
        //"continuing to perform standard message pumping."),
        //Remarks("Call this function after calling the <b>StopOggFile</b> method on all of your Ogg Vorbis files.")]
        public void WaitForAllOggFiles()
        {
            // Wait for all playback threads to terminate
            foreach (Object obj in playbackThreads)
            {
                Thread PlaybackThread = (Thread)obj;
                if (null != PlaybackThread)
                    PlaybackThread.Join();
            }
        }

        //[Description(1, "Blocks the calling thread until all outstanding playback threads terminate, or the " +
        //"specified time elapses, while continuing to perform standard <b>SendMessage</b> pumping."),
        //Remarks("See the overloaded <b>WaitForAllOggFiles</b> method for more details.")]
        public void WaitForAllOggFiles(TimeSpan timeOut)
        {
            // Wait for all playback threads to terminate
            foreach (Object obj in playbackThreads)
            {
                Thread PlaybackThread = (Thread)obj;
                if (null != PlaybackThread)
                    PlaybackThread.Join(timeOut);
            }
        }

        //[Description(1, "Stops the specified Ogg Vorbis file. playId is the Id of the Ogg Vorbis file to stop playback on.")]
        public void StopOggFile(int playId)
        {
            // Let the playback thread know we want to cancel playback
            OggPlayEventArgs StopArgs = new OggPlayEventArgs(playId, true);
            StopOggFileNow(this, StopArgs);
        }

        #endregion

		/// <summary>
        /// Class for decoding and playing back of Ogg audio data. This class contains the method
        /// used for the thread start call in the PlayOggFile method of the OggPlayManager class.
		/// </summary>
		public class OggPlayThreadInfo
		{
			#region Members and events code
public delegate void stopEventHandler();
public event stopEventHandler stopEvent;

			private string fileName;
			private byte[] memFile;
			private int bitsPerSample;  // either 8 or 16
			private bool stopNow;
private bool isPlaying=true;
            private bool wantGlobalFocus;
            private DirectSound directSoundDevice;
public OggPlayEventArgs oggInfo;
			private OggPlayManager oggPlay;
public SecondarySoundBuffer SecBuf;
public bool IsPlaying {
get { return isPlaying; }
}

			#endregion

			#region Initialization code

            //[Description(1, "Overloaded constructor. Validates parameters and initializes object members " +
            //"that are common whether your are loading from a file or memory.")]
			public OggPlayThreadInfo(OggPlayEventArgs oggInfo, int bitsPerSample, 
                bool wantGlobalFocus, DirectSound directSound, OggPlayManager oggPlay)
			{
				// Verify parameters
				if (null == oggInfo)
                    throw new System.ArgumentNullException("oggInfo");

                if (null == directSound)
                    throw new System.ArgumentNullException("directSound");

                if (null == oggPlay)
                    throw new System.ArgumentNullException("oggPlay");

                // Initialize this objects data members
				this.oggInfo = oggInfo;
				this.bitsPerSample = bitsPerSample;
				this.directSoundDevice = directSound;
                this.wantGlobalFocus = wantGlobalFocus;
                this.oggPlay = oggPlay;
                
				// Add the interrupt event handler
				oggPlay.StopOggFileNow += new EventHandler<OggPlayEventArgs>(InterruptOggFilePlayback);
			}

            //[Description(1, "Overloaded constructor. Validates parameters and initializes object members " +
            //"when you want to load Ogg audio data from a file.")]
            public OggPlayThreadInfo(OggPlayEventArgs oggInfo, string fileName,
                int bitsPerSample, bool wantGlobalFocus, DirectSound directSound, OggPlayManager oggPlay)
                : this(oggInfo, bitsPerSample, wantGlobalFocus, directSound, oggPlay)
            {
                if (null == fileName)
                    throw new System.ArgumentNullException("fileName");
                this.fileName = fileName;
            }

            //[Description(1, "Constructor. Validates parameters and initializes object members " +
            //"when you want to load Ogg audio data from memory.")]
            public OggPlayThreadInfo(OggPlayEventArgs oggInfo, byte[] memFile,
                int bitsPerSample, bool wantGlobalFocus, DirectSound directSound, OggPlayManager oggPlay)
                : this(oggInfo, bitsPerSample, wantGlobalFocus, directSound, oggPlay)
            {
                if (null == memFile)
                    throw new System.ArgumentNullException("memFile");
                this.memFile = memFile;
                this.fileName = string.Empty;
            }

			#endregion

			#region Interrupt handler code

			//[Description(1, "Stops thread playback thread immediately.")]
			public void InterruptOggFilePlayback(object sender, OggPlayEventArgs e)
			{
				if (e.PlayId == oggInfo.PlayId)
				{
					stopNow = true;
				}
			}

			#endregion

            #region Decode and playback code

            //[Description(1, "Playback thread for use by the PlayOggFile method of OggPlayManager class.")]
			public void PlayOggDecodeThreadProc()
			{
				unsafe 
				{
					void *vf = null;
SecBuf = null;
					SoundBufferDescription MyDescription;
                    MemoryStream PcmStream = null;
                    int ErrorCode = 0;

                        if (null != memFile)
                            ErrorCode = NativeMethods.memory_stream_for_ogg_decode(memFile, memFile.Length, &vf);
                        else if (null != fileName)
                            ErrorCode = NativeMethods.init_for_ogg_decode(fileName, &vf);
                        int PcmBytes = -1, ChannelsCount = 0, SamplingRate = 0,
                            AverageBytesPerSecond = 0, BlockAlign = 0;

                        // Vorbisfile API documentation recommends 
                        // a PCM buffer of 4096 bytes.
                        byte[] PcmBuffer = new byte[4096];
                        PcmStream = new MemoryStream();
                        WaveFormat MyWaveFormat = new WaveFormat();
                        // Decode the Ogg Vorbis data into its PCM data
                        while (PcmBytes != 0)
                        {
                            // Get the next chunk of PCM data, pin these so the GC can't 
                            // relocate them.
                            fixed (byte* buf = &PcmBuffer[0])
                            {
                                fixed (int* HoleCount = &oggInfo.ErrorHoleCount)
                                {
                                    fixed (int* BadLinkCount = &oggInfo.ErrorBadLinkCount)
                                    {
                                        // The sample size of the returned PCM data -- either 8-bit 
                                        // or 16-bit samples -- is set by BitsPerSample.
                                        PcmBytes = NativeMethods.ogg_decode_one_vorbis_packet(
                                            vf, buf, PcmBuffer.Length,
                                            bitsPerSample,
                                            &ChannelsCount, &SamplingRate,
                                            HoleCount, BadLinkCount);
                                    }
                                }
                            }
PcmStream.Write(PcmBuffer, 0, PcmBytes);
} //while pcmBytes!=0

                                // Handle first time through this method

                                // Compute format items
                                BlockAlign = ChannelsCount * (bitsPerSample / 8);
                                AverageBytesPerSecond = SamplingRate * BlockAlign;

                                // Set the format items
                                MyWaveFormat.AverageBytesPerSecond = AverageBytesPerSecond;
                                MyWaveFormat.BitsPerSample = (short)bitsPerSample;
                                MyWaveFormat.BlockAlignment = (short)BlockAlign;
                                MyWaveFormat.Channels = (short)ChannelsCount;
                                MyWaveFormat.SamplesPerSecond = SamplingRate;
                                MyWaveFormat.FormatTag = WaveFormatTag.Pcm;

                                // Describe the capabilities of this buffer
                                MyDescription = new SoundBufferDescription();
                                MyDescription.Format = MyWaveFormat;
                                MyDescription.SizeInBytes =(int)PcmStream.Length;
                                MyDescription.Flags = BufferFlags.ControlVolume
|                                BufferFlags.ControlPan
|                                BufferFlags.GlobalFocus;
PcmStream.Position = 0;
SecBuf=new SecondarySoundBuffer(directSoundDevice, MyDescription);
SecBuf.Write(PcmStream.ToArray(),
             0, LockFlags.EntireBuffer);
SecBuf.Volume = oggInfo.VolumeLevel;
oggInfo.WaitFlag.WaitOne(); //if this is a consecutive track,
//pause here until we are told
//that we can play this thread.

while (true)
{
if (stopNow)
break;
SecBuf.CurrentPlayPosition = 0;
	SecBuf.Play(0, PlayFlags.None);

while (SecBuf.Status == BufferStatus.Playing)
{
if (stopNow)
break;
Thread.Sleep(10);
} //while buffer playing
if (! oggInfo.LoopFlag)
break;
} //while
isPlaying=false;
if (!stopNow)
this.stopEvent();
//don't play consecutive track if want to stop playback.

	NativeMethods.final_ogg_cleanup(vf);
	if (SecBuf!=null)
	SecBuf.Stop();
SecBuf = null;

PcmStream = null;

} //unsafe
                    } //method

#endregion

} //class
} //class
} //namespace