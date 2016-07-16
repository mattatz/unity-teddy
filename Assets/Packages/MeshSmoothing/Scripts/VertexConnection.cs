using UnityEngine;

using System.Collections;
using System.Collections.Generic;

namespace mattatz.MeshSmoothingSystem {

	public class VertexConnection {

		public HashSet<int> Connection { get { return connection; } }

		HashSet<int> connection;

		public VertexConnection() {
			this.connection = new HashSet<int>();
		}

		public void Connect (int to) {
			connection.Add(to);
		}

		public static Dictionary<int, VertexConnection> BuildNetwork (int[] triangles) {
			var table = new Dictionary<int, VertexConnection>();

			for(int i = 0, n = triangles.Length; i < n; i += 3) {
				int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
				if(!table.ContainsKey(a)) {
					table.Add(a, new VertexConnection());
				}
				if(!table.ContainsKey(b)) {
					table.Add(b, new VertexConnection());
				}
				if(!table.ContainsKey(c)) {
					table.Add(c, new VertexConnection());
				}
				table[a].Connect(b); table[a].Connect(c);
				table[b].Connect(a); table[b].Connect(c);
				table[c].Connect(a); table[c].Connect(b);
			}

			return table;
		}

	}

}

