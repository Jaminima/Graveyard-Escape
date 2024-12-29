using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Graveyard_Escape_Game.Renderers;

namespace Graveyard_Escape_Lib.Types
{
    public class World
    {
        public List<Entity<EntityRenderer>> Entities { get; set; }

        public World()
        {
            Random random = new Random();

            Entities = new List<Entity<EntityRenderer>>();

            // Add some entities
            for (int i = 0; i < 1; i++)
            {
                float x = (float)random.NextDouble() * 2.0f - 1.0f;
                float y = (float)random.NextDouble() * 2.0f - 1.0f;
                float vx = (float)random.NextDouble() * 0.02f - 0.01f;
                float vy = (float)random.NextDouble() * 0.02f - 0.01f;

                float r = random.Next(0, 255) / 255.0f;
                float g = random.Next(0, 255) / 255.0f;
                float b = random.Next(0, 255) / 255.0f;

                Entities.Add(new Entity<EntityRenderer>() { Position = new System.Numerics.Vector2(x, y), Scale=0.5f, Velocity = new System.Numerics.Vector2(vx, vy), Colour = new System.Numerics.Vector4(r, g, b, 1.0f) });
            }
        }

        public void Update(float dtime)
        {
            foreach (var entity in Entities)
            {
                entity.Position += entity.Velocity * dtime;
                entity.Rotation += 0.1f * dtime;
            }
        }
    }
}