shader_type canvas_item;

// Arrow Projectile - Projectile Effect
// A sharp, fast arrow with motion blur trail

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 head_color : source_color = vec4(0.6, 0.45, 0.3, 1.0);
uniform vec4 trail_color : source_color = vec4(0.6, 0.45, 0.3, 0.3);
uniform float size : hint_range(0.5, 3.0) = 1.0;
uniform int trail_length : hint_range(0, 10) = 2;
uniform vec2 direction = vec2(1.0, 0.0);

void fragment() {
    vec2 uv = UV - vec2(0.5);

    // Rotate UV to align with direction
    float angle = atan(direction.y, direction.x);
    float cos_a = cos(-angle);
    float sin_a = sin(-angle);
    vec2 rotated_uv = vec2(
        uv.x * cos_a - uv.y * sin_a,
        uv.x * sin_a + uv.y * cos_a
    );

    // Arrow dimensions
    float arrow_length = 0.35 * size;
    float arrow_width = 0.08 * size;
    float head_length = 0.12 * size;

    // Arrow shaft (rectangle)
    float shaft_x = smoothstep(-arrow_length, -arrow_length + 0.02, rotated_uv.x) *
                   (1.0 - smoothstep(head_length * 0.3, head_length * 0.5, rotated_uv.x));
    float shaft_y = 1.0 - smoothstep(arrow_width * 0.4, arrow_width * 0.6, abs(rotated_uv.y));
    float shaft = shaft_x * shaft_y;

    // Arrow head (triangle pointing in direction)
    float head_x = smoothstep(0.0, head_length, rotated_uv.x);
    float head_width = arrow_width * 1.5 * (1.0 - head_x);
    float head_y = 1.0 - smoothstep(head_width * 0.8, head_width, abs(rotated_uv.y));
    float head = head_y * smoothstep(-0.02, 0.02, rotated_uv.x) * (1.0 - smoothstep(head_length * 0.9, head_length, rotated_uv.x));

    // Fletching (back feathers)
    float fletch_x = smoothstep(-arrow_length - 0.02, -arrow_length + 0.05, rotated_uv.x) *
                    (1.0 - smoothstep(-arrow_length + 0.08, -arrow_length + 0.12, rotated_uv.x));
    float fletch_width = arrow_width * 1.2;
    float fletch_y = 1.0 - smoothstep(fletch_width * 0.6, fletch_width, abs(rotated_uv.y));
    float fletching = fletch_x * fletch_y * 0.7;

    // Combine arrow parts
    float arrow = max(max(shaft, head), fletching);

    // Motion blur trail
    float trail = 0.0;
    if (trail_length > 0) {
        float trail_fade = smoothstep(-arrow_length * 2.0, -arrow_length, rotated_uv.x);
        float trail_width = arrow_width * 0.3;
        float trail_y = 1.0 - smoothstep(trail_width * 0.5, trail_width, abs(rotated_uv.y));
        trail = trail_fade * trail_y * (1.0 - arrow) * 0.5;

        // Extend trail based on trail_length
        float extended_trail = smoothstep(-arrow_length * (1.0 + float(trail_length) * 0.3), -arrow_length, rotated_uv.x);
        trail = max(trail, extended_trail * trail_y * 0.3 * (1.0 - arrow));
    }

    // Color composition
    vec4 color = vec4(0.0);

    // Metal head highlight
    vec4 metal_color = vec4(0.8, 0.8, 0.85, 1.0);
    float metal_highlight = head * smoothstep(0.0, head_length * 0.5, rotated_uv.x);

    color = mix(color, trail_color, trail);
    color = mix(color, head_color, arrow);
    color = mix(color, metal_color, metal_highlight * 0.5);

    // Edge definition
    color.rgb *= 0.9 + arrow * 0.2;

    COLOR = color;
}
