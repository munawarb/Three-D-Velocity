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
using SharpDX;
using System.IO;
using BPCSharedComponent.VectorCalculation;

namespace TDV
{
	public class MapNode : IComparable
	{
		public Vector2 position;
		public MapNode left, front, right, back;
		private static MapNode entryPoint;
		private static int edgeLength;
		public static int maxX, maxY;
		private static List<MapNode> AllNodes;
		private static List<List<MapNode>> paths;


		public MapNode(Vector2 position)
		{
			this.position = position;
			left = front = right = back = null;
		}

		/// <summary>
		/// Builds a map.
		/// </summary>
		/// <param name="furnishing">The list of furniture.</param>
		/// <param name="maxX">The max X boundary not adjusted for walls.</param>
		/// <param name="maxY">The max Y boundary not adjusted for walls.</param>
		/// <returns>The node that will be considered the start node.</returns>
		public static MapNode buildMap(List<Furniture> furnishing, int maxX, int maxY)
		{
			AllNodes = new List<MapNode>();
			MapNode.maxX = maxX;
			MapNode.maxY = maxY;
			//StreamWriter outFile = File.CreateText("Debug.log");
			MapNode startNode = null;
			MapNode lastX = null;
			MapNode lastY = null;
			edgeLength = 4;
			MapNode n = null;
			int currentX = 2, currentY = 2;
			while (currentX < maxX)
			{
				if (currentX == 2 || currentX % edgeLength == 0 || currentX == maxX - 1)
				{
					//outFile.WriteLine("Execuing x " + currentX);
					MapNode xn = new MapNode(new Vector2(currentX, 2));
					if (lastX != null)
					{
						lastX.right = xn;
						xn.left = lastX;
					}
					AllNodes.Add(xn);
					lastX = xn;
					lastY = lastX;
					if (currentX == 2 && currentY == 2)
						startNode = xn;
				}
				else
				{
					currentX++;
					continue;
				}

				currentY = 2;
				n = null;
				while (currentY < maxY)
				{
					if (currentX == 2 && currentY == 2)
					{
						currentY++;
						continue;
					}
					if (isPotentiallyBlockedInFront(furnishing, currentY, maxX))
					{
						n = new MapNode(new Vector2(currentX, currentY));
						//outFile.WriteLine("Y potential block in front " + currentY);
					}
					else if (currentY > 2 && isPotentiallyBlockedBehind(furnishing, currentY, maxX))
					{
						n = new MapNode(new Vector2(currentX, currentY));
						//outFile.WriteLine("Y potential block behind " + currentY);
					}
					else if (!isBlocked(furnishing, currentX, currentY) && currentY % edgeLength == 0)
					{
						n = new MapNode(new Vector2(currentX, currentY));
						//outFile.WriteLine("Node length reached at y " + currentY);
					}
					//Next, we need to connect with the nodes around us.
					if (n != null)
					{
						//outFile.WriteLine("Y " + currentY + " created.");
						lastY.front = n;
						//outFile.WriteLine(String.Format(" {0}.front={1}", lastY, n));
						n.back = lastY;
						//outFile.WriteLine(String.Format(" {0}.back={1}", n, lastY));
						if (lastY.left != null && lastY.left.front != null)
						{
							lastY.left.front.right = n;
							//outFile.WriteLine(String.Format("{0}.right={1}", lastY.left.front, n));
							n.left = lastY.left.front;
							//outFile.WriteLine(String.Format("{0}.left={1}", n, lastY.left.front));
						}
						lastY = n;
						AllNodes.Add(n);
					}
					if (isBlocked(furnishing, currentX, currentY))
					{
						//outFile.WriteLine("Block at " + currentY  + " resolving...");
						while (currentY < maxY)
						{
							if (!isBlocked(furnishing, currentX, ++currentY))
							{
								//outFile.WriteLine("Success, y = " + currentY);
								n = new MapNode(new Vector2(currentX, currentY));
								MapNode addTo = getNodeToLeft(currentX, currentY, startNode);
								if (addTo != null)
								{
									addTo.right = n;
									n.left = addTo;
									//outFile.WriteLine(String.Format("{0}.right={1}", addTo, n));
									//outFile.WriteLine(String.Format("{0}.left={1}", n, addTo));
								}
								else
									//outFile.WriteLine("Node to left is null!");
									addTo = getNodeToRight(currentX, currentY, startNode);
								if (addTo != null)
								{
									addTo.left = n;
									n.right = addTo;
									//outFile.WriteLine(String.Format("{0}.left={1}", addTo, n));
									//outFile.WriteLine(String.Format("{0}.right={1}", n, addTo));

								}
								else
									//outFile.WriteLine("Node to right is null!");
									lastY = n;
								AllNodes.Add(n);
								break;
							}
						} //while
					} //if blocked
					currentY++;
					n = null;
				} //y
				currentX++;
			} //x
			//outFile.Close();
			return entryPoint = startNode;
		}


