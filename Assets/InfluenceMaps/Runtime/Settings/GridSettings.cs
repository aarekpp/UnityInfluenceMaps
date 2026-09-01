using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Konfiguracja wymiarów i pozycji siatki wpływów</summary>
    [Serializable]
    public class GridSettings
    {
        /// <summary>Szerokość w osi X</summary>
        [Header("Wymiar siatki")]
        [Min(InfluenceMapConstants.MinWorldSize)]
        [SerializeField]
        private float worldSizeX = InfluenceMapConstants.DefaultWorldWidth;

        /// <summary>Szerokość w osi Z</summary>
        [Min(InfluenceMapConstants.MinWorldSize)]
        [SerializeField]
        private float worldSizeZ = InfluenceMapConstants.DefaultWorldHeight;

        /// <summary>Liczba komórek w osi X</summary>
        [Header("Liczba komórek w osi X")]
        [Min(InfluenceMapConstants.MinGridDimension)]
        [SerializeField]
        private int cellsX = InfluenceMapConstants.DefaultCellsX;

        /// <summary>Tryb położenia siatki w świecie</summary>
        [Header("Pozycja w świecie")]
        [SerializeField]
        private GridOriginMode originMode = GridOriginMode.Manual;

        /// <summary>Lewy dolny róg siatki w przestrzeni świata domyślnie w punkcie (0,0,0)</summary>
        [SerializeField]
        private Vector3 origin = Vector3.zero;

        /// <summary>Rozmiar komórki wyliczany z worldSizeX / cellsX</summary>
        public float CellSize
        {
            get
            {
                int safeX = Mathf.Max(cellsX, InfluenceMapConstants.MinGridDimension);
                float safeWorldX = Mathf.Max(worldSizeX, InfluenceMapConstants.MinWorldSize);
                return Mathf.Max(safeWorldX / safeX, InfluenceMapConstants.MinCellSize);
            }
        }

        /// <summary>Szerokość siatki</summary>
        public int Width => Mathf.Max(cellsX, InfluenceMapConstants.MinGridDimension);

        /// <summary>Wysokość siatki</summary>
        public int Height => Mathf.Max(Mathf.CeilToInt(Mathf.Max(worldSizeZ, InfluenceMapConstants.MinWorldSize) / CellSize), InfluenceMapConstants.MinGridDimension);

        /// <summary>Szerokość obszaru w jednostkach świata</summary>
        public float WorldSizeX
        {
            get => worldSizeX;
            set => worldSizeX = Mathf.Max(InfluenceMapConstants.MinWorldSize, value);
        }

        /// <summary>Wysokość obszaru w jednostkach świata</summary>
        public float WorldSizeZ
        {
            get => worldSizeZ;
            set => worldSizeZ = Mathf.Max(InfluenceMapConstants.MinWorldSize, value);
        }

        /// <summary>Liczba komórek w osi X</summary>
        public int CellsX
        {
            get => cellsX;
            set => cellsX = Mathf.Max(InfluenceMapConstants.MinGridDimension, value);
        }

        /// <summary>Tryb wyznaczania origin</summary>
        public GridOriginMode OriginMode
        {
            get => originMode;
            set => originMode = value;
        }

        /// <summary>Lewy dolny róg siatki</summary>
        public Vector3 Origin
        {
            get => origin;
            set => origin = value;
        }

        /// <summary>Rzeczywista liczba komórek</summary>
        public int TotalCells => Width * Height;

        /// <summary>Szerokość siatki w jednostkach świata</summary>
        public float WorldWidth => Width * CellSize;

        /// <summary>Wysokość siatki w jednostkach świata</summary>
        public float WorldHeight => Height * CellSize;

        /// <summary>Centrum siatki w świecie</summary>
        public Vector3 WorldCenter => origin + new Vector3(WorldWidth * 0.5f, 0f, WorldHeight * 0.5f);

        /// <summary>Obliczanie pozycji origin siatki na podstawie wybranego trybu</summary>
        /// <param name="mapTransform">Transform obiektu z InfluenceMap dla trybu MapPosition</param>
        /// <param name="anchorBounds">Bounds obiektu kotwicy dla trybu AnchorObject</param>
        /// <returns>Obliczona pozycja origin</returns>
        public Vector3 ComputeOrigin(Transform mapTransform = null, Bounds? anchorBounds = null)
        {
            switch (originMode)
            {
                case GridOriginMode.AnchorObject:
                    if (anchorBounds.HasValue)
                    {
                        Bounds b = anchorBounds.Value;
                        return new Vector3(b.min.x, 0f, b.min.z);
                    }
                    Debug.LogWarning("[InfluenceMaps] GridSettings: AnchorObject jest null. Fallback do Manual origin");
                    return origin;

                case GridOriginMode.MapPosition:
                    if (mapTransform != null)
                    {
                        Vector3 pos = mapTransform.position;
                        return new Vector3(pos.x - WorldWidth * 0.5f, 0f, pos.z - WorldHeight * 0.5f);
                    }
                    return origin;

                case GridOriginMode.Manual:
                default:
                    return origin;
            }
        }

        /// <summary>Konstruktor domyślny</summary>
        public GridSettings() { }

        /// <summary>Konstruktor z parametrami</summary>
        /// <param name="worldSizeX">Szerokość obszaru w osi X w jednostkach świata</param>
        /// <param name="worldSizeZ">Szerokość obszaru w osi Z w jednostkach świata</param>
        /// <param name="cellsX">Liczba komórek w osi X (rozmiar komórki to worldSizeX / cellsX)</param>
        /// <param name="origin">Lewy dolny róg siatki w świecie</param>
        public GridSettings(float worldSizeX, float worldSizeZ, int cellsX, Vector3 origin = default)
        {
            WorldSizeX = worldSizeX;
            WorldSizeZ = worldSizeZ;
            CellsX = cellsX;
            this.origin = origin;
        }

        /// <summary>Wymuszenie zastosowania minimalnych wartości</summary>
        public void Validate()
        {
            worldSizeX = Mathf.Max(InfluenceMapConstants.MinWorldSize, worldSizeX);
            worldSizeZ = Mathf.Max(InfluenceMapConstants.MinWorldSize, worldSizeZ);
            cellsX = Mathf.Max(InfluenceMapConstants.MinGridDimension, cellsX);
        }
    }
}
