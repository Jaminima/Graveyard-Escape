using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Graveyard_Escape_Lib.Types
{
    public class World
    {
        public List<Entity> Entities { get; set; }

        public World()
        {
            Entities = new List<Entity>() {
                new Entity() { Position = new System.Numerics.Vector2(50.0f, 50.0f), Velocity = new System.Numerics.Vector2(0.01f, 0.01f) },
                new Entity() { Position = new System.Numerics.Vector2(100.0f, 100.0f), Velocity = new System.Numerics.Vector2(-0.01f, -0.01f) }
            };
        }

        public void Update()
        {
            foreach (var entity in Entities)
            {
                entity.Position += entity.Velocity;
            }
        }
    }
}