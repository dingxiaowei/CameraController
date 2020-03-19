using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraForPrediction
{
    public Vector3 position { get => transform.position; private set => transform.position = value; }
    public Vector3 angles { get => transform.eulerAngles; private set => transform.eulerAngles = value; }
    public float viewport_z { get; private set; }

    private Camera camera;
    private Transform transform;

    private Vector2 joystick_dir;
    private Vector3 should_position;

    public CameraForPrediction(Camera c)
    {
        camera = c;
        transform = camera.transform;
    }

    public void drawGizmos(CameraController config)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(should_position, config.collision_radius);

        var target_p = config.follow_position;
        var see_ret = canSee(config, position, target_p, out Vector3 collision_p, out float collision_distance);
        if (SeeResult.CAN_SEE == see_ret)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(position, target_p);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(collision_p, target_p);
            Gizmos.DrawWireSphere(collision_p, config.collision_radius);
        }
    }

    public void setJoystickDir(Vector2 dir)
    {
        joystick_dir = dir;
    }

    public void tick(CameraController config, float time, float delta_time)
    {
        // 保持与主相机参数一致性
        tick_camera(config, time, delta_time);

        // 处理摇杆对yaw和pitch的影响
        var can_see = tick_joystick(config, time, delta_time);
        if (!can_see)
        {
            // 通过调整yaw或者pitc以及viewport_z，找到可以看到目标的位置
            tick_see(config, time, delta_time);
        }

        // 根据fvp调整相机位置
        tick_follow(config, time, delta_time);
    }

    // 保持相机参数一致性
    private void tick_camera(CameraController config, float time, float delta_time)
    {
        camera.fieldOfView = config.camera.fieldOfView;
        camera.nearClipPlane = config.camera.nearClipPlane;
        camera.farClipPlane = config.camera.farClipPlane;
        camera.aspect = config.camera.aspect;
    }

    // 处理摇杆对yaw和pitch的影响
    private bool tick_joystick(CameraController config, float time, float delta_time)
    {
        if (joystick_dir.equalsZero())
        {
            return false;
        }

        // 在以下逻辑中，我们将摇杆与中心点连线看成指针

        var yaw_signed_offset = 0f;
        var pitch_signed_offset = 0f;
        var target_angles = angles;

        // 先处理摇杆左右对yaw值的影响
        if (90 > config.joystick_high_pass)
        {
            // 指针相对于表盘up方向的角度
            float joystick_angle_vertial = Vector2.Angle(Vector2.up, joystick_dir);
            // 把角度值换算到+-90
            if (90 < joystick_angle_vertial)
            {
                joystick_angle_vertial = 180 - joystick_angle_vertial;
            }

            // 如果角度值低于设置的高通参数，将不生效，这样可以通过高通参数调整对摇杆左右的敏感度
            if (joystick_angle_vertial > config.joystick_high_pass)
            {
                // adjust yaw
                var speed_scale = (joystick_angle_vertial - config.joystick_high_pass) / (90 - config.joystick_high_pass);
                var delta_angle = config.joystick_yaw_speed_max * speed_scale * delta_time;
                if (0 < joystick_dir.x)
                {
                    yaw_signed_offset = delta_angle;
                }
                else
                {
                    yaw_signed_offset = -delta_angle;
                }
            }
        }

        // 再处理摇杆上下对pitch值的影响
        if (!joystick_dir.y.equalsZero())
        {
            // adjust pitch
            var delta_angle = config.joystick_pitch_speed * delta_time;
            if (0 < joystick_dir.y)
            {
                // 为了避免相机倒转，pitch值的调整范围有最小值
                if (target_angles.x > config.joystick_pitch_range.x)
                {
                    if ((target_angles.x - delta_angle) < config.joystick_pitch_range.x)
                    {
                        delta_angle = target_angles.x - config.joystick_pitch_range.x;
                    }
                    pitch_signed_offset = -delta_angle;
                }
                else
                {
                    pitch_signed_offset = config.joystick_pitch_range.x - target_angles.x;
                }
            }
            else
            {
                // 为了避免相机倒转，pitch值的调整范围有最大值
                if (target_angles.x < config.joystick_pitch_range.y)
                {
                    if ((target_angles.x + delta_angle) > config.joystick_pitch_range.y)
                    {
                        delta_angle = config.joystick_pitch_range.y - target_angles.x;
                    }
                    pitch_signed_offset = delta_angle;
                }
                else
                {
                    pitch_signed_offset = config.joystick_pitch_range.y - target_angles.x;
                }
            }
        }

        // 根据yaw和pitch调整的几种情况，以及每种情况对目标的可见性，来决定最终的yaw和pitch的调整值是否被应用
        var camera_right_vec = transform.right;
        if (!yaw_signed_offset.equalsZero() && !pitch_signed_offset.equalsZero())
        {
            if (rotateCanSee(
                config,
                (target_to_source_vec) => {
                    var temp_vec = target_to_source_vec.rotateByAngleAxis(pitch_signed_offset, camera_right_vec);
                    return temp_vec.rotateByAngles(Vector3.up * yaw_signed_offset);
                },
                out float collision_distance))
            {
                // yaw和pitch同时生效后可见
                target_angles.y += yaw_signed_offset;
                target_angles.x += pitch_signed_offset;
                angles = target_angles;

                viewport_z = collision_distance;
                return true;
            }
        }
        else if (!pitch_signed_offset.equalsZero() && rotateCanSee(
            config,
            (target_to_source_vec) => target_to_source_vec.rotateByAngleAxis(pitch_signed_offset, camera_right_vec),
            out float collision_distance))
        {
            // 仅pitch生效可见
            target_angles.x += pitch_signed_offset;
            angles = target_angles;

            viewport_z = collision_distance;
            return true;
        }
        else if (!yaw_signed_offset.equalsZero() && rotateCanSee(
            config,
            (target_to_source_vec) => target_to_source_vec.rotateByAngles(Vector3.up * yaw_signed_offset),
            out collision_distance))
        {
            // 仅yaw生效可见
            target_angles.y += yaw_signed_offset;
            angles = target_angles;

            viewport_z = collision_distance;
            return true;
        }

        return false;
    }

    // 通过调整yaw或者pitc以及viewport_z，找到可以看到目标的位置
    private void tick_see(CameraController config, float time, float delta_time)
    {
        var source_p = should_position;
        var target_p = config.follow_position;

        // 1. 尝试正常的viewport
        var see_ret = canSee(config, source_p, target_p, out Vector3 collision_p, out float collision_distance);
        if (SeeResult.CAN_SEE == see_ret)
        {
            // 恢复到正常viewport_z
            viewport_z = 0;
            return;
        }
        else if (SeeResult.CAN_SEE_SINGLE == see_ret)
        {
            viewport_z = collision_distance;
            return;
        }

        // 2. 确定当前位置的可见性
        see_ret = canSee(config, position, target_p, out collision_p, out collision_distance);
        if (SeeResult.CAN_SEE == see_ret)
        {
            return;
        }

        // 3. 尝试旋转找到可见位置
        var target_to_source_vec = source_p - target_p;

        var current_distance = Vector3.Distance(position, target_p);

        var positive_yaw_offset = findOutCanSeeAngleOffset(
            config,
            config.yaw_offset_delta,
            config.yaw_offset_limit,
            (signed_offset) => target_to_source_vec.rotateByAngles(Vector3.up * signed_offset),
            source_p, target_p, false,
            out float positive_yaw_viewport_z);
        var positive_yaw_viewport_z_offset = positive_yaw_viewport_z.equalsZero() ? 0 : Mathf.Abs(positive_yaw_viewport_z - current_distance);

        var negative_yaw_offset = findOutCanSeeAngleOffset(
            config,
            config.yaw_offset_delta,
            config.yaw_offset_limit,
            (signed_offset) => target_to_source_vec.rotateByAngles(Vector3.up * signed_offset),
            source_p, target_p, true,
            out float negative_yaw_viewport_z);
        var negative_yaw_viewport_z_offset = negative_yaw_viewport_z.equalsZero() ? 0 : Mathf.Abs(negative_yaw_viewport_z - current_distance);


        var current_angles = angles;
        var current_pitch = current_angles.x;
        if (180 < current_pitch)
        {
            current_pitch = current_pitch - 360;
        }
        else if (-180 > current_pitch)
        {
            current_pitch = current_pitch + 360;
        }
        var camera_right_vec = transform.right;

        var positive_pitch_offset = findOutCanSeeAngleOffset(
            config,
            config.pitch_offset_delta,
            config.pitch_offset_limit.y - current_pitch,
            (signed_offset) => target_to_source_vec.rotateByAngleAxis(signed_offset, camera_right_vec),
            source_p, target_p, false,
            out float positive_pitch_viewport_z);
        var positive_pitch_viewport_z_offset = positive_pitch_viewport_z.equalsZero() ? 0 : Mathf.Abs(positive_pitch_viewport_z - current_distance);

        var negative_pitch_offset = findOutCanSeeAngleOffset(
            config,
            config.pitch_offset_delta,
            current_pitch - config.pitch_offset_limit.x,
            (signed_offset) => target_to_source_vec.rotateByAngleAxis(signed_offset, camera_right_vec),
            source_p, target_p, true,
            out float negative_pitch_viewport_z);
        var negative_pitch_viewport_z_offset = negative_pitch_viewport_z.equalsZero() ? 0 : Mathf.Abs(negative_pitch_viewport_z - current_distance);

        // 从四个不同的旋转调整选项中，找到最合适的调整选项并应用
        var positive_yaw_choose_test = calcChooseTest(config, positive_yaw_offset, positive_yaw_viewport_z_offset);
        var negative_yaw_choose_test = calcChooseTest(config, negative_yaw_offset, negative_yaw_viewport_z_offset);
        var positive_pitch_choose_test = calcChooseTest(config, positive_pitch_offset, positive_pitch_viewport_z_offset);
        var negative_pitch_choose_test = calcChooseTest(config, negative_pitch_offset, negative_pitch_viewport_z_offset);

        // 测试值越小越合适
        if (positive_yaw_choose_test < negative_yaw_choose_test && positive_yaw_choose_test < positive_pitch_choose_test && positive_yaw_choose_test < negative_pitch_choose_test)
        {
            current_angles.y += positive_yaw_offset;
            viewport_z = positive_yaw_viewport_z;
            angles = current_angles;
        }
        else if (negative_yaw_choose_test < positive_pitch_choose_test && negative_yaw_choose_test < negative_pitch_choose_test)
        {
            current_angles.y -= negative_yaw_offset;
            viewport_z = negative_yaw_viewport_z;
            angles = current_angles;
        }
        else if (positive_pitch_choose_test < negative_pitch_choose_test)
        {
            current_angles.x += positive_pitch_offset;
            viewport_z = positive_pitch_viewport_z;
            angles = current_angles;
        }
        else if (360 > negative_pitch_choose_test)
        {
            current_angles.x -= negative_pitch_offset;
            viewport_z = negative_pitch_viewport_z;
            angles = current_angles;
        }
        else
        {
            // 没有任何旋转调整选项的话，如果仅zoom便可见且zoom值在设定的最小值以上，则使用zoom，否则不可
            if (config.viewport_z_min < collision_distance)
            {
                viewport_z = collision_distance;
            }
            else
            {
                // TODO 此时可以通过屏幕指示来告知玩
                Debug.Log("can't see");
            }
        }
    }

    // 根据fvp调整相机位置
    private void tick_follow(CameraController config, float time, float delta_time)
    {
        if (null == config.follow_target)
        {
            return;
        }
        var target_p = config.follow_position;
        should_position = position + (target_p - camera.ViewportToWorldPoint(config.should_fvp));
        position += target_p - camera.ViewportToWorldPoint(config.final_fvp);
    }

    // 判断旋转后是否对目标可见
    private bool rotateCanSee(CameraController config, System.Func<Vector3, Vector3> rotate_func, out float collision_distance)
    {
        var target_p = config.follow_position;

        var source_p = should_position;
        var target_to_source_vec = source_p - target_p;
        source_p = target_p + rotate_func(target_to_source_vec);

        var see_ret = canSee(config, source_p, target_p, out Vector3 _, out collision_distance);
        if (SeeResult.CAN_NOT_SEE == see_ret)
        {
            source_p = position;
            target_to_source_vec = source_p - target_p;
            source_p = target_p + rotate_func(target_to_source_vec);

            see_ret = canSee(config, source_p, target_p, out Vector3 _, out collision_distance);
        }

        return SeeResult.CAN_NOT_SEE != see_ret;
    }

    private enum SeeResult
    {
        CAN_NOT_SEE,    // 看不见目标，目标也看不见镜头
        CAN_SEE,        // 目标和镜头互相可以看到
        CAN_SEE_SINGLE, // 目标看不见镜头，但镜头可以看到目标（镜头可能在碰撞体内部）
    }
    // 计算镜头和目标的相互可见性
    private static SeeResult canSee(CameraController config, Vector3 camera_p, Vector3 target_p, out Vector3 collision_p, out float collision_distance)
    {
        var dir = target_p - camera_p;
        var dist = dir.magnitude;

        collision_distance = 0;

        var see = !Uti_Physics.sphereCast(target_p, config.collision_radius, (dir * -1f), config.obstacle_layers, out RaycastHit hit, out collision_p, dist);
        if (see)
        {
            return SeeResult.CAN_SEE;
        }

        collision_p = Vector3.MoveTowards(collision_p, target_p, config.viewport_z_decrease);
        collision_distance = Vector3.Distance(target_p, collision_p);

        var see_single = !Uti_Physics.sphereCast(camera_p, config.collision_radius, dir, config.obstacle_layers, out hit, out Vector3 _, dist);
        if (see_single)
        {
            if (collision_distance > config.viewport_z_min && Mathf.Abs(dir.magnitude - collision_distance) < config.viewport_z_changed_max)
            {
                return SeeResult.CAN_SEE_SINGLE;
            }
        }

        return SeeResult.CAN_NOT_SEE;
    }

    // 计算选项的测试值（结合角度偏移值和zoom值，以及它们各自设定的权值）
    private static float calcChooseTest(CameraController config, float angle_offset, float viewport_z_offset)
    {
        var choose_test = float.MaxValue;
        if (360 > angle_offset)
        {
            choose_test = angle_offset * config.choose_angle_test_scale + viewport_z_offset * config.choose_viewport_z_test_scale;
        }
        return choose_test;
    }

    // 找到可以看见目标的位置所需要的角度偏移值
    private static float findOutCanSeeAngleOffset(
        CameraController config,
        float offset_delta,
        float offset_limit,
        System.Func<float, Vector3> vec_rotate_func,
        Vector3 source_p, Vector3 target_p, bool negative_rotate,
        out float collision_distance)
    {
        int offset_sign = negative_rotate ? -1 : 1;

        float offset = 0;
        collision_distance = 0;
        SeeResult see_ret = SeeResult.CAN_NOT_SEE;

        Vector3 collision_p = source_p;
        var current_distance = Vector3.Distance(source_p, target_p);

        for (; offset < offset_limit && SeeResult.CAN_NOT_SEE == see_ret; offset += offset_delta)
        {
            source_p = target_p + vec_rotate_func(offset * offset_sign);
            see_ret = canSee(config, source_p, target_p, out collision_p, out collision_distance);
        }

        if (SeeResult.CAN_NOT_SEE == see_ret)
        {
            offset = float.MaxValue;
        }

        return offset;
    }
}
