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

namespace TDV
{
	public delegate void ExtraItemFunction();
	public struct ExtraItem
	{
		private Aircraft.Action m_a;
		private ExtraItemFunction m_f;
		public Aircraft.Action action
		{
			get { return m_a; }
		}

		public ExtraItemFunction extraItemProc
		{
			get { return m_f; }
		}

		/// <summary>
		/// Defines a key-binding for menus.
		/// </summary>
		/// <param name="a">The Aircraft.Action to associate to this binding.</param>
		/// <param name="f">A reference to the function that will handle the action. This parameter should be an ExtraItemFunction delegate, and will be called by the menu to execute the encapsulated method.</param>
		public ExtraItem(Aircraft.Action a, ExtraItemFunction f)
		{
			m_a = a;
			m_f = f;
		}
	}
}
