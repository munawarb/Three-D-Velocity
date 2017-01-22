//	Copyright (c) 2003-2005 TrayGames, LLC 
//	All rights reserved. Reproduction or transmission of this file, or a portion
//	thereof, is forbidden without prior written permission of TrayGames, LLC.
//
//	Author: Perry L. Marchant
//	Date:	June 2 2005

#include "stdafx.h"
#include "TgPlayOgg_use_vorbisfile.h"

ov_callbacks oggCallbacks;	// Callbacks used to read the file from a memory stream

//	Function:	init_for_ogg_decode
//
//	Remarks:	Initialization for decoding the given Ogg Vorbis file name.
//
//	Returns:	0 if successful, otherwise returns one of the ifod_err_values.
//
//	Notes:		If no error, the returned vf_out pointer is a pointer to malloc 
//				memory. Thus when all done using the returned vf_out pointer for 
//				making other calls into this DLL, be sure to call final_ogg_cleanup
//				function with this pointer.

int init_for_ogg_decode(wchar_t* filename, void** vf_out)
{
    // Initialize the memory pointer from the caller
    *vf_out = NULL;

    // Open the file for reading
    FILE* file_ptr = NULL;
	errno_t err = _wfopen_s(&file_ptr, filename, L"rb");

    if (NULL == file_ptr || 0 != err)
        return ifod_err_open_failed;

    // Get memory for holding an instance of struct OggVorbis_File
	void *vf_ptr = malloc(sizeof(OggVorbis_File));
	if (NULL == vf_ptr)
    {
        fclose(file_ptr);
		return ifod_err_malloc_failed;
    }

    // Open it
	int ov_ret = ov_open(file_ptr, static_cast<OggVorbis_File *>(vf_ptr), NULL, 0);

	if (0 > ov_ret)
	{
		// There was an error
		int err_code = ifod_err_unspecified_error;
		if (ov_ret == OV_EREAD)
			err_code = ifod_err_read_failed;
		else if (ov_ret == OV_ENOTVORBIS)
			err_code = ifod_err_not_vorbis_data;
		else if (ov_ret == OV_EVERSION)
			err_code = ifod_err_vorbis_version_mismatch;
		else if (ov_ret == OV_EBADHEADER)
			err_code = ifod_err_invalid_vorbis_header;
		else if (ov_ret == OV_EFAULT)
			err_code = ifod_err_internal_fault;
		
        // Cleanup
        fclose(file_ptr);
        free(vf_ptr);

        // Return the ifod_err_code
		return err_code;
    }

	// Copy the memory pointer to the caller
    *vf_out = vf_ptr;
	
	return 0;  // Success!
}

//	Function:	memory_stream_for_ogg_decode
//
//	Remarks:	Initialization for decoding the given Ogg Vorbis memory stream.
//
//	Returns:	0 if successful, otherwise returns one of the ifod_err_values.
//
//	Notes:		If no error, the returned vf_out pointer is a pointer to malloc 
//				memory. Thus when all done using the returned vf_out pointer for 
//				making other calls into this DLL, be sure to call final_ogg_cleanup
//				function with this pointer.

int memory_stream_for_ogg_decode(unsigned char* stream, int sizeOfStream, void** vf_out)
{
	// Get memory for holding an instance of struct OggVorbis_File
	void *vf_ptr = malloc(sizeof(OggVorbis_File));
	if (NULL == vf_ptr)
		return ifod_err_malloc_failed;

	// Save the data in the Ogg memory stream because we need this when we
	// are actually reading in the data, we haven't read anything yet!
	POGG_MEMORY_FILE oggStream = new OGG_MEMORY_FILE;	
	oggStream->dataRead = 0;
	
	// Save the size so we know how much we need to read
	oggStream->dataSize = sizeOfStream;	

	// Copy the data into our memory
	oggStream->dataPtr = new unsigned char[sizeOfStream];
	for (int i=0; i < sizeOfStream; i++, stream++)
		oggStream->dataPtr[i] = *stream;

	// Now we have our file in memory, we need to let the Vorbis libraries how to read it. 
	// To do this, we provide callback functions that enable us to do the reading. 
	oggCallbacks.read_func = vorbis_read;
	oggCallbacks.close_func = vorbis_close;
	oggCallbacks.seek_func = vorbis_seek;
	oggCallbacks.tell_func = vorbis_tell;

	// Open the file from memory. We need to pass it a pointer to our data (OGG_MEMORY_FILE structure),
	// a pointer to our output buffer (which the Vorbis libraries will fill for us), and our callbacks.
	int ov_ret = ov_open_callbacks(oggStream, static_cast<OggVorbis_File *>(vf_ptr), 
		NULL, 0, oggCallbacks);
	
	if (0 > ov_ret)
	{
		// There was an error
		int err_code = ifod_err_unspecified_error;
		if (ov_ret == OV_EREAD)
			err_code = ifod_err_read_failed;
		else if (ov_ret == OV_ENOTVORBIS)
			err_code = ifod_err_not_vorbis_data;
		else if (ov_ret == OV_EVERSION)
			err_code = ifod_err_vorbis_version_mismatch;
		else if (ov_ret == OV_EBADHEADER)
			err_code = ifod_err_invalid_vorbis_header;
		else if (ov_ret == OV_EFAULT)
			err_code = ifod_err_internal_fault;

		// Cleanup
		free(vf_ptr);
		delete[] oggStream->dataPtr;
		delete oggStream;

        // Return the ifod_err_code
		return err_code;
    }

	// Copy the memory pointer to the caller
    *vf_out = vf_ptr;
	
	return 0;  // Success!
}

