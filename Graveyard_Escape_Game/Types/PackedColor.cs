namespace Graveyard_Escape_Game.Types
{
    public struct PackedColor
    {
        public uint Value;

        public PackedColor(byte r, byte g, byte b, byte a = 255)
        {
            Value = (uint)(r | (g << 8) | (b << 16) | (a << 24));
        }

        public byte R => (byte)(Value & 0xFF);
        public byte G => (byte)((Value >> 8) & 0xFF);
        public byte B => (byte)((Value >> 16) & 0xFF);
        public byte A => (byte)((Value >> 24) & 0xFF);

        public void SetR(byte r) => Value = (Value & 0xFFFFFF00) | r;
        public void SetG(byte g) => Value = (Value & 0xFFFF00FF) | ((uint)g << 8);
        public void SetB(byte b) => Value = (Value & 0xFF00FFFF) | ((uint)b << 16);
        public void SetA(byte a) => Value = (Value & 0x00FFFFFF) | ((uint)a << 24);

        public static PackedColor FromArgb(byte r, byte g, byte b, byte a = 255) => new PackedColor(r, g, b, a);
    }
}
