using System.Drawing;
using System.IO;
using WeifenLuo.WinFormsUI.Docking;

namespace AVR.Debugger
{
    public class DisassemblyView : DockContent
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

        public DisassemblyView(DockPanel panel)
        {
            _textControl = new Scintilla();
            _textControl.Dock = System.Windows.Forms.DockStyle.Fill;
            _textControl.WrapMode = WrapMode.None;
            InitScintilla();
            _textControl.Text = "Disassembly not available";
            _textControl.ReadOnly = true;
            this.Controls.Add(_textControl);
            this.Text = "Disassembly";
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
            _textControl.Markers[1].Symbol = MarkerSymbol.Background;
            _textControl.Markers[1].SetBackColor(IntToColor(0xAB616B));
            //_textControl.Styles[Style.Asm.Identifier].ForeColor = IntToColor(0xD0DAE2);
            _textControl.Styles[Style.Asm.Comment].ForeColor = IntToColor(0x008000);
            _textControl.Styles[Style.Asm.String].ForeColor = IntToColor(0xFFFF00);
            _textControl.Styles[Style.Asm.Character].ForeColor = IntToColor(0xE95454);
            //_textControl.Styles[Style.Asm.Register].ForeColor = IntToColor(0xE95454);
            _textControl.Styles[Style.Asm.Operator].ForeColor = IntToColor(0xE0E0E0);
            _textControl.Styles[Style.Asm.CpuInstruction].ForeColor = IntToColor(0xFFFF00);
            _textControl.Lexer = Lexer.Asm;
            _textControl.SetKeywords(0, "add adc adiw sub subi sbc sbci sbiw and andi or ori eor com neg sbr cbr inc dec tst clr ser mul muls mulsu fmul fmuls fmulsu rjmp ijmp jmp rcall icall call ret reti cpse cp cpc cpi sbrc sbrs sbic sbis brbs brbc breq brne brcs brcc brsh brlo brmi brpl brge brlt brhs brhc brts brtc brvs brvc brie brid sbi cbi lsl lsr rol ror asr swap bset bclr bst bld sec clc sen cln sez clz sei cli ses cls sev clv set clt seh clh mov movw ldi ld ld ld ld ld ld ldd ld ld ld ldd lds st st st st st st std st st st std sts lpm lpm lpm spm in out push pop nop sleep wdr break");
            _textControl.SetKeywords(2, "r0 r1 r2 r3 r4 r5 r6 r7 r8 r9 r10 r11 r12 r13 r14 r15 r16 r17 r18 r19 r20 r21 r22 r23 r24 r25 r26 r27 r28 r29 r30 r31 xh xl yh yl zh zl x y z -x -y -z +x +y +z x+ y+ z+ x- y- z-");

            _textControl.Styles[Style.LineNumber].BackColor = IntToColor(BACK_COLOR);
            _textControl.Styles[Style.LineNumber].ForeColor = IntToColor(FORE_COLOR);
            _textControl.Styles[Style.IndentGuide].ForeColor = IntToColor(FORE_COLOR);
            _textControl.Styles[Style.IndentGuide].BackColor = IntToColor(BACK_COLOR);

            var nums = _textControl.Margins[NUMBER_MARGIN];
            nums.Width = 40;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;
        }

        public void OpenFile(string file)
        {
            LoadDataFromFile(file);
        }


        public void ScrollToPc(int pc)
        {
            _textControl.TargetStart = 0;
            _textControl.TargetEnd = _textControl.TextLength;
            var line = _textControl.LineFromPosition(_textControl.SearchInTarget($"{pc:x}:"));
            if (line >= 0)
            {
                _textControl.MarkerDeleteAll(1);
                var lineData = _textControl.Lines[line];
                lineData.MarkerAdd(1);
                _textControl.ScrollRange(lineData.Position, lineData.EndPosition);
            }
        }

        #region Zoom

        public void ZoomIn()
        {
            _textControl.ZoomIn();
        }

        public void ZoomOut()
        {
            _textControl.ZoomOut();
        }

        public void ZoomDefault()
        {
            _textControl.Zoom = 0;
        }


        #endregion

        private void LoadDataFromFile(string path)
        {
            if (File.Exists(path))
            {
                _textControl.ReadOnly = false;
                _textControl.MarkerDeleteAll(1);
                _textControl.Text = string.Empty;
                _textControl.Text = GetDisassembly(path);
                _textControl.ReadOnly = true;
            }
        }

        private string GetDisassembly(string file)
        {
            return (new AvrDisassembler().Disassemble(file));
        }

        private static Color IntToColor(int rgb)
        {
            return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }
    }
}
