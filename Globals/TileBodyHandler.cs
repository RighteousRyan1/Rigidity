using XNA = Microsoft.Xna.Framework;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Rigidity.Physics;

namespace Rigidity.Globals;
// for items, construct physics maps onto each item on mod load.
public class TileBodyHandler : ModSystem {
    public const int CHECK_DIMS = 80;

    public static bool[] TileCurrent = new bool[CHECK_DIMS * CHECK_DIMS];
    public static bool[] OldTileCurrent = new bool[CHECK_DIMS * CHECK_DIMS];
    public static BlockType[] OldTileTypes = new BlockType[CHECK_DIMS * CHECK_DIMS];
    public static BlockType[] TileTypes = new BlockType[CHECK_DIMS * CHECK_DIMS];
    public static Dictionary<int, Body> TileBodies = new();

    public static int SlopesDownRightAccountedFor;
    public static int SlopesDownLeftAccountedFor;
    public static int SlopesUpRightAccountedFor;
    public static int SlopesUpLeftAccountedFor;

    public static int FullBlocksAccountedFor;
    public static int HalfBlocksAccountedFor;

    public static XNA.Rectangle PlayerCheckAround;
    public override void PostUpdateEverything() {
        var player = Main.LocalPlayer;
        PlayerCheckAround = new XNA.Rectangle((int)player.Center.X - CHECK_DIMS * 16 / 2, (int)player.Center.Y - CHECK_DIMS * 16 / 2, CHECK_DIMS * 16, CHECK_DIMS * 16);
        // this is definitely horrid. who cares for right now.
        if (Main.GameUpdateCount % 20 == 0) {
            for (int i = 0; i < PhysicsSystem.World.BodyList.Count; i++) {
                if (!TileBodies.ContainsValue(PhysicsSystem.World.BodyList[i]) && PhysicsSystem.World.BodyList[i].BodyType == BodyType.Static)
                    PhysicsSystem.World.Remove(PhysicsSystem.World.BodyList[i]);
            }
        }
        if (Main.GameUpdateCount % 1 == 0) {
            // reset all of these to maintain accurate counts.
            FullBlocksAccountedFor = 0;
            HalfBlocksAccountedFor = 0;
            SlopesDownLeftAccountedFor = 0;
            SlopesDownRightAccountedFor = 0;
            SlopesUpLeftAccountedFor = 0;
            SlopesUpRightAccountedFor = 0;
            // i'll have to either remove and re-create or just update positions.
            int iteration = 0;
            for (int i = (int)player.Center.X / 16 - CHECK_DIMS / 2; i < (int)player.Center.X / 16 + CHECK_DIMS / 2; i++) {
                for (int j = (int)player.Center.Y / 16 - CHECK_DIMS / 2; j < (int)player.Center.Y / 16 + CHECK_DIMS / 2; j++) {
                    if (WorldGen.InWorld(i, j)) {
                        var t = Framing.GetTileSafely(i, j);

                        TileCurrent[iteration] = t.HasTile && Main.tileSolid[t.TileType];
                        if (t.HasTile && Main.tileSolid[t.TileType]) {
                            TileTypes[iteration] = t.BlockType;
                            var pos = new Vector2(i, j) * 16 / PhysicsSystem.UNITS_PER_METER; // from tile to pixel coordinates
                            if (!TileBodies.ContainsKey(iteration) /*&& !TileBodies.Values.Any(x => x.Position == pos)*/) {
                                if (t.BlockType == BlockType.Solid) {
                                    TileBodies.Add(iteration, GenBlockFull(pos));
                                    FullBlocksAccountedFor++;
                                }
                                else if (t.BlockType == BlockType.HalfBlock) {
                                    TileBodies.Add(iteration, GenBlockHalf(pos));
                                    HalfBlocksAccountedFor++;
                                }
                                else if (t.BlockType == BlockType.SlopeDownRight) {
                                    TileBodies.Add(iteration, GenSlopeDR(pos));
                                    SlopesDownRightAccountedFor++;
                                }
                                else if (t.BlockType == BlockType.SlopeDownLeft) {
                                    TileBodies.Add(iteration, GenSlopeDL(pos));
                                    SlopesDownLeftAccountedFor++;
                                }
                                // for some reason slopes are gaining rectangular collision.
                                // downright and downleft are flipped. wtf.
                            }
                            else {
                                if (t.BlockType == BlockType.Solid) {
                                    FullBlocksAccountedFor++;
                                    if (TileTypes[iteration] != OldTileTypes[iteration]) {
                                        TileBodies[iteration] = GenBlockFull(pos);
                                    }
                                }
                                else if (t.BlockType == BlockType.HalfBlock) {
                                    HalfBlocksAccountedFor++;
                                    if (TileTypes[iteration] != OldTileTypes[iteration]) {
                                        TileBodies[iteration] = GenBlockHalf(pos);
                                    }
                                }
                                else if (t.BlockType == BlockType.SlopeDownRight) {
                                    SlopesDownRightAccountedFor++;
                                    if (TileTypes[iteration] != OldTileTypes[iteration]) {
                                        TileBodies[iteration] = GenSlopeDR(pos);
                                    }
                                }
                                else if (t.BlockType == BlockType.SlopeDownLeft) {
                                    SlopesDownLeftAccountedFor++;
                                    if (TileTypes[iteration] != OldTileTypes[iteration]) {
                                        TileBodies[iteration] = GenSlopeDL(pos);
                                    }
                                }
                                TileBodies[iteration].Position = pos;
                            }
                        }
                        // remove tiles that now have no block current.
                        if ((OldTileCurrent[iteration] && !TileCurrent[iteration]) || (TileTypes[iteration] != OldTileTypes[iteration])) {
                            if (TileBodies.TryGetValue(iteration, out var body)) {
                                if (PhysicsSystem.World.BodyList.Contains(body)) {
                                    PhysicsSystem.World.Remove(body);
                                }
                                TileBodies.Remove(iteration);
                            }
                        }
                        OldTileTypes[iteration] = t.BlockType;
                        OldTileCurrent[iteration] = t.HasTile && Main.tileSolid[t.TileType];
                    }
                    iteration++;
                }
            }
            // this loop is causing weird ass stuff in the placement of physics bodies.
            // TODO: fix this problem next time i boot up this project.
            /*for (int i = 0; i < PhysicsSystem.World.BodyList.Count; i++) {
                var body = PhysicsSystem.World.BodyList[i];
                if (body.Tag.Equals(PhysicsTags.DynamicPhysicsBody))
                    continue;
                if (!PlayerCheckAround.Contains((int)(body.Position.X * PhysicsSystem.UnitsPerMeter), (int)(body.Position.Y * PhysicsSystem.UnitsPerMeter))) {
                    PhysicsSystem.World.Remove(body);
                    // body is a slope or a block
                    if (body.Tag.EqualsAny(PhysicsTags.TileBodyRectangular, PhysicsTags.TileBodyTriangular)) {
                        //TileBodies.Remove(iteration);
                    }
                }
            }*/
            //Main.NewText("total: " + PhysicsSystem.World.BodyList.Count);
            //Main.NewText("tilesOnly: " + TileBodies.Count);
            //Main.NewText($"fb: {FullBlocksAccountedFor} | hb: {HalfBlocksAccountedFor} | sdl: {SlopesDownLeftAccountedFor} | sdr {SlopesDownRightAccountedFor}");
        }
        //Main.NewText(Main.MouseWorld);
        //Main.NewText(Main.MouseWorld / PhysicsSystem.UNITS_PER_METER);
    }

