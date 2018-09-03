This repository is deprecated. 
[The official release version](https://www.assetstore.unity3d.com/#!/content/99075) is in the asset store!

[Teddy](https://www.assetstore.unity3d.com/#!/content/99075)

unity-teddy
=====================

Teddy algorithm (converting 2D polygon into 3D model) implementation in Unity.

<img src="https://raw.githubusercontent.com/mattatz/unity-teddy/master/Captures/Demo.gif" width="620px">

## Usage

```cs
// input points for a Polygon2D contor
List<Vector2> points = new List<Vector2>();

// Add Vector2 to points
points.Add(new Vector2(-2.5f, -2.5f));
points.Add(new Vector2(2.5f, -2.5f));
points.Add(new Vector2(4.5f, 2.5f));
points.Add(new Vector2(0.5f, 4.5f));
points.Add(new Vector2(-3.5f, 2.5f));

// construct Teddy 
Teddy teddy = new Teddy(points);

Mesh mesh = teddy.Build(
    MeshSmoothingMethod.HC,  // select mesh smoothing methods : None, Laplacian, HC
    5, // count of smoothing
    0.25f, // alpha value for smoothing 
    0.5f // beta value for smoothing
);
// GetComponent<MeshFilter>().sharedMesh = mesh;
```

## Demo

[Demo](https://mattatz.github.io/unity/teddy)

## Compatibility 

tested on Unity 2018.2.6f, windows10 (GTX 1060).

## Sources

- Teddy: A Sketching Interface for 3D Freeform Design - http://www-ui.is.s.u-tokyo.ac.jp/~takeo/papers/siggraph99.pdf
- mattatz/unity-triangulation2D - https://github.com/mattatz/unity-triangulation2D
- mattatz/unity-mesh-smoothing - https://github.com/mattatz/unity-mesh-smoothing

