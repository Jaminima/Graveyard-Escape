using System.Numerics;
using Graveyard_Escape_Game.Engine;
using Graveyard_Escape_Game.Types;
using System.Runtime.InteropServices;
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
        private readonly Renderer _renderer;

        //FPS counter
        private double _time = 0;
        private int _frames = 0;
        private float _zoom = 1.0f;
        private float _timeScale = 20.0f;
        private System.Numerics.Vector2 _cameraPosition = new System.Numerics.Vector2(0, 0f);
        private int _textureHandle;
        private int _fboHandle;

        public Window(int width, int height, string title): base(new GameWindowSettings(), new NativeWindowSettings() { ClientSize = new Vector2i(width, height), Title = title,   })
        {
            _width = width;
            _height = height;
            _title = title;
            _world = new World();
            _renderer = new Renderer();
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(Color4.LightGreen);
            PrintGraphicsInfo();

            // Create texture
            _textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _textureHandle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Create FBO
            _fboHandle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fboHandle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _textureHandle, 0);

            var fboStatus = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (fboStatus != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine($"Framebuffer is not complete: {fboStatus}");
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            PackedColor[] renderedScene = _renderer.RenderWorld(_world, _width, _height, _zoom, _cameraPosition);

            // 1. Upload data to texture
            GL.BindTexture(TextureTarget.Texture2D, _textureHandle);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, _width, _height, PixelFormat.Rgba, PixelType.UnsignedByte, renderedScene);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // 2. Bind the FBO as the read framebuffer
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fboHandle);
            
            // 3. Bind the default framebuffer as the draw framebuffer
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            // 4. Blit the framebuffer
            GL.BlitFramebuffer(
                0, 0, _width, _height, // Source rectangle
                0, 0, ClientSize.X, ClientSize.Y, // Destination rectangle
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Nearest
            );

            // 5. Unbind framebuffers
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

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
            HandleInput((float)e.Time);
            _world.Update((float)e.Time * _timeScale);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            
            // Clean up resources
            _world?.Dispose();
            
            if (_textureHandle != 0)
            {
                GL.DeleteTexture(_textureHandle);
            }
            
            if (_fboHandle != 0)
            {
                GL.DeleteFramebuffer(_fboHandle);
            }
        }

        private float mouseHeldFor = 0.0f;
        private Random random = new Random();
        private void HandleInput(float deltaTime)
        {
            KeyboardState keyboardState = KeyboardState.GetSnapshot();
            MouseState mouseState = MouseState.GetSnapshot();

            //TIME SCALE
            float timeScaleStep = 1 + (1.0f * deltaTime);

            if (keyboardState.IsKeyDown(Keys.Z))
            {
                _timeScale *= timeScaleStep;
            }
            if (keyboardState.IsKeyDown(Keys.X))
            {
                _timeScale /= timeScaleStep;
            }

            //MOVEMENT
            float step = 0.5f * deltaTime / _zoom;

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

            // ZOOM
            float zoomStep = 0.25f * deltaTime;

            if (keyboardState.IsKeyDown(Keys.Q))
            {
                _zoom += zoomStep;
            }
            if (keyboardState.IsKeyDown(Keys.E))
            {
                _zoom -= zoomStep;
            }

            _zoom = Math.Max(0.01f, _zoom);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
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
