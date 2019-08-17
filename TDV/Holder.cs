/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections;
using System.Threading;


namespace TDV
{
	public class Holder
	{
		protected Object lockObject;
		private bool added;
		protected long startMarker;
		private Thread thread;
		protected ArrayList projectors;
		protected bool done;
		protected int count;
		protected bool requestedHault;
		private bool haultBecausePaused;
		private bool m_haulted;
		protected bool modifiedProjectorList;
		protected bool running; //Used for Interaction.clearData and terminateProjectors.

		public bool haulted
		{
			get
			{
				return (m_haulted);
			}
			set
			{
				m_haulted = value;
			}
		}

		public Holder()
		{
			lockObject = new object();
			projectors = new ArrayList();
		}

		public virtual void add(Object p)
		{
			lock (lockObject)
			{
				modifiedProjectorList = true;
				projectors.Add(p);
				System.Diagnostics.Trace.WriteIf(p as Projector != null, "Added " + p.ToString() + ", running " + running);
				added = true;
			}
		}

		public void remove(Object p)
		{
			lock (projectors)
				projectors.Remove(p);
			Interaction.removeFromObjectTable(((Projector)p).id);				 //if object was prematurely terminated, .kill will not be called.
			modifiedProjectorList = true;
			/*This condition is here mainly for FFA games where
			 * if the last aircraft gets shot down and there is one remaining, the AI thread pool would have 0 projectors.
			 * This would cause this thread to terminate prematurely and when a new player enters the game, the last aircraft standing in the alst match would not
			 * see the object update.
			 * */
			if (Options.mode == Options.Modes.freeForAll && projectors.Count == 0)
				added = false; //Stop game from exiting if 0 opponents exist
		}

		/// <summary>
		/// Starts the running thread on this Holder if it isn't already running.
		/// </summary>
		public void startThread()
		{
			System.Diagnostics.Trace.WriteLine("Starting holder, running state is already " + running);
			if (running)
				return;
			done = false;
			thread = new Thread(activate);
			thread.Start();
		}

		protected virtual void activate()
		{
			try
			{
				running = true;
				while (true)
				{
					modifiedProjectorList = false;
					startMarker = Environment.TickCount;
					count = 0;
					hault(); //will block here if asked to do so,
					//and then move on when released.
					lock (lockObject)
					{
						foreach (Object o in projectors)
						{
							Projector p = (Projector)o;
							if (!p.isTerminated)
							{
								p.move();
							}
							else
							{
								p.freeResources();
								remove(p);
							}
							if (modifiedProjectorList) //IE: if object requested another object to be added to this holder
								break;
						} //foreach
					} //lock
					long executionTime = Environment.TickCount - startMarker;
					if (executionTime < Common.intervalMS)
						Thread.Sleep((int)(Common.intervalMS - executionTime));

					/* BUG FIX:
					 * We need to have the added check below since
					 * in an online game, holder[1] will have 0 projectors until a player connects to the FFA.
					 * This would cause the holder to terminate, so when the player connected and
					 * moved, the move would not register.
					 * */
					if (projectors.Count == 0 && (added
						|| Interaction.isFFAFinished()
					|| Options.abortGame
					|| Options.serverEndedGame
					|| Options.requestedShutdown))
					{
						running = false;
						done = true;
						System.Diagnostics.Trace.WriteLine("Holder ended: " + (this is WeaponsHolder));
						return;
					}
				} //while
			}
			catch (Exception e)
			{
				Common.handleError(e);
			} //catch
		}

		public bool stopped
		{
			get { return (done); }
		}

		public Thread getAssociatedThread()
		{
			return (thread);
		}

		public bool isEmpty()
		{
			return (projectors == null || projectors.Count == 0);
		}

		public bool changeHaultStatus()
		{
			lock (lockObject)
				return (requestedHault = !requestedHault);
		}
		public bool changeHaultStatus(bool status)
		{
			lock (lockObject)
			{
				requestedHault = status;
				return requestedHault;
			} //lock
		}

		//stops this holder from moving its projectors until this method
		//is called again
		//This method will also cause a hault if
		//the game is paused.
		//requestedHault and haultBecausePaused flags work independently of each other.
		//Therefore, a game could be unpaused but still frozen because requestedHault is set, and vice versa.
		protected void hault()
		{
			if (Options.isPaused)
				haultBecausePaused = true;
			if (requestedHault || haultBecausePaused)
			{
				while (requestedHault || haultBecausePaused)
				{
					System.Diagnostics.Trace.WriteLine("In pause state");
					if (Interaction.isGameFinished() && !Options.isLoading)
					{ //during load, gameFinished may be true because objects are cleared
						requestedHault = false; //don't hault again,
						//since objects need to clean up.
						haultBecausePaused = false;
						break;
					}
					if (haultBecausePaused)
					{
						if (!Options.isPaused)
							haultBecausePaused = false;
					} //if haulted because the game paused
					//Now we've done everything,
					//like mute if we have to, so signal a hault success.
					if (!haulted)
						haulted = true;
					Thread.Sleep(200);
				} //while requestedHault || haultBecausePaused
				haulted = false;
			} //if requested hault
		}

		/// <summary>
		/// Clears this holder, but does not stop it.
		/// </summary>
		public void clear()
		{
			lock (lockObject)
			{
				Projector p = null;
				Weapons w = null;
				foreach (Object o in projectors)
				{
					if ((p = o as Projector) != null)
					{
						p.requestingTerminate();
						p.freeResources();
						Interaction.removeFromObjectTable(p.id);
					}
					else if ((w = o as Weapons) != null)
					{
						Weapons.requestedClear = true;
						w.use();
						Weapons.requestedClear = false;
					}
				}
				projectors.Clear();
				modifiedProjectorList = true;
			}
			added = false;
		}

		/// <summary>
		/// Determines if this holder is ticking. Note that holders can be running and empty at the same time
		/// such as in an online game where a spectator is waiting for aircraft to join.
		/// </summary>
		/// <returns>True if the holder is ticking, false otherwise.</returns>
		public bool isRunning()
		{
			return running;
		}

		/// <summary>
		/// Clears the holder and stops the holder thread.
		/// </summary>
		public void stopNow()
		{
			bool added2 = added;
			clear();
			added = added2; //clear sets added to false, but we may not want that here.
			done = true;
		}

	}
}
