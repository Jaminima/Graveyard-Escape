using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

            float[] entityVertexData = Entity<EntityRenderer>.LoadVertexData("entity");

            // Add some entities
            for (int i = 0; i < 100; i++)
            {
                float x = (float)random.NextDouble() * 2.0f - 1.0f;
                float y = (float)random.NextDouble() * 2.0f - 1.0f;
                float vx = (float)random.NextDouble() * 0.02f - 0.01f;
                float vy = (float)random.NextDouble() * 0.02f - 0.01f;

                float r = random.Next(0, 255) / 255.0f;
                float g = random.Next(0, 255) / 255.0f;
                float b = random.Next(0, 255) / 255.0f;

                Entities.Add(new Entity<EntityRenderer>(entityVertexData) { Position = new System.Numerics.Vector2(x, y), Scale=0.05f, Velocity = new System.Numerics.Vector2(vx, vy), Colour = new System.Numerics.Vector4(r, g, b, 1.0f) });
            }
        }

        public void Update(float dtime)
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                var entity = Entities[i];
                entity.Position += entity.Velocity * dtime * 10;
                entity.Rotation += 0.1f * dtime;
            }

            // Check for collisions
            for (int i = 0; i < Entities.Count; i++)
            {
                var entity = Entities[i];
                for (int j = 0; j < Entities.Count; j++)
                {
                    var otherEntity = Entities[j];
                    if (entity != otherEntity)
                    {
                        if (entity.CollidesWith(otherEntity, out Vector2 collisionPoint))
                        {
                            entity.Velocity = -entity.Velocity;
                            entity.Position += entity.Velocity * dtime * 10;

                            otherEntity.Velocity = -otherEntity.Velocity;
                            otherEntity.Position += otherEntity.Velocity * dtime * 10;
                        }
                    }
                }
            }
        }
    }
}