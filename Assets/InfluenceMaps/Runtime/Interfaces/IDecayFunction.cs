namespace InfluenceMaps
{
    /// <summary>
    /// Definicja zanikania wpływu
    /// Evaluate zwraca wartość do odjęcia
    /// </summary>
    public interface IDecayFunction
    {
        /// <summary>Oblicza wartość zaniku do odjęcia</summary>
        /// <param name="currentValue">Bieżąca wartość wpływu</param>
        /// <param name="distance">Odległość od źródła do komórki w jednostkach świata</param>
        /// <param name="maxDistance">Maksymalny zasięg wpływu źródła</param>
        /// <returns>Wartość zaniku d >= 0 do odjęcia</returns>
        float Evaluate(float currentValue, float distance, float maxDistance);
    }
}