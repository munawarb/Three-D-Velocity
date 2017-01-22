@echo off

::
:: When doing a release build of the Libs_All.sln solution in Visual Studio.NET 2003 the
:: TgPlayOgg and TgPlayOgg_vorbis targets do not get copied to the common lib folder 
:: TGProjects\TrayGames_Release\Lib. The C# project doesn't do it because of a known 
:: issue with VS.NET reporting locked files if you have more than one project set to
:: output to a common folder. The CPP project TGPlayOgg_vorbis doesn't do it because it 
:: breaks compatibility with the NANT build system. You should therefore use the NANT 
:: build command to build release OR run this batch file manually after doing a release
:: build in VS.NET. Debug builds will copy to the common folder using post build steps.
::

copy .\Bin\Release\TgPlayOgg.dll ..\..\TrayGames_Release\Lib\
copy ..\TgPlayOgg_vorbisfile\Release\TgPlayOgg_vorbisfile.dll ..\..\TrayGames_Release\Lib\

if errorlevel 1 goto CSharpReportError
goto CSharpEnd
:CSharpReportError
echo Project error: A tool returned an error code from the build event
exit 1
:CSharpEnd