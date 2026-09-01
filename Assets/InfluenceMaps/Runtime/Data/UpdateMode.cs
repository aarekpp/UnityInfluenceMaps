namespace InfluenceMaps
{
    /// <summary>Tryb aktualizacji mapy wpływów</summary>
    public enum UpdateMode
    {
        /// <summary>Aktualizacja w każdej klatce</summary>
        EveryFrame = 0,

        /// <summary>Aktualizacja w stałym kroku czasowym fizyki</summary>
        FixedUpdate = 1,

        /// <summary>Aktualizacja co określony interwał czasowy w sekundach</summary>
        Interval = 2,

        /// <summary>Aktualizacja określoną liczbę razy na sekundę</summary>
        TargetFPS = 3,

        /// <summary>Aktualizacja przez ręczne wywołanie</summary>
        Manual = 4
    }
}
