namespace InfluenceMaps
{
    /// <summary>Operacje łączenia wartości z wielu map wpływów. Używane do scalania</summary>
    public enum CombineOperation
    {
        /// <summary>Suma wartości z wszystkich map</summary>
        Add = 0,

        /// <summary>Pierwsza mapa minus pozostałe</summary>
        Subtract = 1,

        /// <summary>Iloczyn wartości wszystkich map</summary>
        Multiply = 2,

        /// <summary>Maksimum wartości wszystkich map</summary>
        Max = 3,

        /// <summary>Minimum wartości wszystkich map</summary>
        Min = 4,

        /// <summary>Średnia arytmetyczna wartości wszystkich map</summary>
        Average = 5
    }
}
