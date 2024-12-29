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
            for (int i = 0; i < 1000; i++)
            {
                float x = (float)random.NextDouble() * 2.0f - 1.0f;
                float y = (float)random.NextDouble() * 2.0f - 1.0f;
                float vx = (float)random.NextDouble() * 0.02f - 0.01f;
                float vy = (float)random.NextDouble() * 0.02f - 0.01f;

                if (random.Next(0, 10) > 8){
                    vx *= 100;
                    vy *= 100;
                }

                float r = random.Next(0, 255) / 255.0f;
                float g = random.Next(0, 255) / 255.0f;
                float b = random.Next(0, 255) / 255.0f;

                float SpinSpeed = (float)random.NextDouble() * 1.0f - 0.50f;

                Entities.Add(new Entity<EntityRenderer>(entityVertexData) { Id = i, Position = new System.Numerics.Vector2(x, y), Scale=0.01f, SpinSpeed = SpinSpeed, Velocity = new System.Numerics.Vector2(vx, vy), Colour = new System.Numerics.Vector4(r, g, b, 1.0f) });
            }
        }

        public void Update(float dtime)
        {
            // Check for collisions
            for (int i = 0; i < Entities.Count; i++)
            {
                var entity = Entities[i];
                for (int j = 0; j < Entities.Count; j++)
                {
                    var otherEntity = Entities[j];
                    if (entity.Id != otherEntity.Id && entity.LastCollisionId != otherEntity.Id)
                    {
                        if (entity.IsNear(otherEntity, 1) && entity.CollidesWith(otherEntity, out Vector2 collisionPoint))
                        {
                            // Calculate the bounce
                            Vector2 normal = Vector2.Normalize(entity.Position - otherEntity.Position);
                            Vector2 relativeVelocity = entity.Velocity - otherEntity.Velocity;
                            float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

                            if (velocityAlongNormal > 0)
                                continue;

                            float restitution = 1.0f; // Perfectly elastic collision
                            float impulseScalar = -(1 + restitution) * velocityAlongNormal;
                            impulseScalar /= 1 / entity.Mass + 1 / otherEntity.Mass;

                            Vector2 impulse = impulseScalar * normal;

                            // Apply impulse based on relative positions
                            var angleBetween = Vector2.Dot(entity.Position - otherEntity.Position, impulse);
                            if (angleBetween > Math.PI / 2)
                            {
                                entity.Velocity -= impulse / entity.Mass;
                                otherEntity.Velocity += impulse / otherEntity.Mass;
                            }
                            else
                            {
                                entity.Velocity += impulse / entity.Mass;
                                otherEntity.Velocity -= impulse / otherEntity.Mass;
                            }

                            // Calculate the effect of SpinSpeed
                            float spinEffect = (entity.SpinSpeed - otherEntity.SpinSpeed) * 0.1f;
                            entity.Velocity += new Vector2(-normal.Y, normal.X) * spinEffect;
                            otherEntity.Velocity -= new Vector2(-normal.Y, normal.X) * spinEffect;

                            entity.LastCollisionId = otherEntity.Id;
                            otherEntity.LastCollisionId = entity.Id;
                        }
                    }
                }
            }

            for (int i = 0; i < Entities.Count; i++)
            {
                var entity = Entities[i];
                entity.Position += entity.Velocity * dtime;
                entity.Rotation += entity.SpinSpeed;
            }
        }
    }
}