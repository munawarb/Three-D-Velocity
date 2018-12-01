/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using BPCSharedComponent.Input;
using SharpDX.DirectInput;

namespace TDV
{
	public struct KeyData
	{
		private long m_modifier;
		private long m_real;
		public long modifier
		{
			get { return (m_modifier); }
			set { m_modifier = value; }
		}
		public long real
		{
			get { return (m_real); }
			set { m_real = value; }
		}

		public bool isEmpty()
		{
			if (m_modifier == 0 && m_real == 0)
				return (true);
			return (false);
		}
		public long[] toArray()
		{
			long[] d = new long[2];
			d[0] = modifier;
			d[1] = real;
			return d;
		}
	}
}

namespace TDV
{

	////This class contains key mapping data.
	////Each assignable key has a key identifier, whose value is of the type SharpDX.DirectInput.Key.
	public class KeyMap
	{
		public enum Device
		{
			keyboard,
			joystick,
			both
		}

		private static KeyData[] m_keys;
		private static KeyData[] m_joystick;
		public static KeyData[] keysData
		{
			get { return (m_keys); }
			set { m_keys = value; }
		}
		public static KeyData[] joystickData
		{
			get { return (m_joystick); }
			set { m_joystick = value; }
		}

		public static string keyMapFile
		{
			get { return (Addendums.File.appPath + "\\keymap.tdv"); }
		}

		public static string joystickKeymapFile
		{
			get { return (Addendums.File.appPath + "\\jskeymap.tdv"); }
		}

		public static void initialize(Device device)
		{
			if (device == Device.both
				|| device == Device.keyboard)
			{
				m_keys = (KeyData[])
					(Array.CreateInstance(typeof(KeyData),
					Aircraft.Action.GetValues(typeof(Aircraft.Action)).Length));
				addKey(Aircraft.Action.weaponsRadar, Key.R);
				addKey(Aircraft.Action.switchToWeapon1, Key.D1);
				addKey(Aircraft.Action.switchToWeapon2, Key.D2);
				addKey(Aircraft.Action.switchToWeapon3, Key.D3);
				addKey(Aircraft.Action.switchToWeapon4, Key.D4);
				addKey(Aircraft.Action.switchToWeapon5, Key.D5);
				addKey(Aircraft.Action.whoIs, Key.W);
				addKey(Aircraft.Action.chat, Key.Grave);
				addKey(Aircraft.Action.prevMessage, Key.F8);
				addKey(Aircraft.Action.nextMessage, Key.F9);
				addKey(Aircraft.Action.copyMessage, Key.F10);
				addKey(Aircraft.Action.admin, Key.F3);
				addKey(Aircraft.Action.addBot, Key.M);
				addKey(Aircraft.Action.removeBot, Key.Comma);
				addKey(Aircraft.Action.requestRefuel, Key.F);
				addKey(Aircraft.Action.sectorNav, Key.N);
				addKey(Aircraft.Action.autoelevation, Key.E);
				addKey(Aircraft.Action.optionsMenu, Key.O);
				addKey(Aircraft.Action.exitGame, Key.Escape);
				addKey(Aircraft.Action.throttleUp, Key.Up);
				addKey(Aircraft.Action.turnLeft, Key.Left);
				addKey(Aircraft.Action.turnRight, Key.Right);
				addKey(Aircraft.Action.leftBarrelRoll, Key.LeftAlt, Key.Left);
				addKey(Aircraft.Action.rightBarrelRoll, Key.LeftAlt, Key.Right);
				addKey(Aircraft.Action.splitS, Key.LeftAlt, Key.Down);
				addKey(Aircraft.Action.throttleDown, Key.Down);
				addKey(Aircraft.Action.ascend, Key.PageDown);
				addKey(Aircraft.Action.descend, Key.PageUp);
				addKey(Aircraft.Action.level, Key.Home);
				addKey(Aircraft.Action.retractLandingGear, Key.G);
				addKey(Aircraft.Action.registerLock, Key.L);
				addKey(Aircraft.Action.fireWeapon, Key.Space);
				addKey(Aircraft.Action.bankLeft, Key.LeftShift, Key.Left);
				addKey(Aircraft.Action.bankRight, Key.LeftShift, Key.Right);
				addKey(Aircraft.Action.brake, Key.LeftControl);
				addKey(Aircraft.Action.activateAfterburners, Key.A);
				addKey(Aircraft.Action.togglePointOfView, Key.V);
				addKey(Aircraft.Action.pauseGame, Key.P);
				addKey(Aircraft.Action.stopSAPI, Key.Q);
				addKey(Aircraft.Action.decreaseMusicVolume, Key.F6);
				addKey(Aircraft.Action.increaseMusicVolume, Key.F7);
			}

			if (device == Device.both
				|| device == Device.joystick)
			{
				m_joystick = (KeyData[])
					(Array.CreateInstance(typeof(KeyData),
					Aircraft.Action.GetValues(typeof(Aircraft.Action)).Length));
				for (int i = 0; i < m_joystick.Length; i++)
					m_joystick[i].real = -2;
				addKey(Aircraft.Action.weaponsRadar, 10);
				addKey(Aircraft.Action.requestRefuel, 13);
				addKey(Aircraft.Action.sectorNav, 12);
				addKey(Aircraft.Action.autoelevation, 11);
				addKey(Aircraft.Action.optionsMenu, 3);
				addKey(Aircraft.Action.retractLandingGear, 7);
				addKey(Aircraft.Action.registerLock, 2);
				addKey(Aircraft.Action.fireWeapon, 0);
				addKey(Aircraft.Action.switchWeapon, 1);
				addKey(Aircraft.Action.brake, 6);
				addKey(Aircraft.Action.activateAfterburners, 5);
				addKey(Aircraft.Action.pauseGame, 4);
				addKey(Aircraft.Action.stopSAPI, 9);
				addKey(Aircraft.Action.throttleDown, -1);
				addKey(Aircraft.Action.turnLeft, -1);
				addKey(Aircraft.Action.turnRight, -1);
				addKey(Aircraft.Action.bankLeft, -1);
				addKey(Aircraft.Action.bankRight, -1);
				addKey(Aircraft.Action.leftBarrelRoll, -1);
				addKey(Aircraft.Action.rightBarrelRoll, -1);
				addKey(Aircraft.Action.splitS, -1);
				addKey(Aircraft.Action.ascend, -1);
				addKey(Aircraft.Action.descend, -1);
				addKey(Aircraft.Action.decreaseMusicVolume, -1);
				addKey(Aircraft.Action.increaseMusicVolume, -1);
				addKey(Aircraft.Action.level, -1);
			}
		}

