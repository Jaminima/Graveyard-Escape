using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Graveyard_Escape_Lib.Types
{
    public class Entity<T> where T : Renderer, new()
    {
        private readonly Renderer _Renderer;
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Scale { get; set; } = 1.0f;
        public Vector4 Colour { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public float Rotation { get; set; } = 0.0f;
        public float[] VertexData { get; set; }

        public Entity(string vertexFileName)
        {
            _Renderer = new T();
            this.VertexData =  LoadVertexData(vertexFileName);
        }

        public Entity(float[] vertexData)
        {
            _Renderer = new T();
            this.VertexData = vertexData;
        }

        public static float[] LoadVertexData(string vertexFileName)
        {
            // Load vertex data from file
            string[] vertexRowData = System.IO.File.ReadAllLines($"GLSL/{vertexFileName}.vertex.map");

            List<float> vertexData = new List<float>();

            foreach (string row in vertexRowData)
            {
                string[] rowData = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (string data in rowData)
                {
                    vertexData.Add(float.Parse(data));
                }
            }

            return vertexData.ToArray();
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
            Scale = newScale;
        }

        public void Render()
        {
            _Renderer.RenderGL(this);
        }

        public void Unload(){
            _Renderer.UnloadGL();
        }

        public bool CollidesWith(Entity<T> other, out Vector2 collisionPoint)
        {
            Matrix3x2 transform = Matrix3x2.CreateScale(Scale) * Matrix3x2.CreateRotation(Rotation) * Matrix3x2.CreateTranslation(Position);
            Matrix3x2 otherTransform = Matrix3x2.CreateScale(other.Scale) * Matrix3x2.CreateRotation(other.Rotation) * Matrix3x2.CreateTranslation(other.Position);

            for (int i = 0; i < VertexData.Length; i += 4)
            {
                Vector2 point = new Vector2(VertexData[i], VertexData[i + 1]);
                point = Vector2.Transform(point, transform);

                for (int j = 0; j < other.VertexData.Length; j += 4)
                {
                    Vector2 otherPoint = new Vector2(other.VertexData[j], other.VertexData[j + 1]);
                    otherPoint = Vector2.Transform(otherPoint, otherTransform);

                    if (Vector2.Distance(point, otherPoint) < (Scale + other.Scale) * 0.5f)
                    {
                        collisionPoint = point;
                        return true;
                    }
                }
            }
            collisionPoint = Vector2.Zero;
            return false;
        }
    }
}