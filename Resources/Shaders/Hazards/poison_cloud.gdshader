shader_type canvas_item;

// Poison Cloud - Persistent Tile Hazard
// Creates a swirling, semi-transparent toxic cloud effect

uniform vec4 hazard_color : source_color = vec4(0.47, 0.87, 0.4, 0.6);  // Poison green
uniform float time : hint_range(0.0, 1000.0) = 0.0;

// Simple pseudo-random hash function
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Value noise
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);

    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Fractal Brownian Motion for layered noise
float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < 4; i++) {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }

    return value;
}

void fragment() {
    vec2 uv = UV;
    vec2 center = vec2(0.5, 0.5);

    // Calculate distance from center for circular shape
    float dist = length(uv - center) * 2.0;

    // Animated time for swirling effect
    float t = time * 0.5;

    // Create swirling motion
    float angle = atan(uv.y - 0.5, uv.x - 0.5);
    vec2 swirl_uv = vec2(
        cos(angle + t * 0.3) * dist,
        sin(angle + t * 0.3) * dist
    );

    // Layered noise for cloud effect
    float cloud1 = fbm(uv * 4.0 + vec2(t * 0.2, t * 0.1));
    float cloud2 = fbm(uv * 6.0 - vec2(t * 0.15, t * 0.25));
    float cloud3 = fbm(swirl_uv * 3.0 + vec2(t * 0.1, -t * 0.1));

    // Combine clouds
    float cloud = (cloud1 + cloud2 * 0.7 + cloud3 * 0.5) / 2.2;

    // Pulsing intensity
    float pulse = 0.8 + 0.2 * sin(t * 2.0);

    // Create soft circular boundary
    float edge = 1.0 - smoothstep(0.3, 0.95, dist);

    // Final color with transparency
    vec4 color = hazard_color;
    color.rgb *= 0.8 + cloud * 0.4;  // Vary brightness with cloud pattern
    color.rgb *= pulse;

    // Alpha based on cloud density and edge
    color.a = hazard_color.a * edge * (0.5 + cloud * 0.5);

    // Add subtle glow at edges
    float glow = exp(-dist * 3.0) * 0.3;
    color.rgb += hazard_color.rgb * glow;

    COLOR = color;
}
