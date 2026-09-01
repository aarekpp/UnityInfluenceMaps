namespace InfluenceMaps
{
    /// <summary>Stałe domyślne wartości używane w mapach wpływów</summary>
    public static class InfluenceMapConstants
    {
        /// <summary>Domyślna szerokość świata w osi X</summary>
        public const float DefaultWorldWidth = 50f;

        /// <summary>Domyślna szerokość świata w osi Z</summary>
        public const float DefaultWorldHeight = 50f;

        /// <summary>Domyślna liczba komórek w osi X</summary>
        public const int DefaultCellsX = 50;

        /// <summary>Minimalna dozwolona szerokość/wysokość siatki w komórkach</summary>
        public const float MinWorldSize = 0.1f;

        /// <summary>Minimalna dozwolona liczba komórek wzdłuż osi siatki</summary>
        public const int MinGridDimension = 2;

        /// <summary>Minimalny dozwolony rozmiar komórki w jednostkach świata</summary>
        public const float MinCellSize = 0.1f;

        /// <summary>Domyślna minimalna wartość wpływu</summary>
        public const float DefaultMinInfluenceValue = -1f;

        /// <summary>Domyślna maksymalna wartość wpływu</summary>
        public const float DefaultMaxInfluenceValue = 1f;

        /// <summary>Próg poniżej którego wartość jest traktowana jako zero</summary>
        public const float InfluenceValueEpsilon = 0.001f;

        /// <summary>Domyślna liczba aktualizacji mapy na sekundę w trybie TargetFPS</summary>
        public const int DefaultTargetFPS = 10;

        /// <summary>Domyślny interwał między aktualizacjami w trybie Interval jako sekundy</summary>
        public const float DefaultUpdateInterval = 0.1f;

        /// <summary>Domyślna przezroczystość wizualizacji Gizmos</summary>
        public const float DefaultGizmoAlpha = 1f;

        /// <summary>Czy wykonywać metodę Clamp w pipeline aktualizacji</summary>
        public const bool DefaultApplyClamp = false;
    }
}
