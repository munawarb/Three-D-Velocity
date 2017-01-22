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
using System.Runtime.InteropServices;

namespace OggVorbisDecoder
{

	[StructLayout(LayoutKind.Sequential)]
	internal class NativeVorbisInfo
	{
		internal int Version;
		internal int Channels;
		internal int Rate;
		internal int BitrateUpper;
		internal int BitrateNominal;
		internal int BitrateLower;
		internal int BitrateWindow;
		internal IntPtr CodecSetup;
	}

	public class VorbisInfo : AudioInfo, IDisposable
	{
		#region Variables
        private NativeVorbisInfo info;
        private bool disposed = false;
		#endregion

		#region Properties

		public int Version 
		{
			get { return this.info.Version; }
		}
	
        public override int Channels 
		{
			get { return this.info.Channels; }
		}

        public override int Rate 
		{
			get { return this.info.Rate; }
		}

		public int BitrateUpper 
		{
			get { return this.info.BitrateUpper; }
		}

		public int BitrateNominal 
		{
			get { return this.info.BitrateNominal; }
		}
	
		public int BitrateLower 
		{
			get { return this.info.BitrateLower; }
		}
		#endregion

        #region Constructors
        public VorbisInfo () : base(0,0,0)
	    {
            info = new NativeVorbisInfo();
            NativeInitialize(info);
	    }

        public VorbisInfo(IntPtr existingNative): base(0,0,0)
        {
            info = (NativeVorbisInfo)Marshal.PtrToStructure(existingNative, typeof(NativeVorbisInfo)); 

        }
        #endregion

        #region Native Methods

        [DllImport(Externals.VorbisLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "vorbis_info_init")]
		private static extern void NativeInitialize([In,Out] NativeVorbisInfo vi);

        [DllImport(Externals.VorbisLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "vorbis_info_clear")]
		private static extern void NativeClear([In,Out] NativeVorbisInfo vi);

        [DllImport(Externals.VorbisLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "vorbis_info_blocksize")]
		private static extern int NativeBlockSize([In] NativeVorbisInfo vi, int zo);

		#endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposeManagedResources)
        {
            if (!this.disposed)
            {
                if (disposeManagedResources)
                {
                    // dispose managed resources
                }
                // dispose unmanaged resources
                NativeClear(info);
                disposed = true;
            }

        }


        #endregion
	}
}
