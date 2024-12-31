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
                x/=3;
                
                float y = (float)random.NextDouble() * 2.0f - 1.0f;
                y/=3;

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

                Entities.Add(new Entity<EntityRenderer>(entityVertexData) { Id = i, Position = new System.Numerics.Vector2(x, y), Scale=0.005f, SpinSpeed = SpinSpeed, Velocity = new System.Numerics.Vector2(vx, vy), Colour = new System.Numerics.Vector4(r, g, b, 1.0f) });
            }
        }

        class CollisionResult
        {
            public int Entity1 { get; set; }
            public int Entity2 { get; set; }
            public bool Near { get; set; }
            public float Distance { get; set; }
            public bool Collided { get; set; }
            public Vector2 CollisionPoint { get; set; }
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

                bool near = entityX.IsNear(entityY, 50, out float distance);

                if (!near || distance > 1.0f)
                {
                    collisionResults[i] = new CollisionResult { Entity1 = x, Entity2 = y, Near = near, Collided = false, Distance = distance, CollisionPoint = Vector2.Zero };
                    return;
                }

                bool collided = entityX.CollidesWith(entityY, out Vector2 collisionPoint);

                collisionResults[i] = new CollisionResult { Entity1 = x, Entity2 = y, Near = near, Collided = collided, Distance = distance, CollisionPoint = collisionPoint };
            });

            Parallel.For(0, totalCollisions, i => {
                var collisionResult = collisionResults[i];

                if (collisionResult == null)
                {
                    return;
                }

                var entityX = Entities[collisionResult.Entity1];
                var entityY = Entities[collisionResult.Entity2];


                if (collisionResult.Collided && !entityX.LastCollidedWith.Contains(entityY.Id) && !entityY.LastCollidedWith.Contains(entityX.Id)){
                    // Calculate the bounce
                    Vector2 normal = Vector2.Normalize(entityX.Position - entityY.Position);
                    Vector2 relativeVelocity = entityX.Velocity - entityY.Velocity;

                    float relativeSpeed = Math.Abs(Vector2.Dot(relativeVelocity, normal));
                    if (relativeSpeed < 0.001f)
                    {
                        Vector2 relativeMomentum = entityX.Velocity / entityX.Mass + entityY.Velocity / entityY.Mass;
                        relativeMomentum /= 2;

                        entityX.Scale = (float)Math.Sqrt(entityX.Scale * entityX.Scale + entityY.Scale * entityY.Scale);
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

                    float restitution = 0.5f; // Perfectly elastic collision
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

                    entityX.HasCollidedWith.Add(entityY.Id);
                    entityY.HasCollidedWith.Add(entityX.Id);
                }

                if (collisionResult.Near){
                    // Apply gravitational pull

                    Vector2 direction = entityY.Position - entityX.Position;
                    float distanceSquared = direction.LengthSquared();
                    float gravitationalConstant = 0.0001f;
                    float relativeSize = entityX.Scale / entityY.Scale;
                    Vector2 gravitationalForce = gravitationalConstant * direction / distanceSquared;
                    entityX.Velocity += gravitationalForce * dtime * relativeSize;
                    entityY.Velocity -= gravitationalForce * dtime * relativeSize;
                }
            });

            Parallel.For(0, Entities.Count, i => {
                var entity = Entities[i];

                entity.Position += entity.Velocity * dtime;
                entity.Rotation += entity.SpinSpeed * dtime;

                if (entity.Position.X > 1.0f || entity.Position.X < -1.0f)
                {
                    entity.Velocity = new Vector2(-entity.Velocity.X, entity.Velocity.Y) / 2;
                }

                if (entity.Position.Y > 1.0f || entity.Position.Y < -1.0f)
                {
                    entity.Velocity = new Vector2(entity.Velocity.X, -entity.Velocity.Y) / 2;
                }

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

            // for (int i = 0;i< Entities.Count; i++)
            // {
            //     var entity = Entities[i];
            //     entity.LastCollidedWith = entity.HasCollidedWith;
            //     entity.HasCollidedWith.Clear();
            // }

            // // Check for collisions and gravitational pull
            // for (int i = 0; i < Entities.Count; i++)
            // {
            //     var entity = Entities[i];
                
            //     if (entity.MarkedForDeletion)
            //     {
            //         Entities.RemoveAt(i);
            //         i--;
            //         continue;
            //     }

            //     for (int j = 0; j < Entities.Count; j++)
            //     {
            //         var otherEntity = Entities[j];
            //         if (entity.Id != otherEntity.Id)
            //         {
            //                 if (entity.IsNear(otherEntity, 50, out float distance))
            //                 {
            //                     Vector2 direction = otherEntity.Position - entity.Position;
            //                     float distanceSquared = direction.LengthSquared();
            //                     float gravitationalConstant = 0.00001f;
            //                     float relativeSize = entity.Scale / otherEntity.Scale;
            //                     Vector2 gravitationalForce = gravitationalConstant * direction / distanceSquared;
            //                     entity.Velocity += gravitationalForce * dtime * relativeSize;

            //                     if (distance < 1.0f && entity.CollidesWith(otherEntity, out Vector2 collisionPoint))
            //                     {
            //                         if (entity.LastCollidedWith.Contains(otherEntity.Id) && entity.HasCollidedWith.Contains(otherEntity.Id))
            //                         {
            //                             entity.HasCollidedWith.Add(otherEntity.Id);
            //                             continue;
            //                         }

            //                         // Calculate the bounce
            //                         Vector2 normal = Vector2.Normalize(entity.Position - otherEntity.Position);
            //                         Vector2 relativeVelocity = entity.Velocity - otherEntity.Velocity;

            //                         float relativeSpeed = Math.Abs(Vector2.Dot(relativeVelocity, normal));
            //                         if (relativeSpeed < 0.005f)
            //                         {
            //                             Vector2 relativeMomentum = entity.Velocity / entity.Mass + otherEntity.Velocity / otherEntity.Mass;
            //                             relativeMomentum /= 2;

            //                             entity.Scale = (float)Math.Sqrt(entity.Scale * entity.Scale + otherEntity.Scale * otherEntity.Scale);
            //                             entity.Mass = entity.Mass + otherEntity.Mass;
            //                             entity.SpinSpeed = (entity.SpinSpeed + otherEntity.SpinSpeed) / 2;
            //                             entity.Colour = entity.Colour + otherEntity.Colour / 2;
            //                             entity.Velocity = relativeMomentum * entity.Mass / (entity.Mass + otherEntity.Mass);

            //                             otherEntity.MarkedForDeletion = true;
            //                             continue;
            //                         }

            //                         float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

            //                         if (velocityAlongNormal > 0)
            //                             continue;

            //                         float restitution = 0.1f; // Perfectly elastic collision
            //                         float impulseScalar = -(1 + restitution) * velocityAlongNormal;
            //                         impulseScalar /= 1 / entity.Mass + 1 / otherEntity.Mass;

            //                         Vector2 impulse = impulseScalar * normal;

            //                         // Apply impulse based on relative positions
            //                         var angleBetween = Vector2.Dot(entity.Position - otherEntity.Position, impulse);
            //                         if (angleBetween > Math.PI / 2)
            //                         {
            //                             entity.Velocity -= impulse / entity.Mass;
            //                             otherEntity.Velocity += impulse / otherEntity.Mass;
            //                         }
            //                         else
            //                         {
            //                             entity.Velocity += impulse / entity.Mass;
            //                             otherEntity.Velocity -= impulse / otherEntity.Mass;
            //                         }

            //                         // Calculate the effect of SpinSpeed
            //                         float spinEffect = (entity.SpinSpeed - otherEntity.SpinSpeed) * 0.1f;
            //                         entity.Velocity += new Vector2(-normal.Y, normal.X) * spinEffect;
            //                         otherEntity.Velocity -= new Vector2(-normal.Y, normal.X) * spinEffect;

            //                         entity.HasCollidedWith.Add(otherEntity.Id);
            //                         otherEntity.HasCollidedWith.Add(entity.Id);
            //                     }
            //                 }
            //             }
            //     }
            // }

            // for (int i = 0; i < Entities.Count; i++)
            // {
            //     var entity = Entities[i];

            //     entity.Position += entity.Velocity * dtime;
            //     entity.Rotation += entity.SpinSpeed * dtime;

            //     // if (entity.Position.X > 1.0f || entity.Position.X < -1.0f)
            //     // {
            //     //     entity.Velocity = new Vector2(-entity.Velocity.X, entity.Velocity.Y);
            //     // }

            //     // if (entity.Position.Y > 1.0f || entity.Position.Y < -1.0f)
            //     // {
            //     //     entity.Velocity = new Vector2(entity.Velocity.X, -entity.Velocity.Y);
            //     // }

            //     entity.Velocity *= 0.9999f;
            // }
        //}
    }
}