		/// <summary>
		/// Determines if the supplied x and y coordinates are blocked by an object.
		/// </summary>
		/// <param name="furnishing">The list of furnishing.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <returns>True if blocked, false otherwise.</returns>
		private static bool isBlocked(List<Furniture> furnishing, int x, int y)
		{
			foreach (Furniture f in furnishing)
			{
				if (f.isBlocked(x, y))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Determines if along the line y = y, there exists a block such that if y = y-1 some (x, y) would be blocked by furniture.
		/// </summary>
		/// <param name="furnishing">The list of furniture.</param>
		/// <param name="y">The y value to check. It will be held constant to this value along the search.</param>
		/// <param name="maxX">The max x boundary of the map.</param>
		/// <returns>True if there is a block, false otherwise.</returns>
		private static bool isPotentiallyBlockedBehind(List<Furniture> furnishing, int y, int maxX)
		{
			Ray r;
			float output;
			for (int x = 2; x < maxX; x++)
			{
				r = new Ray(new Vector3(x, y, 0), new Vector3(0, -1, 0));
				foreach (Furniture f in furnishing)
				{
					if (f.isBlocked(r, out output) && output == 1.0)
						return true;
				}
			}
			return false;
		}


		/// <summary>
		/// Determines if along the line y = y, there exists a block such that if y = y+1 some (x, y) would be blocked by furniture.
		/// </summary>
		/// <param name="furnishing">The list of furniture.</param>
		/// <param name="y">The y value to check. It will be held constant to this value along the search.</param>
		/// <param name="maxX">The max x boundary of the map.</param>
		/// <returns>True if there is a block, false otherwise.</returns>
		private static bool isPotentiallyBlockedInFront(List<Furniture> furnishing, int y, int maxX)
		{
			Ray r;
			float output;
			for (int x = 2; x < maxX; x++)
			{
				r = new Ray(new Vector3(x, y, 0), new Vector3(0, 1, 0));
				foreach (Furniture f in furnishing)
				{
					if (f.isBlocked(r, out output) && output == 1.0)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets all nodes with a y value as indicated.
		/// </summary>
		/// <param name="y">The y value to search for.</param>
		/// <param name="output">The list into which all nodes will be inserted.</param>
		/// <param name="current">The start node.</param>
		/// <param name="visited">A list of visited nodes, used for infrastructure and should be given null on first execution.</param>
		private static void getAllNodesY(int y, List<MapNode> output, MapNode current, List<MapNode> visited)
		{
			if (current == null || (visited != null && visited.Contains(current)))
				return;
			if (visited == null)
				visited = new List<MapNode>();
			visited.Add(current);
			if (current.position.Y == y)
				output.Add(current);
			getAllNodesY(y, output, current.front, visited);
			getAllNodesY(y, output, current.right, visited);
			getAllNodesY(y, output, current.back, visited);
			getAllNodesY(y, output, current.left, visited);
		}

		/// <summary>
		/// Gets the node to the left of these coordinates.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="start">The start node.</param>
		/// <returns>The node to the left of this location if one exists, null otherwise.</returns>
		private static MapNode getNodeToLeft(int x, int y, MapNode start)
		{
			List<MapNode> l = new List<MapNode>();
			getAllNodesY(y, l, start, null);
			if (l.Count == 0)
				return null;
			//Next, if all nodes are less than this x,
			//IE: all are to the left, return the nearest one.
			if (l[l.Count - 1].position.X < x)
				return l[l.Count - 1];
			for (int i = 0; i < l.Count; i++)
			{
				if (l[i].position.X >= x)
				{
					if (i > 0)
						return l[i - 1];
					else
						return null;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the node to the right of these coordinates.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="start">The start node.</param>
		/// <returns>The node to the right of this location if one exists, null otherwise.</returns>
		private static MapNode getNodeToRight(int x, int y, MapNode start)
		{
			List<MapNode> l = new List<MapNode>();
			getAllNodesY(y, l, start, null);
			if (l.Count == 0)
				return null;
			if (l[0].position.X > x)
				return l[0];
			for (int i = l.Count - 1; i >= 0; i--)
			{
				if (l[i].position.X <= x)
				{
					if (i < l.Count - 1)
						return l[i + 1];
					else
						return null;
				}
			}
			return null;
		}



		public int CompareTo(object obj)
		{
			MapNode m = (MapNode)obj;
			return (int)(position.X - m.position.X);
		}

		public static void printNodes(MapNode start, int maxY)
		{
			StringBuilder s = new StringBuilder();
			List<MapNode> l = new List<MapNode>();
			for (int y = 2; y < maxY; y++)
			{
				l.Clear();
				s.Append(y);
				getAllNodesY(y, l, start, null);
				l.Sort();
				if (l.Count > 0)
					s.Append(",");
				for (int i = 0; i < l.Count; i++)
				{
					s.Append(l[i].position.X);
					if (i < l.Count - 1)
						s.Append(",");
				} //for i
				s.AppendLine();
			} //for y
			StreamWriter f = File.CreateText("Map.csv");
			f.Write(s.ToString());
			f.Close();
		}

		public List<MapNode> getAllNodes()
		{
			List<MapNode> l = new List<MapNode>();
			if (front != null)
				l.Add(front);
			if (right != null)
				l.Add(right);
			if (back != null)
				l.Add(back);
			if (left != null)
				l.Add(left);
			if (l.Count == 0)
				return null;
			return l;
		}

		/// <summary>
		/// Finds the shortest path from the start to the end position.
		/// </summary>
		/// <param name="beginPosition">The (x, y) pair that represents the start location.</param>
		/// <param name="endPosition">The (x, y) pair that represents the end location.</param>
		/// <returns>A list of mapNodes representing the shortest path from start to end.</returns>
		public static List<MapNode> getShortestPathTo(Vector2 beginPosition, Vector2 endPosition)
		{
			MapNode start = getNearestNodeTo(beginPosition.X, beginPosition.Y, entryPoint), goal = getNearestNodeTo(endPosition.X, endPosition.Y, entryPoint);
			List<AStarElement> pQueue = new List<AStarElement>(); //priority queue
			pQueue.Add(new AStarElement(start, goal));
			AStarElement toAdd = null;
			while (!pQueue[0].isFullPath())
			{
				//The shortest path found so far will always be at the head of the list,
				//so per aStar, build on that path.
				AStarElement best = pQueue[0];
				foreach (MapNode n in best.getLastNode().getAllNodes())
				{
					if (best.exists(n))
					{
						continue;
					}
					toAdd = new AStarElement(best);
					toAdd.addNode(n);
					pQueue.Add(toAdd);
					if (n == goal)
						break;
				}
				if (!best.isFullPath())
					pQueue.Remove(best);
				pQueue.Sort(); //Custom sorting that makes shortest path be sorted to head of list.
			} //while
			return pQueue[0].getNodes();
		}

		/// <summary>
		/// Gets the nearest node to the given set of coordinates.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="start">The start node.</param>
		/// <returns>The closest node to the (x, y) pair.</returns>
		private static MapNode getNearestNodeTo(float x, float y, MapNode start)
		{
			if (x % edgeLength == 0 && y % edgeLength == 0)
			{
				foreach (MapNode node in AllNodes)
				{
					if (node.position.X == x && node.position.Y == y)
						return node;
				} //foreach
			}
			float minDistance = Degrees.getDistanceBetween(x, y, start.position.X, start.position.Y);
			MapNode minNode = start;
			float tempDistance = 0f;
			foreach (MapNode node in AllNodes)
			{
				tempDistance = Degrees.getDistanceBetween(x, y, node.position.X, node.position.Y);
				if (tempDistance < minDistance)
				{
					minDistance = tempDistance;
					minNode = node;
				}
			}
			return minNode;
		}

		public static void printPath(List<MapNode> path)
		{
			StreamWriter file = File.CreateText("path.txt");
			foreach (MapNode n in path)
				file.WriteLine(String.Format("({0}, {1})", n.position.X, n.position.Y));
			file.Close();
		}

		public override string ToString()
		{
			return String.Format("({0}, {1})", position.X, position.Y);
		}

		/// <summary>
		/// Writes paths from node to every other node to a file.
		/// </summary>
		/// <param name="fileName">The path to the file to store the paths into.</param>
		public static void writeAStarToFile(String fileName)
		{
			List<List<MapNode>> paths = new List<List<MapNode>>();
			int totalIterations = maxX * maxX * maxY * maxY;
			int iteration = 1;
			for (int i = 0; i <= maxX; i++)
			{
				for (int j = 0; j <= maxY; j++)
				{
					for (int k = 0; k <= maxX; k++)
					{
						for (int l = 0; l <= maxY; l++)
						{
							if (i == k && j == l)
								continue;
							MapNode start = getNearestNodeTo(i, j, entryPoint), end = getNearestNodeTo(k, l, entryPoint);
							foreach (List<MapNode> tempPath in paths)
							{
								if ((start.Equals(tempPath[0]) && end.Equals(tempPath[tempPath.Count - 1])) || (start.Equals(tempPath[tempPath.Count - 1]) && end.Equals(tempPath[0])))
									continue;
							}
							System.Diagnostics.Trace.WriteLine(String.Format("Writing ({0}, {1}), ({2}, {3})", i, j, k, l));
							System.Diagnostics.Trace.WriteLine(String.Format("{0}%", (double)(iteration / totalIterations)));
							iteration++;
							List<MapNode> path = getShortestPathTo(new Vector2(i, j), new Vector2(k, l));
							paths.Add(path);
						}
					}
				}
			}
			BinaryWriter outFile = new BinaryWriter(new FileStream(fileName, FileMode.Create));
			outFile.Write(paths.Count);
			foreach (List<MapNode> path in paths)
			{
				outFile.Write(path.Count);
				foreach (MapNode node in path)
				{
					outFile.Write((int)node.position.X);
					outFile.Write((int)node.position.Y);
				}
			}
			outFile.Close();
		}


		/// <summary>
		/// Loads paths from a file.
		/// </summary>
		/// <param name="fileName">The file to load from.</param>
		public static void loadAStarFromFile(String fileName)
		{
			paths = new List<List<MapNode>>();
			BinaryReader inFile = new BinaryReader(new FileStream(fileName, FileMode.Open));
			int totalPaths = inFile.ReadInt32();
			int numberOfNodes = 0;
			for (int i = 0; i < totalPaths; i++)
			{
				numberOfNodes = inFile.ReadInt32();
				List<MapNode> path = new List<MapNode>();
				for (int j = 0; j < numberOfNodes; j++)
					path.Add(getNearestNodeTo(inFile.ReadInt32(), inFile.ReadInt32(), entryPoint));
				paths.Add(path);
			}
			inFile.Close();
		}

		public override bool Equals(object obj)
		{
			MapNode o = (MapNode)obj;
			return position.X == o.position.X && position.Y == o.position.Y;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary>
		/// Gets the path in the cache.
		/// </summary>
		/// <param name="beginPosition">The (x, y) pair describing the start position.</param>
		/// <param name="endPosition">The (x, y) pair describing the goal.</param>
		/// <returns>The path to get from start to end.</returns>
		public static List<MapNode> getCachedPathTo(Vector2 beginPosition, Vector2 endPosition)
		{
			MapNode start = getNearestNodeTo(beginPosition.X, beginPosition.Y, entryPoint), goal = getNearestNodeTo(endPosition.X, endPosition.Y, entryPoint);
			foreach (List<MapNode> path in paths)
			{
				if (path[0].Equals(start) && path[path.Count - 1].Equals(goal))
					return path;
				if (path[0].Equals(goal) && path[path.Count - 1].Equals(start))
				{
					path.Reverse();
					return path;
				}
			}
			return null;
		}

	}
}