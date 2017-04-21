using System.Windows;
using WeifenLuo.WinFormsUI.Docking;

namespace AVR.Debugger.Interfaces
{
    public class WPFDocument : DockContent
    {
        public WPFDocument()
        {
            InitializeComponent();
        }

        public WPFDocument(FrameworkElement element)
        {
            InitializeComponent();
            AddControl(element);
        }

        public void AddControl(FrameworkElement element)
        {
            SuspendLayout();
            elementHost1.Child = element;
            ResumeLayout(true);
        }

        private System.Windows.Forms.Integration.ElementHost elementHost1;

        private void InitializeComponent()
        {
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.SuspendLayout();
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.elementHost1.Location = new System.Drawing.Point(0, 0);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(284, 261);
            this.elementHost1.TabIndex = 0;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = null;
            // 
            // Document
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.elementHost1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Document";
            this.ResumeLayout(false);

        }
    }
}
