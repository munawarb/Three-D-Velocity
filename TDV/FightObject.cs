/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.VectorCalculation;

namespace TDV
{
	public class FightObject
	{
		public List<FightObject> things;
		public int damage { get; set; }
		public int maxDamage { get; set; }
		public int x, y;
		protected bool isAI;

		public FightObject(int x, int y, int damage, bool isAI)
		{
			this.x = x;
			this.y = y;
			this.damage = damage;
			this.maxDamage = damage;
			this.isAI = isAI;
			things = new List<FightObject>();
		}

		/// <summary>
		/// Hits this object.
		/// </summary>
		/// <param name="max">The maximum amount of damage to subtract.</param>
		/// <returns>True if dead, false otherwise</returns>
		public virtual bool hit(int max)
		{
			damage -= Common.getRandom(1, max);
			return damage <= 0;
		}

		/// <summary>
		/// Gets the remaining health of this object.
		/// </summary>
		/// <returns>The health expressed as a percent</returns>
		public int getHealthPercentage()
		{
			return (int)((double)damage / maxDamage * 100);
		}

		/// <summary>
		/// Gets the distance between this object and another object.
		/// </summary>
		/// <param name="f">The other object</param>
		/// <returns>The distance</returns>
		public float distanceBetween(FightObject f)
		{
			return Degrees.getDistanceBetween(x, y, f.x, f.y);
		}

		/// <summary>
		/// Punches this object.
		/// </summary>
		/// <param name="max">The max damage to subtract</param>
		/// <returns>True if dead, false otherwise.</returns>
		public virtual bool punch(int max)
		{
			return hit(max);
		}

		/// <summary>
		/// Gets all objects of type T within one unit of this object.
		/// </summary>
		/// <typeparam name="T">The type of the object to search for.</typeparam>
		/// <returns>A list of type T, or null if nothing is in range.</returns>
		public List<T> getObjectInRangeByType<T>()
		{
			List<T> lowest = new List<T>();
			foreach (Object o in things)
			{
				if (this == o || !(o is T))
					continue;
				if (distanceBetween((FightObject)o) <= 1)
					lowest.Add((T)o);
			}
			if (lowest.Count == 0)
				return null;
			else
				return lowest;
		}

		/// <summary>
		/// Gets all objects of a specified type.
		/// </summary>
		/// <typeparam name="T">The type of the object to collect.</typeparam>
		/// <returns>A list of this object or null if nothing is found.</returns>
		public List<T> getAllObjects<T>()
		{
			List<T> lowest = new List<T>();
			foreach (Object o in things)
			{
				if (this == o || !(o is T))
					continue;
				lowest.Add((T)o);
			}
			if (lowest.Count == 0)
				return null;
			else
				return lowest;
		}



		/// <summary>
		/// Gets the objects in one unit range of this object.
		/// </summary>
		/// <returns>A List of FightObjects or null if nothing in range.</returns>
		public List<FightObject> getObjectInRange()
		{
			List<FightObject> lowest = new List<FightObject>();
			foreach (FightObject o in things)
			{
				if (this == o)
					continue;
				if (distanceBetween(o) <= 1)
					lowest.Add(o);
			}
			if (lowest.Count == 0)
				return null;
			else
				return lowest;
		}

		/// <summary>
		/// True if o is within range of this object, false otherwise.
		/// </summary>
		/// <param name="o">The object to measure by</param>
		/// <returns>True if in range, false otherwise</returns>
		public bool isInRange(FightObject o)
		{
			return distanceBetween(o) <= 1;
		}

		/// <summary>
		/// Determines if the target coordinates are in range of this object.
		/// </summary>
		/// <param name="target">The Vector2 representing the target coordinates.</param>
		/// <returns>True if in range, false otherwise.</returns>
		public bool isInRange(Vector2 target)
		{
			return Degrees.getDistanceBetween(x, y, target.X, target.Y) <= 1;
		}

