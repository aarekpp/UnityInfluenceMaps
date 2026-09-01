using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Globalna konfiguracja map wpływów</summary>
    [CreateAssetMenu(fileName = "InfluenceMapsGlobalConfig", menuName = "Influence Maps/Global Configuration")]
    public class InfluenceMapsGlobalConfig : ScriptableObject
    {
        /// <summary>Ustawienia siatki</summary>
        [Header("Siatka")]
        [SerializeField]
        private GridSettings gridSettings = new GridSettings();

        /// <summary>Ustawienia propagacji</summary>
        [Header("Propagacja")]
        [SerializeField]
        private PropagationSettings propagationSettings = new PropagationSettings();

        /// <summary>Ustawienia zaniku</summary>
        [Header("Zanik")]
        [SerializeField]
        private DecaySettings decaySettings = new DecaySettings();

        /// <summary>Ustawienia aktualizacji</summary>
        [Header("Aktualizacja")]
        [SerializeField]
        private UpdateSettings updateSettings = new UpdateSettings();

        /// <summary>Ustawienia pipeline aktualizacji</summary>
        [Header("Pipeline")]
        [Tooltip("Własna implementacja pipeline aktualizacji mapy. Brak - domyślny pipeline na main thread")]
        [SerializeField]
        private PipelineSettings pipelineSettings = new PipelineSettings();

        /// <summary>Ustawienia wizualizacji</summary>
        [Header("Wizualizacja")]
        [SerializeField]
        private VisualizationSettings visualizationSettings = new VisualizationSettings();

        /// <summary>Walidacja danych</summary>
        private void OnValidate()
        {
            gridSettings?.Validate();
            updateSettings?.Validate();
            pipelineSettings?.Validate();
        }

        /// <summary>Ustawienia siatki</summary>
        public GridSettings GridSettings => gridSettings;

        /// <summary>Ustawienia propagacji</summary>
        public PropagationSettings PropagationSettings => propagationSettings;

        /// <summary>Ustawienia zaniku</summary>
        public DecaySettings DecaySettings => decaySettings;

        /// <summary>Ustawienia aktualizacji</summary>
        public UpdateSettings UpdateSettings => updateSettings;

        /// <summary>Ustawienia pipeline aktualizacji</summary>
        public PipelineSettings PipelineSettings => pipelineSettings;

        /// <summary>Ustawienia wizualizacji</summary>
        public VisualizationSettings VisualizationSettings => visualizationSettings;
    }
}
