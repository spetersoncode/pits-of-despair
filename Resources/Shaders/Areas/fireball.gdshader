shader_type canvas_item;

// Fireball Blast - Impact Effect
// Creates a dramatic filled radial burst with animated fire-like noise

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float radius : hint_range(0.0, 200.0) = 50.0;
uniform vec4 inner_color : source_color = vec4(1.0, 1.0, 0.9, 1.0);  // Hot white
uniform vec4 mid_color : source_color = vec4(1.0, 0.6, 0.1, 1.0);    // Bright orange
uniform vec4 outer_color : source_color = vec4(0.9, 0.2, 0.05, 1.0); // Deep red
uniform float noise_scale : hint_range(0.1, 10.0) = 5.0;
uniform float noise_intensity : hint_range(0.0, 1.0) = 0.4;

// Simple pseudo-random hash function
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Value noise for organic fire effect
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);

    // Smooth interpolation
    vec2 u = f * f * (3.0 - 2.0 * f);

    // Four corners
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

// Fractal Brownian Motion for more complex noise
float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < 5; i++) {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }

    return value;
}

// Ease out quad for smooth animation
float ease_out_quad(float t) {
    return 1.0 - (1.0 - t) * (1.0 - t);
}

// Ease out cubic for even smoother expansion
float ease_out_cubic(float t) {
    return 1.0 - pow(1.0 - t, 3.0);
}

void fragment() {
    // Calculate distance from center (UV is 0-1, center is 0.5, 0.5)
    vec2 center = vec2(0.5, 0.5);
    vec2 uv = UV - center;
    float dist = length(uv) * 2.0; // Normalize to 0-1 range

    // Eased progress for smooth expansion
    float eased_progress = ease_out_cubic(progress);

    // Current explosion radius (expands over time)
    // Start with a small initial burst, then expand
    float initial_burst = 0.2;
    float current_radius = initial_burst + eased_progress * (1.0 - initial_burst);

    // Add animated noise to the edge for fire-like appearance
    float time_offset = progress * 4.0; // Animate the noise faster
    float angle = atan(uv.y, uv.x);

    // Create turbulent fire noise
    vec2 noise_coord = vec2(angle * noise_scale, dist * noise_scale * 2.0 - time_offset);
    float noise_val = fbm(noise_coord) * noise_intensity;

    // Additional swirling noise
    vec2 swirl_coord = vec2(angle * 3.0 + time_offset * 0.5, dist * 4.0);
    float swirl_noise = fbm(swirl_coord) * 0.15;

    // Distort the edge with combined noise (more noise at edges)
    float edge_dist = dist + (noise_val + swirl_noise) * (0.5 + dist * 0.5);

    // Calculate gradient from center to edge
    float gradient = clamp(edge_dist / max(current_radius, 0.001), 0.0, 1.0);

    // Create color gradient: inner (hot white) -> mid (orange) -> outer (red)
    vec4 color;
    if (gradient < 0.2) {
        // Inner hot zone - white hot core
        color = mix(inner_color, mid_color, gradient / 0.2);
    } else if (gradient < 0.6) {
        // Mid orange zone - main fire
        color = mix(mid_color, outer_color, (gradient - 0.2) / 0.4);
    } else {
        // Outer red edge - cooler flames
        color = outer_color;
    }

    // Add intensity variation with noise for flickering effect
    float intensity_noise = fbm(vec2(angle * 3.0 + time_offset, dist * 2.0));
    color.rgb *= 0.85 + intensity_noise * 0.3;

    // Boost brightness in the center
    float center_boost = 1.0 + (1.0 - gradient) * 0.5;
    color.rgb *= center_boost;

    // Fade alpha based on progress (explosion fades out toward end)
    float alpha_fade = 1.0 - pow(progress, 1.5) * 0.8;

    // Soft edge falloff with noise-influenced softness
    float edge_softness = 0.15 + noise_val * 0.1;
    float soft_edge = 1.0 - smoothstep(current_radius - edge_softness, current_radius + 0.02, edge_dist);

    // Final alpha
    color.a *= alpha_fade * soft_edge;

    // Add bright glow at the expanding edge
    float glow_dist = abs(edge_dist - current_radius);
    float glow = exp(-glow_dist * 12.0) * (1.0 - progress * 0.5) * 0.6;
    color.rgb += vec3(1.0, 0.7, 0.3) * glow;

    // Add outer halo/heat shimmer
    float halo_dist = edge_dist - current_radius;
    if (halo_dist > 0.0 && halo_dist < 0.2) {
        float halo = exp(-halo_dist * 8.0) * (1.0 - progress) * 0.3;
        color.rgb += outer_color.rgb * halo;
        color.a = max(color.a, halo * 0.5);
    }

    COLOR = color;
}
