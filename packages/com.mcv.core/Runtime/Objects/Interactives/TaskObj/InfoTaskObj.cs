using System.Collections;
using MCV_Module.Models;
using UnityEngine;

namespace MCV_Module.Objects.Interactives.TaskObj
{
    // WHY: 停用时 Unity 会自动终止协程——OnDisable 必须手动 StopRotate 并清 currentSpeed，否则下次启用误判协程仍在跑
    /// <summary>信息展示物：绕指定轴（默认 Y）持续旋转；鼠标移入惯性减速停、移出惯性加速到目标转速</summary>
    public class InfoTaskObj : InteractiveBase
    {
        #region 序列化参数
        [SerializeField] float rotateSpeed = 50f;
        [SerializeField] float InertiaTime = 0.5f;
        [SerializeField] ObjAxis rotateAxis = ObjAxis.Y;
        #endregion

        #region 私有字段
        /// <summary> 当前转速（度/秒），由惯性协程渐变 </summary>
        float currentSpeed;
        Coroutine rotateCoroutine;
        Coroutine inertiaCoroutine;
        #endregion

        #region 私有方法
        /// <summary>设置旋转开关：true 惯性加速到目标转速，false 惯性减速到停止</summary>
        void SetObjRotate(bool rotate)
        {
            if (rotate && rotateCoroutine == null)
            {
                rotateCoroutine = StartCoroutine(RotateCoroutine());
            }
            SetSpeed(rotate ? rotateSpeed : 0f);
        }

        /// <summary>转速惯性渐变到目标转速（度/秒）</summary>
        void SetSpeed(float speed)
        {
            if (inertiaCoroutine != null) StopCoroutine(inertiaCoroutine);
            inertiaCoroutine = StartCoroutine(InertiaCoroutine(speed));
        }

        /// <summary>转速惯性协程：当前转速线性渐变到目标；结束后若为 0 则停止旋转</summary>
        IEnumerator InertiaCoroutine(float targetSpeed)
        {
            if (InertiaTime > 0f)
            {
                float start = currentSpeed;
                float time = 0f;
                while (time < InertiaTime)
                {
                    time += Time.deltaTime;
                    currentSpeed = Mathf.Lerp(start, targetSpeed, Mathf.Clamp01(time / InertiaTime));
                    yield return null;
                }
            }
            currentSpeed = targetSpeed;
            inertiaCoroutine = null;
            // 转速降到 0：停掉旋转协程，等待下次启动
            if (Mathf.Abs(currentSpeed) < 0.001f) StopRotate();
        }

        /// <summary>持续旋转协程：按当前转速绕自身轴旋转</summary>
        IEnumerator RotateCoroutine()
        {
            while (true)
            {
                float angle = currentSpeed * Time.deltaTime;
                switch (rotateAxis)
                {
                    case ObjAxis.X:
                        transform.Rotate(angle, 0f, 0f, Space.Self);
                        break;
                    case ObjAxis.Y:
                        transform.Rotate(0f, angle, 0f, Space.Self);
                        break;
                    case ObjAxis.Z:
                        transform.Rotate(0f, 0f, angle, Space.Self);
                        break;
                }
                yield return null;
            }
        }

        /// <summary>立即停止旋转协程（不改变转速）</summary>
        void StopRotate()
        {
            if (rotateCoroutine == null) return;
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }
        #endregion

        #region 生命周期
        void OnEnable()
        {
            // 启用时自动开始旋转（含首次激活）
            SetObjRotate(true);
        }

        void OnDisable()
        {
            // 对象停用时 Unity 会自动终止协程，这里只清理状态，避免下次启用时误判协程仍在运行
            StopRotate();
            if (inertiaCoroutine != null)
            {
                StopCoroutine(inertiaCoroutine);
                inertiaCoroutine = null;
            }
            currentSpeed = 0f;
        }
        #endregion

        #region 事件重写
        protected override void MoEnterEvent()
        {
            // 鼠标移入：惯性减速停止
            SetObjRotate(false);
        }

        protected override void MoExitEvent()
        {
            // 鼠标移出：惯性加速到目标转速
            SetObjRotate(true);
        }
        #endregion
    }
}
