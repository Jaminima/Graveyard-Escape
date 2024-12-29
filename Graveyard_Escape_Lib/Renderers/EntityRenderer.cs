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
            uniform float entityScale;
            uniform float entityRotation;

            void main(void)
            {
                float cosTheta = cos(entityRotation);
                float sinTheta = sin(entityRotation);
                mat2 rotationMatrix = mat2(cosTheta, -sinTheta, sinTheta, cosTheta);
                vec2 rotatedPosition = rotationMatrix * position.xy;
                gl_Position = vec4(rotatedPosition * entityScale + entityPosition, position.zw);
            }
        ";

        private const string FragmentShaderSource = @"
            #version 330

            uniform vec4 entityColour;
            out vec4 outputColor;

            void main(void)
            {
                outputColor = entityColour;
            }
        ";

        // Quad vertices using two triangles
        private readonly float[] _points = {
            // First triangle
            -0.5f, -0.5f, 0.0f, 1.0f,
            0.5f, -0.5f, 0.0f, 1.0f,
            0.5f, 0.5f, 0.0f, 1.0f,
            // Second triangle
            0.5f, 0.5f, 0.0f, 1.0f,
            -0.5f, 0.5f, 0.0f, 1.0f,
            -0.5f, -0.5f, 0.0f, 1.0f
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

            // Set the entity position and scale uniforms
            int entityPositionLocation = GL.GetUniformLocation(_shaderProgram, "entityPosition");
            GL.Uniform2(entityPositionLocation, entity.Position.X, entity.Position.Y);

            int entityScaleLocation = GL.GetUniformLocation(_shaderProgram, "entityScale");
            GL.Uniform1(entityScaleLocation, entity.Scale);

            // Set the entity rotation uniform
            int entityRotationLocation = GL.GetUniformLocation(_shaderProgram, "entityRotation");
            GL.Uniform1(entityRotationLocation, entity.Rotation);

            // Set the entity colour uniform
            int entityColourLocation = GL.GetUniformLocation(_shaderProgram, "entityColour");
            GL.Uniform4(entityColourLocation, entity.Colour.X, entity.Colour.Y, entity.Colour.Z, entity.Colour.W);

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
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