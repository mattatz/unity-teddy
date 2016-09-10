using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using mattatz.Utils;
using mattatz.Triangulation2DSystem;
using mattatz.MeshSmoothingSystem;

namespace mattatz.TeddySystem.Example {

	public class Demo : MonoBehaviour {

		[SerializeField] string fileName = "duck";
		[SerializeField] bool debug = false;
		[SerializeField] Material lineMat = null;

		[SerializeField] int smoothingTimes = 5;
		[SerializeField, Range(0f, 1f)] float smoothingAlpha = 0.25f, smoothingBeta = 0.5f;

		Teddy teddy;
		List<Segment2D> contour;

		void Start () {
			if(debug) {
				var points = LocalStorage.LoadList<Vector2>(fileName + ".json");
				teddy = new Teddy(points);
				contour = BuildContourSegments(teddy.triangulation);
				GetComponent<MeshFilter>().sharedMesh = teddy.Build(MeshSmoothingMethod.HC, smoothingTimes, smoothingAlpha, smoothingBeta);
			}
		}

		List<Segment2D> BuildContourSegments (Triangulation2D triangulation) {
			var triangles = teddy.triangulation.Triangles;

			var contour = new List<Segment2D>();

			var table = new Dictionary<Segment2D, HashSet<Triangle2D>>();
			for(int i = 0, n = triangles.Length; i < n; i++) {
				var t = triangles[i];
				if(!table.ContainsKey(t.s0)) table.Add(t.s0, new HashSet<Triangle2D>());
				if(!table.ContainsKey(t.s1)) table.Add(t.s1, new HashSet<Triangle2D>());
				if(!table.ContainsKey(t.s2)) table.Add(t.s2, new HashSet<Triangle2D>());

				table[t.s0].Add(t);
				table[t.s1].Add(t);
				table[t.s2].Add(t);
			}

			contour = table.Keys.ToList().FindAll(s => {
				return table[s].Count == 1;
			}).ToList();

			return contour;
		}

		void OnDrawGizmos () {
			if(contour != null) {
				Gizmos.color = Color.yellow;
				contour.ForEach(s => {
					Gizmos.DrawLine(s.a.Coordinate, s.b.Coordinate);
				});
			}
		}

		void OnRenderObject () {
			if(teddy == null) return;

			if(teddy.triangulation != null) {
				lineMat.SetColor("_Color", Color.black);
				DrawTriangles(teddy.triangulation.Triangles);
			}
		}

		void DrawTriangles (Triangle2D[] triangles) {
			GL.PushMatrix();
			GL.MultMatrix (transform.localToWorldMatrix);

			lineMat.SetPass(0);

			GL.Begin(GL.LINES);

			for(int i = 0, n = triangles.Length; i < n; i++) {
				var t = triangles[i];
				GL.Vertex(t.s0.a.Coordinate); GL.Vertex(t.s0.b.Coordinate);
				GL.Vertex(t.s1.a.Coordinate); GL.Vertex(t.s1.b.Coordinate);
				GL.Vertex(t.s2.a.Coordinate); GL.Vertex(t.s2.b.Coordinate);
			}

			GL.End();
			GL.PopMatrix();
		}

	}

}

