namespace InfluenceMaps
{
    /// <summary>Tryb wyznaczania lewego dolnego rogu siatki</summary>
    public enum GridOriginMode
    {
        /// <summary>Ręczne ustawienie w Inspektorze</summary>
        Manual,

        /// <summary>
        /// Lewy dolny róg wyznaczany z Bounds obiektu kotwicy
        /// Najpierw sprawdza Renderer.bounds, następnie Collider.bounds i fallback na Manual
        /// </summary>
        AnchorObject,

        /// <summary>Siatka wycentrowana na pozycji GameObject z komponentem InfluenceMap</summary>
        MapPosition
    }
}
