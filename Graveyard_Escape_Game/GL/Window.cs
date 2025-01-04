using System.Numerics;
using Graveyard_Escape_Lib.Types;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Graveyard_Escape_Game
{
    public class Window : GameWindow
    {
        private readonly int _width;
        private readonly int _height;
        private readonly string _title;
        private readonly World _world;

        //FPS counter
        private double _time = 0;
        private int _frames = 0;
        private float _zoom = 1.0f;
        private System.Numerics.Vector2 _cameraPosition = new System.Numerics.Vector2(0, 0f);

        public Window(int width, int height, string title): base(new GameWindowSettings(), new NativeWindowSettings() { ClientSize = new Vector2i(width, height), Title = title,   })
        {
            _width = width;
            _height = height;
            _title = title;
            _world = new World();
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(Color4.Black);

            // Print graphics system information
            PrintGraphicsInfo();

            // Disable depth test for 2D rendering
            GL.Disable(EnableCap.DepthTest);

            // Set up the projection matrix
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, _width, 0.0, _height, -1.0, 1.0);

            // Set up the modelview matrix
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();

            // Initialize OpenGL for entities
            foreach (var entity in _world.Entities)
            {
                entity.Init();
            }
        }

        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            _world.Entities.ForEach(entity => entity.Unload());

            base.OnUnload();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Render entities
            foreach (var entity in _world.Entities)
            {
                entity.Render(_cameraPosition, _zoom);
            }

            SwapBuffers();

            _time += e.Time;
            _frames++;
            if (_time > 1.0)
            {
                Console.WriteLine($"FPS: {_frames / _time:0}");
                _time = 0;
                _frames = 0;
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            // ...update logic...
            HandleInput((float)e.Time);
            _world.Update((float)e.Time);
        }

        private void HandleInput(float deltaTime)
        {
            KeyboardState keyboardState = KeyboardState.GetSnapshot();

            float step = 0.5f * deltaTime;

            if (keyboardState.IsKeyDown(Keys.W))
            {
                _cameraPosition.Y += step;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                _cameraPosition.Y -= step;
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                _cameraPosition.X -= step;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                _cameraPosition.X += step;
            }

            float zoomStep = 1.0f * deltaTime;

            if (keyboardState.IsKeyDown(Keys.Q))
            {
                _zoom += zoomStep;
            }
            if (keyboardState.IsKeyDown(Keys.E))
            {
                _zoom -= zoomStep;
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);

            // Update the projection matrix
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0.0, e.Width, 0.0, e.Height, -1.0, 1.0);
        }

        private void PrintGraphicsInfo()
        {
            string renderer = GL.GetString(StringName.Renderer);
            string version = GL.GetString(StringName.Version);
            string vendor = GL.GetString(StringName.Vendor);
            Console.WriteLine($"Renderer: {renderer}");
            Console.WriteLine($"OpenGL version: {version}");
            Console.WriteLine($"Vendor: {vendor}");
        }
    }
}
