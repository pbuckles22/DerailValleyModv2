using System.Text;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Main-thread StringBuilder pool. Rent, append, then Return (clears).
    /// Do not concatenate strings in render loops — write into a rented builder.
    /// </summary>
    public sealed class StringBuilderPool
    {
        public const int DefaultCapacity = 128;
        public const int MaxPooled = 8;

        public static readonly StringBuilderPool Shared = new StringBuilderPool();

        private readonly StringBuilder?[] _pool = new StringBuilder?[MaxPooled];
        private int _count;

        public StringBuilder Rent()
        {
            if (_count > 0)
            {
                _count--;
                var sb = _pool[_count];
                _pool[_count] = null;
                return sb!;
            }

            return new StringBuilder(DefaultCapacity);
        }

        public void Return(StringBuilder sb)
        {
            sb.Length = 0;
            if (_count >= MaxPooled)
            {
                return;
            }

            _pool[_count] = sb;
            _count++;
        }
    }
}
