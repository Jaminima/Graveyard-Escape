using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Graveyard_Escape_Game.Renderers;

namespace Graveyard_Escape_Lib.Types
{
    public class Entity
    {
        private readonly EntityRenderer _Renderer;
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Scale { get; set; } = 1.0f;
        public Vector4 Colour { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public float Rotation { get; set; } = 0.0f;

        public Entity()
        {
            _Renderer = new EntityRenderer();
        }

        public void Init(){
            _Renderer.InitGL();
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