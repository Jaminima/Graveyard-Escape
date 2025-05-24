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
        public int maxEntityId = 0;

        public World()
        {
            Random random = new Random();

            Entities = new List<Entity<EntityRenderer>>();

            // Add some entities
            for (int i = 0; i < 2000; i++)
            {
                float x = (float)random.NextDouble() * 2.0f - 1.0f;
                x/=3;
                
                float y = (float)random.NextDouble() * 2.0f - 1.0f;
                y/=3;

                // Calculate clockwise velocity using trigonometric functions
                float angle = (float)Math.Atan2(y, x);
                float speed = 0.001f;
                float vx = speed * (float)Math.Sin(angle);
                float vy = -speed * (float)Math.Cos(angle);

                if (random.Next(0, 10) > 8)
                {
                    vx *= 10;
                    vy *= 10;
                }

                float r = random.Next(0, 255) / 255.0f;
                float g = random.Next(0, 255) / 255.0f;
                float b = random.Next(0, 255) / 255.0f;

                Entities.Add(new Entity<EntityRenderer>() { Id = maxEntityId, Position = new System.Numerics.Vector2(x, y), Radius=0.01f, Velocity = new System.Numerics.Vector2(vx, vy), Colour = new System.Numerics.Vector4(r, g, b, 1.0f) });
                maxEntityId++;
            }
        }

        public void Update(float dtime)
        {
            dtime = Math.Min(dtime, 0.1f);

            int n = Entities.Count;
            int totalPairs = n * (n - 1) / 2;

            Parallel.For(0, totalPairs, idx =>
            {
                // Map idx to unique (i, j) where i < j
                int i = (int)(n - 2 - Math.Floor(Math.Sqrt(-8 * idx + 4 * n * (n - 1) - 7) / 2.0 - 0.5));
                int j = idx + i + 1 - n * (n - 1) / 2 + (n - i) * ((n - i) - 1) / 2;

                var entityA = Entities[i];
                var entityB = Entities[j];

                // Your collision logic here

                bool collided = entityA.CollidesWith(entityB, out float distance);

                if (collided && distance < 0.9f)
                {
                    entityA.Velocity = (entityA.Velocity + entityB.Velocity) / 2;
                }
                else if (distance < 1.2f)
                {
                    //Repulse Apart
                    Vector2 direction = entityB.Position - entityA.Position;

                    Vector2 forceDirection = Vector2.Normalize(direction);

                    entityA.Velocity -= forceDirection * dtime;
                }
                else
                {
                    //Attract Together
                    Vector2 direction = entityB.Position - entityA.Position;

                    Vector2 forceDirection = Vector2.Normalize(direction);

                    entityA.Velocity += forceDirection * dtime / distance;
                }

            });

            Parallel.For(0, Entities.Count, i => {
                var entity = Entities[i];

                entity.Position += entity.Velocity * dtime;

                entity.Velocity *= 0.9995f;
            });

            for (int i = 0; i < Entities.Count; i++)
            {
                var entity = Entities[i];
                if (entity.MarkedForDeletion)
                {
                    Entities.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}