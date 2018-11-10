/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Threading;
using System.IO;
using SharpDX.DirectSound;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
namespace TDV
{
	public class MidAirRefueler : MissionObjectBase
	{
		private long time;
		private SecondarySoundBuffer moveSound;
		public SecondarySoundBuffer message;
		private bool firstLoad;
		private double sx;
		private double sy;
		private bool isCalling;
		public bool isConnecting;
		public Range startConnectManeuverRange;
		public Range connectRange;

		public MidAirRefueler(double x, double y)
			: base("r", null)
		{
			isObject = false;
			firstLoad = true;
			showInList = false;
			setSpan(0.3, 0.6);
			if (!Options.isLoading)
				initPosition(x, y);
			neutralizeSpeed(1500.0);
			weapon.weaponIndex = WeaponTypes.battleShipGuns;
			moveSound = loadSound(soundPath + "e4.wav");
			setDamagePoints(500);
			explodeString = soundPath + "d3.wav";
			startConnectManeuverRange = new Range(5.0, 50.0);
			connectRange = new Range(2.0, 50.0);
			addVolume(moveSound);
		}

		public MidAirRefueler()
			: this(0.0, 0.0)
		{ }

		public void initPosition(double x, double y)
		{
			this.x = x;
			this.y = y;
			sx = x;
			sy = y;
		}

		public override void move()
		{
			if (firstLoad)
			{
				playSound(moveSound, true, true);
				firstLoad = false;
			}
			performDeaths();
			if (readyToTerminate())
			{
				if (moveSound != null)
					moveSound.Stop();
				isProjectorStopped = true;
				return;
			}
			if (!isRequestedTerminated)
			{
				base.move();
				playSound(moveSound, false, true);
				base.updateTotalDistance();

				if (isCalling)
				{
					z = Mission.player.z;
					if (!isConnecting)
						direction = Degrees.GetDegreesBetween(x, y, Mission.player.x, Mission.player.y);
					if (isConnecting)
						useFuel();
				} //if calling
				else //not calling
					direction = Degrees.GetDegreesBetween(x, y, sx, sy);

			}
		}

		protected override void performDeaths()
		{
			if (hit())
			{
				moveSound.Stop();
				base.performDeaths();
			}
		}

		public void call()
		{
			showInList = true;
			isCalling = true;
			z = Mission.player.z;
			direction = Degrees.GetDegreesBetween(x, y, Mission.player.x, Mission.player.y);
			int minutes = (int)(Degrees.getDistanceBetween(x, y,
Mission.player.x, Mission.player.y)
/
speed //this gives total hours
*
60.0); //convert to minutes

			if (minutes <= 1)
				playMessage(DSound.SoundPath + "\\rf0.wav");
			else if (minutes >= 2 && minutes <= 9)
				playMessage(DSound.SoundPath + String.Format("\\rf{0}.wav", minutes));
			else
				SelfVoice.NLS("#" + minutes + "&mius.wav", true, true); //playMessage(DSound.SoundPath + "\\rf10.wav");
		}

		public void uncall()
		{
			showInList = false;
			direction = Degrees.GetDegreesBetween(x, y, sx, sy);
			isCalling = false;
			isConnecting = false;
			time = 0;
			neutralizeSpeed(1500.0);
			if (message != null && DSound.isPlaying(message))
				message.Stop();
		}

		public void connectManeuver()
		{
			playMessage(DSound.SoundPath + "\\rf14.wav", true);
			isConnecting = true;
			direction = Degrees.getDegreeValue((short)(Mission.player.direction + 180));
			neutralizeSpeed(500.0);
			time = Environment.TickCount;
		}

		public void turnAround()
		{
			direction = Degrees.getDegreeValue((short)(direction + 180));
		}

		protected override void useFuel()
		{
			if ((Environment.TickCount - time) / 1000 / 60 >= 5)
			{
				playMessage(DSound.SoundPath + "\\rf13.wav");
				((Aircraft)Mission.player).requestRefuel();
			}
		}

		private void playMessage(String o, bool wait)
		{
			if (message != null)
				message.Stop();
			message = DSound.LoadSound(o);
			DSound.PlaySound(message, true, false);
			if (wait)
			{
				Interaction.stopAndMute(true);
				while (DSound.isPlaying(message))
					Thread.Sleep(100);
				Interaction.resumeAndUnmute();
			} //if wait
		}

		private void playMessage(String o)
		{
			playMessage(o, false);
		}

		public void connect()
		{
			Interaction.stopAndMute(true);
			playMessage(DSound.SoundPath + "\\rf12.wav");
			((Aircraft)Mission.player).rearm();
			((Aircraft)Mission.player).restoreDamage(
				(int)(Mission.player.maxDamagePoints * 0.30));
			((Aircraft)Mission.player).restartEngine(false);
			while (DSound.isPlaying(message))
				Thread.Sleep(500);
			uncall();
			((Aircraft)Mission.player).requestRefuel();
			Mission.refuelCount++;
			Mission.writeToFile();
			Interaction.resumeAndUnmute();
		}

		public override void save(BinaryWriter w)
		{
			base.save(w);
			w.Write(isConnecting);
			w.Write(isCalling);
			w.Write(time);
			w.Write(sx);
			w.Write(sy);
		}

		public override bool load()
		{
			if (!base.load())
				return false;
			BinaryReader r = Common.inFile;
			isConnecting = r.ReadBoolean();
			isCalling = r.ReadBoolean();
			time = r.ReadInt64();
			sx = r.ReadDouble();
			sy = r.ReadDouble();
			return true;
		}

		public void setHoverPosition(double sx, double sy)
		{
			this.sx = sx;
			this.sy = sy;
			x = sx - 10.0;
			y = sy - 10.0;
		}

		public void setJuliusHoverPosition()
		{
			setHoverPosition(
								Mission.player.x - 10.0, Mission.player.y - 10.0);
		}

		public override void freeResources()
		{
			base.freeResources();
			DSound.unloadSound(ref moveSound);
		}

	}
}
