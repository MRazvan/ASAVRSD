using WeifenLuo.WinFormsUI.Docking;

namespace AVR.Debugger.Interfaces
{
    public class Document : DockContent
    {
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Document
            // 
            this.ClientSize = new System.Drawing.Size(276, 236);
            this.Font = new System.Drawing.Font("Segoe UI", 10.2F);
            this.Name = "Document";
            this.ResumeLayout(false);

        }
    }
}
