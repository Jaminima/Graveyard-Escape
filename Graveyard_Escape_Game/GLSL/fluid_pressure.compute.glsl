#version 430

layout(local_size_x = 16, local_size_y = 16) in;

layout(std430, binding = 0) restrict readonly buffer InputBuffer {
    vec4 inputTiles[]; // xy = FlowMomentum, z = Pressure, w = unused
};

layout(std430, binding = 1) restrict writeonly buffer OutputBuffer {
    vec4 outputTiles[];
};

uniform int sceneWidth;
uniform int sceneHeight;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    
    if (coords.x >= sceneWidth || coords.y >= sceneHeight) {
        return;
    }
    
    int index = coords.y * sceneWidth + coords.x;
    vec4 tile = inputTiles[index];
    
    // Calculate divergence from momentum field
    float m_right = 0.0;
    float m_left = 0.0;
    float m_down = 0.0;
    float m_up = 0.0;
    
    // Right neighbor
    if (coords.x < sceneWidth - 1) {
        m_right = inputTiles[index + 1].x;
    }
    
    // Left neighbor
    if (coords.x > 0) {
        m_left = inputTiles[index - 1].x;
    }
    
    // Bottom neighbor
    if (coords.y < sceneHeight - 1) {
        m_down = inputTiles[index + sceneWidth].y;
    }
    
    // Top neighbor
    if (coords.y > 0) {
        m_up = inputTiles[index - sceneWidth].y;
    }
    
    float divergence = m_right - m_left + m_down - m_up;
    
    // Update pressure based on divergence
    float pressureCorrection = divergence * 0.1;
    float newPressure = max(0.0, tile.z - pressureCorrection);
    
    outputTiles[index] = vec4(tile.xy, newPressure, tile.w);
}