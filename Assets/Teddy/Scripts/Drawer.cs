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

namespace mattatz.TeddySystem.Example {

	public class Drawer : MonoBehaviour {

		[SerializeField, Range(0.2f, 1.5f)] float threshold = 1.0f;
		[SerializeField] GameObject prefab;
		[SerializeField] GameObject floor;
		[SerializeField] Material lineMat;
		[SerializeField] bool debugTriangles = false;

		[SerializeField] Material chordMat;
		[SerializeField] bool debugChord = false;
		[SerializeField] bool debugPruned = true;
		[SerializeField] int debugPruneDepth = 0;

		[SerializeField] bool debugTerminal = false, debugSleeve = false, debugJunction = false;
		[SerializeField] int networkIndex = 0;

		Teddy teddy;
		List<Vector2> points;
		List<Puppet> puppets = new List<Puppet>();

		Camera cam;
		float screenZ = 0f;
		bool dragging;

		void Start () {
			cam = Camera.main;
			screenZ = Mathf.Abs(cam.transform.position.z - transform.position.z);

			points = new List<Vector2>();
			points = LocalStorage.LoadList<Vector2>("duck.json");
			Build();
		}

		void Update () {

			var bottom = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, screenZ));
			floor.transform.position = bottom;

			if(Input.GetMouseButtonDown(0)) {
				dragging = true;
				Clear();
			} else if(Input.GetMouseButtonUp(0)) {
				dragging = false;
				Build();
			}

			if(dragging) {
				var screen = Input.mousePosition;
				screen.z = screenZ;
				var p = cam.ScreenToWorldPoint(screen);
				var p2D = new Vector2(p.x, p.y);
				if(points.Count <= 0 || Vector2.Distance(p2D, points.Last()) > threshold) {
					points.Add(p2D);
				}
			}
		}

		void Build () {
			if(points.Count < 3) return;

			points = Utils2D.Constrain(points, threshold);
			if(points.Count < 3) return;

			teddy = new Teddy(points);
			var mesh = teddy.Build(2, 0.1f, 0.75f);
			// GetComponent<MeshFilter>().sharedMesh = mesh;
			var go = Instantiate(prefab);
			go.transform.parent = transform;

			var puppet = go.GetComponent<Puppet>();
			puppet.SetMesh(mesh);
			puppets.Add(puppet);
		}

		void Clear () {
			points.Clear();
			GetComponent<MeshFilter>().sharedMesh = null;
		}

		public void Save () {
			LocalStorage.SaveList<Vector2>(points, "points.json");
		}

		public void Reset () {
			puppets.ForEach(puppet => {
				puppet.Ignore();
				Destroy(puppet.gameObject, 10f);
			});
			puppets.Clear();
		}

		void OnDrawGizmos () {

			if(points != null) {
				Gizmos.color = Color.white;
				points.ForEach(p => {
					Gizmos.DrawSphere(p, 0.02f);
				});
			}

			if(teddy == null) return;

			#if UNITY_EDITOR
			if(teddy.debugVertices != null) {
				for(int i = 0, n = teddy.debugVertices.Count; i < n; i++) {
					UnityEditor.Handles.Label(teddy.debugVertices[i].Coordinate, i.ToString());
				}
			}
			#endif

			if(teddy.networks != null) {
				var networks = teddy.networks;
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
			}

			/*
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
			*/

		}

		void OnRenderObject () {

			if(points != null) {
				GL.PushMatrix();
				GL.MultMatrix (transform.localToWorldMatrix);
				lineMat.SetColor("_Color", Color.white);
				lineMat.SetPass(0);
				GL.Begin(GL.LINES);
				for(int i = 0, n = points.Count - 1; i < n; i++) {
					GL.Vertex(points[i]); GL.Vertex(points[i + 1]);
				}
				GL.End();
				GL.PopMatrix();
			}

			if(teddy == null) return;

			if(teddy.triangulation != null && debugTriangles) {
				lineMat.SetColor("_Color", Color.black);
				DrawTriangles(teddy.triangulation.Triangles);
			}

			if(teddy.chord != null && debugChord) {
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
			if(d > debugPruneDepth) return;

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

