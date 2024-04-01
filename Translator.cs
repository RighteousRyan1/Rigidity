using Microsoft.Xna.Framework;
using Rigidity.Globals;
using System.Linq;
using phys = nkast.Aether.Physics2D.Common;

namespace Rigidity;

public static class Translator {
    public static Vector2 ToXnaV2(this phys.Vector2 v) => new(v.X, v.Y);
    public static phys.Vector2 ToPhysV2(this Vector2 v) => new(v.X, v.Y);

    public static Vector2 ToPhysicsFromTileCoordinates(this Vector2 tileCoords) => tileCoords * 16 / PhysicsSystem.UNITS_PER_METER;
    public static Vector2 ToPhysicsFromPixelCoordinates(this Vector2 pixelCoords) => pixelCoords / PhysicsSystem.UNITS_PER_METER;

    public static phys.Vector2 ToPhysicsFromTileCoordinates(this phys.Vector2 tileCoords) => tileCoords * 16 / PhysicsSystem.UNITS_PER_METER;
    public static phys.Vector2 ToPhysicsFromPixelCoordinates(this phys.Vector2 pixelCoords) => pixelCoords / PhysicsSystem.UNITS_PER_METER;
#nullable enable
    public static bool EqualsAny(this object? item, params object?[] items) {
        if (item is null || items is null)
            return false;
        return items.Any(item.Equals);
    }
#nullable disable
}
