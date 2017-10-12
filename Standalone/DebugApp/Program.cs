using System;
using System.Linq;
using System.Threading;
using Debugger.Server;
using Debugger.Server.Commands;
using Debugger.Server.Transports;

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable InconsistentNaming

namespace DebugApp
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

            Console.WriteLine("Available register names");
            Console.WriteLine("**************************************");
            Console.WriteLine(Registers.RegisterMappings.Aggregate("", (s, pair) => s + ", " + pair.Key).Trim(','));
            Console.WriteLine("**************************************");
            Console.WriteLine("Available commands");
            Console.WriteLine("c - Continue execution");
            Console.WriteLine("t - Toggle all of PORTB (usually a led is on PB5)");
            Console.WriteLine("w - write to memory, w (location, or IO reg) data");
            Console.WriteLine("    the data is in the following format (number)[,(number)]");
            Console.WriteLine("    Exp. ");
            Console.WriteLine("    w pinb 0xFF");
            Console.WriteLine("    w 0x0123 0xFF,0xF0,0xF1,100,255");
            Console.WriteLine("r - read from memory, r (location, or IO reg) size");
            Console.WriteLine("    Exp. ");
            Console.WriteLine("    r pinb 1");
            Console.WriteLine("    r 0x0123 0xFF");
            Console.WriteLine("**************************************");

            Console.WriteLine("Press any key to continue");
            Console.ReadLine();

            _srv.Start();
            _th.Start();
            Console.WriteLine("Powercycle the target");

            // Block everything 
            _th.Join();
        }

        private static void SrvOnUnknownData(byte data)
        {
            Console.Write(Convert.ToChar(data));
        }

        private static void _srv_DebuggerDetached()
        {
            Console.WriteLine("Debugger detached");
            Console.WriteLine("**************************************");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }

        private static void _srv_DebuggerAttached()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("**************************************");
            Console.WriteLine("Debugger attached");
            Console.WriteLine($"CAPS       {_srv.Caps}");
            Console.WriteLine($"Version    {_srv.DebugVersion}");
            Console.WriteLine($"Signature  0x{_srv.DeviceSignature:X6}");
            Console.WriteLine("**************************************");
            Console.WriteLine("Enter commands");
            Console.WriteLine("**************************************");
        }

        private static void CommandLoop()
        {
            while (true)
            {
                var cmd = Console.ReadLine()?.ToLower();
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

            _commandProcessor.InvalidCommand = s => Console.WriteLine($"Invalid command {s}");
        }

        private static void Continue(CommandLine cmd)
        {
            _srv.Continue();
        }

        private static void Write(CommandLine cmdLine)
        {
            if ((_srv.Caps & DebuggerCapabilities.CAPS_RAM_W_BIT) != DebuggerCapabilities.CAPS_RAM_W_BIT)
            {
                Console.WriteLine("No support for RAM change");
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
                Console.WriteLine("No support for RAM change");
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
