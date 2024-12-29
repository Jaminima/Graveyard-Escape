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
        public int LastCollisionId { get; set; } = -1;

        private readonly Renderer _Renderer;
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float SpinSpeed { get; set; } = 0.0f;
        public float Scale { get; set; } = 1.0f;
        public float Mass { get; set; } = 1.0f;
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

        public bool IsNear(Entity<T> other, float maxDistance, out float distance)
        {
            distance = Vector2.Distance(Position, other.Position) / Scale;
            return distance < maxDistance;
        }

        public bool CollidesWith(Entity<T> other, out Vector2 collisionPoint)
        {
            Matrix3x2 transform = Matrix3x2.CreateScale(Scale) * Matrix3x2.CreateRotation(Rotation) * Matrix3x2.CreateTranslation(Position);
            Matrix3x2 otherTransform = Matrix3x2.CreateScale(other.Scale) * Matrix3x2.CreateRotation(other.Rotation) * Matrix3x2.CreateTranslation(other.Position);

            for (int i = 0; i < VertexData.Length; i += 4)
            {
                Vector2 point1 = new Vector2(VertexData[i], VertexData[i + 1]);
                Vector2 point2 = new Vector2(VertexData[(i + 4) % VertexData.Length], VertexData[(i + 5) % VertexData.Length]);
                point1 = Vector2.Transform(point1, transform);
                point2 = Vector2.Transform(point2, transform);

                for (int j = 0; j < other.VertexData.Length; j += 4)
                {
                    Vector2 otherPoint1 = new Vector2(other.VertexData[j], other.VertexData[j + 1]);
                    Vector2 otherPoint2 = new Vector2(other.VertexData[(j + 4) % other.VertexData.Length], other.VertexData[(j + 5) % other.VertexData.Length]);
                    otherPoint1 = Vector2.Transform(otherPoint1, otherTransform);
                    otherPoint2 = Vector2.Transform(otherPoint2, otherTransform);

                    if (LinesIntersect(point1, point2, otherPoint1, otherPoint2, out collisionPoint))
                    {
                        return true;
                    }
                }
            }
            collisionPoint = Vector2.Zero;
            return false;
        }

        private bool LinesIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
        {
            float A1 = p2.Y - p1.Y;
            float B1 = p1.X - p2.X;
            float C1 = A1 * p1.X + B1 * p1.Y;

            float A2 = p4.Y - p3.Y;
            float B2 = p3.X - p4.X;
            float C2 = A2 * p3.X + B2 * p3.Y;

            float delta = A1 * B2 - A2 * B1;

            if (delta == 0)
            {
                intersection = Vector2.Zero;
                return false; // Lines are parallel
            }

            float x = (B2 * C1 - B1 * C2) / delta;
            float y = (A1 * C2 - A2 * C1) / delta;
            intersection = new Vector2(x, y);

            if (IsBetween(p1, p2, intersection) && IsBetween(p3, p4, intersection))
            {
                return true;
            }

            intersection = Vector2.Zero;
            return false;
        }

        private bool IsBetween(Vector2 a, Vector2 b, Vector2 c)
        {
            return (c.X >= Math.Min(a.X, b.X) && c.X <= Math.Max(a.X, b.X) &&
                    c.Y >= Math.Min(a.Y, b.Y) && c.Y <= Math.Max(a.Y, b.Y));
        }
    }
}