shader_type canvas_item;

// Tunneling Beam - Beam Effect
// Creates a solid beam that flashes bright then fades out

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float beam_width : hint_range(1.0, 20.0) = 6.0;
uniform vec4 core_color : source_color = vec4(1.0, 0.95, 0.8, 1.0);  // Hot white-yellow core
uniform vec4 mid_color : source_color = vec4(0.9, 0.6, 0.2, 1.0);   // Orange energy
uniform vec4 outer_color : source_color = vec4(0.6, 0.4, 0.2, 1.0); // Brown earth tone
uniform float noise_scale : hint_range(0.1, 20.0) = 12.0;

// Simple pseudo-random hash function
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// Value noise for texture
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

// Fractal noise
float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 3; i++) {
        value += amplitude * noise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

void fragment() {
    vec2 uv = UV;

    // Distance from center line (y = 0.5 is center)
    float dist_from_center = abs(uv.y - 0.5) * 2.0;

    // Brightness curve: flash up quickly, fade out slowly
    // Peak at progress ~0.15, then fade
    float flash_peak = 0.15;
    float brightness;
    if (progress < flash_peak) {
        // Quick ramp up
        brightness = progress / flash_peak;
    } else {
        // Slow fade out
        brightness = 1.0 - (progress - flash_peak) / (1.0 - flash_peak);
    }
    brightness = brightness * brightness; // Ease the curve

    // Add noise for energy turbulence along the beam
    float noise_val = fbm(vec2(uv.x * noise_scale, uv.y * noise_scale * 0.5)) * 0.2;

    // Beam width with noise variation
    float beam_edge = 0.5; // Normalized beam half-width
    float noisy_dist = dist_from_center + noise_val * 0.3;

    // Calculate beam intensity - solid core fading to edges
    float beam_intensity = 0.0;
    if (noisy_dist < beam_edge) {
        // Gradient from core to edge
        float edge_factor = noisy_dist / beam_edge;

        // Inner bright core
        if (edge_factor < 0.3) {
            beam_intensity = 1.0;
        } else if (edge_factor < 0.7) {
            beam_intensity = 1.0 - (edge_factor - 0.3) * 0.5;
        } else {
            beam_intensity = 0.75 - (edge_factor - 0.7) * 2.5;
        }
    }

    // Soft edge falloff
    float soft_edge = 1.0 - smoothstep(beam_edge - 0.1, beam_edge + 0.05, noisy_dist);

    // Color gradient based on distance from center
    vec4 beam_color;
    float color_factor = clamp(noisy_dist / beam_edge, 0.0, 1.0);
    if (color_factor < 0.3) {
        beam_color = mix(core_color, mid_color, color_factor / 0.3);
    } else {
        beam_color = mix(mid_color, outer_color, (color_factor - 0.3) / 0.7);
    }

    // Add subtle pulsing energy along the beam
    float energy_pulse = sin(uv.x * 30.0 + noise_val * 10.0) * 0.1 + 0.9;

    // Combine everything
    vec4 color = beam_color;
    color.rgb *= beam_intensity * energy_pulse;
    color.a = soft_edge * brightness;

    // Boost brightness at flash peak
    if (progress < flash_peak * 2.0) {
        float boost = 1.0 - progress / (flash_peak * 2.0);
        color.rgb += core_color.rgb * boost * 0.3 * soft_edge;
    }

    // Add glow around beam edges during flash
    float glow_dist = abs(noisy_dist - beam_edge);
    if (glow_dist < 0.2 && noisy_dist > beam_edge * 0.5) {
        float glow = exp(-glow_dist * 10.0) * brightness * 0.4;
        color.rgb += mid_color.rgb * glow;
        color.a = max(color.a, glow * 0.5);
    }

    COLOR = color;
}
