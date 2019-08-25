/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;

namespace TDV
{
	public class InstructionNode
	{
		private int m_id;
		private bool m_changeNoseAngle;
		private bool m_ChangeCourse;
		private bool m_target;
		private float m_x;
		private float m_y;
		private short m_noseAngle;
		private short m_course;

		public bool changeNoseAngle
		{
			get { return (m_changeNoseAngle); }
			set { m_changeNoseAngle = value; }
		}
		public bool target
		{
			get { return (m_target); }
			set { m_target = value; }
		}
		public bool changeCourse
		{
			get { return (m_ChangeCourse); }
			set { m_ChangeCourse = value; }
		}
		public short course
		{
			get { return (m_course); }
			set { m_course = value; }
		}
		public float x
		{
			get { return (m_x); }
			set { m_x = value; }
		}
		public float y
		{
			get { return (m_y); }
			set { m_y = value; }
		}
		public short noseAngle
		{
			get { return (m_noseAngle); }
			set { m_noseAngle = value; }
		}
		public short direction
		{
			get { return (m_course); }
			set { m_course = value; }
		}
		public int id
		{
			get { return (m_id); }
		}
		public InstructionNode(int i)
		{
			m_id = i;
		}


	}
}
