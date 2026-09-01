using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Ustawienia wizualizacji mapy wpływów</summary>
    [Serializable]
    public class VisualizationSettings
    {
        /// <summary>Gradient mapujący wartość wpływu na kolor</summary>
        [SerializeField]
        private Gradient colorGradient;

        /// <summary>Czy automatycznie dostosowywać zakres kolorów do aktualnych wartości na siatce</summary>
        [Tooltip("Automatycznie dopasuj zakres kolorów do aktualnych wartości na siatce")]
        [SerializeField]
        private bool autoRange = true;

        /// <summary>Minimalna wartość wpływu, lewy koniec gradientu</summary>
        [SerializeField]
        private float minValue = InfluenceMapConstants.DefaultMinInfluenceValue;

        /// <summary>Maksymalna wartość wpływu, prawy koniec gradientu</summary>
        [SerializeField]
        private float maxValue = InfluenceMapConstants.DefaultMaxInfluenceValue;

        /// <summary>Czy wizualizacja jest włączona</summary>
        [SerializeField]
        private bool enabled = true;

        /// <summary>Czy rysować linie siatki</summary>
        [SerializeField]
        private bool showGridLines = true;

        /// <summary>Czy wyświetlać wartości liczbowe w komórkach</summary>
        [SerializeField]
        private bool showValues = false;

        /// <summary>Przezroczystość wizualizacji</summary>
        [Range(0f, 1f)]
        [SerializeField]
        private float alpha = InfluenceMapConstants.DefaultGizmoAlpha;

        /// <summary>Cache auto-range — dolna granica</summary>
        private float autoMin;

        /// <summary>Cache auto-range — górna granica</summary>
        private float autoMax;

        /// <summary>Gradient kolorów wizualizacji</summary>
        public Gradient ColorGradient
        {
            get => colorGradient;
            set => colorGradient = value;
        }

        /// <summary>Czy auto-range jest włączony</summary>
        public bool AutoRange
        {
            get => autoRange;
            set => autoRange = value;
        }

        /// <summary>Minimalna wartość zakresu ręcznego</summary>
        public float MinValue
        {
            get => minValue;
            set => minValue = value;
        }

        /// <summary>Maksymalna wartość zakresu ręcznego</summary>
        public float MaxValue
        {
            get => maxValue;
            set => maxValue = value;
        }

        /// <summary>Czy wizualizacja jest włączona</summary>
        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        /// <summary>Czy rysować linie siatki</summary>
        public bool ShowGridLines
        {
            get => showGridLines;
            set => showGridLines = value;
        }

        /// <summary>Czy wyświetlać wartości liczbowe</summary>
        public bool ShowValues
        {
            get => showValues;
            set => showValues = value;
        }

        /// <summary>Przezroczystość wizualizacji [0, 1]</summary>
        public float Alpha
        {
            get => alpha;
            set => alpha = Mathf.Clamp01(value);
        }

        /// <summary>Oblicza min/max z aktualnych wartości na siatce</summary>
        /// <param name="grid">Siatka do obliczenia zakresu</param>
        public void UpdateAutoRange(InfluenceGrid grid)
        {
            if (!autoRange || grid == null) return;
            ReadOnlySpan<float> values = grid.Values;
            float foundMin = 0f;
            float foundMax = 0f;

            for (int i = 0; i < values.Length; i++)
            {
                float v = values[i];
                if (v < foundMin) foundMin = v;
                if (v > foundMax) foundMax = v;
            }

            if (foundMin < -InfluenceMapConstants.InfluenceValueEpsilon)
            {
                float absMax = Mathf.Max(Mathf.Abs(foundMin), Mathf.Abs(foundMax));
                if (absMax < InfluenceMapConstants.InfluenceValueEpsilon) absMax = 1f;
                autoMin = -absMax;
                autoMax = absMax;
            }
            else
            {
                if (foundMax < InfluenceMapConstants.InfluenceValueEpsilon) foundMax = 1f;
                autoMin = 0f;
                autoMax = foundMax;
            }
        }

        /// <summary>Zwraca kolor dla danej wartości wpływu</summary>
        /// <param name="influenceValue">Wartość wpływu w komórce</param>
        /// <returns>Kolor z gradientu z zastosowaną przezroczystością</returns>
        public Color GetColor(float influenceValue)
        {
            float rangeMin = autoRange ? autoMin : minValue;
            float rangeMax = autoRange ? autoMax : maxValue;
            float range = rangeMax - rangeMin;
            float t;

            if (Mathf.Abs(range) < InfluenceMapConstants.InfluenceValueEpsilon) t = 0.5f;
            else t = Mathf.Clamp01((influenceValue - rangeMin) / range);

            Color color = colorGradient.Evaluate(t);
            color.a *= alpha;
            return color;
        }

        /// <summary>Aktywna minimalna wartość zakresu</summary>
        public float EffectiveMinValue => autoRange ? autoMin : minValue;

        /// <summary>Aktywna maksymalna wartość zakresu</summary>
        public float EffectiveMaxValue => autoRange ? autoMax : maxValue;

        /// <summary>Konstruktor domyślny z gradientem czerwony do zanikającej przezroczystości</summary>
        public VisualizationSettings()
        {
            colorGradient = CreateDefaultGradient();
        }

        /// <summary>Domyślny gradient czerwony z malejącą przezroczystością</summary>
        private static Gradient CreateDefaultGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.red, 0f),
                    new GradientColorKey(Color.red, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }
    }
}
