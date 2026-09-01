using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Harmonogram aktualizacji mapy. Odmierza czas i decyduje kiedy zaktualizować</summary>
    public class UpdateScheduler
    {
        /// <summary>Zbiera deltaTime między aktualizacjami</summary>
        private float timeAccumulator;

        /// <summary>Czas ostatniej aktualizacji</summary>
        private float lastUpdateTime;

        /// <summary>Czy został zainicjalizowany</summary>
        private bool initialized;

        /// <summary>Referencja do ustawień aktualizacji</summary>
        private UpdateSettings settings;

        /// <summary>Logika akumulatora czasu dla trybów Interval i TargetFPS</summary>
        private bool CheckTimerUpdate(float frameDeltaTime, out float mapDeltaTime)
        {
            mapDeltaTime = 0f;
            float interval = settings.EffectiveInterval;
            if (interval <= 0f)
            {
                mapDeltaTime = frameDeltaTime;
                RecordUpdate();
                return true;
            }
            if (!initialized)
            {
                mapDeltaTime = interval;
                RecordUpdate();
                return true;
            }
            timeAccumulator += frameDeltaTime;
            if (timeAccumulator >= interval)
            {
                mapDeltaTime = interval;
                timeAccumulator -= interval;
                if (timeAccumulator > interval) timeAccumulator = interval;
                RecordUpdate();
                return true;
            }
            return false;
        }

        /// <summary>Zapamiętywanie czasu aktualizacji</summary>
        private void RecordUpdate()
        {
            lastUpdateTime = Time.time;
            initialized = true;
        }

        /// <summary>Konstruktor z ustawieniami</summary>
        /// <param name="settings">Ustawienia aktualizacji</param>
        public UpdateScheduler(UpdateSettings settings)
        {
            this.settings = settings;
            Reset();
        }

        /// <summary>Sprawdza czy mapa powinna się zaktualizować w tej klatce</summary>
        /// <param name="frameDeltaTime">Czas tej klatki - Time.deltaTime dla Update lub Time.fixedDeltaTime dla FixedUpdate</param>
        /// <param name="mapDeltaTime">Czas od ostatniej aktualizacji mapy w sekundach</param>
        /// <returns>True jeśli powinna się zaktualizować</returns>
        public bool ShouldUpdate(float frameDeltaTime, out float mapDeltaTime)
        {
            mapDeltaTime = 0f;

            switch (settings.Mode)
            {
                case UpdateMode.EveryFrame:
                case UpdateMode.FixedUpdate:
                    mapDeltaTime = frameDeltaTime;
                    RecordUpdate();
                    return true;

                case UpdateMode.Interval:
                case UpdateMode.TargetFPS:
                    return CheckTimerUpdate(frameDeltaTime, out mapDeltaTime);

                case UpdateMode.Manual:
                default:
                    return false;
            }
        }

        /// <summary>Wymuszenie aktualizacji</summary>
        /// <returns>Czas w sekundach od ostatniej aktualizacji</returns>
        public float ForceUpdate()
        {
            float currentTime = Time.time;
            float mapDeltaTime;

            if (!initialized) mapDeltaTime = settings.EffectiveInterval > 0f ? settings.EffectiveInterval : Time.deltaTime;
            else mapDeltaTime = currentTime - lastUpdateTime;

            mapDeltaTime = Mathf.Max(mapDeltaTime, Time.deltaTime);
            RecordUpdate();
            timeAccumulator = 0f;
            return mapDeltaTime;
        }

        /// <summary>Zmiana ustawień aktualizacji</summary>
        public void SetSettings(UpdateSettings newSettings)
        {
            settings = newSettings;
            Reset();
        }

        /// <summary>Resetuje scheduler do stanu początkowego</summary>
        public void Reset()
        {
            timeAccumulator = 0f;
            lastUpdateTime = Time.time;
            initialized = false;
        }
    }
}
