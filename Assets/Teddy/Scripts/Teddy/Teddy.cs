using UnityEngine;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using mattatz.Utils;
using mattatz.Triangulation2DSystem;
using mattatz.MeshSmoothingSystem;

namespace mattatz.TeddySystem {

    public enum MeshSmoothingMethod {
        None,
        Laplacian,
        HC
    }

    public class Teddy {

        public List<Segment2D> contourSegments;

        public Triangulation2D triangulation;
        public List<Face2D> faces;
        public Chord2D chord;
        public List<VertexNetwork2D> networks;
        Dictionary<Vertex2D, float> heightTable = new Dictionary<Vertex2D, float>();

        public Dictionary<int, VertexConnection> network;

        public Teddy(List<Vector2> points) {
            var polygon = Polygon2D.Contour(points.ToArray());
            triangulation = new Triangulation2D(polygon, 0f);
            Init(triangulation);
        }

        void Init(Triangulation2D triangulation) {
            contourSegments = BuildContourSegments(triangulation);

            var triangles = triangulation.Triangles.ToList();
            faces = Categorize(triangles);

            var terminal = faces.Find(f => f.Type == Face2DType.Terminal);
            chord = GetChordalAxis(terminal, faces);

            var terminalChords = Traverse(chord, null, (Chord2D c) => {
                return c.Face.Type == Face2DType.Terminal;
            });

            var convergence = new List<Vertex2D>();
            terminalChords.ForEach(c => {
                Prune(c, convergence);
            });

            Subdivide(chord);

            // var spine = GetSpinePoints(chord);
            var networkTable = BuildTable(triangulation);
            networks = BuildNetworks(triangulation, networkTable);
            Elevate(networks);

            heightTable = new Dictionary<Vertex2D, float>();
            foreach (Vertex2D v in networkTable.Keys) {
                heightTable.Add(v, networkTable[v].Height);
            }

            Sew(triangulation, chord, heightTable, 3);
        }

        public Mesh Build(MeshSmoothingMethod method, int times = 5, float alpha = 0.2f, float beta = 0.5f) {
            var mesh = triangulation.Build(
                (Vertex2D v) => {
                    float z = 0f;
                    if (heightTable.ContainsKey(v)) {
                        z = heightTable[v];
                    }
                    return new Vector3(v.Coordinate.x, v.Coordinate.y, -z);
                }
            );

            mesh = Symmetrize(mesh);

            switch (method) {
                case MeshSmoothingMethod.Laplacian:
                    mesh = MeshSmoothing.LaplacianFilter(mesh, times);
                    break;
                case MeshSmoothingMethod.HC:
                    mesh = MeshSmoothing.HCFilter(mesh, times, alpha, beta);
                    break;
            }

            network = VertexConnection.BuildNetwork(mesh.triangles);
            return mesh;
        }

        protected List<Face2D> Categorize(List<Triangle2D> triangles) {
            return triangles.Select(t => {
                return new Face2D(GetFaceType(t), t);
            }).ToList();
        }

        protected Face2DType GetFaceType(Triangle2D t) {
            int count = 0;
            count += ExternalSegment(t.s0) ? 1 : 0;
            count += ExternalSegment(t.s1) ? 1 : 0;
            count += ExternalSegment(t.s2) ? 1 : 0;

            Face2DType type;
            if (count == 0) {
                type = Face2DType.Junction;
            } else if (count == 1) {
                type = Face2DType.Sleeve;
            } else {
                type = Face2DType.Terminal;
            }
            return type;
        }

        protected bool ExternalSegment(Segment2D s) {
            return ContainsSegment(contourSegments, s);
        }

        protected bool ContainsSegment(List<Segment2D> segments, Segment2D s) {
            for (int i = 0, n = segments.Count; i < n; i++) {
                var s2 = segments[i];
                // if(s2.HasPoint(s.a.Coordinate) && s2.HasPoint(s.b.Coordinate)) return true;
                if (s2.On(s.a.Coordinate) && s2.On(s.b.Coordinate)) return true;
                // if(Distance(s2, s.a) <= epsilon && Distance(s2, s.b) <= epsilon) return true;
                // if(s2.Distance(s.a) <= epsilon && s2.Distance(s.b) <= epsilon) return true;
            }
            return false;
        }

