namespace IdleSim
{
    // Achievement-style milestones: cross a cumulative threshold (customers served / lifetime earned)
    // and earn a small PERMANENT global income boost. They're derived from monotonic stats, so no
    // extra save is needed and they can never un-complete. This is the cheap "texture" layer that
    // gives the idle curve its long pull (like Cookie Clicker's milk/kittens).
    public static class Milestones
    {
        public const double PerMilestone = 0.02;   // +2% global income per completed milestone

        static readonly long[] Served = { 10, 50, 250, 1000, 5000, 25000, 100000, 500000, 2000000, 10000000 };
        static readonly double[] Earned = { 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12 };

        public static int Total => Served.Length + Earned.Length;

        public static int Completed(int served, double earned)
        {
            int n = 0;
            foreach (var s in Served) if (served >= s) n++;
            foreach (var e in Earned) if (earned >= e) n++;
            return n;
        }

        // global income multiplier from all completed milestones
        public static double Mult(int served, double earned) => 1.0 + PerMilestone * Completed(served, earned);
    }
}
