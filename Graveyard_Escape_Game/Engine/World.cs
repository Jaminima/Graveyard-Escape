using System;
using System.Numerics;
using System.Threading.Tasks;
using Graveyard_Escape_Game.Types;

namespace Graveyard_Escape_Game.Engine
{
    public class World : IDisposable
    {
        public const int SceneWidth = 1920;
        public const int SceneHeight = 1080;
        public const int SceneTotalElements = SceneWidth * SceneHeight;
        
        private int _currentBufferIndex = 0;
        
        // CPU data
        public Tile[] DoubleTileBuffer { get; private set; }
        public int BufferIndex => _currentBufferIndex;
        
        private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
        private bool _disposed = false;

        public World()
        {
            InitializeSimulationData();
        }

        private void InitializeSimulationData()
        {
            // Create initial data
            Tile[] initialData = new Tile[SceneTotalElements];
            
            // Initialize with base pressure and zero momentum
            for (int i = 0; i < SceneTotalElements; i++)
            {
                initialData[i] = new Tile
                {
                    Pressure = 0.2f,
                    FlowMomentum = Vector2.Zero
                };
            }

            // Create random splodges of pressure and momentum
            int numberOfSplodges = 50;
            for (int s = 0; s < numberOfSplodges; s++)
            {
                int centerX = _random.Value!.Next(SceneWidth);
                int centerY = _random.Value!.Next(SceneHeight);
                float radius = _random.Value!.Next(50, 150);
                float maxPressure = (float)(_random.Value!.NextDouble());
                Vector2 flowMomentum = new Vector2(
                    (float)(_random.Value!.NextDouble() * 4 - 2),
                    (float)(_random.Value!.NextDouble() * 4 - 2)
                );

                int startX = Math.Max(0, (int)(centerX - radius));
                int endX = Math.Min(SceneWidth, (int)(centerX + radius));
                int startY = Math.Max(0, (int)(centerY - radius));
                int endY = Math.Min(SceneHeight, (int)(centerY + radius));

                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        float dx = x - centerX;
                        float dy = y - centerY;
                        float distanceSq = dx * dx + dy * dy;

                        if (distanceSq < radius * radius)
                        {
                            int index = y * SceneWidth + x;
                            float distance = (float)Math.Sqrt(distanceSq);
                            float falloff = 1.0f - (distance / radius);
                            falloff *= falloff;

                            initialData[index].Pressure += maxPressure * falloff;
                            initialData[index].FlowMomentum += flowMomentum * falloff;
                        }
                    }
                }
            }

            // Clamp initial pressure
            for (int i = 0; i < SceneTotalElements; i++)
            {
                initialData[i].Pressure = Math.Max(0, Math.Min(1.0f, initialData[i].Pressure));
            }

