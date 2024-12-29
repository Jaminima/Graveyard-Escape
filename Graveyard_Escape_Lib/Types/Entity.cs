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
        public float ZIndex { get; set; } = 0.0f;
        public Vector2 Velocity { get; set; }

        public Entity()
        {
            _Renderer = new EntityRenderer(this);
        }

        public void Init(){
            _Renderer.InitGL();
        }

        public void Render()
        {
            _Renderer.RenderGL();
        }

        public void Unload(){
            _Renderer.UnloadGL();
        }
    }
}