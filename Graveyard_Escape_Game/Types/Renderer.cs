using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Graveyard_Escape_Lib.Types
{
    public interface Renderer
    {
        public virtual void InitGL<T>(Entity<T> entity) where T : Renderer, new(){}
        public virtual void RenderGL<T>(Entity<T> entity) where T : Renderer, new(){}
        public virtual void UnloadGL(){}

    }
}