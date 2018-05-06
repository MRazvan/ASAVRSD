using System;
using System.Linq;
using System.Threading;
using Debugger.Server;
using Debugger.Server.Commands;
using Debugger.Server.Transports;
using SysConsole = System.Console;

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable InconsistentNaming

namespace AVR.Debugger.Console
{
    class Program
    {
        private static readonly byte[] CmdData = { 0xFF };
        private static DebugServer _srv;
        private static Thread _th;
        private static CommandProcessor _commandProcessor;

        // LIMITS
        //  WRITING TO UART REGISTERS WILL STOP THE COMMUNICATION
        static void Main(string[] args)
        {
            SetupCommandProcessor();

            var transport = new SerialTransport();
            transport.SetPort("COM4");
            transport.SetSpeed(500000);
            
            _srv = new DebugServer();
            _srv.SetTransport(transport);
            _srv.DebuggerAttached += _srv_DebuggerAttached;
            _srv.DebuggerDetached += _srv_DebuggerDetached;
            _srv.UnknownData += SrvOnUnknownData;
            _th = new Thread(CommandLoop);

            SysConsole.WriteLine("Available register names");
            SysConsole.WriteLine("**************************************");
            SysConsole.WriteLine(Registers.RegisterMappings.Aggregate("", (s, pair) => s + ", " + pair.Key).Trim(','));
            SysConsole.WriteLine("**************************************");
            SysConsole.WriteLine("Available commands");
            SysConsole.WriteLine("c - Continue execution");
            SysConsole.WriteLine("t - Toggle all of PORTB (usually a led is on PB5)");
            SysConsole.WriteLine("w - write to memory, w (location, or IO reg) data");
            SysConsole.WriteLine("    the data is in the following format (number)[,(number)]");
            SysConsole.WriteLine("    Exp. ");
            SysConsole.WriteLine("    w pinb 0xFF");
            SysConsole.WriteLine("    w 0x0123 0xFF,0xF0,0xF1,100,255");
            SysConsole.WriteLine("r - read from memory, r (location, or IO reg) size");
            SysConsole.WriteLine("    Exp. ");
            SysConsole.WriteLine("    r pinb 1");
            SysConsole.WriteLine("    r 0x0123 0xFF");
            SysConsole.WriteLine("**************************************");

            SysConsole.WriteLine("Press any key to continue");
            SysConsole.ReadLine();

            _srv.Start();
            _th.Start();
            SysConsole.WriteLine("Powercycle the target");

            // Block everything 
            _th.Join();
        }

        private static void SrvOnUnknownData(byte data)
        {
            SysConsole.Write(Convert.ToChar(data));
        }

        private static void _srv_DebuggerDetached()
        {
            SysConsole.WriteLine("Debugger detached");
            SysConsole.WriteLine("**************************************");
            SysConsole.WriteLine();
            SysConsole.WriteLine();
            SysConsole.WriteLine();
        }

        private static void _srv_DebuggerAttached()
        {
            SysConsole.WriteLine();
            SysConsole.WriteLine();
            SysConsole.WriteLine();
            SysConsole.WriteLine("**************************************");
            SysConsole.WriteLine("Debugger attached");
            SysConsole.WriteLine($"CAPS       {_srv.Caps}");
            SysConsole.WriteLine($"Version    {_srv.DebugVersion}");
            SysConsole.WriteLine($"Signature  0x{_srv.DeviceSignature:X6}");
            SysConsole.WriteLine("**************************************");
            SysConsole.WriteLine("Enter commands");
            SysConsole.WriteLine("**************************************");
        }

        private static void CommandLoop()
        {
            while (true)
            {
                var cmd = SysConsole.ReadLine()?.ToLower();
                _commandProcessor.Parse(cmd);
            }
        }

        private static void SetupCommandProcessor()
        {
            // For now the only IO registers recognized are PINB and DDRB
            _commandProcessor = new CommandProcessor();
            _commandProcessor
                .AddCommand(CommandLine.Create("t")
                    .AddHandler(Toggle))
                .AddCommand(CommandLine.Create("c")
                    .AddHandler(Continue))
                // Read command in the following format
                //  r (location, pin id) (size)
                //  r 0x23 1
                //  r 0x23 0x10
                //  r 0x25 100
                //  r pinb 0xff
                .AddCommand(CommandLine.Create("r")
                    .AddHandler(Read)
                    .Argument(CommandArgument.Create("location")
                        .AddConverter(s => Converters.UInteger(s) ?? Registers.RegisterToAddress(s))
                        .AddCondition(Conditions.UInteger))
                    .Argument(CommandArgument.Create("size")
                        .AddConverter(Converters.UInteger)
                        .AddCondition(Conditions.UInteger))
                )
                // Write command in the following format
                // w (location, pin id) (data delimited by , without space)
                // w 0x23 0xFF
                // w 0x25 0xFF,0x12,0x30
                // w pinb 0xff 
                .AddCommand(CommandLine.Create("w")
                    .AddHandler(Write)
                    .Argument(CommandArgument.Create("location")
                        .AddConverter(s => Converters.UInteger(s) ?? Registers.RegisterToAddress(s))
                        .AddCondition(Conditions.UInteger))
                    .Argument(CommandArgument.Create("data")
                        .AddConverter(Converters.ToBuffer))
                );

            _commandProcessor.InvalidCommand = s => SysConsole.WriteLine($"Invalid command {s}");
        }

        private static void Continue(CommandLine cmd)
        {
            _srv.Continue();
        }

        private static void Write(CommandLine cmdLine)
        {
            if ((_srv.Caps & DebuggerCapabilities.CAPS_RAM_W_BIT) != DebuggerCapabilities.CAPS_RAM_W_BIT)
            {
                SysConsole.WriteLine("No support for RAM change");
                return;
            }

            var location = cmdLine.GetArgument<uint>("location");
            var data = cmdLine.GetArgument<byte[]>("data");
            _srv.AddCommand(new DebugCommand_Ram_Write(location, data));
            _srv.AddCommand(new DebugCommand_Ram_Read(location, (uint)data.Length)
            {
                Done = response =>
                {
                    for (int i = 0; i < response.Length; i++)
                    {
                        if (data[i] != response[i])
                        {
                            Console.WriteLine("The data does not match");
                            return;
                        }
                    }
                    Console.WriteLine("Done.");
                }
            });
        }

        private static void Read(CommandLine cmdLine)
        {
            var read_location = cmdLine.GetArgument<uint>("location");
            var size = cmdLine.GetArgument<uint>("size");
            _srv.AddCommand(new DebugCommand_Ram_Read(read_location, size)
            {
                Done = response =>
                {
                    response.ToList().ForEach(b => Console.Write($"0x{b:X2}"));
                    Console.WriteLine();
                }
            });
        }

        private static void Toggle(CommandLine cmdLine)
        {
            if ((_srv.Caps & DebuggerCapabilities.CAPS_RAM_W_BIT) != DebuggerCapabilities.CAPS_RAM_W_BIT)
            {
                SysConsole.WriteLine("No support for RAM change");
                return;
            }
            // toggle all of port B
            // DDRB Register fill with 0xFF - making all of port B output
            _srv.AddCommand(new DebugCommand_Ram_Write(Registers.DDRB, CmdData));
            // PINB register fill with 0xFF, this will toggle the port value
            _srv.AddCommand(new DebugCommand_Ram_Write(Registers.PINB, CmdData));
        }

    }
}
