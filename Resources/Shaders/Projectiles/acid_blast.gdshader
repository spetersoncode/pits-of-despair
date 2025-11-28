shader_type canvas_item;

// Acid Blast Projectile - Aggressive Corrosive Effect
// A volatile, bubbling mass of caustic acid with sizzling trail

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 head_color : source_color = vec4(0.6, 1.0, 0.3, 1.0);
uniform vec4 trail_color : source_color = vec4(0.6, 1.0, 0.3, 0.5);
uniform float size : hint_range(0.5, 3.0) = 1.2;
uniform int trail_length : hint_range(0, 10) = 4;
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
    for (int i = 0; i < 5; i++) {
        value += amplitude * noise(p);
        p *= 2.2;
        amplitude *= 0.45;
    }
    return value;
}

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float time = progress * 18.0;  // Faster animation than poison

    // Rotate UV to align with direction
    float angle = atan(direction.y, direction.x);
    float cos_a = cos(-angle);
    float sin_a = sin(-angle);
    vec2 rotated_uv = vec2(
        uv.x * cos_a - uv.y * sin_a,
        uv.x * sin_a + uv.y * cos_a
    );

    // Main acid mass - more volatile shape
    float acid_radius = 0.16 * size;
    float dist = length(uv) * 2.0;

    // Aggressive bubbling distortion - faster and more chaotic
    float acid_angle = atan(uv.y, uv.x);
    float bubble1 = fbm(vec2(acid_angle * 4.0 + time * 1.5, dist * 6.0)) * 0.2;
    float bubble2 = noise(vec2(acid_angle * 8.0 - time * 2.5, dist * 10.0)) * 0.1;
    float distorted_dist = dist + (bubble1 + bubble2) * (0.4 + dist);

    // Main acid body with eating-away edges
    float acid = 1.0 - smoothstep(acid_radius * 0.6, acid_radius * 1.2, distorted_dist);

    // Hot caustic core - brighter center
    float core = 1.0 - smoothstep(0.0, acid_radius * 0.35, distorted_dist);

    // Violent surface bubbles - more and faster
    float bubbles = 0.0;
    for (int i = 0; i < 6; i++) {
        float bubble_time = time * (0.8 + float(i) * 0.15);
        vec2 bubble_pos = vec2(
            cos(float(i) * 1.05 + bubble_time) * acid_radius * 0.55,
            sin(float(i) * 1.05 + bubble_time) * acid_radius * 0.55
        );
        float bubble_dist = length(uv - bubble_pos);
        float bubble_size = 0.018 + hash(vec2(float(i), 0.0)) * 0.025;
        // Bubbles pop and reform
        float pop = sin(bubble_time * 2.0 + float(i)) * 0.5 + 0.5;
        bubbles += (1.0 - smoothstep(0.0, bubble_size, bubble_dist)) * 0.6 * pop;
    }
    bubbles *= acid;

    // Sizzling dripping trail - more aggressive
    float trail = 0.0;
    if (trail_length > 0) {
        float trail_extent = -0.45 * float(trail_length) * 0.15;
        float trail_x = smoothstep(trail_extent, 0.0, rotated_uv.x);

        // Chaotic sizzle pattern
        float sizzle = sin(rotated_uv.x * 25.0 - time * 4.0) * 0.025;
        sizzle += noise(vec2(rotated_uv.x * 15.0 - time * 3.0, rotated_uv.y * 10.0)) * 0.02;
        float trail_width = acid_radius * 0.55;
        float trail_y = 1.0 - smoothstep(0.0, trail_width, abs(rotated_uv.y - sizzle));

        trail = trail_x * trail_y * 0.6 * (1.0 - acid);

        // Splashing droplets - more scattered
        for (int i = 0; i < 5; i++) {
            float droplet_phase = hash(vec2(float(i), 456.0));
            float droplet_x = -0.08 - float(i) * 0.07 - droplet_phase * 0.06;
            float droplet_y = sin(float(i) * 2.5 + time * 0.5 + droplet_phase * 6.28) * 0.045;
            vec2 droplet_pos = vec2(droplet_x, droplet_y);

            // Transform to rotated space
            vec2 droplet_rotated = vec2(
                droplet_pos.x * cos_a + droplet_pos.y * sin_a,
                -droplet_pos.x * sin_a + droplet_pos.y * cos_a
            );

            float droplet_dist = length(uv - droplet_rotated);
            float droplet_size = 0.02 + hash(vec2(float(i), 789.0)) * 0.018;
            float droplet = (1.0 - smoothstep(0.0, droplet_size, droplet_dist)) * 0.8;
            trail = max(trail, droplet * (1.0 - acid));
        }
    }

    // Caustic vapor - more intense
    float mist_dist = length(uv) * 1.3;
    float mist = fbm(vec2(acid_angle * 3.0 - time * 0.8, mist_dist * 4.0));
    mist *= (1.0 - smoothstep(acid_radius, acid_radius * 2.2, dist)) * 0.35;

    // Color composition
    vec4 color = vec4(0.0);
    vec4 core_color = vec4(0.95, 1.0, 0.7, 1.0);  // Hot white-yellow core
    vec4 bright_color = vec4(0.7, 1.0, 0.4, 1.0);  // Bright caustic green
    vec4 dark_acid = vec4(0.3, 0.6, 0.15, 1.0);  // Dark concentrated acid

    // Compose layers
    color = mix(color, trail_color, trail);
    color = mix(color, head_color * 0.7, mist);
    color = mix(color, head_color, acid);
    color = mix(color, dark_acid, core * 0.5);
    color = mix(color, core_color, core * 0.8);
    color = mix(color, bright_color, bubbles);

    // Intense caustic glow
    float glow = exp(-dist * 2.5) * 0.4;
    color.rgb += head_color.rgb * glow;

    // Erratic pulsing - unstable energy
    float pulse1 = sin(time * 3.5) * 0.08 + 0.92;
    float pulse2 = sin(time * 7.0 + 1.0) * 0.05;
    color.rgb *= pulse1 + pulse2;

    // Extra brightness flicker for volatility
    float flicker = noise(vec2(time * 5.0, 0.0)) * 0.15;
    color.rgb += head_color.rgb * flicker * acid;

    COLOR = color;
}
