using ScintillaNET;
using System.Drawing;
using System.IO;
using WeifenLuo.WinFormsUI.Docking;

namespace AVR.Debugger
{
    public class SourceCodeView : DockContent, ISourceCodeView
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

        private string _file;
        public string FileName
        {
            get
            {
                return Path.GetFileName(_file);
            }
        }

        public string FilePath
        {
            get
            {
                return Path.GetDirectoryName(_file);
            }
        }

        public SourceCodeView(DockPanel panel)
        {
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
            _textControl.SetSelectionBackColor(true, Utils.IntToColor(0x114D9C));
            _textControl.ClearAllCmdKeys();
            _textControl.StyleResetDefault();
            _textControl.Styles[Style.Default].Font = "Consolas";
            _textControl.Styles[Style.Default].Size = 10;
            _textControl.Styles[Style.Default].BackColor = Utils.IntToColor(0x212121);
            _textControl.Styles[Style.Default].ForeColor = Utils.IntToColor(0xFFFFFF);
            _textControl.StyleClearAll();
            _textControl.Markers[1].Symbol = MarkerSymbol.Background;
            _textControl.Markers[1].SetBackColor(Utils.IntToColor(0xAB616B));
            //_textControl.Styles[Style.Asm.Identifier].ForeColor = IntToColor(0xD0DAE2);
            _textControl.Styles[Style.Cpp.Default].ForeColor = Color.Silver;
            _textControl.Styles[Style.Cpp.Comment].ForeColor = Color.FromArgb(0, 128, 0); // Green
            _textControl.Styles[Style.Cpp.CommentLine].ForeColor = Color.FromArgb(0, 128, 0); // Green
            _textControl.Styles[Style.Cpp.CommentLineDoc].ForeColor = Color.FromArgb(128, 128, 128); // Gray
            _textControl.Styles[Style.Cpp.Number].ForeColor = Color.FromArgb(0xB5, 0xB5, 0xFF);
            _textControl.Styles[Style.Cpp.Word].ForeColor = Color.Blue;
            _textControl.Styles[Style.Cpp.Word2].ForeColor = Color.Blue;
            _textControl.Styles[Style.Cpp.String].ForeColor = Color.Yellow; // Red
            _textControl.Styles[Style.Cpp.Character].ForeColor = Color.FromArgb(163, 21, 21); // Red
            _textControl.Styles[Style.Cpp.Verbatim].ForeColor = Color.FromArgb(163, 21, 21); // Red
            _textControl.Styles[Style.Cpp.StringEol].BackColor = Color.Pink;
            _textControl.Styles[Style.Cpp.Preprocessor].ForeColor = Color.DarkGray;
            _textControl.Lexer = Lexer.Cpp;

            _textControl.Styles[Style.LineNumber].BackColor = Utils.IntToColor(BACK_COLOR);
            _textControl.Styles[Style.LineNumber].ForeColor = Utils.IntToColor(FORE_COLOR);
            _textControl.Styles[Style.IndentGuide].ForeColor = Utils.IntToColor(FORE_COLOR);
            _textControl.Styles[Style.IndentGuide].BackColor = Utils.IntToColor(BACK_COLOR);

            var nums = _textControl.Margins[NUMBER_MARGIN];
            nums.Width = 40;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;
        }

        public void LoadDataFromFile(string path)
        {
            if (File.Exists(path))
            {
                this.Text = Path.GetFileName(path);
                _textControl.ReadOnly = false;
                _textControl.MarkerDeleteAll(1);
                _textControl.Text = string.Empty;
                _textControl.Text = File.ReadAllText(path);
                _textControl.ReadOnly = true;
                _file = path;
            }
        }

        public void ScrollToLine(int line)
        {
            _textControl.MarkerDeleteAll(1);
            _textControl.Lines[line].MarkerAdd(1);
            _textControl.Lines[line].EnsureVisible();
        }

        public void ClearMakers()
        {
            _textControl.MarkerDeleteAll(1);
        }
    }
}
