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

namespace TDVServer
{
	class BotInfo: IComparable
	{
		public byte[] data
		{
			get;
			set;
		}

		public String creator
		{
			get;
			set;
		}

		public Game.ObjectType objectType
		{
			get;
			set;
		}

		public String name
		{
			get;
			set;
		}

		public String id
		{
			get;
			set;
		}

		public BotInfo(String creator, String id, String name, Game.ObjectType objectType)
		{
			this.creator = creator;
			this.id = id;
			this.name = name;
			this.objectType = objectType;
		}

		public int CompareTo(object obj)
		{
			return id.CompareTo(((BotInfo)obj).id);
		}
	}
}
