using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SoftwareDebuggerExtension.SDebugger;

namespace SoftwareDebuggerExtension
{
    internal class VSTraceListener : TextWriterTraceListener
    {
        private readonly List<IExtensionLogger> _loggers;

        public static VSTraceListener Instance { get; private set; }

        static VSTraceListener()
        {
            Instance = new VSTraceListener();
        }

        private VSTraceListener()
        {
            _loggers = new List<IExtensionLogger> {new FileTraceWriter()};
        }

        public void AddOutputPaneLogger(Output output)
        {
            _loggers.Add(new OutputPanelLogger(output));
        }

        public void SetVerboseOutput(bool verbose)
        {
            if (verbose)
            {
                Trace.Listeners.Add(this);
            }
            else
            {
                Trace.Listeners.Remove(this);
            }
        }

        public override void Write(string message)
        {
            _loggers.ForEach(l => l.Write(message));
        }

        public override void WriteLine(string message)
        {
            _loggers.ForEach(l => l.WriteLine(message));
        }

        public void LogException(string message, Exception exception)
        {
            WriteLine(message);
            WriteLine(exception.Message);
            WriteLine(exception.StackTrace);
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                WriteLine(exception.Message);
                WriteLine(exception.StackTrace);
            }
        }
    }

    interface IExtensionLogger
    {
        void WriteLine(string message);
        void Write(string message);
    }

    class FileTraceWriter : IExtensionLogger
    {
        private readonly StreamWriter _writer;

        public FileTraceWriter()
        {
            _writer = new StreamWriter(Path.Combine(PathHelper.GetExtensionAppDataFolder(), "software-debug.txt"));
        }

        public void WriteLine(string message)
        {
            _writer.WriteLine(message);
            _writer.Flush();
        }

        public void Write(string message)
        {
            _writer.Write(message);
            _writer.Flush();
        }
    }

    class OutputPanelLogger : IExtensionLogger
    {
        private readonly Output _output;

        public OutputPanelLogger(Output output)
        {
            _output = output;
        }

        public void WriteLine(string message)
        {
            _output.DebugOutLine(message);
        }

        public void Write(string message)
        {
            _output.DebugOut(message);
        }
    }
}