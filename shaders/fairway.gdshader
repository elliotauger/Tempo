shader_type spatial;

uniform vec4 stripe_dark : source_color = vec4(0.12, 0.38, 0.08, 1.0);
uniform vec4 stripe_light : source_color = vec4(0.15, 0.45, 0.10, 1.0);
uniform float stripe_width : hint_range(0.5, 10.0) = 3.0;
uniform float stripe_angle : hint_range(-1.0, 1.0) = 0.0;
uniform float roughness_val : hint_range(0.0, 1.0) = 0.85;

void fragment() {
	// Mow stripe pattern along Z axis (down the fairway)
	float world_z = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).z;
	float world_x = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).x;
	float stripe_coord = world_x + world_z * stripe_angle;
	float stripe = sin(stripe_coord / stripe_width * TAU) * 0.5 + 0.5;
	// Smooth the stripe edges slightly for a natural mow look
	stripe = smoothstep(0.35, 0.65, stripe);
	vec4 color = mix(stripe_dark, stripe_light, stripe);

	ALBEDO = color.rgb;
	ROUGHNESS = roughness_val;
	METALLIC = 0.0;
	NORMAL_MAP_DEPTH = 0.3;
}
