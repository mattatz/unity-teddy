using UnityEngine;
using Random = UnityEngine.Random;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace mattatz.TeddySystem.Example {

	[RequireComponent (typeof(Rigidbody)) ]
	[RequireComponent (typeof(MeshFilter)) ]
	[RequireComponent (typeof(MeshCollider)) ]
	public class Puppet : MonoBehaviour {

		Rigidbody body {
			get {
				if(_body == null) {
					_body = GetComponent<Rigidbody>();
				}
				return _body;
			}
		}

		MeshFilter filter {
			get {
				if(_filter == null) {
					_filter = GetComponent<MeshFilter>();
				}
				return _filter;
			}
		}

		MeshCollider col {
			get {
				if(_collider == null) {
					_collider = GetComponent<MeshCollider>();
				}
				return _collider;
			}
		}

		Rigidbody _body;
		MeshFilter _filter;
		MeshCollider _collider;

		void Start () {}
		void Update () {}

		public void Ignore () {
			col.enabled = false;
		}

		public void SetMesh (Mesh mesh) {
			body.mass = mesh.bounds.size.magnitude;
			filter.sharedMesh = mesh;
			if(mesh.vertices.Length > 250) {
				var oVertices = mesh.vertices.ToList();
				var oTriangles = mesh.triangles.ToList();
				int count = oTriangles.Count / 3;

				var vertices = new List<Vector3>();
				var triangles = new List<int>();

				for(int i = 0; i < 85; i++) {
					int idx = Random.Range(0, count) * 3;
					int a = oTriangles[idx], b = oTriangles[idx + 1], c = oTriangles[idx + 2];
					int vCount = vertices.Count;
					vertices.Add(oVertices[a]); vertices.Add(oVertices[b]); vertices.Add(oVertices[c]);
					triangles.Add(vCount); triangles.Add(vCount + 1); triangles.Add(vCount + 2);

					oTriangles.RemoveAt(idx + 2);
					oTriangles.RemoveAt(idx + 1);
					oTriangles.RemoveAt(idx);
					count -= 3;
				}	

				var colliderMesh = new Mesh();
				colliderMesh.vertices = vertices.ToArray();
				colliderMesh.SetTriangles(triangles.ToArray(), 0);
				col.sharedMesh = colliderMesh;
			} else {
				col.sharedMesh = mesh;
			}
		}

	}

}

