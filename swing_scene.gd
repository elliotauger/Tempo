extends Node3D
## Main swing mechanic controller.
## Handles input, tempo detection, body rotation, club animation,
## ball launch, and camera pan on impact.

# ── Node references (assigned in _ready) ──────────────────────────
@onready var camera: Camera3D = $Camera3D
@onready var body_pivot: Node3D = $BodyPivot          # rotates arms + club
@onready var left_arm: MeshInstance3D = $BodyPivot/LeftArm
@onready var right_arm: MeshInstance3D = $BodyPivot/RightArm
@onready var club_shaft: MeshInstance3D = $BodyPivot/ClubShaft
@onready var club_head: MeshInstance3D = $BodyPivot/ClubShaft/ClubHead
@onready var ball: MeshInstance3D = $Ball
@onready var tee: MeshInstance3D = $Tee
@onready var flag: Node3D = $Flag
@onready var swing_ui: Control = $SwingUI
@onready var shot_label: Label = $SwingUI/ShotLabel
@onready var power_bar: ProgressBar = $SwingUI/PowerBar

# ── Swing state machine ──────────────────────────────────────────
enum SwingState { ADDRESS, BACKSWING, DOWNSWING, FOLLOW_THROUGH, BALL_FLIGHT, RESET }
var state: SwingState = SwingState.ADDRESS

# ── Input tracking ───────────────────────────────────────────────
var drag_start_pos := Vector2.ZERO
var drag_current_pos := Vector2.ZERO
var is_dragging := false
var backswing_amount := 0.0          # 0‥1 normalised
var max_drag_distance := 300.0       # pixels for full backswing

# ── Tempo tracking ───────────────────────────────────────────────
var backswing_start_time := 0.0
var backswing_end_time := 0.0
var downswing_start_time := 0.0
var impact_time := 0.0
var swing_duration := 0.0            # total seconds back‥impact
var tempo_ratio := 0.0               # backswing_time / total_time

# ── Rotation limits (degrees) ────────────────────────────────────
const MAX_BACKSWING_ROTATION := 85.0  # body rotation right
const MAX_FOLLOWTHROUGH_ROTATION := 100.0
const WRIST_COCK_LAG := 0.25         # club lags behind body by this fraction

# ── Ball flight ──────────────────────────────────────────────────
var ball_in_flight := false
var ball_velocity := Vector3.ZERO
var ball_start_pos := Vector3.ZERO
var flight_time := 0.0
const GRAVITY := 9.8

# ── Camera pan ───────────────────────────────────────────────────
var camera_original_transform: Transform3D
var camera_pan_active := false
var camera_pan_time := 0.0
const CAMERA_PAN_DURATION := 1.2     # seconds to pan up to follow ball

# ── Shot result data ─────────────────────────────────────────────
var shot_type := ""
var shot_distance := 0.0
var shot_power := 0.0
var shot_accuracy := 1.0

# ── Animation ────────────────────────────────────────────────────
var followthrough_tween: Tween
var reset_tween: Tween


func _ready() -> void:
	camera_original_transform = camera.transform
	ball_start_pos = ball.position
	_update_ui_hidden()


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		_handle_touch(event)
	elif event is InputEventMouseMotion and is_dragging:
		_handle_drag(event)
	elif event is InputEventScreenTouch:
		_handle_screen_touch(event)
	elif event is InputEventScreenDrag and is_dragging:
		_handle_screen_drag(event)


# ── Touch / click handling ───────────────────────────────────────

func _handle_touch(event: InputEventMouseButton) -> void:
	if event.button_index != MOUSE_BUTTON_LEFT:
		return
	if event.pressed:
		_start_drag(event.position)
	else:
		_end_drag(event.position)


func _handle_drag(event: InputEventMouseMotion) -> void:
	drag_current_pos = event.position
	if state == SwingState.BACKSWING:
		_update_backswing()


func _handle_screen_touch(event: InputEventScreenTouch) -> void:
	if event.pressed:
		_start_drag(event.position)
	else:
		_end_drag(event.position)


func _handle_screen_drag(event: InputEventScreenDrag) -> void:
	drag_current_pos = event.position
	if state == SwingState.BACKSWING:
		_update_backswing()


func _start_drag(pos: Vector2) -> void:
	if state != SwingState.ADDRESS and state != SwingState.RESET:
		return
	if state == SwingState.RESET:
		_instant_reset()
	is_dragging = true
	drag_start_pos = pos
	drag_current_pos = pos
	backswing_start_time = Time.get_ticks_msec() / 1000.0
	state = SwingState.BACKSWING
	_update_ui_hidden()


