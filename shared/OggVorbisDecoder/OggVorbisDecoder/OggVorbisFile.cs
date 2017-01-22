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

using System.IO;
using System.Security;
using System.Collections.Generic;

namespace OggVorbisDecoder
{
    [StructLayout(LayoutKind.Sequential)]
	internal struct NativeOggSyncState
	{
		internal IntPtr	Data;
		internal int Storage;
		internal int Fill;
		internal int Returned;
		internal int Unsyncronized;
		internal int HeaderBytes;
		internal int BodyBytes;
	}

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeOggStreamState
    {
        internal IntPtr BodyData;
        internal int BodyStorage;
        internal int BodyFill;
        internal int BodyReturned;
        internal IntPtr LacingValues;
        internal IntPtr GranuleValues;
        internal int LacingStorage;
        internal int LacingFill;
        internal int LacingPacket;
        internal int LacingReturned;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 282)]
        internal byte[] Header;
        internal int HeaderFill;
        internal int EOS;
        internal int BOS;
        internal int SerialNumber;
        internal int PageNumber;
        internal long PacketNumber;
        internal long GranulePosition;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeVorbisDspState
    {
        internal int AnalysIsp;
        internal IntPtr VorbisInfo;
        internal IntPtr Pcm;	
        internal IntPtr PcmRet;	
        internal int PcmStorage;
        internal int PcmCurrent;
        internal int PcmReturned;
        internal int Preextrapolate;
        internal int EOFFlag;
        internal int LW;
        internal int W;
        internal int NW;
        internal int CenterW;
        internal long Hranulepos;
        internal long Sequence;
        internal long GlueBits;
        internal long TimeBits;
        internal long FloorBits;
        internal long ResBits;
        internal IntPtr BackendState;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeOggPackBuffer
    {
        internal int EndByte;
        internal int EndBit;
        internal IntPtr Buffer;
        internal IntPtr Ptr;
        internal int Storage;
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeVorbisBlock
    {
        internal IntPtr Pcm;
        internal NativeOggPackBuffer Opb;
        internal int LW;
        internal int W;
        internal int NW;
        internal int PcmEnd;
        internal int Mode;
        internal int EOFFlag;
        internal long Granulepos;
        internal long Sequence;
        internal IntPtr VD;
        internal IntPtr LocalStore;	
        internal int LocalTop;
        internal int LocalAlloc;
        internal int TotalUse;
        internal IntPtr Reap;
        internal int GlueBits;
        internal int TimeBits;
        internal int FloorBits;
        internal int ResBits;
        internal IntPtr InternalPtr;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class NativeOggVorbisFile
    {
        internal IntPtr DataSource;	
        internal int IsSekable;
        internal long Offset;
        internal long End;
        internal NativeOggSyncState SyncState;
        internal int Links;
        internal IntPtr Offsets;	
        internal IntPtr DataOffsets;
        internal IntPtr SerialNos;	
        internal IntPtr PcmLengths;	
        internal IntPtr VorbisInfo;
        internal IntPtr VorbisComments;
        internal long PcmOffset;
        internal int ReadyState;
        internal int CurrentSerialNo;
        internal int CurrentLink;
        internal double BitTrack;
        internal double SampleTrack;
        internal NativeOggStreamState StreamState;
        internal NativeVorbisDspState DspState;
        internal NativeVorbisBlock VorbisBlock;
        internal NativeCallbacks Callbacks;
    }

    internal static class OggVorbisFile
    {

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_clear"), SuppressUnmanagedCodeSecurity]
        internal static extern int NativeClear([In, Out] NativeOggVorbisFile vf);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_seekable"), SuppressUnmanagedCodeSecurity]
        internal static extern int NativeIsSeekable([In, Out] NativeOggVorbisFile vf);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_open_callbacks"), SuppressUnmanagedCodeSecurity]
        internal static extern int NativeOpenCallbacks(byte[] datasource, [In, Out] NativeOggVorbisFile vf, IntPtr initial, int ibytes, NativeCallbacks callbacks);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_fopen"), SuppressUnmanagedCodeSecurity]
        internal static extern int NativeFOpen([In] string path, [In, Out] NativeOggVorbisFile vf);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_info"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr NativeGetInfo([In, Out] NativeOggVorbisFile vf, int link);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_comment"), SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr NativeGetComment([In, Out] NativeOggVorbisFile vf, int link);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_pcm_total"), SuppressUnmanagedCodeSecurity]
        internal static extern long NativeGetLength([In, Out] NativeOggVorbisFile vf, int i);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_raw_total"), SuppressUnmanagedCodeSecurity]
        internal static extern long NativeGetRawLength([In, Out] NativeOggVorbisFile vf, int i);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_pcm_seek_page_lap"), SuppressUnmanagedCodeSecurity]
        internal static extern int NativeSeek([In, Out] NativeOggVorbisFile vf, long pos);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_time_seek_page_lap"), SuppressUnmanagedCodeSecurity]
        internal static extern int NativeSeekTime([In, Out] NativeOggVorbisFile vf, double pos);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_pcm_tell"), SuppressUnmanagedCodeSecurity]
        internal static extern long NativeGetPosition([In, Out] NativeOggVorbisFile vf);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_time_tell"), SuppressUnmanagedCodeSecurity]
        internal static extern double NativeGetTime([In, Out] NativeOggVorbisFile vf);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_time_total"), SuppressUnmanagedCodeSecurity]
        internal static extern double NativeGetDuration([In, Out] NativeOggVorbisFile vf, int i);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_crosslap"), SuppressUnmanagedCodeSecurity]
        internal static extern long NativeCrosslap([In, Out] NativeOggVorbisFile old_vf, [In, Out] NativeOggVorbisFile new_vf);

        [DllImport(Externals.VorbisFileLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ov_read"), SuppressUnmanagedCodeSecurity]
        internal static unsafe extern int NativeRead([In, Out] NativeOggVorbisFile vf, byte* buffer, int length, int bigendianp, int word, int sgned, IntPtr bitstream);

    }


}