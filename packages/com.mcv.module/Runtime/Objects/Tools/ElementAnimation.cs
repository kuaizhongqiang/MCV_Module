using System;
using MCV_Module.Utils;
using System.Collections;
using System.Collections.Generic;
using MCV_Module.Models;
using MCV_Module.Objects.Interactives.Elements;
using UnityEngine;

namespace MCV_Module.Objects.Tools
{
    /// <summary>
    /// 旋转动画（一次性：从当前位置转到目标角度）
    /// </summary>
    [Serializable]
    public class ElementRotationAnimation
    {
        [SerializeField] public Transform rotateObj;
        [SerializeField] List<ElementRotationStruct> rotationStructs = new List<ElementRotationStruct>();
        ElementObjBase element;
        Coroutine coroutine = null;
        public List<ElementRotationStruct> RotationStructs
        {
            get => rotationStructs;
            set => rotationStructs = value;
        }
        public ElementRotationAnimation(ElementObjBase element)
        {
            this.element = element;
        }

        public ElementRotationAnimation(ElementObjBase element, Transform obj, List<ElementRotationStruct> structs)
        {
            this.element = element;
            rotateObj = obj;
            rotationStructs = structs;
        }

        public ElementRotationAnimation()
        {
        }
        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="animTag"> 动画标签 </param>
        /// <param name="onComplete"> 播放完成的回调 </param>
        public void Play(string animTag, Action onComplete = null)
        {
            if (rotateObj == null || element == null) return;
            var data = rotationStructs.Find(x => x.animTag == animTag);
            if (data.animTag == null)
            {
                Log.Warning($"ElementRotationAnimation: 未找到动画标签 {animTag}");
                return;
            }
            if (data.duration <= 0)
            {
                // 时长为 0 时直接跳转到目标角度
                SetRotation(data, data.rotationLimitation.y);
                onComplete?.Invoke();
                return;
            }
            Rotate(data, onComplete);
        }

        void Rotate(ElementRotationStruct data, Action onComplete = null)
        {
            if (coroutine != null)
            {
                element.StopCoroutine(coroutine);
            }
            coroutine = element.StartCoroutine(RotateCoroutine(data, onComplete));
        }

        IEnumerator RotateCoroutine(ElementRotationStruct data, Action onComplete)
        {
            float time = 0;
            float current = GetCurrentRotation(data);
            float target = GetTargetRotation(data);
            // 取当前到目标的最短角差（[-180, 180]），避免欧拉角 0/360 翻转导致绕整圈
            float delta = Mathf.DeltaAngle(current, target);
            while (time < data.duration)
            {
                float t = time / data.duration;
                float angle = current + Mathf.Lerp(0, delta, t);
                SetRotation(data, angle);
                time += Time.deltaTime;
                yield return null;
            }
            SetRotation(data, current + delta);
            coroutine = null;
            onComplete?.Invoke();
        }

        float GetCurrentRotation(ElementRotationStruct data)
        {
            switch (data.rotationAxis)
            {
                case ObjAxis.X:
                    return rotateObj.localEulerAngles.x;
                case ObjAxis.Y:
                    return rotateObj.localEulerAngles.y;
                case ObjAxis.Z:
                    return rotateObj.localEulerAngles.z;
                default:
                    return 0;
            }
        }

        float GetTargetRotation(ElementRotationStruct data)
        {
            return data.rotationLimitation.y;
        }

        void SetRotation(ElementRotationStruct data, float angle)
        {
            switch (data.rotationAxis)
            {
                case ObjAxis.X:
                    rotateObj.localEulerAngles = new Vector3(angle, rotateObj.localEulerAngles.y, rotateObj.localEulerAngles.z);
                    break;
                case ObjAxis.Y:
                    rotateObj.localEulerAngles = new Vector3(rotateObj.localEulerAngles.x, angle, rotateObj.localEulerAngles.z);
                    break;
                case ObjAxis.Z:
                    rotateObj.localEulerAngles = new Vector3(rotateObj.localEulerAngles.x, rotateObj.localEulerAngles.y, angle);
                    break;
            }
        }
    }

    [Serializable]
    public struct ElementRotationStruct
    {
        public string animTag;
        public ObjAxis rotationAxis;
        public Vector2 rotationLimitation;
        public float duration;
    }

