/*
 * @author mattatz / http://mattatz.github.io

 * https://www.researchgate.net/publication/220507688_Improved_Laplacian_Smoothing_of_Noisy_Surface_Meshes
 * http://graphics.stanford.edu/courses/cs468-12-spring/LectureSlides/06_smoothing.pdf
 * http://wiki.unity3d.com/index.php?title=MeshSmoother
 */

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

namespace mattatz.MeshSmoothingSystem {

	public class MeshSmoothing {

		public static Mesh LaplacianFilter (Mesh mesh, int times = 1) {
			mesh.vertices = LaplacianFilter(mesh.vertices, mesh.triangles, times);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		public static Vector3[] LaplacianFilter(Vector3[] vertices, int[] triangles, int times) {
			var network = VertexConnection.BuildNetwork(triangles);
			for(int i = 0; i < times; i++) {
				vertices = LaplacianFilter(network, vertices, triangles);
			}
			return vertices;
		}

		static Vector3[] LaplacianFilter(Dictionary<int, VertexConnection> network, Vector3[] origin, int[] triangles) {
			Vector3[] vertices = new Vector3[origin.Length];
			for(int i = 0, n = origin.Length; i < n; i++) {
				if(network.ContainsKey(i)) {
					var connection = network[i].Connection;
					var v = Vector3.zero;
					foreach(int adj in connection) {
						v += origin[adj];
					}
					vertices[i] = v / connection.Count;
				} else {
					vertices[i] = origin[i];
				}
			}
			return vertices;
		}

		/*
		 * HC (Humphrey’s Classes) Smooth Algorithm - Reduces Shrinkage of Laplacian Smoother
		 * alpha 0.0 ~ 1.0
		 * beta  0.0 ~ 1.0
		*/
		public static Mesh HCFilter (Mesh mesh, int times = 5, float alpha = 0.5f, float beta = 0.75f) {
			mesh.vertices = HCFilter(mesh.vertices, mesh.triangles, times, alpha, beta);
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		static Vector3[] HCFilter(Vector3[] vertices, int[] triangles, int times, float alpha, float beta) {
			alpha = Mathf.Clamp01(alpha);
			beta = Mathf.Clamp01(beta);

			var network = VertexConnection.BuildNetwork(triangles);

			Vector3[] origin = new Vector3[vertices.Length];
			Array.Copy(vertices, origin, vertices.Length);
			for(int i = 0; i < times; i++) {
				vertices = HCFilter(network, origin, vertices, triangles, alpha, beta);
			}
			return vertices;
		}
			
		public static Vector3[] HCFilter(Dictionary<int, VertexConnection> network, Vector3[] o, Vector3[] q, int[] triangles, float alpha, float beta) {
			Vector3[] p = LaplacianFilter(network, q, triangles);
			Vector3[] b = new Vector3[o.Length];

			for(int i = 0; i < p.Length; i++) {
				b[i] = p[i] - (alpha * o[i] + (1f - alpha) * q[i]);
			}

			for(int i = 0; i < p.Length; i++) {
				if(network.ContainsKey(i)) {
					var adjacents = network[i].Connection;
					var bs = Vector3.zero;
					foreach(int adj in adjacents) {
						bs += b[adj];
					}
					p[i] = p[i] - (beta * b[i] + (1 - beta) / adjacents.Count * bs);
				} else {
					p[i] = b[i];
				}
			}

			return p;
		}

	}

}

