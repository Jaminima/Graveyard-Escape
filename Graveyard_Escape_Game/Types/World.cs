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

            // Add some entities
            for (int i = 0; i < 1000; i++)
            {
                float x = (float)random.NextDouble() * 2.0f - 1.0f;
                x/=2;
                
                float y = (float)random.NextDouble() * 2.0f - 1.0f;
                y/=2;

                float vx = (float)random.NextDouble() * 0.02f - 0.01f;
                float vy = (float)random.NextDouble() * 0.02f - 0.01f;

                if (random.Next(0, 10) > 8){
                    vx *= 10;
                    vy *= 10;
                }

                float r = random.Next(0, 255) / 255.0f;
                float g = random.Next(0, 255) / 255.0f;
                float b = random.Next(0, 255) / 255.0f;

                float SpinSpeed = (float)random.NextDouble() * 1.0f - 0.50f;

                Entities.Add(new Entity<EntityRenderer>() { Id = i, Position = new System.Numerics.Vector2(x, y), Radius=0.001f, SpinSpeed = SpinSpeed, Velocity = new System.Numerics.Vector2(vx, vy), Colour = new System.Numerics.Vector4(r, g, b, 1.0f) });
            }
        }

        class CollisionResult
        {
            public int Entity1 { get; set; }
            public int Entity2 { get; set; }
            public bool Near { get; set; }
            public float Distance { get; set; }
            public bool Collided { get; set; }
        }

        public void Update(float dtime)
        {
            int totalCollisions = ((Entities.Count * Entities.Count) / 2) - Entities.Count; 
            CollisionResult[] collisionResults = new CollisionResult[totalCollisions];

            for (int i = 0; i<Entities.Count; i++)
            {
                var entity = Entities[i];
                entity.LastCollidedWith = entity.HasCollidedWith;
                entity.HasCollidedWith.Clear();
            }

            Parallel.For(0, totalCollisions, i => {
                int y = (int)((-1 + Math.Sqrt(1 + 8 * i)) / 2);
                int x = i - (y * (y + 1)) / 2;

                if (x == y)
                {
                    return;
                }

                var entityX = Entities[x];
                var entityY = Entities[y];

                bool collided = entityX.CollidesWith(entityY, out float collisionDistance);
                float gravityDistance =collisionDistance / (entityX.Mass + entityY.Mass);
                bool near = collisionDistance < 1.0f;

                Vector2 direction = entityY.Position - entityX.Position;
                float distanceSquared = direction.LengthSquared();
                float gravitationalConstant = 0.0001f;
                float relativeSize = 1 / (entityY.Radius + entityX.Radius);
                Vector2 gravitationalForce = gravitationalConstant * direction / distanceSquared;
                entityX.Velocity += gravitationalForce * (dtime * entityX.Radius * relativeSize);
                entityY.Velocity -= gravitationalForce * (dtime * entityY.Radius * relativeSize);

                collisionResults[i] = new CollisionResult { Entity1 = x, Entity2 = y, Near = near, Collided = collided, Distance = collisionDistance };
            });

            var collisons = collisionResults.Where(c => c != null && c.Collided).ToList();
            Parallel.For(0, collisons.Count, i => {
                var collisionResult = collisons[i];

                var entityX = Entities[collisionResult.Entity1];
                var entityY = Entities[collisionResult.Entity2];

                // Calculate the bounce
                Vector2 normal = Vector2.Normalize(entityX.Position - entityY.Position);
                Vector2 relativeVelocity = entityX.Velocity - entityY.Velocity;

                float relativeSpeed = Math.Abs(Vector2.Dot(relativeVelocity, normal));
                if (relativeSpeed < 0.001f)
                {
                    Vector2 relativeMomentum = entityX.Velocity / entityX.Mass + entityY.Velocity / entityY.Mass;
                    relativeMomentum /= 2;

                    entityX.Radius = (float)Math.Sqrt(entityX.Radius * entityX.Radius + entityY.Radius * entityY.Radius);
                    entityX.Mass = entityX.Mass + entityY.Mass;
                    entityX.SpinSpeed = (entityX.SpinSpeed + entityY.SpinSpeed) / 2;
                    entityX.Colour = entityX.Colour + entityY.Colour / 2;
                    entityX.Velocity = relativeMomentum * entityX.Mass / (entityX.Mass + entityY.Mass);

                    entityY.MarkedForDeletion = true;
                    return;
                }

                float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

                if (velocityAlongNormal > 0)
                    return;

                float restitution = 0.1f; // Perfectly elastic collision
                float impulseScalar = -(1 + restitution) * velocityAlongNormal;
                impulseScalar /= 1 / entityX.Mass + 1 / entityY.Mass;

                Vector2 impulse = impulseScalar * normal;

                // Apply impulse based on relative positions
                var angleBetween = Vector2.Dot(entityX.Position - entityY.Position, impulse);
                if (angleBetween > Math.PI / 2)
                {
                    entityX.Velocity -= impulse / entityX.Mass;
                    entityY.Velocity += impulse / entityY.Mass;
                }
                else
                {
                    entityX.Velocity += impulse / entityX.Mass;
                    entityY.Velocity -= impulse / entityY.Mass;
                }

                // Calculate the effect of SpinSpeed
                float spinEffect = (entityX.SpinSpeed - entityY.SpinSpeed) * 0.1f;
                entityX.Velocity += new Vector2(-normal.Y, normal.X) * spinEffect;
                entityY.Velocity -= new Vector2(-normal.Y, normal.X) * spinEffect;

                // Move the entities apart to avoid double collision
                float overlap = (entityX.Radius + entityY.Radius) - collisionResult.Distance;
                entityX.Position += normal * overlap / 2;
                entityY.Position -= normal * overlap / 2;

                entityX.HasCollidedWith.Add(entityY.Id);
                entityY.HasCollidedWith.Add(entityX.Id);
            });

            Parallel.For(0, Entities.Count, i => {
                var entity = Entities[i];

                entity.Position += entity.Velocity * dtime;

                // if (entity.Position.X > 1.0f || entity.Position.X < -1.0f)
                // {
                //     entity.Velocity = new Vector2(-entity.Velocity.X, entity.Velocity.Y) / 2.0f;
                // }

                // if (entity.Position.Y > 1.0f || entity.Position.Y < -1.0f)
                // {
                //     entity.Velocity = new Vector2(entity.Velocity.X, -entity.Velocity.Y) / 2.0f;
                // }

                entity.Velocity *= 0.9999f;
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