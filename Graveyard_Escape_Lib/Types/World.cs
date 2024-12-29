using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Graveyard_Escape_Lib.Types
{
    public class World
    {
        public List<Entity> Entities { get; set; }

        public World()
        {
            Entities = new List<Entity>();
        }

        public void Update()
        {
            foreach (var entity in Entities)
            {
                entity.Position += entity.Velocity;
            }
        }
    }
}