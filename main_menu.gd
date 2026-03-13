extends Control

@onready var title_label: Label = $VBoxContainer/Title
@onready var subtitle_label: Label = $VBoxContainer/Subtitle
@onready var play_button: Button = $VBoxContainer/PlayButton
@onready var animation_player: AnimationPlayer = $AnimationPlayer

var pulse_tween: Tween


func _ready() -> void:
	play_button.pressed.connect(_on_play_pressed)
	_start_pulse_animation()


func _start_pulse_animation() -> void:
	pulse_tween = create_tween()
	pulse_tween.set_loops()
	pulse_tween.tween_property(play_button, "modulate:a", 0.5, 0.8)
	pulse_tween.tween_property(play_button, "modulate:a", 1.0, 0.8)


func _on_play_pressed() -> void:
	if pulse_tween and pulse_tween.is_valid():
		pulse_tween.kill()
	# Fade out then switch scene
	var tween := create_tween()
	tween.tween_property(self, "modulate:a", 0.0, 0.4)
	tween.tween_callback(_go_to_swing)


func _go_to_swing() -> void:
	get_tree().change_scene_to_file("res://swing_scene.tscn")
