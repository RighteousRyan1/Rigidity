using nkast.Aether.Physics2D.Dynamics;
using Rigidity.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace Rigidity.Globals;

public class NPCBodyHandler : GlobalNPC {
    public override bool InstancePerEntity => true;
    public Body Physics;
    public override void PostAI(NPC npc) {
        if (Physics == null) {
            Physics = PhysicsSystem.World.CreateRectangle(
                npc.width / PhysicsSystem.UNITS_PER_METER,
                npc.height / PhysicsSystem.UNITS_PER_METER, 2f, npc.position.ToPhysV2() / PhysicsSystem.UNITS_PER_METER);
            Physics.BodyType = BodyType.Kinematic;
            Physics.Tag = PhysicsTags.NPC(npc);
            Physics.FixedRotation = true;
        }
        else {
            Physics.LinearVelocity = npc.velocity.ToPhysV2() / PhysicsSystem.UNITS_PER_METER;
            if (Main.GameUpdateCount % 60 == 0) {
                Physics.Position = npc.position.ToPhysV2() / PhysicsSystem.UNITS_PER_METER;
            }
        }
    }
    public override void OnKill(NPC npc) {
        PhysicsSystem.QueueForDeletion(Physics);
        Physics = null;
    }
}
