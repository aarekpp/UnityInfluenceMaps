using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Interfejs źródła wpływu</summary>
    public interface IInfluenceSource
    {
        /// <summary>Pozycja źródła w przestrzeni świata</summary>
        Vector3 Position { get; }

        /// <summary>Zasięg wpływu źródła w jednostkach świata</summary>
        float Radius { get; }

        /// <summary>Bazowa siła wpływu</summary>
        float Intensity { get; }

        /// <summary>Czy źródło jest aktywne</summary>
        bool IsActive { get; }
    }
}
