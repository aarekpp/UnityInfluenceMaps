using System.Collections.Generic;

namespace InfluenceMaps
{
    /// <summary>Kontekst danych przekazywany do pipeline aktualizacji mapy</summary>
    public struct PipelineContext
    {
        /// <summary>Siatka wartości wpływów</summary>
        public InfluenceGrid Grid;

        /// <summary>Zarejestrowane źródła wpływu</summary>
        public IReadOnlyList<IInfluenceSource> Sources;

        /// <summary>Zarejestrowane przeszkody</summary>
        public IReadOnlyList<IInfluenceObstacle> Obstacles;

        /// <summary>Funkcja propagacji wpływu</summary>
        public IPropagationFunction Propagation;

        /// <summary>Funkcja zaniku wpływu, null - zerowanie</summary>
        public IDecayFunction Decay;

        /// <summary>Czy poza zasięgiem wpływu używać osobnego wygaszania zamiast krzywej zaniku na krawędzi</summary>
        public bool UseOutOfRangeFade;

        /// <summary>Współczynnik wygaszania [0,1] na klatkę dla komórek poza jakimkolwiek wpływem</summary>
        public float OutOfRangeFadeFactor;

        /// <summary>Czy stosować Clamp w pipeline</summary>
        public bool ApplyClamp;

        /// <summary>Minimalna wartość wpływu dla Clamp</summary>
        public float MinValue;

        /// <summary>Maksymalna wartość wpływu dla Clamp</summary>
        public float MaxValue;

        /// <summary>Czas od ostatniej aktualizacji mapy w sekundach</summary>
        public float DeltaTime;
    }
}
