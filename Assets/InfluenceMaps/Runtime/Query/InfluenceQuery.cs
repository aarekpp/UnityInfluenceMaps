using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>
    /// Zapytania do siatek wpływów.
    /// Metody statyczne do odczytu wartości, wyszukiwania ekstremów i analizy obszarów.
    /// </summary>
    public static class InfluenceQuery
    {
        /// <summary>Pobiera wartość wpływu z pozycji w świecie</summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="worldPosition">Pozycja w świecie</param>
        /// <returns>Wartość wpływu lub 0 jeśli grid to null lub pozycja poza granicami</returns>
        public static float GetValueAt(InfluenceGrid grid, Vector3 worldPosition)
        {
            if (grid == null) return 0f;
            return grid.GetValue(worldPosition);
        }

        /// <summary>Pobiera wartość wpływu z koordynatów siatki</summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="x">Kolumna w siatce</param>
        /// <param name="y">Wiersz w siatce</param>
        /// <returns>Wartość wpływu lub 0 jeśli grid to null lub poza granicami</returns>
        public static float GetValueAt(InfluenceGrid grid, int x, int y)
        {
            if (grid == null) return 0f;
            return grid.GetValue(x, y);
        }

        /// <summary>Znajduje komórkę z najwyższą wartością w promieniu od punktu</summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="worldCenter">Środek obszaru przeszukiwania</param>
        /// <param name="radius">Promień przeszukiwania w jednostkach świata</param>
        /// <returns>Komórka z najwyższą wartością</returns>
        public static InfluenceCell GetHighestCellInRadius(InfluenceGrid grid, Vector3 worldCenter, float radius)
        {
            if (grid == null) return default;
            InfluenceCell best = default;
            bool found = false;
            IterateCellsInRadius(grid, worldCenter, radius, (x, y, value, distance) =>
            {
                if (!found || value > best.Value)
                {
                    best = new InfluenceCell(x, y, value);
                    found = true;
                }
            });
            return best;
        }

        /// <summary>Znajduje komórkę z najniższą wartością w promieniu od punktu</summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="worldCenter">Środek obszaru przeszukiwania</param>
        /// <param name="radius">Promień przeszukiwania w jednostkach świata</param>
        /// <returns>Komórka z najniższą wartością</returns>
        public static InfluenceCell GetLowestCellInRadius(InfluenceGrid grid, Vector3 worldCenter, float radius)
        {
            if (grid == null) return default;
            InfluenceCell best = default;
            bool found = false;
            IterateCellsInRadius(grid, worldCenter, radius, (x, y, value, distance) =>
            {
                if (!found || value < best.Value)
                {
                    best = new InfluenceCell(x, y, value);
                    found = true;
                }
            });
            return best;
        }

        /// <summary>Oblicza średnią wartość wpływu w promieniu od punktu</summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="worldCenter">Środek obszaru przeszukiwania</param>
        /// <param name="radius">Promień przeszukiwania w jednostkach świata</param>
        /// <returns>Średnia wartość lub 0 jeśli brak komórek w zasięgu</returns>
        public static float GetAverageInRadius(InfluenceGrid grid, Vector3 worldCenter, float radius)
        {
            if (grid == null) return 0f;
            float sum = 0f;
            int count = 0;
            IterateCellsInRadius(grid, worldCenter, radius, (x, y, value, distance) =>
            {
                sum += value;
                count++;
            });
            return count > 0 ? sum / count : 0f;
        }

        /// <summary>Oblicza sumę wartości wpływu w promieniu od punktu</summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="worldCenter">Środek obszaru przeszukiwania</param>
        /// <param name="radius">Promień przeszukiwania w jednostkach świata</param>
        /// <returns>Suma wartości w zasięgu</returns>
        public static float GetSumInRadius(InfluenceGrid grid, Vector3 worldCenter, float radius)
        {
            if (grid == null) return 0f;
            float sum = 0f;
            IterateCellsInRadius(grid, worldCenter, radius, (x, y, value, distance) =>
            {
                sum += value;
            });
            return sum;
        }

        /// <summary>Zwraca wszystkie komórki z wartością powyżej progu</summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="threshold">Próg wartości</param>
        /// <returns>Lista komórek z wartością powyżej progu</returns>
        public static List<InfluenceCell> GetCellsAboveThreshold(InfluenceGrid grid, float threshold)
        {
            var results = new List<InfluenceCell>();
            if (grid == null) return results;
            ReadOnlySpan<float> values = grid.Values;
            for (int i = 0; i < grid.CellCount; i++)
            {
                if (values[i] > threshold)
                {
                    Vector2Int coords = grid.GetCoordinates(i);
                    results.Add(new InfluenceCell(coords.x, coords.y, values[i]));
                }
            }
            return results;
        }

        /// <summary>Zwraca wszystkie komórki z wartością poniżej progu</summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="threshold">Próg wartości</param>
        /// <returns>Lista komórek z wartością poniżej progu</returns>
        public static List<InfluenceCell> GetCellsBelowThreshold(InfluenceGrid grid, float threshold)
        {
            var results = new List<InfluenceCell>();
            if (grid == null) return results;
            ReadOnlySpan<float> values = grid.Values;
            for (int i = 0; i < grid.CellCount; i++)
            {
                if (values[i] < threshold)
                {
                    Vector2Int coords = grid.GetCoordinates(i);
                    results.Add(new InfluenceCell(coords.x, coords.y, values[i]));
                }
            }
            return results;
        }

        /// <summary>
        /// Oblicza gradient wpływu w danym punkcie
        /// Gradient wskazuje kierunek największego wzrostu wartości wpływu
        /// Obliczany jako różnica centralna z sąsiednich komórek
        /// </summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="worldPosition">Pozycja w świecie</param>
        /// <returns>Wektor gradientu w płaszczyźnie XZ (Y = 0)</returns>
        public static Vector3 GetInfluenceGradient(InfluenceGrid grid, Vector3 worldPosition)
        {
            if (grid == null) return Vector3.zero;
            Vector2Int cell = grid.WorldToGrid(worldPosition);
            int x = cell.x;
            int y = cell.y;
            float left = grid.GetValue(Mathf.Max(0, x - 1), y);
            float right = grid.GetValue(Mathf.Min(grid.Width - 1, x + 1), y);
            float down = grid.GetValue(x, Mathf.Max(0, y - 1));
            float up = grid.GetValue(x, Mathf.Min(grid.Height - 1, y + 1));
            float inv = 1f / (2f * grid.CellSize);
            float gradX = (right - left) * inv;
            float gradZ = (up - down) * inv;
            return new Vector3(gradX, 0f, gradZ);
        }

        /// <summary>Zwraca wszystkie komórki w promieniu od punktu</summary>
        /// <param name="grid">Siatka wpływów</param>
        /// <param name="worldCenter">Środek obszaru przeszukiwania</param>
        /// <param name="radius">Promień przeszukiwania w jednostkach świata</param>
        /// <returns>Lista komórek w promieniu</returns>
        public static List<InfluenceCell> GetCellsInRadius(InfluenceGrid grid, Vector3 worldCenter, float radius)
        {
            var results = new List<InfluenceCell>();
            if (grid == null) return results;

            IterateCellsInRadius(grid, worldCenter, radius, (x, y, value, distance) =>
            {
                results.Add(new InfluenceCell(x, y, value));
            });

            return results;
        }

        /// <summary>Delegat wywoływany dla każdej komórki w promieniu</summary>
        /// <param name="x">Kolumna komórki</param>
        /// <param name="y">Wiersz komórki</param>
        /// <param name="value">Wartość wpływu w komórce</param>
        /// <param name="distance">Odległość od centrum do środka komórki</param>
        private delegate void CellAction(int x, int y, float value, float distance);

        /// <summary>Iteruje po komórkach w promieniu od punktu i wywołuje akcję</summary>
        private static void IterateCellsInRadius(InfluenceGrid grid, Vector3 worldCenter, float radius, CellAction action)
        {
            Vector2Int centerCell = grid.WorldToGrid(worldCenter);
            int cellRadius = Mathf.CeilToInt(radius / grid.CellSize);
            int minX = Mathf.Max(0, centerCell.x - cellRadius);
            int maxX = Mathf.Min(grid.Width - 1, centerCell.x + cellRadius);
            int minY = Mathf.Max(0, centerCell.y - cellRadius);
            int maxY = Mathf.Min(grid.Height - 1, centerCell.y + cellRadius);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector3 cellWorld = grid.GridToWorld(x, y);
                    float dx = cellWorld.x - worldCenter.x;
                    float dz = cellWorld.z - worldCenter.z;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    if (distance > radius) continue;
                    float value = grid.GetValue(x, y);
                    action(x, y, value, distance);
                }
            }
        }
    }
}
