using Graveyard_Escape_Lib.Types;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Graveyard_Escape_Game
{
    public class Window : GameWindow
    {
        private readonly int _width;
        private readonly int _height;
        private readonly string _title;
        private readonly World _world;

        public Window(int width, int height, string title): base(new GameWindowSettings(), new NativeWindowSettings() { ClientSize = new Vector2i(width, height), Title = title,   })
        {
            _width = width;
            _height = height;
            _title = title;
            _world = new World();
        }

        protected override void OnLoad()
        {
            GL.ClearColor(Color4.CornflowerBlue);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            // ...rendering code...
            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            // ...update logic...
            _world.Update();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
        }
    }
}
