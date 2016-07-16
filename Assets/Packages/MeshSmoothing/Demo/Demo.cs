using UnityEngine;
using System.Collections;

namespace mattatz.MeshSmoothingSystem.Demo {

	[RequireComponent (typeof(MeshRenderer))]
	[RequireComponent (typeof(MeshFilter))]
	public class Demo : MonoBehaviour {

		[System.Serializable] 
		enum FilterType {
			Laplacian, HC
		};

		MeshFilter filter {
			get {
				if(_filter == null) {
					_filter = GetComponent<MeshFilter>();
				}
				return _filter;
			}
		}

		MeshFilter _filter;

		[SerializeField, Range(0f, 1f)] float intensity = 0.5f;
		[SerializeField] FilterType type;
		[SerializeField, Range(0, 20)] int times = 3;
		[SerializeField, Range(0f, 1f)] float hcAlpha = 0.5f;
		[SerializeField, Range(0f, 1f)] float hcBeta = 0.5f;

		void Start () {
			var mesh = filter.mesh;
			filter.mesh = ApplyNormalNoise(mesh);

			switch(type) {
			case FilterType.Laplacian:
				filter.mesh = MeshSmoothing.LaplacianFilter(filter.mesh, times);
				break;
			case FilterType.HC:
				filter.mesh = MeshSmoothing.HCFilter(filter.mesh, times, hcAlpha, hcBeta);
				break;
			}
		}
		
		// void Update () {}

		Mesh ApplyNormalNoise (Mesh mesh) {

			var vertices = mesh.vertices;
			var normals = mesh.normals;
			for(int i = 0, n = mesh.vertexCount; i < n; i++) {
				vertices[i] = vertices[i] + normals[i] * Random.value * intensity;
			}
			mesh.vertices = vertices;

			return mesh;
		}

	}

}

