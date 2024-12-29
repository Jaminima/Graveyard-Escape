#version 330

uniform vec4 entityColour;
out vec4 outputColor;

void main(void)
{
    outputColor = entityColour;
}