		public static void initialize()
		{
			initialize(Device.both);
		}

		//this method should only be called by a joystick setup.
		public static void addKey(Aircraft.Action key, int value)
		{
			joystickData[(int)key - 1].real = value;
			joystickData[(int)key - 1].modifier = 0;
		}

		//Overloaded. For use with a keyboard only.
		//Sets modifier to 0 (eg. none)
		public static void addKey(Aircraft.Action key, Key value)
		{
			keysData[(int)key - 1].real = (long)value;
			keysData[(int)key - 1].modifier = -1;
		}

		public static void addKey(Aircraft.Action key, Key m, Key r)
		{
			keysData[(int)key - 1].modifier = (long)m;
			keysData[(int)key - 1].real = (long)r;
		}

		public static long[] getKey(Aircraft.Action key, bool fromJS)
		{
			if (!fromJS)
				return (keysData[(int)key - 1].real == 0) ? null : keysData[(int)key - 1].toArray();
			else
				return (joystickData[(int)key - 1].real == -2) ? null : joystickData[(int)key - 1].toArray();
		}

		//Overloaded. Reads key data from keyboard.
		public static long[] getKey(Aircraft.Action key)
		{
			return (getKey(key, false));
		}

		//The method below returns an array of all Aircraft.Action in string format for use in a menu or user friendly UI.
		//the Exclude array must be supplied and holds all keys which should not be programmed by the user (for instance, when using a joystick, it is not fit
		// to program the ascend and descend commands.)
		//Should there be no exclusion keys, a value of -1 should be supplied or the method call will fail.
		public static string[] getKeyStrings(params Aircraft.Action[] exclude)
		{
			string[] strArray = new string[keysData.Length];
			if (exclude == null)
			{
				for (int i = 0; i < keysData.Length; i++)
					strArray[i] = getStringValue(i + 1);
				return strArray;
			}

			for (int i = 0; i < strArray.Length; i++)
			{
				if (Array.IndexOf(exclude, ((Aircraft.Action)i + 1)) > -1)
				{
					strArray[i] = "";
				}
				else
				{
					strArray[i] = getStringValue(i + 1);
				}
			}
			return (strArray);
		}

		public static int getNumberOfKeys()
		{
			return keysData.Length;
		}

		//Returns the string representation of the parameter a.
		private static string getStringValue(long a)
		{
			if (a == -1)
				return "";
			return ("keymap" + a + ".wav");
		}

		public static void saveToFile()
		{
			BinaryWriter s = new BinaryWriter(new FileStream(keyMapFile, FileMode.Create));
			int i = 0;
			for (i = 0; i <= keysData.Length - 1; i++)
			{
				s.Write(i);
				s.Write(keysData[i].modifier);
				s.Write(keysData[i].real);
			}
			s.Close();
			s = null;

			s = new BinaryWriter(new FileStream(joystickKeymapFile, FileMode.Create));
			for (i = 0; i <= joystickData.Length - 1; i++)
			{
				s.Write(i);
				s.Write(joystickData[i].modifier);
				s.Write(joystickData[i].real);
			}
		}

