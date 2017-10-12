using System;
using System.Collections.Generic;
using System.Linq;
using ELFSharp.DWARF.Enums;
using ELFSharp.ELF.Sections;
using MiscUtil.IO;

namespace ELFSharp.DWARF.Sections
{
    public partial class DebugLineSection
    {
        private readonly DWARFData _data;
        private readonly EndianBinaryReader _stream;
        private Dictionary<CompilationUnit, LineProgram> _lineProgramCache;

        internal DebugLineSection(DWARFData data, ISection section)
        {
            _data = data;
            _stream = section.GetSectionStream();
            LoadLinePrograms();
        }

        public List<FileInfo> GetFiles()
        {
            var files = new List<FileInfo>();
            foreach (var lineProgram in _lineProgramCache.Values.Where(v => v != null))
                files.AddRange(lineProgram.Header.IncludeFiles.Select(f => new FileInfo
                {
                    File = f.Name,
                    Directory = lineProgram.Header.IncludeDirectories[(int) (f.DirIndex - 1)]
                }));
            return files;
        }

        public LineInfo GetLineFromAddress(long address)
        {
            foreach (var lineProgram in _lineProgramCache.Values.Where(v => v != null))
            {
                var line = lineProgram.GetLine(address);
                if (line != null)
                    return line;
            }
            return null;
        }

        private void LoadLinePrograms()
        {
            _lineProgramCache = new Dictionary<CompilationUnit, LineProgram>();
            foreach (var compilationUnit in _data.InfoSection.CompilationUnits)
            {
                var die = compilationUnit.DIEList?.FirstOrDefault();
                if (die == null)
                {
                    _lineProgramCache[compilationUnit] = null;
                    continue;
                }
                var attr = die.Attributes?.FirstOrDefault(a => a.Name == EAttributes.DW_AT_stmt_list);
                if (attr == null)
                {
                    _lineProgramCache[compilationUnit] = null;
                    continue;
                }
                _lineProgramCache[compilationUnit] = new LineProgram(_stream, Convert.ToInt64(attr.Value));
            }
        }
    }
}