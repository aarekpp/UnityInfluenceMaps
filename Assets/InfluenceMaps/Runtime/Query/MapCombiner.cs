using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Łączenie wyników z wielu map wpływów o identycznych rozmiarach</summary>
    public static class MapCombiner
    {
        /// <summary>Łączenie wielu siatek jedną operacją</summary>
        /// <param name="grids">Tablica siatek do połączenia</param>
        /// <param name="operation">Operacja łączenia</param>
        /// <returns>Nowa tablica float[] z wynikami lub null jeśli dane wejściowe są błędne</returns>
        public static float[] Combine(InfluenceGrid[] grids, CombineOperation operation)
        {
            if (!ValidateGrids(grids)) return null;
            if (grids.Length == 1) return CopyGridValues(grids[0]);

            int cellCount = grids[0].CellCount;
            float[] result = new float[cellCount];

            switch (operation)
            {
                case CombineOperation.Add:
                    CombineAdd(grids, result, cellCount);
                    break;
                case CombineOperation.Subtract:
                    CombineSubtract(grids, result, cellCount);
                    break;
                case CombineOperation.Multiply:
                    CombineMultiply(grids, result, cellCount);
                    break;
                case CombineOperation.Max:
                    CombineMax(grids, result, cellCount);
                    break;
                case CombineOperation.Min:
                    CombineMin(grids, result, cellCount);
                    break;
                case CombineOperation.Average:
                    CombineAverage(grids, result, cellCount);
                    break;
                default:
                    Debug.LogWarning($"[InfluenceMaps] MapCombiner: Nieznana operacja łączenia: '{operation}'");
                    return null;
            }
            return result;
        }

        /// <summary>Łączenie siatek do istniejącej tablicy (bez alokacji)</summary>
        /// <param name="grids">Tablica siatek</param>
        /// <param name="operation">Operacja łączenia</param>
        /// <param name="result">Prealokowana tablica wynikowa</param>
        /// <returns>True jeśli operacja się powiodła</returns>
        public static bool CombineNonAlloc(InfluenceGrid[] grids, CombineOperation operation, float[] result)
        {
            if (!ValidateGrids(grids)) return false;
            int cellCount = grids[0].CellCount;
            if (result == null || result.Length < cellCount)
            {
                Debug.LogWarning($"[InfluenceMaps] MapCombiner: Tablica wynikowa za mała: {result?.Length ?? 0} < {cellCount}");
                return false;
            }
            Array.Clear(result, 0, cellCount);

            switch (operation)
            {
                case CombineOperation.Add:
                    CombineAdd(grids, result, cellCount);
                    break;
                case CombineOperation.Subtract:
                    CombineSubtract(grids, result, cellCount);
                    break;
                case CombineOperation.Multiply:
                    CombineMultiply(grids, result, cellCount);
                    break;
                case CombineOperation.Max:
                    CombineMax(grids, result, cellCount);
                    break;
                case CombineOperation.Min:
                    CombineMin(grids, result, cellCount);
                    break;
                case CombineOperation.Average:
                    CombineAverage(grids, result, cellCount);
                    break;
                default:
                    Debug.LogWarning($"[InfluenceMaps] MapCombiner: Nieznana operacja łączenia: '{operation}'");
                    return false;
            }
            return true;
        }

        /// <summary>Łączenie dwóch siatek jedną operacją</summary>
        /// <param name="gridA">Pierwsza siatka</param>
        /// <param name="gridB">Druga siatka</param>
        /// <param name="operation">Operacja łączenia</param>
        /// <returns>Nowa tablica float[] z wynikami</returns>
        public static float[] Combine(InfluenceGrid gridA, InfluenceGrid gridB, CombineOperation operation)
        {
            return Combine(new[] { gridA, gridB }, operation);
        }

        /// <summary>Suma ważona siatek</summary>
        /// <param name="grids">Tablica siatek</param>
        /// <param name="weights">Tablica wag (długość musi odpowiadać liczbie siatek)</param>
        /// <returns>Nowa tablica float[] z wynikami lub null</returns>
        public static float[] CombineWeightedAdd(InfluenceGrid[] grids, float[] weights)
        {
            if (!ValidateGrids(grids)) return null;
            if (weights == null || weights.Length != grids.Length)
            {
                Debug.LogWarning($"[InfluenceMaps] MapCombiner: Liczba wag ({weights?.Length ?? 0}) nie zgadza się z liczbą siatek ({grids.Length})");
                return null;
            }

            float[] result = new float[grids[0].CellCount];
            WeightedAdd(grids, weights, result);
            return result;
        }

        /// <summary>Średnia ważona siatek</summary>
        /// <param name="grids">Tablica siatek</param>
        /// <param name="weights">Tablica wag (długość musi odpowiadać liczbie siatek)</param>
        /// <returns>Nowa tablica float[] z wynikami lub null</returns>
        public static float[] CombineWeightedAverage(InfluenceGrid[] grids, float[] weights)
        {
            if (!ValidateGrids(grids)) return null;
            if (weights == null || weights.Length != grids.Length)
            {
                Debug.LogWarning($"[InfluenceMaps] MapCombiner: Liczba wag ({weights?.Length ?? 0}) nie zgadza się z liczbą siatek ({grids.Length})");
                return null;
            }
            float[] result = new float[grids[0].CellCount];
            WeightedAverage(grids, weights, result);
            return result;
        }

        /// <summary>Suma wartości ze wszystkich siatek</summary>
        private static void CombineAdd(InfluenceGrid[] grids, float[] result, int cellCount)
        {
            for (int g = 0; g < grids.Length; g++)
            {
                ReadOnlySpan<float> values = grids[g].Values;
                for (int i = 0; i < cellCount; i++) result[i] += values[i];
            }
        }

        /// <summary>Pierwsza siatka minus pozostałe</summary>
        private static void CombineSubtract(InfluenceGrid[] grids, float[] result, int cellCount)
        {
            ReadOnlySpan<float> first = grids[0].Values;
            for (int i = 0; i < cellCount; i++) result[i] = first[i];
            for (int g = 1; g < grids.Length; g++)
            {
                ReadOnlySpan<float> values = grids[g].Values;
                for (int i = 0; i < cellCount; i++) result[i] -= values[i];
            }
        }

        /// <summary>Iloczyn wartości wszystkich siatek</summary>
        private static void CombineMultiply(InfluenceGrid[] grids, float[] result, int cellCount)
        {
            ReadOnlySpan<float> first = grids[0].Values;
            for (int i = 0; i < cellCount; i++) result[i] = first[i];
            for (int g = 1; g < grids.Length; g++)
            {
                ReadOnlySpan<float> values = grids[g].Values;
                for (int i = 0; i < cellCount; i++) result[i] *= values[i];
            }
        }

        /// <summary>Maksimum z wszystkich siatek</summary>
        private static void CombineMax(InfluenceGrid[] grids, float[] result, int cellCount)
        {
            ReadOnlySpan<float> first = grids[0].Values;
            for (int i = 0; i < cellCount; i++) result[i] = first[i];
            for (int g = 1; g < grids.Length; g++)
            {
                ReadOnlySpan<float> values = grids[g].Values;
                for (int i = 0; i < cellCount; i++)
                {
                    if (values[i] > result[i]) result[i] = values[i];
                }
            }
        }

        /// <summary>Minimum z wszystkich siatek</summary>
        private static void CombineMin(InfluenceGrid[] grids, float[] result, int cellCount)
        {
            ReadOnlySpan<float> first = grids[0].Values;
            for (int i = 0; i < cellCount; i++) result[i] = first[i];
            for (int g = 1; g < grids.Length; g++)
            {
                ReadOnlySpan<float> values = grids[g].Values;
                for (int i = 0; i < cellCount; i++)
                {
                    if (values[i] < result[i]) result[i] = values[i];
                }
            }
        }

        /// <summary>Średnia arytmetyczna z wszystkich siatek</summary>
        private static void CombineAverage(InfluenceGrid[] grids, float[] result, int cellCount)
        {
            CombineAdd(grids, result, cellCount);
            float divisor = 1f / grids.Length;
            for (int i = 0; i < cellCount; i++) result[i] *= divisor;
        }

        /// <summary>Suma ważona siatek</summary>
        private static void WeightedAdd(InfluenceGrid[] grids, float[] weights, float[] result)
        {
            int cellCount = grids[0].CellCount;
            for (int g = 0; g < grids.Length; g++)
            {
                ReadOnlySpan<float> values = grids[g].Values;
                float weight = weights[g];
                for (int i = 0; i < cellCount; i++)
                    result[i] += values[i] * weight;
            }
        }

        /// <summary>Średnia ważona siatek</summary>
        private static void WeightedAverage(InfluenceGrid[] grids, float[] weights, float[] result)
        {
            int cellCount = grids[0].CellCount;
            float totalWeight = 0f;
            for (int i = 0; i < grids.Length; i++) totalWeight += weights[i];
            if (totalWeight <= InfluenceMapConstants.InfluenceValueEpsilon) return;
            for (int g = 0; g < grids.Length; g++)
            {
                ReadOnlySpan<float> values = grids[g].Values;
                float weight = weights[g];
                for (int i = 0; i < cellCount; i++) result[i] += values[i] * weight;
            }
            float inverseTotalWeight = 1f / totalWeight;
            for (int i = 0; i < cellCount; i++)
                result[i] *= inverseTotalWeight;
        }

        /// <summary>Walidacja tablicy siatek</summary>
        private static bool ValidateGrids(InfluenceGrid[] grids)
        {
            if (grids == null || grids.Length == 0)
            {
                Debug.LogWarning("[InfluenceMaps] MapCombiner: Brak siatek do połączenia");
                return false;
            }
            if (grids[0] == null)
            {
                Debug.LogWarning("[InfluenceMaps] MapCombiner: Pierwsza siatka jest null");
                return false;
            }
            int width = grids[0].Width;
            int height = grids[0].Height;

            for (int i = 1; i < grids.Length; i++)
            {
                if (grids[i] == null)
                {
                    Debug.LogWarning($"[InfluenceMaps] MapCombiner: Siatka [{i}] jest null");
                    return false;
                }
                if (grids[i].Width != width || grids[i].Height != height)
                {
                    Debug.LogWarning($"[InfluenceMaps] MapCombiner: Siatka [{i}] ma wymiary {grids[i].Width}x{grids[i].Height}, oczekiwano {width}x{height}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>Kopiowanie wartości siatki do nowej tablicy</summary>
        private static float[] CopyGridValues(InfluenceGrid grid)
        {
            float[] copy = new float[grid.CellCount];
            ReadOnlySpan<float> values = grid.Values;
            for (int i = 0; i < grid.CellCount; i++) copy[i] = values[i];
            return copy;
        }
    }
}
