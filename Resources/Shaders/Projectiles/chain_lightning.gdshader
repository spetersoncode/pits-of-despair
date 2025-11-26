shader_type canvas_item;

// Chain Lightning - CRACKLING ELECTRIC ARC
// Used for chain lightning bounces (Spark projectile)
// Intense crackling bolt with violent branching and electric chaos

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 head_color : source_color = vec4(1.0, 1.0, 0.4, 1.0);
uniform vec4 trail_color : source_color = vec4(1.0, 1.0, 0.4, 0.5);
uniform float size : hint_range(0.5, 3.0) = 1.0;
uniform int trail_length : hint_range(0, 10) = 5;
uniform vec2 direction = vec2(1.0, 0.0);

// Fast hash
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

// Aggressive jagged displacement
float lightning_offset(float x, float time, float seed) {
    float offset = 0.0;
    float amplitude = 0.12;
    float frequency = 6.0;

    for (int i = 0; i < 4; i++) {
        offset += (noise(vec2(x * frequency + time * 5.0 + seed, float(i) + seed)) - 0.5) * amplitude;
        amplitude *= 0.55;
        frequency *= 2.2;
    }
    return offset;
}

// Sharp step for crisp edges
float sharp_step(float edge, float value) {
    return 1.0 - smoothstep(edge * 0.6, edge, value);
}

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float time = progress * 40.0;

    // Rotate UV to align with direction
    float angle = atan(direction.y, direction.x);
    float cos_a = cos(-angle);
    float sin_a = sin(-angle);
    vec2 rotated_uv = vec2(
        uv.x * cos_a - uv.y * sin_a,
        uv.x * sin_a + uv.y * cos_a
    );

    // Bolt dimensions
    float bolt_length = 0.4 * size;
    float bolt_width = 0.05 * size;

    // AGGRESSIVE flicker
    float flicker = 1.0;
    flicker *= noise(vec2(time * 12.0, 0.0)) * 0.4 + 0.6;
    flicker *= noise(vec2(time * 20.0 + 100.0, 0.0)) * 0.3 + 0.7;
    flicker *= step(0.15, noise(vec2(time * 25.0, 50.0)));  // Random blackouts
    flicker = max(flicker, 0.5);

    // Main lightning bolt with jagged displacement
    float lightning_y = lightning_offset(rotated_uv.x * 12.0, time, 0.0);
    float dist_from_bolt = abs(rotated_uv.y - lightning_y);

    // Bolt visibility along length
    float bolt_x = smoothstep(-bolt_length, -bolt_length + 0.03, rotated_uv.x) *
                  (1.0 - smoothstep(bolt_length - 0.03, bolt_length, rotated_uv.x));

    // Core bolt (bright white-yellow)
    float core = sharp_step(bolt_width * 0.3, dist_from_bolt) * bolt_x;

    // Inner glow
    float inner = sharp_step(bolt_width * 0.8, dist_from_bolt) * bolt_x;

    // Outer electric field
    float outer = sharp_step(bolt_width * 2.0, dist_from_bolt) * bolt_x;

    // === BRANCHING SPARKS - More aggressive ===
    float sparks = 0.0;
    for (int i = 0; i < 6; i++) {
        float spark_seed = float(i) * 123.45 + floor(time);
        float spark_x = (hash(vec2(spark_seed, 67.89)) - 0.5) * bolt_length * 1.8;
        float spark_dir = hash(vec2(float(i) * 234.56 + floor(time * 2.0), 78.9)) > 0.5 ? 1.0 : -1.0;
        float spark_len = 0.06 + hash(vec2(float(i) * 345.67, 89.01)) * 0.1;

        if (abs(rotated_uv.x - spark_x) < spark_len) {
            float local_x = (rotated_uv.x - spark_x) / spark_len;
            float spark_y_offset = lightning_offset(spark_x * 12.0, time, 0.0);
            float expected_y = spark_y_offset + local_x * spark_dir * spark_len * 2.5;
            expected_y += lightning_offset(rotated_uv.x * 20.0 + float(i) * 50.0, time * 1.5, float(i)) * 0.3;

            float spark_dist = abs(rotated_uv.y - expected_y);
            float taper = 1.0 - abs(local_x);
            float spark_intensity = sharp_step(bolt_width * 0.5 * taper, spark_dist) * taper;

            // Sparks flicker independently
            float spark_flicker = step(0.4, noise(vec2(time * 30.0 + float(i) * 40.0, 0.0)));
            sparks = max(sparks, spark_intensity * spark_flicker * 0.8);
        }
    }

    // === ELECTRIC PARTICLES ===
    float particles = 0.0;
    float particle_field = noise(vec2(rotated_uv.x * 50.0 - time * 6.0, rotated_uv.y * 50.0 + time * 4.0));
    particle_field = pow(particle_field, 5.0);
    float particle_mask = bolt_x * (1.0 - smoothstep(0.0, bolt_width * 4.0, abs(rotated_uv.y - lightning_y)));
    particles = particle_field * particle_mask * 0.5;

    // Trail (electric afterglow)
    float trail = 0.0;
    if (trail_length > 0) {
        float trail_extent = bolt_length * (1.0 + float(trail_length) * 0.25);
        float trail_x = smoothstep(-trail_extent, -bolt_length * 0.3, rotated_uv.x);
        float trail_y = 1.0 - smoothstep(0.0, bolt_width * 4.0, abs(rotated_uv.y - lightning_y));
        trail = trail_x * trail_y * 0.35 * (1.0 - core) * flicker;

        // Electric particles in trail
        float trail_particles = noise(vec2(rotated_uv.x * 35.0 - time * 4.0, rotated_uv.y * 35.0));
        trail_particles = pow(trail_particles, 4.0);
        trail += trail_particles * trail_x * 0.5;
    }

    // === COLOR COMPOSITION ===
    vec4 color = vec4(0.0);
    vec4 core_color = vec4(1.0, 1.0, 1.0, 1.0);
    vec4 electric_blue = vec4(0.5, 0.75, 1.0, 1.0);

    // Layer composition
    color = mix(color, trail_color * flicker, trail);
    color = mix(color, electric_blue * flicker, outer * 0.4);
    color = mix(color, head_color * flicker, inner * 0.8);
    color = mix(color, head_color, sparks);
    color.rgb += head_color.rgb * particles;
    color = mix(color, core_color, core);

    // Bright flash on core
    color.rgb += core_color.rgb * core * 0.4;

    // Ambient glow
    float glow_dist = length(uv) * 2.0;
    float glow = exp(-glow_dist * 2.5) * 0.25 * flicker;
    color.rgb += head_color.rgb * glow;
    color.a = max(color.a, glow * 0.5);

    // Chromatic aberration
    float aberration = (1.0 - core) * 0.015 * flicker;
    color.r += sharp_step(bolt_width, abs(rotated_uv.y - lightning_y - aberration)) * bolt_x * 0.1;
    color.b += sharp_step(bolt_width, abs(rotated_uv.y - lightning_y + aberration)) * bolt_x * 0.15;

    COLOR = color;
}