            // Initialize CPU buffer
            DoubleTileBuffer = new Tile[2 * SceneTotalElements];
            for (int i = 0; i < SceneTotalElements; i++)
            {
                DoubleTileBuffer[i] = initialData[i];
                DoubleTileBuffer[i + SceneTotalElements] = initialData[i];
            }
        }

        public void Update(float dtime)
        {
            UpdateCPU(dtime);
        }

        private void UpdateCPU(float dtime)
        {
            int lastBufferIndex = _currentBufferIndex;
            NextBuffer();

            int lastOffset = lastBufferIndex * SceneTotalElements;
            int currentOffset = _currentBufferIndex * SceneTotalElements;

            // Advection, pressure, and momentum updates
            Parallel.For(0, SceneTotalElements, i =>
            {
                int x = i % SceneWidth;
                int y = i / SceneWidth;
                int myIndex = i + lastOffset;
                int currentCellIndex = i + currentOffset;

                var lastTile = DoubleTileBuffer[myIndex];

                // 1. Advection: Move pressure and momentum along the flow
                float sourceX = x - lastTile.FlowMomentum.X * dtime;
                float sourceY = y - lastTile.FlowMomentum.Y * dtime;

                // Clamp source coordinates to be within bounds
                sourceX = Math.Max(0.0f, Math.Min(SceneWidth - 1.001f, sourceX));
                sourceY = Math.Max(0.0f, Math.Min(SceneHeight - 1.001f, sourceY));

                int x0 = (int)sourceX;
                int y0 = (int)sourceY;
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                float sx = sourceX - x0;
                float sy = sourceY - y0;

                // Bilinear interpolation for advected pressure and momentum
                float p00 = DoubleTileBuffer[lastOffset + y0 * SceneWidth + x0].Pressure;
                float p10 = DoubleTileBuffer[lastOffset + y0 * SceneWidth + x1].Pressure;
                float p01 = DoubleTileBuffer[lastOffset + y1 * SceneWidth + x0].Pressure;
                float p11 = DoubleTileBuffer[lastOffset + y1 * SceneWidth + x1].Pressure;
                float advectedPressure = Lerp(Lerp(p00, p10, sx), Lerp(p01, p11, sx), sy);

                Vector2 m00 = DoubleTileBuffer[lastOffset + y0 * SceneWidth + x0].FlowMomentum;
                Vector2 m10 = DoubleTileBuffer[lastOffset + y0 * SceneWidth + x1].FlowMomentum;
                Vector2 m01 = DoubleTileBuffer[lastOffset + y1 * SceneWidth + x0].FlowMomentum;
                Vector2 m11 = DoubleTileBuffer[lastOffset + y1 * SceneWidth + x1].FlowMomentum;
                Vector2 advectedMomentum = Lerp(Lerp(m00, m10, sx), Lerp(m01, m11, sx), sy);

                // Store advected values temporarily in the current buffer
                DoubleTileBuffer[currentCellIndex].Pressure = advectedPressure;
                DoubleTileBuffer[currentCellIndex].FlowMomentum = advectedMomentum;
            });

            // Pressure-projection to enforce incompressibility
            Parallel.For(0, SceneTotalElements, i =>
            {
                int x = i % SceneWidth;
                int y = i / SceneWidth;
                int myIndex = i + currentOffset; // Use current buffer for this step

                // 2. Calculate Divergence from the advected momentum field
                float m_right, m_left, m_down, m_up;

                // Right neighbor
                if (x < SceneWidth - 1)
                    m_right = DoubleTileBuffer[myIndex + 1].FlowMomentum.X;
                else // Right boundary (wall)
                    m_right = 0.0f; // No-slip

                // Left neighbor
                if (x > 0)
                    m_left = DoubleTileBuffer[myIndex - 1].FlowMomentum.X;
                else // Left boundary (wall)
                    m_left = 0.0f; // No-slip

                // Bottom neighbor
                if (y < SceneHeight - 1)
                    m_down = DoubleTileBuffer[myIndex + SceneWidth].FlowMomentum.Y;
                else // Bottom boundary (wall)
                    m_down = 0.0f; // No-slip

                // Top neighbor
                if (y > 0)
                    m_up = DoubleTileBuffer[myIndex - SceneWidth].FlowMomentum.Y;
                else // Top boundary (wall)
                    m_up = 0.0f; // No-slip

                float divergence = m_right - m_left + m_down - m_up;

                // 3. Update Pressure based on divergence
                // This is a simplification. A full projection would solve a Poisson equation.
                float pressureCorrection = divergence * 0.1f; // Correction factor
                DoubleTileBuffer[myIndex].Pressure = Math.Max(0, DoubleTileBuffer[myIndex].Pressure - pressureCorrection);
            });

            // Update momentum from pressure gradient
            Parallel.For(0, SceneTotalElements, i =>
            {
                int x = i % SceneWidth;
                int y = i / SceneWidth;
                int myIndex = i + currentOffset;

                // 4. Update FlowMomentum based on the new pressure gradient
                float p_right = (x < SceneWidth - 1) ? DoubleTileBuffer[myIndex + 1].Pressure : DoubleTileBuffer[myIndex].Pressure;
                float p_left = (x > 0) ? DoubleTileBuffer[myIndex - 1].Pressure : DoubleTileBuffer[myIndex].Pressure;
                float p_down = (y < SceneHeight - 1) ? DoubleTileBuffer[myIndex + SceneWidth].Pressure : DoubleTileBuffer[myIndex].Pressure;
                float p_up = (y > 0) ? DoubleTileBuffer[myIndex - SceneWidth].Pressure : DoubleTileBuffer[myIndex].Pressure;

                Vector2 gradient = new Vector2(p_right - p_left, p_down - p_up);
                
                Vector2 newFlowMomentum = DoubleTileBuffer[myIndex].FlowMomentum - gradient * dtime;

                // Optional: Add some damping/viscosity to the flow
                newFlowMomentum *= 0.995f;

                // Enforce boundary conditions (no-slip walls)
                if (x == 0 || x == SceneWidth - 1)
                {
                    newFlowMomentum.X = 0;
                }

                if (y == 0 || y == SceneHeight - 1)
                {
                    // No-slip on top and bottom
                    newFlowMomentum.Y = 0;
                }

                DoubleTileBuffer[myIndex].FlowMomentum = newFlowMomentum;
            });
        }

        private float Lerp(float a, float b, float t) => a * (1 - t) + b * t;
        private Vector2 Lerp(Vector2 a, Vector2 b, float t) => a * (1 - t) + b * t;

        private void NextBuffer()
        {
            _currentBufferIndex = (_currentBufferIndex + 1) % 2;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _random?.Dispose();
                _disposed = true;
            }
        }
    }
}