﻿namespace AVR.Debugger
{
    interface ISourceCodeView
    {
        string FileName { get; }
        string FilePath { get; }

        void LoadDataFromFile(string file);
        void ScrollToLine(int line);
        void ClearMakers();
    }
}
