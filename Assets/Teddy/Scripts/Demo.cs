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
		[SerializeField] bool debugSrc = false, debugDst = false;
		[SerializeField] bool debugTerminal = false, debugSleeve = false, debugJunction = false;
		[SerializeField] Material lineMat = null, chordMat = null;
		[SerializeField] bool debugPruned = true;
		[SerializeField] int depth = 0;
		[SerializeField] int networkIndex = 0;

		[SerializeField] int smoothingTimes = 5;
		[SerializeField, Range(0f, 1f)] float smoothingAlpha = 0.25f, smoothingBeta = 0.5f;

		Teddy teddy;

		void Start () {
			if(debug) {
				var points = LocalStorage.LoadList<Vector2>(fileName + ".json");
				teddy = new Teddy(points);
				GetComponent<MeshFilter>().sharedMesh = teddy.Build(smoothingTimes, smoothingAlpha, smoothingBeta);
			}
		}

		void OnDrawGizmos () {
			if(teddy == null) return;

			#if UNITY_EDITOR
			if(teddy.debugVertices != null) {
				for(int i = 0, n = teddy.debugVertices.Count; i < n; i++) {
					UnityEditor.Handles.Label(teddy.debugVertices[i].Coordinate, i.ToString());
				}
			}
			#endif

			if(teddy.triangulation != null) {
				// debug external
				/*
				var triangles = triangulation.Triangles;
				Gizmos.color = Color.green;
				for(int i = 0, n = triangles.Length; i < n; i++) {
					var t = triangles[i];
					if(ExternalPoint(t.a)) Gizmos.DrawSphere(t.a.Coordinate, 0.2f);
					if(ExternalPoint(t.b)) Gizmos.DrawSphere(t.b.Coordinate, 0.2f);
					if(ExternalPoint(t.c)) Gizmos.DrawSphere(t.c.Coordinate, 0.2f);
				}
				*/
			}

			if(teddy.chord != null) {
				// DrawChordPointsGizmos(chord);
			}

			if(teddy.networks != null) {
				/*
				var nw = networks[Mathf.Abs(networkIndex) % networks.Count];
				if(nw.Contour) {
					Gizmos.color = Color.red;
				} else {
					Gizmos.color = Color.blue;
				}
				Gizmos.DrawSphere(nw.Vertex.Coordinate, 0.2f);
				Gizmos.color = Color.white;
				var from = nw.Vertex.Coordinate;
				foreach(VertexNetwork2D vn in nw.Connection) {
					Gizmos.DrawLine(from, vn.Vertex.Coordinate);
				}
				*/
			}

			if(teddy.network != null) {
				var mesh = GetComponent<MeshFilter>().sharedMesh;
				var keys = teddy.network.Keys.ToList();
				var key = keys[Mathf.Abs(networkIndex) % keys.Count];
				var vn = teddy.network[key];
				var from = mesh.vertices[key];
				Gizmos.color = Color.white;
				foreach(int adj in vn.Connection) {
					var to = mesh.vertices[adj];
					Gizmos.DrawLine(from, to);
				}
			}

		}

		void OnRenderObject () {
			if(teddy == null) return;

			if(teddy.triangulation != null) {
				lineMat.SetColor("_Color", Color.black);
				DrawTriangles(teddy.triangulation.Triangles);
			}

			if(teddy.chord != null) {
				DrawChord2D(teddy.chord);
			}

			if(debugTerminal) {
				lineMat.SetColor("_Color", Color.green);
				DrawTriangles(teddy.faces.FindAll(f => f.Type == Face2DType.Terminal).Select(f => f.Triangle).ToArray());
			}

			if(debugSleeve) {
				lineMat.SetColor("_Color", Color.yellow);
				DrawTriangles(teddy.faces.FindAll(f => f.Type == Face2DType.Sleeve).Select(f => f.Triangle).ToArray());
			}

			if(debugJunction) {
				lineMat.SetColor("_Color", Color.red);
				DrawTriangles(teddy.faces.FindAll(f => f.Type == Face2DType.Junction).Select(f => f.Triangle).ToArray());
			}

		}

		void DrawChordPointsGizmos (Chord2D chord, Chord2D from = null, int d = 0) {
			if(chord.Pruned) {
				Gizmos.color = Color.green;
			} else {
				Gizmos.color = Color.yellow;
			}
			if(debugSrc) Gizmos.DrawSphere(chord.Src.Coordinate, 0.1f);
			if(debugDst) Gizmos.DrawSphere(chord.Dst.Coordinate, 0.1f);

			var connection = chord.Connection;
			connection.ForEach(to => {
				if(to != from) {
					DrawChordPointsGizmos(to, chord, d + 1);
				}
			});
		}

		void DrawChord2D (Chord2D chord) {
			GL.PushMatrix();
			GL.MultMatrix (transform.localToWorldMatrix);

			chordMat.SetPass(0);

			GL.Begin(GL.LINES);

			DrawChord2DRoutine(chord, null);

			GL.End();
			GL.PopMatrix();
		}

		void DrawChord2DRoutine (Chord2D chord, Chord2D from, int d = 0) {
			if(d > depth) return;

			if(debugPruned || !chord.Pruned) {
				GL.TexCoord2(0f, 0f); GL.Vertex(chord.Src.Coordinate); 
				GL.TexCoord2(0f, 1f); GL.Vertex(chord.Dst.Coordinate);
			}

			var connection = chord.Connection;
			connection.ForEach(to => {
				if(to != from) {
					DrawChord2DRoutine(to, chord, d + 1);
				}
			});
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