        protected bool ExternalPoint(Vertex2D p) {
            // var segments = contour.Segments;
            return ContainsPoint(contourSegments, p);
        }

        const float epsilon = 0.0001f;
        protected bool ContainsPoint(List<Segment2D> segments, Vertex2D p) {
            for (int i = 0, n = segments.Count; i < n; i++) {
                var s = segments[i];
                if (s.HasPoint(p) || s.HasPoint(p) || s.On(p)) {
                    return true;
                }
            }
            return false;
        }

        // https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
        protected float Distance(Segment2D line, Vertex2D v) {
            Vector2 a = line.a.Coordinate, b = line.b.Coordinate, p = v.Coordinate;
            float dx = (b.x - a.x), dy = (b.y - a.y);
            return Mathf.Abs((dy * p.x) - (dx * p.y) + (b.x * a.y) - (b.y * a.x)) / Mathf.Sqrt(dy * dy + dx * dx);
        }

        /*
		 * The chordal axis is obtained by connecting the midpoints of the internal edges.
		 */
        protected Chord2D GetChordalAxis(Face2D external, List<Face2D> faces) {
            var t = external.Triangle;

            Vertex2D src, dst;
            Segment2D dstEdge;

            bool e0 = ExternalSegment(t.s0);
            bool e1 = ExternalSegment(t.s1);
            bool e2 = ExternalSegment(t.s2);
            if (e0 && e1) {
                src = t.s0.HasPoint(t.s1.a) ? t.s1.a : t.s1.b;
                dst = triangulation.CheckAndAddVertex(t.s2.Midpoint());
                dstEdge = t.s2;
            } else if (e1 && e2) {
                src = t.s1.HasPoint(t.s2.a) ? t.s2.a : t.s2.b;
                dst = triangulation.CheckAndAddVertex(t.s0.Midpoint());
                dstEdge = t.s0;
            } else {
                src = t.s2.HasPoint(t.s0.a) ? t.s0.a : t.s0.b;
                dst = triangulation.CheckAndAddVertex(t.s1.Midpoint());
                dstEdge = t.s1;
            }

            var chord = new Chord2D(src, dst, external);
            chord.SetDstEdge(dstEdge);

            var connection = new Connection2D(faces);
            ChordalAxisRoutine(chord, connection);

            return chord;
        }

        protected void ChordalAxisRoutine(Chord2D chord, Connection2D connection) {
            var origin = chord.Face;
            var neighbors = connection.GetNeighbors(origin);
            connection.Remove(origin); // prevent overlaping to traverse

            neighbors.ForEach(neighbor => {
                var face = neighbor.face;
                var segment = neighbor.joint;

                Vertex2D destination;
                Segment2D destinationEdge = null;

                switch (face.Type) {
                    case Face2DType.Junction:
                        destination = triangulation.CheckAndAddVertex(face.Centroid());
                        break;

                    case Face2DType.Sleeve:
                        var others = face.Triangle.ExcludeSegment(segment);
                        if (!ExternalSegment(others[0])) {
                            destination = triangulation.CheckAndAddVertex(others[0].Midpoint());
                            destinationEdge = others[0];
                        } else {
                            destination = triangulation.CheckAndAddVertex(others[1].Midpoint());
                            destinationEdge = others[1];
                        }
                        break;

                    // case Face2DType.Terminal:
                    default:
                        destination = face.GetUncommonPoint(origin);
                        break;
                }

                Chord2D nextChord;
                if (origin.Type == Face2DType.Junction) {
                    var interval = new Chord2D(chord.Dst, triangulation.CheckAndAddVertex(segment.Midpoint()), origin);
                    interval.SetSrcEdge(chord.DstEdge); // maybe null
                    interval.SetDstEdge(segment);

                    nextChord = new Chord2D(interval.Dst, destination, face);
                    nextChord.SetSrcEdge(segment);
                    nextChord.SetDstEdge(destinationEdge); // null or exist

                    chord.Connect(interval);
                    interval.Connect(nextChord);
                } else {
                    nextChord = new Chord2D(chord.Dst, destination, face);
                    nextChord.SetSrcEdge(chord.DstEdge);
                    nextChord.SetDstEdge(destinationEdge); // null or exist

                    chord.Connect(nextChord);
                }

                ChordalAxisRoutine(nextChord, connection);
            });

        }

