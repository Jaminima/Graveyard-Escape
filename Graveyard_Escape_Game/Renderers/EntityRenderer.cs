using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Graveyard_Escape_Lib.Types;
using OpenTK.Graphics.OpenGL;

namespace Graveyard_Escape_Game.Renderers
{
    public class EntityRenderer: Renderer
    {
        private int _vertexShader;
        private int _fragmentShader;
        private int _shaderProgram;
        private int _vertexBufferObject;
        private int _vertexArrayObject;
        private int _entityPositionLocation;
        private int _entityScaleLocation;
        private int _entityColourLocation;
        private int _sceneZoomLocation;

        public void InitGL<T>(Entity<T> entity) where T : Renderer, new()
        {
            // Compile shaders
            string vertexShaderCode = File.ReadAllText("GLSL/entity.vertex.glsl");
            _vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(_vertexShader, vertexShaderCode);
            GL.CompileShader(_vertexShader);

            string fragmentShaderCode = File.ReadAllText("GLSL/entity.fragment.glsl");
            _fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(_fragmentShader, fragmentShaderCode);
            GL.CompileShader(_fragmentShader);

            // Create shader program
            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, _vertexShader);
            GL.AttachShader(_shaderProgram, _fragmentShader);
            GL.LinkProgram(_shaderProgram);

            // Create VBO
            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, entity.VertexData.Length * sizeof(float), entity.VertexData, BufferUsageHint.StaticDraw);

            // Create VAO
            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);
            var positionLocation = GL.GetAttribLocation(_shaderProgram, "position");
            GL.VertexAttribPointer(positionLocation, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(positionLocation);

            // Get uniform locations
            _entityPositionLocation = GL.GetUniformLocation(_shaderProgram, "entityPosition");
            _entityScaleLocation = GL.GetUniformLocation(_shaderProgram, "entityScale");
            _entityColourLocation = GL.GetUniformLocation(_shaderProgram, "entityColour");
            _sceneZoomLocation = GL.GetUniformLocation(_shaderProgram, "sceneZoom");
        }

        public void RenderGL<T>(Entity<T> entity, Vector2 cameraPosition, float sceneZoom) where T : Renderer, new()
        {
            // Bind and use shader program
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BindVertexArray(_vertexArrayObject);
            GL.UseProgram(_shaderProgram);

            // Set the entity position and scale uniforms
            Vector2 scenePosition = entity.Position - cameraPosition;
            GL.Uniform2(_entityPositionLocation, scenePosition.X, scenePosition.Y);
            GL.Uniform1(_entityScaleLocation, entity.Radius);

            // Set the entity colour uniform
            GL.Uniform4(_entityColourLocation, entity.Colour.X, entity.Colour.Y, entity.Colour.Z, entity.Colour.W);

            // Set the scene zoom
            GL.Uniform1(_sceneZoomLocation, sceneZoom);

            GL.DrawArrays(PrimitiveType.Triangles, 0, entity.VertexData.Length / 4);
        }

        public void UnloadGL()
        {
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);
            GL.DeleteProgram(_shaderProgram);
            GL.DeleteShader(_vertexShader);
            GL.DeleteShader(_fragmentShader);
        }
    }
}