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

                Entities.Add(new Entity<EntityRenderer>() { Id = maxEntityId, Position = new System.Numerics.Vector2(x, y), Radius=0.001f, Velocity = new System.Numerics.Vector2(vx, vy), Colour = new System.Numerics.Vector4(r, g, b, 1.0f) });
                maxEntityId++;
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
            var entityRegions = new Dictionary<(int, int), List<Entity<EntityRenderer>>>();
            float regionSize = 0.5f;

            var regionAverageGravities = new Dictionary<(int, int), Vector2>();

            foreach (var entity in Entities)
            {
                int regionX = (int)(entity.Position.X / regionSize);
                int regionY = (int)(entity.Position.Y / regionSize);
                var regionKey = (regionX, regionY);

                if (!entityRegions.ContainsKey(regionKey))
                {
                    entityRegions[regionKey] = new List<Entity<EntityRenderer>>();
                    regionAverageGravities[regionKey] = new Vector2(0, 0);
                }

                entityRegions[regionKey].Add(entity);

                regionAverageGravities[regionKey] += entity.Position;
            }
            
            var collisionResults = new List<CollisionResult>();
            
            Parallel.For(0, Entities.Count, i => {
                var entity = Entities[i];

                int regionX = (int)(entity.Position.X / regionSize);
                int regionY = (int)(entity.Position.Y / regionSize);
                var regionKey = (regionX, regionY);

                if (!entityRegions.ContainsKey(regionKey))
                    return;

                var entitiesInRegion = entityRegions[regionKey];

                for (int j = 0; j < entitiesInRegion.Count; j++)
                {
                    var otherEntity = entitiesInRegion[j];

                    if (entity == otherEntity)
                        continue;

                    bool collided = entity.CollidesWith(otherEntity, out float collisionDistance);
                    float gravityDistance = collisionDistance / (entity.Mass + otherEntity.Mass);
                    bool near = collisionDistance < 1.0f;

                    Vector2 direction = otherEntity.Position - entity.Position;
                    float distanceSquared = direction.LengthSquared();
                    float gravitationalConstant = 0.0001f;
                    float relativeSize = 1 / (otherEntity.Radius + entity.Radius);
                    Vector2 gravitationalForce = gravitationalConstant * direction / distanceSquared;
                    entity.Velocity += gravitationalForce * (dtime * entity.Radius * relativeSize);
                    otherEntity.Velocity -= gravitationalForce * (dtime * otherEntity.Radius * relativeSize);

                    if (collided)
                    {
                        lock (collisionResults)
                        {
                            collisionResults.Add(new CollisionResult { Entity1 = i, Entity2 = Entities.IndexOf(otherEntity), Near = near, Collided = collided, Distance = collisionDistance });
                        }
                    }
                }

                foreach (var regionGravity in regionAverageGravities)
                {
                    if (regionKey == regionGravity.Key)
                        continue;

                    Vector2 regionLocation = new Vector2(regionX, regionY) * regionSize;

                    Vector2 direction = regionLocation - entity.Position;
                    float distanceSquared = direction.LengthSquared();
                    float gravitationalConstant = 0.0001f;
                    float relativeSize = 1 / entity.Radius;
                    Vector2 gravitationalForce = gravitationalConstant * direction / distanceSquared;
                    entity.Velocity += gravitationalForce * (dtime * entity.Radius * relativeSize);
                }
            });

            Parallel.For(0, collisionResults.Count, i => {
                var collisionResult = collisionResults[i];

                var entityX = Entities[collisionResult.Entity1];
                var entityY = Entities[collisionResult.Entity2];

                // Calculate the bounce
                Vector2 normal = Vector2.Normalize(entityX.Position - entityY.Position);
                Vector2 relativeVelocity = entityX.Velocity - entityY.Velocity;

                float relativeSpeed = Math.Abs(Vector2.Dot(relativeVelocity, normal));
                if (relativeSpeed < 0.0001f)
                {
                    Vector2 relativeMomentum = entityX.Velocity / entityX.Mass + entityY.Velocity / entityY.Mass;
                    relativeMomentum /= 2;

                    entityX.Radius = (float)Math.Sqrt(entityX.Radius * entityX.Radius + entityY.Radius * entityY.Radius);
                    entityX.Mass = entityX.Mass + entityY.Mass;
                    entityX.Colour = entityX.Colour + entityY.Colour / 2;
                    entityX.Velocity = relativeMomentum * entityX.Mass / (entityX.Mass + entityY.Mass);

                    entityY.MarkedForDeletion = true;
                    return;
                }

                float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);

                if (velocityAlongNormal > 0)
                    return;

                float restitution = 0.9f; // Perfectly elastic collision
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

                // Move the entities apart to avoid double collision
                float overlap = (entityX.Radius + entityY.Radius) - collisionResult.Distance;
                entityX.Position += normal * overlap / 2;
                entityY.Position -= normal * overlap / 2;
            });

            Parallel.For(0, Entities.Count, i => {
                var entity = Entities[i];

                entity.Position += entity.Velocity * dtime;

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