    // pos = tileCoords * 16 / UnitsPerMeter
    // pos expects a pixel coordinate (from terraria, not the physics system)
    public static Body GenBlockFull(Vector2 pos) {
        var d = 16 / PhysicsSystem.UNITS_PER_METER;
        var body = PhysicsSystem.World.CreateRectangle(
                                        d, d, 1f,
                                        pos, bodyType: BodyType.Static);
        body.Tag = PhysicsTags.Rectangle(pos.X, pos.Y, d, d);
        return body;
    }
    public static Body GenBlockHalf(Vector2 pos) {
        var w = 16 / PhysicsSystem.UNITS_PER_METER;
        var h = 2 / PhysicsSystem.UNITS_PER_METER;
        Vector2 position = pos + new Vector2(0, PhysicsSystem.PIXELS_PER_TILE * 0.8f) / PhysicsSystem.UNITS_PER_METER;
        var body = PhysicsSystem.World.CreateRectangle(
                                        w, h, 1f,
                                        position, bodyType: BodyType.Static);
        body.Tag = PhysicsTags.Rectangle(position.X, position.Y, w, h);
        return body;
    }
    public static Body GenSlopeDL(Vector2 pos) {
        var hypStart = pos;
        // 16 because that's how many units a tile is wide/tall
        var hypEnd = hypStart + new Vector2(PhysicsSystem.PIXELS_PER_TILE / PhysicsSystem.UNITS_PER_METER);
        var vertices = new Vertices(new List<Vector2>() {
                // hypoteneuse start
                hypStart,
                // hypoteneuse end
                hypEnd,
                // right triangle corner
                PhysicsSystem.GetCorner(hypStart, hypEnd), hypEnd - new Vector2(100, 0)
        });
        // i'm convinced this polyogn is just creating problems.
        var body = PhysicsSystem.World.CreatePolygon(vertices, 1f, pos);
        body.Tag = PhysicsTags.Triangle(hypStart, hypEnd);
        return body;
    }
    public static Body GenSlopeDR(Vector2 pos) {
        var hypStart = pos + new Vector2(0, PhysicsSystem.PIXELS_PER_TILE / PhysicsSystem.UNITS_PER_METER);
        // 16 because that's how many units a tile is wide/tall
        var hypEnd = pos + new Vector2(PhysicsSystem.PIXELS_PER_TILE / PhysicsSystem.UNITS_PER_METER, 0);
        var vertices = new Vertices(new List<Vector2>() {
                // hypoteneuse start
                hypStart,
                // hypoteneuse end
                hypEnd,
                // right triangle corner
                PhysicsSystem.GetCorner(hypStart, hypEnd)
        });
        var body = PhysicsSystem.World.CreatePolygon(vertices, 1f, pos);
        body.Tag = PhysicsTags.Triangle(hypEnd, hypStart); // reversed because GetCorner is goofy
        return body;
    }
}
public enum Translation {
    TileToPixel,
    TileToPhysics,
    PixelToPhysics
}