# What's New
On this page you'll find a list of all updates made to Three-D Velocity since it's open-source push on January 21, 2017. The binary distribution incorporates all of the changes listed here. The Master branch might be ahead of this change log.

## Version 2.42, released on 09/03/2019

### Fixed
- The player's health would drop to 0 after one hit from a missile during Mission Mode ([#101](../../issues/101))
- Due to premature destruction of buffer memory, music files and cut scenes were sometimes distorted. In rare cases, playing music files and cut scenes would result in memory access violations ([#92](../../issues/92)), ([#102](../../issues/102))

## Version 2.41, released on 09/01/2019

### Fixed
- When using a screen-reader, speech is stopped when a menu exits and during the status command. Further, pressing TAB twice announces the target's name using the appropriate speech output source instead of always redirecting to self-voiced output ([#100](../../issues/100))
- The frequency of the engine is now properly adjusted. Previously, it would stop increasing once the afterburners were activated ([#99](../../issues/99))

## Version 2.4, released on 09/01/2019
Compatibility note: Saved games and settings files are not backwards compatible due to the switch to `float`.

### Added
- TDV now uses XAudio2 for sound playback instead of DirectSound. XAudio2 is Microsoft's successor to DirectSound and offers many much-desired features that DirectSound lacked. As a result of the switch to XAudio2, much of the code in BPCSharedComponent.dll has been rewritten and a lot of old code was removed ([#91](../../issues/91)), ([#95](../../issues/95))
- All `double` types in TDV now use `float` values instead, resulting in a smaller memory footprint and better conformance to multimedia API standards that typically use `float` values as parameters. In addition, the binaries are compiled for the "Any CPU" architecture, resulting in native 32-bit and 64-bit support. Previously, TDV was compiled for 32-bit processors only  ([#97](../../issues/97))

### Fixed
- The game would crash with a "null reference exception" after the airbase cut scene. This problem would also cause crashes when new aircraft were spawned, such as in Training Mode and during the Dark Blaze fight ([#93](../../issues/93)), ([#96](../../issues/96)), ([#98](../../issues/98))
- After an update, the updater would not delete the downloaded zip file with new binaries. Also, the update check happens when TDV first loads as opposed to after the logo ([#94](../../issues/94))

## Version 2.31, released on 08/01/2019

### Added
- The Ogg Vorbis player in `BPCSharedComponent.dll` has been completely rewritten to take advantage of the latest advances in C#; this has resulted in much cleaner code in the Ogg player. TDV also uses a new library, [NVorbis](https://github.com/ioctlLR/NVorbis), to decode Ogg packets. In addition, it uses XAudio2 to play music and large cut scenes instead of DirectSound. Finally, Ogg files are streamed into memory instead of loading and decoding entire files at once. This results in a lower memory footprint and allows cut scenes to play with no delays. Previously, the game would take at least a few seconds to load large cut scenes and would occupy a significant portion of memory since the entire decoded Ogg files would be stored in memory ([#91](../../issues/91))

### Fixed
- When using a screen-reader, proper object names are now announced instead of their programmatic names ([#90](../../issues/90))

## Version 2.30, released on 07/28/2019

### Added
- On startup, TDV now asks the player to configure how menus and status messages are spoken. The game can speak messages using either recorded wave files or by using the player's screen reader. This allows for quick localization of written text and allows the player to control the rate of speech of menus and status messages since these messages can be routed to screen-reading software ([#82](../../issues/82)), ([#85](../../issues/85))
- When throttling up or down, a click will sound when the throttle is closed, one quarter, one half, three quarters and full open ([#86](../../issues/86))
- A click to mark zero degrees plays as the aircraft turns. The player can turn towards the click to reach zero degrees ([#83](../../issues/83))
- A new status command, SHIFT + F, announces how many times the player is able to call on the midair refueler for assistance in Mission Mode ([#80](../../issues/80))
- A new status command, O, replays the most recent mission objective when in Mission Mode ([#63](../../issues/63))

### Fixed
- When saving a game in Mission Mode and then reloading the same game in that session, the aircraft would orient itself to the in-game heading instead of the heading from the save file ([#64](../../issues/64))
- When the refueler runs out of fuel in Mission Mode, the proper message is now played instead of just the lock broken signal ([#77](../../issues/77))
- Some RIO messages that were never played in the game have been removed ([#84](../../issues/84))
- Under certain circumstances, the music would restore to full volume instead of the volume it was set at when the game regains focus ([#81](../../issues/81))

### Changes
- The "RIO Messages" option is now above the "Save Game" option to make it more accessible ([#75](../../issues/75))
- The Cruise Missiles will no longer wipe out the boss aircraft in one hit. Their effectiveness against other targets has not changed ([#60](../../issues/60))
- In Mission Mode, all fighters that swarm the player are no longer labeled "fighter 1" ([#56](../../issues/56))

## Version 2.24, released on 12/07/2018

### Added
- NuGet packages for SharpDX now use the latest stable version, since the XAudio2 bug introduced in SharpDX 4.1.0 that would prevent XAudio2 from initializing was fixed. Also, TDV now points to `BPCSharedComponent.Input` for the `DXInput` class instead of a stand-alone class. The class inside TDV has been moved to `BPCSharedComponent.Input.DXInput` ([#70](../../issues/70))
- TDV no longer uses XAudio2 for music playback and is instead using DirectSound. This means that the game no longer relies on the XNA framework which has been deprecated by Microsoft for some time. The `SharpDX.XAudio2` NuGet package has been removed. Please note that since DirectSound buffers use integers for volume control instead of floating-point numbers, it is recommended for everyone upgrading to this version to delete their data/settings.tdv file or the menu music might be silenced ([#57](../../issues/57))
- A new option has been added to the in-game options menu to turn off RIO messages. These messages are on by default ([#62](../../issues/62))
- TDV now uses left and right CTRL to grab an opponent during CQC instead of using the ALT key. This key configuration is compatible across a wider range of keyboard configurations compared to relying on the ALT key ([#59](../../issues/59))

### Fixed
- Background music is properly faded in when the game window regains focus ([#67](../../issues/67))
- A potential race condition that would prevent the game from moving past the chopper fight in the mission has been corrected ([#65](../../issues/65))
- Sometimes, when adding objects to the object table, object IDs would be recycled. This would cause problems such as newly spawned fighters in Training Mode to not appear on the radar because they were already tracked. This was due to the new object's ID being the same as the previously destroyed object's ID. Object IDs are longer than they were which will prevent this issue. Previously, an object's ID could range from one to nine characters. The minimum length of an ID is now ten characters ([#74](../../issues/74))
- In older key-handling code, TDV would sometimes read keyboard input outside of the game environment ([#72](../../issues/72))

## Version 2.21, released on 11/13/2018

### Added
- Update progress messages are now shown on-screen instead of only spoken through a screen-reader or through the Microsoft's Text-To-Speech engine. UI elements are also aesthetically pleasing ([#58](../../issues/58))

## Version 2.20, released on 11/11/2018

### Added
- If the refuel tanker is more than ten minutes out, the generic message no longer plays, and an accurate time is now given ([#48](../../issues/48))
- Configuration files are now loaded from and saved to the `data` directory under the directory where the TDV executable is located, instead of in `%appdata%\BPCPrograms\TDV2`. This allows TDV to be easily portable since players can take the entire TDV directory which will contain the state of the game ([#52](../../issues/52))
- During a brawl, players can now press h to get their health and t to get their opponent's health. The documentation has been updated accordingly ([#43](../../issues/43))

### Fixed
- TDV would crash with an arithmetic overflow exception during Mission Mode during the Power Plant strike ([#51](../../issues/51))
- Locks are no longer allowed to be broken on the landing beacon or aircraft carrier. By design, these objects don't appear on radar, so once the lock was broken it was not possible to reestablish them ([#55](../../issues/55))
- If there were no open rooms on the server, pressing ENTER on "join chat room" would freeze TDV ([#42](../../issues/42))
- If the player destroyed either the mid air refueler or aircraft carrier, TDV would behave in an undefined manner due to a race condition, so while the game was handling this situation, its execution was inconsistent, sometimes resulting in a crash. The race condition is now resolved and the game will behave consistently when destroying these objects ([#41](../../issues/41))
- TDV would crash with a null reference exception if a game was loaded during an active game in Mission Mode ([#53](../../issues/53))
- TDV would sometimes crash on startup when trying to disable a certain screen reader's keyboard hook ([#54](../../issues/54))

## Version 2.18, released on 08/19/2018

### Added
- TDV now uses NuGet Package Manager to link against SharpDX instead of hard-coded references. Also, there is only one solution in the TDV folder now that will open all four projects (TDV, TDVServer, Updater, and BPCSharedComponent) inside one solution ([#45](../../issues/45))

## Version 2.17, released on 06/24/2018

### Added
- TDV now shows a user-friendly message at startup if XAudio2 is not installed ([#40](../../issues/40))

### Fixed
- While on the server, TDV would send the size of the payload as an async request along with the payload. This meant that sometimes the size of the payload would arrive before the payload (the expected behavior) and sometimes the size of the payload would arrive after the payload (unexpected behavior.) In the latter case, the client or server would assume the first four bytes of the freshly arrived payload would be the size of the  payload. Naturally, sometimes this would fail, since the ordering of the size header and payload were not guaranteed. This would cause errors in read operations. Now, the size of the payload is attached to the front of the payload and sent as one write operation to the network stream, guaranteeing its order ([#39](../../issues/39))

## Version 2.16, released on 05/06/2018

### Added
- TDV now has an auto updater, and has been upgraded to .NET Framework 4.6. The server and TDV will check for updates when they are first launched ([#36](../../issues/36))

### Fixed
- Implemented a new method for looping background sounds that avoids the memory access violation that started to happen after the project was upgraded to .NET Framework 4.6 from 3.5 ([#38](../../issues/38))

## Version 2.15, released on 05/04/2018

### Added
- The server now contains a `--log` option. The choices are `info, debug, messages, error, and chat`. You can specify log options by using the `--log level1,level2,...` syntax, like this: `--log error,debug`. The server will tell you what log levels are in use so you can make sure the levels you want are in place ([#32](../../issues/32))
- The IP address and call sign the player enters are now saved, and the prompts asking for this information will be populated with them. This way, the player doesn't have to repeatedly enter these pieces of information ([#18](../../issues/18))
- The server now displays the version number when started ([#35](../../issues/35))

### Fixed
- In a Death Match game online, if you created a bot, the bot would not be counted as a player in the game, so even if it was just you and one or more bots, you would not be permitted to start the game. Now, bots are enrolled as part of the player count in an online Death Match game ([#28](../../issues/28))
- When many weapons were fired, the server would experience a lag. The server now consumes data faster which greatly increases response time. This fixed an issue where sometimes bots would not clear from the server immediately upon destruction ([#27](../../issues/27))
- On the server, players are no longer permitted to send private messages to themselves when in the hangar ([#26](../../issues/26))
- When presented with an input prompt, such as when asked to type an IP address to connect to, CPU usage would suddenly spike. This was caused by a blocking thread which was waiting for input from this prompt, and has been corrected ([#25](../../issues/25))
- When TDV prompted for an IP address or domain to connect to, pressing ESCAPE to back out of the prompt would not work. Now, players can exit this prompt by pressing ESCAPE. This also works for the call sign prompt  ([#34](../../issues/34))
- When loading a game from the main menu while multiplayer mode was selected, the game would load but context would immediately switch to online mode. Now, TDV loads games properly and doesn't try to connect to a multiplayer server ([#19](../../issues/19))

## Version 2.11, released on 04/29/2018

### Fixed
- When the player selects "Act as spectator," TDV no longer crashes ([#16](../../issues/16))
- TDV would sometimes crash with an invalid status error when checking for playing status of a sound ([#30](../../issues/30))
- The "message of the day," if set on the server, will now show upon connect to the server instead of a little while after connecting ([#22](../../issues/22))

## Version 2.10, released on 04/30/2017
### Added
- Admins can now control the server from the console; in-game changes are no longer permitted ([#17](../../issues/17))

### Fixed
- When loading a game from the main menu, the player no longer hears two engines. Previously, the player would hear an idle engine, and their own engine which was throttling up ([#5](../../issues/5))
- In Multiplayer Mode, when a second player joins a private game, TDV no longer crashes ([#13](../../issues/13))
- In parts of the game involving close-quarters combat, the player's orientation would get stuck according to the last known direction of their aircraft ([#14](../../issues/14))
- TDV would behave unexpectedly if the player provided a blank IP address and/or call sign when connecting to the server. Blank input in these fields is no longer allowed ([#15](../../issues/15))

## Version 2.0, released on 04/29/2017
### Added
- The multiplayer mode is available in this version. Instructions for running the server are found elsewhere in this document ([#7](../../issues/7))

## Version 1.01, released on 01/23/2017
### Fixed
- Music no longer plays over death scenes in Mission Mode ([#1](../../issues/1))
- In Training Mode, the player is no longer allowed to flip their aircraft unless required or in free-range combat with Fighter 3 ([#2](../../issues/2))

## Version 1.0, released on 01/21/2017
### Added
- Initial source code push
