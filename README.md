#### Three-D Velocity: A Fighter Jet Flight Simulator For The Blind and Visually Impaired

In 2007, as I was watching "Top Gun," I thought to myself: "How cool would it be if a blind person could fly a fighter jet?" Until this time, there had been no fighter jet simulators that blind people could play. So, a handful of friends and I embarked on the journey to create the world's first ever fully accessible fighter jet simulation.

**We succeeded.** In 2010, my company at that time, BPCPrograms, released Three-D Velocity. It was complete with a story line and a minimally realistic fighter jet that the player controlled.

Over time, we improved the simulator, and then in 2012 decided to redo most of it to make it more realistic. We also remastered the sounds to make them more real. This is how Three-D Velocity version 2 was born. Unfortunately, we were not able to release TDV 2 as planned, and the company was later dissolved.

I decided to make TDV available as open source for two reasons. First, I feel that people can learn from what it has to offer. There is full support for flight controllers in this game, along with many other things that developers can benefit from. Second, flying a jet at mach-2 is a unique experience for blind people, and now that the company no longer exists, this is my best option to get TDV to people who will enjoy it.

#### Features
Three-D Velocity is a real-time fighter jet simulation designed for blind and visually impaired people. Your craft is designed to get you out of the tightest of situations, provided that you are  willing to try daring maneuvers. Your jet is able to go at a top airspeed of mach 2, and is equipped with afterburner jets to give you that extra boost in speed that can mean the difference between a success or a bust.

The simulator features directional sound to let the blind player know of their current environment. Hear missiles rocketing toward your aircraft, the fire of surface-to-air guns, other aircraft speeding past, and completely engage yourself in a sky-lighting fight taking place fifty-thousand feet above sealevel.

Blind players can use auditory cross-hairs to lock on to other aircraft and land on runways and aircraft carriers. The radar reads out information using human-recorded speech, making for a clear, verbal readout of the surrounding objects which allow players to make split-second decisions.

Three-D Velocity even features a Radio Intercept Officer (RIO) who gives the player vital feedback during missions, allowing them to roll just in time to avoid being slammed by a missile or suffer damage to the aircraft because of a strafe from an enemy.

Best of all, the game is completely real-time!

Here are some of the features in the game:
- A realistic aircraft which you will pilot.
- Afterburner jets to assist your aircraft in achieving its top airspeed.
- Support for combat flight controllers.
- Force feedback support.
- Completely self-voicing (no screen reader is required.)
- Surround sound support.
- A variety of weapons, including guns, missiles, and active-radar-guided missiles which will follow their targets.
- A weapons radar to view all missiles coming towards your aircraft.
- Racing, Death Match, and Auto Play modes.
- Mission Mode to follow the "Cloning Malice" story.
- A comprehensive [manual in HTML format](http://htmlpreview.github.io/?https://github.com/munawarb/Three-D-Velocity-Binaries/blob/master/docs/documentation.htm).

#### Background
Three-D Velocity is a sequel to Treasure Hunt, which was released in 2004 by BPCPrograms. The story picks up after the destruction of James Brutus' laboratory and his subsequent death. The original mission was to be an end-all to the Cloning Malice project, which is a privately funded project whose sole goal is to harness the power of today's scientific advances to build a new breed of soldiers, each clones of one another. Project Cloning Malice was started by a scientist named James Brutus. Unfortunately, attempts to shut down the project failed, and the world finds itself faced with the horrors of this project once again.

The player acts as Lieutenant Orion, a pilot at the center of an effort led by the United States to crash the project. The game follows Orion as he's sent on a mission to stage airstrikes on the project's headquarters.

#### Is it abandonware?
No. It's open source, not abandonware. I'll be working on it as time permits. If you have suggestions for future improvements or find bugs, post them on the issues page and you might see the fix or feature in a future update!

#### Documentation
There's a training mode in the game that will help you get familiar with the basic concepts of TDV. [Full documentation can be found here.](http://htmlpreview.github.io/?https://github.com/munawarb/Three-D-Velocity-Binaries/blob/master/docs/documentation.htm)

#### Change Log

##### Version 1.01, released on 01/23/2017
###### Fixed
- Music no longer plays over death scenes in Mission Mode ([#1](../../issues/1))
- In Training Mode, the player is no longer allowed to flip their aircraft unless required or in free-range combat with Fighter 3 ([#2](../../issues/2))

##### Version 1.0, released on 01/21/2017
###### Added
- Initial source code push

#### Downloading The Game
There are several ways to download TDV, depending on your situation and what you want.

##### Dependencies
TDV relies on XAudio2 for some of its sound rendering. If you don't have XAudio2 installed, you'll get a semi-cryptic message at startup with the word "xaudio2" in it. If this happens, download and run the [Microsoft DirectX Web Installer](https://www.microsoft.com/en-us/download/details.aspx?id=35).

Also, make sure to delete your previous configuration of TDV if you've ever had TDV on your computer, or this version might not work. You can find TDV config files in %APPDATA%\BPCPrograms

If you find any bugs or have any suggestions, please post them on the [Issues Page.](https://github.com/munawarb/Three-D-Velocity/issues)

While the commercial version offered a 64-bit version of the game, I've removed it from this source to make building easier. TDV will run fine on 64-bit systems under WoW-64 mode.

##### I just want to play it
If all you want is the executable and supporting files and you're not interested in the source code, you can always [download the latest version here](https://github.com/munawarb/Three-D-Velocity-Binaries/archive/latest.zip).

The zip file contains the TDV executable and all sounds. Run the file tdv.exe to start Three-D Velocity.

##### I just want the source code
If you just want the source code without the TDV executable and media files, use the git cloen command, like this:
git clone https://github.com/munawarb/Three-D-Velocity.git

Three-D Velocity is written in C#.NET. You will need an IDE that supports Visual Studio 2013 solutions, and .NET Framework 3.5 or higher to compile the project. You can get a copy of Visual Studio 2013 Express from Microsoft. This is the recommended IDE. Visual Studio Express is free. Open up the TDV/Three-D Velocity.sln solution in your C# IDE of choice and the project files will load. If you want to successfully run TDV, you must fetch the binaries submodule to include all dependencies and sound files.

##### I want everything!
Now we're talking! If you want the whole thing which includes the source code and the huge binary release, use the git clone command, like this:
git clone --recursive https://github.com/munawarb/Three-D-Velocity.git

The --recursive option will tell Git to fetch the binaries submodule. It will be placed in TDV/Three-D-Velocity-Binaries

Three-D Velocity is written in C#.NET. You will need an IDE that supports Visual Studio 2013 solutions, and .NET Framework 3.5 or higher to compile the project. You can get a copy of Visual Studio 2013 Express from Microsoft. This is the recommended IDE. Visual Studio Express is free. All other dependencies such as SharpDX and Ogg Vorbis libraries are downloaded with the binaries submodule, which was downloaded for you already. The project solution already points to them. Open up the Three-D Velocity.sln solution in your C# IDE of choice and then build the project. The executable will be placed in TDV/Three-D-Velocity-Binaries and is called tdv.exe

#### Advisories
Three-D Velocity contains adult-oriented cut scenes and is not suitable for minors; parental guidance is advised.