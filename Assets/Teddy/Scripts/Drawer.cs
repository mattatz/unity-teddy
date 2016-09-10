using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using mattatz.Utils;
using mattatz.Triangulation2DSystem;

namespace mattatz.TeddySystem.Example {

	public enum OperationMode {
		Default,
		Draw,
		Move
	};

	public class Drawer : MonoBehaviour {

		[SerializeField, Range(0.2f, 1.5f)] float threshold = 1.0f;
		[SerializeField] GameObject prefab;
		[SerializeField] GameObject floor;
		[SerializeField] Material lineMat;
		[SerializeField] TextAsset json;

		OperationMode mode;

		Teddy teddy;
		List<Vector2> points;
		List<Puppet> puppets = new List<Puppet>();

		Camera cam;
		float screenZ = 0f;

		Puppet selected;
		Vector3 origin;
		Vector3 startPoint;

		void Start () {
			cam = Camera.main;
			screenZ = Mathf.Abs(cam.transform.position.z - transform.position.z);

			points = new List<Vector2>();
			points = JsonUtility.FromJson<JsonSerialization<Vector2>>(json.text).ToList();
			Build();
		}

		void Update () {
			var bottom = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, screenZ));
			floor.transform.position = bottom;

			var screen = Input.mousePosition;
			screen.z = screenZ;

			switch(mode) {

			case OperationMode.Default:

				if(Input.GetMouseButtonDown(0)) {
					Clear();

					var ray = cam.ScreenPointToRay(screen);
					RaycastHit hit;
					if(Physics.Raycast(ray.origin, ray.direction, out hit, float.MaxValue)) {
						startPoint = cam.ScreenToWorldPoint(screen);;

						selected = hit.collider.GetComponent<Puppet>();
						selected.Select();
						startPoint = hit.point;
						origin = selected.transform.position;

						mode = OperationMode.Move;
					} else {
						mode = OperationMode.Draw;
					}
				}

				break;

			case OperationMode.Draw:
				if(Input.GetMouseButtonUp(0)) {
					Build();
					mode = OperationMode.Default;
				} else {
					var p = cam.ScreenToWorldPoint(screen);
					var p2D = new Vector2(p.x, p.y);
					if(points.Count <= 0 || Vector2.Distance(p2D, points.Last()) > threshold) {
						points.Add(p2D);
					}
				}
				break;

			case OperationMode.Move:

				if(Input.GetMouseButtonUp(0)) {
					selected.Unselect();
					selected = null;

					mode = OperationMode.Default;
				} else {
					var currentPoint = cam.ScreenToWorldPoint(screen);
					var offset = currentPoint - startPoint;
					selected.transform.position = origin + offset;
				}

				break;

			}

		}

		void Build () {
			if(points.Count < 3) return;

			points = Utils2D.Constrain(points, threshold);
			if(points.Count < 3) return;

			teddy = new Teddy(points);
			var mesh = teddy.Build(MeshSmoothingMethod.HC, 2, 0.2f, 0.75f);
			var go = Instantiate(prefab);
			go.transform.parent = transform;

			var puppet = go.GetComponent<Puppet>();
			puppet.SetMesh(mesh);
			puppets.Add(puppet);
		}

		void Clear () {
			points.Clear();
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

		}

	}

}

