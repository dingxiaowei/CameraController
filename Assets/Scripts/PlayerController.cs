using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class PlayerController : MonoBehaviour
{
    private const float FALL_INIT_SPEED = 0.00001f;

    public CharacterController char_controller;

    [BoxGroup("Config"), Tooltip("每秒步长")]
    public float move_steps_ps = 1f;
    [BoxGroup("Config"), Tooltip("移动速度倍率")]
    public float move_speed = 1f;
    [BoxGroup("Config"), Tooltip("每秒转身度数")]
    public float rotate_degree_ps = 180f;
    [BoxGroup("Config"), Tooltip("转身速度倍率")]
    public float rotate_speed = 1f;
    [BoxGroup("Config")]
    public float jump_init_speed = 1f;
    [BoxGroup("Config")]
    public float gravity = -9.8f;

    [BoxGroup("Status"), ShowNonSerializedField]
    private Vector2 move_dir;
    [BoxGroup("Status"), ShowNonSerializedField]
    private float rotate_to_yaw;
    [BoxGroup("Status"), ShowNonSerializedField]
    private float vertical_speed;
    [BoxGroup("Status"), ShowNonSerializedField]
    private bool in_the_air;

    public Vector3 position
    {
        get => transform.position;
        private set => transform.position = value;
    }

    public Vector3 angles
    {
        get => transform.eulerAngles;
        private set => transform.eulerAngles = value;
    }

    public bool move(Vector2 dir)
    {
        move_dir = dir.normalized;
        rotate_to_yaw = move_dir.calcYaw().uniformAngle360();
        return true;
    }

    public void stopMove()
    {
        move_dir = Vector2.zero;
    }

    public void jump()
    {
        if (!in_the_air)
        {
            vertical_speed = jump_init_speed;
            in_the_air = true;
        }
    }

    private void tick_move(float time, float delta_time)
    {
        var move_vec = Vector3.zero;
        // horizontal
        var vec = move_dir.normalized * move_steps_ps * move_speed * delta_time;
        move_vec.x = vec.x;
        move_vec.z = vec.y;

        // vertical
        if (!in_the_air && !char_controller.isGrounded)
        {
            // fall
            vertical_speed = FALL_INIT_SPEED;
            in_the_air = true;
        }

        if (in_the_air)
        {
            move_vec.y = vertical_speed * delta_time;
        }

        // move
        var collition_flags = char_controller.Move(move_vec);

        // fater move
        if (char_controller.isGrounded)
        {
            vertical_speed = 0f;
            in_the_air = false;
        }
        else if (CollisionFlags.CollidedAbove == collition_flags)
        {
            vertical_speed = FALL_INIT_SPEED;
        }
        else
        {
            vertical_speed += gravity * delta_time;
        }
    }

    private void tick_yaw(float time, float delta_time)
    {
        var current_angles = angles;
        var current_yaw = current_angles.y.uniformAngle360();
        if ((rotate_to_yaw - current_yaw).equalsZero())
        {
            return;
        }

        var delta_degree = rotate_degree_ps * rotate_speed * delta_time;
        current_angles.y = Mathf.MoveTowardsAngle(current_yaw, rotate_to_yaw, delta_degree);
        angles = current_angles;
    }

    #region MonoBehaviour
    private void Update()
    {
        var time = Time.time;
        var delta_time = Time.deltaTime;

        tick_move(time, delta_time);

        tick_yaw(time, delta_time);
    }
    #endregion
}
