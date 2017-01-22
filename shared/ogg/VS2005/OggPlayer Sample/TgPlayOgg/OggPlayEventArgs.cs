using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TG.Sound
{
    //[Description(1, "<b>EventArgs</b> class for event notification when the <b>PlayOggFile</b> method completes."),
    //Remarks("The Ogg Vorbis decoder may have encountered errors while decoding the Ogg Vorbis data. " +
    //"The two error counts are for information purposes only. If successful the created waveform data " +
	//"was played, but it may not have sounded as intended if either of these two counts are nonzero."),
    //Example("http://developer.traygames.com/Docs/?doc=OggLib")]
    public class OggPlayEventArgs : EventArgs
	{
		#region Members and events code

        private bool success;
private bool loopFlag;
private AutoResetEvent waitFlag =
new AutoResetEvent(false);
        private string reasonForFailure;
        private int playId;
        private string fileName;
        private bool stopRequest;
        private float volumeLevel;
        private int balance;

        //[Description(1, "Count of encountered <b>OV_HOLE</b> errors during " +
        //"decoding indicates there was an interruption in the data. "),
        //Remarks("See the Vorbis SDK documentation for more details.")]
        public int ErrorHoleCount;

        //[Description(1, "Count of encountered <b>OV_EBADLINK</b> errors during decoding " +
        //"indicates that an invalid stream section was supplied to libvorbisfile, " +
        //"or the requested link is corrupt."),
        //Remarks("See the Vorbis SDK documentation for more details.")]
        public int ErrorBadLinkCount;

		#endregion

		#region Properties code	

        //[Description(1, "Indicates successful playback or stop request.")]
        public bool Success 
		{
			get { return success; }
			set { success = value; }
		}
public AutoResetEvent WaitFlag
{
get { return waitFlag; }
set { waitFlag = value; }
}
        public bool LoopFlag
		{
			get { return loopFlag; }
			set { loopFlag = value; }
		}

        //[Description(1, "A detailed explanation for the failure, if any.")]
        public string ReasonForFailure 
		{
			get { return reasonForFailure; }
			set { reasonForFailure = value; }
		}

        //[Description(1, "Value of the <b>playID</b> parameter assigned to the Ogg Vorbis file."),
        //Remarks("This property is read only.")]
        public int PlayId 
		{
			get { return playId; }
        }

        //[Description(1, "Fully qualified name of the Ogg Vorbis file that is playing now " +
        //"or has finished. If this object is being used to playback from memory this member " +
        //"will be set to the string 'Played from memory buffer'."),
        //Remarks("This property is read only.")]
        public string FileName
        {
            get { return fileName; }
        }

        //[Description(1, "Indicates the client wants to stop playback immediately.")]
        public bool StopRequest
        {
            get { return stopRequest; }
            set { stopRequest = value; }
        }

        //[Description(1, "Can be set in a range from 0 (full volume) to -10,000 (completely silent)."),
        //Remarks("0 is the default value.")]
        public float VolumeLevel
        {
            get { return volumeLevel; }
        }

        //[Description(1, "Can be set in a range from -10,000 (full left) to -10,000 (full right)."),
        //Remarks("0 (center) is the default value.")]
        public int Balance
        {
            get { return balance; }
        }

        #endregion

		#region Initialization code

        //[Description(1, "Constructor. Initializes object by allowing all properties to be set.")]
        public OggPlayEventArgs(bool success, string reasonForFailure, int Id, string fileName,
            float volumeLevel, int balance, int errorHoleCount, int errorBadLinkCount, bool stopRequest)
        {
            this.success = success;
            this.reasonForFailure = reasonForFailure;
            this.playId = Id;
            this.fileName = fileName;
            this.volumeLevel = volumeLevel;
            this.balance = balance;
            this.stopRequest = stopRequest;
            this.ErrorBadLinkCount = errorBadLinkCount;
            this.ErrorHoleCount = errorHoleCount;
        }

        //[Description(1, "Overloaded constructor. You set only the <b>playId</b> for the file.")]
        public OggPlayEventArgs(int Id)
		{
            this.playId = Id;
            this.fileName = "Played from memory buffer";
            this.ReasonForFailure = "Unknown";
        }

        //[Description(1, "Overloaded constructor. You set the <b>playId</b> and a flag indicating " +
        //"whether or not you want to stop playback.")]
        public OggPlayEventArgs(int Id, bool stopRequest)
        {
            this.playId = Id;
            this.fileName = "Unknown";
            this.stopRequest = stopRequest;
            this.Success = true;
            this.ReasonForFailure = "This was a stop request.";
        }

        //[Description(1, "Overloaded constructor. You set the <b>playId</b> and the name of the " +
        //"Ogg Vorbis audio file.")]
        public OggPlayEventArgs(int Id, string fileName)
            : this(Id)
        {
            this.fileName = fileName;
        }

        //[Description(1, "Overloaded constructor. You set the <b>playId</b>, the volume level and balance.")]
        public OggPlayEventArgs(int Id, float volumeLevel, int balance)
            : this(Id)
        {
            this.volumeLevel = volumeLevel;
            this.balance = balance;
        }

        //[Description(1, "Overloaded constructor. You set the <b>playId</b>, name of the " +
        //"Ogg Vorbis audio file, the volume level and balance.")]
        public OggPlayEventArgs(int Id, string fileName, float volumeLevel, int balance)
            : this(Id, fileName)
        {
            this.volumeLevel = volumeLevel;
            this.balance = balance;
        }

        //[Description(1, "Copy Constructor. Initializes object from passed object.")]
        public OggPlayEventArgs(OggPlayEventArgs e)
            : this(e.success, e.reasonForFailure, e.PlayId, e.fileName, e.volumeLevel, 
                e.balance, e.ErrorHoleCount, e.ErrorBadLinkCount, e.stopRequest)
		{
        }

        #endregion
	}
}
