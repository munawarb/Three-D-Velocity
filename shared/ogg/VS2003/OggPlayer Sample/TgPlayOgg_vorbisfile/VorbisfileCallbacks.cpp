#include "stdafx.h"
#include "TgPlayOgg_use_vorbisfile.h"

//	Function:	vorbis_read- Callback for the Vorbisfile API read function.
//
//	Remarks:	Reads up to count items of size bytes from the input stream and stores them 
//				in the data buffer. 
//
//	Returns:	The number of full items actually read if successful, which may be less than 
//				sizeToRead if an error occurs or if the end of the file is encountered before 
//				reaching count. A return of 0 means that we have reached the end of the file,
//				and we were unable to read anymore data. A return value of -1 means there was
//				an error.

size_t vorbis_read(void* data_ptr,	// A pointer to the data that the vorbis files need
				  size_t byteSize,	// Byte size on this particular system
				  size_t sizeToRead,// Maximum number of items to be read
				  void* data_src)	// A pointer to the data we passed to ov_open_callbacks
{
	POGG_MEMORY_FILE vorbisData = static_cast<POGG_MEMORY_FILE>(data_src);
	if (NULL == vorbisData) return -1;

	// Calculate how much we need to read. This can be sizeToRead*byteSize 
	// or less depending on how near the EOF marker we are.
	size_t actualSizeToRead, spaceToEOF = vorbisData->dataSize - vorbisData->dataRead;
	if ((sizeToRead*byteSize) < spaceToEOF)
		actualSizeToRead = (sizeToRead*byteSize);
	else
		actualSizeToRead = spaceToEOF;	
	
	// A copy of the data from memory to the datastruct that the 
	// Vorbisfile API will use.
	if (actualSizeToRead)
	{
		// Copy the data from the start of the file PLUS how much 
		// we have already read in.
		memcpy(data_ptr, (char*)vorbisData->dataPtr + vorbisData->dataRead, actualSizeToRead);

		// Increase by how much we have read by
		vorbisData->dataRead += actualSizeToRead;
	}

	return actualSizeToRead;
}

//	Function:	vorbis_seek- Callback for the Vorbisfile API seek function.
//
//	Remarks:	Moves the pointer (if any) associated with stream to a new location that is 
//				offset bytes from origin. You can use this function to reposition the data  
//				pointer anywhere in the stream. The function is given a point from which to 
//				seek (SEEK_SET, SEEK_CUR, SEEK_END), and the data pointer is moved accordingly 
//				making sure not to pass past the boundary of the data.
//
//	Returns:	Returns 0 if seek operation was successful. Otherwise it returns -1 meaning 
//				that this file is not seekable.

int vorbis_seek(void* data_src,		// A pointer to the data we passed to ov_open_callbacks
			   ogg_int64_t offset,	// Number of bytes from origin
			   int origin)			// Initial position
{
	POGG_MEMORY_FILE vorbisData = static_cast<POGG_MEMORY_FILE>(data_src);
	if (NULL == vorbisData) return -1;

	switch (origin)
	{
		case SEEK_SET: 
		{	// Seek to the start of the data file, make sure we are not 
			// going to the end of the file.
			ogg_int64_t actualOffset; 
			if (vorbisData->dataSize >= offset)
				actualOffset = offset;
			else
				actualOffset = vorbisData->dataSize;

			// Set where we now are
			vorbisData->dataRead = static_cast<int>(actualOffset);
			break;
		}

		case SEEK_CUR: 
		{
			// Seek from where we are, make sure we don't go past the end
			size_t spaceToEOF = vorbisData->dataSize - vorbisData->dataRead;

			ogg_int64_t actualOffset; 
			if (offset < spaceToEOF)
				actualOffset = offset;
			else
				actualOffset = spaceToEOF;	

			// Seek from our currrent location
			vorbisData->dataRead += static_cast<long>(actualOffset);
			break;
		}

		case SEEK_END: 
			// Seek from the end of the file
			vorbisData->dataRead = vorbisData->dataSize+1;
			break;

		default:
			_ASSERT(false && "The 'origin' argument must be one of the following constants, defined in STDIO.H!\n");
			break;
	};

	return 0;
}

//	Function:	vorbis_close- Callback for the Vorbisfile API close function.
//
//	Remarks:	Closes the stream, any system-allocated buffers are released when the 
//				stream is closed.
//
//	Returns:	Returns 0 if the stream is successfully closed. Otherwise it returns
//				EOF to indicate an error.

int vorbis_close(void* data_src)
{
	// Free the memory that we created for the stream.
	POGG_MEMORY_FILE oggStream = static_cast<POGG_MEMORY_FILE>(data_src);

	if (NULL != oggStream)
	{
		if (NULL != oggStream->dataPtr)
		{
			delete[] oggStream->dataPtr;
			oggStream->dataPtr = NULL;
		}

		delete oggStream;
		return 0;
	}

	_ASSERT(false && "The 'data_src' argument (set by ov_open_callbacks) was NULL so memory was not cleaned up!\n");

	return EOF;
}

//	Function:	vorbis_tell- Callback for the Vorbisfile API tell function.
//
//	Remarks:	Gets the current position of the pointer (if any) associated with stream. 
//				The position is expressed as an offset relative to the beginning of the stream.
//
//	Returns:	Current position if successful, returns -1L to indicate an error.

long vorbis_tell(void* data_src) 
{
	POGG_MEMORY_FILE vorbisData = static_cast<POGG_MEMORY_FILE>(data_src);
	if (NULL == vorbisData) return -1L;

	// We just want to tell the Vorbisfile API how much we have read so far
	return vorbisData->dataRead;
}