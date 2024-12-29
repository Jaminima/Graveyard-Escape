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
    }
}