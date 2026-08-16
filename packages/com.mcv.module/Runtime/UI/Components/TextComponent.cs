
using MCV_Module.Managers;
using MCV_Module.Models;
using MCV_Module.Models.System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MCV_Module.UI.Components
{
    public class TextComponent : ComponentBase
    {
        [SerializeField] TextType textType = TextType.Legacy;
        [SerializeField,TextArea(5,10)] string text = "";
        [SerializeField] Font font = null;
        [SerializeField] TMP_FontAsset fontAsset = null;
        [SerializeField] int fontSize = 16;
        [SerializeField] Color color = Color.black;
        [SerializeField] OverrideAlignment alignment = OverrideAlignment.Auto;
        [SerializeField] LanguageClip languageClip = new();
        Text textComponent;
        TextMeshProUGUI TmpTextComponent;
        protected override void Awake()
        {
            base.Awake();
            if (GetComponent<Text>() != null)
            {
                Destroy(GetComponent<Text>());
            }
            if (GetComponent<TextMeshProUGUI>() != null)
            {
                Destroy(GetComponent<TextMeshProUGUI>());
            }
            if (textType == TextType.Legacy)
            {
                textComponent = gameObject.AddComponent<Text>();
            }
            else if (textType == TextType.TextMeshPro)
            {
                TmpTextComponent = gameObject.AddComponent<TextMeshProUGUI>();
            }

            // 指定了多语言条目则按当前语言显示，否则显示静态文本。
            // 有 id 时优先从 JSON（GlobalDataMgr）按 id 反向找回最新内容，改 JSON 即全局生效；
            // 取不到（数据未加载等）回退用字段里的快照。
            if (languageClip != null && !string.IsNullOrEmpty(languageClip.id))
            {
                LanguageClip resolved = ResolveClipById(languageClip.id) ?? languageClip;
                if (HasLocalizedContent(resolved))
                {
                    SetContent(resolved);
                }
                else
                {
                    RefreshText();
                }
            }
            else
            {
                RefreshText();
            }
        }

        /// <summary>从已加载的 LanguageData 中按 id 查找 Clip（反向找回）。</summary>
        LanguageClip ResolveClipById(string id)
        {
            var languageData = GlobalDataMgr.Instance != null ? GlobalDataMgr.Instance.LanguageData : null;
            if (languageData == null || languageData.languageClips == null) return null;
            return languageData.languageClips.Find(c => c.id == id);
        }

        /// <summary>Clip 是否已填写至少一种语言的文本（全空视为未填，回退静态文本）。</summary>
        static bool HasLocalizedContent(LanguageClip clip)
        {
            if (clip == null || clip.clips == null || clip.clips.Length == 0) return false;
            for (int i = 0; i < clip.clips.Length; i++)
                if (!string.IsNullOrEmpty(clip.clips[i])) return true;
            return false;
        }

        #region 工具方法
        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="text"> Legacy Text </param>
        void SetValume(Text text)
        {
            text.text = this.text;
            text.font = this.font;
            text.fontSize = this.fontSize;
            text.color = this.color;
            switch (this.alignment)
            {
                case OverrideAlignment.Auto:
                    break;
                case OverrideAlignment.UpperLeft:
                    text.alignment = TextAnchor.UpperLeft;
                    break;
                case OverrideAlignment.UpperCenter:
                    text.alignment = TextAnchor.UpperCenter;
                    break;
                case OverrideAlignment.UpperRight:
                    text.alignment = TextAnchor.UpperRight;
                    break;
                case OverrideAlignment.MiddleLeft:
                    text.alignment = TextAnchor.MiddleLeft;
                    break;
                case OverrideAlignment.MiddleCenter:
                    text.alignment = TextAnchor.MiddleCenter;
                    break;
                case OverrideAlignment.MiddleRight:
                    text.alignment = TextAnchor.MiddleRight;
                    break;
                case OverrideAlignment.LowerLeft:
                    text.alignment = TextAnchor.LowerLeft;
                    break;
                case OverrideAlignment.LowerCenter:
                    text.alignment = TextAnchor.LowerCenter;
                    break;
                case OverrideAlignment.LowerRight:
                    text.alignment = TextAnchor.LowerRight;
                    break;
            }
        }
        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="text"> Tmp Text </param>
        void SetValume(TextMeshProUGUI text)
        {
            text.text = this.text;
            text.font = this.fontAsset;
            text.fontSize = this.fontSize;
            text.color = this.color;
            switch (this.alignment)
            {
                case OverrideAlignment.Auto:
                    break;
                case OverrideAlignment.UpperLeft:
                    text.alignment = TextAlignmentOptions.TopLeft;
                    break;
                case OverrideAlignment.UpperCenter:
                    text.alignment = TextAlignmentOptions.Top;
                    break;
                case OverrideAlignment.UpperRight:
                    text.alignment = TextAlignmentOptions.TopRight;
                    break;
                case OverrideAlignment.MiddleLeft:
                    text.alignment = TextAlignmentOptions.BaselineLeft;
                    break;
                case OverrideAlignment.MiddleCenter:
                    text.alignment = TextAlignmentOptions.Baseline;
                    break;
                case OverrideAlignment.MiddleRight:
                    text.alignment = TextAlignmentOptions.MidlineRight;
                    break;
                case OverrideAlignment.LowerLeft:
                    text.alignment = TextAlignmentOptions.BottomLeft;
                    break;
                case OverrideAlignment.LowerCenter:
                    text.alignment = TextAlignmentOptions.Bottom;
                    break;
                case OverrideAlignment.LowerRight:
                    text.alignment = TextAlignmentOptions.BottomRight;
                    break;
            }
        }
        #endregion

        public void SetContent(string text)
        {
            this.text = text;
            RefreshText();
        }

        /// <summary>
        /// 设置多语言内容：按当前语言从 LanguageClip.clips 中取对应文本。
        /// clips 按 LanguageType 顺序索引（Chinese=0, English=1…）。
        /// </summary>
        /// <param name="languageClip">多语言文本条目。</param>
        public void SetContent(LanguageClip languageClip)
        {
            if (languageClip == null)
            {
                Debug.LogWarning($"[TextComponent] 传入的 LanguageClip 为空：{name}", this);
                return;
            }
            this.languageClip = languageClip;

            var languageData = GlobalDataMgr.Instance.LanguageData;
            int index = (int)(languageData != null ? languageData.languageType : LanguageType.Chinese);
            if (languageClip.clips == null || index < 0 || index >= languageClip.clips.Length)
            {
                Debug.LogWarning($"[TextComponent] LanguageClip（{languageClip.id}）缺少当前语言文本，索引 {index} 越界", this);
                return;
            }
            this.text = languageClip.clips[index];
            RefreshText();
        }

        /// <summary>把 this.text 同步到当前文本组件。</summary>
        void RefreshText()
        {
            if (textType == TextType.Legacy)
            {
                SetValume(textComponent);
            }
            else if (textType == TextType.TextMeshPro)
            {
                SetValume(TmpTextComponent);
            }
        }

    }
}