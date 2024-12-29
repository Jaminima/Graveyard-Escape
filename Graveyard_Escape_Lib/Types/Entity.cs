using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Graveyard_Escape_Lib.Types
{
    public class Entity
    {
        public Vector2 Position { get; set; }
        public float ZIndex { get; set; } = 0.0f;
        public Vector2 Velocity { get; set; }
    }
}