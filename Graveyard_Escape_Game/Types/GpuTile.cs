using System.Numerics;
using System.Runtime.InteropServices;

namespace Graveyard_Escape_Game.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GpuTile
    {
        public float FlowMomentumX;
        public float FlowMomentumY;
        public float Pressure;
        public float Unused; // Padding for vec4 alignment
        
        public GpuTile(float flowX, float flowY, float pressure)
        {
            FlowMomentumX = flowX;
            FlowMomentumY = flowY;
            Pressure = pressure;
            Unused = 0.0f;
        }
        
        public Vector2 FlowMomentum
        {
            get => new Vector2(FlowMomentumX, FlowMomentumY);
            set { FlowMomentumX = value.X; FlowMomentumY = value.Y; }
        }
        
        public static implicit operator GpuTile(Tile tile)
        {
            return new GpuTile(tile.FlowMomentum.X, tile.FlowMomentum.Y, tile.Pressure);
        }
        
        public static implicit operator Tile(GpuTile gpuTile)
        {
            return new Tile
            {
                FlowMomentum = new Vector2(gpuTile.FlowMomentumX, gpuTile.FlowMomentumY),
                Pressure = gpuTile.Pressure
            };
        }
    }
}