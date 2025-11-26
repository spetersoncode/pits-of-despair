shader_type canvas_item;

// Lightning Beam - ELECTRIC DEVASTATION
// Creates a crackling, violent lightning bolt with multiple strands,
// aggressive branching, and intense electric effects

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float beam_width : hint_range(1.0, 20.0) = 6.0;
uniform vec4 core_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);   // White hot core
uniform vec4 mid_color : source_color = vec4(1.0, 1.0, 0.4, 1.0);    // Electric yellow
uniform vec4 outer_color : source_color = vec4(0.6, 0.8, 1.0, 0.6);  // Blue glow
uniform float noise_scale : hint_range(0.1, 20.0) = 15.0;

// Fast pseudo-random hash
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float hash2(float p) {
    return fract(sin(p * 127.1) * 43758.5453);
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

// Generate aggressive jagged lightning displacement
float lightning_offset(float x, float time, float seed) {
    float offset = 0.0;
    float amplitude = 0.18;  // More aggressive base displacement
    float frequency = 5.0;

    for (int i = 0; i < 5; i++) {
        // Rapid time variation for violent movement
        offset += (noise(vec2(x * frequency + time * 4.0 + seed, float(i) * 17.3 + seed)) - 0.5) * amplitude;
        amplitude *= 0.6;
        frequency *= 2.3;
    }
    return offset;
}

// Sharp step for crisp lightning edges
float sharp_step(float edge, float value) {
    return 1.0 - smoothstep(edge * 0.7, edge, value);
}

void fragment() {
    vec2 uv = UV;
    float time = progress * 50.0;  // Faster animation

    // Brightness curve: INTENSE flash, then crackle fade
    float flash_peak = 0.08;
    float brightness;
    if (progress < flash_peak) {
        brightness = 1.2;  // Overexposed flash
    } else {
        float fade = (progress - flash_peak) / (1.0 - flash_peak);
        brightness = 1.0 - fade * fade;  // Slower fade
        // Add crackle bursts during fade
        brightness += noise(vec2(time * 6.0, progress * 10.0)) * 0.3 * (1.0 - fade);
    }

    // AGGRESSIVE flicker - rapid strobing
    float flicker = 1.0;
    flicker *= noise(vec2(time * 15.0, 0.0)) * 0.4 + 0.6;
    flicker *= noise(vec2(time * 22.0 + 50.0, 0.0)) * 0.3 + 0.7;
    flicker *= step(0.1, noise(vec2(time * 30.0, 100.0)));  // Random blackouts
    flicker = max(flicker, 0.4);  // Keep minimum visibility
    brightness *= flicker;

    float center_y = 0.5;
    float beam_edge = 0.12;

    // === MAIN LIGHTNING BOLT ===
    float main_offset = lightning_offset(uv.x * 8.0, time, 0.0);
    float main_y = center_y + main_offset;
    float main_dist = abs(uv.y - main_y);

    float main_core = sharp_step(beam_edge * 0.15, main_dist);
    float main_inner = sharp_step(beam_edge * 0.4, main_dist);
    float main_outer = sharp_step(beam_edge * 1.0, main_dist);

    // === SECONDARY PARALLEL STRANDS ===
    float strand_intensity = 0.0;
    for (int s = 0; s < 2; s++) {
        float strand_seed = float(s) * 456.789;
        float strand_offset_base = (float(s) - 0.5) * 0.08;
        float strand_y = center_y + strand_offset_base + lightning_offset(uv.x * 10.0, time * 1.3, strand_seed) * 0.7;
        float strand_dist = abs(uv.y - strand_y);

        // Strands flicker independently
        float strand_flicker = noise(vec2(time * 18.0 + strand_seed, 0.0)) * 0.5 + 0.5;
        float strand_core = sharp_step(beam_edge * 0.08, strand_dist) * strand_flicker;
        float strand_glow = sharp_step(beam_edge * 0.3, strand_dist) * strand_flicker * 0.5;
        strand_intensity = max(strand_intensity, strand_core + strand_glow);
    }

    // === BRANCHING SPARKS - Many more, longer ===
    float branches = 0.0;
    for (int i = 0; i < 8; i++) {
        float branch_seed = float(i) * 123.45 + time * 0.1;
        float branch_x = hash(vec2(branch_seed, 67.89 + floor(time * 2.0)));
        float branch_start = branch_x * 0.9 + 0.05;
        float branch_len = 0.15 + hash(vec2(branch_seed, 234.56)) * 0.2;

        if (uv.x > branch_start && uv.x < branch_start + branch_len) {
            float local_x = (uv.x - branch_start) / branch_len;
            float branch_dir = hash(vec2(float(i) * 234.56 + floor(time), 78.9)) > 0.5 ? 1.0 : -1.0;

            // Branch originates from main bolt
            float main_y_at_branch = center_y + lightning_offset(branch_start * 8.0, time, 0.0);
            float branch_y = main_y_at_branch;
            branch_y += local_x * branch_dir * 0.2;  // Longer reach
            branch_y += lightning_offset(uv.x * 20.0 + float(i) * 100.0, time * 1.5, float(i)) * 0.4;

            float branch_dist = abs(uv.y - branch_y);
            float taper = (1.0 - local_x * local_x);  // Taper toward end
            float branch_core = sharp_step(beam_edge * 0.1 * taper, branch_dist) * taper;
            float branch_glow = sharp_step(beam_edge * 0.3 * taper, branch_dist) * taper * 0.4;

            // Branches flicker rapidly
            float branch_flicker = step(0.3, noise(vec2(time * 25.0 + float(i) * 50.0, 0.0)));
            branches = max(branches, (branch_core + branch_glow) * branch_flicker * 0.8);
        }
    }

    // === ELECTRIC PARTICLES / SPARKS ===
    float particles = 0.0;
    float particle_field = noise(vec2(uv.x * 40.0 - time * 5.0, uv.y * 40.0 + time * 3.0));
    particle_field = pow(particle_field, 6.0);  // Sharp particles
    float dist_from_center = abs(uv.y - center_y);
    float particle_mask = 1.0 - smoothstep(0.0, 0.25, dist_from_center);
    particles = particle_field * particle_mask * 0.6;

    // Extra spark bursts
    for (int p = 0; p < 4; p++) {
        float spark_time = time + float(p) * 10.0;
        vec2 spark_pos = vec2(
            fract(hash(vec2(floor(spark_time), float(p))) + time * 0.5),
            center_y + (hash(vec2(float(p), floor(spark_time * 2.0))) - 0.5) * 0.15
        );
        float spark_dist = length(uv - spark_pos);
        float spark = exp(-spark_dist * 60.0) * step(0.7, noise(vec2(spark_time * 20.0, float(p))));
        particles += spark * 0.8;
    }

    // === COLOR COMPOSITION ===
    vec4 color = vec4(0.0);

    // Ambient electric field (furthest layer)
    float field_dist = abs(uv.y - center_y);
    float electric_field = exp(-field_dist * 4.0) * 0.25 * flicker;
    color.rgb += outer_color.rgb * electric_field;
    color.a = electric_field * 0.5;

    // Outer glow
    color = mix(color, outer_color, main_outer * 0.4);

    // Inner glow (yellow electric)
    color = mix(color, mid_color, main_inner * 0.8);

    // Secondary strands
    color = mix(color, mid_color, strand_intensity * 0.7);

    // Branches (yellow-white)
    vec4 branch_color = mix(mid_color, core_color, 0.5);
    color = mix(color, branch_color, branches);

    // Particles
    color.rgb += mid_color.rgb * particles;
    color.a = max(color.a, particles * 0.8);

    // Main bolt core (WHITE HOT)
    color = mix(color, core_color, main_core);

    // === FINAL ADJUSTMENTS ===

    // Apply brightness
    color.rgb *= brightness;
    color.a *= brightness;

    // INTENSE initial flash - blindingly bright
    if (progress < flash_peak * 2.0) {
        float boost = 1.0 - progress / (flash_peak * 2.0);
        boost = boost * boost;  // Sharper falloff
        color.rgb += vec3(1.0) * boost * 0.6;
        color.a = max(color.a, boost);
    }

    // Chromatic aberration on edges for electric feel
    float aberration = (1.0 - main_core) * 0.02 * flicker;
    color.r += sharp_step(beam_edge * 0.5, abs(uv.y - main_y - aberration)) * 0.1 * brightness;
    color.b += sharp_step(beam_edge * 0.5, abs(uv.y - main_y + aberration)) * 0.15 * brightness;

    COLOR = color;
}
