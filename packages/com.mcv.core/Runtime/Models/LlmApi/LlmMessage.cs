using System;
using System.Collections.Generic;

namespace MCV_Module.Models.LlmApi
{
    [Serializable]
    public class Message
    {
        
    }
    [Serializable]
    public class Request
    {
        public bool thinking = false;
        public string effort = "high";
        public string temperature = "0.5";
        public bool streaming = false;  
    }
    [Serializable]
    public class Response
    {
        
    }
    [Serializable]
    public class Error
    {
        
    }
    [Serializable]
    public class Reasoning
    {
        
    }
    [Serializable]
    public class Content
    {
        
    }

    [Serializable]
    public class ReasoningCache
    {
        
    }

    public class ContentCache
    {
        
    }
}