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
	public class AddOnArgs
	{
		private int addOn;
		private int arg;
		public AddOnArgs(int addOn, int arg)
		{
			this.addOn = addOn;
			this.arg = arg;
		}
		public CSCommon.AddOns getAddOn()
		{
			return (CSCommon.AddOns)addOn;
		}
		public int getArg()
		{
			return arg;
		}
	}

	public class GameInfoArgs
	{
		private int t;
		private String i;
		private String d;
		public Options.Modes getGameType()
		{
			return (Options.Modes)t;
		}
		public String getId()
		{
			return i;
		}
		public String getDescription()
		{
			return d;
		}
		public GameInfoArgs(String id, String description, int mode)
		{
			i = id;
			d = description;
			t = mode;
		}
	}

	public class ViewAddOnArgs
	{
		private int id;
		private string desc;
		private bool f1, f2, f3, f4;

		public ViewAddOnArgs(int id, String desc)
		{
			this.id = id;
			this.desc = desc;
		}

		public ViewAddOnArgs(int id, String desc, bool f1, bool f2, bool f3, bool f4)
			: this(id, desc)
		{
			this.f1 = f1;
			this.f2 = f2;
			this.f3 = f3;
			this.f4 = f4;
		}

		public int getId()
		{
			return id;
		}
		public String getDescription()
		{
			return desc;
		}
		public bool isEnabledOrDisabledType()
		{
			return f1;
		}
		public bool showEnableText()
		{
			return f2;
		}
		public bool showDecrement()
		{
			return f3;
		}
		public bool showIncrement()
		{
			return f4;
		}
	}

	public class ChatRoomArgs
	{
		public String friendlyName
		{
			get;
			set;
		}
		public String id
		{
			get;
			set;
		}
		public bool passworded
		{
			get;
			set;
		}

		public ChatRoomArgs(String id, String friendlyName, bool passworded)
		{
			this.id = id;
			this.friendlyName = friendlyName;
			this.passworded = passworded;
		}
	}

	public struct ChatRoomMember : IComparable
	{
		public String id
		{
			get;
			set;
		}

		public String name
		{
			get;
			set;
		}

		public ChatRoomMember(String i, String n)
			: this()
		{
			id = i;
			name = n;
		}

		public override string ToString()
		{
			return name;
		}

		public override bool Equals(Object obj)
		{
			return Equals((ChatRoomMember)obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public bool Equals(ChatRoomMember other)
		{
			return id.Equals(other.id);
		}

		public int CompareTo(object obj)
		{
			return id.CompareTo(((ChatRoomMember)obj).id);
		}

		public int CompareTo(ChatRoomMember obj)
		{
			return id.CompareTo(obj.id);
		}
	}

}