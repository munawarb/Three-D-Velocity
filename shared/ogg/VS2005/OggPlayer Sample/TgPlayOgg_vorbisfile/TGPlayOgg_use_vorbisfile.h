//	Copyright (c) 2003-2005 TrayGames Corp. 
//	All rights reserved. Reproduction or transmission of this file, or a portion
//	thereof, is forbidden without prior written permission of TrayGames Corp.
//
//	Author: Perry L. Marchant
//	Date:	August 12 2005

// Error codes returned by Vorbisfile API open calls
const int
	ifod_err_open_failed = 1,              // Open file failed
	ifod_err_malloc_failed = 2,            // Malloc() call failed; out of memory?
	ifod_err_read_failed = 3,              // A read from media returned an error     
	ifod_err_not_vorbis_data = 4,          // Bitstream is not Vorbis data       
	ifod_err_vorbis_version_mismatch = 5,  // Vorbis version mismatch
	ifod_err_invalid_vorbis_header = 6,    // Invalid Vorbis bitstream header
	ifod_err_internal_fault = 7,           // Internal logic fault; indicates a bug or heap/stack corruption
	ifod_err_unspecified_error = 8;        // ov_open() returned an undocumented error

// Struct that contains the pointer to our Ogg Vorbis file in memory
typedef struct _OggMemoryFile
{
	unsigned char*	dataPtr;	// Pointer to the data in memory
	long			dataSize;	// Size of the data
	long			dataRead;	// Bytes read so far
} OGG_MEMORY_FILE, *POGG_MEMORY_FILE;

// External Ogg Vorbis functions for .NET library
extern "C" __declspec(dllexport)
int init_for_ogg_decode(wchar_t* filename, void** vf_out);

extern "C" __declspec(dllexport)
int memory_stream_for_ogg_decode(unsigned char* stream, int sizeOfStream, void** vf_out);

extern "C" __declspec(dllexport) 
int ogg_decode_one_vorbis_packet(void* vf_ptr, void* buf_out, int buf_byte_size,
          int ogg_sample_size, int* channels_cnt, int* sampling_rate, int* err_ov_hole_cnt, int* err_ov_ebadlink_cnt);

extern "C" __declspec(dllexport) 
int final_ogg_cleanup(void *vf_ptr);

// Vorbisfile API callback functions
size_t vorbis_read(void* ptr, size_t byteSize, size_t sizeToRead, void* data_src);
int vorbis_seek(void* data_src, ogg_int64_t offset, int where);
int vorbis_close(void* data_src);
long vorbis_tell(void* data_src);