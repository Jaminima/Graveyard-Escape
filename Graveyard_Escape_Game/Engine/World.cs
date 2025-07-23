using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Graveyard_Escape_Game.Types;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace Graveyard_Escape_Game.Engine
{
    public class World : IDisposable
    {
        public const int SceneWidth = 1920;
        public const int SceneHeight = 1080;
        public const int SceneTotalElements = SceneWidth * SceneHeight;
        
        // GPU buffers
        private int[] _bufferHandles;
        private int _currentBufferIndex = 0;
        
        // Compute shaders
        private ComputeShader _advectionShader;
        private ComputeShader _pressureShader;
        private ComputeShader _momentumShader;
        
        // CPU data for compatibility
        public Tile[] DoubleTileBuffer { get; private set; }
        public int BufferIndex => _currentBufferIndex;
        
        private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));
        private bool _disposed = false;
        private bool _useGPU = true;

        public World()
        {
            try
            {
                InitializeGPUBuffers();
                LoadComputeShaders();
                Console.WriteLine("GPU fluid simulation initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize GPU simulation, falling back to CPU: {ex.Message}");
                _useGPU = false;
            }
            
            InitializeSimulationData();
        }

        private void InitializeGPUBuffers()
        {
            // Check if compute shaders are supported
            GL.GetInteger(GetPName.MaxComputeWorkGroupCount, out int maxWorkGroups);
            if (maxWorkGroups == 0)
            {
                throw new NotSupportedException("Compute shaders are not supported on this hardware");
            }
            
            // Create two buffer objects for double buffering
            _bufferHandles = new int[2];
            GL.GenBuffers(2, _bufferHandles);
            
            int bufferSize = SceneTotalElements * Marshal.SizeOf<GpuTile>();
            
            for (int i = 0; i < 2; i++)
            {
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _bufferHandles[i]);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, bufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        private void LoadComputeShaders()
        {
            _advectionShader = new ComputeShader("GLSL/fluid_advection.compute.glsl");
            _pressureShader = new ComputeShader("GLSL/fluid_pressure.compute.glsl");
            _momentumShader = new ComputeShader("GLSL/fluid_momentum.compute.glsl");
        }

        private void InitializeSimulationData()
        {
            // Create initial data on CPU
            GpuTile[] initialData = new GpuTile[SceneTotalElements];
            
            // Initialize with base pressure and zero momentum
            for (int i = 0; i < SceneTotalElements; i++)
            {
                initialData[i] = new GpuTile(0.0f, 0.0f, 0.2f);
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
                            Vector2 momentum = initialData[index].FlowMomentum + flowMomentum * falloff;
                            initialData[index].FlowMomentum = momentum;
                        }
                    }
                }
            }

            // Clamp initial pressure
            for (int i = 0; i < SceneTotalElements; i++)
            {
                initialData[i].Pressure = Math.Max(0, Math.Min(1.0f, initialData[i].Pressure));
            }

            if (_useGPU)
            {
                // Upload initial data to both GPU buffers
                int bufferSize = SceneTotalElements * Marshal.SizeOf<GpuTile>();
                for (int i = 0; i < 2; i++)
                {
                    GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _bufferHandles[i]);
                    GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, bufferSize, initialData);
                }
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            }

            // Initialize CPU buffer for compatibility
            DoubleTileBuffer = new Tile[2 * SceneTotalElements];
            for (int i = 0; i < SceneTotalElements; i++)
            {
                DoubleTileBuffer[i] = initialData[i];
                DoubleTileBuffer[i + SceneTotalElements] = initialData[i];
            }
        }

        public void Update(float dtime)
        {
            if (_useGPU)
            {
                UpdateGPU(dtime);
            }
            else
            {
                UpdateCPU(dtime);
            }
        }

        private void UpdateGPU(float dtime)
        {
            int inputBufferIndex = _currentBufferIndex;
            int outputBufferIndex = (_currentBufferIndex + 1) % 2;
            
            // Set up buffer bindings
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _bufferHandles[inputBufferIndex]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _bufferHandles[outputBufferIndex]);
            
            // Calculate work group dimensions
            int workGroupsX = (SceneWidth + 15) / 16;  // Round up to nearest multiple of 16
            int workGroupsY = (SceneHeight + 15) / 16;
            
            // Step 1: Advection
            _advectionShader.Use();
            _advectionShader.SetUniform("sceneWidth", SceneWidth);
            _advectionShader.SetUniform("sceneHeight", SceneHeight);
            _advectionShader.SetUniform("deltaTime", dtime);
            _advectionShader.Dispatch(workGroupsX, workGroupsY, 1);
            
            // Wait for advection to complete
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            
            // Swap buffers for next step
            int tempBufferIndex = inputBufferIndex;
            inputBufferIndex = outputBufferIndex;
            outputBufferIndex = tempBufferIndex;
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _bufferHandles[inputBufferIndex]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _bufferHandles[outputBufferIndex]);
            
            // Step 2: Pressure projection
            _pressureShader.Use();
            _pressureShader.SetUniform("sceneWidth", SceneWidth);
            _pressureShader.SetUniform("sceneHeight", SceneHeight);
            _pressureShader.Dispatch(workGroupsX, workGroupsY, 1);
            
            // Wait for pressure update to complete
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            
            // Swap buffers for next step
            tempBufferIndex = inputBufferIndex;
            inputBufferIndex = outputBufferIndex;
            outputBufferIndex = tempBufferIndex;
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _bufferHandles[inputBufferIndex]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _bufferHandles[outputBufferIndex]);
            
            // Step 3: Momentum update
            _momentumShader.Use();
            _momentumShader.SetUniform("sceneWidth", SceneWidth);
            _momentumShader.SetUniform("sceneHeight", SceneHeight);
            _momentumShader.SetUniform("deltaTime", dtime);
            _momentumShader.Dispatch(workGroupsX, workGroupsY, 1);
            
            // Wait for momentum update to complete
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            
            // Update current buffer index
            _currentBufferIndex = outputBufferIndex;
            
            // Update CPU buffer for compatibility with renderer
            UpdateCPUBufferFromGPU();
            
            // Unbind buffers
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, 0);
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

        private void UpdateCPUBufferFromGPU()
        {
            // Download current GPU buffer to CPU for rendering compatibility
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _bufferHandles[_currentBufferIndex]);
            IntPtr ptr = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadOnly);
            
            if (ptr != IntPtr.Zero)
            {
                unsafe
                {
                    GpuTile* gpuData = (GpuTile*)ptr.ToPointer();
                    int offset = _currentBufferIndex * SceneTotalElements;
                    
                    for (int i = 0; i < SceneTotalElements; i++)
                    {
                        DoubleTileBuffer[offset + i] = gpuData[i];
                    }
                }
                
                GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);
            }
            
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
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
                _advectionShader?.Dispose();
                _pressureShader?.Dispose();
                _momentumShader?.Dispose();
                
                if (_bufferHandles != null)
                {
                    GL.DeleteBuffers(2, _bufferHandles);
                }
                
                _disposed = true;
            }
        }
    }
}