using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>
    /// Zanik wpływu oparty o AnimationCurve
    /// Krzywa definiuje mnożnik wartości odejmowanej w funkcji znormalizowanej odległości
    /// </summary>
    [CreateAssetMenu(fileName = "NewAnimationCurveDecay", menuName = "Influence Maps/Decay Animation Curve")]
    public class AnimationCurveDecay : ScriptableObject, IDecayFunction
    {
        /// <summary>Krzywa definiująca mnożnik zanikuw funkcji distance/maxDistance</summary>
        [SerializeField]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        /// <summary>Obliczana wartość zaniku do odjęcia od bieżącej wartości w komórce</summary>
        /// <param name="currentValue">Bieżąca wartość wpływu</param>
        /// <param name="distance">Odległość od źródła do komórki</param>
        /// <param name="maxDistance">Zasięg źródła wpływu</param>
        /// <returns>Wartość zaniku d >= 0 do odjęcia od komórki</returns>
        public float Evaluate(float currentValue, float distance, float maxDistance)
        {
            if (maxDistance <= 0f) return 0f;
            float t = Mathf.Clamp01(distance / maxDistance);
            float multiplier = curve.Evaluate(t);
            return Mathf.Abs(currentValue) * multiplier;
        }

        /// <summary>Krzywa zaniku modyfikowalna z kodu</summary>
        public AnimationCurve Curve
        {
            get => curve;
            set => curve = value ?? AnimationCurve.Linear(0f, 1f, 1f, 0f);
        }
    }
}
