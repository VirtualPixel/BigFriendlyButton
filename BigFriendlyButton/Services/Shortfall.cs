namespace BigFriendlyButton.Services
{
    // what early sends still owe, goes on the next point that opens. send early on the last one and its forfeit
    internal static class Shortfall
    {
        private static RoundDirector? level;
        private static int owed;

        public static void Add(int amount)
        {
            Sync();
            owed += amount;
        }

        public static int Take()
        {
            Sync();
            int taken = owed;
            owed = 0;
            return taken;
        }

        private static void Sync()
        {
            if (level == RoundDirector.instance)
                return;
            level = RoundDirector.instance;
            owed = 0;
        }
    }
}