        /*
		 * BUG
		 * 既にPrune済みのJunctionを再度Pruneしてしまうバグ
		 */
        protected void Prune(Chord2D chord, List<Vertex2D> convergence, Chord2D from = null, List<Chord2D> past = null) {

            if (chord.Face.Type == Face2DType.Junction) {
                past.Add(chord);

                var centroid = chord.Face.Centroid();

                var points = new List<Vertex2D>();
                var ignores = new List<Vertex2D>();

                for (int i = 0, n = past.Count - 1; i < n; i++) {
                    var c0 = past[i];
                    var c1 = past[i + 1];
                    points.Add(c0.Face.GetUncommonPoint(c1.Face));
                }

                var t = chord.Face.Triangle;
                Vertex2D dividePoint = null;

                if (!chord.Face.Pruned) {
                    points.Add(t.a); points.Add(t.b); points.Add(t.c);

                    var coord = chord.Src.Coordinate;
                    if (coord == (t.a.Coordinate + t.b.Coordinate) * 0.5f) {
                        dividePoint = t.c;
                    } else if (coord == (t.b.Coordinate + t.c.Coordinate) * 0.5f) {
                        dividePoint = t.a;
                    } else {
                        dividePoint = t.b;
                    }

                } else {
                    points.Add(t.a); points.Add(t.b); points.Add(t.c);

                    var divides = chord.Face.Divides;
                    Triangle2D t0 = divides[0], t1 = divides[1];
                    Vertex2D[] ps0 = t0.ExcludePoint(centroid), ps1 = t1.ExcludePoint(centroid);
                    if (chord.Dst.Coordinate == (ps0[0].Coordinate + ps0[1].Coordinate) * 0.5f) {
                        triangulation.RemoveTriangle(t0);
                        if (t.a != ps0[0] && t.a != ps0[1]) {
                            ignores.Add(t.a);
                        } else if (t.b != ps0[0] && t.b != ps0[1]) {
                            ignores.Add(t.b);
                        } else {
                            ignores.Add(t.c);
                        }
                    } else if (chord.Dst.Coordinate == (ps1[0].Coordinate + ps1[1].Coordinate) * 0.5f) {
                        triangulation.RemoveTriangle(t1);
                        if (t.a != ps1[0] && t.a != ps1[1]) {
                            ignores.Add(t.a);
                        } else if (t.b != ps1[0] && t.b != ps1[1]) {
                            ignores.Add(t.b);
                        } else {
                            ignores.Add(t.c);
                        }
                    } else {
                        // Debug.LogWarning("error!");
                        return;
                    }
                }

                var vertices = points.OrderBy(p => Angle(centroid, p.Coordinate)).ToList();
                var cv = triangulation.CheckAndAddVertex(centroid);

                var newTriangles = new List<Triangle2D>();
                for (int i = 0, n = vertices.Count; i < n; i++) {
                    Vertex2D a = cv, b = vertices[i], c = vertices[(i + 1) % n];
                    if (ignores.Contains(b) || ignores.Contains(c)) continue;
                    var nt = triangulation.AddTriangle(a, b, c);
                    newTriangles.Add(nt);
                }

                if (!chord.Face.Pruned && dividePoint != null) {
                    var divides = newTriangles.FindAll(nt => nt.HasPoint(dividePoint));
                    chord.Face.SetDivides(divides.ToList());
                }

                // finish current loop
                convergence.Add(cv);
                Prune(past);

                return;
            }

            Segment2D diameter;
            if (chord.Face.Type == Face2DType.Terminal) {
                var t = chord.Face.Triangle;
                if (!ExternalSegment(t.s0)) {
                    diameter = t.s0;
                } else if (!ExternalSegment(t.s1)) {
                    diameter = t.s1;
                } else {
                    diameter = t.s2;
                }
            } else {
                var segments = chord.Face.GetUncommonSegments(from.Face);
                if (!ExternalSegment(segments[0])) {
                    diameter = segments[0];
                } else {
                    diameter = segments[1];
                }
            }

            if (past == null) past = new List<Chord2D>();
            past.Add(chord);

            Vector2 center = diameter.Midpoint();
            float radius = Vector2.Distance(center, diameter.a.Coordinate);

            var found = past.Find(ch => {
                var t = ch.Face.Triangle;
                Vector2 a = t.a.Coordinate, b = t.b.Coordinate, c = t.c.Coordinate;
                return
                    (!diameter.HasPoint(a) && Vector2.Distance(a, center) - radius > float.Epsilon) ||
                    (!diameter.HasPoint(b) && Vector2.Distance(b, center) - radius > float.Epsilon) ||
                    (!diameter.HasPoint(c) && Vector2.Distance(c, center) - radius > float.Epsilon);
            });

            if (found != null) {
                var points = new List<Vertex2D>();

                for (int i = 0, n = past.Count - 1; i < n; i++) {
                    var c0 = past[i];
                    var c1 = past[i + 1];
                    points.Add(c0.Face.GetUncommonPoint(c1.Face));
                }
                points.Add(chord.Face.GetUncommonPoint(diameter));

                var basis = diameter.a.Coordinate;
                var vertices = points.OrderBy(p => Angle(center, basis, p.Coordinate)).ToList();
                vertices.Insert(0, diameter.a);
                vertices.Add(diameter.b);

                var cv = triangulation.CheckAndAddVertex(center);
                for (int i = 0, n = vertices.Count - 1; i < n; i++) {
                    Vertex2D a = cv, b = vertices[i], c = vertices[i + 1];
                    triangulation.AddTriangle(a, b, c);
                }

                convergence.Add(cv);
                Prune(past);
                return;
            }

            chord.Connection.ForEach(to => {
                if (to != from) {
                    Prune(to, convergence, chord, past.ToList());
                }
            });

        }

