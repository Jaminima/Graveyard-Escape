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
uniform float deltaTime;

void main() {
    ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
    
    if (coords.x >= sceneWidth || coords.y >= sceneHeight) {
        return;
    }
    
    int index = coords.y * sceneWidth + coords.x;
    vec4 tile = inputTiles[index];
    
    // Get pressure from neighbors
    float p_right = tile.z;
    float p_left = tile.z;
    float p_down = tile.z;
    float p_up = tile.z;
    
    if (coords.x < sceneWidth - 1) {
        p_right = inputTiles[index + 1].z;
    }
    
    if (coords.x > 0) {
        p_left = inputTiles[index - 1].z;
    }
    
    if (coords.y < sceneHeight - 1) {
        p_down = inputTiles[index + sceneWidth].z;
    }
    
    if (coords.y > 0) {
        p_up = inputTiles[index - sceneWidth].z;
    }
    
    // Calculate pressure gradient
    vec2 gradient = vec2(p_right - p_left, p_down - p_up);
    
    // Update momentum from pressure gradient
    vec2 newFlowMomentum = tile.xy - gradient * deltaTime;
    
    // Apply damping/viscosity
    newFlowMomentum *= 0.995;
    
    // Enforce boundary conditions (no-slip walls)
    if (coords.x == 0 || coords.x == sceneWidth - 1) {
        newFlowMomentum.x = 0.0;
    }
    
    if (coords.y == 0 || coords.y == sceneHeight - 1) {
        newFlowMomentum.y = 0.0;
    }
    
    outputTiles[index] = vec4(newFlowMomentum, tile.z, tile.w);
}