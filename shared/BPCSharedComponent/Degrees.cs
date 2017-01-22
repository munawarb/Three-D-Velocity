/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Threading;
namespace BPCSharedComponent.VectorCalculation
{
	//degrees class.
	//Written and copyrighted by Munawar Bijani on 04/14/2005, with the help of http://forum.audiogames.net/viewtopic.php?id=85
	//Note: all methods and properties found within this class are static, therefore an explicit instantiation of this class is not required for these methods and properties to be accessible.
	//Last updated: 06/28/2007
	////**Updated: Improved algorithms for moveObject and changeDir.
	////**Updated: Added getShortestRotation method and Rotation enumeration.
	/// <summary>
	/// Does clock mark calculations.
	/// </summary>
	public class Degrees
	{
		/// <summary>
		/// The following enumeration defines rotation direction.
		/// </summary>
		public enum Rotation
		{
			counterClockwise = 1,
			clockwise = 2,
			equal = 3,
			stationary = 4
		}

		private const Int16 m_North = 0;
		private const Int16 m_East = 90;
		private const Int16 m_South = 180;
		private const Int16 m_West = 270;
		private const Int16 m_NorthEast = 45;
		private const Int16 m_NorthWest = 315;
		private const Int16 m_SouthEast = 135;
		private const Int16 m_SouthWest = 225;

		//--------
		//the following properties return the constant degree values for cardinal and subcardinal directions.
		//Note: this class runs under the assumption that 0.0 represents North, and progressing
		//degree values are in the clockwise direction.
		//Thus, East is 90.0 degrees.
		public static Int16 NORTH
		{
			get { return (m_North); }
		}
		public static Int16 EAST
		{
			get { return (m_East); }
		}
		public static Int16 SOUTH
		{
			get { return (m_South); }
		}
		public static Int16 WEST
		{
			get { return (m_West); }
		}
		public static Int16 NORTHEAST
		{
			get { return (m_NorthEast); }
		}
		public static Int16 SOUTHEAST
		{
			get { return (m_SouthEast); }
		}
		public static Int16 SOUTHWEST
		{
			get { return (m_SouthWest); }
		}
		public static Int16 NORTHWEST
		{
			get { return (m_NorthWest); }
		}

		/// <summary>
		/// Scales down a degree value. For instance, if given 361, the method returns 1.
		/// </summary>
		/// <param name="dir">The degree value to adjust</param>
		/// <returns>The adjusted value.</returns>
		public static int getDegreeValue(int dir)
		{
			if (dir >= 0)
				dir %= 360;
			else
				dir = 360 + (dir % 360);
			if (dir == 360)
				dir = 0;
			return dir;
		}

		/// <summary>
		/// Scales down a degree value. For instance, if given 361, the method returns 1.
		/// </summary>
		/// <param name="dir">The degree value to adjust</param>
		/// <returns>The adjusted value.</returns>
		public static float getDegreeValue(float dir)
		{
			return (float)getDegreeValue((int)Math.Floor(dir)) + (dir - (float)Math.Floor(dir));
		}


		////Adjusts a given degree value,
		////such that the returned value is shifted 90 degrees counterclockwise.
		////This way, 0.0 represents North and 90.0 represents East.
		private static int adjustDirection(int dir)
		{
			if (dir >= NORTH & dir <= EAST)
			{
				dir = 90 - dir;
			}
			else if (dir > EAST & dir <= SOUTH)
			{
				dir = 180 - dir + 270;
			}
			else if (dir > SOUTH & dir <= WEST)
			{
				dir = 270 - dir + 180;
			}
			else if (dir > WEST & dir <= 359)
			{
				dir = 359 - dir + 1 + 90;
			}
			return (dir);
		}

		///<summary>
		///expects an object's coordinates and updates their values based on the information supplied
		///</summary>
		///<param name="X">The starting x coordinate of the object.</param>param>
		///<param name="Y">The starting y coordinate of the object.</param>
		///<param name="Dir">The naval direction of the object.</param>
		///<param name="speed">The speed of the object.</param>
		///<param name="timeElapsed">The amount of time elapsed between the time this method was last called.</param>
		///<remarks>Assuming timeElapsed is measured in seconds, this method will return the coordinates after one second of travel; if timeElapsed==0.5, this method will return the coordinates after 1/2 seconds of travel, or after 500 milliseconds.
		///Note: the unit time and time elapsed variables must match.
		///For instance, if an object moved 5 miles/milliseconds, a value of 1 to the time elapsed variable will assume that 1 millisecond has passed between calls of this method.</remarks> 
		public static void moveObject(ref double X, ref double Y, int Dir, double speed, double timeElapsed)
		{
			Dir = adjustDirection(Dir);
			double RadiansDir = toRadians(Dir);
			Interlocked.Exchange(ref X, X + speed * Math.Cos(RadiansDir) * timeElapsed);
			Interlocked.Exchange(ref Y, Y + speed * Math.Sin(RadiansDir) * timeElapsed);
		}

