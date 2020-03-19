using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class InputController : MonoBehaviour
{
    [BoxGroup("Config")]
    public string axis_horizontal_name = "Horizontal";
    [BoxGroup("Config")]
    public string axis_vertical_name = "Vertical";
    [BoxGroup("Config")]
    public float axis_high_pass = 0.1f;
    [BoxGroup("Config")]
    public string button_jump_name = "Jump";

    [BoxGroup("Controller")]
    public PlayerController player_controller;
    [BoxGroup("Controller")]
    public CameraController camera_controller;

    private bool getMoveDir(out Vector2 dir, out Vector2 joystick_dir)
    {
        joystick_dir = new Vector2(Input.GetAxis(axis_horizontal_name), Input.GetAxis(axis_vertical_name));

        if (axis_high_pass > Mathf.Abs(joystick_dir.x) && axis_high_pass > Mathf.Abs(joystick_dir.y))
        {
            dir = Vector2.zero;
            return false;
        }

        var temp_dir = new Vector3(joystick_dir.x, 0, joystick_dir.y);
        temp_dir = Quaternion.Euler(new Vector3(0, camera_controller.angles.y, 0)) * temp_dir;

        dir = temp_dir.toVector2XZ();
        dir.Normalize();
        return true;
    }

    private void tickMove(float time, float delta_time)
    {
        if (getMoveDir(out Vector2 dir, out Vector2 joystick_dir))
        {
            player_controller.move(dir);
            camera_controller.rotateByInput(joystick_dir);
        }
        else
        {
            player_controller.stopMove();
            camera_controller.stopRotateByInput();
        }
    }

    private void tickJump(float time, float delta_time)
    {
        var v = Input.GetAxis(button_jump_name);
        if (v.equalsZero())
        {
            return;
        }

        player_controller.jump();
    }

    #region MonoBehaviour
    private void Update()
    {
        if (null == camera_controller || null == player_controller)
        {
            return;
        }

        var time = Time.time;
        var delta_time = Time.deltaTime;

        tickMove(time, delta_time);
        tickJump(time, delta_time);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawIcon(transform.position, "Cross", true);

        if (Application.isPlaying)
        {
            if (getMoveDir(out Vector2 dir, out Vector2 joystick_dir))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, joystick_dir.normalized * 0.2f);
            }

            var v = Input.GetAxis(button_jump_name);
            if (!v.equalsZero())
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, v * 0.1f);
            }
        }
    }
    #endregion
}
