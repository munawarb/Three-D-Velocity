/*
 * Copyright © 2008, Atachiants Roman (kelindar@gmail.com)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 *
 *    - Redistributions of source code must retain the above copyright notice, 
 *      this list of conditions and the following disclaimer.
 * 
 *    - Redistributions in binary form must reproduce the above copyright notice, 
 *      this list of conditions and the following disclaimer in the documentation 
 *      and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
 * IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT 
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, 
 * OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY 
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OggVorbisDecoder
{

    public class OggVorbisMemoryStream : MemoryStream
    {
        #region Properties
        private VorbisInfo info = null;
        private byte[] buffer = null;
        private long rawLength = 0;
        private double duration = 0;
        
        /// <summary>
        /// Current file information
        /// </summary>
        public VorbisInfo Info
        {
            get { return info; }
        }

        public long RawLength
        {
            get { return rawLength; }
        }

        public double Duration
        {
            get { return duration; }
        }

        public double Time
        {
            get { return (double)((Position * Duration) / Length); }
        }
        #endregion

        #region Constructors and Initialization

        public OggVorbisMemoryStream(byte[] buffer, VorbisInfo info, long rawLength, double duration)
            : base(buffer)
        {
            this.buffer = buffer;
            this.info = info;
            this.rawLength = rawLength;
            this.duration = duration;
        }

        #endregion

        #region Static Helpers

        public static OggVorbisMemoryStream LoadFromFile(string filename)
        {
            //load from file
            OggVorbisFileStream fileStream = new OggVorbisFileStream(filename);
            VorbisInfo ninfo = fileStream.Info;

            //read to memory
            byte[] nbuffer = new byte[fileStream.Length];
            //fileStream.Read(nbuffer, 0, nbuffer.Length);
            nbuffer = ReadWholeArray(fileStream, nbuffer);

            OggVorbisMemoryStream memoryStream = new OggVorbisMemoryStream(nbuffer, ninfo, fileStream.RawLength, fileStream.Duration);

            //clean up
            fileStream.Close();

            return memoryStream;
        }

        public static byte[] ReadStream(OggVorbisFileStream stream)
        {
            byte[] readBuffer = new byte[4096];
            if (stream.Read(readBuffer, 0, 4096) > 0)
            {
                return readBuffer;
            }
            return null;
        }

        public static byte[] ReadWholeArray(OggVorbisFileStream stream, byte[] buffer)
        {
            List<byte> data = new List<byte>();
            byte[] readBuffer = null;
            while ((readBuffer = ReadStream(stream)) != null)
            {
                data.AddRange(readBuffer);
            }
            return data.ToArray();
        }
        #endregion

    }
}
