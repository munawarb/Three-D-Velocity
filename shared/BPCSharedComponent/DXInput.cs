/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Collections;
using System.Threading;
using System.Windows.Forms;


namespace BPCSharedComponent.Input
{
	public static class DXInput
	{
		/// <summary>
		/// The states of initializing force feedback.
		/// </summary>
		public enum ForceFeedbackStatus
		{
			/// <summary>
			/// No force feedback is available.
			/// </summary>
			noForceFeedback,
			/// <summary>
			/// Force feedback is available but could not be initialized.
			/// </summary>
			couldNotInitialize,
			/// <summary>
			/// Force feedback is initialized.
			/// </summary>
			initialized
		}
		/// <summary>
		///Represents position of directional or point-of-view pad
		/// </summary>
		public enum DirectionalPadPositions
		{
			neutral = -1,
			up = 0,
			right = 9000,
			down = 18000,
			left = 27000
		}
		private const int durationInfinite = 1000000000;
		private static DirectionalPadPositions
			lastDPPosition = DirectionalPadPositions.neutral;
		private static bool[] TheKeys = new bool[212];
		public static bool useSlider;
		public static bool useZ;
		private static bool[] TheJSButtons;
		private static bool keyboardAcquired;


		public static Keyboard diDev;
		public static Joystick JSDevice;
		private static Effect afterburnerEffect;
		private static Effect gForceEffect;
		private static Effect cruiseMissileEffect;
		private static Effect fireEffect;
		private static Effect explodeEffect;
		public static Effect hitEffect;
		/// <summary>
		/// Check this flag before initiating any force feedback calls.
		/// </summary>
		public static bool forceFeedbackEnabled;
		public static KeyboardState diState;
		public static bool[] aKeys = new bool[212];
		public static int JSXCenter;
		public static int JSYCenter;
		public static int JSZCenter;
		public static int JSRZCenter;
		private static DeviceObjectId JSSliderId;
		private static DeviceObjectId jsZId;
		private static DirectInput m_input;

		/// <summary>
		/// Gets the DirectInput device.
		/// </summary>
		public static DirectInput input
		{
			get { return (m_input); }
		}

		/// <summary>
		/// Gets the state of the joystick device.
		/// </summary>
		public static JoystickState JSState
		{
			get { return (JSDevice.GetCurrentState()); }
		}

