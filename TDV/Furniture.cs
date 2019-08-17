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
	public class Furniture : FightObject
	{
		private BoundingBox box;
		private ExtendedAudioBuffer destroySound, crashSound;
		protected String destroyFile, hitFile;
		public int length { get; set; }
		public int width { get; set; }


		/// <summary>
		/// Creates new furniture
		/// </summary>
		/// <param name="x">The mid x value.</param>
		/// <param name="y">The mid y value.</param>
		/// <param name="damage">The starting damage value.</param>
		/// <param name="length">The length.</param>
		/// <param name="width">The width</param>
		public Furniture(int x, int y, int damage, int length, int width)
			: base(x, y, damage, true)
		{
			this.length = length;
			this.width = width;
			box = new BoundingBox(new Vector3(x - width, y - length, 0), new Vector3(x + width, y + length, 0));
		}

		/// <summary>
		/// Hits this furniture.
		/// </summary>
		/// <param name="max">Maximum damage value subtracted.</param>
		/// <returns>True if this furniture is destroyed, false otherwise.</returns>
		public override bool hit(int max)
		{
			damage -= Common.getRandom(1, max);
			return damage <= 0;
		}

		public void playDestroy(bool isAI)
		{
			bool isAITemp = this.isAI;
			this.isAI = isAI;
			if (destroySound != null)
				DSound.unloadSound(ref destroySound);
			destroySound = loadSound((damage <= 0) ? destroyFile : hitFile);
			playSound(destroySound, true, false);
			this.isAI = isAITemp;
		}

		public override void cleanUp()
		{
			DSound.unloadSound(ref destroySound);
			DSound.unloadSound(ref crashSound);
		}

		public override bool clearForRemoval()
		{
			if (damage > 0)
				return false;
			return destroySound != null && !DSound.isPlaying(destroySound);
		}

		public override bool isBlocked(int x, int y)
		{
			if (damage <= 0)
				return false;
			return x >= this.x - width && x <= this.x + width && y >= this.y - length && y <= this.y + length;
		}

		/// <summary>
		/// Determines if this furniture blocks the specified ray.
		/// </summary>
		/// <param name="ray">The ray to check.</param>
		/// <param name="distance">How far the ray should be projected before it hits the object.</param>
		/// <returns>True if the furniture intersects the ray, false otherwise.</returns>
		public virtual bool isBlocked(Ray ray, out float distance)
		{
			if (damage <= 0)
			{
				distance = 0.0f;
				return false;
			}
			return box.Intersects(ref ray, out distance);
		}

		/// <summary>
		/// Gets the closest corner of this furniture to the specified coordinates.
		/// </summary>
		/// <param name="x">The x coordinate of the source object.</param>
		/// <param name="y">The y coordinate of the source.</param>
		/// <returns>A Vectore2 containing the closest corner to this set of coordinates.</returns>
		public Vector2 getClosestCorner(int x, int y)
		{
			Vector3 min = Vector3.Zero;
			foreach (Vector3 point in box.GetCorners())
			{
				if (min == Vector3.Zero)
					min = point;
				else
				{
					if (Degrees.getDistanceBetween(x, y, point.X, point.Y) < Degrees.getDistanceBetween(x, y, min.X, min.Y))
						min = point;
				}
			}
			return new Vector2(min.X, min.Y);
		}

		/// <summary>
		/// Plays the crash sound.
		/// </summary>
		/// <param name="x">The x coordinate where to position the sound.</param>
		/// <param name="y">The y coordinate where to position the sound.</param>
		public override void playCrashSound(int x, int y)
		{
			if (crashSound == null)
				crashSound = loadSound(hitFile);
			if (DSound.isPlaying(crashSound))
				return;
			DSound.PlaySound3d(crashSound, true, false, x, 0, y);
		}
	}
}
