shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
uniform bool enabled = true;
uniform float intensity : hint_range(0.0, 1.0) = 0.5;

void fragment() {
    if (!enabled) {
        COLOR = texture(screen_texture, SCREEN_UV);
        // Workaround for Screen Texture in Godot 4: 
        // Replaceable with COLOR = vec4(0.0, 0.0, 0.0, 0.0); If we want it to not have any effect
    } else {
        vec2 uv = SCREEN_UV;
        vec4 color = texture(screen_texture, uv); // Requires ScreenTexture defined
        
        // Scanlines
        float scanline = sin(uv.y * 800.0) * 0.04 * intensity;
        color.rgb -= scanline;
        
        // Vignette
        float vignette = uv.x * uv.y * (1.0 - uv.x) * (1.0 - uv.y);
        vignette = clamp(pow(16.0 * vignette, 0.25), 0.0, 1.0);
        color.rgb *= mix(1.0, vignette, intensity);

        COLOR = color;
    }
}