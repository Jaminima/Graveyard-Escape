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
                new Entity() { Position = new System.Numerics.Vector2(0.0f, 0.0f), Velocity = new System.Numerics.Vector2(0.01f, 0.01f), Colour= new System.Numerics.Vector4(1.0f, 0.0f, 0.0f, 1.0f) },
                new Entity() { Position = new System.Numerics.Vector2(0.0f, 0.5f), Rotation=0.5f, Velocity = new System.Numerics.Vector2(-0.01f, -0.01f), Colour= new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f), Scale=0.1f },
            };
        }

        public void Update()
        {
            foreach (var entity in Entities)
            {
                //entity.Position += entity.Velocity;
                entity.Rotation += 0.01f;
            }
        }
    }
}