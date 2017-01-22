========================================================================
    OGG VORBIS LIBRARY WRAPPER : TgPlayOgg_vorbisfile Project Overview
========================================================================

Ogg Vorbis' high-level API, vorbisfile, has only two input choices: either a C 
file pointer or a set of custom callback functions that do the reading of the 
input Ogg Vorbis data. The better and more portable of these choices is probably 
custom callbacks, but I wasn't aware that .NET 1.1 gave any control over the 
calling convention of its methods, and its standard calling convention is 
StdCall, while the vorbisfile DLLs are compiled with the Cdecl calling convention. 
Thus, given C# and .NET 1.1, we decided to write some C/C++ code and compile it 
into a DLL, and this DLL includes the callbacks that vorbisfile needs. This is 
why we created the TGPlayOgg_vorbisfile wrapper project.

/////////////////////////////////////////////////////////////////////////////

TgPlayOgg_use_vorbisfile.c - this C file is compiled into a DLL that can be
called from any .NET C# project.

/////////////////////////////////////////////////////////////////////////////

To create a standalone DLL, compile this file and statically linked with 
the following 3 Ogg Vorbis libraries availabe in the Ogg Vorbis SDK.

	Debug version:
		ogg_static_d.lib
		vorbis_static_d.lib
		vorbisfile_static_d.lib

	Release version:
		ogg_static.lib
		vorbis_static.lib
		vorbisfile_static.lib