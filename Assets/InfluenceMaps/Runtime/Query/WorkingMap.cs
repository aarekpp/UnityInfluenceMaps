using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Tymczasowa mapa lokalna centrowana na pozycji agenta AI</summary>
    public class WorkingMap
    {
        /// <summary>Bufor wartości working map</summary>
        private float[] buffer;

        /// <summary>Bufor pomocniczy MultiplyFrom</summary>
        private bool[] coveredCache;

        /// <summary>Szerokość working map w komórkach</summary>
        private int width;

        /// <summary>Wysokość working map w komórkach</summary>
        private int height;

        /// <summary>Rozmiar komórki w jednostkach świata</summary>
        private float cellSize;

        /// <summary>Lewy dolny róg working map w świecie</summary>
        private Vector3 origin;

        /// <summary>Centrum working map (pozycja agenta)</summary>
        private Vector3 center;

        /// <summary>Promień zainteresowania w jednostkach świata</summary>
        private float radius;

        /// <summary>Promień zainteresowania w komórkach</summary>
        private int cellRadius;

        /// <summary>Czy working map pokrywa całą mapę bazową</summary>
        private bool isFullMap;

        /// <summary>Szerokość w komórkach</summary>
        public int Width => width;

        /// <summary>Wysokość w komórkach</summary>
        public int Height => height;

        /// <summary>Rozmiar komórki</summary>
        public float CellSize => cellSize;

        /// <summary>Lewy dolny róg w świecie</summary>
        public Vector3 Origin => origin;

        /// <summary>Centrum (pozycja agenta)</summary>
        public Vector3 Center => center;

        /// <summary>Łączna liczba komórek</summary>
        public int CellCount => width * height;

        /// <summary>Bufor wartości tylko do odczytu</summary>
        public ReadOnlySpan<float> Values => buffer.AsSpan(0, CellCount);

        /// <summary>Tworzy WorkingMap z podanym promieniem zainteresowania i rozmiarem komórki</summary>
        /// <param name="interestRadius">Promień zainteresowania w jednostkach świata</param>
        /// <param name="cellSize">Rozmiar komórki (powinien odpowiadać rozmiarowi komórek map bazowych)</param>
        public WorkingMap(float interestRadius, float cellSize)
        {
            this.cellSize = Mathf.Max(cellSize, InfluenceMapConstants.MinCellSize);
            radius = Mathf.Max(interestRadius, this.cellSize);
            cellRadius = Mathf.CeilToInt(this.radius / this.cellSize);
            width = cellRadius * 2 + 1;
            height = cellRadius * 2 + 1;
            buffer = new float[width * height];
        }

        /// <summary>Tworzy WorkingMap pokrywającą całą mapę bazową</summary>
        /// <param name="baseMap">Mapa bazowa której wymiary zostaną skopiowane</param>
        public WorkingMap(InfluenceGrid baseMap)
        {
            if (baseMap == null) throw new ArgumentNullException(nameof(baseMap));
            cellSize = baseMap.CellSize;
            isFullMap = true;
            width = baseMap.Width;
            height = baseMap.Height;
            origin = baseMap.Origin;
            radius = 0f;
            cellRadius = 0;
            buffer = new float[width * height];
        }

        /// <summary>Zeruje bufor i ustawia centrum na nową pozycję agenta w trybie wycinka</summary>
        /// <param name="agentPosition">Pozycja agenta w świecie</param>
        public void Clear(Vector3 agentPosition)
        {
            center = agentPosition;
            if (!isFullMap)
            {
                origin = new Vector3(agentPosition.x - cellRadius * cellSize, 0f, agentPosition.z - cellRadius * cellSize);
            }
            Array.Clear(buffer, 0, buffer.Length);
        }

        /// <summary>Zmienia rozmiar working map na wymiary nowej mapy bazowej</summary>
        /// <param name="baseMap">Nowa mapa bazowa</param>
        public void ResizeToMap(InfluenceGrid baseMap)
        {
            if (baseMap == null || !isFullMap) return;
            if (width == baseMap.Width && height == baseMap.Height && Mathf.Approximately(cellSize, baseMap.CellSize)) return;
            cellSize = baseMap.CellSize;
            width = baseMap.Width;
            height = baseMap.Height;
            origin = baseMap.Origin;
            buffer = new float[width * height];
        }

        /// <summary>Dodaje fragment mapy bazowej do working map z wagą</summary>
        /// <param name="baseMap">Mapa bazowa</param>
        /// <param name="weight">Mnożnik wartości (1 - pełna wartość, -0.5 - odwrócona połowa)</param>
        public void AddFrom(InfluenceGrid baseMap, float weight = 1f)
        {
            if (baseMap == null) return;
            IterateOverlap(baseMap, (localIdx, baseX, baseY) =>
            {
                float value = baseMap.GetValue(baseX, baseY);
                buffer[localIdx] += value * weight;
            });
        }

        /// <summary>Dodaje odwrócony fragment mapy bazowej</summary>
        /// <param name="baseMap">Mapa bazowa</param>
        /// <param name="weight">Mnożnik wartości</param>
        public void AddInverseFrom(InfluenceGrid baseMap, float weight = 1f)
        {
            if (baseMap == null) return;
            ComputeRange(baseMap, out float minVal, out float maxVal);
            float range = maxVal - minVal;
            if (Mathf.Abs(range) < InfluenceMapConstants.InfluenceValueEpsilon)
            {
                for (int i = 0; i < CellCount; i++) buffer[i] += weight;
                return;
            }
            float invRange = 1f / range;
            IterateOverlap(baseMap, (localIdx, baseX, baseY) =>
            {
                float value = baseMap.GetValue(baseX, baseY);
                float normalized = (value - minVal) * invRange;
                float inverted = 1f - normalized;
                buffer[localIdx] += inverted * weight;
            });
        }

        /// <summary>Mnoży wartości w working map przez fragment mapy bazowej z wagą</summary>
        /// <param name="baseMap">Mapa bazowa</param>
        /// <param name="weight">Mnożnik wartości z mapy bazowej</param>
        public void MultiplyFrom(InfluenceGrid baseMap, float weight = 1f)
        {
            if (baseMap == null) return;
            if (coveredCache == null || coveredCache.Length < CellCount) coveredCache = new bool[CellCount];
            else Array.Clear(coveredCache, 0, CellCount);
            IterateOverlap(baseMap, (localIdx, baseX, baseY) =>
            {
                float value = baseMap.GetValue(baseX, baseY);
                buffer[localIdx] *= value * weight;
                coveredCache[localIdx] = true;
            });
            for (int i = 0; i < CellCount; i++)
            {
                if (!coveredCache[i]) buffer[i] = 0f;
            }
        }

        /// <summary>Normalizuje wartości w buforze do zakresu [0, 1]</summary>
        public void Normalize()
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < CellCount; i++)
            {
                if (buffer[i] < min) min = buffer[i];
                if (buffer[i] > max) max = buffer[i];
            }
            float range = max - min;
            if (Mathf.Abs(range) < InfluenceMapConstants.InfluenceValueEpsilon) return;
            float invRange = 1f / range;
            for (int i = 0; i < CellCount; i++) buffer[i] = (buffer[i] - min) * invRange;
        }

        /// <summary>Znajduje komórkę z najwyższą wartością w working map</summary>
        /// <returns>Komórka z najwyższą wartością</returns>
        public InfluenceCell GetHighestCell()
        {
            int bestIdx = 0;
            float bestVal = buffer[0];
            for (int i = 1; i < CellCount; i++)
            {
                if (buffer[i] > bestVal)
                {
                    bestVal = buffer[i];
                    bestIdx = i;
                }
            }
            int x = bestIdx % width;
            int y = bestIdx / width;
            return new InfluenceCell(x, y, bestVal);
        }

        /// <summary>Znajduje komórkę z najniższą wartością w working map</summary>
        /// <returns>Komórka z najniższą wartością</returns>
        public InfluenceCell GetLowestCell()
        {
            int bestIdx = 0;
            float bestVal = buffer[0];

            for (int i = 1; i < CellCount; i++)
            {
                if (buffer[i] < bestVal)
                {
                    bestVal = buffer[i];
                    bestIdx = i;
                }
            }

            int x = bestIdx % width;
            int y = bestIdx / width;
            return new InfluenceCell(x, y, bestVal);
        }

        /// <summary>Konwertuje komórkę z najwyższą wartością na pozycję w świecie</summary>
        /// <returns>Pozycja środka komórki o najwyższej wartości w świecie</returns>
        public Vector3 GetHighestLocation()
        {
            InfluenceCell cell = GetHighestCell();
            return LocalToWorld(cell.X, cell.Y);
        }

        /// <summary>Konwertuje komórkę z najniższą wartością na pozycję w świecie</summary>
        /// <returns>Pozycja środka komórki o najniższej wartości w świecie</returns>
        public Vector3 GetLowestLocation()
        {
            InfluenceCell cell = GetLowestCell();
            return LocalToWorld(cell.X, cell.Y);
        }

        /// <summary>Pobiera wartość w lokalnych koordynatach working map</summary>
        /// <param name="localX">Kolumna w working map</param>
        /// <param name="localY">Wiersz w working map</param>
        /// <returns>Wartość lub 0 jeśli poza granicami</returns>
        public float GetValue(int localX, int localY)
        {
            if (localX < 0 || localX >= width || localY < 0 || localY >= height) return 0f;
            return buffer[localY * width + localX];
        }

        /// <summary>Pobiera wartość na podstawie pozycji w świecie</summary>
        /// <param name="worldPosition">Pozycja w świecie</param>
        /// <returns>Wartość lub 0 jeśli poza zasięgiem working map</returns>
        public float GetValue(Vector3 worldPosition)
        {
            WorldToLocal(worldPosition, out int lx, out int ly);
            return GetValue(lx, ly);
        }

        /// <summary>Konwertuje lokalne koordynaty working map na pozycję w świecie</summary>
        /// <param name="localX">Kolumna w working map</param>
        /// <param name="localY">Wiersz w working map</param>
        /// <returns>Pozycja środka komórki w świecie</returns>
        public Vector3 LocalToWorld(int localX, int localY)
        {
            float halfCell = cellSize * 0.5f;
            float worldX = origin.x + localX * cellSize + halfCell;
            float worldZ = origin.z + localY * cellSize + halfCell;
            return new Vector3(worldX, center.y, worldZ);
        }

        /// <summary>Konwertuje pozycję w świecie na lokalne koordynaty working map</summary>
        /// <param name="worldPosition">Pozycja w świecie</param>
        /// <param name="localX">Kolumna w working map</param>
        /// <param name="localY">Wiersz w working map</param>
        public void WorldToLocal(Vector3 worldPosition, out int localX, out int localY)
        {
            localX = Mathf.FloorToInt((worldPosition.x - origin.x) / cellSize);
            localY = Mathf.FloorToInt((worldPosition.z - origin.z) / cellSize);
        }

        /// <summary>Mnoży wartość w komórce przez mnożnik</summary>
        /// <param name="localX">Kolumna w working map</param>
        /// <param name="localY">Wiersz w working map</param>
        /// <param name="multiplier">Mnożnik wartości</param>
        public void MultiplyValue(int localX, int localY, float multiplier)
        {
            if (localX < 0 || localX >= width || localY < 0 || localY >= height) return;
            buffer[localY * width + localX] *= multiplier;
        }

        /// <summary>Ustawia wartość w komórce</summary>
        public void SetValue(int localX, int localY, float value)
        {
            if (localX < 0 || localX >= width || localY < 0 || localY >= height) return;
            buffer[localY * width + localX] = value;
        }

        /// <summary>Delegat wywoływany dla każdej komórki w overlappie working map i mapy bazowej</summary>
        /// <param name="localIndex">Indeks w buforze working map</param>
        /// <param name="baseX">Kolumna w mapie bazowej</param>
        /// <param name="baseY">Wiersz w mapie bazowej</param>
        private delegate void OverlapAction(int localIndex, int baseX, int baseY);

        /// <summary>Iteruje po komórkach working map, mapuje je na koordynaty mapy bazowej i wywołuje akcję dla komórek które mieszczą się w obu siatkach</summary>
        private void IterateOverlap(InfluenceGrid baseMap, OverlapAction action)
        {
            for (int ly = 0; ly < height; ly++)
            {
                for (int lx = 0; lx < width; lx++)
                {
                    Vector3 worldPos = LocalToWorld(lx, ly);
                    Vector2Int baseCoords = baseMap.WorldToGrid(worldPos);
                    if (!baseMap.IsInBounds(baseCoords.x, baseCoords.y)) continue;
                    int localIdx = ly * width + lx;
                    action(localIdx, baseCoords.x, baseCoords.y);
                }
            }
        }

        /// <summary>Oblicza zakres wartości na fragmencie mapy bazowej pokrywającym working map</summary>
        private void ComputeRange(InfluenceGrid baseMap, out float minVal, out float maxVal)
        {
            minVal = float.MaxValue;
            maxVal = float.MinValue;
            for (int ly = 0; ly < height; ly++)
            {
                for (int lx = 0; lx < width; lx++)
                {
                    Vector3 worldPos = LocalToWorld(lx, ly);
                    Vector2Int baseCoords = baseMap.WorldToGrid(worldPos);
                    if (!baseMap.IsInBounds(baseCoords.x, baseCoords.y)) continue;
                    float v = baseMap.GetValue(baseCoords.x, baseCoords.y);
                    if (v < minVal) minVal = v;
                    if (v > maxVal) maxVal = v;
                }
            }
            if (minVal > maxVal)
            {
                minVal = 0f;
                maxVal = 0f;
            }
        }
    }
}