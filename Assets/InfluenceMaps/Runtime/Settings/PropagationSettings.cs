using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Ustawienie propagacji wpływu</summary>
    [Serializable]
    public class PropagationSettings
    {
        /// <summary>Referencja do ScriptableObject</summary>
        [SerializeField]
        private ScriptableObject propagationFunction;

        /// <summary>Zwraca funkcję propagacji jako IPropagationFunction</summary>
        public IPropagationFunction GetFunction()
        {
            if (propagationFunction == null) return null;
            if (propagationFunction is IPropagationFunction function) return function;
            Debug.LogWarning($"[InfluenceMaps] Przypisany obiekt '{propagationFunction.name}' " + $"(typ: {propagationFunction.GetType().Name}) nie implementuje IPropagationFunction", propagationFunction);
            return null;
        }

        /// <summary>Ustawia funkcję propagacji z kodu</summary>
        /// <param name="function">ScriptableObject implementujący IPropagationFunction</param>
        public void SetFunction(ScriptableObject function)
        {
            if (function != null && function is not IPropagationFunction)
            {
                Debug.LogWarning($"[InfluenceMaps] Obiekt '{function.name}' nie implementuje IPropagationFunction", function);
                return;
            }
            propagationFunction = function;
        }

        /// <summary>Czy funkcja propagacji jest przypisana poprawnie</summary>
        public bool IsValid => propagationFunction != null && propagationFunction is IPropagationFunction;
    }
}
