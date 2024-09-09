using System.Globalization;
using BinaryEncoding;

namespace Multiformats.Address.Protocols
{
    public abstract class PortNumber : MultiaddressProtocol
    {
        public int Port => (int?)Value ?? 0;

        protected PortNumber(string name, int code)
            : base(name, code, 16)
        {
        }

        public override void Decode(string value) => Value = (int)ushort.Parse(value, NumberStyles.Number);
        public override void Decode(byte[] bytes) => Value = (int)Binary.BigEndian.GetUInt16(bytes, 0);
        public override byte[] ToBytes() => Binary.BigEndian.GetBytes((ushort)(int)Value);
    }
}
