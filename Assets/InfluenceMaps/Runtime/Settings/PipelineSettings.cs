using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Ustawienia pipeline aktualizacji mapy</summary>
    [Serializable]
    public class PipelineSettings
    {
        /// <summary>Domyślny pipeline</summary>
        [Header("Algorytm cyklu mapy")]
        [Tooltip("Algorytmy propagacji i zaniku. Środowisko main thread lub jobs")]
        [SerializeField]
        private PipelineMode mode = PipelineMode.Wave;

        /// <summary>Własna implementacja pipeline</summary>
        [Tooltip("Implementacja IInfluenceMapPipeline")]
        [SerializeField]
        private ScriptableObject customPipeline;

        /// <summary>Czy stosować ograniczenie wartości wpływu w pipeline</summary>
        [Tooltip("Czy stosować Clamp w pipeline aktualizacji")]
        [SerializeField]
        private bool applyClamp = InfluenceMapConstants.DefaultApplyClamp;

        /// <summary>Minimalna wartość wpływu</summary>
        [Tooltip("Minimalna wartość wpływu w komórce")]
        [SerializeField]
        private float minInfluenceValue = InfluenceMapConstants.DefaultMinInfluenceValue;

        /// <summary>Maksymalna wartość wpływu</summary>
        [Tooltip("Maksymalna wartość wpływu w komórce")]
        [SerializeField]
        private float maxInfluenceValue = InfluenceMapConstants.DefaultMaxInfluenceValue;

        /// <summary>Walidacja</summary>
        public void Validate()
        {
            if (customPipeline != null && !(customPipeline is IInfluenceMapPipeline)) Debug.LogWarning($"[InfluenceMaps] PipelineSettings: Pole Custom Pipeline wymaga obiektu implementującego IInfluenceMapPipeline", customPipeline);
            if (minInfluenceValue > maxInfluenceValue) maxInfluenceValue = minInfluenceValue;
        }

        /// <summary>Algorytm cyklu mapy</summary>
        public PipelineMode Mode
        {
            get => mode;
            set => mode = value;
        }

        /// <summary>Własna implementacja cyklu aktualizacji</summary>
        public ScriptableObject CustomPipeline
        {
            get => customPipeline;
            set
            {
                if (value != null && !(value is IInfluenceMapPipeline))
                {
                    Debug.LogWarning($"[InfluenceMaps] PipelineSettings: Obiekt '{value.name}' nie implementuje IInfluenceMapPipeline", value);
                    return;
                }
                customPipeline = value;
            }
        }

        /// <summary>Zastosowanie ograniczenia wartości wpływu</summary>
        public bool ApplyClamp
        {
            get => applyClamp;
            set => applyClamp = value;
        }

        /// <summary>Minimalna wartość wpływu</summary>
        public float MinInfluenceValue
        {
            get => minInfluenceValue;
            set => minInfluenceValue = value;
        }

        /// <summary>Maksymalna wartość wpływu</summary>
        public float MaxInfluenceValue
        {
            get => maxInfluenceValue;
            set => maxInfluenceValue = value;
        }

        /// <summary>Zwraca przekazany własny obiekt cyklu aktualizacji</summary>
        /// <returns>Obiekt implementujący IInfluenceMapPipeline</returns>
        public IInfluenceMapPipeline GetCustomPipeline()
        {
            if (customPipeline == null) return null;
            if (customPipeline is IInfluenceMapPipeline pipeline) return pipeline;

            Debug.LogWarning($"[InfluenceMaps] PipelineSettings: Przypisany obiekt '{customPipeline.name}' jest nieprawidłowy.", customPipeline);
            return null;
        }
    }
}
