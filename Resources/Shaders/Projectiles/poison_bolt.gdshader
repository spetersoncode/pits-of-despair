shader_type canvas_item;

// Poison Bolt Projectile - Projectile Effect
// A toxic glob with dripping poison trail

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 head_color : source_color = vec4(0.45, 0.85, 0.4, 1.0);
uniform vec4 trail_color : source_color = vec4(0.45, 0.85, 0.4, 0.5);
uniform float size : hint_range(0.5, 3.0) = 1.0;
uniform int trail_length : hint_range(0, 10) = 5;
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
    float time = progress * 12.0;

    // Rotate UV to align with direction
    float angle = atan(direction.y, direction.x);
    float cos_a = cos(-angle);
    float sin_a = sin(-angle);
    vec2 rotated_uv = vec2(
        uv.x * cos_a - uv.y * sin_a,
        uv.x * sin_a + uv.y * cos_a
    );

    // Glob dimensions
    float glob_radius = 0.18 * size;
    float dist = length(uv) * 2.0;

    // Animate the glob shape with bubbling distortion
    float glob_angle = atan(uv.y, uv.x);
    float bubble = fbm(vec2(glob_angle * 3.0 + time, dist * 5.0)) * 0.15;
    float distorted_dist = dist + bubble * (0.5 + dist);

    // Main glob body
    float glob = 1.0 - smoothstep(glob_radius * 0.7, glob_radius * 1.1, distorted_dist);

    // Darker core (concentrated poison)
    float core = 1.0 - smoothstep(0.0, glob_radius * 0.4, distorted_dist);

    // Bubbles on surface
    float bubbles = 0.0;
    for (int i = 0; i < 4; i++) {
        vec2 bubble_pos = vec2(
            cos(float(i) * 1.57 + time * 0.5) * glob_radius * 0.5,
            sin(float(i) * 1.57 + time * 0.5) * glob_radius * 0.5
        );
        float bubble_dist = length(uv - bubble_pos);
        float bubble_size = 0.02 + hash(vec2(float(i), 0.0)) * 0.02;
        bubbles += (1.0 - smoothstep(0.0, bubble_size, bubble_dist)) * 0.5;
    }
    bubbles *= glob;

    // Dripping trail
    float trail = 0.0;
    if (trail_length > 0) {
        // Main drip stream
        float trail_extent = -0.4 * float(trail_length) * 0.12;
        float trail_x = smoothstep(trail_extent, 0.0, rotated_uv.x);

        // Wavy drip pattern
        float drip_wave = sin(rotated_uv.x * 15.0 - time * 2.0) * 0.02;
        float trail_width = glob_radius * 0.5;
        float trail_y = 1.0 - smoothstep(0.0, trail_width, abs(rotated_uv.y - drip_wave));

        trail = trail_x * trail_y * 0.5 * (1.0 - glob);

        // Individual drip droplets
        for (int i = 0; i < 3; i++) {
            float droplet_x = -0.1 - float(i) * 0.08 - hash(vec2(float(i), time * 0.1)) * 0.05;
            float droplet_y = sin(float(i) * 2.0 + time * 0.3) * 0.03;
            vec2 droplet_pos = vec2(droplet_x, droplet_y);

            // Transform to rotated space
            vec2 droplet_rotated = vec2(
                droplet_pos.x * cos_a + droplet_pos.y * sin_a,
                -droplet_pos.x * sin_a + droplet_pos.y * cos_a
            );

            float droplet_dist = length(uv - droplet_rotated);
            float droplet_size = 0.025 + hash(vec2(float(i), 123.0)) * 0.015;
            float droplet = (1.0 - smoothstep(0.0, droplet_size, droplet_dist)) * 0.7;
            trail = max(trail, droplet * (1.0 - glob));
        }
    }

    // Toxic vapor/mist
    float mist_dist = length(uv) * 1.5;
    float mist = fbm(vec2(glob_angle * 2.0 - time * 0.5, mist_dist * 3.0));
    mist *= (1.0 - smoothstep(glob_radius, glob_radius * 2.5, dist)) * 0.3;

    // Color composition
    vec4 color = vec4(0.0);
    vec4 core_color = vec4(0.2, 0.5, 0.15, 1.0);  // Dark poison green
    vec4 bright_color = vec4(0.6, 1.0, 0.5, 1.0);  // Bright toxic green

    // Compose layers
    color = mix(color, trail_color, trail);
    color = mix(color, head_color * 0.6, mist);
    color = mix(color, head_color, glob);
    color = mix(color, core_color, core * 0.7);
    color = mix(color, bright_color, bubbles);

    // Sickly glow
    float glow = exp(-dist * 3.0) * 0.25;
    color.rgb += head_color.rgb * glow;

    // Pulsing effect
    float pulse = sin(time * 2.0) * 0.1 + 0.95;
    color.rgb *= pulse;

    COLOR = color;
}
