using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using mattatz.Triangulation2DSystem;

namespace mattatz.TeddySystem {

	public class VertexNetwork2D {

		public Vertex2D Vertex { get { return vertex; } }
		public float Height { get { return height; } }
		public bool Contour { get { return contour; } }
		public HashSet<VertexNetwork2D> Connection { get { return connection; } }
		public bool Elevated { get { return elevated; } }

		Vertex2D vertex;
		float height;

		HashSet<VertexNetwork2D> connection;
		bool contour;
		bool elevated;

		public VertexNetwork2D (Vertex2D vertex, bool contour) {
			this.vertex = vertex;
			this.height = 0f;
			this.contour = this.elevated = contour;
			this.connection = new HashSet<VertexNetwork2D>();
		} 

		public void Connect (VertexNetwork2D to) {
			this.connection.Add(to);
		}

		public bool Elevate () {
			if(elevated) return true;

			foreach(VertexNetwork2D vn in connection) {
				elevated = elevated || vn.Elevated;
			}

			if(elevated) {
				height = 0f;
				int count = 0;
				foreach(VertexNetwork2D vn in connection) {
					if(vn.Contour) {
						height += Vector2.Distance(vn.Vertex.Coordinate, Vertex.Coordinate);
						count++;
					} else if(vn.elevated) {
						height += vn.Height;
						count++;
					}
				}
				height /= count;
			}

			return elevated;
		}

	}

}

