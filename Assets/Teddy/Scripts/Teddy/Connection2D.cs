using UnityEngine;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using mattatz.Triangulation2DSystem;

namespace mattatz.TeddySystem {

	public class Neighbor2D {
		public Face2D face;
		public Segment2D joint;
		public Neighbor2D (Face2D f, Segment2D s) {
			face = f;
			joint = s;
		}
	}

	public class Connection2D {

		Dictionary<Segment2D, List<Face2D>> connections;

		public Connection2D(List<Face2D> faces) {
			connections = new Dictionary<Segment2D, List<Face2D>>();
			faces.ForEach(face => {
				Add(face);
			});
		}

		public void Add (Face2D f) {
			CheckAndAdd(f.Triangle.s0, f);
			CheckAndAdd(f.Triangle.s1, f);
			CheckAndAdd(f.Triangle.s2, f);
		}

		void CheckAndAdd(Segment2D s, Face2D f) {
			if(!connections.ContainsKey(s)) connections.Add(s, new List<Face2D>());
			connections[s].Add(f);
		}

		public void Remove(Face2D f) {
			connections[f.Triangle.s0].Remove(f);
			connections[f.Triangle.s1].Remove(f);
			connections[f.Triangle.s2].Remove(f);
		}

		public List<Neighbor2D> GetNeighbors (Face2D f) {
			var neighbors0 = connections[f.Triangle.s0].FindAll(f2 => f2 != f).Select(f2 => new Neighbor2D(f2, f.Triangle.s0));
			var neighbors1 = connections[f.Triangle.s1].FindAll(f2 => f2 != f).Select(f2 => new Neighbor2D(f2, f.Triangle.s1));
			var neighbors2 = connections[f.Triangle.s2].FindAll(f2 => f2 != f).Select(f2 => new Neighbor2D(f2, f.Triangle.s2));

			var neighbors = new List<Neighbor2D>();
			neighbors.AddRange(neighbors0);
			neighbors.AddRange(neighbors1);
			neighbors.AddRange(neighbors2);
			return neighbors;
		}

	}

}

