using System.Drawing;

namespace AVR.Debugger
{
    public static class Utils
    {
        public static Color IntToColor(int rgb)
        {
            return Color.FromArgb(255, (byte) (rgb >> 16), (byte) (rgb >> 8), (byte) rgb);
        }
    }
}