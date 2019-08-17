/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using System.Threading;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.Input;

namespace TDV
{
	public class AircraftCarrier : MissionObjectBase
	{
		private OggBuffer message;
		private ExtendedAudioBuffer moveSound;
		private bool landedBefore;
		private bool playerLanding;
		private bool playerLanded;
		private bool announced34Mark;
		private bool announced12Mark;
		private bool announced14Mark;
		private bool firstLoad;
		private Range r34;
		private Range r12;
		private Range r14;
		public AircraftCarrier(double x, double y)
			: base("ac", null)
		{
			showInList = false;
			firstLoad = true;
			this.x = x;
			this.y = y;
			setSpan(300.0, 2.0);
			neutralizeSpeed(50.0);
			weapon.weaponIndex = WeaponTypes.battleShipGuns;
			moveSound = loadSound(soundPath + "e7.wav");
			setDamagePoints(5000);
			explodeString = soundPath + "d3.wav";
			r34 = new Range(4.75, Weapons.maxVRange);
			r12 = new Range(3.50, Weapons.maxVRange);
			r14 = new Range(2.25, Weapons.maxVRange);
		}

		public AircraftCarrier()
			: this(0.0, 0.0)
		{ }

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
				moveSound.stop();
				isProjectorStopped = true;
				return;
			}
			if (!isRequestedTerminated)
			{
				base.move();
				playSound(moveSound, false, true);
				base.updateTotalDistance();
				if (playerLanding && Weapons.isValidLock(Mission.player))
				{
					announceMarkers();
					showInList = true;
				}
				if (playerLanded)
				{
					Mission.player.x = x;
					Mission.player.y = y;
					Mission.player.z = z;
				} //if player landed
				sendObjectUpdate();
			} //if !requested terminated
		}

		public void playerRequestLand()
		{
			playerLanding = true;
			showInList = true;
			playMessage(DSound.SoundPath + "\\ac1.ogg");
		}


		public void landPlayer()
		{
			playerLanded = true;
			Interaction.stopAndMute(true, true);
			moveSound.stop();
			Common.fadeMusic();
			playMessage(DSound.SoundPath + "\\ac" + ((landedBefore) ? "6" : "5") + ".ogg");
			landedBefore = true;
			long mark = Environment.TickCount;
			((Aircraft)Mission.player).rearm();
			((Aircraft)Mission.player).restoreDamage(0);
			while (message.isPlaying())
			{
				if ((Environment.TickCount - mark) / 1000 >= 3 && (DXInput.isKeyHeldDown() || DXInput.isJSButtonHeldDown()))
					break;
				Thread.Sleep(50);
			}
			message.stopOgg();
			playMessage(DSound.SoundPath + "\\ac7.ogg");
			while (message.isPlaying())
				Thread.Sleep(500);
			((Aircraft)Mission.player).catapult();
			Common.startMusic();
			((Aircraft)Mission.player).requestRefuel();
			playSound(moveSound, true, true);
			Interaction.resumeAndUnmute();
		}

		private void announceMarkers()
		{
			if (!announced34Mark)
			{
				if (Weapons.inRange(this, Mission.player, r34))
				{
					playMessage(DSound.SoundPath + "\\ac2.ogg");
					announced34Mark = true;
				}
			}
			if (!announced12Mark)
			{
				if (Weapons.inRange(this, Mission.player, r12))
				{
					playMessage(DSound.SoundPath + "\\ac3.ogg");
					announced12Mark = true;
				}
			}
			if (!announced14Mark)
			{
				if (Weapons.inRange(this, Mission.player, r14))
				{
					playMessage(DSound.SoundPath + "\\ac4.ogg", true);
					announced14Mark = true;
				}
			}

		}

		protected override void performDeaths()
		{
			if (hit())
			{
				sendFinalObjectUpdate();
				moveSound.stop();
				base.performDeaths();
			}
		}

		private void playMessage(String o, bool wait)
		{
			if (message != null)
				message.stopOgg();
			message = DSound.loadOgg(o);
			message.play();
			if (wait)
			{
				Interaction.stopAndMute(true);
				while (message.isPlaying())
					Thread.Sleep(100);
				message.stopOgg();
				Interaction.resumeAndUnmute();
			} //if wait
		}

		private void playMessage(String o)
		{
			playMessage(o, false);
		}

		public override void save(BinaryWriter w)
		{
			base.save(w);
			w.Write(playerLanded);
			w.Write(playerLanding);
			w.Write(announced34Mark);
			w.Write(announced14Mark);
			w.Write(announced12Mark);
		}

		public override bool load()
		{
			if (!base.load())
				return false;
			BinaryReader r = Common.inFile;
			playerLanded = r.ReadBoolean();
			playerLanding = r.ReadBoolean();
			announced34Mark = r.ReadBoolean();
			announced14Mark = r.ReadBoolean();
			announced12Mark = r.ReadBoolean();
			return true;
		}

		private void reset()
		{
			playerLanding = false;
			playerLanded = false;
			announced34Mark = false;
			announced12Mark = false;
			announced14Mark = false;
			showInList = false;
		}

		public void abort()
		{
			reset();
			if (message != null && message.isPlaying())
				message.stopOgg();
		}

		public override void freeResources()
		{
			base.freeResources();
			DSound.unloadSound(ref moveSound);
		}
	}
}
