using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Graveyard_Escape_Lib.Types;

namespace Graveyard_Escape_Game.Renderers
{
    public class EntityRenderer
    {
        private readonly Entity _entity;

        public EntityRenderer(Entity entity)
        {
            _entity = entity;
        }

        public void Render()
        {
            Console.WriteLine($"Rendering entity at {_entity.Position}");
        }
    }
}