using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using nkast.Aether.Physics2D.Dynamics;
using Rigidity.Debug;
using Rigidity.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using phys = nkast.Aether.Physics2D.Common;

namespace Rigidity.Globals;
public class PhysicsSystem : ModSystem {
    public static World World = new(new phys.Vector2(0, 0.1f));

    public const float UNITS_PER_METER = 4f;
    public const float PIXELS_PER_TILE = 16;
    public const float UNITS_PER_TILE = PIXELS_PER_TILE / UNITS_PER_METER;

    public static Dictionary<Body, (int Id, int BodyId, Texture2D Texture, Vector2 Center)> DynamicBodies = new();
    public static List<int> BodyLifetimes = new();
    public static float DefaultFriction = 0.5f;
    public override void Load() {
        On_Main.NewText_string_byte_byte_byte += ChangeGravity;
        On_Gore.NewGore_Vector2_Vector2_int_float += ReplaceGores;
        On_Item.NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool += ReplaceWithPhysics;
    }
    // p sure this is the base method
    private int ReplaceWithPhysics(On_Item.orig_NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool orig, Terraria.DataStructures.IEntitySource source, int X, int Y, int Width, int Height, int Type, int Stack, bool noBroadcast, int pfix, bool noGrabDelay, bool reverseLookup) {
        //var item = new Item();
        //item.SetDefaults(Type);
        //PhysicsDrop(new Vector2(X, Y), Vector2.Zero, item);
        //return item.whoAmI;
        return orig(source, X, Y, Width, Height, Type, Stack, noBroadcast, pfix, noGrabDelay, reverseLookup);
    }

    private int ReplaceGores(On_Gore.orig_NewGore_Vector2_Vector2_int_float orig, Vector2 Position, Vector2 Velocity, int Type, float Scale) {
        return orig(Position, Velocity, Type, Scale);
    }

