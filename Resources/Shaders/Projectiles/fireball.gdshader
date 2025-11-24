shader_type canvas_item;

// Fireball Projectile - Projectile Effect
// A fiery orb with flickering flames and ember trail

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 head_color : source_color = vec4(1.0, 0.5, 0.1, 1.0);
uniform vec4 trail_color : source_color = vec4(1.0, 0.3, 0.0, 0.6);
uniform float size : hint_range(0.5, 3.0) = 1.0;
uniform int trail_length : hint_range(0, 10) = 4;
uniform vec2 direction = vec2(1.0, 0.0);

// Pseudo-random hash
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

// Fractal Brownian Motion
float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 4; i++) {
        value += amplitude * noise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float dist = length(uv) * 2.0;

    // Animate noise for flickering
    float time = progress * 20.0;
    float angle = atan(uv.y, uv.x);

    // Core fireball with noise distortion
    float noise_val = fbm(vec2(angle * 3.0, time * 2.0)) * 0.3;
    float distorted_dist = dist + noise_val * (0.3 + dist * 0.5);

    // Size factor
    float base_radius = 0.4 * size;

    // Hot core (white-yellow)
    float core_intensity = 1.0 - smoothstep(0.0, base_radius * 0.4, distorted_dist);
    vec4 core_color = vec4(1.0, 1.0, 0.9, 1.0);

    // Middle fire (orange)
    float mid_intensity = 1.0 - smoothstep(base_radius * 0.3, base_radius * 0.7, distorted_dist);

    // Outer flames (red-orange)
    float outer_intensity = 1.0 - smoothstep(base_radius * 0.5, base_radius * 1.2, distorted_dist);

    // Combine layers
    vec4 color = vec4(0.0);
    color = mix(color, vec4(0.8, 0.2, 0.0, outer_intensity * 0.8), outer_intensity);
    color = mix(color, head_color, mid_intensity);
    color = mix(color, core_color, core_intensity);

    // Add flickering brightness
    float flicker = fbm(vec2(time * 3.0, 0.0)) * 0.2 + 0.9;
    color.rgb *= flicker;

    // Soft edge glow
    float glow = exp(-distorted_dist * 3.0) * 0.5;
    color.rgb += head_color.rgb * glow;

    // Trail effect - elongate in opposite of movement direction
    vec2 trail_uv = uv + direction * 0.2 * float(trail_length) * 0.1;
    float trail_dist = length(trail_uv) * 2.0;
    float trail_intensity = (1.0 - smoothstep(0.0, base_radius * 1.5, trail_dist)) * 0.4;
    trail_intensity *= fbm(vec2(angle * 2.0, time)) * 0.5 + 0.5;
    color.rgb += trail_color.rgb * trail_intensity;
    color.a = max(color.a, trail_intensity * trail_color.a);

    // Final alpha
    color.a = clamp(color.a, 0.0, 1.0);

    COLOR = color;
}