		//Returns the vertical and horizontal maximum speed of an object with a nose angle
		//defined by v.direction. The ground is assumed to be the base line by which the nose angle of the object is calculated.
		//For instance, if 90 degrees were passed to this method, the object would move straight upward with no horizontal thrust.
		//The information calculated by this method is stored in a Range structure, which contains the horizontal and vertical speed of the object.
		//For instance, if the speed value of 5 miles/hour was passed, a hypothetical situation is demonstrated below.
		//Assuming the nose angle was 45.0 degrees, the returned Range would yield a horizontal speed of 2.5 miles/hour, and the vertical speed would yield 2.5 miles/hour.
		public static Range getHVSpeed(Velocity v)
		{
			double radianDir = v.direction * 2.0 * Math.PI / 360.0;
			return (new Range(v.speed * Math.Cos(radianDir), v.speed * Math.Sin(radianDir)));
		}

		/// <summary>
		///expects an object's coordinates and updates their values based on the information supplied by the Range structure.
		/// </summary>
		/// <param name="z">The starting Z coordinate of the object.</param>
		/// <param name="speed">A range structure describing the horizontal and vertical speed of the object.</param>
		public static void moveObject(ref double X, ref double Y, ref double z, int Dir, Range speed, double timeElapsed)
		{
			//First, call the original moveObject method to update x and y
			moveObject(ref X, ref Y, Dir, speed.horizontalDistance, timeElapsed);
			//Next, update the z value based on the speed in the speed Range.
			//This value is a speed vector, so simply adding it to the z value will yield the new coordinate for z.
			Interlocked.Exchange(ref z, z + speed.verticalDistance * timeElapsed);
		}

		////The following method  expects two points in space and returns the angle at which one object must face to be in the direct line of the other object,
		////such that the object at coordinates (x1, y1) must face the course of 
		////degrees.getDegreesBetween(x1, y1, x2, y2)
		////to be able to orient itself on a collision with an object stationed at (x2, y2).
		public static int GetDegreesBetween(double X1, double Y1, double X2, double Y2)
		{
			double TheTa = 0;
			double X = 0;
			double Y = 0;
			X = X1 - X2;
			Y = Y2 - Y1;
			TheTa = Math.Atan2(-X, Y);
			if (TheTa < 0)
			{
				TheTa = TheTa + 2.0 * Math.PI;
			}
			return (int)(TheTa * 180 / Math.PI);
		}

		////The following method returns the distance between two ordered pairs.
		public static double getDistanceBetween(double X1, double Y1, double X2, double Y2)
		{
			return (System.Math.Sqrt(System.Math.Pow(X2 - X1, 2) + System.Math.Pow(Y2 - Y1, 2)));
		}

		////The following method returns a boolean flag indicating whether the supplied degree value is a cardinal direction.
		public static bool isCardinalDirection(Int16 TheDir)
		{
			return (TheDir == NORTH | TheDir == EAST | TheDir == SOUTH | TheDir == WEST);
		}

		////The following method returns a boolean flag indicating whether the supplied degree value is a subcardinal direction.
		public static bool IsSubcardinalDirection(Int16 TheDir)
		{
			return (TheDir == NORTHEAST | TheDir == SOUTHEAST | TheDir == SOUTHWEST | TheDir == NORTHWEST);
		}

		////The following method is of type Rotation.
		////It determines the rotation direction that is the quickest (viz. shortest distance) from dir to destDir
		////dir: Current direction in degrees of the object.
		////destDir: The destination direction of the object.
		////For instance, if the current object's direction was 50 degrees, and the shortest rotation to 70 degrees was to be calculated, this method would be called by using
		////Degrees.getShortestRotation(50, 70)
		////For this call, the method would return the value Degrees.Rotation.Clockwise,
		////because going from 50 to 70 is shorter if one turns to the right, rather than left and going all the way around.
		public static Rotation getShortestRotation(int dir, int destDir)
		{
			int counterClockwise = 0;
			int clockwise = 0;
			if ((destDir < dir))
			{
				counterClockwise = Math.Abs(destDir - dir);
				clockwise = Math.Abs(359 - dir + destDir);
			}
			if ((destDir > dir))
			{
				clockwise = Math.Abs(destDir - dir);
				counterClockwise = Math.Abs(dir + (359 - destDir));
			}
			if ((counterClockwise < clockwise))
			{
				return (Rotation.counterClockwise);
			}
			if ((clockwise < counterClockwise))
			{
				return (Rotation.clockwise);
			}
			if ((dir == destDir))
			{
				return (Rotation.stationary);
			}
			if ((counterClockwise == clockwise))
			{
				return (Rotation.equal);
			}
			return (Rotation.equal);
		}

