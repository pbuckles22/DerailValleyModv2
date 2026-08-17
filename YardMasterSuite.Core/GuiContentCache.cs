using System.Text;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Cached HUD label strings (Unity-free). HUD (Epic 3) assigns
    /// <c>GUIContent.text</c> only when <see cref="TryCommit"/> returns true.
    /// </summary>
    public sealed class GuiContentCache
    {
        private readonly string?[] _slots;

        public GuiContentCache(int slotCount)
        {
            _slots = new string?[slotCount];
        }

        public string? Get(int slot) => _slots[slot];

        public bool TryCommit(int slot, string value, out string text)
        {
            value ??= string.Empty;
            var cached = _slots[slot];
            if (cached != null && cached == value)
            {
                text = cached;
                return false;
            }

            text = value;
            _slots[slot] = text;
            return true;
        }

        /// <summary>
        /// If <paramref name="sb"/> matches the cached slot, returns false and
        /// the existing string (no ToString). Otherwise ToString once and store.
        /// </summary>
        public bool TryCommit(int slot, StringBuilder sb, out string text)
        {
            var cached = _slots[slot];
            if (ContentsEqual(sb, cached))
            {
                text = cached!;
                return false;
            }

            text = sb.ToString();
            _slots[slot] = text;
            return true;
        }

        public static bool ContentsEqual(StringBuilder sb, string? cached)
        {
            if (cached == null || sb.Length != cached.Length)
            {
                return false;
            }

            for (var i = 0; i < sb.Length; i++)
            {
                if (sb[i] != cached[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