		/// <summary>
		/// Initializes DirectInput and acquires the default keyboard. The keyboard is set to foreground and nonexclusive.
		/// </summary>
		/// <param name="Handle">The handle of the application form. When the specified window loses focus, DirectInput will stop reading from the keyboard.</param>
		public static void DInputInit(IntPtr Handle)
		{
			m_input = new DirectInput();
			//get the default keyboard.
			diDev = new Keyboard(input);
			diDev.SetCooperativeLevel(Handle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
			diDev.Acquire();
		}

		/// <summary>
		/// Initializes a joystick device using the specified global unique identifier.
		/// </summary>
		/// <param name="handle">A pointer to the application's master form.</param>
		/// <param name="g">The GUID of the device to initialize.</param>
		public static ForceFeedbackStatus DInputInit(IntPtr handle, Guid g)
		{
			if (JSDevice != null) {
				JSDevice.Unacquire();
				JSDevice = null;
			}

			JSDevice = new Joystick(input, g);
			int xAxisOffset = 0, yAxisOffset = 0;
			int nextOffset = 0;
			//            JSDevice.Properties.AutoCenter = true;
			foreach (DeviceObjectInstance d in JSDevice.GetObjects()) {
				if ((d.ObjectId.Flags & DeviceObjectTypeFlags.ForceFeedbackActuator) == DeviceObjectTypeFlags.ForceFeedbackActuator) {
					if (nextOffset == 0)
						xAxisOffset = d.Offset;
					else
						yAxisOffset = d.Offset;
					nextOffset++;
				}
				if (d.ObjectType == ObjectGuid.XAxis) {
					JSDevice.GetObjectPropertiesById(
						d.ObjectId).Range = new InputRange(-5, 5);
					JSDevice.GetObjectPropertiesById(
						d.ObjectId).DeadZone = 1000;
				}
				if (d.ObjectType == ObjectGuid.YAxis) {
					JSDevice.GetObjectPropertiesById(d.ObjectId).Range = new InputRange(-9, 9);
					JSDevice.GetObjectPropertiesById(
						d.ObjectId).DeadZone = 1000;
				}
				if (d.ObjectType == ObjectGuid.Slider) {
					JSDevice.GetObjectPropertiesById(
						d.ObjectId).Range = new InputRange(0, 11);
					JSSliderId = d.ObjectId;
					useSlider = true;
				}
				if (d.ObjectType == ObjectGuid.ZAxis) {
					JSDevice.GetObjectPropertiesById(
						d.ObjectId).Range = new InputRange(0, 11);
					jsZId = d.ObjectId;
					useZ = true;
				}
				if (d.ObjectType == ObjectGuid.RzAxis) {
					JSDevice.GetObjectPropertiesById(
						d.ObjectId).Range = new InputRange(-5, 5);
				}
			} //for
			if (useSlider && useZ)
				useSlider = false;
			JSDevice.SetCooperativeLevel(handle,
				CooperativeLevel.Background | CooperativeLevel.Exclusive);
			JSDevice.Acquire();
			updateJSState();
			TheJSButtons = JSState.Buttons;
			if (nextOffset > 0) {
				if (!dInputInitFD(JSDevice, xAxisOffset, yAxisOffset, nextOffset)) {
					forceFeedbackEnabled = false;
					return ForceFeedbackStatus.couldNotInitialize;
				} else {
					forceFeedbackEnabled = true;
					return ForceFeedbackStatus.initialized;
				}
			}
			return ForceFeedbackStatus.noForceFeedback;
		}

		/// <summary>
		/// Initializes force feedback.
		/// </summary>
		/// <param name="device">The device representing the joystick.</param>
		/// <param name="xAxisOffset">The id of the x axis-providing device.</param>
		/// <param name="yAxisOffset">The id of the y axis-providing device.</param>
		/// <param name="numAxes">The number of axes supported by the device. If set to 1, an x-axis is assumed; if anything else, an x and y-axis is assumed.</param>
		/// <returns>True if force feedback was initialized, false otherwise.</returns>
		public static bool dInputInitFD(Device device, int xAxisOffset, int yAxisOffset, int numAxes)
		{
			try {
				Guid forceFeedbackGuid = Guid.Empty;
				forceFeedbackGuid = EffectGuid.ConstantForce;

				int[] offsets = null;
				int[] coords = null;

				if (numAxes == 1) {
					offsets = new int[1];
					offsets[0] = xAxisOffset;
					coords = new int[1];
				} else {
					offsets = new int[2];
					offsets[0] = xAxisOffset;
					offsets[1] = yAxisOffset;
					coords = new int[2];
				}
				//offsets[0] = 4;
				//offsets[1] = 0;
				for (int i = 0; i < coords.Length; i++)
					coords[i] = 0;

				EffectParameters info = new EffectParameters();
				info.Flags = EffectFlags.Polar | EffectFlags.ObjectIds;
				ConstantForce typeSpec = new ConstantForce();
				typeSpec.Magnitude = 5000;
				info.Duration = -1;
				info.SamplePeriod = 0;
				info.Parameters = typeSpec;
				info.TriggerButton = -1;
				info.TriggerRepeatInterval = 0;

				info.Gain = 5000;

				info.SetAxes(offsets, coords);

				info.StartDelay = 0;
				Envelope env = new Envelope();
				env.AttackLevel = 10000;
				env.AttackTime = 0;
				env.FadeLevel = 10000;
				env.FadeTime = 0;
				info.Envelope = null;
				afterburnerEffect = new Effect(device, EffectGuid.ConstantForce, info);

				ConstantForce gFTypeSpec = new ConstantForce();
				gFTypeSpec.Magnitude = 3000;
				info = new EffectParameters();
				info.SamplePeriod = 0;
				info.Parameters = gFTypeSpec;
				info.TriggerButton = -1;
				info.TriggerRepeatInterval = -1;


				info.Gain = 3000;
				info.Flags = EffectFlags.Polar | EffectFlags.ObjectIds;
				info.SetAxes(offsets, coords);

				info.StartDelay = 0;

				info.Envelope = null;

				info.Duration = -1;

				gForceEffect = new Effect(device, EffectGuid.ConstantForce, info);


				env.AttackLevel = 0; //this is +parameter.magnitude
				env.AttackTime = 0;
				env.FadeLevel = 10000;
				env.FadeTime = 2000000; //How long it will take effect to reach fadeLevel
				info.Envelope = env;
				ConstantForce cmType = new ConstantForce();
				cmType.Magnitude = 3000;
				info.Parameters = cmType;
				info.Duration = 1000000; //at sustained level
				info.Gain = 3000;

				cruiseMissileEffect = new Effect(device, EffectGuid.ConstantForce, info);

				ConstantForce fType = new ConstantForce();
				fType.Magnitude = 3000;
				info.Duration = 50000;
				info.Gain = 3000;
				info.Parameters = fType;
				info.Envelope = null;
				fireEffect = new Effect(device, EffectGuid.ConstantForce, info);

				ConstantForce eType = new ConstantForce();
				eType.Magnitude = 10000;
				info.Parameters = eType;
				info.Gain = 10000;
				info.Duration = 0;
				env.AttackTime = 0;
				env.AttackLevel = 0;
				env.FadeTime = 7000000;
				env.FadeLevel = 3000;
				info.Envelope = env;
				explodeEffect = new Effect(device, EffectGuid.ConstantForce, info);

				ConstantForce hType = new ConstantForce();
				hType.Magnitude = 3200;
				info.Parameters = hType;
				info.Gain = 3200;
				info.Duration = 100000;
				info.Envelope = null;
				hitEffect = new Effect(device, EffectGuid.ConstantForce, info);
				return true;
			}
			catch {
				return false;
			}
		}


		/// <summary>
		/// Checks to see which keys are pressed. Polls the device for fresh data.
		/// </summary>
		public static void CheckKeys()
		{
			if (!isKeyboardAcquired())
				return;

			diState = diDev.GetCurrentState();
			int i = 0;

			//Scan through all the keys to check which are depressed
			for (i = 1; i <= 211; i++) {
				if (diState.IsPressed((Key)i)) {
					aKeys[i] = true;
					//If the key is pressed, set the appropriate array index to true
				} else {
					aKeys[i] = false;
					//If the key is not pressed, set the appropriate array index to false
				}
				Application.DoEvents();
			}
		}

		/// <summary>
		/// Unacquires the keyboard and cleans up DirectInput.
		/// </summary>
		public static void Terminate()
		{
			diDev.Unacquire();
		}

		/// <summary>
		/// Checks to see if the given key is held down.
		/// </summary>
		/// <param name="k">The key to check.</param>
		/// <param name="acquire">If true, the keyboard is polled for updated data; otherwise, the last update of the keyboard is used.</param>
		/// <returns>True if the key is held down, false otherwise.</returns>
		public static bool isKeyHeldDown(Key k, bool acquire)
		{
			if ((acquire && !updateKeyboardState()) || !keyboardAcquired)
				return (false);
			return (diState.IsPressed(k));
		}

		/// Checks to see if the given key is held down. Will poll the keyboard for fresh data before checking.
		/// </summary>
		/// <param name="k">The key to check.</param>
		/// <returns>True if the key is held down, false otherwise.</returns>
		public static bool isKeyHeldDown(Key k)
		{
			return (isKeyHeldDown(k, true));
		}

		/// <summary>
		/// Checks if one of the CTRL keys is held down.
		/// </summary>
		/// <returns>True if either the left or right CTRL keys are held down, false otherwise.</returns>
		public static bool IsControl()
		{
			if (isKeyHeldDown(Key.LeftControl) || isKeyHeldDown(Key.RightControl, false)) {
				return (true);
			}
			return (false);
		}

		/// <summary>
		/// Checks if one of the SHIFT keys is held down.
		/// </summary>
		/// <returns>True if either the left or right SHIFT keys are held down, false otherwise.</returns>
		public static bool IsShift()
		{
			if (isKeyHeldDown(Key.LeftShift) || isKeyHeldDown(Key.RightShift, false)) {
				return (true);
			}
			return (false);
		}

		/// <summary>
		/// Checks if one of the ALT keys is held down.
		/// </summary>
		/// <returns>True if either the left or right ALT keys are held down, false otherwise.</returns>
		public static bool IsAlt()
		{
			if (isKeyHeldDown(Key.LeftAlt) || isKeyHeldDown(Key.RightAlt, false)) {
				return (true);
			}
			return (false);
		}

		/// <summary>
		/// Checks to see if the press of this key is the first press, and optionallly polls the keyboard for the most up-to-date data.
		/// </summary>
		/// <param name="k">The key to check.</param>
		/// <param name="acquire">True if the keyboard should be polled before the key is checked, false otherwise. If false, this will save processing time where there are multiple keys to check in sequence.</param>
		/// <returns>True if this is the first press, false otherwise.</returns>
		public static bool isFirstPress(Key k, bool acquire)
		{
			if (acquire)
				updateKeyboardState();
			if (!keyboardAcquired) {
				TheKeys[(int)k] = false;
				return false;
			}

			if (TheKeys[(int)k] && diState.IsPressed(k))
				return (false);
			else if (!diState.IsPressed(k)) {
				TheKeys[(int)k] = false;
				return (false);
				//if the key is not held down, treat it as it is so the program doesn't intercept it.
			} else { //assume that theKeys(k) is false, so allow the key press
				TheKeys[(int)k] = true;
				return (true);
			}
		}

		/// <summary>
		/// Checks to see if the press of this key is the first press, and polls the keyboard for the most up-to-date data.
		/// </summary>
		/// <param name="k">The key to check.</param>
		/// <returns>True if this is the first press; else false</returns>
		public static bool isFirstPress(Key k)
		{
			return isFirstPress(k, true);
		}

		//Overloaded. Checks to see if any keys are held down. If they are, this function returns true; otherwise, it returns false.
		public static bool isKeyHeldDown()
		{
			if (!updateKeyboardState())
				return (false);

			bool pressed = false;
			foreach (Key k in diState.PressedKeys) {
				pressed = true;
				break;
			}
			return (pressed);
		}

		/// <summary>
		/// Checks if only one key is held down.
		/// </summary>
		/// <returns>True if only one key is held down, false otherwise.</returns>
		public static bool isSingleKeyHeldDown()
		{
			if (!updateKeyboardState()) {
				return (false);
			}

			int count = 0;
			foreach (Key k in diState.PressedKeys)
				count++;
			return (count == 1);
		}

		/// <summary>
		/// Checks to see if any of the provided keys are held down.
		/// </summary>
		/// <param name="KeyList">The keys to check.</param>
		/// <returns>If any one of the supplied keys are held down, this method returns true. It returns false otherwise.</returns>
		public static bool isKeyHeldDown(params Key[] KeyList)
		{
			if (!updateKeyboardState())
				return (false);
			short I = 0;
			for (I = 0; I < KeyList.Length; I++) {
				if (diState.IsPressed(KeyList[I])) {
					return (true);
				}
			}
			return (false);
		}

		/// <summary>
		/// Checks if any commonly used key is held down. These include Q through P, A through L, ENTER, Z through M, LEFT and RIGHT SHIFT, LEFT and RIGHT CONTROL, LEFT and RIGHT ALT, and SPACE.
		/// </summary>
		/// <returns>True if any of the common keys are held down, false otherwise.</returns>
		public static bool IsCommonKeyHeldDown()
		{
			return (isKeyHeldDown(Key.Q, Key.W, Key.E, Key.R, Key.T, Key.Y, Key.U, Key.I, Key.O, Key.P,
			Key.A, Key.S, Key.D, Key.F, Key.G, Key.H, Key.J, Key.K, Key.L, Key.Return,
			Key.LeftShift, Key.Z, Key.X, Key.C, Key.V, Key.B, Key.N, Key.M, Key.RightShift, Key.LeftControl,
			Key.LeftAlt, Key.Space, Key.RightControl, Key.RightAlt));
		}

		/// <summary>
		/// Gets an array of all pressed keys.
		/// </summary>
		/// <returns>An array containing a list of all keys that are currently pressed, or null if no keys are pressed.</returns>
		public static Key[] getKeys()
		{
			if (!updateKeyboardState())
				return null;
			int count = diState.PressedKeys.Count;
			if (count == 0)
				return null;
			Key[] theKeys = new Key[count];
			int i = 0;
			foreach (Key k in diState.PressedKeys) {
				theKeys[i] = k;
				i++;
			}
			return theKeys;
		}

		/// <summary>
		/// Gets the key that is the first in a list of pressed keys.
		/// </summary>
		/// <returns>The key being pressed, or Key.unknown if polling failed.</returns>
		public static Key getKeyPressed()
		{
			if (!updateKeyboardState() || diState.PressedKeys.Count == 0)
				return Key.Unknown;
			return diState.PressedKeys[0];
		}

		/// <summary>
		/// Presents the list of buttons on a joystick that are held down in an array.
		/// </summary>
		/// <returns>An array containing all the buttons that are held down, or null if no buttons are held down.</returns>
		public static int[] getJSKeys()
		{
			if (JSDevice == null)
				return (null);
			ArrayList theKeys = new ArrayList();
			int i = 0;
			bool[] pKeys = JSState.Buttons;
			for (i = 0; i < pKeys.Length; i++) {
				if (isJSButtonHeldDown(i))
					theKeys.Add(i);
			}
			if (theKeys.Count > 0) {
				int[] theArray = new int[theKeys.Count];
				for (i = 0; i < theArray.Length; i++)
					theArray[i] = (int)theKeys[i];
				return (theArray);
			}
			return (null);
		}

		/// <summary>
		/// Checks if a modifier is held down. This includes LEFT and RIGHT SHIFT, LEFT and RIGHT CONTROL, and LEFT and RIGHT ALT.
		/// </summary>
		/// <returns>True if any of the mentioned keys are held down, false otherwise.</returns>
		public static bool isModifierHeldDown()
		{
			return (isKeyHeldDown(Key.LeftAlt, Key.RightAlt, Key.LeftControl, Key.RightControl, Key.LeftShift, Key.RightShift));
		}

		/// <summary>
		/// Checks to see if a joystick button is being held down.
		/// </summary>
		/// <returns>True if a button is held; otherwise, false.</returns>
		public static bool isJSButtonHeldDown()
		{
			if (JSDevice == null) {
				return false;
			}
			foreach (bool b in JSState.Buttons) {
				if (b)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Checks to see if a joystick button is held down.
		/// </summary>
		/// <param name="b">The button to check, zero-based.</param>
		/// <returns>True if the button is held down; otherwise, false.</returns>
		public static bool isJSButtonHeldDown(int b)
		{
			if (JSDevice == null) {
				return false;
			}
			return JSState.Buttons[b];
		}

		public static int JSXAxis()
		{
			return (getMaskedAxis(0, JSState.X));
		}
		public static int JSYAxis()
		{
			return (getMaskedAxis(0, JSState.Y));
		}

		public static int JSZAxis()
		{
			InputRange r = JSDevice.GetObjectPropertiesById(jsZId).Range;
			return (r.Maximum - JSState.Z
				+ r.Minimum); //scale the values if the min range
							  //is not 0
		}
		public static int JSRZAxis()
		{
			return JSState.RotationZ;
		}
		public static bool JSXAxisIsCenter()
		{
			return (JSXAxis() == 0);
		}
		public static bool JSYAxisIsCenter()
		{
			return (JSYAxis() == 0);
		}

		/// <summary>
		/// Gets the position of the directional pad. This method only looks at the first dp on the device.
		/// </summary>
		/// <returns>The position of the directional pad.</returns>
		public static DirectionalPadPositions JSDirectionalPad()
		{
			if (JSDevice == null) {
				return (DirectionalPadPositions.neutral);
			}
			return ((DirectionalPadPositions)JSState.PointOfViewControllers[0]);
		}

		/// <summary>
		/// Checks to see if the directional pad is centered. This method only looks at the first available dp on the device.
		/// </summary>
		/// <returns></returns>
		public static bool JSDirectionalPadIsCenter()
		{
			return (JSDirectionalPad() == DirectionalPadPositions.neutral);
		}


		/// <summary>
		/// Checks to see if this is the first time the directional pad has been pressed. For instance, if the DP is
		/// pressed and then held down, on subsequent calls this method will return FALSE.
		/// </summary>
		/// <returns>True if this is the first time the dp has been pressed, false otherwise.</returns>
		public static bool isFirstPressJSDP()
		{
			if (JSDirectionalPad() != DirectionalPadPositions.neutral) {
				if (lastDPPosition == JSDirectionalPad())
					return (false);
				else {
					lastDPPosition = JSDirectionalPad();
					return (true);
				}
			} //if !neutral

			lastDPPosition = DirectionalPadPositions.neutral;
			return (false);
		}

		/// <summary>
		/// Checks to see if the given direction is pressed for the first time on the directional pad.
		/// </summary>
		/// <param name="d">The direction to check.</param>
		/// <returns>True if the direction is pressed for the first time, otherwise false.</returns>
		public static bool isFirstPressJSDP(DirectionalPadPositions d)
		{
			if (JSDirectionalPad() != DirectionalPadPositions.neutral) {
				if (JSDirectionalPad() == d) {
					if (lastDPPosition == d)
						return (false);
					lastDPPosition = d;
					return (true);
				} //if pressing the indicated direction
				else
					return false;
			} //if !neutral
			  //if neutral  next press will pass this function and return true.
			lastDPPosition = DirectionalPadPositions.neutral;
			return (false);
		}

		private static int getMaskedAxis(int center, int realAxis)
		{
			int final = Math.Abs(center - realAxis);
			if (realAxis < center) {
				return (-final);
			}
			return (final);
		}

		/// <summary>
		///Because values go up as the down slider is pressed, this method will reverse the slider output so that 0 means the slider is at
		///the lower extreme value.
		/// </summary>
		/// <returns>The reversed slider output.</returns>
		public static int JSSlider()
		{
			InputRange r = JSDevice.GetObjectPropertiesById(JSSliderId).Range;
			return (r.Maximum - JSState.Sliders[0]
				+ r.Minimum); //scale the values if the min range
							  //is not 0
		}

		public static void updateJSState()
		{
			if (JSDevice != null)
				JSDevice.Poll();
		}

		/// <summary>
		/// Checks to see if the specified button is pressed for the first time. For instance, if the button is pressed and held
		/// the first call to this method will return true, and all subsequent calls will return false until the button is no longer pressed.
		/// </summary>
		/// <param name="k">The button to check, zero-based.</param>
		/// <returns>True if this is the first press, false otherwise.</returns>
		public static bool isFirstPressJSB(int k)
		{
			if (JSDevice == null)
				return false;
			if (TheJSButtons[k] && JSState.Buttons[k])
				return false;
			else if (!JSState.Buttons[k]) {
				TheJSButtons[k] = false;
				return false; //if the key is not held down, treat it as it is so the program doesn't intercept it.
			} else { //assume that theKeys(k) is false, so allow the key press
				TheJSButtons[k] = true;
				return true;
			}
		}

		private static bool isKeyboardAcquired()
		{
			if (diDev == null)
				return false;

			//The try/catch block traps any potential null reference exceptions or exceptions while acquiring the device
			//Occasionally, these will occur while acquiring the device.
			try {
				diDev.Acquire();
			}
			catch (SharpDXException) {
				return false;
			}
			catch (NullReferenceException) {
				return false;
			}
			return true;
		}

		/// <summary>
		///Resets variables so that the next press on the dp
		///will be the first press, even if it actually isn't.
		/// </summary>
		public static void resetJSDP()
		{
			lastDPPosition = DirectionalPadPositions.neutral;
		}

		////Will reset all keys
		////so that they're first presses,
		////even if they actually aren't.
		public static void resetKeys()
		{
			int i = 0;
			for (i = 0; i <= TheKeys.Length - 1; i++) {
				TheKeys[i] = false;
			}
		}

		/// <summary>
		///Will reset all jsButtons so that they're first presses,
		///even if they actually aren't.
		/// </summary>
		public static void resetJSB()
		{
			if (JSDevice == null) {
				return;
			}
			for (int i = 0; i < TheJSButtons.Length; i++)
				TheJSButtons[i] = false;
		}

		public static bool isMenuKeyPressed()
		{
			if (!updateKeyboardState())
				return (false);
			return (diState.IsPressed(Key.Left)
				|| diState.IsPressed(Key.Right)
				|| diState.IsPressed(Key.Up)
				|| diState.IsPressed(Key.Down)
				|| diState.IsPressed(Key.Return)
				|| diState.IsPressed(Key.Escape)
				|| diState.IsPressed(Key.Home)
				|| diState.IsPressed(Key.End)
				|| isJSButtonHeldDown(0)
				|| isJSButtonHeldDown(1)
				|| !JSDirectionalPadIsCenter());
		}

		/// <summary>
		/// In case the need should arise to scale values when the device has been acquired already,
		/// this method will allow to do that.
		/// </summary>
		/// <param name="lower">The new lower range.</param>
		/// <param name="upper">The new upper range.</param>
		public static void setJSSliderRange(int lower, int upper)
		{
			if (JSDevice == null || !useSlider)
				return;
			JSDevice.Unacquire();
			JSDevice.GetObjectPropertiesById(JSSliderId).Range = new InputRange(lower, upper);
			JSDevice.Acquire();
			updateJSState();
		}


		/// <summary>
		/// In case the need should arise to scale values when the device has been acquired already,
		/// this method will allow to do that.
		/// </summary>
		/// <param name="lower">The new lower range.</param>
		/// <param name="upper">The new upper range.</param>
		public static void setJSZRange(int lower, int upper)
		{
			if (JSDevice == null || !useZ)
				return;
			JSDevice.GetObjectPropertiesById(jsZId).Range = new InputRange(lower, upper);
			updateJSState();
		}

		//updates the state of the keyboard,
		//returns true if successful, false otherwise eg.
		//keyboard not acquired.
		//Method also modifies keyboardAcquired, in case a call to this method
		//fails; other programs don't have to keep
		//processing this method to see if
		//keyboard is not acquired.
		public static bool updateKeyboardState()
		{
			if (!isKeyboardAcquired()) {
				keyboardAcquired = false;
				return (false);
			}

			try {
				diState = diDev.GetCurrentState();
				keyboardAcquired = true;
				return (true);
			}
			catch {
				keyboardAcquired = false;
				return false;
			}
		}

		/// <summary>
		/// Starts the specified forcefeedback effect.
		/// </summary>
		/// <param name="effect">The effect to start.</param>
		private static void startEffect(Effect effect)
		{
			try { //Effect may not be downloaded
				if (!forceFeedbackEnabled || effect.Status == EffectStatus.Playing)
					return;
			}
			catch (SharpDXException) {
				if (forceFeedbackEnabled)
					effect.Start();
				return;
			}
			effect.Start();
		}

		public static void startAfterburnerEffect()
		{
			startEffect(afterburnerEffect);
		}

		public static void stopAfterburnerEffect()
		{
			if (!forceFeedbackEnabled)
				return;
			try {
				afterburnerEffect.Stop();
			}
			catch {
				//silently fail
			}
		}

		public static void startGForceEffect()
		{
			startEffect(gForceEffect);
		}

		public static void stopGForceEffect()
		{
			if (!forceFeedbackEnabled)
				return;

			gForceEffect.Stop();
		}

		public static void startCruiseMissileEffect()
		{
			startEffect(cruiseMissileEffect);
		}

		public static void stopCruiseMissileEffect()
		{
			if (!forceFeedbackEnabled)
				return;

			cruiseMissileEffect.Stop();
		}

		public static void startFireEffect()
		{
			startEffect(fireEffect);
		}

		public static void startExplodeEffect()
		{
			startEffect(explodeEffect);
		}

		public static void startHitEffect()
		{
			startEffect(hitEffect);
		}

		public static void unacquireJoystick(bool destroyOjbect)
		{
			if (JSDevice != null) {
				unloadEffect(afterburnerEffect, destroyOjbect);
				unloadEffect(cruiseMissileEffect, destroyOjbect);
				unloadEffect(fireEffect, destroyOjbect);
				unloadEffect(gForceEffect, destroyOjbect);
				unloadEffect(hitEffect, destroyOjbect);
				if (destroyOjbect) {
					unloadEffect2(true); //these effects were not stopped in game,
										 //but we've been requested to unacquire the joystick.
					JSDevice.Unacquire();
					JSDevice = null;
					forceFeedbackEnabled = false;
				} //if destroyObject
			} //if jsdevice != null
		}

		private static void unloadEffect(Effect e, bool unload)
		{
			if (forceFeedbackEnabled) {
				try {
					e.Stop();
				}
				catch (SharpDXException err) {
					if (err.Message.Contains("DIERR_NOTDOWNLOADED"))
						return;
					else
						throw;
				} //couldn't stop effect
				if (unload) {
					e.Unload();
					e = null;
				} //if unload

			} //if
		}

		//Called by cleanup method after effects are done playing.
		//Method intended to clean up effects that should not be prematurely stopped,
		//for instance, an effect of player's mobile blowing up.
		public static void unloadEffect2(bool unload)
		{
			unloadEffect(explodeEffect, unload);
		}


		public static void waitTillNothingHeld()
		{
			while (isKeyHeldDown() || isJSButtonHeldDown())
				Thread.Sleep(100);
		}

		public static void cleanUp()
		{
			diDev.Dispose();
			if (forceFeedbackEnabled) {
				afterburnerEffect.Dispose();
				gForceEffect.Dispose();
				cruiseMissileEffect.Dispose();
				fireEffect.Dispose();
				hitEffect.Dispose();
			}
			if (JSDevice != null)
				JSDevice.Dispose();
			m_input.Dispose();
		}

		public static bool isJSEnabled()
		{
			return JSDevice != null;
		}
	}
}