        protected void Prune(List<Chord2D> chords) {
            chords.ForEach(ch => {
                ch.Prune();
                triangulation.RemoveTriangle(ch.Face.Triangle);
            });
        }

        protected void SubdivideRoutine(List<Triangle2D> rTriangles, List<Segment2D> rSegments, Chord2D chord, Chord2D from)
        {
            switch (chord.Face.Type) {
                case Face2DType.Sleeve:

                    if (!chord.Pruned) {
                        Vertex2D top, lb, rb;
                        Segment2D s0 = chord.SrcEdge, s1 = chord.DstEdge;
                        if (s0.a == s1.a || s0.a == s1.b) {
                            top = s0.a;
                            lb = s0.b;
                        } else {
                            top = s0.b;
                            lb = s0.a;
                        }
                        rb = (s1.a == top) ? s1.b : s1.a;

                        Vertex2D tl = chord.Src, tr = chord.Dst, bottom = triangulation.CheckAndAddVertex((lb.Coordinate + rb.Coordinate) * 0.5f);

                        // triangulation.RemoveTriangle(chord.Face.Triangle);
                        rTriangles.Add(chord.Face.Triangle);
                        triangulation.AddTriangle(top, tl, tr);
                        triangulation.AddTriangle(tl, lb, bottom);
                        triangulation.AddTriangle(tr, bottom, rb);
                        triangulation.AddTriangle(tl, bottom, tr);
                    }

                    break;

                case Face2DType.Junction:

                    if (!chord.Pruned) {
                        Vertex2D top, lb, rb, bottom;
                        if (chord.SrcEdge != null) {
                            top = chord.Dst;
                            lb = chord.SrcEdge.a;
                            rb = chord.SrcEdge.b;
                            bottom = chord.Src;
                            // triangulation.RemoveTriangle(chord.SrcEdge);
                            rSegments.Add(chord.SrcEdge);
                        } else {
                            top = chord.Src;
                            lb = chord.DstEdge.a;
                            rb = chord.DstEdge.b;
                            bottom = chord.Dst;
                            // triangulation.RemoveTriangle(chord.DstEdge);
                            rSegments.Add(chord.DstEdge);
                        }
                        // triangulation.RemoveTriangle(chord.Face.Triangle);
                        triangulation.AddTriangle(top, lb, bottom);
                        triangulation.AddTriangle(top, rb, bottom);
                    }

                    break;
            }

            chord.Connection.ForEach(to => {
                if (to != from) {
                    SubdivideRoutine(rTriangles, rSegments, to, chord);
                }
            });
        }

