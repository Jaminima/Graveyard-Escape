using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Threading.Tasks;

namespace Graveyard_Escape_Lib.Types
{
    public class Entity<T> where T : Renderer, new()
    {
        public int Id { get; set; }

        private readonly Renderer _Renderer;
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Radius { get; set; } = 1.0f;
        public float Mass { get; set; } = 1.0f;
        public Vector4 Colour { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public bool MarkedForDeletion { get; set; } = false;
        public float[] VertexData { get; set; }


        public Entity()
        {
            _Renderer = new T();
            // Diamond composing of 4 triangles

            int segments = 16;

            float[] EdgeData = new float[segments * 2];

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)(2.0f * Math.PI / segments) * i;

                float x = (float)Math.Cos(angle);
                float y = (float)Math.Sin(angle);

                EdgeData[i * 2] = x;
                EdgeData[i * 2 + 1] = y;
            }

            int vertexTriangleSize = 3 * 4;
            this.VertexData = new float[segments * vertexTriangleSize];

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;

                int vi = i * vertexTriangleSize;

                this.VertexData[vi] = EdgeData[i * 2];
                this.VertexData[vi + 1] = EdgeData[i * 2 + 1];
                this.VertexData[vi + 2] = 0.0f;
                this.VertexData[vi + 3] = 1.0f;

                this.VertexData[vi + 4] = EdgeData[next * 2];
                this.VertexData[vi + 5] = EdgeData[next * 2 + 1];
                this.VertexData[vi + 6] = 0.0f;
                this.VertexData[vi + 7] = 1.0f;

                this.VertexData[vi + 8] = 0.0f;
                this.VertexData[vi + 9] = 0.0f;
                this.VertexData[vi + 10] = 0.0f;
                this.VertexData[vi + 11] = 1.0f;
            }

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

        public void Render(Vector2 cameraPosition, float sceneZoom)
        {
            _Renderer.RenderGL(this,cameraPosition, sceneZoom);
        }

        public void Unload(){
            _Renderer.UnloadGL();
        }

        public bool CollidesWith(Entity<T> other, out float distance)
        {
            distance =  Vector2.Distance(Position, other.Position);

            return distance / (Radius + other.Radius) < 1.0f;
        }
    }
}