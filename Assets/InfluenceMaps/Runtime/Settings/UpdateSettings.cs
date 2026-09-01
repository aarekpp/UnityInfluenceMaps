using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Ustawienia aktualizacji mapy</summary>
    [Serializable]
    public class UpdateSettings
    {
        /// <summary>Wybrany tryb aktualizacji</summary>
        [SerializeField]
        private UpdateMode mode = UpdateMode.TargetFPS;

        /// <summary>Co ile aktualizować mapę dla trybu Interval</summary>
        [Min(0.001f)]
        [SerializeField]
        private float interval = InfluenceMapConstants.DefaultUpdateInterval;

        /// <summary>Ile razy na sekundę aktualizować mapę dla trybu TargetFPS</summary>
        [Min(1)]
        [SerializeField]
        private int targetFPS = InfluenceMapConstants.DefaultTargetFPS;

        /// <summary>Tryb aktualizacji</summary>
        public UpdateMode Mode
        {
            get => mode;
            set => mode = value;
        }

        /// <summary>Interwał w sekundach</summary>
        public float Interval
        {
            get => interval;
            set => interval = Mathf.Max(0.001f, value);
        }

        /// <summary>Ile razy na sekundę</summary>
        public int TargetFPS
        {
            get => targetFPS;
            set => targetFPS = Mathf.Max(1, value);
        }

        /// <summary>Interwał w sekundach niezależnie od trybu</summary>
        public float EffectiveInterval
        {
            get
            {
                return mode switch
                {
                    UpdateMode.Interval => interval,
                    UpdateMode.TargetFPS => 1f / targetFPS,
                    _ => 0f
                };
            }
        }

        /// <summary>Konstruktor domyślny</summary>
        public UpdateSettings() { }

        /// <summary>Konstruktor z parametrami</summary>
        /// <param name="mode">Tryb aktualizacji</param>
        /// <param name="value">Czas aktualizacji dla trybu Interval w sekundach, liczba aktualizacji na sekundę dla trybu TargetFPS</param>
        public UpdateSettings(UpdateMode mode, float value = 0f)
        {
            this.mode = mode;
            switch (mode)
            {
                case UpdateMode.Interval:
                    Interval = value;
                    break;
                case UpdateMode.TargetFPS:
                    TargetFPS = Mathf.RoundToInt(value);
                    break;
            }
        }

        /// <summary>Walidacja</summary>
        public void Validate()
        {
            interval = Mathf.Max(0.001f, interval);
            targetFPS = Mathf.Max(1, targetFPS);
        }
    }
}
