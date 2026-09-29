// 由 MCV Editor/创建/UI Panel 生成器生成（2026-09-14）—— 请按需补充业务代码
using System.Collections.Generic;
using MCV_Module.UI;
using MCV_Module.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Panels
{
    /// <summary>TaskInfoPanel 面板</summary>
    [RequireController(typeof(MCV_Module.Controllers.TaskInfoController))]
    public class TaskInfoPanel : TaskPanelBase
    {
        [SerializeField] Transform picsParent;
        [SerializeField] Transform textParent;
        
        
        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion

        #region 初始化
        /// <summary>装配简介页：pics 填进图槽（多余槽位隐藏），textIndex 选出要显示的那段文案。</summary>
        public void Init(List<Sprite> pics, int textIndex)
        {
            int picCount = pics != null ? pics.Count : 0;

            // WHY: 必须遍历全部子物体——旧实现只处理前 picCount 个，8 个槽填 6 张会漏出 2 个没隐藏的空槽
            for (int i = 0; i < picsParent.childCount; i++)
            {
                var slot = picsParent.GetChild(i);
                bool used = i < picCount;
                slot.gameObject.SetActive(used);
                if (!used) continue;

                var image = slot.GetChild(0).GetComponent<Image>();
                if (image != null) image.sprite = pics[i];
            }

            // 文案：一段文案对应一个器件（prefab 里手填，顺序与 ProjectData.clips 一致）
            if (textParent.childCount == 0)
            {
                // WHY: 图槽显隐已经改过布局，这里提前返回也必须刷一次（不能再走"同帧直接 ForceRebuild"的老写法）
                RequestLayoutRebuild();
                return;
            }

            if (textIndex < 0 || textIndex >= textParent.childCount)
            {
                Log.Warning($"[TaskInfoPanel] 文案序号 {textIndex} 越界（prefab 共 {textParent.childCount} 段），回退到 0");
                textIndex = 0;
            }

            for (int i = 0; i < textParent.childCount; i++)
            {
                textParent.GetChild(i).gameObject.SetActive(i == textIndex);
            }

            // WHY: 图槽显隐 + 文案段落切换都改过布局，统一请求一次（等一帧 + 自下而上 + 防重入）：
            //      同帧直接 ForceRebuild 会量到 TMP 形态下还没装配完的文本尺寸；只传一个 root 才能一次刷全（重复请求只保留最后一次）
            RequestLayoutRebuild();
        }
        #endregion

        public override string GetPanelContent()
        {
            string result = "";
            
            return result;
        }
    }
}
