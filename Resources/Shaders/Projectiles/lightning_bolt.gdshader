shader_type canvas_item;

// Lightning Bolt Projectile - Projectile Effect
// A crackling bolt of electricity with branching sparks

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 head_color : source_color = vec4(1.0, 1.0, 0.4, 1.0);
uniform vec4 trail_color : source_color = vec4(1.0, 1.0, 0.4, 0.5);
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

// Generate jagged lightning displacement
float lightning_offset(float x, float time) {
    float offset = 0.0;
    float amplitude = 0.08;
    float frequency = 8.0;

    for (int i = 0; i < 3; i++) {
        offset += (noise(vec2(x * frequency + time * 3.0, float(i))) - 0.5) * amplitude;
        amplitude *= 0.6;
        frequency *= 2.0;
    }
    return offset;
}

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float time = progress * 25.0;

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
    float bolt_width = 0.04 * size;

    // Main lightning bolt with jagged displacement
    float lightning_y = lightning_offset(rotated_uv.x * 10.0, time);
    float dist_from_bolt = abs(rotated_uv.y - lightning_y);

    // Bolt visibility along length
    float bolt_x = smoothstep(-bolt_length, -bolt_length + 0.05, rotated_uv.x) *
                  (1.0 - smoothstep(bolt_length - 0.05, bolt_length, rotated_uv.x));

    // Core bolt (bright white-yellow)
    float core = (1.0 - smoothstep(0.0, bolt_width * 0.4, dist_from_bolt)) * bolt_x;

    // Inner glow
    float inner = (1.0 - smoothstep(bolt_width * 0.3, bolt_width, dist_from_bolt)) * bolt_x;

    // Outer electric field
    float outer = (1.0 - smoothstep(bolt_width * 0.5, bolt_width * 2.0, dist_from_bolt)) * bolt_x;

    // Branching sparks
    float sparks = 0.0;
    for (int i = 0; i < 3; i++) {
        float spark_x = hash(vec2(float(i) * 123.45, 67.89)) * bolt_length * 1.5 - bolt_length * 0.5;
        float spark_dir = hash(vec2(float(i) * 234.56, 78.9)) > 0.5 ? 1.0 : -1.0;
        float spark_len = 0.05 + hash(vec2(float(i) * 345.67, 89.01)) * 0.08;

        // Position along main bolt
        if (abs(rotated_uv.x - spark_x) < spark_len) {
            float local_x = (rotated_uv.x - spark_x) / spark_len;
            float spark_y_offset = lightning_offset(spark_x * 10.0, time);
            float expected_y = spark_y_offset + local_x * spark_dir * spark_len * 2.0;
            float spark_dist = abs(rotated_uv.y - expected_y);
            float spark_intensity = (1.0 - smoothstep(0.0, bolt_width * 0.6, spark_dist)) *
                                   (1.0 - abs(local_x));
            sparks = max(sparks, spark_intensity * 0.7);
        }
    }

    // Flicker effect
    float flicker = noise(vec2(time * 5.0, 0.0)) * 0.3 + 0.7;
    flicker *= noise(vec2(time * 8.0 + 100.0, 0.0)) * 0.2 + 0.8;

    // Trail (electric afterglow)
    float trail = 0.0;
    if (trail_length > 0) {
        float trail_x = smoothstep(-bolt_length * (1.0 + float(trail_length) * 0.2), -bolt_length * 0.5, rotated_uv.x);
        float trail_y = 1.0 - smoothstep(0.0, bolt_width * 3.0, abs(rotated_uv.y - lightning_y));
        trail = trail_x * trail_y * 0.3 * (1.0 - core) * flicker;

        // Electric particles in trail
        float particles = noise(vec2(rotated_uv.x * 30.0 - time * 3.0, rotated_uv.y * 30.0));
        particles = pow(particles, 4.0);
        trail += particles * trail_x * 0.4;
    }

    // Color composition
    vec4 color = vec4(0.0);
    vec4 core_color = vec4(1.0, 1.0, 1.0, 1.0);
    vec4 electric_blue = vec4(0.6, 0.8, 1.0, 1.0);

    color = mix(color, trail_color * flicker, trail);
    color = mix(color, electric_blue * flicker, outer * 0.5);
    color = mix(color, head_color * flicker, inner);
    color = mix(color, core_color, core);
    color = mix(color, head_color, sparks);

    // Bright flash on core
    color.rgb += core_color.rgb * core * 0.3;

    // Ambient glow
    float glow_dist = length(uv) * 2.0;
    float glow = exp(-glow_dist * 3.0) * 0.2 * flicker;
    color.rgb += head_color.rgb * glow;

    COLOR = color;
}
