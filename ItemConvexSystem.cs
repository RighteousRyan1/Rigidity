using MIConvexHull;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using phys = nkast.Aether.Physics2D.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Rigidity;

public class ItemConvexSystem : ModSystem
{
    public static Dictionary<int, Vector2[]> ItemIDToConvexHull = new();
    public override void PostAddRecipes() {
        // two things to figure out here.
        // 1) Only 140-150 (give or take?) items are being loaded into the dictionary. Find out why. try different things?
        // 2) The convex hull for the star i checked randomly has vertices way past the 24x24 limit. check on that as well.
        GenerateConvexShapesPerItem();
    }
    public static void GenerateConvexShapesPerItem() {
        Main.QueueMainThreadAction(() => {
            for (int i = 0; i < ItemLoader.ItemCount; i++) {
                Main.instance.LoadItem(i);
                var texture = TextureAssets.Item[i].Value;
                // if there is no registered DrawAnimation for an item, it just has one frame
                var frameCount = Main.itemAnimations[i] is not null ? Main.itemAnimations[i].FrameCount : 1;
                var vertices = GetHullVertices(texture, texture.Height / frameCount);

                if (vertices.Length > 2) {
                    var hull = ConvexHull.Create2D(vertices);
                    ItemIDToConvexHull.Add(i, hull.Result.Select(v => new Vector2((float)v.X, (float)v.Y)).ToArray());
                }
            }
        });
    }
    public static DefaultVertex2D[] GetHullVertices(Texture2D texture, int yCutoff = 0) {
        var realHeight = texture.Height - (texture.Height - yCutoff);
        Color[] pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        // resize the colors array to only contain the colors within the y-range [0, yCutoff]
        pixels = pixels[..((texture.Height - (texture.Height - yCutoff)) * texture.Width)];
        var vertices = new List<DefaultVertex2D>();
        for (int x = 0; x < texture.Width; x++) {
            for (int y = 0; y < realHeight; y++) {
                Color px = pixels[x + y * texture.Width];
                if (px.A > 0)
                    vertices.Add(new(x, y));
            }
        }
        return [..vertices];
    }
    public static List<phys.Vertices> Vector2ArrayToVerticesList(Vector2[] vertices) {
        List<phys.Vertices> list = [];
        for (int i = 0; i < vertices.Length; i++) {
            list.Add(new phys.Vertices([
                new(vertices[i].X, vertices[i].Y)
            ]));
        }
        return list;
    }
    public static phys.Vertices Vector2ArrayToVertices(Vector2[] vertices) {
        phys.Vertices verts = [];
        for (int i = 0; i < vertices.Length; i++) {
            verts.Add(new(vertices[i].X, vertices[i].Y));
        }
        return verts;
    }
    public static Vector2[] VerticesToVector2Array(phys.Vertices vertices) {
        List<Vector2> verts = new();
        foreach (var vert in vertices)
            verts.Add(new(vert.X, vert.Y));
        return [..verts];
    }

    public static Vector2 Center(params Vector2[] vertices) {
        Vector2 total = new();
        for (int i = 0; i < vertices.Length; i++)
            total += vertices[i];
        total /= vertices.Length;
        return total;
    }
}
