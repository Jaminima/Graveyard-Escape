using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Graveyard_Escape_Lib.Types;
using OpenTK.Graphics.OpenGL;

namespace Graveyard_Escape_Game.Renderers
{
    public class EntityRenderer
    {
        private int _vertexShader;
        private int _fragmentShader;
        private int _shaderProgram;
        private int _vertexBufferObject;
        private int _vertexArrayObject;

        // Shader sources
        private const string VertexShaderSource = @"
            #version 330

            layout(location = 0) in vec4 position;
            uniform vec2 entityPosition;

            void main(void)
            {
                gl_Position = position + vec4(entityPosition, 1.0, 1.0);
            }
        ";

        private const string FragmentShaderSource = @"
            #version 330

            out vec4 outputColor;

            void main(void)
            {
                outputColor = vec4(1.0, 1.0, 0.0, 1.0);
            }
        ";

        // Triangle vertices
        private readonly float[] _points = {
            -0.5f, 0.0f, 0.0f, 1.0f,
            0.5f, 0.0f, 0.0f, 1.0f,
            0.0f, 0.5f, 0.0f, 1.0f
        };

        public void InitGL()
        {
            // Compile shaders
            _vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(_vertexShader, VertexShaderSource);
            GL.CompileShader(_vertexShader);

            _fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(_fragmentShader, FragmentShaderSource);
            GL.CompileShader(_fragmentShader);

            // Create shader program
            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, _vertexShader);
            GL.AttachShader(_shaderProgram, _fragmentShader);
            GL.LinkProgram(_shaderProgram);

            // Create VBO
            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _points.Length * sizeof(float), _points, BufferUsageHint.StaticDraw);

            // Create VAO
            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);
            var positionLocation = GL.GetAttribLocation(_shaderProgram, "position");
            GL.VertexAttribPointer(positionLocation, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(positionLocation);
        }

        public void RenderGL(Entity entity)
        {
            // Bind and use shader program
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BindVertexArray(_vertexArrayObject);
            GL.UseProgram(_shaderProgram);

            // Set the entity position uniform
            int entityPositionLocation = GL.GetUniformLocation(_shaderProgram, "entityPosition");
            GL.Uniform2(entityPositionLocation, entity.Position.X, entity.Position.Y);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
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