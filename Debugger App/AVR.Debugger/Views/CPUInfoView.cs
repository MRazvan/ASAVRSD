using ScintillaNET;
using System.Text;
using WeifenLuo.WinFormsUI.Docking;

namespace AVR.Debugger
{
    public class CPUInfoView : DockContent
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

        public CPUInfoView()
        {
            this.Text = "Cpu Info";
            _textControl = new Scintilla();
            _textControl.Dock = System.Windows.Forms.DockStyle.Fill;
            _textControl.WrapMode = WrapMode.None;
            InitScintilla();
            _textControl.ReadOnly = true;
            this.Controls.Add(_textControl);
            this.CloseButtonVisible = false;
        }

        private void InitScintilla()
        {
            _textControl.IndentationGuides = IndentView.LookBoth;
            _textControl.SetSelectionBackColor(true, Utils.IntToColor(0x114D9C));
            _textControl.ClearAllCmdKeys();
            _textControl.StyleResetDefault();
            _textControl.Styles[Style.Default].Font = "Consolas";
            _textControl.Styles[Style.Default].Size = 10;
            _textControl.Styles[Style.Default].BackColor = Utils.IntToColor(0x212121);
            _textControl.Styles[Style.Default].ForeColor = Utils.IntToColor(0xFFFFFF);
            _textControl.StyleClearAll();
            _textControl.Lexer = Lexer.Null;
            _textControl.Zoom = 2;
        }

        public void SetCpuInfo(CpuState state)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"PC      = 0x{(state.PC * 2):X4}");
            sb.AppendLine($"Stack   = 0x{state.Stack:X4}");
            sb.AppendLine();
            for(var i = 0; i < state.Registers.Length; ++i)
            {
                if (i % 4 == 0)
                    sb.AppendLine();

                sb.Append($"R{(i)} = 0x{state.Registers[i]:X2}\t");
            }
            _textControl.ReadOnly = false;
            _textControl.Text = sb.ToString();
            _textControl.ReadOnly = true;
        }
    }
}
