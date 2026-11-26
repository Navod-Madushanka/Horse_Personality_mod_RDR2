namespace HorsePersonalities.Core.GameApi
{
    /// <summary>A position or direction in game space, without a reference to the hook's own vector type.</summary>
    public struct Vec3
    {
        public float X, Y, Z;

        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public float Length => (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);

        /// <summary>How far <paramref name="point"/> is behind an entity at <paramref name="origin"/> facing <paramref name="forward"/>, and to its side (flat, metres).</summary>
        public static (double Behind, double Side) BehindAndSide(Vec3 origin, Vec3 forward, Vec3 point)
        {
            double fx = forward.X, fy = forward.Y, len = System.Math.Sqrt(fx * fx + fy * fy);
            if (len < 1e-6) return (0, double.MaxValue);
            fx /= len; fy /= len;
            double dx = point.X - origin.X, dy = point.Y - origin.Y;
            double ahead = dx * fx + dy * fy;
            double side = dx * -fy + dy * fx;
            return (-ahead, side);
        }
    }
}
