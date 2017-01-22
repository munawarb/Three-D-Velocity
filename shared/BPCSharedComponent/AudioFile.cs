/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using SharpDX;
using SharpDX.Multimedia;
using System.Windows.Forms;
namespace BPCSharedComponent.ExtendedAudio
{
	/// <summary>
	/// Represents a wave file.
	/// </summary>
	public class AudioFile
	{
		private byte[] rawWaveData;
		private BinaryReader stream;
		private string chunkID;
		private int chunkSize;
		private short tag;
		private short channels;
		private int samplingRate;
		private int bytesPerSecond;
		private short blockAlign;
		private short bitsPerSample;
		private long dataPosition;
		//used to reference start of wave data.
		private int dataLength;
		//how long is the wave data series?
		private long chunkStartPosition;
		//used to reference start of current chunk for moveNext method

		//Loads a file from a Stream class.
		public AudioFile(Stream s)
		{
			try
			{
				stream = new BinaryReader(s);
				stream.BaseStream.Position = 0;
				prepareForProcessing();
				//everything is ready now.
				getData(true); //get the raw wave data
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message + "\n" + e.StackTrace,
							   "Error in BPCShared Component.dll.",
							  MessageBoxButtons.OK,
							 MessageBoxIcon.Error);
			}
		}

		//Loads a file from a byte array.
		public AudioFile(byte[] s)
			: this(new MemoryStream(s))
		{

		}

		/*The method below can allow the stream position
		to be any where, since it will reset it before moving forward
		to calculate the new offset properly.
		Therefore, this method will always update the global position variable and data chunk variables, so
		it's not recommended to move to another chunk without the use of this method.
		*/
		private void moveToNextChunk()
		{
			stream.BaseStream.Position = chunkStartPosition; //if we did some reading,
			//reset the position so we can move properly.
			if (chunkSize != 0) //not first time calling method
				stream.BaseStream.Seek(chunkSize, SeekOrigin.Current);

			chunkID = new string(stream.ReadChars(4));
			chunkSize = stream.ReadInt32();
			chunkStartPosition = stream.BaseStream.Position;
		}

		//the method below is used only for preprocessing.
		//the overloaded method willinitialize the array
		//For instance, this method will mark the start and end offsets of the wave data;
		//the overloaded method will actually fill the buffer array.
		//Therefore, this method assumes that the stream as at the start of the wave data.
		private void getData()
		{
			dataPosition = stream.BaseStream.Position;
			dataLength = chunkSize;
		}

		//This method will fill the rawWaveData array with wave data obtained from the 'data' chunk.
		private void getData(bool fillArray)
		{
			stream.BaseStream.Position = dataPosition;
			rawWaveData = new byte[dataLength];
			for (int i = 0; i < dataLength; i++)
				rawWaveData[i] = stream.ReadByte();
		}

		//Used if a calling method does not want to construct a WaveFormat object--
		//instead, it wants the format values by themselves.
		//(I don't know why anyone would do it,
		//since you can just read the values from WaveFormat, but in case
		//we need it, there it is. Smile)
		public void getAudioFormat(ref short tag,
								   ref short channels,
								   ref int samplingRate,
								   ref int bytesPerSecond,
								   ref short blockAlign,
								   ref short bitsPerSample)
		{
			tag = this.tag;
			channels = this.channels;
			samplingRate = this.samplingRate;
			bytesPerSecond = this.bytesPerSecond;
			blockAlign = this.blockAlign;
			bitsPerSample = this.bitsPerSample;
		}

		//Initialize the class fields with the format for the wave file.
		//the overloaded method above will fill the reference variables
		//with this information.
		//Therefore, this method should be called before the overloaded method above is called.
		//note: this method assumes that the stream is already
		//in the format chunk.
		private void getAudioFormat()
		{
			tag = stream.ReadInt16();
			channels = stream.ReadInt16();
			samplingRate = stream.ReadInt32();
			bytesPerSecond = stream.ReadInt32();
			blockAlign = stream.ReadInt16();
			bitsPerSample = stream.ReadInt16();
		}

		//Here, we tie everything together...Finally!
		private void prepareForProcessing()
		{
			do
			{
				moveToNextChunk();
				if (chunkID.Equals("fmt "))
					getAudioFormat();
				if (chunkID.Equals("data"))
					getData();
				if (chunkID.Equals("RIFF"))
				{
					//Crawl past the RIFF chunk, since we don't really
					//need it.
					stream.ReadChars(4);
					chunkSize = 4;
				}
				if (chunkStartPosition + chunkSize >= stream.BaseStream.Length)
					break; //last chunk, don't read past the end
			}
			while (stream.BaseStream.Position < stream.BaseStream.Length);
		}

		/*[ANOTHER NOTE to SharpDX]: the method below contains byte eg. unsigned 8 bit data, but the link I provided online said that
		it's important to use 8 bit for strictly 8 bit data, and 16 bit
		eg. short[] for sixteen bit data.
		When I tried loading a SecondarySoundBuffer using short[], it failed, but byte[] so far works
		for both 8 and 16 bit data...I've loaded 16 bit waves using this same byte[] rawWaveData array.
		*/
		public byte[] getRawWaveData()
		{
			return (rawWaveData);
		}

		public void close()
		{
			stream.Close();
			stream = null;
			rawWaveData = null;
		}

		public WaveFormat format()
		{
			WaveFormat w = new WaveFormat(samplingRate, bitsPerSample, channels);
			return (w);
		}
	} //class
} //namespace
