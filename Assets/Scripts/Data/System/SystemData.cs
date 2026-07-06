using System;

namespace MCV_Module.Data.System
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
            projectName = "MCV_Module";
            projectEnglishName = "MCV_Module";
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

}
