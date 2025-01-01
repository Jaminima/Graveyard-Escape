using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Graveyard_Escape_Lib.Types
{
    public class Entity<T> where T : Renderer, new()
    {
        public int Id { get; set; }
        public List<int> LastCollidedWith { get; set; } = new List<int>();
        public List<int> HasCollidedWith { get; set; } = new List<int>();

        private readonly Renderer _Renderer;
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float SpinSpeed { get; set; } = 0.0f;
        public float Radius { get; set; } = 1.0f;
        public float Mass { get; set; } = 1.0f;
        public Vector4 Colour { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public bool MarkedForDeletion { get; set; } = false;


        public Entity()
        {
            _Renderer = new T();
        }

        public void Init(){
            _Renderer.InitGL(this);
        }

        public void UpdatePosition(Vector2 newPosition)
        {
            Position = newPosition;
        }

        public void UpdateScale(float newScale)
        {
            Radius = newScale;
        }

        public void Render()
        {
            _Renderer.RenderGL(this);
        }

        public void Unload(){
            _Renderer.UnloadGL();
        }

        public bool IsNear(Entity<T> other, float maxDistance, out float distance)
        {
            distance = Vector2.Distance(Position, other.Position);
            return distance < maxDistance;
        }

        public bool CollidesWith(Entity<T> other)
        {
            float distance = Vector2.Distance(Position, other.Position) / (Radius + other.Radius);

            return distance < 1.0f;
        }
    }
}