using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Graveyard_Escape_Game.Types;

namespace Graveyard_Escape_Lib.Types
{
    public class World
    {
        public const int SceneWidth = 1000;
        public const int SceneHeight = 1000;
        public const int SceneTotalElements = SceneWidth * SceneHeight;
        public Tile[] DoubleTileBuffer { get; private set; }
        public int BufferIndex { get; private set; } = 0;

        public World()
        {
            Random random = new Random();

            DoubleTileBuffer = new Tile[2 * SceneTotalElements];

            for (int i = 0; i < SceneTotalElements; i++)
            {
                DoubleTileBuffer[i] = new Tile
                {
                    Pressure = (float)random.NextDouble(),
                    FlowMomentum = new Vector2()
                };
            }
        }

        public void Update(float dtime)
        {
            int lastBufferIndex = BufferIndex;
            NextBuffer();

            int lastOffset = lastBufferIndex * SceneTotalElements;
            int currentOffset = BufferIndex * SceneTotalElements;

            Parallel.For(0, SceneTotalElements, i =>
            {
                int myIndex = i + lastOffset;

                Tile tileAbove = DoubleTileBuffer[myIndex - SceneWidth];
                Tile tileBelow = DoubleTileBuffer[myIndex + SceneWidth];

                Tile thisTile = DoubleTileBuffer[myIndex];

                Tile tileLeft = DoubleTileBuffer[myIndex - 1];
                Tile tileRight = DoubleTileBuffer[myIndex + 1];

                // Calculate pressure based on neighboring tiles
                DoubleTileBuffer[i + currentOffset].Pressure = (tileAbove.Pressure + tileBelow.Pressure + tileLeft.Pressure + tileRight.Pressure) / 4.0f;
            });

        }

        private void NextBuffer()
        {
            BufferIndex = (BufferIndex + 1) % 2;
        }
    }
}