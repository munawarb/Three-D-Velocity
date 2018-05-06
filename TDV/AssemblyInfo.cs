/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

// Review the values of the assembly attributes

[assembly: AssemblyTitle("Three-D Velocity")]
[assembly: AssemblyDescription("A real-time fighter jet simulation for blind and visually impaired people.")]
[assembly: AssemblyCompany("BPCPrograms, LLC")]
[assembly: AssemblyProduct("Three-D Velocity")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: CLSCompliant(false)]

//The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("00834477-B14F-4D8F-A51E-4FF2200D6CB3")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
#if BETA
[assembly: AssemblyVersion("2.20.5.*")]
#else
[assembly: AssemblyVersion("2.16.1.*")]
#endif
