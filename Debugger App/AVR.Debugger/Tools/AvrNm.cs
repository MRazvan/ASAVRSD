using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace AVR.Debugger
{
    class AvrNm
    {
        public List<Symbol> GetInfo(string file)
        {
            var process = new Process();
            var si = new ProcessStartInfo(ToolUtils.AvrNM);
            si.Arguments = $"-l -a --defined-only -S -g \"{file}\"";
            si.CreateNoWindow = true;
            si.WindowStyle = ProcessWindowStyle.Hidden;
            si.UseShellExecute = false;
            si.RedirectStandardError = true;
            si.RedirectStandardOutput = true;

            process.StartInfo = si;
            process.Start();
            var data = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var symbols = new List<Symbol>();
            var lines = data.Split(new []{ '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
            foreach(var line in lines)
            {
                var sections = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (sections.Length < 4)
                    continue;
                uint start;
                if (!uint.TryParse(sections[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out start)){
                    continue;
                }
                uint size;
                if (!uint.TryParse(sections[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out size)){
                    continue;
                }
                symbols.Add(new Symbol
                {
                    File = sections.Length >= 5 ? sections[4].Substring(0, sections[4].LastIndexOf(':')) : string.Empty,
                    Location = start > 0x00800000 ? start - 0x00800000 : start,
                    Size = size,
                    Name = sections[3],
                    Type = GetSection(sections[2])
                });
            }
            return symbols;
        }

        private SymbolSection GetSection(string v)
        {
            switch (v.ToLower())
            {
                case "t": return SymbolSection.Text;
                case "b": return SymbolSection.Ram;
                default: return SymbolSection.Unknown;
            }
        }
    }
}