        // triangles are divided at the spine
        protected void Subdivide(Chord2D root) {
            var rTriangles = new List<Triangle2D>();
            var rSegments = new List<Segment2D>();

            SubdivideRoutine(rTriangles, rSegments, root, null);
            rTriangles.ForEach(t => triangulation.RemoveTriangle(t));
            rSegments.ForEach(s => triangulation.RemoveTriangle(s));
        }

        protected void GetSpinePointsRoutine(List<Vertex2D> points, Chord2D current, Chord2D from)
        {
            if (!current.Pruned)
            {
                points.Add(current.Src);
            }
            current.Connection.ForEach(to =>
            {
                if (to != from)
                {
                    GetSpinePointsRoutine(points, to, current);
                }
            });
        }

        protected List<Vertex2D> GetSpinePoints(Chord2D chord) {
            var points = new List<Vertex2D>();
            GetSpinePointsRoutine(points, chord, null);
            return points;
        }

        protected Dictionary<Vertex2D, VertexNetwork2D> BuildTable(Triangulation2D tri) {
            var table = new Dictionary<Vertex2D, VertexNetwork2D>();
            var triangles = tri.Triangles;
            for (int i = 0, n = triangles.Length; i < n; i++) {
                var t = triangles[i];
                Vertex2D a = t.a, b = t.b, c = t.c;
                if (!table.ContainsKey(a)) table.Add(a, new VertexNetwork2D(a, ExternalPoint(a)));
                if (!table.ContainsKey(b)) table.Add(b, new VertexNetwork2D(b, ExternalPoint(b)));
                if (!table.ContainsKey(c)) table.Add(c, new VertexNetwork2D(c, ExternalPoint(c)));
            }
            // tri.Points.ForEach(p => {
            // if(!table.ContainsKey(p)) table.Add(p, new VertexNetwork2D(p, ExternalPoint(p)));
            // });
            return table;
        }

        protected List<VertexNetwork2D> BuildNetworks(Triangulation2D tri, Dictionary<Vertex2D, VertexNetwork2D> networkTable) {
            var network = new Dictionary<Vertex2D, HashSet<Vertex2D>>();

            var triangles = tri.Triangles;
            for (int i = 0, n = triangles.Length; i < n; i++) {
                var t = triangles[i];
                Segment2D s0 = t.s0, s1 = t.s1, s2 = t.s2;
                if (!network.ContainsKey(t.a)) {
                    network.Add(t.a, new HashSet<Vertex2D>());
                }
                if (!network.ContainsKey(t.b)) {
                    network.Add(t.b, new HashSet<Vertex2D>());
                }
                if (!network.ContainsKey(t.c)) {
                    network.Add(t.c, new HashSet<Vertex2D>());
                }
                network[s0.a].Add(s0.b); network[s0.b].Add(s0.a);
                network[s1.a].Add(s1.b); network[s1.b].Add(s1.a);
                network[s2.a].Add(s2.b); network[s2.b].Add(s2.a);
            }

            return network.Keys.Select(v => {
                var n = networkTable[v];
                foreach (Vertex2D to in network[v]) {
                    n.Connect(networkTable[to]);
                }
                return n;
            }).ToList();
        }

        protected List<Segment2D> BuildContourSegments(Triangulation2D triangulation) {
            var triangles = triangulation.Triangles;

            var table = new Dictionary<Segment2D, HashSet<Triangle2D>>();
            for (int i = 0, n = triangles.Length; i < n; i++) {
                var t = triangles[i];
                if (!table.ContainsKey(t.s0)) table.Add(t.s0, new HashSet<Triangle2D>());
                if (!table.ContainsKey(t.s1)) table.Add(t.s1, new HashSet<Triangle2D>());
                if (!table.ContainsKey(t.s2)) table.Add(t.s2, new HashSet<Triangle2D>());

                table[t.s0].Add(t);
                table[t.s1].Add(t);
                table[t.s2].Add(t);
            }

            return table.Keys.ToList().FindAll(s => {
                return table[s].Count == 1;
            }).ToList();
        }

