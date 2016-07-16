using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using mattatz.Triangulation2DSystem;

namespace mattatz.TeddySystem {

	public enum Face2DType {
		Junction, Sleeve, Terminal
	};

	public class Face2D {
		public Face2DType Type { get { return type; } }
		public Triangle2D Triangle { get { return triangle; } }

		public bool Pruned { get { return divides != null; } }
		public List<Triangle2D> Divides { get { return divides; } }

		Face2DType type;
		Triangle2D triangle;
		List<Triangle2D> divides;

		public Face2D (Face2DType tp, Triangle2D tri) {
			type = tp;
			triangle = tri;
		}

		public Vector2 Centroid () {
			return (triangle.a.Coordinate + triangle.b.Coordinate + triangle.c.Coordinate) / 3f;
		}

		public Vertex2D GetUncommonPoint (Face2D face) {
			var t0 = triangle;
			var t1 = face.Triangle;
			if(!t1.HasPoint(t0.a)) {
				return t0.a;
			} else if(!t1.HasPoint(t0.b)) {
				return t0.b;
			}
			return t0.c;
		}

		public Vertex2D GetUncommonPoint (Segment2D s) {
			var t = triangle;
			if(!s.HasPoint(t.a.Coordinate)) return t.a;
			else if(!s.HasPoint(t.b.Coordinate)) return t.b;
			return t.c;
		}

		public Segment2D[] GetUncommonSegments (Face2D face) {
			var t0 = triangle;
			var t1 = face.Triangle;
			if(t1.HasSegment(t0.s0)) {
				return new Segment2D[] { t0.s1, t0.s2 };
			} else if(t1.HasSegment(t0.s1)) {
				return new Segment2D[] { t0.s0, t0.s2 };
			}
			return new Segment2D[] { t0.s0, t0.s1 };
		}

		public void SetDivides (List<Triangle2D> divides) {
			this.divides = divides;
		}

	}

}

