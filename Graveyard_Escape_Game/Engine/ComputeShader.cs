using OpenTK.Graphics.OpenGL;
using System;
using System.IO;

namespace Graveyard_Escape_Game.Engine
{
    public class ComputeShader : IDisposable
    {
        public int Handle { get; private set; }
        private bool _disposed = false;

        public ComputeShader(string shaderPath)
        {
            string shaderSource = File.ReadAllText(shaderPath);
            
            int computeShader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(computeShader, shaderSource);
            GL.CompileShader(computeShader);
            
            string infoLog = GL.GetShaderInfoLog(computeShader);
            if (!string.IsNullOrEmpty(infoLog))
            {
                Console.WriteLine($"Compute shader compilation log for {shaderPath}: {infoLog}");
            }
            
            int success;
            GL.GetShader(computeShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                throw new Exception($"Compute shader compilation failed for {shaderPath}: {infoLog}");
            }
            
            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, computeShader);
            GL.LinkProgram(Handle);
            
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string programInfoLog = GL.GetProgramInfoLog(Handle);
                throw new Exception($"Compute shader program linking failed for {shaderPath}: {programInfoLog}");
            }
            
            GL.DetachShader(Handle, computeShader);
            GL.DeleteShader(computeShader);
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public void SetUniform(string name, int value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1)
            {
                GL.Uniform1(location, value);
            }
        }

        public void SetUniform(string name, float value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1)
            {
                GL.Uniform1(location, value);
            }
        }

        public void Dispatch(int numGroupsX, int numGroupsY, int numGroupsZ)
        {
            GL.DispatchCompute(numGroupsX, numGroupsY, numGroupsZ);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteProgram(Handle);
                _disposed = true;
            }
        }
    }
}