using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class CameraController : MonoBehaviour
{
    public new Camera camera;
    public Camera prediction_camera;

    [BoxGroup("Follow")]
    public PlayerController follow_target;
    [BoxGroup("Follow")]
    public Vector3 follow_target_offset;
    [BoxGroup("Follow")]
    public Vector3 follow_view_port = new Vector3(0.5f, 0.5f, 5f);
    [BoxGroup("Follow"), Slider(0.01f, 1.0f)]
    public float follow_smooth_time = 0.3f;
    [BoxGroup("Follow")]
    public float zoom = 1f;

    [BoxGroup("Rotation"), Slider(0.01f, 1.0f)]
    public float yaw_smooth_time = 0.3f;
    [BoxGroup("Rotation"), Slider(0.01f, 1.0f)]
    public float pitch_smooth_time = 0.3f;

    [BoxGroup("Joystick"), Slider(0, 90), Tooltip("摇杆相对于垂直线的角度大于此值时，相机会左右调整yaw值，调整速度和摇杆角度相关")]
    public float joystick_high_pass = 10f;
    [BoxGroup("Joystick"), Tooltip("根据摇杆角度调整yaw值时的最大速度")]
    public float joystick_yaw_speed_max = 60f;
    [BoxGroup("Joystick"), Tooltip("根据摇杆角度调整pitch值时的范围")]
    public Vector2 joystick_pitch_range = new Vector2(10, 80);
    [BoxGroup("Joystick"), Tooltip("根据摇杆方向调整pitch值时的速度")]
    public float joystick_pitch_speed = 30f;

    [BoxGroup("Collision"), Tooltip("球形射线的半径")]
    public float collision_radius = 0.1f;
    [BoxGroup("Collision"), Tooltip("阻挡层")]
    public LayerMask obstacle_layers;
    [BoxGroup("Collision"), Tooltip("预测时，yaw的探测极限变化值")]
    public float yaw_offset_limit = 90;
    [BoxGroup("Collision"), Tooltip("预测时，yaw的探测变化量")]
    public float yaw_offset_delta = 1;
    [BoxGroup("Collision"), Tooltip("预测时，pitch的探测极限绝对值")]
    public Vector2 pitch_offset_limit = new Vector2(-10, 80);
    [BoxGroup("Collision"), Tooltip("预测时，yaw的探测变化量")]
    public float pitch_offset_delta = 1;
    [BoxGroup("Collision"), Tooltip("预测时，viewport_z的减量，适应相机位置与nearClipPlane的差")]
    public float viewport_z_decrease = 0.4f;
    [BoxGroup("Collision"), Tooltip("决策时，角度变化量的参考倍率（乘法）")]
    public float choose_angle_test_scale = 1;
    [BoxGroup("Collision"), Tooltip("决策时，viewport_z变化量的参考倍率（乘法）")]
    public float choose_viewport_z_test_scale = 1;
    [BoxGroup("Collision"), Tooltip("决策时，viewport_z的最小值")]
    public float viewport_z_min = 1;
    [BoxGroup("Collision"), Tooltip("决策时，viewport_z的最大变化量")]
    public float viewport_z_changed_max = 1;


    [BoxGroup("Status"), ShowNonSerializedField]
    private Vector3 follow_current_velocity;
    [BoxGroup("Status"), ShowNonSerializedField]
    private float yaw_current_velocity;
    [BoxGroup("Status"), ShowNonSerializedField]
    private float pitch_current_velocity;

    private Transform camera_transform;
    private CameraForPrediction prediction;

    public Vector3 should_fvp
    {
        get
        {
            var fvp = follow_view_port;
            fvp.z *= zoom;
            return fvp;
        }
    }

    public Vector3 final_fvp
    {
        get
        {
            var fvp = follow_view_port;
            var force_z = prediction.viewport_z;
            fvp.z = force_z.equalsZero() ? fvp.z * zoom : force_z;
            return fvp;
        }
    }

    public Vector3 position
    {
        get => camera_transform.position;
        private set => camera_transform.position = value;
    }

    public Vector3 angles
    {
        get => camera_transform.eulerAngles;
        private set => camera_transform.eulerAngles = value;
    }

    public Vector3 follow_position
    {
        get => follow_target.position + follow_target_offset;
    }

    public void rotateByInput(Vector2 joystick_dir)
    {
        prediction.setJoystickDir(joystick_dir);
    }

    public void stopRotateByInput()
    {
        prediction.setJoystickDir(Vector2.zero);
    }

    // -----------

    private void tick_rotate(float time, float delta_time)
    {
        var current_angles = angles;
        var target_angles = prediction.angles;

        current_angles.y = Mathf.SmoothDampAngle(current_angles.y, target_angles.y, ref yaw_current_velocity, yaw_smooth_time);
        current_angles.x = Mathf.SmoothDampAngle(current_angles.x, target_angles.x, ref pitch_current_velocity, pitch_smooth_time);

        angles = current_angles;
    }

    private void tick_follow(float time, float delta_time)
    {
        if (null == follow_target)
        {
            return;
        }
        var current_p = position;
        var target_p = current_p + (follow_position - camera.ViewportToWorldPoint(final_fvp));
        if (!current_p.Equals(target_p))
        {
            position = Vector3.SmoothDamp(current_p, target_p, ref follow_current_velocity, follow_smooth_time, float.MaxValue, delta_time);
        }
    }

    #region MonoBehaviour
    private void Awake()
    {
        camera_transform = camera.transform;
        prediction_camera.transform.eulerAngles = camera_transform.eulerAngles;
        prediction = new CameraForPrediction(prediction_camera);
    }

    private void Update()
    {
        var time = Time.time;
        var delta_time = Time.deltaTime;

        // 1. 预测 
        prediction.tick(this, time, delta_time);
        // 2. 旋转
        tick_rotate(time, delta_time);
    }

    private void LateUpdate()
    {
        var time = Time.time;
        var delta_time = Time.deltaTime;

        // 追踪
        tick_follow(time, delta_time);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        prediction.drawGizmos(this);
    }
    #endregion
}
