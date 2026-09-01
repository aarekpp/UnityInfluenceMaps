using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>
    /// Interfejs przeszkody osłabiającej wpływ
    /// Przeszkoda działa kierunkowo osłabiając wpływ za sobą względem źródła
    /// </summary>
    public interface IInfluenceObstacle
    {
        /// <summary>Współczynnik blokowania wpływu w zakresie [0, 1]</summary>
        float BlockingFactor { get; }

        /// <summary>Czy przeszkoda jest aktywna</summary>
        bool IsActive { get; }

        /// <summary>Pozycja przeszkody w przestrzeni świata</summary>
        Vector3 Position { get; }

        /// <summary>Oblicza mnożnik wpływu dla linii źródło-komórka. 1.0 - brak blokady, 0.0 - pełna blokada</summary>
        /// <param name="sourcePos">Pozycja źródła wpływu</param>
        /// <param name="cellPos">Pozycja komórki docelowej</param>
        /// <returns>Wartość w zakresie [0,1]</returns>
        float EvaluateBlocking(Vector3 sourcePos, Vector3 cellPos);
    }
}