    private void ChangeGravity(On_Main.orig_NewText_string_byte_byte_byte orig, string newText, byte R, byte G, byte B) {
        if (float.TryParse(newText, out float f)) {
            World.Gravity = new(0, f);
            orig("Changed gravity to " + f, R, G, B);
        }
        orig(newText, R, G, B);
    }
    public override void PostUpdateEverything() {
        _timeSinceLastCollision++;
        //Main.NewText("MouseReal: " + Main.MouseWorld);
        //Main.NewText("MousePhys: " + Main.MouseWorld / UNITS_PER_METER);
        World.Step(1);
        World.Gravity = new(0, 0.1f);

        for (int i = 0; i < BodyLifetimes.Count; i++) {
            BodyLifetimes[i]++;
        }

        if (Main.keyState.IsKeyDown(Keys.P) && Main.oldKeyState.IsKeyUp(Keys.P))
            debug = !debug;
        if (Main.keyState.IsKeyDown(Keys.L) && Main.oldKeyState.IsKeyUp(Keys.L)) {
            if (DynamicBodies != null) {
                for (int i = 0; i < DynamicBodies.Count; i++) {
                    var b = DynamicBodies.ElementAt(i);
                    World.Remove(b.Key);
                    DynamicBodies.Remove(b.Key);
                }
                BodyLifetimes.Clear();
            }
        }
        if (Main.mouseRight && Main.mouseRightRelease && !Main.playerInventory) {
            if (!Main.LocalPlayer.HeldItem.IsAir) {
                // this just doesn't clone the item lol
                var item = Main.LocalPlayer.HeldItem;
                var velocity = (Main.MouseWorld - Main.LocalPlayer.Center) / 5;//new Vector2(5 * Main.LocalPlayer.direction, 2.5f);
                var pos = Main.LocalPlayer.Center - TextureAssets.Item[item.type].Size() + new Vector2(20, 0).RotatedBy((Main.MouseWorld - Main.LocalPlayer.Center).ToRotation()) * Main.LocalPlayer.direction;
                // Main.LocalPlayer.Center - new Vector2(40, 100) + new Vector2(15 * Main.LocalPlayer.direction, 0).RotatedBy((Main.MouseWorld - Main.LocalPlayer.Center).ToRotation())
                var clone = item.Clone();
                PhysicsDrop(pos, velocity, clone);
                Main.LocalPlayer.HeldItem.TurnToAir();
                Main.LocalPlayer.PlayDroppedItemAnimation(10);
            }
        }
        if (DynamicBodies != null) {
            for (int i = 0; i < DynamicBodies.Count; i++) {
                var b = DynamicBodies.ElementAt(i);
                if (EntityConvexSystem.ItemIDToConvexHull[b.Value.Id].Any(x => Collision.DrownCollision(b.Key.Position.ToXnaV2() * UNITS_PER_METER + (x / UNITS_PER_METER).RotatedBy(b.Key.Rotation), 16, 16))) {
                    b.Key.LinearDamping = 0.175f;
                    b.Key.AngularDamping = 0.175f;
                    // _cb.LinearVelocity -= new phys.Vector2(0, 0.11f);
                }
                else {
                    b.Key.LinearDamping = 0;
                    b.Key.AngularDamping = 0;
                }
            }
        }
    }
    public void PhysicsDrop(Vector2 position, Vector2 velocity, Item item) {
        var id = item.type;
        var tex = TextureAssets.Item[Main.LocalPlayer.HeldItem.type].Value;
        var vertices = EntityConvexSystem.Vector2ArrayToVertices(EntityConvexSystem.ItemIDToConvexHull[id]);
        // call it before so i can step over and see the actual texture center
        for (int i = 0; i < vertices.Count; i++) {
            vertices[i] /= UNITS_PER_METER;
        }
        var center = EntityConvexSystem.GetCentroid(EntityConvexSystem.VerticesToVector2Array(vertices));

        var body = World.CreatePolygon(vertices, 1f, position.ToPhysV2() / UNITS_PER_METER, bodyType: BodyType.Dynamic);
        body.LinearVelocity = velocity.ToPhysV2();
        body.AngularDamping = 0f;
        body.OnCollision += SetProperties;
        body.SleepingAllowed = false;
        body.Tag = PhysicsTags.Item(item);
        body.AngularVelocity = velocity.Length() / 250;
        BodyLifetimes.Add(0);
        DynamicBodies.Add(body, (id, DynamicBodies.Count, tex, center));
    }
    private static uint _timeSinceLastCollision;
    private bool SetProperties(Fixture sender, Fixture other, nkast.Aether.Physics2D.Dynamics.Contacts.Contact contact) {
        var rounded = new Point(((int)(other.Body.Position.X * UNITS_PER_METER)) / 16, ((int)(other.Body.Position.Y * UNITS_PER_METER)) / 16);
        var tile = Framing.GetTileSafely(rounded);
        var tileType = tile.TileType;
        var absVel = MathHelper.Lerp(0f, 2f, new Vector2(MathF.Abs(sender.Body.LinearVelocity.X), MathF.Abs(sender.Body.LinearVelocity.Y)).Length());
        //var contactPositionInWorld = (sender.Body.Position.ToXnaV2() + contact.Manifold.LocalPoint.ToXnaV2().RotatedBy(sender.Body.Rotation)) * UNITS_PER_METER;
        /*var contactPointsInWorld = new List<Vector2>() {
            contact.Manifold.Points[0].LocalPoint.ToXnaV2(),
            contact.Manifold.Points[1].LocalPoint.ToXnaV2()
        };*/
        //Main.NewText(contact.Manifold.Points[0].LocalPoint + " " + contact.Manifold.Points[1].LocalPoint);
        //Main.NewText(absVel);
        var volScale = 0.08f;

        int timeBeforeSounds = 10;
        var tileSolid = Main.tileSolid[tileType];
        var tileSolidTop = Main.tileSolidTop[tileType];
        /*if (tileSolidTop) {
            if (sender.Body.Position.Y > other.Body.Position.Y) {
                // oddly enough doesn't like to work.
                return false;
            }
        }*/
        var senderTags = (PhysicsTags)sender.Body.Tag;
        var otherTags = (PhysicsTags)other.Body.Tag;
        if (StoneBlocks.Contains(tileType)) {
            if (_timeSinceLastCollision > timeBeforeSounds) {
                SoundEngine.PlaySound(SoundID.Tink.WithVolumeScale(absVel * volScale), sender.Body.Position.ToXnaV2() * UNITS_PER_METER);
                _timeSinceLastCollision = 0;
            }
            // dust spawning works fine when there's no rotation involved, otherwise no.
            /*contactPointsInWorld.ForEach(x => {
                var d = Dust.NewDustPerfect((sender.Body.Position.ToXnaV2() + x) * UNITS_PER_METER + offset, DustID.Stone, Vector2.Zero);
                d.noGravity = true;
                });*/
        }
        if (IceBlocks.Contains(tileType)) {
            // SoundEngine.PlaySound(SoundID.SomeIceSound.WithVolumeScale(absVel * volScale), sender.Body.Position.ToXnaV2() * UNITS_PER_METER);
            contact.Friction = 0.02f;
        } else if (StickyBlocks.Contains(tileType)) {
            if (_timeSinceLastCollision > timeBeforeSounds) {
                SoundEngine.PlaySound(SoundID.NPCDeath1.WithVolumeScale(absVel * volScale), sender.Body.Position.ToXnaV2() * UNITS_PER_METER);
                _timeSinceLastCollision = 0;
            }

            contact.Friction = 0.98f;
        } else if (tileType == TileID.SillyBalloonGreen || tileType == TileID.SillyBalloonPink || tileType == TileID.SillyBalloonPurple) {
            contact.Restitution = 0.925f;
        }
        else {
            if (!AllTileLists.Any(x => x.Contains(tileType))) {
                if (_timeSinceLastCollision > timeBeforeSounds) {
                    SoundEngine.PlaySound(SoundID.Dig.WithVolumeScale(absVel * volScale), sender.Body.Position.ToXnaV2() * UNITS_PER_METER);
                    _timeSinceLastCollision = 0;
                }
            }
        }
        if (PhysicsTags.IsPlayer(otherTags)) {
            // horrid but idc atp
            var player = ((PhysicsTags)other.Body.Tag).GetTags()[1] as Player;
            if (_timeSinceLastCollision > timeBeforeSounds) {
                if (PhysicsTags.IsItem(senderTags)) {
                    if (BodyLifetimes.Count > DynamicBodies[sender.Body].BodyId) {
                        var lifeTime = BodyLifetimes[DynamicBodies[sender.Body].BodyId];
                        if (lifeTime < 10) {
                            return false;
                        }
                        if (lifeTime > 60) {
                            var item = senderTags.GetTags()[1] as Item;
                            if (Main.LocalPlayer.CanAcceptItemIntoInventory(item)) {
                                Item.NewItem(Entity.GetSource_NaturalSpawn(), player.Center, item);
                                QueueForDeletion(sender.Body);
                                BodyLifetimes.RemoveAt(DynamicBodies[sender.Body].BodyId);
                            }
                        }
                    }
                }
                SoundEngine.PlaySound(SoundID.PlayerHit.WithVolumeScale(absVel * volScale * 2), Main.LocalPlayer.Center);
                _timeSinceLastCollision = 0;
            }
        }
        if (PhysicsTags.IsNPC(otherTags)) {
            var speed = sender.Body.LinearVelocity.Length();
            if (speed > 0.5f) {
                var npc = otherTags.GetTags()[1] as NPC;
                var item = senderTags.GetTags()[1] as Item;
                npc.StrikeNPC(new NPC.HitInfo() {
                    HitDirection = sender.Body.Position.X < npc.Center.X ? -1 : 1,
                    Knockback = speed * 2,
                    Crit = Main.rand.NextBool(2),
                    Damage = (int)(item.damage * (speed - 0.5f))
                });
            }
        }
        return true;
    }
    private static bool debug;
    private static BasicEffect effect;
    public override void PostDrawTiles() {
        Main.spriteBatch.Begin(default, default, SamplerState.PointClamp, default, default, default, Main.GameViewMatrix.TransformationMatrix);
        var wp = Mod.Assets.Request<Texture2D>("Assets/white_pixel", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        for (int i = 0; i < DynamicBodies.Count; i++) {
            var b = DynamicBodies.ElementAt(i);
            var color = Lighting.GetSubLight(b.Key.Position.ToXnaV2() * UNITS_PER_METER);
            var offset = (b.Value.Center * 2 * new Vector2(32f / b.Value.Texture.Width, 32f / b.Value.Texture.Height)) + Vector2.One;
            if (debug) {
                Vector2 pos = b.Key.Position.ToXnaV2() * UNITS_PER_METER + offset;
                var gd = Main.instance.GraphicsDevice;
                var vp = gd.Viewport;
                effect ??= new BasicEffect(gd) {
                    VertexColorEnabled = true,
                    TextureEnabled = true
                };
                effect.World = Matrix.Identity;//Matrix.CreateRotationZ(b.Rotation);//Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0)); // allow using world positions directly
                effect.View = Main.GameViewMatrix.TransformationMatrix; // zoom and other world transforms 
                effect.Projection = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, -1f, 1000); // screen to normalized coords
                VertexPositionColorTexture[] vertices = EntityConvexSystem.ItemIDToConvexHull[b.Value.Id]
                    .Select(pixel => new VertexPositionColorTexture(new Vector3(pos + pixel.RotatedBy(b.Key.Rotation, default), 0), Color.White, Vector2.Zero))
                    .Select(t => t with { Position = t.Position + new Vector3(-Main.screenPosition, 0) })
                    .ToArray();
                vertices = vertices.Append(vertices[0]).ToArray();
                gd.Textures[0] = wp.Value;
                gd.RasterizerState = RasterizerState.CullNone;
                effect.CurrentTechnique.Passes[0].Apply();

                gd.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, vertices.Length - 1);
            }
            Main.spriteBatch.Draw(b.Value.Texture,
                b.Key.Position.ToXnaV2() * UNITS_PER_METER - Main.screenPosition + offset,
                Main.itemAnimations[b.Value.Id] != null ? new Rectangle(0, b.Value.Texture.Height / Main.itemAnimations[b.Value.Id].FrameCount * Main.itemAnimations[b.Value.Id].Frame, b.Value.Texture.Width, b.Value.Texture.Height / Main.itemAnimations[b.Value.Id].FrameCount) : null, new Color(color), b.Key.Rotation,
                Vector2.Zero, Vector2.One, default, default);
        }
        foreach (var b in World.BodyList) {
            var tags = (PhysicsTags)b.Tag;
            var tagArray = tags.GetTags();
            // nah but why does this return false for something with the DynamicPhysicsBody tag?
            if (!tagArray.Contains(PhysicsTags.DynamicPhysicsBody)) {
                if (debug) {
                    if (PhysicsTags.IsPlayer(tags)) {
                        Main.spriteBatch.Draw(wp.Value, b.Position.ToXnaV2() * UNITS_PER_METER - Main.screenPosition, null, Color.Blue * 0.25f,
                            b.Rotation,
                            Vector2.Zero,
                            Vector2.One * Main.LocalPlayer.Size, default, default);
                    }
                    else if (PhysicsTags.IsNPC(tags)) {
                        var npc = tags.GetTags()[1] as NPC;
                        Main.spriteBatch.Draw(wp.Value, b.Position.ToXnaV2() * UNITS_PER_METER - Main.screenPosition, null, Color.Lime * 0.25f,
                            b.Rotation,
                            Vector2.Zero,
                            Vector2.One * npc.Size, default, default);
                    }
                    else if (PhysicsTags.IsTriangle(tags)) {
                        Vector2 pos = b.Position.ToXnaV2() * UNITS_PER_METER;
                        //Main.NewText($"{World.BodyList.IndexOf(b)}: pPhys: {b.Position} | pWorld: {pos}");
                        var gd = Main.instance.GraphicsDevice;
                        var vp = gd.Viewport;
                        effect ??= new BasicEffect(gd) {
                            VertexColorEnabled = true,
                            TextureEnabled = true
                        };
                        effect.World = Matrix.Identity;
                        effect.View = Main.GameViewMatrix.TransformationMatrix; // zoom and other world transforms 
                        effect.Projection = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, -1f, 1000); // screen to normalized coords
                        var verts = new phys.Vector2[] { (phys.Vector2)tagArray[1] * UNITS_PER_METER, (phys.Vector2)tagArray[2] * UNITS_PER_METER, GetCorner((phys.Vector2)tagArray[1], (phys.Vector2)tagArray[2]) * UNITS_PER_METER };
                        //Array.ForEach(verts, x => Main.NewText(x));
                        //Array.ForEach(verts, x => Main.NewText(x / UNITS_PER_METER));
                        VertexPositionColorTexture[] vertices = verts
                            .Select(p => p.ToXnaV2())
                            .Select(corner => {
                                var vec = new Vector3(corner - Main.screenPosition, 0);
                                return new VertexPositionColorTexture(vec, Color.Orange, Vector2.Zero);
                                })
                            .ToArray();
                        vertices = vertices.Append(vertices[0]).ToArray();
                        gd.Textures[0] = wp.Value;
                        gd.RasterizerState = RasterizerState.CullNone;
                        effect.CurrentTechnique.Passes[0].Apply();

                        gd.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, vertices.Length - 1);
                    }
                    else {
                        Main.spriteBatch.Draw(wp.Value, b.Position.ToXnaV2() * UNITS_PER_METER - Main.screenPosition, null, Color.Orange * 0.25f,
                            b.Rotation,
                            Vector2.Zero,
                            Vector2.One * 16, default, default);
                    }
                }
            }
        }
        //Main.spriteBatch.Draw(wp.Value, new Rectangle(TileBodyHandler.PlayerCheckAround.X - (int)Main.screenPosition.X, TileBodyHandler.PlayerCheckAround.Y - (int)Main.screenPosition.Y, TileBodyHandler.PlayerCheckAround.Width, TileBodyHandler.PlayerCheckAround.Height), Color.White * 0.2f);
        Main.spriteBatch.End();
    }
    public static void QueueForDeletion(Body body) {
        Main.QueueMainThreadAction(() => {
            World.Remove(body);
            DynamicBodies.Remove(body);
        });
    }
    public static phys.Vector2 GetCorner(Vector2 hypStart, Vector2 hypEnd, bool cornerLower = true) => new(hypEnd.X - (hypEnd.X - hypStart.X), hypStart.Y + (hypEnd.Y - hypStart.Y));
    public static phys.Vector2 GetCorner(phys.Vector2 hypStart, phys.Vector2 hypEnd) => new(hypEnd.X - (hypEnd.X - hypStart.X), hypStart.Y + (hypEnd.Y - hypStart.Y));
    public static List<int> GrassBlocks { get; private set; } = new()
{
                TileID.Grass,
                TileID.BlueMoss,
                TileID.BrownMoss,
                TileID.GreenMoss,
                TileID.LavaMoss,
                TileID.LongMoss,
                TileID.PurpleMoss,
                TileID.RedMoss,
                TileID.JungleGrass,
                TileID.CorruptGrass,
                TileID.CrimsonGrass,
                TileID.HallowedGrass,
                TileID.MushroomGrass,
                TileID.ArgonMoss,
                TileID.XenonMoss,
                TileID.KryptonMoss,
                TileID.GolfGrass,
                TileID.GolfGrassHallowed
        };
    public static List<int> DirtBlocks { get; private set; } = new()
    {
            TileID.Dirt,
            TileID.ClayBlock,
            TileID.Silt,
            TileID.Slush,
        };
    public static List<int> StoneBlocks { get; private set; } = new()
    {
                TileID.Asphalt,
                TileID.Stone,
                TileID.ActiveStoneBlock,
                TileID.Diamond,
                TileID.Ruby,
                TileID.Topaz,
                TileID.Sapphire,
                TileID.Amethyst,
                TileID.Emerald,
                TileID.Ebonstone,
                TileID.Crimstone,
                TileID.CrimsonSandstone,
                TileID.CorruptHardenedSand,
                TileID.CorruptSandstone,
                TileID.CrimsonHardenedSand,
                TileID.HardenedSand,
                TileID.Sandstone,
                TileID.HallowSandstone,
                TileID.HallowHardenedSand,
                TileID.Sunplate,
                TileID.Obsidian,
                TileID.Pearlstone,
                TileID.Mudstone,
                TileID.MythrilAnvil,
                TileID.Adamantite,
                TileID.Mythril,
                TileID.Cobalt,
                TileID.Titanium,
                TileID.Titanstone,
                TileID.Palladium,
                TileID.LunarOre,
                TileID.Copper,
                TileID.Tin,
                TileID.Silver,
                TileID.Tungsten,
                TileID.Iron,
                TileID.Lead,
                TileID.Gold,
                TileID.Platinum,
                TileID.Hellstone,
                TileID.FossilOre,
                TileID.DesertFossil,
                TileID.ShellPile,
                TileID.Meteorite,
                TileID.Demonite,
                TileID.Chlorophyte
        };
    public static List<int> SandBlocks { get; private set; } = new()
    {
                TileID.Sand,
                TileID.Ebonsand,
                TileID.Crimsand,
                TileID.Ash,
                TileID.Pearlsand
        };
    public static List<int> SnowBlocks { get; private set; } = new()
    {
            TileID.SnowBlock,
            TileID.SnowCloud,
            TileID.RainCloud,
            TileID.Cloud,
        };
    public static List<int> IceBlocks { get; private set; } = new()
    {
            TileID.IceBlock,
            TileID.BreakableIce,
            TileID.HallowedIce,
            TileID.CorruptIce,
            TileID.FleshIce,
            TileID.MagicalIceBlock,
        };
    public static List<int> SmoothStones { get; private set; } = new()
    {
            TileID.Titanstone,
            TileID.Sunplate,
            TileID.PearlstoneBrick,
            TileID.IridescentBrick,
            TileID.AdamantiteBeam,
            TileID.GraniteColumn,
            TileID.MarbleColumn,
            TileID.PalladiumColumn,
            TileID.SandstoneColumn,
            TileID.SandStoneSlab,
            TileID.SmoothSandstone,
            TileID.ObsidianBrick,
            TileID.Asphalt,
            TileID.StoneSlab,
            TileID.AccentSlab,
            TileID.SandStoneSlab,
            TileID.Coralstone,
            TileID.CrimstoneBrick,
            TileID.EbonstoneBrick,
            TileID.CrackedPinkDungeonBrick,
            TileID.CrackedGreenDungeonBrick,
            TileID.CrackedBlueDungeonBrick,
            TileID.SandstoneBrick,
            TileID.RedMossBrick,
            TileID.RedBrick,
            TileID.RainbowBrick,
            TileID.PurpleMossBrick,
            TileID.ArgonMossBrick,
            TileID.XenonMossBrick,
            TileID.KryptonMossBrick,
            TileID.LavaMossBrick,
            TileID.GreenMossBrick,
            TileID.BlueMossBrick,
            TileID.BlueDungeonBrick,
            TileID.GreenDungeonBrick,
            TileID.PinkDungeonBrick,
            TileID.SnowBrick,
            TileID.SolarBrick,
            TileID.StardustBrick,
            TileID.TungstenBrick,
            TileID.VortexBrick,
            TileID.NebulaBrick,
            TileID.GrayBrick,
            TileID.GrayStucco,
            TileID.GreenStucco,
            TileID.RedStucco,
            TileID.YellowStucco,
            TileID.MarbleBlock,
            TileID.GraniteBlock,
            TileID.LihzahrdBrick,
            TileID.MeteoriteBrick,
            TileID.IceBrick,
            TileID.Teleporter,
            TileID.HellstoneBrick
        };
    public static List<int> MetalBlocks { get; private set; } = new()
    {
            TileID.MetalBars,
            TileID.Anvils,
            TileID.MythrilAnvil,
            TileID.MythrilBrick,
            TileID.CobaltBrick,
            TileID.LunarBrick,
            TileID.IronBrick,
            TileID.GoldBrick,
            TileID.PlatinumBrick,
            TileID.CopperBrick,
            TileID.TinBrick,
            TileID.SilverBrick,
            TileID.DemoniteBrick,
            TileID.CrimtaneBrick,
            TileID.LeadBrick,
            TileID.MartianConduitPlating,
            TileID.TinPlating,
            TileID.ShroomitePlating,
            TileID.CopperPlating,
            TileID.TrapdoorClosed
        };
    public static List<int> MarblesGranites { get; private set; } = new()
    {
            TileID.Granite,
            TileID.GraniteBlock,
            TileID.GraniteColumn,
            TileID.Marble,
            TileID.MarbleBlock,
            TileID.MarbleColumn,
        };
    public static List<int> GlassBlocks { get; private set; } = new()
    {
            TileID.Glass,
            TileID.BlueStarryGlassBlock,
            TileID.GoldStarryGlassBlock,
            TileID.Confetti,
            TileID.ConfettiBlack,
            TileID.Waterfall,
            TileID.Lavafall,
            TileID.Honeyfall,
        };
    public static List<int> LeafBlocks { get; private set; } = new()
    {
            TileID.LivingMahoganyLeaves,
            TileID.LeafBlock,
    };
    public static List<int> GemBlocks { get; private set; } = new()
    {
        TileID.AmberGemspark,
        TileID.AmethystGemspark,
        TileID.DiamondGemspark,
        TileID.EmeraldGemspark,
        TileID.RubyGemspark,
        TileID.SapphireGemspark,
        TileID.TopazGemspark,
    };
    public static List<int> StickyBlocks { get; private set; } = new()
    {
            TileID.Mud,
            TileID.SlimeBlock,
            TileID.PinkSlimeBlock,
            TileID.FrozenSlimeBlock,
            TileID.BeeHive,
            TileID.Hive,
            TileID.HoneyBlock,
            TileID.CrispyHoneyBlock,
            TileID.FleshBlock
    };
    // VERY IMPORTANT TO ADD ALL ABOVE LISTS HERE.
    internal static List<List<int>> AllTileLists = new()
    {
            GrassBlocks,
            DirtBlocks,
            IceBlocks,
            LeafBlocks,
            SmoothStones,
            SnowBlocks,
            SandBlocks,
            StoneBlocks,
            MarblesGranites,
            MetalBlocks,
            GlassBlocks,
            StickyBlocks,
            GemBlocks
    };
    public static List<List<int>> LowFrictionSurfaces = new() {
        GrassBlocks, DirtBlocks, StoneBlocks, SandBlocks,
        SnowBlocks, SmoothStones, MetalBlocks, MarblesGranites,
        GlassBlocks, LeafBlocks, GemBlocks
    };
}
