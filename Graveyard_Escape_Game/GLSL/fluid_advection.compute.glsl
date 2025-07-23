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
    
    vec2 flowMomentum = tile.xy;
    float pressure = tile.z;
    
    // Calculate source position for advection
    vec2 sourcePos = vec2(coords) - flowMomentum * deltaTime;
    
    // Clamp to valid range
    sourcePos = clamp(sourcePos, vec2(0.0), vec2(sceneWidth - 1.001, sceneHeight - 1.001));
    
    // Bilinear interpolation coordinates
    ivec2 p0 = ivec2(floor(sourcePos));
    ivec2 p1 = p0 + ivec2(1, 1);
    vec2 frac = sourcePos - vec2(p0);
    
    // Ensure p1 is within bounds
    p1 = min(p1, ivec2(sceneWidth - 1, sceneHeight - 1));
    
    // Sample the four neighboring tiles
    int idx00 = p0.y * sceneWidth + p0.x;
    int idx10 = p0.y * sceneWidth + p1.x;
    int idx01 = p1.y * sceneWidth + p0.x;
    int idx11 = p1.y * sceneWidth + p1.x;
    
    vec4 tile00 = inputTiles[idx00];
    vec4 tile10 = inputTiles[idx10];
    vec4 tile01 = inputTiles[idx01];
    vec4 tile11 = inputTiles[idx11];
    
    // Bilinear interpolation
    vec4 top = mix(tile00, tile10, frac.x);
    vec4 bottom = mix(tile01, tile11, frac.x);
    vec4 advectedTile = mix(top, bottom, frac.y);
    
    outputTiles[index] = advectedTile;
}