    /// <summary>
    /// 运行动画（持续旋转，用于电机等）
    /// </summary>
    [Serializable]
    public class ElementRunAnimation
    {
        [SerializeField] public Transform runObj;
        [SerializeField] public ObjAxis rotationAxis = ObjAxis.Z;
        [SerializeField] public float runSpeed = 500f;
        [SerializeField] public float speedChangeDuration = 2f;
        ElementObjBase element;
        Coroutine runCoroutine = null;
        Coroutine speedCoroutine = null;
        float defaultSpeed;
        Vector3 initialRotation;
        bool captured = false;

        public ElementRunAnimation()
        {
        }

        public ElementRunAnimation(ElementObjBase element,
            Transform obj, ObjAxis axis, float speed, float duration)
        {
            this.element = element;
            runObj = obj;
            rotationAxis = axis;
            runSpeed = speed;
            speedChangeDuration = duration;
        }

        public bool IsRunning { get => runCoroutine != null; }
        /// <summary> 当前转速（度/秒） </summary>
        public float Speed { get => runSpeed; }

        /// <summary>
        /// 记录初始状态（首次使用时采集，避免 Unity 反序列化顺序问题）
        /// </summary>
        void CaptureInitial()
        {
            if (captured) return;
            if (runObj != null) initialRotation = runObj.localEulerAngles;
            defaultSpeed = runSpeed;
            captured = true;
        }

        /// <summary>
        /// 开始运行（以当前转速）
        /// </summary>
        public void Play()
        {
            if (runObj == null || element == null) return;
            CaptureInitial();
            // 取消可能仍在进行的减速协程并恢复默认转速，
            // 避免 Stop 把转速降到 0 后 Play 无法再启动（转速保持 0）。
            if (speedCoroutine != null)
            {
                element.StopCoroutine(speedCoroutine);
                speedCoroutine = null;
            }
            runSpeed = defaultSpeed;
            if (runCoroutine == null)
            {
                runCoroutine = element.StartCoroutine(RunningCoroutine());
            }
        }

        /// <summary>
        /// 开始运行，并渐变到指定转速
        /// </summary>
        /// <param name="speed"> 目标转速（度/秒） </param>
        public void Play(float speed)
        {
            Play();
            SetSpeed(speed);
        }

        /// <summary>
        /// 减速停止（转速渐变到 0）
        /// </summary>
        public void Stop()
        {
            SetSpeed(0f);
        }

        /// <summary>
        /// 立即停止运行动画（转速保持，仅供复位使用）
        /// </summary>
        void StopRunning()
        {
            if (runCoroutine != null)
            {
                element.StopCoroutine(runCoroutine);
                runCoroutine = null;
            }
        }

        /// <summary>
        /// 停止并复位到初始角度与初始转速
        /// </summary>
        public void Reset()
        {
            StopRunning();
            if (speedCoroutine != null)
            {
                element.StopCoroutine(speedCoroutine);
                speedCoroutine = null;
            }
            CaptureInitial();
            if (runObj != null) runObj.localEulerAngles = initialRotation;
            runSpeed = defaultSpeed;
        }

        /// <summary>
        /// 渐变转速
        /// </summary>
        void SetSpeed(float speed)
        {
            if (speedCoroutine != null)
            {
                element.StopCoroutine(speedCoroutine);
            }
            speedCoroutine = element.StartCoroutine(SpeedChangeCoroutine(speed));
        }

        IEnumerator RunningCoroutine()
        {
            while (true)
            {
                Rotate(runSpeed * Time.deltaTime);
                yield return null;
            }
        }

        IEnumerator SpeedChangeCoroutine(float speed)
        {
            float time = 0;
            float current = runSpeed;
            while (time < speedChangeDuration)
            {
                float t = time / speedChangeDuration;
                runSpeed = Mathf.Lerp(current, speed, t);
                time += Time.deltaTime;
                yield return null;
            }
            runSpeed = speed;
            speedCoroutine = null;
        }

        void Rotate(float angle)
        {
            switch (rotationAxis)
            {
                case ObjAxis.X:
                    runObj.Rotate(angle, 0, 0, Space.Self);
                    break;
                case ObjAxis.Y:
                    runObj.Rotate(0, angle, 0, Space.Self);
                    break;
                case ObjAxis.Z:
                    runObj.Rotate(0, 0, angle, Space.Self);
                    break;
            }
        }
    }

