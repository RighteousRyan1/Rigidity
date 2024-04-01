using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using nkast.Aether.Physics2D.Dynamics;
using Rigidity.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
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

    private Body _cb;
    public static float DefaultFriction = 0.5f;

    public static Texture2D TestTexture;
    public static Vector2 CenterOfCurrentHull;
    public override void Load() {
        On_Main.NewText_string_byte_byte_byte += ChangeGravity;
    }

    private void ChangeGravity(On_Main.orig_NewText_string_byte_byte_byte orig, string newText, byte R, byte G, byte B) {
        if (float.TryParse(newText, out float f)) {
            World.Gravity = new(0, f);
            orig("Changed gravity to " + f, R, G, B);
        }
    }

    public static phys.Vector2 GetCorner(Vector2 hypStart, Vector2 hypEnd) => new(hypEnd.X - (hypEnd.X - hypStart.X), hypStart.Y + (hypEnd.Y - hypStart.Y));
    public override void PostUpdateEverything() {
        World.Step(1);
        World.Gravity = new(0, 0.1f);

        if (Main.mouseRight && Main.mouseRightRelease && !Main.playerInventory) {
            if (_cb != null) {
                if (World.BodyList.Contains(_cb)) {
                    World.Remove(_cb);
                    _cb = null;
                }
            }
            var id = ItemID.IronPickaxe; //Main.rand.Next(ItemLoader.ItemCount);
            TestTexture = TextureAssets.Item[id].Value; //TextureAssets.Item[id].Value;
            var pos = Main.LocalPlayer.Top + (new Vector2(25, 0) * Main.LocalPlayer.direction);
            var vel = new phys.Vector2(Main.rand.NextFloat(2, 3) * Main.LocalPlayer.direction, Main.rand.NextFloat(-2, -4));
            var vertices = ItemConvexSystem.Vector2ArrayToVertices(ItemConvexSystem.ItemIDToConvexHull[id]);
            // call it before so i can step over and see the actual texture center
            CenterOfCurrentHull = ItemConvexSystem.Center(ItemConvexSystem.VerticesToVector2Array(vertices));
            for (int i = 0; i < vertices.Count; i++) {
                vertices[i] /= UNITS_PER_METER;
            }
            CenterOfCurrentHull /= UNITS_PER_METER;

            _cb = World.CreatePolygon(vertices, 1f, Main.MouseWorld.ToPhysV2() / UNITS_PER_METER, bodyType: BodyType.Dynamic);
            // _cb = World.CreateRectangle(TestTexture.Width / UNITS_PER_METER, TestTexture.Height / UNITS_PER_METER, 1f, pos.ToPhysV2() / UNITS_PER_METER, 0f, BodyType.Dynamic);
            //_cb.LinearVelocity = vel;
            _cb.AngularDamping = 0f;
            _cb.OnCollision += SetProperties;
            _cb.OnSeparation += ResetProperties;
            _cb.SleepingAllowed = false;
            _cb.Tag = PhysicsTags.DynamicPhysicsBody;

        }
        if (_cb != null) {
            if (Collision.DrownCollision(_cb.Position.ToXnaV2() * UNITS_PER_METER, 16, 16)) {
                _cb.LinearDamping = 0.25f;
            }
        }
    }

    private void ResetProperties(Fixture sender, Fixture other, nkast.Aether.Physics2D.Dynamics.Contacts.Contact contact) {
        //sender.Restitution = 0f;
        //sender.Friction = DefaultFriction;
    }

    private bool SetProperties(Fixture sender, Fixture other, nkast.Aether.Physics2D.Dynamics.Contacts.Contact contact) {
        var rounded = new Point(((int)(other.Body.Position.X * UNITS_PER_METER)) / 16, ((int)(other.Body.Position.Y * UNITS_PER_METER)) / 16);
        var tile = Framing.GetTileSafely(rounded);
        var tileType = tile.TileType;
        if (IceBlocks.Contains(tileType)) {
            contact.Friction = 0.01f;
        } else if (StickyBlocks.Contains(tileType)) {
            contact.Friction = 0.98f;
        } else if (tileType == TileID.SillyBalloonGreen || tileType == TileID.SillyBalloonPink || tileType == TileID.SillyBalloonPurple) {
            contact.Friction = 0.99f;
            contact.Restitution = 0.99f;
        }
        //}
        return true;
    }

    public override void PostDrawTiles() {
        Main.spriteBatch.Begin(default, default, default, default, default, default, Main.GameViewMatrix.TransformationMatrix);
        var wp = Mod.Assets.Request<Texture2D>("Assets/white_pixel", ReLogic.Content.AssetRequestMode.ImmediateLoad);
        foreach (var b in World.BodyList) {
            var tagArray = (PhysicsTags)b.Tag;
            var tags = tagArray.GetTags();
            if (tags.Contains(PhysicsTags.TileBodyTriangular))
                continue;
            // nah but why does this return false for something with the DynamicPhysicsBody tag?
            if (tags.Contains(PhysicsTags.DYN_PHYS_BODY_STR)) {
                var color = Lighting.GetSubLight(b.Position.ToXnaV2() * UNITS_PER_METER);
                Main.spriteBatch.Draw(TestTexture, 
                    b.Position.ToXnaV2() * UNITS_PER_METER - Main.screenPosition, 
                    null, new Color(color), b.Rotation,
                    Vector2.Zero/* + -CenterOfCurrentHull * UNITS_PER_METER*/, Vector2.One, default, default);
            }
            else if (tags.Contains(PhysicsTags.PLR_BDY_STR)) { 
                Main.spriteBatch.Draw(wp.Value, b.Position.ToXnaV2() * UNITS_PER_METER - Main.screenPosition, null, Color.Blue * 0.25f,
                    b.Rotation,
                    Vector2.Zero,
                    Vector2.One * Main.LocalPlayer.Size, default, default);
            }
            /*else {
                Main.spriteBatch.Draw(wp.Value, Vector2.Transform(b.Position.ToXnaV2() * UNITS_PER_METER - Main.screenPosition,
                    Main.GameViewMatrix.TransformationMatrix), null, Color.Orange * 0.25f,
                    b.Rotation,
                    Vector2.Zero,
                    Vector2.One * 16 * Main.GameZoomTarget, default, default);
            }*/
        }
        //Main.spriteBatch.Draw(wp.Value, new Rectangle(TileBodyHandler.PlayerCheckAround.X - (int)Main.screenPosition.X, TileBodyHandler.PlayerCheckAround.Y - (int)Main.screenPosition.Y, TileBodyHandler.PlayerCheckAround.Width, TileBodyHandler.PlayerCheckAround.Height), Color.White * 0.2f);
        Main.spriteBatch.End();
    }
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
}
