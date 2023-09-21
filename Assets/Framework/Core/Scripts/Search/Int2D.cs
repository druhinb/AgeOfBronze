using System;

namespace RTSEngine.Utilities
{
    [System.Serializable]
    public struct Int2D : IEquatable<Int2D>
    {
        public int x;
        public int y;

        public Int2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(Int2D other)
        {
            return other.x == x && other.y == y;
        }

        public override int GetHashCode()
        {
            var hashCode = 43270662;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }
    }
}