    /// <summary>
    /// 移动动画（在打开/关闭两个位置之间移动，用于滑块开关等）
    /// </summary>
    [Serializable]
    public class ElementMoveAnimation
    {
        [SerializeField] public Transform moveObj;
        [SerializeField] public Vector2 moveLimitation;
        [SerializeField] public float duration = 0.2f;
        [SerializeField] public ObjAxis moveAxis = ObjAxis.Z;
        /// <summary> 是否处于打开状态（打开对应 moveLimitation.x，关闭对应 moveLimitation.y） </summary>
        public bool Open
        {
            get => open;
            set
            {
                if (element == null || moveObj == null) return;
                CaptureInitial();
                if (open != value)
                {
                    open = value;
                    SetMove(open);
                }
            }
        }
        ElementObjBase element;
        Coroutine moveCoroutine = null;
        bool open = true;
        bool captured = true;
        Vector3 initialPosition;
        bool initialOpen;

        public ElementMoveAnimation()
        {
        }

        public ElementMoveAnimation(ElementObjBase element,
            Transform obj, ObjAxis axis, Vector2 limitation, float duration)
        {
            this.element = element;
            moveObj = obj;
            moveAxis = axis;
            moveLimitation = limitation;
            this.duration = duration;
        }

        /// <summary>
        /// 记录初始状态（首次使用时采集，避免 Unity 反序列化顺序问题；
        /// 初始开合状态根据当前位置与两端距离推断，保证与实际场景一致）
        /// </summary>
        void CaptureInitial()
        {
            if (captured) return;
            if (moveObj != null)
            {
                initialPosition = moveObj.localPosition;
                float current = GetCurrentLocalPos();
                initialOpen = Mathf.Abs(current - moveLimitation.x) < Mathf.Abs(current - moveLimitation.y);
                open = initialOpen;
            }
            captured = true;
        }

        /// <summary>
        /// 停止并复位到初始位置与初始开合状态
        /// </summary>
        public void Reset()
        {
            if (moveCoroutine != null && element != null)
            {
                element.StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
            CaptureInitial();
            if (moveObj != null)
            {
                moveObj.localPosition = initialPosition;
                open = initialOpen;
            }
        }

        void SetMove(bool isOpen)
        {
            if (element == null || moveObj == null) return;
            if (moveCoroutine != null)
            {
                element.StopCoroutine(moveCoroutine);
            }
            moveCoroutine = element.StartCoroutine(MoveCoroutine(isOpen));
        }

        IEnumerator MoveCoroutine(bool isOpen)
        {
            if (duration <= 0)
            {
                SetLocalPos(GetTargetLocalPos(isOpen));
                moveCoroutine = null;
                yield break;
            }
            float time = 0;
            float current = GetCurrentLocalPos();
            float target = GetTargetLocalPos(isOpen);
            while (time < duration)
            {
                float t = time / duration;
                float pos = Mathf.Lerp(current, target, t);
                SetLocalPos(pos);
                time += Time.deltaTime;
                yield return null;
            }
            SetLocalPos(target);
            moveCoroutine = null;
        }

        float GetCurrentLocalPos()
        {
            switch (moveAxis)
            {
                case ObjAxis.X:
                    return moveObj.localPosition.x;
                case ObjAxis.Y:
                    return moveObj.localPosition.y;
                case ObjAxis.Z:
                    return moveObj.localPosition.z;
            }
            return 0;
        }

        float GetTargetLocalPos(bool isOpen)
        {
            if (isOpen) return moveLimitation.x;
            else return moveLimitation.y;
        }

        void SetLocalPos(float pos)
        {
            switch (moveAxis)
            {
                case ObjAxis.X:
                    moveObj.localPosition = new Vector3(pos, moveObj.localPosition.y, moveObj.localPosition.z);
                    break;
                case ObjAxis.Y:
                    moveObj.localPosition = new Vector3(moveObj.localPosition.x, pos, moveObj.localPosition.z);
                    break;
                case ObjAxis.Z:
                    moveObj.localPosition = new Vector3(moveObj.localPosition.x, moveObj.localPosition.y, pos);
                    break;
            }
        }
    }
}
