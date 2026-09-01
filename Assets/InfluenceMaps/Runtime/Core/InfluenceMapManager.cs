using System.Collections.Generic;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Centralny manager systemu map wpływów</summary>
    [AddComponentMenu("Influence Maps/Influence Map Manager")]
    public class InfluenceMapManager : MonoBehaviour
    {
        /// <summary>Tryb harmonogramu aktualizacji map</summary>
        public enum MapUpdateScheduling
        {
            /// <summary>Każda mapa aktualizuje się sama</summary>
            SelfScheduled,
            /// <summary>Manager aktualizuje wszystkie mapy w jednej klatce</summary>
            AllAtOnce,
            /// <summary>Manager aktualizuje maksymalnie N map na klatkę</summary>
            Budgeted
        }

        /// <summary>Statyczna instancja managera</summary>
        private static InfluenceMapManager instance;

        /// <summary>Globalna konfiguracja systemu</summary>
        [Header("Konfiguracja")]
        [Tooltip("Globalna konfiguracja domyślna dla wszystkich map")]
        [SerializeField]
        private InfluenceMapsGlobalConfig globalConfig;

        /// <summary>Czy automatycznie wyszukać i zarejestrować wszystkie mapy</summary>
        [Header("Automatyczna rejestracja")]
        [Tooltip("Automatyczne wyszukanie i rejestracja wszystkich map na scenie")]
        [SerializeField]
        private bool autoDiscoverMaps = true;

        /// <summary>Metoda zarządzania wykonywaniem aktualizacji map wpływów</summary>
        [Header("Harmonogram aktualizacji")]
        [Tooltip("SelfScheduled = każda mapa sama się aktualizuje, AllAtOnce = wszystkie naraz, Budgeted = N na klatkę")]
        [SerializeField]
        private MapUpdateScheduling scheduling = MapUpdateScheduling.SelfScheduled;

        /// <summary>Interwał aktualizacji dla wszystkich map w tej samej klatce</summary>
        [Tooltip("Co ile sekund aktualizować wszystkie mapy naraz")]
        [SerializeField, Min(0f)]
        private float allAtOnceInterval = 0f;

        /// <summary>Liczba map aktualizowanych w jednym czasie</summary>
        [Tooltip("Ile map maksymalnie aktualizować w jednej klatce")]
        [SerializeField, Min(1)]
        private int mapsPerFrame = 4;

        /// <summary>Licznik dla aktualizacji N map naraz</summary>
        private int roundRobinCursor;

        /// <summary>Licznik czasu dla wszystkich map naraz</summary>
        private float allAtOnceTimer;

        /// <summary>Lista zarejestrowanych map</summary>
        private readonly List<InfluenceMap> maps = new List<InfluenceMap>();

        /// <summary>Słownik nazwa mapy - mapa</summary>
        private readonly Dictionary<string, InfluenceMap> mapsByName = new Dictionary<string, InfluenceMap>();

        /// <summary>Globalna kofiguracja</summary>
        public InfluenceMapsGlobalConfig GlobalConfig => globalConfig;

        /// <summary>Odczyt wszystkich zarejestrowanych map</summary>
        public IReadOnlyList<InfluenceMap> Maps => maps;

        /// <summary>Liczba zarejestrowanych map</summary>
        public int MapCount => maps.Count;

        /// <summary>Inicjalizacja i automatyczna rejestracja map</summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[InfluenceMaps] Influence Map Manager: Znaleziono drugą instację InfluenceMapManager. Zostaje usunięta");
                Destroy(gameObject);
                return;
            }
            instance = this;
            if (globalConfig == null) Debug.LogWarning($"[InfluenceMaps] Influence Map Manager: Brak przypisanego GlobalConfig");
            if (autoDiscoverMaps) DiscoverAndRegisterMaps();
        }

        /// <summary>Czyszczenie po zniszczeniu</summary>
        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>Po inicjalizacji map ustawia tryb harmonogramu</summary>
        private void Start()
        {
            ApplySchedulingMode();
        }

        /// <summary>Sterowanie aktualizacją map zależnie od trybu harmonogramu</summary>
        private void Update()
        {
            switch (scheduling)
            {
                case MapUpdateScheduling.AllAtOnce:
                    allAtOnceTimer += Time.deltaTime;
                    if (allAtOnceTimer >= allAtOnceInterval)
                    {
                        allAtOnceTimer -= allAtOnceInterval;
                        for (int i = 0; i < maps.Count; i++)
                            if (maps[i] != null) maps[i].ForceUpdate();
                    }
                    break;

                case MapUpdateScheduling.Budgeted:
                    int n = Mathf.Min(mapsPerFrame, maps.Count);
                    for (int k = 0; k < n; k++)
                    {
                        if (maps.Count == 0) break;
                        roundRobinCursor %= maps.Count;
                        InfluenceMap m = maps[roundRobinCursor];
                        roundRobinCursor++;
                        if (m != null) m.ForceUpdate();
                    }
                    break;
            }
        }

        /// <summary>Ustawia flagę harmonogramu zewnętrznego na wszystkich mapach wg trybu</summary>
        private void ApplySchedulingMode()
        {
            bool external = scheduling != MapUpdateScheduling.SelfScheduled;
            for (int i = 0; i < maps.Count; i++)
                if (maps[i] != null) maps[i].SetExternallyScheduled(external);
        }

        /// <summary>Zmienia tryb harmonogramu w runtime</summary>
        /// <param name="mode">Nowy tryb</param>
        /// <param name="budget">Opcjonalny limit map na klatkę dla trybu Budgeted</param>
        public void SetScheduling(MapUpdateScheduling mode, int budget = -1)
        {
            scheduling = mode;
            if (budget > 0) mapsPerFrame = budget;
            roundRobinCursor = 0;
            allAtOnceTimer = 0f;
            ApplySchedulingMode();
        }

        /// <summary>Wyszukiwanie wszystkich map w scenie i rejestracja</summary>
        private void DiscoverAndRegisterMaps()
        {
            InfluenceMap[] sceneMaps = FindObjectsByType<InfluenceMap>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneMaps.Length; i++) RegisterMap(sceneMaps[i]);
        }

        /// <summary>Dostęp do instancji managera</summary>
        public static InfluenceMapManager Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindAnyObjectByType<InfluenceMapManager>();
                return instance;
            }
        }

        /// <summary>Wyszukiwanie mapy po nazwie</summary>
        /// <param name="mapName">Nazwa mapy</param>
        /// <returns>Mapa lub null jeśli nie znaleziono</returns>
        public InfluenceMap GetMap(string mapName)
        {
            if (string.IsNullOrEmpty(mapName)) return null;
            mapsByName.TryGetValue(mapName, out InfluenceMap map);
            return map;
        }

        /// <summary>Wyszukiwanie mapy po nazwie</summary>
        /// <param name="mapName">Nazwa mapy</param>
        /// <param name="map">Znaleziona mapa lub null</param>
        /// <returns>True jeśli mapa została znaleziona</returns>
        public bool TryGetMap(string mapName, out InfluenceMap map)
        {
            map = GetMap(mapName);
            return map != null;
        }

        /// <summary>Rejestruje mapę w managerze</summary>
        /// <param name="map">Mapa do zarejestrowania</param>
        public void RegisterMap(InfluenceMap map)
        {
            if (map == null || maps.Contains(map)) return;
            maps.Add(map);
            string name = map.MapName;
            if (!string.IsNullOrEmpty(name))
            {
                if (mapsByName.ContainsKey(name)) Debug.LogWarning($"[InfluenceMaps] Influence Map Manager: Duplikat nazwy mapy '{name}'");
                mapsByName[name] = map;
            }
        }

        /// <summary>Wyrejestrowuje mapę z managera</summary>
        /// <param name="map">Mapa do wyrejestrowania</param>
        public void UnregisterMap(InfluenceMap map)
        {
            if (map == null) return;
            maps.Remove(map);
            string name = map.MapName;
            if (!string.IsNullOrEmpty(name) && mapsByName.TryGetValue(name, out var registered))
            {
                if (registered == map) mapsByName.Remove(name);
            }
        }

        /// <summary>Wymusza aktualizację wszystkich map</summary>
        public void ForceUpdateAll()
        {
            for (int i = 0; i < maps.Count; i++) maps[i].ForceUpdate();
        }

        /// <summary>Czyści wszystkie mapy</summary>
        public void ClearAll()
        {
            for (int i = 0; i < maps.Count; i++) maps[i].ClearMap();
        }

        /// <summary>Ustawia globalną konfigurację</summary>
        /// <param name="config">Nowa globalna konfiguracja</param>
        public void SetGlobalConfig(InfluenceMapsGlobalConfig config)
        {
            globalConfig = config;
        }
    }
}