using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using mattatz.Triangulation2DSystem;

namespace mattatz.TeddySystem {

	public class Chord2D {
		public Face2D Face { get { return face; } }

		public Vertex2D Src { get { return src; } }
		public Vertex2D Dst { get { return dst; } }
		public Segment2D SrcEdge { get { return srcEdge; } }
		public Segment2D DstEdge { get { return dstEdge; } }

		public List<Chord2D> Connection { get { return connection; } }
		public bool Pruned { get { return pruned; } }

		Face2D face;
		Vertex2D src, dst;
		Segment2D srcEdge, dstEdge;

		List<Chord2D> connection;

		bool pruned;

		public Chord2D (Vertex2D src, Vertex2D dst, Face2D face) {
			this.src = src;
			this.dst = dst;
			this.face = face;
			this.connection = new List<Chord2D>();
		}

		public void Connect (Chord2D c) {
			this.connection.Add(c);
			c.connection.Add(this);
		}

		public void Disconnect (Chord2D c) {
			this.connection.Remove(c);
			c.connection.Remove(this);
		}

		public void DisconnectAll () {
			for(int i = 0, n = this.connection.Count; i < n; i++) {
				var c = this.connection[i];
				c.connection.Remove(this);
			}
			this.connection.Clear();
		}

		/*
		public void SetSrc (Vertex2D v) {
			src = v;
		}

		public void SetDst (Vertex2D v) {
			dst = v;
		}
		*/

		public void SetSrcEdge (Segment2D s) {
			srcEdge = s;
		}

		public void SetDstEdge (Segment2D s) {
			dstEdge = s;
		}

		public void Prune () {
			pruned = true;
		}

	}

}

