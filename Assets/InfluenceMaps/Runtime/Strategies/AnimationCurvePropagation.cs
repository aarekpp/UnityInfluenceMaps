using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>
    /// Propagacja wpływu oparta o Animation Curve
    /// Krzywa definiuje kształt spadku wpływu wraz z odległością od źródła
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnimationCurvePropagation", menuName = "Influence Maps/Propagation Animation Curve")]
    public class AnimationCurvePropagation : ScriptableObject, IPropagationFunction
    {
        /// <summary>Krzywa spadku wpływu</summary>
        [SerializeField]
        private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        /// <summary>Oblicza przyrost wartości wpływu dodawany do komórki</summary>
        /// <param name="baseValue">Intensywność źródła</param>
        /// <param name="distance">Odległość od źródła do komórki</param>
        /// <param name="maxDistance">Zasięg źródła wpływu</param>
        /// <returns>Przyrost wpływu dodawany do komórki</returns>
        public float Evaluate(float baseValue, float distance, float maxDistance)
        {
            if (maxDistance <= 0f) return 0f;
            float t = Mathf.Clamp01(distance / maxDistance);
            float multiplier = curve.Evaluate(t);
            return baseValue * multiplier;
        }

        /// <summary>Krzywa propagacji modyfikowalna z kodu</summary>
        public AnimationCurve Curve
        {
            get => curve;
            set => curve = value ?? AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        }
    }
}
