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