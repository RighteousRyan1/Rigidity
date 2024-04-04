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
using Terraria.UI.Chat;

namespace Rigidity;

public class EntityConvexSystem : ModSystem
{
    public static Dictionary<int, Vector2[]> ItemIDToConvexHull = [];
    public static Dictionary<int, Vector2[]> GoreIDToConvexHull = [];
    private static bool _drawNotice;
    private static bool _displayCounts;
    private static int _hullsCreated;
    private static float _opacity = 1f;
    public override void PostSetupContent() {
        On_Main.PostDrawMenu += DrawNoticeString;
        _drawNotice = true;
    }
    private void DrawNoticeString(On_Main.orig_PostDrawMenu orig, Point screenSizeCache, Point screenSizeCacheAfterScaling) {
        orig(screenSizeCache, screenSizeCacheAfterScaling);
        var scale = 0.6f;
        var font = FontAssets.DeathText.Value;
        var pos = new Vector2(Main.screenWidth / 2, Main.screenHeight);
        Main.spriteBatch.Begin();
        if (_drawNotice) {
            var notice = "Item Physics Maps are being generated! Please wait. This could take a while...";
            var textDims = font.MeasureString(notice) * scale;
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, notice, pos, Color.White, 0f, new Vector2(textDims.X / 2, textDims.Y), new Vector2(scale));
        }
        if (_displayCounts) {
            var notice = "Created " + _hullsCreated + " hulls";
            var textDims = font.MeasureString(notice) * scale;
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, notice, pos, Color.Lime * _opacity, 0f, new Vector2(textDims.X / 2, textDims.Y), new Vector2(scale));
            if (_opacity > 0)
                _opacity -= 0.0075f;
            else {
                _opacity = 0;
                _displayCounts = false;
                On_Main.PostDrawMenu -= DrawNoticeString;
            }
        }
        Main.spriteBatch.End();
    }

    public override void PostAddRecipes() {
        Main.QueueMainThreadAction(() => {
            GenerateConvexShapesPerItem();
            GenerateConvexShapesPerGore();
        });
        _drawNotice = false;
    }
    public static void GenerateConvexShapesPerItem() {
        ItemIDToConvexHull.Clear();
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
        _hullsCreated += ItemIDToConvexHull.Count;
    }
    public static void GenerateConvexShapesPerGore() {
        GoreIDToConvexHull.Clear();
        for (int i = 1; i < GoreLoader.GoreCount; i++) {
            // these gores just don't exist for some reason
            if (i == 507 || i == 537)
                continue;
            Main.instance.LoadGore(i);
            var texture = TextureAssets.Gore[i].Value;
            // if there is no registered DrawAnimation for an item, it just has one frame
            var vertices = GetHullVertices(texture);

            if (vertices.Length > 2) {
                var hull = ConvexHull.Create2D(vertices);
                GoreIDToConvexHull.Add(i, hull.Result.Select(v => new Vector2((float)v.X, (float)v.Y)).ToArray());
            }
        }
        _displayCounts = true;
        _hullsCreated += GoreIDToConvexHull.Count;
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
    public static Vector2 GetCentroid(params Vector2[] vertices) {
        Vector2 total = new();
        for (int i = 0; i < vertices.Length; i++)
            total += vertices[i];
        total /= vertices.Length;
        return total;
    }
}
