using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Konfiguracja jednej warstwy wizualizacji mapy wpływów</summary>
    [Serializable]
    public class MapVisualizationLayer
    {
        /// <summary>Mapa wpływów do wizualizacji</summary>
        [Tooltip("Mapa wpływów wyświetlana w tej warstwie")]
        [SerializeField]
        private InfluenceMap map;

        /// <summary>Gradient kolorów mapujący wartość wpływu na kolor warstwy</summary>
        [Tooltip("Gradient kolorów: lewy koniec = min, środek = zero, prawy koniec = max")]
        [SerializeField]
        private Gradient colorGradient;

        /// <summary>Przezroczystość warstwy [0, 1]</summary>
        [Tooltip("Przezroczystość warstwy wizualizacji")]
        [Range(0f, 1f)]
        [SerializeField]
        private float alpha = 0.5f;

        /// <summary>Czy warstwa jest włączona</summary>
        [Tooltip("Włącz lub wyłącz tę warstwę wizualizacji")]
        [SerializeField]
        private bool enabled = true;

        /// <summary>Mapa wpływów</summary>
        public InfluenceMap Map
        {
            get => map;
            set => map = value;
        }

        /// <summary>Gradient kolorów warstwy</summary>
        public Gradient ColorGradient
        {
            get => colorGradient;
            set => colorGradient = value;
        }

        /// <summary>Przezroczystość warstwy [0, 1]</summary>
        public float Alpha
        {
            get => alpha;
            set => alpha = Mathf.Clamp01(value);
        }

        /// <summary>Czy warstwa jest włączona</summary>
        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        /// <summary>Czy warstwa jest gotowa do renderowania (mapa przypisana, aktywna, zainicjalizowana)</summary>
        public bool IsReady => enabled && map != null && map.IsInitialized && map.Grid != null;

        /// <summary>Oblicza kolor dla danej wartości wpływu na podstawie gradientu warstwy</summary>
        /// <param name="influenceValue">Wartość wpływu</param>
        /// <param name="rangeMin">Dolna granica zakresu</param>
        /// <param name="rangeMax">Górna granica zakresu</param>
        /// <returns>Kolor z gradientu z zastosowaną przezroczystością warstwy</returns>
        public Color GetColor(float influenceValue, float rangeMin, float rangeMax)
        {
            float range = rangeMax - rangeMin;
            float t;
            if (Mathf.Abs(range) < InfluenceMapConstants.InfluenceValueEpsilon) t = 0.5f;
            else t = Mathf.Clamp01((influenceValue - rangeMin) / range);
            Color color = colorGradient.Evaluate(t);
            color.a *= alpha;
            return color;
        }

        /// <summary>Konstruktor domyślny z gradientem niebieski→przezroczysty→czerwony</summary>
        public MapVisualizationLayer()
        {
            colorGradient = CreateDefaultGradient();
        }

        /// <summary>Domyślny gradient</summary>
        private static Gradient CreateDefaultGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.blue, 0f),
                    new GradientColorKey(Color.black, 0.5f),
                    new GradientColorKey(Color.red, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 0.5f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }
    }
}
