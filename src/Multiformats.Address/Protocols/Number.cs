using System.Globalization;
using BinaryEncoding;

namespace Multiformats.Address.Protocols
{
    public abstract class Number : MultiaddressProtocol
    {
        public int Port => (int?)Value ?? 0;

        protected Number(string name, int code)
            : base(name, code, 16)
        {
        }

        public override void Decode(string value) => Value = int.Parse(value, NumberStyles.Number);
        public override void Decode(byte[] bytes) => Value = Binary.BigEndian.GetUInt16(bytes, 0);
        public override byte[] ToBytes() => Binary.BigEndian.GetBytes((int)Value);
    }
}
