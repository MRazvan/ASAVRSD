using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AVR.Debugger.Console
{
    public abstract class Condition
    {
        public virtual bool IsValid(object data)
        {
            return false;
        }
    }

    public class IntegerCondition : Condition
    {
        public override bool IsValid(object data)
        {
            return (data is int);
        }
    }

    public class UIntegerCondition : Condition
    {
        public override bool IsValid(object data)
        {
            return (data is uint);
        }
    }

    public static class Converters
    {
        public static object Integer(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;
            int value;
            if (int.TryParse(data, out value))
                return value;

            try
            {
                value = Convert.ToInt32(data, 16);
            }
            catch
            {
                return null;
            }
            return value;
        }

        public static object ToBuffer(string arg)
        {
            // Split at ','
            if (string.IsNullOrEmpty(arg))
                return null;
            var bytes = arg.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            var buffer = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                var data = Integer(bytes[i]);
                if (data == null)

                    return null;
                buffer[i] = (byte)((int) data);
            }
            return buffer;
        }

        public static object UInteger(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;
            uint value;
            if (uint.TryParse(data, out value))
                return value;
            if (uint.TryParse(data.TrimStart('0', 'x', 'X'), NumberStyles.AllowHexSpecifier, null, out value))
                return value;
            return null;
        }
    }

    public static class Conditions
    {
        public static Condition Integer = new IntegerCondition();
        public static Condition UInteger = new UIntegerCondition();
    }

    public class CommandArgument
    {
        public List<Condition> Conditions { get; } = new List<Condition>();
        public string Name { get; set; }
        public object Value { get; set; }
        public Func<string, object> Converter { get; set; }

        public T GetValue<T>()
        {
            return (T)Value;
        }

        public static CommandArgument Create(string name)
        {
            return new CommandArgument()
            {
                Name = name
            };
        }

        public CommandArgument AddCondition(Condition condition)
        {
            Conditions.Add(condition);
            return this;
        }

        public CommandArgument AddConverter(Func<string, object> converter)
        {
            Converter = converter;
            return this;
        }
    }

    public class CommandLine
    {
        private readonly string _cmd;
        private readonly List<CommandArgument> _arguments;
        public bool IsValid { get; set; }
        public Action<CommandLine> Handler { get; set; }

        public static CommandLine Create(string cmd)
        {
            return new CommandLine(cmd);
        }

        private CommandLine(string cmd)
        {
            _cmd = cmd;
            _arguments = new List<CommandArgument>();
        }

        public CommandLine AddHandler(Action<CommandLine> handler)
        {
            Handler = handler;
            return this;
        }

        public T GetArgument<T>(string name)
        {
            var arg = _arguments.FirstOrDefault(a => a.Name.ToLower() == name.ToLower());
            if (arg == null)
                throw new ArgumentOutOfRangeException(nameof(name));
            return arg.GetValue<T>();
        }

        public CommandLine Argument(CommandArgument arg)
        {
            _arguments.Add(arg);
            return this;
        }

        public void Parse(string command)
        {
            IsValid = false;
            _arguments.ForEach(a => a.Value = null);
            var parts = command.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != _arguments.Count + 1)
                return;
            
            // We might have a chance
            if (parts[0].ToLower() != _cmd.ToLower())
                return;

            // Try and parse all the paramters
            for (var i = 0; i < _arguments.Count; i++)
            {
                var arg = parts[i + 1];
                var argData = _arguments[i];
                var convertedData = argData.Converter(arg);
                var valid = argData.Conditions.All(c => c.IsValid(convertedData));
                argData.Value = convertedData;
                if (!valid)
                    return;
            }
            IsValid = true;
        }
    }

    public class CommandProcessor
    {
        public List<CommandLine> Commands { get; } = new List<CommandLine>();
        public Action<string> InvalidCommand;
        public CommandProcessor AddCommand(CommandLine cmd)
        {
            Commands.Add(cmd);
            return this;
        }

        public void Parse(string cmd)
        {
            Commands.ForEach(c => c.Parse(cmd));
            var valid = Commands.FirstOrDefault(c => c.IsValid);
            if (valid != null)
            {
                valid.Handler?.Invoke(valid);
            }
            else
            {
                InvalidCommand?.Invoke(cmd);
            }
        }
    }
}
