namespace InfluenceMaps
{
    /// <summary>
    /// Definicja rozprzestrzeniania się wpływu ze źródła na otaczające komórki
    /// Evaluate zwraca przyrost dodawany do komórki
    /// </summary>
    public interface IPropagationFunction
    {
        /// <summary>Oblicza przysrost wartości wpływu w komórce na podstawie odległości od źródła</summary>
        /// <param name="baseValue">Intensywność źródła</param>
        /// <param name="distance">Odległość od źródła do komórki w jednostkach świata</param>
        /// <param name="maxDistance">Maksymalny zasięg wpływu źródła</param>
        /// <returns>Przyrost wartości wpływu dodawany do komórki</returns>
        float Evaluate(float baseValue, float distance, float maxDistance);
    }
}
