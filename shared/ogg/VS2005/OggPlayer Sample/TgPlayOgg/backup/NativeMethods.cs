using System;
using System.Runtime.InteropServices;

namespace TG.Sound
{
	/// <summary>
	/// External C functions and error codes in the TgPlayOgg_vorbisfile unmanaged DLL.
	/// </summary>
	internal class NativeMethods
	{
		private NativeMethods() 
		{ // Disallow instantiation of this class
		}

		public const int
			ifod_err_open_failed = 1,              // open file failed
			ifod_err_malloc_failed = 2,            // malloc() call failed; out of memory
			ifod_err_read_failed = 3,              // A read from media returned an error     
			ifod_err_not_vorbis_data = 4,          // Bitstream is not Vorbis data       
			ifod_err_vorbis_version_mismatch = 5,  // Vorbis version mismatch
			ifod_err_invalid_vorbis_header = 6,    // Invalid Vorbis bitstream header
			ifod_err_internal_fault = 7,           // Internal logic fault; indicates a bug or heap/stack corruption
			ifod_err_unspecified_error = 8;        // ov_open() returned an undocumented error

		// Initialization for decoding the given Ogg Vorbis file name.
		[DllImport("TgPlayOgg_vorbisfile.dll", CharSet=CharSet.Unicode,
             CallingConvention=CallingConvention.Cdecl)]
        public unsafe static extern int init_for_ogg_decode(
			string fileName, void** vf_out);

		// Initialization for decoding the given Ogg Vorbis memory stream.
		[DllImport("TgPlayOgg_vorbisfile.dll", CharSet=CharSet.Unicode,
			 CallingConvention=CallingConvention.Cdecl)]
		public unsafe static extern int memory_stream_for_ogg_decode(
			byte[] stream, int sizeOfStream, void** vf_out);

		// Writes Pulse Code Modulation (PCM) data into the given buffer beginning 
		// at buf_out[0].
        [DllImport("TgPlayOgg_vorbisfile.dll", CallingConvention=CallingConvention.Cdecl)]
        public unsafe static extern int ogg_decode_one_vorbis_packet(
            void* vf_ptr, void* buf_out, int buf_byte_size, 
			int bits_per_sample, int* channels_cnt, int* sampling_rate, 
			int* err_ov_hole_cnt, int* err_ov_ebadlink_cnt);

		//	Free the memory pointed to by vf_out and also close the Ogg Vorbis file 
		//	opened by init_for_ogg_decode(). OK to call with null vf_ptr parameter.
        [DllImport("TgPlayOgg_vorbisfile.dll", CallingConvention=CallingConvention.Cdecl)]
        public unsafe static extern int final_ogg_cleanup(void* vf_ptr);
	}
}
