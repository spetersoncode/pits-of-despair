shader_type canvas_item;

// Cone of Cold - Directional Ice Blast Effect
// Creates a cone-shaped freezing blast with ice crystals and frost particles
// Note: Direction is handled by ColorRect rotation, not shader

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 inner_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);  // Bright white core
uniform vec4 mid_color : source_color = vec4(0.6, 0.85, 1.0, 1.0);   // Ice blue
uniform vec4 outer_color : source_color = vec4(0.2, 0.4, 0.8, 1.0);  // Deep cold blue
uniform float crystal_density : hint_range(1.0, 20.0) = 8.0;
uniform float frost_intensity : hint_range(0.0, 1.0) = 0.6;
uniform float cone_half_width : hint_range(0.1, 0.5) = 0.5;  // Half-width in UV space at tip (0.5 = fills rect)

// Hash function for randomness
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
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

// Fractal noise for ice crystal patterns
float fbm(vec2 p) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < 4; i++) {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }

    return value;
}

// Sharp crystalline noise for ice shards
float crystal_noise(vec2 p, float scale) {
    vec2 i = floor(p * scale);
    vec2 f = fract(p * scale);

    // Create sharp diamond-like patterns
    float d = abs(f.x - 0.5) + abs(f.y - 0.5);
    float crystal = 1.0 - smoothstep(0.0, 0.4, d);

    // Add variation per cell
    float variation = hash(i);
    return crystal * (0.5 + variation * 0.5);
}

// Ease functions
float ease_out_cubic(float t) {
    return 1.0 - pow(1.0 - t, 3.0);
}

void fragment() {
    // UV is 0-1, cone points from left (0) to right (1)
    // Origin is at the left-center of the rect (0.0, 0.5)
    // ColorRect rotation handles direction, shader just draws cone pointing right

    // Distance along cone axis (0 at origin, 1 at max length)
    float dist_along = UV.x;

    // Perpendicular distance from cone center axis (0.5 is center, range 0 to 0.5)
    float dist_perp = abs(UV.y - 0.5);

    // Calculate cone boundary at this distance
    // Cone widens linearly from origin (0) to tip (1)
    float cone_width_at_dist = dist_along * cone_half_width * 0.85;

    // Eased progress for smooth expansion
    float eased_progress = ease_out_cubic(progress);
    float current_reach = eased_progress;

    // Check if we're inside the visible cone area
    bool in_cone = dist_along > 0.0 && dist_along < 1.0 && dist_perp < cone_width_at_dist;
    bool in_blast = dist_along <= current_reach + 0.1;

    // Calculate all values (even if not used, to avoid early returns)
    float depth = 1.0 - (dist_perp / max(cone_width_at_dist, 0.001));
    depth = clamp(depth, 0.0, 1.0);

    float dist_factor = dist_along;

    // Ice crystal pattern
    float time_offset = progress * 3.0;
    vec2 cone_uv = vec2(UV.x, UV.y - 0.5);
    vec2 crystal_coord = cone_uv * crystal_density + vec2(time_offset, 0.0);
    float crystals = crystal_noise(crystal_coord, 5.0);

    // Frost swirl pattern
    vec2 frost_coord = cone_uv * 4.0 + vec2(-time_offset * 0.5, time_offset * 0.3);
    float frost = fbm(frost_coord) * frost_intensity;

    // Add sharp ice shard streaks along the cone
    float shard_angle = atan(cone_uv.y, cone_uv.x);
    float shards = 0.0;
    for (int i = 0; i < 5; i++) {
        float offset = float(i) * 0.628 + time_offset * 0.5;
        float shard_dist = abs(sin(shard_angle * 8.0 + offset));
        shards += (1.0 - smoothstep(0.0, 0.15, shard_dist)) * 0.2;
    }

    // Color gradient: inner (white) -> mid (ice blue) -> outer (deep blue)
    vec4 color;
    float color_factor = (1.0 - depth) * 0.7 + dist_factor * 0.3;

    if (color_factor < 0.3) {
        color = mix(inner_color, mid_color, color_factor / 0.3);
    } else if (color_factor < 0.7) {
        color = mix(mid_color, outer_color, (color_factor - 0.3) / 0.4);
    } else {
        color = outer_color;
    }

    // Add crystal shimmer
    color.rgb += vec3(crystals * 0.3) * depth;

    // Add frost texture
    color.rgb = mix(color.rgb, vec3(0.9, 0.95, 1.0), frost * depth * 0.3);

    // Add ice shard highlights
    color.rgb += vec3(0.8, 0.9, 1.0) * shards * depth;

    // Bright leading edge of the blast
    float edge_dist = abs(dist_along - current_reach);
    float leading_edge = exp(-edge_dist * 15.0) * (1.0 - progress * 0.5);
    color.rgb += vec3(0.9, 0.95, 1.0) * leading_edge * 0.8;

    // Sparkle effect (ice crystals catching light)
    float sparkle = hash(floor(crystal_coord * 10.0) + vec2(floor(progress * 20.0)));
    float sparkle_add = step(0.95, sparkle) * (1.0 - progress) * depth;
    color.rgb += vec3(sparkle_add);

    // Alpha: full in center, fade at edges and tip
    float alpha = depth * 0.9;

    // Fade out at the leading edge
    float edge_fade = smoothstep(current_reach + 0.1, current_reach - 0.1, dist_along);
    alpha *= edge_fade;

    // Fade out as effect ends
    float end_fade = 1.0 - pow(progress, 2.0) * 0.6;
    alpha *= end_fade;

    // Soften cone edges
    float edge_softness = smoothstep(0.0, 0.15, depth);
    alpha *= edge_softness;

    color.a = alpha;

    // Add outer frost halo at cone edges
    float halo = max(0.0, (0.2 - depth) / 0.2) * (1.0 - progress * 0.7);
    float halo_active = step(0.001, halo) * step(dist_along, current_reach);
    color.rgb = mix(color.rgb, outer_color.rgb * 1.2, halo * 0.5 * halo_active);
    color.a = max(color.a, halo * 0.3 * end_fade * halo_active);

    // Final visibility mask - set to transparent if outside cone or blast
    float visible = float(in_cone && in_blast);
    COLOR = vec4(color.rgb, color.a * visible);
}
