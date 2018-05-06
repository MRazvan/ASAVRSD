using AVR.Debugger.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace AVR.Debugger.DeviceInfo
{
    public class DeviceReader : IDeviceInfoProvider
    {
        private static List<Device> _devices;
        static DeviceReader()
        {
            _devices = new List<Device>();
        }
        public List<Device> Load(string devicePackPath)
        {
            var files = GetAVRDeviceFiles(devicePackPath);
            ConcurrentBag<Device> container = new ConcurrentBag<Device>();
            Parallel.ForEach(files, (file) =>
            {
                try
                {
                    var device = LoadDevice(file);
                    if (device != null)
                        container.Add(device);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{file} - {ex}");
                }
            });
            _devices = container.ToList();
            return _devices;
        }

        public Device LoadDevice(string file)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(file);
            var rootElement = xDoc.DocumentElement;

            var device = new Device();

            device.Variants = LoadVariants(rootElement);
            device.ValueGroups = LoadValueGroups(rootElement);
            device.AddressSpace = LoadAddressSpace(rootElement);
            device.Modules = LoadModules(device.AddressSpace, device.ValueGroups, rootElement);
            device.Interrupts = LoadInterrupts(rootElement);
            device.Signature = LoadSignature(rootElement);
            return device;
        }

        private static long LoadSignature(XmlElement rootElement)
        {
            var sig0 = rootElement.SelectSingleNode("//property[@name='SIGNATURE0']");
            var sig1 = rootElement.SelectSingleNode("//property[@name='SIGNATURE1']");
            var sig2 = rootElement.SelectSingleNode("//property[@name='SIGNATURE2']");
            return (sig0.Value("value").ToLong() << 16)
                | (sig1.Value("value").ToLong() << 8)
                | (sig2.Value("value").ToLong());
        }

        private static List<Interrupt> LoadInterrupts(XmlElement rootElement)
        {
            var interrupts = new List<Interrupt>();
            foreach(XmlNode i in rootElement.SelectNodes("//interrupts/interrupt"))
            {
                if (i.NodeType != XmlNodeType.Element)
                    continue;
                interrupts.Add(new Interrupt
                {
                    Caption = i.Value("caption"),
                    Index = i.Value("index").ToLong(),
                    Name = i.Value("name")
                });
            }
            return interrupts;
        }

        private static List<Module> LoadModules(List<AddressSpace> addressSpaces, List<ValueGroup> valueGroups, XmlElement rootElement)
        {
            var modules = new List<Module>();
            foreach (XmlNode node in rootElement.SelectNodes("//peripherals/module/instance"))
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                var rg = node.SelectSingleNode("register-group");
                if (rg == null)
                    continue;
                modules.Add(new Module
                {
                    Caption = node.Value("caption"),
                    Name = node.Value("name"),
                    AddressSpace = addressSpaces.First(a => a.Id == rg.Value("address-space")),
                    BaseOffset = rg.Value("offset").ToLong(),
                    Registers = LoadRegisters(valueGroups, rootElement, rg)
                });
            }
            return modules;
        }

        private static List<Register> LoadRegisters(List<ValueGroup> valueGroups, XmlElement rootElement, XmlNode rg)
        {
            var registerGroup = rootElement.SelectSingleNode($"//modules/module/register-group[@name='{rg.Value("name-in-module")}']");
            var registers = new List<Register>();
            foreach(XmlNode regNode in registerGroup.ChildNodes)
            {
                if (regNode.NodeType != XmlNodeType.Element)
                    continue;
                registers.Add(new Register
                {
                    Caption = regNode.Value("caption"),
                    Name = regNode.Value("name"),
                    Size = regNode.Value("size").ToLong(),
                    Offset = regNode.Value("offset").ToLong(),
                    Mask = regNode.Value("mask").ToLong(),
                    BitFields = LoadBitFields(valueGroups, regNode)
                });
            }
            return registers;
        }

        private static List<BitField> LoadBitFields(List<ValueGroup> valueGroups, XmlNode regNode)
        {
            var bitFields = new List<BitField>();
            foreach(XmlNode bf in regNode.ChildNodes)
            {
                if (bf.NodeType != XmlNodeType.Element)
                    continue;
                var values = bf.Value("values");
                var valueGroup = string.IsNullOrEmpty(values) ? null : valueGroups.FirstOrDefault(vg => vg.Name == values);
                bitFields.Add(new BitField
                {
                    Caption = bf.Value("caption"),
                    Name = bf.Value("name"),
                    Mask = bf.Value("mask").ToLong(),
                    Values = valueGroup
                });
            }
            return bitFields;
        }

        private static List<AddressSpace> LoadAddressSpace(XmlElement rootElement)
        {
            var addresses = new List<AddressSpace>();
            foreach(XmlNode node in rootElement.SelectNodes("//address-space"))
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;
                Endianess end = Endianess.None;
                if (!string.IsNullOrWhiteSpace(node.Value("endianness"))) {
                    end = (Endianess)Enum.Parse(typeof(Endianess), node.Value("endianness"), true);
                }
                addresses.Add(new AddressSpace
                {
                    Id = node.Value("id"),
                    Name = node.Value("name"),
                    Start = node.Value("start").ToLong(),
                    Size = node.Value("size").ToLong(),
                    Endianness = end,
                    MemorySegments = LoadMemorySegments(node)
                });
            }
            return addresses;
        }

        private static List<MemorySegment> LoadMemorySegments(XmlNode node)
        {
            var memSegments = new List<MemorySegment>();
            foreach(XmlNode memSeg in node.ChildNodes)
            {
                if (memSeg.NodeType != XmlNodeType.Element)
                    continue;
                memSegments.Add(new MemorySegment
                {
                    Name = memSeg.Value("name"),
                    Start = memSeg.Value("start").ToLong(),
                    Size = memSeg.Value("size").ToLong(),
                    PageSize = memSeg.Value("pagesize").ToLong(),
                    Type = memSeg.Value("type"),
                    Executable = memSeg.Value("exec") == "1",
                    OnlyReadable = memSeg.Value("rw")?.ToLower() == "r",
                    External = memSeg.Value("external")?.ToLower() == "true"
                });
            }
            return memSegments;
        }

        private static List<ValueGroup> LoadValueGroups(XmlElement rootElement)
        {
            var valueGroups = new List<ValueGroup>();
            foreach(XmlNode vg in rootElement.SelectNodes("//value-group"))
            {
                if (vg.NodeType != XmlNodeType.Element)
                    continue;
                var valueGroup = new ValueGroup
                {
                    Caption = vg.Value("caption"),
                    Name = vg.Value("name"),
                    Values = LoadValues(vg)
                };
                valueGroups.Add(valueGroup);

            }
            return valueGroups;
        }

        private static List<ValueGroupValue> LoadValues(XmlNode vg)
        {
            var vgv = new List<ValueGroupValue>();
            foreach(XmlNode v in vg.ChildNodes)
            {
                if (v.NodeType != XmlNodeType.Element)
                    continue;
                vgv.Add(new ValueGroupValue
                {
                    Caption = v.Value("caption"),
                    Name = v.Value("name"),
                    Value = v.Value("value").ToLong()
                });
            }
            return vgv;
        }

        private static List<Variant> LoadVariants(XmlElement elem)
        {
            var variants = new List<Variant>();
            foreach(XmlNode v in elem.SelectNodes("//variant"))
            {
                if (v.NodeType != XmlNodeType.Element)
                    continue;
                variants.Add(new Variant
                {
                    Code = v.Value("ordercode"),
                    TempMin = v.Value("tempmin").ToLong(),
                    TempMax = v.Value("tempmax").ToLong(),
                    Speed = v.Value("speedmax").ToLong(),
                    VCCMin = v.Value("vccmin").ToFloat(),
                    VCCMax = v.Value("vccmax").ToFloat(),
                    Pinout = v.Value("pinout"),
                    Package = v.Value("package")
                });
            }
            return variants;
        }

        private static List<string> GetAVRDeviceFiles(string devicePackPath)
        {
            XmlDocument xDoc = new XmlDocument();
            List<string> avrFiles = new List<string>();
            foreach (var familiyDirectory in Directory.GetDirectories(Path.Combine(devicePackPath, "atmel")))
            {
                var latestPackDirectory = Directory.GetDirectories(familiyDirectory)
                        .OrderByDescending(f => f)
                        .First();
                avrFiles.AddRange(Directory.GetFiles(latestPackDirectory, "*.atdf", SearchOption.AllDirectories));
            }
            return avrFiles;
        }

        public Device GetDevice(long id)
        {
            throw new NotImplementedException();
        }
    }
}