func _end_drag(pos: Vector2) -> void:
	if not is_dragging:
		return
	is_dragging = false
	drag_current_pos = pos

	if state == SwingState.BACKSWING and backswing_amount > 0.15:
		backswing_end_time = Time.get_ticks_msec() / 1000.0
		downswing_start_time = backswing_end_time
		state = SwingState.DOWNSWING
		_execute_downswing()
	else:
		# Didn't pull back far enough – cancel
		_animate_reset()


func _update_backswing() -> void:
	# Drag to the RIGHT loads the backswing
	var drag_delta := drag_current_pos.x - drag_start_pos.x
	backswing_amount = clampf(drag_delta / max_drag_distance, 0.0, 1.0)

	# Rotate body pivot to the RIGHT (positive Y in our setup)
	var body_rot := backswing_amount * MAX_BACKSWING_ROTATION
	body_pivot.rotation_degrees.y = body_rot

	# Club lags behind due to wrist cock
	var club_lag := backswing_amount * MAX_BACKSWING_ROTATION * (1.0 - WRIST_COCK_LAG)
	club_shaft.rotation_degrees.y = club_lag - body_rot  # relative to body

	# Update power bar
	power_bar.value = backswing_amount * 100.0
	power_bar.visible = true


# ── Downswing & Impact ───────────────────────────────────────────

func _execute_downswing() -> void:
	state = SwingState.DOWNSWING

	# Calculate tempo
	var backswing_time := backswing_end_time - backswing_start_time
	_classify_shot(backswing_time, backswing_amount)

	# Tween the body from current backswing rotation through to follow-through
	var current_rot := body_pivot.rotation_degrees.y
	var impact_rot := 0.0
	var followthrough_rot := -MAX_FOLLOWTHROUGH_ROTATION

	# Downswing speed depends on shot type
	var downswing_duration := _get_downswing_duration()

	if followthrough_tween and followthrough_tween.is_valid():
		followthrough_tween.kill()

	followthrough_tween = create_tween()
	followthrough_tween.set_ease(Tween.EASE_IN_OUT)
	followthrough_tween.set_trans(Tween.TRANS_CUBIC)

	# Phase 1: Downswing to impact
	var to_impact_time := downswing_duration * 0.4
	followthrough_tween.tween_property(body_pivot, "rotation_degrees:y", impact_rot, to_impact_time)
	followthrough_tween.parallel().tween_property(club_shaft, "rotation_degrees:y", 0.0, to_impact_time * 0.8)
	followthrough_tween.tween_callback(_on_impact)

	# Phase 2: Follow-through
	var followthrough_time := downswing_duration * 0.6
	followthrough_tween.tween_property(body_pivot, "rotation_degrees:y", followthrough_rot, followthrough_time)
	followthrough_tween.parallel().tween_property(club_shaft, "rotation_degrees:y", -15.0, followthrough_time)
	followthrough_tween.tween_callback(_on_followthrough_complete)


func _classify_shot(backswing_time: float, power: float) -> void:
	## Classify based on tempo (how fast the backswing was performed)
	## and the amount of backswing loaded.

	if backswing_time < 0.25 and power > 0.7:
		shot_type = "POWER DRIVE"
		shot_power = power * 1.15
		shot_accuracy = 0.75
	elif backswing_time < 0.5 and power > 0.5:
		shot_type = "CLEAN STRIKE"
		shot_power = power * 1.0
		shot_accuracy = 1.0
	elif power < 0.45:
		shot_type = "PUNCH SHOT"
		shot_power = power * 0.8
		shot_accuracy = 0.9
	elif backswing_time > 0.7:
		shot_type = "SOFT LOB"
		shot_power = power * 0.65
		shot_accuracy = 0.85
	else:
		shot_type = "CLEAN STRIKE"
		shot_power = power * 0.95
		shot_accuracy = 0.95

	shot_power = clampf(shot_power, 0.1, 1.2)
	swing_duration = backswing_time


func _get_downswing_duration() -> float:
	match shot_type:
		"POWER DRIVE":
			return 0.3
		"CLEAN STRIKE":
			return 0.4
		"PUNCH SHOT":
			return 0.25
		"SOFT LOB":
			return 0.55
		_:
			return 0.4


func _on_impact() -> void:
	impact_time = Time.get_ticks_msec() / 1000.0
	state = SwingState.FOLLOW_THROUGH

	# Launch ball
	_launch_ball()

	# Begin camera pan
	_start_camera_pan()

	# Show shot info
	shot_label.text = shot_type
	shot_label.visible = true
	power_bar.visible = false


