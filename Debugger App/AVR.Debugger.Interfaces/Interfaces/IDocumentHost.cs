using WeifenLuo.WinFormsUI.Docking;

namespace AVR.Debugger.Interfaces
{
    public interface IDocumentHost
    {
        void OpenFile(string file);
        void AddDocument(string key, DockContent content);
    }
}