		//Returns a RelativePosition structure that defines the
		//position of jbect 2 to object 1, from the perspective of object 1.
		public static RelativePosition getPosition(double x1, double y1, double z1, int dir1,
			double x2, double y2, double z2, int dir2,
			bool isObject)
		{
			RelativePosition r = new RelativePosition();
			r.distance = getDistanceBetween(x1, y1, x2, y2);
			r.degrees = GetDegreesBetween(x1, y1, x2, y2);
			int f1 = getDegreeValue(dir1 - 80);
			int f2 = getDegreeValue(dir1 + 80);
			Rotation rDir = getShortestRotation(dir1, r.degrees);
			r.isAhead = inRange(f1, f2, r.degrees);

			//Since front view is at dir1, back view is dir1+180, and the
			//respective spread.
			r.isBehind = inRange(getDegreeValue(dir1 + 180 - 80),
								 getDegreeValue(dir1 + 180 + 80),
								 r.degrees);

			////check for view
			//isObject is true if the target object is
			//something that is not a flying object. eg. a SAM battery,
			//in which case views will not be relevant.
			if (!isObject)
			{
				int oD = getDegreeValue(r.degrees + 180);
				////since d is the degree at which object1 must face to be on a
				////collision path with object2, d+180 is the exact
				////degree mark object2 must be on to face object1 on a collision path.
				r.isNoseFacing = inRange(getDegreeValue(oD - 30),
										 getDegreeValue(oD + 30),
										 dir2);
				oD -= 180;
				r.isTailFacing = inRange(getDegreeValue(oD - 30),
										 getDegreeValue(oD + 30),
										 dir2);
				r.isWingFacing = !r.isNoseFacing && !r.isTailFacing;
			} //if !isObject

			r.degreesDifference = getDifference(dir1, r.degrees);

			if (rDir == Rotation.counterClockwise)
				r.relativeDegrees = 360 - r.degreesDifference;
			else if (rDir == Rotation.clockwise)
				r.relativeDegrees = r.degreesDifference;
			else if (rDir == Rotation.equal)
				r.relativeDegrees = 180;
			else if (rDir == Rotation.stationary)
				r.relativeDegrees = 0;

			r.clockMark = getClockValue(r.relativeDegrees);
			r.vDistance = z2 - z1;
			return r;
		}

		//Overloaded. Should be called if the target object is an aircraft or other flying object
		//for which views would be relevant
		public static RelativePosition getPosition(double x1, double y1, double z1, int dir1,
			double x2, double y2, double z2, int dir2)
		{
			return (getPosition(x1, y1, z1, dir1,
				x2, y2, z2, dir2,
				false));
		}

		public static bool inRange(int ld, int rd, int target)
		{
			return (ld < rd && target >= ld && target <= rd)
				|| (ld > rd && (target >= ld || target <= rd))
			|| (ld == rd && target == ld);
		}

		////Returns the difference between two degree values.
		////For instance, passing 0 and 358 to this function would return 2.
		public static int getDifference(int dir1, int dir2)
		{
			Rotation r = getShortestRotation(dir1, dir2);
			if (r == Rotation.equal)
				return 180;
			if (r == Rotation.counterClockwise)
			{
				if (dir2 < dir1)
					return dir1 - dir2;
				else
					return 360 - dir2 + dir1;
			}

			if (r == Rotation.clockwise)
			{
				if (dir1 < dir2)
					return dir2 - dir1;
				else
					return 360 - dir1 + dir2;
			}
			return 0;
		}

		public static double getCircumference(double r)
		{
			return (2.0 * Math.PI * r);
		}


		public static int getClockValue(int dir)
		{
			if (dir >= 1 && dir <= 30)
				return 1;
			else if (dir >= 31 && dir <= 60)
				return 2;
			else if (dir >= 61 && dir <= 90)
				return 3;
			else if (dir >= 91 && dir <= 120)
				return 4;
			else if (dir >= 121 && dir <= 150)
				return 5;
			else if (dir >= 151 && dir <= 180)
				return 6;
			else if (dir >= 181 && dir <= 210)
				return 7;
			else if (dir >= 211 && dir <= 240)
				return 8;
			else if (dir >= 241 && dir <= 270)
				return 9;
			else if (dir >= 271 && dir <= 300)
				return 10;
			else if (dir >= 301 && dir <= 330)
				return 11;
			else
				return 12;
		}

		/// <summary>
		/// Converts degree value to radians
		/// </summary>
		/// <param name="d">The degree value to convert</param>
		/// <returns>The degree value in radians</returns>
		public static double toRadians(int d)
		{
			return d * 2.0 * Math.PI / 360.0;
		}
	}
}
