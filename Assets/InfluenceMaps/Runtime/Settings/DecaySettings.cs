using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Ustawienie zaniku wpływu</summary>
    [Serializable]
    public class DecaySettings
    {
        /// <summary>Referencja do ScriptableObject z funkcją zaniku</summary>
        [SerializeField]
        private ScriptableObject decayFunction;

        /// <summary>Czy zastosować osobny tryb zaniku dla komórek poza zasięgiem</summary>
        [Tooltip("Osobne wygaszanie komórek, które wyszły poza jakikolwiek wpływ")]
        [SerializeField] private bool useOutOfRangeFade = false;
        public bool UseOutOfRangeFade => useOutOfRangeFade;

        /// <summary>Ułamek wartości wygaszany dla komórek poza zasięgiem źródeł</summary>
        [Range(0f, 1f)]
        [Tooltip("Ułamek wartości wygaszany na klatkę poza zasięgiem")]
        [SerializeField] private float outOfRangeFadeFactor = 0.1f;
        public float OutOfRangeFadeFactor => outOfRangeFadeFactor;

        /// <summary>Zwraca funkcję zaniku jako IDecayFunction</summary>
        public IDecayFunction GetFunction()
        {
            if (decayFunction == null) return null;
            if (decayFunction is IDecayFunction function) return function;

            Debug.LogWarning($"[InfluenceMaps] Przypisany obiekt '{decayFunction.name}' " + $"(typ: {decayFunction.GetType().Name}) nie implementuje IDecayFunction", decayFunction);

            return null;
        }

        /// <summary>Ustawia funkcję zaniku z kodu</summary>
        /// <param name="function">ScriptableObject implementujący IDecayFunction</param>
        public void SetFunction(ScriptableObject function)
        {
            if (function != null && function is not IDecayFunction)
            {
                Debug.LogWarning($"[InfluenceMaps] Obiekt '{function.name}' nie implementuje IDecayFunction", function);
                return;
            }

            decayFunction = function;
        }

        /// <summary>Sprawdza czy funkcja zaniku jest przypisana poprawnie</summary>
        public bool IsValid => decayFunction != null && decayFunction is IDecayFunction;
    }
}