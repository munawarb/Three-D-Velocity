/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.Collections;
using System.IO;

namespace TDV
{
	public class Instructions
	{
		private InstructionNode[] m_nodes;
		private static Int32 m_maxMustKill;
		private static Int32 m_currentKills;
		private int m_index;

		public Instructions()
		{
			m_nodes = new InstructionNode[0];
		}

		private int index
		{
			get { return (m_index); }
			set { m_index = value; }
		}
		public InstructionNode nodeAt(int index)
		{
			return (m_nodes[index]);
		}

		////Returns the InstructionNode created by this method
		private InstructionNode addNode()
		{
			ArrayList temp = new ArrayList();
			int i = 0;
			if (m_nodes.Length > 0)
			{
				for (i = 0; i <= m_nodes.Length - 1; i++)
				{
					temp.Add(nodeAt(i));
				}
			}
			if ((temp.Count >= 1))
			{
				temp.Add(new InstructionNode(temp.Count - 1));
			}
			else
			{
				temp.Add(new InstructionNode(0));
			}

			////Now, convert this array back in to the nodes array
			InstructionNode[] final = new InstructionNode[temp.Count];
			for (i = 0; i <= final.Length - 1; i++)
			{
				final[i] = (InstructionNode)temp[i];
			}
			m_nodes = final;
			return ((InstructionNode)temp[temp.Count - 1]);
		}
		public int maxNodePosition
		{
			get { return (m_nodes.Length - 1); }
		}
		public bool isEnd()
		{
			return (index >= maxNodePosition);
		}
		public bool moveNext(bool wrapToBeginning)
		{
			if (!isEnd())
			{
				index += 1;
				return (true);
			}
			else
			{
				////if isEnd()
				if (wrapToBeginning)
				{
					index = 0;
					return (true);
				}
				else
				{
					////if !wrap
					return (false);
					////can't move, so return false
				}
			}
		}

		////sets WrapToBeginning to true--default
		public bool moveNext()
		{
			return (moveNext(true));
		}

		////returns the node that is currently pointed to by index
		public InstructionNode current()
		{
			return (nodeAt(index));
		}

		////maxMustKills represents how many objects must be destroyed for a mission success.
		////currentKills represents how many objects were destroyed.
		public static Int32 getMaxMustKill()
		{
			return (m_maxMustKill);
		}
		public static void incrementMaxMustKill()
		{
			m_maxMustKill = System.Threading.Interlocked.Increment(ref m_maxMustKill);
		}
		public static Int32 getCurrentKills()
		{
			return (m_currentKills);
		}
		public static void incrementCurrentKills()
		{
			m_currentKills = System.Threading.Interlocked.Increment(ref m_currentKills);
		}

		////Method adds node expecting all parameters to be set. See overload below
		public void addNode(bool changeNoseAngle, bool changeCourse, bool target, Int16 direction, Int16 noseAngle, float x, float y)
		{
			InstructionNode n = addNode();
			n.changeNoseAngle = changeNoseAngle;
			n.changeCourse = changeCourse;
			n.course = direction;
			n.noseAngle = noseAngle;
			n.target = target;
			n.x = x;
			n.y = y;
		}
		public void addNode(bool target, float x, float y, InstructionNode n)
		{
			if (n == null)
			{
				n = addNode();
			}
			n.target = target;
			n.x = x;
			n.y = y;
		}

		////Passes node as NULL reference
		public void addNode(bool target, float x, float y)
		{
			addNode(target, x, y, null);
		}

		public void save(BinaryWriter w)
		{
			w.Write(index);
			w.Write(m_nodes.Length);
			for (int i = 0; i < m_nodes.Length; i++)
			{
				w.Write(m_nodes[i].changeNoseAngle);
				w.Write(m_nodes[i].changeCourse);
				w.Write(m_nodes[i].course);
				w.Write(m_nodes[i].noseAngle);
				w.Write(m_nodes[i].target);
				w.Write(m_nodes[i].x);
				w.Write(m_nodes[i].y);
			} //for
		}

		public void load()
		{
			BinaryReader r = Common.inFile;
			index = r.ReadInt32();
			int nodes = r.ReadInt32();
			for (int i = 0; i < nodes; i++)
			{
				InstructionNode n = addNode();
				n.changeNoseAngle = r.ReadBoolean();
				n.changeCourse = r.ReadBoolean();
				n.course = r.ReadInt16();
				n.noseAngle = r.ReadInt16();
				n.target = r.ReadBoolean();
				n.x = r.ReadSingle();
				n.y = r.ReadSingle();
			}
		}

	}
}
