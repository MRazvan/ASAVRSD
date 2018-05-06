namespace AVR.Debugger.Interfaces
{
    public class Variant
    {
        public string Code { get; set; }
        public long TempMin { get; set; }
        public long TempMax { get; set; }
        public long Speed { get; set; }
        public float VCCMin { get; set; }
        public float VCCMax { get; set; }
        public string Pinout { get; set; }
        public string Package { get; set; }
    }
}
