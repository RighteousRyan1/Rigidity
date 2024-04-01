using nkast.Aether.Physics2D.Dynamics;
using Rigidity.Globals;
using Rigidity.Physics;
using Terraria;
using Terraria.ModLoader;

namespace Rigidity;

public class PlayerRigidBody : ModPlayer {
    public Body PlayerPhysics;
    public override void PostUpdate() {
        if (PlayerPhysics == null) {
            PlayerPhysics = PhysicsSystem.World.CreateRectangle(
                Player.width / PhysicsSystem.UNITS_PER_METER, 
                Player.height / PhysicsSystem.UNITS_PER_METER, 2f, Player.position.ToPhysV2() / PhysicsSystem.UNITS_PER_METER);
            PlayerPhysics.BodyType = BodyType.Kinematic;
            PlayerPhysics.Tag = PhysicsTags.PlayerBody;
            PlayerPhysics.FixedRotation = true;
        } else {
            PlayerPhysics.LinearVelocity = Player.velocity.ToPhysV2() / PhysicsSystem.UNITS_PER_METER;
            if (Main.GameUpdateCount % 60 == 0) {
                PlayerPhysics.Position = Player.position.ToPhysV2() / PhysicsSystem.UNITS_PER_METER;
            }
        }
    }
}