//  Function:	ogg_decode_one_vorbis_packet
//
//  Remarks:	Writes Pulse Code Modulation (PCM) data into the given buffer beginning 
//				at buf_out[0].
//
//  Returns:	The number of bytes written into the buffer. If it hits the end of the 
//				file then it returns 0.

int ogg_decode_one_vorbis_packet(void* vf_ptr, void* buf_out, int buf_byte_size,
                                         int ogg_sample_size,
                                         int* channels_cnt, int* sampling_rate,
                                         int* err_ov_hole_cnt, int* err_ov_ebadlink_cnt)
{
    int word_size, want_signed;

    _ASSERT(8 == ogg_sample_size || 16 == ogg_sample_size);
    
	if (8 == ogg_sample_size)
    {
        word_size = 1;
        // NOTE: We want unsigned data, since Microsoft's DirectSound 
        //       expects this when the sample size is 8 bits.
        want_signed = 0;  
    }
    else 
    {
        word_size = 2;
        // NOTE: We want signed data, since Microsoft's DirectSound 
        //       expects this when the sample size is 16 bits.
        want_signed = 1;
    }
    
	int bytes_put_in_buf;
    for (bytes_put_in_buf = 0;;)
    {
        // NOTE: Parameter 4 is 0 for little endian
        //
		int bitstream;
        long ov_ret = ov_read(static_cast<OggVorbis_File*>(vf_ptr), static_cast<char*>(buf_out), 
			buf_byte_size, 0, word_size, want_signed, &bitstream);
        
        if (ov_ret == 0) 
		{
            break; // at EOF
		}
        else if (0 > ov_ret)  // an error, bad ogg data of some kind
        {
            // NOTE: Other than recording the error, we can't do anything about it except
            //		 skip over it, which is what the Vorbisfile API example code does. Possible
            //		 error codes returned from ov_read() according to the Voribsfile API 
			//			documentation:
            //
            //      OV_HOLE 
            //         indicates there was an interruption in the data. 
            //         (either garbage between pages, loss of sync followed 
            //          by recapture, or a corrupt page) 
            //
            //      OV_EBADLINK 
            //         indicates that an invalid stream section was supplied to
            //         libvorbisfile, or the requested link is corrupt.

            if (OV_HOLE == ov_ret)
                ++(*err_ov_hole_cnt);
            else if (OV_EBADLINK == ov_ret)
                ++(*err_ov_ebadlink_cnt);
        }
        else 
        {
            _ASSERT(ov_ret <= buf_byte_size);

            vorbis_info* vi_ptr = ov_info(static_cast<OggVorbis_File*>(vf_ptr), bitstream);
            if (NULL != vi_ptr)
            {
                *channels_cnt = vi_ptr->channels;  // Number of channels in the bitstream
                *sampling_rate = vi_ptr->rate;     // Sampling rate of the bitstream
            }
            
            bytes_put_in_buf = ov_ret;
            break;
        }
    }
    
    return bytes_put_in_buf;
}

//	Function:	final_ogg_cleanup
//
//	Remarks:	Free the memory pointed to by vf_out and also close the Ogg Vorbis file 
//				opened by init_for_ogg_decode(). OK to call with null vf_ptr parameter.
//
//	Returns:	0 if successful. A non-zero value if ov_clear() was called and failed.

int final_ogg_cleanup(void* vf_ptr)
{
    if (NULL != vf_ptr)
    {
		// Clear the decoder's buffers
        return ov_clear(static_cast<OggVorbis_File*>(vf_ptr));  // non-zero is failure!
        free(vf_ptr);
	}

    return 0;
}

