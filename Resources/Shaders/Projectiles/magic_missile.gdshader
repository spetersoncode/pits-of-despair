shader_type canvas_item;

// Magic Missile Projectile - Projectile Effect
// An arcane diamond of energy with sparkle trail

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 head_color : source_color = vec4(0.0, 0.75, 0.9, 1.0);
uniform vec4 trail_color : source_color = vec4(0.0, 0.75, 0.9, 0.4);
uniform float size : hint_range(0.5, 3.0) = 1.0;
uniform int trail_length : hint_range(0, 10) = 3;
uniform vec2 direction = vec2(1.0, 0.0);

// Pseudo-random hash
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
    float time = progress * 15.0;

    // Rotate UV to align with direction
    float angle = atan(direction.y, direction.x);
    float cos_a = cos(-angle);
    float sin_a = sin(-angle);
    vec2 rotated_uv = vec2(
        uv.x * cos_a - uv.y * sin_a,
        uv.x * sin_a + uv.y * cos_a
    );

    // Diamond shape (rotated square)
    float diamond_size = 0.2 * size;
    float diamond_dist = abs(rotated_uv.x) + abs(rotated_uv.y);

    // Core diamond
    float core = 1.0 - smoothstep(diamond_size * 0.3, diamond_size * 0.5, diamond_dist);

    // Inner glow
    float inner_glow = 1.0 - smoothstep(diamond_size * 0.4, diamond_size * 0.8, diamond_dist);

    // Outer energy field
    float outer = 1.0 - smoothstep(diamond_size * 0.6, diamond_size * 1.2, diamond_dist);

    // Pulsing effect
    float pulse = sin(time * 3.0) * 0.15 + 0.85;

    // Energy sparkles around the diamond
    float sparkle_angle = atan(uv.y, uv.x);
    float sparkle = noise(vec2(sparkle_angle * 5.0 + time, diamond_dist * 10.0));
    sparkle = pow(sparkle, 3.0) * outer * 0.8;

    // Color composition
    vec4 color = vec4(0.0);

    // White-cyan core
    vec4 core_color = vec4(0.9, 1.0, 1.0, 1.0);

    // Trail particles
    float trail = 0.0;
    if (trail_length > 0) {
        // Elongated trail in opposite direction
        float trail_x = smoothstep(-0.4 * float(trail_length) * 0.15, 0.0, rotated_uv.x);
        float trail_width = diamond_size * 0.6;
        float trail_y = 1.0 - smoothstep(trail_width * 0.3, trail_width, abs(rotated_uv.y));

        // Add sparkle particles to trail
        float trail_sparkle = noise(vec2(rotated_uv.x * 20.0 - time * 2.0, rotated_uv.y * 20.0));
        trail_sparkle = pow(trail_sparkle, 4.0);

        trail = trail_x * trail_y * (0.3 + trail_sparkle * 0.5) * (1.0 - core);
    }

    // Compose final color
    color = mix(color, trail_color, trail);
    color = mix(color, head_color * pulse, outer);
    color = mix(color, head_color * 1.2, inner_glow);
    color = mix(color, core_color, core);

    // Add sparkles
    color.rgb += head_color.rgb * sparkle;

    // Soft outer glow
    float glow_dist = length(uv) * 2.0;
    float soft_glow = exp(-glow_dist * 4.0) * 0.3;
    color.rgb += head_color.rgb * soft_glow;

    COLOR = color;
}
