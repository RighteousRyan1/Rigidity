using nkast.Aether.Physics2D.Dynamics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace Rigidity.Globals;

public class ItemBodyHandler : GlobalItem {
    public override bool InstancePerEntity => true;

    public static Dictionary<int, Body> PhysicsBodies = new();

    private int _lifeTime;
    public override void PostUpdate(Item item) {
    }
    public override void Update(Item item, ref float gravity, ref float maxFallSpeed) {
        return;
        if (_lifeTime == 0) {
            PhysicsBodies.Add(item.whoAmI, PhysicsSystem.World.CreateRectangle(item.width / PhysicsSystem.UNITS_PER_METER, item.height / PhysicsSystem.UNITS_PER_METER, 1f, item.Center.ToPhysV2() / PhysicsSystem.UNITS_PER_METER, 0f, BodyType.Dynamic));
            PhysicsBodies[item.whoAmI].LinearVelocity = item.velocity.ToPhysV2();
        }
        else {
            if (PhysicsBodies.ContainsKey(item.whoAmI)) {
                item.position = PhysicsBodies[item.whoAmI].Position.ToXnaV2() / PhysicsSystem.UNITS_PER_METER;
                //item.velocity = PhysicsBodies[item.whoAmI].LinearVelocity.ToXnaV2();
            }
        }
        _lifeTime++;
    }
    public override bool OnPickup(Item item, Player player) {
        return true;
        _lifeTime = 0;
        if (PhysicsSystem.World.BodyList.Contains(PhysicsBodies[item.whoAmI]))
            PhysicsSystem.World.Remove(PhysicsBodies[item.whoAmI]);
        if (PhysicsBodies.ContainsKey(item.whoAmI))
            PhysicsBodies.Remove(item.whoAmI);
        return base.OnPickup(item, player);
    }
}
