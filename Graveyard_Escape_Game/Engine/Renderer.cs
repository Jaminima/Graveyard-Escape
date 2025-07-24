using Graveyard_Escape_Game.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Graveyard_Escape_Game.Engine
{
    public class Renderer
    {
        public PackedColor[] RenderWorld(World world, int width, int height, float zoom, System.Numerics.Vector2 cameraPosition)
        {
            PackedColor[] renderBuffer = new PackedColor[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x >= 0 && x < World.SceneWidth && y >= 0 && y < World.SceneHeight)
                    {
                        int worldIndex = y * World.SceneWidth + x;
                        Tile tile = world.DoubleTileBuffer[world.BufferIndex * World.SceneTotalElements + worldIndex];
                        
                        // Use flow direction for color and pressure for intensity
                        Vector2 flow = tile.FlowMomentum;
                        
                        // Normalize flow to map to color channels
                        byte r = (byte)((Math.Clamp(flow.X, -1f, 1f) * 0.5f + 0.5f) * 255);
                        byte g = (byte)((Math.Clamp(flow.Y, -1f, 1f) * 0.5f + 0.5f) * 255);
                        byte b = 0;

                        byte intensity = (byte)(Math.Clamp(tile.Pressure / zoom, 0.0f, 1.0f) * 255);

                        if (flow.LengthSquared() < 0.01f) // Threshold for low momentum
                        {
                            r = 0;
                            g = 0;
                            b = intensity;
                        }
                        else
                        {
                            // Modulate color by intensity
                            r = (byte)(r * intensity / 255);
                            g = (byte)(g * intensity / 255);
                        }
                        
                        renderBuffer[y * width + x] = PackedColor.FromArgb(r, g, b);
                    }
                    else
                    {
                        renderBuffer[y * width + x] = PackedColor.FromArgb(0, 0, 0); // Black for out-of-bounds
                    }
                }
            }

            return renderBuffer;
        }
    }
}
