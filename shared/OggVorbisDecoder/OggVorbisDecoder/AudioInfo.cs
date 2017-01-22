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

namespace OggVorbisDecoder
{
    /// <summary>
    /// Contains simple audio file information
    /// </summary>
    public class AudioInfo
    {
        private int rate;
        private int channels;
        private double duration;

        /// <summary>
        /// Sampling rate (or frequency)  
        /// </summary>
        public virtual int Rate
        {
            get { return rate; }
        }

        /// <summary>
        /// Number of channels (1 for mono, 2 for stereo... )
        /// </summary>
        public virtual int Channels
        {
            get { return channels; }
        }

        /// <summary>
        /// Duration of audio sample in seconds
        /// </summary>
        public virtual double Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        /// <summary>
        /// Constructs the audio information
        /// </summary>
        /// <param name="rate">Sampling rate (or frequency)</param>
        /// <param name="channels">Number of channels (1 for mono, 2 for stereo... )</param>
        /// <param name="duration">Duration of audio sample in seconds</param>
        public AudioInfo(int rate, int channels, double duration)
        {
            this.rate = rate;
            this.channels = channels;
            this.duration = duration;
        }
    }
}
