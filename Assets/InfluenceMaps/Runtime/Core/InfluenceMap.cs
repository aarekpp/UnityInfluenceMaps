using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace InfluenceMaps
{
    /// <summary>Pojedyncza mapa wpływów</summary>
    [AddComponentMenu("Influence Maps/Influence Map")]
    public class InfluenceMap : MonoBehaviour
    {
        /// <summary>Unikalna nazwa mapy</summary>
        [Header("Identyfikacja")]
        [Tooltip("Unikalna nazwa mapy do wyszukiwania przez Managera")]
        [SerializeField]
        private string mapName = "NewInfluenceMap";

        /// <summary>Obiekt kotwica do wyznaczania pozycji siatki w trybie AnchorObject</summary>
        [Header("Pozycjonowanie siatki - obiekt kotwica")]
        [Tooltip("Obiekt kotwica dla trybu GridOriginMode.AnchorObject")]
        [SerializeField]
        private GameObject gridAnchorObject;

        /// <summary>Czy nadpisać ustawienia siatki</summary>
        [Header("Siatka")]
        [Tooltip("Nadpisz ustawienia siatki z GlobalConfig")]
        [SerializeField]
        private bool overrideGridSettings;

        /// <summary>Ustawienia siatki</summary>
        [SerializeField]
        private GridSettings gridSettings = new GridSettings();

        /// <summary>Czy nadpisać ustawienia propagacji</summary>
        [Header("Propagacja")]
        [Tooltip("Nadpisz ustawienia propagacji z GlobalConfig")]
        [SerializeField]
        private bool overridePropagation;

        /// <summary>Ustawienia propagacji</summary>
        [SerializeField]
        private PropagationSettings propagationSettings = new PropagationSettings();

        /// <summary>Czy nadpisać ustawienia zaniku</summary>
        [Header("Zanik")]
        [Tooltip("Nadpisz funkcję zaniku z GlobalConfig")]
        [SerializeField]
        private bool overrideDecay;

        /// <summary>Ustawienia zaniku</summary>
        [SerializeField]
        private DecaySettings decaySettings = new DecaySettings();

        /// <summary>Czy nadpisać ustawienia aktualizacji</summary>
        [Header("Aktualizacja")]
        [Tooltip("Nadpisz ustawienia aktualizacji z GlobalConfig")]
        [SerializeField]
        private bool overrideUpdate;

        /// <summary>Ustawienia aktualizacji</summary>
        [SerializeField]
        private UpdateSettings updateSettings = new UpdateSettings();

        /// <summary>Czy nadpisać ustawienia pipeline aktualizacji</summary>
        [Header("Pipeline aktualizacji")]
        [Tooltip("Nadpisz pipeline aktualizacji z GlobalConfig")]
        [SerializeField]
        private bool overridePipeline;

        /// <summary>Ustawienia pipeline aktualizacji</summary>
        [SerializeField]
        private PipelineSettings pipelineSettings = new PipelineSettings();

        /// <summary>Czy nadpisać ustawienia wizualizacji</summary>
        [Header("Wizualizacja")]
        [Tooltip("Nadpisz ustawienia wizualizacji z GlobalConfig")]
        [SerializeField]
        private bool overrideVisualization;

        /// <summary>Ustawienia wizualizacji</summary>
        [SerializeField]
        private VisualizationSettings visualizationSettings = new VisualizationSettings();

        /// <summary>Siatka wartości wpływów</summary>
        private InfluenceGrid grid;

        /// <summary>Harmonogram aktualizacji</summary>
        private UpdateScheduler scheduler;

        /// <summary>Czy zarządzana przez manager</summary>
        private bool externallyScheduled;

        /// <summary>Zarejestrowane źródła wpływu</summary>
        private readonly List<IInfluenceSource> sources = new List<IInfluenceSource>();

        /// <summary>Zarejestrowane przeszkody</summary>
        private readonly List<IInfluenceObstacle> obstacles = new List<IInfluenceObstacle>();

        /// <summary>Czy mapa została zainicjalizowana</summary>
        private bool isInitialized;

        /// <summary>Aktywne ustawienia siatki</summary>
        private GridSettings activeGridSettings;

        /// <summary>Aktywne ustawienia propagacji</summary>
        private PropagationSettings activePropagationSettings;

        /// <summary>Aktywne ustawienia zaniku</summary>
        private IDecayFunction activeDecay;

        /// <summary>Aktywne ustawienia zaniku</summary>
        private DecaySettings activeDecaySettings;

        /// <summary>Aktywne ustawienia aktualizacji</summary>
        private UpdateSettings activeUpdateSettings;

        /// <summary>Aktywne ustawienia pipeline aktualizacji</summary>
        private PipelineSettings activePipelineSettings;

        /// <summary>Aktywna instancja pipeline do wykonania</summary>
        private IInfluenceMapPipeline activePipeline;

        /// <summary>Wywoływane po każdej aktualizacji pipeline mapy</summary>
        public event Action<InfluenceMap> OnMapUpdated;

        /// <summary>Wywoływane przy zarejestrowaniu źródła</summary>
        public event Action<IInfluenceSource> OnSourceRegistered;

        /// <summary>Wywoływane przy wyrejestrowaniu źródła</summary>
        public event Action<IInfluenceSource> OnSourceUnregistered;

        /// <summary>Wywoływane przy zarejestrowaniu przeszkody</summary>
        public event Action<IInfluenceObstacle> OnObstacleRegistered;

        /// <summary>Wywoływane przy wyrejestrowaniu przeszkody</summary>
        public event Action<IInfluenceObstacle> OnObstacleUnregistered;

        /// <summary>Nazwa mapy</summary>
        public string MapName => mapName;

        /// <summary>Siatka wartości wpływów</summary>
        public InfluenceGrid Grid => grid;

        /// <summary>Zarejestrowane źródła</summary>
        public IReadOnlyList<IInfluenceSource> Sources => sources;

        /// <summary>Zarejestrowane przeszkody</summary>
        public IReadOnlyList<IInfluenceObstacle> Obstacles => obstacles;

        /// <summary>Czy mapa jest zainicjalizowana</summary>
        public bool IsInitialized => isInitialized;

        /// <summary>Obiekt kotwica do pozycjonowania siatki</summary>
        public GameObject GridAnchorObject
        {
            get => gridAnchorObject;
            set => gridAnchorObject = value;
        }

        /// <summary>Aktywne ustawienia wizualizacji</summary>
        public VisualizationSettings ActiveVisualization
        {
            get
            {
                InfluenceMapsGlobalConfig global = GetGlobalConfig();
                if (overrideVisualization) return visualizationSettings;
                if (global != null) return global.VisualizationSettings;
                return visualizationSettings;
            }
        }

        /// <summary>Pobiera GlobalConfig z Managera</summary>
        /// <returns>Zwraca null jeśli Manager nie istnieje lub nie ma przypisanego GlobalConfig</returns>
        private InfluenceMapsGlobalConfig GetGlobalConfig()
        {
            if (InfluenceMapManager.Instance != null) return InfluenceMapManager.Instance.GlobalConfig;
            return null;
        }

        /// <summary>Stosuje aktywne ustawienia z override lub GlobalConfig</summary>
        private void ResolveActiveSettings()
        {
            InfluenceMapsGlobalConfig global = GetGlobalConfig();
            if (global == null)
            {
                Debug.LogWarning($"[InfluenceMaps] Mapa '{mapName}': brak GlobalConfig. Ustawienia lokalne", this);
                activeGridSettings = gridSettings;
                activePropagationSettings = propagationSettings;
                activeDecaySettings = decaySettings;
                activeDecay = decaySettings.GetFunction();
                activeUpdateSettings = updateSettings;
                activePipelineSettings = pipelineSettings;
            }
            else
            {
                activeGridSettings = overrideGridSettings ? gridSettings : global.GridSettings;
                activePropagationSettings = overridePropagation ? propagationSettings : global.PropagationSettings;
                activeDecaySettings = overrideDecay ? decaySettings : global.DecaySettings;
                activeDecay = activeDecaySettings.GetFunction();
                activeUpdateSettings = overrideUpdate ? updateSettings : global.UpdateSettings;
                activePipelineSettings = overridePipeline ? pipelineSettings : global.PipelineSettings;
            }
            UpdatePipelineInstance();
        }

        /// <summary>Aktualizacja aktywnej instancji cyklu na podstawie ustawień</summary>
        private void UpdatePipelineInstance()
        {
            IInfluenceMapPipeline custom = activePipelineSettings.GetCustomPipeline();
            if (custom != null)
            {
                if (custom != null)
                {
                    DisposeCurrentPipeline();
                    activePipeline = custom;
                }
                return;
            }

            if (activePipeline == null || !(activePipeline is WavePipeline))
            {
                DisposeCurrentPipeline();
                activePipeline = CreateBuiltInPipeline();
            }
        }

        /// <summary>Utworzenie wbudowanego potoku falowego</summary>
        /// <returns>Nowa instancja WavePipeline</returns>
        private IInfluenceMapPipeline CreateBuiltInPipeline()
        {
            return new WavePipeline();
        }

        /// <summary>Zwolnienie zasobów bieżącego cyklu jeśli korzysta z Job System poprzez Dispose</summary>
        private void DisposeCurrentPipeline()
        {
            if (activePipeline is IJobsInfluenceMapPipeline jobsPipeline) jobsPipeline.Dispose();
            activePipeline = null;
        }

        /// <summary>Zwraca granice obiektu do pozycjonowania siatki</summary>
        /// <returns>Bounds lub null</returns>
        private Bounds? GetAnchorBounds()
        {
            if (gridAnchorObject == null) return null;
            Renderer renderer = gridAnchorObject.GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;
            Collider collider = gridAnchorObject.GetComponent<Collider>();
            if (collider != null) return collider.bounds;
            Debug.LogWarning($"[InfluenceMaps] InfluenceMap: Obiekt {gridAnchorObject.name} nie ma Renderer ani Collider. Pozycja jako centrum siatki", gridAnchorObject);
            Vector3 pos = gridAnchorObject.transform.position;
            float w = activeGridSettings.WorldWidth;
            float h = activeGridSettings.WorldHeight;
            return new Bounds(pos, new Vector3(w, 0f, h));
        }

        /// <summary>Główna pętla uruchamiająca pipeline</summary>
        private void Update()
        {
            if (!isInitialized) return;
            if (externallyScheduled) return;
            if (activeUpdateSettings.Mode == UpdateMode.FixedUpdate) return;
            if (scheduler.ShouldUpdate(Time.deltaTime, out float mapDeltaTime)) ExecuteUpdatePipeline(mapDeltaTime);
        }

        /// <summary>Pętla fizyki</summary>
        private void FixedUpdate()
        {
            if (!isInitialized) return;
            if (externallyScheduled) return;
            if (activeUpdateSettings.Mode != UpdateMode.FixedUpdate) return;
            if (scheduler.ShouldUpdate(Time.fixedDeltaTime, out float mapDeltaTime)) ExecuteUpdatePipeline(mapDeltaTime);
        }

        /// <summary>Wykonuje pełny cykl aktualizacji mapy</summary>
        /// <param name="deltaTime">Czas od ostatniej aktualizacji</param>
        private void ExecuteUpdatePipeline(float deltaTime)
        {
            ResolveActiveSettings();
            PipelineContext context = new PipelineContext
            {
                Grid = grid,
                Sources = sources,
                Obstacles = obstacles,
                Propagation = activePropagationSettings.GetFunction(),
                Decay = activeDecay,
                UseOutOfRangeFade = activeDecaySettings.UseOutOfRangeFade,
                OutOfRangeFadeFactor = activeDecaySettings.OutOfRangeFadeFactor,
                ApplyClamp = activePipelineSettings.ApplyClamp,
                MinValue = activePipelineSettings.MinInfluenceValue,
                MaxValue = activePipelineSettings.MaxInfluenceValue,
                DeltaTime = deltaTime
            };
            activePipeline.Execute(context);
            OnMapUpdated?.Invoke(this);
        }

        /// <summary>Inicjalizacja przy starcie</summary>
        private void Awake()
        {
            if (GetGlobalConfig() != null) Initialize();
        }

        private void OnDestroy()
        {
            DisposeCurrentPipeline();
        }

        /// <summary>Ponowna inicjalizacja gdy parametry są zmieniane w edytorze</summary>
        private void OnValidate()
        {
            pipelineSettings?.Validate();
            if (Application.isPlaying && isInitialized && GetGlobalConfig() != null)
            {
                Initialize();
                if (activePipeline is IJobsInfluenceMapPipeline jobsPipeline) jobsPipeline.InvalidateCurves();
            }
        }

        /// <summary>Rysowanie siatki w edytorze</summary>
        private void OnDrawGizmos()
        {
            InfluenceMapGizmoDrawer.DrawGizmos(this);
        }

        /// <summary>Rysowanie dodatkowych elementów gdy obiekt jest zaznaczony</summary>
        private void OnDrawGizmosSelected()
        {
            InfluenceMapGizmoDrawer.DrawGizmos(this);
        }

        /// <summary>Inicjalizacja mapy na podstawie aktualnej konfiguracji</summary>
        public void Initialize()
        {
            ResolveActiveSettings();
            Bounds? anchorBounds = GetAnchorBounds();
            Vector3 computedOrigin = activeGridSettings.ComputeOrigin(transform, anchorBounds);
            int w = activeGridSettings.Width;
            int h = activeGridSettings.Height;
            float cs = activeGridSettings.CellSize;
            if (grid == null) grid = new InfluenceGrid(w, h, cs, computedOrigin);
            else grid.Resize(w, h, cs, computedOrigin);
            if (scheduler == null) scheduler = new UpdateScheduler(activeUpdateSettings);
            else scheduler.SetSettings(activeUpdateSettings);
            isInitialized = true;
        }

        /// <summary>Rejestracja źródła</summary>
        /// <param name="source">Źródło do zarejestrowania</param>
        public void RegisterSource(IInfluenceSource source)
        {
            if (source == null || sources.Contains(source)) return;
            sources.Add(source);
            OnSourceRegistered?.Invoke(source);
        }

        /// <summary>Wyrejestrowanie źródła</summary>
        /// <param name="source">Źródło do wyrejestrowania</param>
        public void UnregisterSource(IInfluenceSource source)
        {
            if (source == null) return;
            if (sources.Remove(source)) OnSourceUnregistered?.Invoke(source);
        }

        /// <summary>rejestracja przeszkody</summary>
        /// <param name="obstacle">Przeszkoda do zarejestrowania</param>
        public void RegisterObstacle(IInfluenceObstacle obstacle)
        {
            if (obstacle == null || obstacles.Contains(obstacle)) return;
            obstacles.Add(obstacle);
            OnObstacleRegistered?.Invoke(obstacle);
        }

        /// <summary>Wyrejstorwanie przeszkody</summary>
        /// <param name="obstacle">Przeszkoda do wyrejestrowania</param>
        public void UnregisterObstacle(IInfluenceObstacle obstacle)
        {
            if (obstacle == null) return;
            if (obstacles.Remove(obstacle)) OnObstacleUnregistered?.Invoke(obstacle);
        }

        /// <summary>Wymuszenie aktualizacji mapy</summary>
        public void ForceUpdate()
        {
            if (!isInitialized) return;
            float deltaTime = scheduler.ForceUpdate();
            ExecuteUpdatePipeline(deltaTime);
        }

        public void SetExternallyScheduled(bool value) => externallyScheduled = value;

        /// <summary>Pobieranie wartości wpływu w pozycji świata</summary>
        public float GetInfluence(Vector3 worldPosition)
        {
            return grid != null ? grid.GetValue(worldPosition) : 0f;
        }

        /// <summary>Czyszczenie wartości wpływów w mapie</summary>
        public void ClearMap()
        {
            grid?.Clear();
        }
    }
}