func _on_followthrough_complete() -> void:
	state = SwingState.BALL_FLIGHT


# ── Ball Flight ──────────────────────────────────────────────────

func _launch_ball() -> void:
	ball_in_flight = true
	flight_time = 0.0
	ball_start_pos = ball.position

	# Calculate launch parameters based on shot type
	var launch_speed := shot_power * 65.0   # max ~75 m/s
	var launch_angle := _get_launch_angle()
	var lateral_error := (1.0 - shot_accuracy) * randf_range(-0.15, 0.15)

	ball_velocity = Vector3(
		lateral_error * launch_speed,
		sin(deg_to_rad(launch_angle)) * launch_speed,
		-cos(deg_to_rad(launch_angle)) * launch_speed  # negative Z = down fairway
	)

	# Calculate estimated distance for display
	var flight_t := (2.0 * ball_velocity.y) / GRAVITY
	shot_distance = abs(ball_velocity.z) * flight_t


func _get_launch_angle() -> float:
	match shot_type:
		"POWER DRIVE":
			return 12.0
		"CLEAN STRIKE":
			return 18.0
		"PUNCH SHOT":
			return 8.0
		"SOFT LOB":
			return 42.0
		_:
			return 18.0


func _process(delta: float) -> void:
	if ball_in_flight:
		_update_ball_flight(delta)
	if camera_pan_active:
		_update_camera_pan(delta)


func _update_ball_flight(delta: float) -> void:
	flight_time += delta
	ball_velocity.y -= GRAVITY * delta
	ball.position += ball_velocity * delta

	# Ball landed (back below start height after going up)
	if flight_time > 0.3 and ball.position.y <= ball_start_pos.y:
		ball.position.y = ball_start_pos.y
		ball_in_flight = false
		_on_ball_landed()


func _on_ball_landed() -> void:
	var dist_yards := ball.position.distance_to(ball_start_pos) * 1.09  # rough m to yards
	shot_label.text = "%s\n%.0f yards" % [shot_type, dist_yards]

	# Wait a moment then reset
	await get_tree().create_timer(2.5).timeout
	_animate_reset()


# ── Camera Pan ───────────────────────────────────────────────────

func _start_camera_pan() -> void:
	camera_pan_active = true
	camera_pan_time = 0.0


func _update_camera_pan(delta: float) -> void:
	camera_pan_time += delta
	var t := clampf(camera_pan_time / CAMERA_PAN_DURATION, 0.0, 1.0)
	t = _ease_out_cubic(t)

	# Pan camera up to follow ball trajectory
	var pan_rotation := lerpf(0.0, -35.0, t)  # tilt upward
	camera.rotation_degrees.x = camera_original_transform.basis.get_euler().x * (180.0 / PI) + pan_rotation

	if camera_pan_time > CAMERA_PAN_DURATION:
		camera_pan_active = false


func _ease_out_cubic(t: float) -> float:
	return 1.0 - pow(1.0 - t, 3.0)


# ── Reset ────────────────────────────────────────────────────────

func _animate_reset() -> void:
	state = SwingState.RESET

	if reset_tween and reset_tween.is_valid():
		reset_tween.kill()

	reset_tween = create_tween()
	reset_tween.set_ease(Tween.EASE_IN_OUT)
	reset_tween.set_trans(Tween.TRANS_QUAD)
	reset_tween.set_parallel(true)

	reset_tween.tween_property(body_pivot, "rotation_degrees:y", 0.0, 0.6)
	reset_tween.tween_property(club_shaft, "rotation_degrees:y", 0.0, 0.6)
	reset_tween.tween_property(camera, "rotation_degrees:x",
		camera_original_transform.basis.get_euler().x * (180.0 / PI), 0.5)

	reset_tween.set_parallel(false)
	reset_tween.tween_callback(_finish_reset)


func _finish_reset() -> void:
	ball.position = ball_start_pos
	ball_in_flight = false
	camera.transform = camera_original_transform
	camera_pan_active = false
	backswing_amount = 0.0
	shot_label.visible = false
	power_bar.visible = false
	state = SwingState.ADDRESS


func _instant_reset() -> void:
	body_pivot.rotation_degrees.y = 0.0
	club_shaft.rotation_degrees.y = 0.0
	ball.position = ball_start_pos
	camera.transform = camera_original_transform
	ball_in_flight = false
	camera_pan_active = false
	backswing_amount = 0.0
	state = SwingState.ADDRESS


func _update_ui_hidden() -> void:
	shot_label.visible = false
	power_bar.visible = false
