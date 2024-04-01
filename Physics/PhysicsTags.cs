using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using phys = nkast.Aether.Physics2D.Common;

namespace Rigidity.Physics;

public readonly struct PhysicsTags {
    public const string DYN_PHYS_BODY_STR = "dynamic_body";
    public const string TILE_BODY_RECT_STR = "tile_physics_rect";
    public const string TILE_BODY_TRI_STR = "tile_physics_tri";
    public const string PLR_BDY_STR = "plr_bdy";
    public static readonly PhysicsTags DynamicPhysicsBody = new(DYN_PHYS_BODY_STR);
    public static readonly PhysicsTags TileBodyRectangular = new("tile_physics_rect");
    public static readonly PhysicsTags TileBodyTriangular = new("tile_physics_tri");
    public static readonly PhysicsTags PlayerBody = new("plr_bdy");
#nullable enable
    private PhysicsTags(params object?[] tags) => _tags = tags;
    private readonly object?[] _tags;
    public readonly object?[] GetTags() => _tags;
#nullable disable
    public static PhysicsTags Rectangle(float x, float y, float width, float height) => new(TileBodyRectangular, x, y, width, height);
    public static bool IsRectangle(PhysicsTags tags) {
        return tags._tags[0].Equals(TileBodyRectangular) && tags._tags[1] is float && tags._tags[2] is float && tags._tags[3] is float && tags._tags[4] is float;
    }
    public static PhysicsTags Triangle(phys.Vector2 hypStart, phys.Vector2 hypEnd) => new(TileBodyRectangular, hypStart, hypEnd);
    public static bool IsTriangle(PhysicsTags tags) {
        return tags._tags[0].Equals(TileBodyTriangular) && tags._tags[1] is phys.Vector2 && tags._tags[2] is phys.Vector2;
    }
    public override string ToString() {
        StringBuilder s = new();
        s.Append("{ ");
        Array.ForEach(_tags, x => s.Append(x.ToString() + " "));
        s.Append(" }");
        return s.ToString();
    }
}