        /*
		 * each vertex of the spine is elevated proportionally to the average distance
		 * between the vertex and the external vertices 
		 * that are directly connected to the vertex 
		*/
        protected void Elevate(List<VertexNetwork2D> networks) {
            for (int i = 0; i < 3; i++) {
                networks.ForEach(network => {
                    network.Elevate();
                });
            }
        }

        protected void SewTriangle(Dictionary<Triangle2D, bool> flags, Dictionary<Triangle2D, List<Triangle2D>> sews, Triangle2D t, Segment2D[] segments, int division)
        {
            triangulation.RemoveTriangle(t);
            var sewTriangles = Sew(heightTable, triangulation, segments[0], segments[1], division);
            sews.Add(t, sewTriangles);
            flags[t] = true;
        }

        protected void SewRoutine(List<Triangle2D> triangles, Dictionary<Triangle2D, bool> flags, Dictionary<Triangle2D, List<Triangle2D>> sews, Chord2D current, Chord2D from, int division)
        {
            if (!current.Pruned)
            {
                triangles.FindAll(t =>
                {
                    return (t.HasPoint(current.Src) && (t.HasPoint(current.Dst) || !sews.ContainsKey(t)));
                }).ForEach(t =>
                {
                    Segment2D[] segments;

                    if (sews.ContainsKey(t))
                    {
                        foreach (Triangle2D st in sews[t])
                        {
                            triangulation.RemoveTriangle(st);
                        }
                        sews.Remove(t);
                    }

                    if (t.HasPoint(current.Dst))
                    {
                        Segment2D s = t.CommonSegment(current.Src, current.Dst);
                        segments = t.ExcludeSegment(s);
                    }
                    else
                    {
                        segments = t.CommonSegments(current.Src);
                    }

                    SewTriangle(flags, sews, t, segments, division);
                });
            }

            var next = current.Connection.FindAll(con => con != from);

            // Prune後のSpineで末端にあたる頂点
            bool terminal = next.All(con => con.Pruned);
            if (terminal && !current.Pruned)
            {
                triangles.FindAll(t =>
                {
                    return !sews.ContainsKey(t) && t.HasPoint(current.Dst);
                }).ForEach(t =>
                {
                    var segments = t.CommonSegments(current.Dst);
                    SewTriangle(flags, sews, t, segments, division);
                });
            }

            next.ForEach(to =>
            {
                SewRoutine(triangles, flags, sews, to, current, division);
            });
        }

        protected void Sew (Triangulation2D triangulation, Chord2D chord, Dictionary<Vertex2D, float> heightTable, int division) {
			var triangles = triangulation.Triangles.ToList();

			Dictionary<Triangle2D, bool> flags = new Dictionary<Triangle2D, bool>();

			// 既にSew済みのTriangle2Dを再度Sewする必要があるケースが存在する
			// 	特定のChord2DのSrcとDstを含むTriangle2Dなのに，それ以外のChord2Dと共通点を持っているがために
			//	Spineと共通エッジを持つ場合のSewの処理がかけられず適切な分割ができていないケース
			Dictionary<Triangle2D, List<Triangle2D>> sews = new Dictionary<Triangle2D, List<Triangle2D>>();

			triangles.ForEach(t => flags.Add(t, false));

			SewRoutine(triangles, flags, sews, chord, null, division);
		}

