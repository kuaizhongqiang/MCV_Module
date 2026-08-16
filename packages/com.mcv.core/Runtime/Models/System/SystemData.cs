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
    }

    [Serializable]
    public class ProjectInfo : DataBase
    {
        public string projectName;
        public string projectEnglishName;
        public string version;
        public string company;

        public ProjectInfo()
        {
            id = "ProjectInfo";
            displayName = "软件信息";
            projectName = "Ebook";
            projectEnglishName = "Ebook";
            version = "1.0.0";
            company = "DefaultCompany";
        }
    }

    [Serializable]
    public class CopyRight : DataBase
    {
        public string copyright;
        public bool isCopyRight = false;
        public CopyRight()
        {
            id = "CopyRight";
            displayName = "版权信息";
            copyright = "Copyright © 2021 DefaultCompany. All rights reserved.";
            isCopyRight = true;
        }
    }

    [Serializable]
    public class RenderQuality : DataBase
    {
        public int renderQuality = 0;
        public bool qualitySetted = false;
        public RenderQuality()
        {
            id = "RenderQuality";
            displayName = "渲染质量";
            renderQuality = 0;
            qualitySetted = false;
        }
    }

    [Serializable]
    public class LanguageData
    {
        public LanguageType languageType = LanguageType.Chinese;
        public List<LanguageClip> languageClips = new List<LanguageClip>();
    }

    [Serializable]
    public class LanguageClip : DataBase
    {
        public string[] clips;

        public LanguageClip()
        {
            id = "LanguageClip";
            displayName = "语言Clip";
            // 默认按语言数量开槽（空字符串）：让 Inspector/JSON 直接看到应有的槽位数；
            // 全空时 TextComponent 判定为未填写，回退静态文本。
            int count = Enum.GetNames(typeof(LanguageType)).Length;
            clips = new string[count];
            for (int i = 0; i < count; i++) clips[i] = string.Empty;
        }
    }
}
