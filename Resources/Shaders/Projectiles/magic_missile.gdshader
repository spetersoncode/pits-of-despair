shader_type canvas_item;

// Magic Missile Projectile - Swirling Arcane Orb
// A sphere of condensed magical energy with ethereal mist trail

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

// Smooth noise
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

// Fractal Brownian Motion for organic, layered noise
float fbm(vec2 p, int octaves) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < octaves; i++) {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return value;
}

// Rotate a 2D point around origin
vec2 rotate(vec2 p, float angle) {
    float c = cos(angle);
    float s = sin(angle);
    return vec2(p.x * c - p.y * s, p.x * s + p.y * c);
}

void fragment() {
    vec2 uv = UV - vec2(0.5);
    float time = progress * 20.0;

    // Rotate UV to align with direction
    float dir_angle = atan(direction.y, direction.x);
    vec2 rotated_uv = rotate(uv, -dir_angle);

    // Distance from center for orb calculations
    float dist = length(uv);
    float orb_radius = 0.15 * size;

    // === SWIRLING ARCANE ORB ===

    // Create swirling pattern using polar coordinates
    float angle = atan(uv.y, uv.x);

    // Multiple swirl layers rotating at different speeds
    float swirl1 = fbm(vec2(angle * 2.0 + time * 1.5, dist * 8.0 - time * 0.5), 3);
    float swirl2 = fbm(vec2(angle * 3.0 - time * 2.0, dist * 6.0 + time * 0.3), 3);
    float swirl3 = noise(vec2(angle * 5.0 + time * 3.0, dist * 12.0));

    // Combine swirls for complex arcane pattern
    float swirl_pattern = swirl1 * 0.5 + swirl2 * 0.3 + swirl3 * 0.2;

    // Distort orb edge with swirling energy
    float edge_distortion = (swirl_pattern - 0.5) * 0.04 * size;
    float distorted_dist = dist + edge_distortion;

    // Core - intense white center
    float core = 1.0 - smoothstep(orb_radius * 0.2, orb_radius * 0.5, distorted_dist);

    // Inner energy - bright cyan
    float inner = 1.0 - smoothstep(orb_radius * 0.4, orb_radius * 0.9, distorted_dist);

    // Outer energy field with swirl visibility
    float outer = 1.0 - smoothstep(orb_radius * 0.7, orb_radius * 1.3, distorted_dist);

    // Arcane aura - visible swirling energy ring
    float aura_inner = smoothstep(orb_radius * 0.6, orb_radius * 0.9, distorted_dist);
    float aura_outer = 1.0 - smoothstep(orb_radius * 1.0, orb_radius * 1.5, distorted_dist);
    float aura = aura_inner * aura_outer * swirl_pattern;

    // Pulsing effect - gentle breathing
    float pulse = sin(time * 2.5) * 0.12 + 0.88;
    float fast_pulse = sin(time * 6.0) * 0.05 + 0.95;

    // Bright arcane sparkles that orbit the orb
    float sparkle_ring = smoothstep(orb_radius * 0.8, orb_radius * 1.0, dist) *
                         (1.0 - smoothstep(orb_radius * 1.2, orb_radius * 1.6, dist));
    float sparkle = pow(swirl3, 4.0) * sparkle_ring * 2.0;

    // === ETHEREAL MIST TRAIL ===

    float mist = 0.0;
    if (trail_length > 0) {
        float trail_extent = 0.35 * float(trail_length) * 0.2;

        // Trail extends behind the orb (negative x in rotated space)
        float trail_falloff = smoothstep(-trail_extent, 0.0, rotated_uv.x);

        // Width tapers as it extends back
        float taper = 1.0 - smoothstep(-trail_extent, 0.0, rotated_uv.x);
        float trail_width = orb_radius * (1.2 + taper * 0.8);
        float width_mask = 1.0 - smoothstep(trail_width * 0.4, trail_width, abs(rotated_uv.y));

        // Ethereal mist using FBM - flows and dissipates
        vec2 mist_uv = rotated_uv * 6.0;
        mist_uv.x -= time * 1.5; // Mist flows backward
        float mist_density = fbm(mist_uv, 4);

        // Add wispy tendrils
        float tendrils = fbm(rotated_uv * 12.0 + vec2(-time * 2.0, 0.0), 3);
        tendrils = pow(tendrils, 2.0) * 0.5;

        // Combine for final mist
        mist = trail_falloff * width_mask * (mist_density * 0.7 + tendrils * 0.3);

        // Fade mist near the orb to avoid overlap
        float orb_fade = smoothstep(orb_radius * 0.8, orb_radius * 1.5, dist);
        mist *= orb_fade;

        // Mist intensity varies with trail length
        mist *= 0.6 + float(trail_length) * 0.08;
    }

    // === COLOR COMPOSITION ===

    vec4 color = vec4(0.0);

    // White-cyan core color
    vec4 core_color = vec4(0.92, 1.0, 1.0, 1.0);

    // Brighter inner color
    vec4 inner_color = head_color * 1.3;
    inner_color.a = 1.0;

    // Layer the mist trail first (background)
    color = mix(color, trail_color * 1.2, mist * 0.8);

    // Arcane swirling aura
    color = mix(color, head_color * pulse, aura * 0.7);

    // Outer glow field
    color = mix(color, head_color * pulse, outer * 0.6);

    // Inner energy
    color = mix(color, inner_color * fast_pulse, inner);

    // Bright core
    color = mix(color, core_color, core);

    // Add sparkles on top
    color.rgb += head_color.rgb * sparkle * pulse;

    // Soft outer glow halo
    float halo = exp(-dist * 5.0) * 0.4;
    color.rgb += head_color.rgb * halo * pulse;

    // Very soft extended glow
    float extended_glow = exp(-dist * 2.5) * 0.15;
    color.rgb += head_color.rgb * extended_glow;

    COLOR = color;
}