		/// <summary>
		/// Gets all furniture in range of this object along the two axes.
		/// </summary>
		/// <param name="distance">The maximum distance that should be searched.</param>
		/// <returns>A list of FightObjectFeeler containing furniture and the x and y coordinates at which there is the first intersection. Null if no objects were found.</returns>
		protected List<FightObjectFeeler> getFurnitureInRange(int distance)
		{
			List<FightObjectFeeler> l = new List<FightObjectFeeler>();
			List<Furniture> things = getAllObjects<Furniture>();
			if (things == null)
				return null;
			//left of this object
			Ray r = new Ray(new Vector3(x, y, 0), new Vector3(-1, 0, 0));
			float d;
			foreach (Furniture f in things)
			{
				if (f.isBlocked(r, out d) && d <= distance)
					l.Add(new FightObjectFeeler(f, (int)(x - d + 1), y, (int)d));
			}
			//Right of this object
			r = new Ray(new Vector3(x, y, 0), new Vector3(1, 0, 0));
			foreach (Furniture f in things)
			{
				if (f.isBlocked(r, out d) && d <= distance)
					l.Add(new FightObjectFeeler(f, (int)(x + d - 1), y, (int)d));
			}
			//In front of this object
			r = new Ray(new Vector3(x, y, 0), new Vector3(0, 1, 0));
			foreach (Furniture f in things)
			{
				if (f.isBlocked(r, out d) && d <= distance)
					l.Add(new FightObjectFeeler(f, x, (int)(y + d - 1), (int)d));
			}
			//Behind this object.
			r = new Ray(new Vector3(x, y, 0), new Vector3(0, -1, 0));
			foreach (Furniture f in things)
			{
				if (f.isBlocked(r, out d) && d <= distance)
					l.Add(new FightObjectFeeler(f, x, (int)(y - d + 1), (int)d));
			}
			if (l.Count == 0)
				return null;
			l.Sort();
			return l;
		}

		public virtual bool isBlocked(int x, int y)
		{
			return x == this.x && y == this.y;
		}

		/// <summary>
		/// Seeks the FightObject
		/// </summary>
		/// <param name="f">The object to seek.</param>
		protected Person.MovementDirection seek(FightObject f)
		{
			if (distanceBetween(f) <= 1)
				return Person.MovementDirection.none;
			if (f.x > x && !isBlockedByObject(x + 1, y))
				return Person.MovementDirection.east;
			else if (f.x < x && !isBlockedByObject(x - 1, y))
				return Person.MovementDirection.west;
			else if (f.y > y && !isBlockedByObject(x, y + 1))
				return Person.MovementDirection.north;
			else if (f.y < y && !isBlockedByObject(x, y - 1))
				return Person.MovementDirection.south;
			return Person.MovementDirection.none;
		}

		/// <summary>
		/// Seeks the node.
		/// </summary>
		/// <param name="node">The node to seek.</param>
		protected Person.MovementDirection seek(MapNode node)
		{
			if (Degrees.getDistanceBetween(x, y, node.position.X, node.position.Y) == 0)
				return Person.MovementDirection.none;
			if (node.position.X > x && !isBlockedByObject(x + 1, y))
				return Person.MovementDirection.east;
			else if (node.position.X < x && !isBlockedByObject(x - 1, y))
				return Person.MovementDirection.west;
			else if (node.position.Y > y && !isBlockedByObject(x, y + 1))
				return Person.MovementDirection.north;
			else if (node.position.Y < y && !isBlockedByObject(x, y - 1))
				return Person.MovementDirection.south;
			return Person.MovementDirection.none;
		}

		/// <summary>
		/// Plays a sound.
		/// </summary>
		/// <param name="theSound">The sound to play.</param>
		/// <param name="stopFlag">If the sound should be stopped before playing.</param>
		/// <param name="loopFlag">Whether the sound should be looped or not.</param>
		protected void playSound(ExtendedAudioBuffer theSound, bool stopFlag, bool loopFlag)
		{
			if (isAI)
				DSound.PlaySound3d(theSound, stopFlag, loopFlag, x, 0, y);
			else
				DSound.PlaySound(theSound, stopFlag, loopFlag);
		}

		protected ExtendedAudioBuffer loadSound(string filename)
		{
			ExtendedAudioBuffer s = null;
			if (isAI)
				s = DSound.LoadSound(DSound.SoundPath + "\\a_" + filename);
			else
				s = DSound.LoadSound(DSound.SoundPath + "\\" + filename);
			return s;
		}

		/// <summary>
		/// Loads sounds into an array of buffers.
		/// </summary>
		/// <param name="filename">The file name to use as the template. Should not include the .wav extension</param>
		/// <param name="n">The number of files to load</param>
		/// <returns>An array populated with sound buffers.</returns>
		protected ExtendedAudioBuffer[] loadSoundArray(String filename, int n)
		{
			ExtendedAudioBuffer[] sounds = new ExtendedAudioBuffer[n];
			for (int i = 0; i < sounds.Length; i++)
				sounds[i] = loadSound(filename + (i + 1) + ".wav");
			return sounds;
		}

		public virtual bool clearForRemoval()
		{
			return false;
		}

		protected bool isBlockedByObject(int x, int y)
		{
			foreach (FightObject o in things)
			{
				if (o.isBlocked(x, y))
					return true;
			}
			return false;
		}

		public virtual void playCrashSound(int x, int y)
		{
			//stub
		}

		public virtual void cleanUp()
		{

		}

	}
}
