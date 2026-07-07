namespace MCV_Module.Controller
{
    public interface IController
    {
        string ControllerName { get; }
        void OnBindView();
    }
}