		public static bool readFromFile()
		{
			try
			{
				if (!File.Exists(keyMapFile))
				{
					initialize();
					return true;
				}
				BinaryReader s = new BinaryReader(new FileStream(keyMapFile, FileMode.Open));
				int i = 0;
				for (i = 0; i <= keysData.Length - 1; i++)
					addKey((Aircraft.Action)s.ReadInt32() + 1,
										  (Key)s.ReadInt64(), (Key)s.ReadInt64());
				s.Close();
				s = null;

				if (!File.Exists(joystickKeymapFile))
					return true; //loaded keyboard, but js doesn't exist
				s = new BinaryReader(new FileStream(joystickKeymapFile, FileMode.Open));
				for (i = 0; i <= joystickData.Length - 1; i++)
				{
					Aircraft.Action a = (Aircraft.Action)s.ReadInt32() + 1;
					s.ReadInt64();
					int k = (int)s.ReadInt64();
					addKey(a, k);
				}
				return true;
			}
			catch
			{
				initialize();
				return false;
			}
		}

		////Returns true if another modifier for this same key assignment is pressed,
		////false otherwise
		public static bool isModifierHeldFor(Aircraft.Action a)
		{
			if (keysData[(int)a - 1].modifier > 0)
			{
				if (DXInput.isKeyHeldDown((Key)keysData[(int)a - 1].modifier))
				{
					return (true);
				}
			}
			return (false);
		}

		//returns an array which holds all keys that have the specified
		//key as their .real properties.
		public static KeyData[] getAllAssignmentsFor(long k, Aircraft.Action exclude)
		{
			KeyData[] theKeys = new KeyData[keysData.Length];
			int index = 0;
			KeyData kd;
			for (index = 0; index < keysData.Length; index++)
			{
				kd = keysData[index];
				if (index + 1 != (int)exclude
					&& k == kd.real)
				{
					theKeys[index] = kd;
					index++;
				}
			}
			if (index == 0)
				return (null);
			else
			{
				KeyData[] dest = new KeyData[index + 1];
				theKeys.CopyTo(dest, 0);
				return dest;
			}
		}

		public static void deleteKeymap(Device device)
		{
			if (device == Device.keyboard)
				File.Delete(keyMapFile);
			else
				File.Delete(joystickKeymapFile);
			initialize(device);
		}

		/*The two methods below return 0 if no match is found.
		 * If a match is found, the int value associated with the Aircraft.Action is returned.
		 * This value can be directly casted to Aircraft.Action and does not need to be incremented or decremented.
		 * To get the index in the keysData array where the match was found, minus 1 from the returned value.
		 * */
		public static int alreadyAssignedTo(Key modifier, Key key)
		{
			long a = (long)modifier;
			long b = (long)key;
			long[] data;
			for (int i = 0; i < keysData.Length; i++)
			{
				data = keysData[i].toArray();
				if (a == data[0] && b == data[1])
					return i + 1;
			} //for
			return 0;
		}

		public static int alreadyAssignedTo(Key key)
		{
			long a = (long)-1;
			long b = (long)key;
			long[] data;
			for (int i = 0; i < keysData.Length; i++)
			{
				data = keysData[i].toArray();
				if (a == data[0] && b == data[1])
					return i + 1;
			} //for
			return 0;
		}

		/// <summary>
		/// Determines whether the given key combination is reserved by the game or not.
		/// </summary>
		/// <param name="m">The modifier. If none, pass Key.Return.</param>
		/// <param name="r">The key in the combination.</param>
		/// <returns>True if the combination is reserved, false otherwise.</returns>
		public static bool isReserved(Key m, Key r)
		{
			if (m == Key.Return && r == Key.Return)
				return true;
			if (isF1ToF5(m) || isF1ToF5(r))
				return true;
			if (isNumber(r) && m == Key.Return)
				return true;
			if (m == Key.Return && (r == Key.W || r == Key.Comma || r == Key.M || r == Key.Grave || r == Key.F8 || r == Key.F9 || r == Key.F10))
				return true;
			return false;
		}
		private static bool isF1ToF5(Key k)
		{
			return k == Key.F1 || k == Key.F2 || k == Key.F3 || k == Key.F4 || k == Key.F5;
		}
		private static bool isNumber(Key k)
		{
			return k == Key.D1 || k == Key.D2 || k == Key.D3 || k == Key.D4 || k == Key.D5 || k == Key.D6;
		}

		public static void clearKeyboardAssignment(Aircraft.Action a)
		{
			addKey(a, Key.Unknown);
		}

		public static void clearJSAssignment(Aircraft.Action a)
		{
			addKey(a, -1);
		}

	}
}
