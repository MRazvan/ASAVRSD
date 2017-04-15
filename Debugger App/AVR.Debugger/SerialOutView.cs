using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;

namespace AVR.Debugger
{
    public class SerialOutView : DockContent
    {
        private Scintilla _textControl;

        /// <summary>
        /// the background color of the text area
        /// </summary>
        private const int BACK_COLOR = 0x2A211C;

        /// <summary>
        /// default text color of the text area
        /// </summary>
        private const int FORE_COLOR = 0xB7B7B7;

        /// <summary>
        /// change this to whatever margin you want the line numbers to show in
        /// </summary>
        private const int NUMBER_MARGIN = 1;

        public SerialOutView(DockPanel panel)
        {
            this.Text = "Serial out";
            _textControl = new Scintilla();
            _textControl.Dock = System.Windows.Forms.DockStyle.Fill;
            _textControl.WrapMode = WrapMode.None;
            InitScintilla();
            _textControl.ReadOnly = true;
            this.Controls.Add(_textControl);
            this.CloseButtonVisible = false;
            if (panel.DocumentStyle == DocumentStyle.SystemMdi)
            {
                this.MdiParent = this;
                this.Show();
            }
            else this.Show(panel);
        }

        private void InitScintilla()
        {
            _textControl.IndentationGuides = IndentView.LookBoth;
            _textControl.SetSelectionBackColor(true, IntToColor(0x114D9C));
            _textControl.ClearAllCmdKeys();
            _textControl.StyleResetDefault();
            _textControl.Styles[Style.Default].Font = "Consolas";
            _textControl.Styles[Style.Default].Size = 10;
            _textControl.Styles[Style.Default].BackColor = IntToColor(0x212121);
            _textControl.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
            _textControl.StyleClearAll();
            _textControl.Lexer = Lexer.Null;
            _textControl.Zoom = 2;
        }

        public void Append(string data)
        {
                _textControl.ReadOnly = false;
                _textControl.Text += data;
                _textControl.ReadOnly = true;
        }

        private static Color IntToColor(int rgb)
        {
            return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }
    }
}