		protected List<Triangle2D> Sew (Dictionary<Vertex2D, float> heightTable, Triangulation2D tri, Segment2D left, Segment2D right, int division) {
			Vertex2D top, lb, rb;

			if(left.a == right.a) {
				top = left.a; lb = left.b; rb = right.b;
			} else if(left.a == right.b) {
				top = left.a; lb = left.b; rb = right.a;
			} else if(left.b == right.a) {
				top = left.b; lb = left.a; rb = right.b;
			} else {
				top = left.b; lb = left.a; rb = right.a;
			}

			Vertex2D[] lp = new Vertex2D[division];
			Vertex2D[] rp = new Vertex2D[division];
			lp[division - 1] = lb;
			rp[division - 1] = rb;

			float inv = 1f / division;
			Vector2 ld = lb.Coordinate - top.Coordinate, rd = rb.Coordinate - top.Coordinate;
			float lm = ld.magnitude, rm = rd.magnitude;
			// float th = heightTable[top], lh = heightTable[lb], rh = heightTable[rb];
			float th = heightTable[top], lh = heightTable[lb] - th, rh = heightTable[rb] - th;

			for(int i = 0; i < division - 1; i++) {
				float r = (float)(i + 1) * inv;
				lp[i] = triangulation.CheckAndAddVertex(top.Coordinate + ld * r);
				if(!heightTable.ContainsKey(lp[i])) {
					// heightTable.Add(lp[i], th + (lh - th) * inv * (i + 1));
					heightTable.Add(lp[i], th + QuarterOval(lh, lm, inv * (i + 1)));
				}

				rp[i] = triangulation.CheckAndAddVertex(top.Coordinate + rd * r);
				if(!heightTable.ContainsKey(rp[i])) {
					// heightTable.Add(rp[i], th + (rh - th) * inv * (i + 1));
					heightTable.Add(rp[i], th + QuarterOval(rh, rm, inv * (i + 1)));
				}
			}

			var triangles = new List<Triangle2D>();

			triangles.Add(triangulation.AddTriangle(top, lp[0], rp[0]));
			for(int i = 0; i < division - 1; i++) {
				var tl = triangulation.AddTriangle(lp[i], rp[i], lp[i + 1]);
				var tr = triangulation.AddTriangle(rp[i], rp[i + 1], lp[i + 1]);
				triangles.Add(tl); triangles.Add(tr);
			}

			return triangles;
		}

		// http://www.mathopenref.com/coordparamellipse.html
		// r : 0.0 ~ 1.0
		const float HalfPI = Mathf.PI * 0.5f;
		protected float QuarterOval (float height, float distance, float r) {
			if(height >= 0f) {
				return height * Mathf.Sin(r * HalfPI);
			}
			return height * (1f - Mathf.Sin((1f - r) * HalfPI));
		}

		protected Mesh Symmetrize (Mesh src) {
			var mesh = new Mesh();
			var vertices = new List<Vector3>();
			var triangles = new List<int>();

			Func<Vector3, bool> Contour = (Vector3 v) => { return v.z > -float.Epsilon; };
			Func<Vector3, Vector3> Symmetrize = (Vector3 v) => { return new Vector3(v.x, v.y, -v.z); };

			vertices.AddRange(src.vertices);
			for(int i = 0, n = src.vertices.Length; i < n; i++) {
				var v = src.vertices[i];
				if(!Contour(v)) {
					vertices.Add(Symmetrize(v));
				}
			}

			for(int i = 0, n = src.triangles.Length; i < n; i += 3) {
				int a = src.triangles[i], b = src.triangles[i + 1], c = src.triangles[i + 2];
				Vector3 va = vertices[a], vb = vertices[b], vc = vertices[c];
				triangles.Add(a); triangles.Add(b); triangles.Add(c); 

				int na, nb, nc;

				if(Contour(va)) {
					na = a;
				} else {
					na = vertices.IndexOf(Symmetrize(va));
				}

				if(Contour(vb)) {
					nb = b;
				} else {
					nb = vertices.IndexOf(Symmetrize(vb));
				}

				if(Contour(vc)) {
					nc = c;
				} else {
					nc = vertices.IndexOf(Symmetrize(vc));
				}

				// counter triangle indices order
				triangles.Add(na); triangles.Add(nc); triangles.Add(nb); 
			}

			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			return mesh;
		}

		protected List<Chord2D> Traverse (Chord2D cur, Chord2D from, Func<Chord2D, bool> check) {
			var chords = new List<Chord2D>();

			if(check(cur)) {
				chords.Add(cur);
			}

			cur.Connection.ForEach(to => {
				if(to != from) {
					chords.AddRange(Traverse(to, cur, check));
				}
			});

			return chords;
		}

		protected float Angle (Vector2 from, Vector2 to0, Vector2 to1) {
			var v0 = (to0 - from);
			var v1 = (to1 - from);

			// 0 ~ PI
			float acos = Mathf.Acos(Vector2.Dot(v0, v1) / Mathf.Sqrt(v0.sqrMagnitude * v1.sqrMagnitude));
			return acos;
		}

		protected float Angle (Vector2 p0, Vector2 p1) {
			return Mathf.Atan2(p1.y - p0.y, p1.x - p0.x);
		}

	}

}

