/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Threading;
using System.Collections;
namespace TDV
{
	public class WeaponsHolder : Holder
	{
		public WeaponsHolder()
			: base()
		{

		}

		protected override void activate()
		{
			try {
				running = true;
				while (true) {
					modifiedProjectorList = false;
					startMarker = Environment.TickCount;
					hault(); //will block here if asked to do so,
					//and then move on when released.
					lock (projectors) {
						foreach (Object o in projectors) {
							((Weapons)o).use();
							if (modifiedProjectorList)
								break;
						}
					} //lock
					if (projectors.Count == 0 && Options.abortGame) {
						done = true;
						System.Diagnostics.Trace.WriteLine("Holder ended: " + (this is WeaponsHolder));
						return;
					}
					if (Interaction.isGameFinished()) {
						//let weapons clean up
						lock (lockObject) {
							Weapons.requestedClear = true;
							foreach (Object o in projectors)
								((Weapons)o).use();
							Weapons.requestedClear = false;
						}
						done = true;
						System.Diagnostics.Trace.WriteLine("Holder ended: " + (this is WeaponsHolder));
						return;
					}
					long executionTime = Environment.TickCount - startMarker;
					if (executionTime < Common.intervalMS)
						Thread.Sleep((int)(Common.intervalMS - executionTime));
				} //while
			}
			catch (Exception e) {
				Common.handleError(e);
			} //catch
		}

	}
}
