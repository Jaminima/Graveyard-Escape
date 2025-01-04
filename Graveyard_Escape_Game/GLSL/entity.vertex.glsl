#version 330

layout(location = 0) in vec4 position;
uniform vec2 entityPosition;
uniform float entityScale;
uniform float entityRotation;
uniform float sceneZoom;

void main(void)
{
    float cosTheta = cos(entityRotation);
    float sinTheta = sin(entityRotation);
    mat2 rotationMatrix = mat2(cosTheta, -sinTheta, sinTheta, cosTheta);
    vec2 scaledPosition = position.xy * entityScale;
    vec2 rotatedPosition = rotationMatrix * scaledPosition;
    gl_Position = vec4((rotatedPosition + entityPosition) * sceneZoom, position.zw);
}