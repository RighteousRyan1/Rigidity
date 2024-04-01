using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace Rigidity.Debug;

public static class MeshRenderer {
    private static Dictionary<Mesh2D, Color> _meshes = [];
    private static List<VertexPositionColor> _colors = [];
    private static VertexBuffer _buffer;
    public static void AddMesh(Mesh2D mesh, Color color) {
        _meshes.Add(mesh, color);
        ResizeBuffer();
    }
    public static void RemoveMesh(Mesh2D mesh) {
        _meshes.Remove(mesh);
        ResizeBuffer();
    }

    public static void ResizeBuffer() {
        _buffer = new(Main.instance.GraphicsDevice, typeof(VertexPositionColor), _meshes.Count * 2, BufferUsage.WriteOnly);
    }

    public static void DrawAll() {

    }
}
public readonly struct Mesh2D {
    public readonly Vector2 Corner1;
    public readonly Vector2 Corner2;
    public readonly Vector2 Corner3;
    public readonly Vector2 Corner4;

    public Mesh2D(Vector2 c1, Vector2 c2, Vector2 c3, Vector2 c4) {
        Corner1 = c1;
        Corner2 = c2;
        Corner3 = c3;
        Corner4 = c4;
    }
    public Mesh2D(Vector2[] corners) {
        if (corners.Length > 4)
            throw new ArgumentException("", nameof(corners));
        Corner1 = corners[0];
        Corner2 = corners[1];
        Corner3 = corners[2];
        Corner4 = corners[3];
    }
}
