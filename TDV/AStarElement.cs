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
using BPCSharedComponent.VectorCalculation;

namespace TDV
{
	class AStarElement : IComparable
	{
		private List<MapNode> chain;
		private MapNode target;

		private int g()
		{
			if (chain.Count == 1)
				return 0;
			else
			{
				int totalG = 0;
				for (int i = 0; i < chain.Count - 1; i++)
					totalG += (int)Degrees.getDistanceBetween(chain[i].position.X, chain[i].position.Y, chain[i + 1].position.X, chain[i + 1].position.Y);
				return totalG;
			}
		}

		private int h()
		{
			return (int)Degrees.getDistanceBetween(getLastNode().position.X, getLastNode().position.Y, target.position.X, target.position.Y);
		}

		public int f
		{
			get { return g() + h(); }
		}

		/// <summary>
		/// Creates a new path.
		/// </summary>
		/// <param name="start">The start node.</param>
		/// <param name="target">The ultimate goal node.</param>
		public AStarElement(MapNode start, MapNode target)
		{
			chain = new List<MapNode>();
			chain.Add(start);
			this.target = target;
		}

		/// <summary>
		/// Creates a new path using an existing AStar path.
		/// </summary>
		/// <param name="obj">The AStarElement from which to build this element.</param>
		public AStarElement(AStarElement obj)
		{
			chain = new List<MapNode>();
			foreach (MapNode n in obj.chain)
				chain.Add(n);
			target = obj.target;
		}

		public void addNode(MapNode node)
		{
			chain.Add(node);
		}


		public int CompareTo(object obj)
		{
			AStarElement o = (AStarElement)obj;
			if (f == o.f)
				return h() - o.h();
			return f - o.f;
		}

		public MapNode getLastNode()
		{
			return chain[chain.Count - 1];
		}

		public bool isFullPath()
		{
			return chain.Contains(target);
		}

		/// <summary>
		/// Gets all nodes contained in this AStarElement
		/// </summary>
		/// <returns>The list of nodes.</returns>
		public List<MapNode> getNodes()
		{
			List<MapNode> nodes = new List<MapNode>();
			foreach (MapNode n in chain)
				nodes.Add(n);
			return nodes;
		}

		public override string ToString()
		{
			StringBuilder res = new StringBuilder();
			res.Append("Path: " + Environment.NewLine);
			foreach (MapNode n in chain)
			{
				res.Append(n.ToString());
				res.AppendLine();
			}
			res.AppendLine();
			return res.ToString();
		}

		/// <summary>
		/// Determines if the given node is already a part of this path.
		/// </summary>
		/// <param name="n">The node to check.</param>
		/// <returns>True if the node is already part of this path, false otherwise.</returns>
		public bool exists(MapNode n)
		{
			return chain.Contains(n);
		}

	}
}
