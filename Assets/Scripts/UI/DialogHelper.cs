using System;
using MCV_Module.Controller;
using MCV_Module.GlobalManager;

namespace MCV_Module.UI
{
    public static class DialogHelper
    {
        public static void ShowAlert(string title, string message)
        {
            GetDialogController().ShowAlert(title, message);
        }

        public static void ShowConfirm(string title, string message, 
            Action onConfirm, Action onCancel)
        {
            GetDialogController().ShowConfirm(title, message, onConfirm, onCancel);
        }
        
        static DialogController GetDialogController()
        {
            return GlobalControllerMgr.Instance.Find<DialogController>();
        }
    }

    public struct DialogHelperData
    {
        public string title;
        public string content;
        public string confirmText;
        public string cancelText;
        public Action onConfirm;
        public Action onCancel;

        public DialogHelperData(string title, string content, 
            string confirmText, string cancelText, Action onConfirm, Action onCancel = null)
        {
            this.title = title;
            this.content = content;
            this.confirmText = confirmText;
            this.cancelText = cancelText;
            this.onConfirm = onConfirm;
            this.onCancel = onCancel;
        }

    }
}