shader_type canvas_item;

// Ice Shard Projectile - Projectile Effect
// A crystalline ice shard with frost trail

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 head_color : source_color = vec4(0.4, 0.7, 0.9, 1.0);
uniform vec4 trail_color : source_color = vec4(0.4, 0.7, 0.9, 0.3);
uniform float size : hint_range(0.5, 3.0) = 1.0;
uniform int trail_length : hint_range(0, 10) = 3;
uniform vec2 direction = vec2(1.0, 0.0);

// Pseudo-random
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

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

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float time = progress * 10.0;

    // Rotate UV to align with direction
    float angle = atan(direction.y, direction.x);
    float cos_a = cos(-angle);
    float sin_a = sin(-angle);
    vec2 rotated_uv = vec2(
        uv.x * cos_a - uv.y * sin_a,
        uv.x * sin_a + uv.y * cos_a
    );

    // Elongated diamond shape (shard)
    float shard_length = 0.3 * size;
    float shard_width = 0.12 * size;

    // Create shard shape - elongated in direction of travel
    float shard_x = abs(rotated_uv.x) / shard_length;
    float shard_y = abs(rotated_uv.y) / shard_width;
    float shard_dist = shard_x + shard_y;

    // Sharper tip in front
    float tip_factor = smoothstep(-0.1, 0.2, rotated_uv.x);
    shard_dist += tip_factor * shard_y * 0.5;

    // Core crystal
    float core = 1.0 - smoothstep(0.6, 0.8, shard_dist);

    // Crystal facets (internal reflections)
    float facet1 = abs(sin(rotated_uv.x * 40.0 + rotated_uv.y * 20.0)) * core;
    float facet2 = abs(sin(rotated_uv.x * 30.0 - rotated_uv.y * 25.0)) * core;
    float facets = (facet1 + facet2) * 0.15;

    // Outer frost aura
    float aura = 1.0 - smoothstep(0.7, 1.3, shard_dist);
    float frost_noise = noise(vec2(rotated_uv.x * 15.0, rotated_uv.y * 15.0 + time));
    aura *= 0.5 + frost_noise * 0.5;

    // Frost particle trail
    float trail = 0.0;
    if (trail_length > 0) {
        float trail_extent = -0.35 * float(trail_length) * 0.15;
        float trail_x = smoothstep(trail_extent, 0.0, rotated_uv.x);
        float trail_width = shard_width * 0.8;
        float trail_y = 1.0 - smoothstep(trail_width * 0.3, trail_width, abs(rotated_uv.y));

        // Frost particles
        float particles = noise(vec2(rotated_uv.x * 25.0 - time * 1.5, rotated_uv.y * 25.0));
        particles = pow(particles, 3.0);

        trail = trail_x * trail_y * (0.2 + particles * 0.4) * (1.0 - core);
    }

    // Color composition
    vec4 color = vec4(0.0);

    // Bright ice core
    vec4 core_color = vec4(0.85, 0.95, 1.0, 1.0);

    // Compose layers
    color = mix(color, trail_color, trail);
    color = mix(color, head_color * 0.7, aura);
    color = mix(color, head_color, core);
    color = mix(color, core_color, core * 0.6);

    // Add facet highlights
    color.rgb += vec3(0.8, 0.9, 1.0) * facets;

    // Edge highlight for crystalline look
    float edge = smoothstep(0.5, 0.7, shard_dist) * (1.0 - smoothstep(0.7, 0.9, shard_dist));
    color.rgb += vec3(0.7, 0.85, 1.0) * edge * 0.4;

    // Cold shimmer
    float shimmer = sin(time * 5.0 + rotated_uv.x * 20.0) * 0.1 + 0.9;
    color.rgb *= shimmer;

    COLOR = color;
}
