using System;
using System.Collections.Generic;

namespace MCV_Module.Models.System
{
    [Serializable]
    public class SystemData : DataBase
    {
        public ProjectInfo projectInfo = new ProjectInfo();
        public CopyRight copyRight = new CopyRight();
        public RenderQuality renderQuality = new RenderQuality();
        /// <summary>界面语言（与画质同属“设置类数据”）：冷启动读取，不做运行期热切。</summary>
        public LanguageType languageType = LanguageType.Chinese;
        /// <summary>文本形态（与语言同属“设置类数据”）：冷启动读取，不做运行期热切。</summary>
        public TextType textType = TextType.Legacy;
    }

    [Serializable]
    public class ProjectInfo : DataBase
    {
        public string projectName;
        public string projectNameEn;                       // 英文列（空 = 回退中文）
        public string projectEnglishName;
        public string version;
        public string company;
        public string companyEn;                           // 英文列（空 = 回退中文）

        public ProjectInfo()
        {
            id = "ProjectInfo";
            displayName = "软件信息";
            projectName = "低压电器仿真实训软件";
            projectEnglishName = "Low-Voltage Electrical Appliance Simulation Training Software";
            version = "1.0.0";
            company = "TK";
        }
    }

    [Serializable]
    public class CopyRight : DataBase
    {
        public string copyright;
        public string copyrightEn;                         // 英文列（空 = 回退中文）
        public bool isCopyRight = false;
        public CopyRight()
        {
            id = "CopyRight";
            displayName = "版权信息";
            copyright = "Copyright © 2026 TK. All rights reserved.";
            isCopyRight = true;
        }
    }

    [Serializable]
    public class RenderQuality : DataBase
    {
        public RenderQualityLevel renderQuality = RenderQualityLevel.High;
        public bool qualitySetted = false;
        public RenderQuality()
        {
            id = "RenderQuality";
            displayName = "渲染质量";
            renderQuality = RenderQualityLevel.High;
            qualitySetted = false;
        }
    }

    [Serializable]
    public class LanguageData
    {
        /// <summary>WHY: 界面语言的真源是 SystemData.languageType（设置类数据）；本类只承载文案表，不再存语言选择。</summary>
        public List<LanguageClip> languageClips = new List<LanguageClip>();
    }

    [Serializable]
    public class LanguageClip : DataBase
    {
        public string[] clips;

        // WHY: 路径式 key 的登记来源必须落在条目上才谈得上"反查与清理" —— 光有 key 无法回答"这个节点还在不在"。
        /// <summary>登记来源：prefab 的资产路径（相对 Assets/，含 .prefab）；存量手写 key 为空。</summary>
        public string prefabPath;
        /// <summary>登记来源：prefab 的资产 GUID —— 改名 / 移动后路径会失效、GUID 不会，故孤儿判定先看它。</summary>
        public string prefabGuid;

        public LanguageClip()
        {
            id = "LanguageClip";
            displayName = "语言Clip";
            // WHY: 默认按语言数量开空串开槽，让 Inspector/JSON 直接看到应有槽位数；全空时 TextComponent 判定未填写并回退静态文本。
            int count = Enum.GetNames(typeof(LanguageType)).Length;
            clips = new string[count];
            for (int i = 0; i < count; i++) clips[i] = string.Empty;
        }
    }
}
