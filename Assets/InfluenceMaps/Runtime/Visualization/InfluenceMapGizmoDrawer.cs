using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InfluenceMaps
{
    /// <summary>Rysowanie wizualizacji siatki wpływów w Scene View za pomocą Gizmos</summary>
    public static class InfluenceMapGizmoDrawer
    {
        /// <summary>Próg wartości poniżej którego komórka nie jest rysowana</summary>
        private static readonly float DrawThreshold = InfluenceMapConstants.InfluenceValueEpsilon;

        /// <summary>Rysuje pełną wizualizację siatki wpływów</summary>
        /// <param name="map">Mapa do wizualizacji</param>
        public static void DrawGizmos(InfluenceMap map)
        {
            if (map == null || !map.IsInitialized || map.Grid == null) return;
            VisualizationSettings settings = map.ActiveVisualization;
            if (settings == null || !settings.Enabled) return;
            InfluenceGrid grid = map.Grid;
            Vector3 cellSize = new Vector3(grid.CellSize, 0.01f, grid.CellSize);
            settings.UpdateAutoRange(grid);
            DrawFilledCells(grid, settings, cellSize);
            if (settings.ShowGridLines) DrawGridLines(grid);

#if UNITY_EDITOR
            if (settings.ShowValues) DrawCellValues(grid, settings);
#endif
        }

        /// <summary>Rysuje tylko granice siatki</summary>
        /// <param name="map">Mapa do wizualizacji</param>
        public static void DrawGridBounds(InfluenceMap map)
        {
            if (map == null || !map.IsInitialized || map.Grid == null) return;
            VisualizationSettings settings = map.ActiveVisualization;
            if (settings == null || !settings.Enabled) return;
            InfluenceGrid grid = map.Grid;
            float worldWidth = grid.Width * grid.CellSize;
            float worldHeight = grid.Height * grid.CellSize;
            Vector3 center = grid.Origin + new Vector3(worldWidth * 0.5f, 0f, worldHeight * 0.5f);
            Vector3 size = new Vector3(worldWidth, 0.01f, worldHeight);
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            Gizmos.DrawWireCube(center, size);
        }

        /// <summary>Rysuje kolorowe prostokąty dla komórek z wartością powyżej progu</summary>
        private static void DrawFilledCells(InfluenceGrid grid, VisualizationSettings settings, Vector3 cellSize)
        {
            ReadOnlySpan<float> values = grid.Values;
            for (int i = 0; i < grid.CellCount; i++)
            {
                float value = values[i];
                if (Mathf.Abs(value) < DrawThreshold) continue;
                Vector2Int coords = grid.GetCoordinates(i);
                Vector3 worldPos = grid.GridToWorld(coords.x, coords.y);
                Gizmos.color = settings.GetColor(value);
                Gizmos.DrawCube(worldPos, cellSize);
            }
        }

        /// <summary>Rysuje linie siatki jako granice między komórkami</summary>
        private static void DrawGridLines(InfluenceGrid grid)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.15f);
            float originX = grid.Origin.x;
            float originZ = grid.Origin.z;
            float maxX = originX + grid.Width * grid.CellSize;
            float maxZ = originZ + grid.Height * grid.CellSize;
            float y = grid.Origin.y;
            for (int x = 0; x <= grid.Width; x++)
            {
                float worldX = originX + x * grid.CellSize;
                Gizmos.DrawLine(new Vector3(worldX, y, originZ), new Vector3(worldX, y, maxZ));
            }
            for (int z = 0; z <= grid.Height; z++)
            {
                float worldZ = originZ + z * grid.CellSize;
                Gizmos.DrawLine(new Vector3(originX, y, worldZ), new Vector3(maxX, y, worldZ));
            }
        }

#if UNITY_EDITOR
        /// <summary>Wyświetla wartości liczbowe w środku każdej komórki</summary>
        private static void DrawCellValues(InfluenceGrid grid, VisualizationSettings settings)
        {
            GUIStyle style = new GUIStyle { fontSize = 9, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }};
            ReadOnlySpan<float> values = grid.Values;
            for (int i = 0; i < grid.CellCount; i++)
            {
                float value = values[i];
                if (Mathf.Abs(value) < DrawThreshold) continue;
                Vector2Int coords = grid.GetCoordinates(i);
                Vector3 worldPos = grid.GridToWorld(coords.x, coords.y);
                worldPos.y += 0.1f;
                Handles.Label(worldPos, value.ToString("F2"), style);
            }
        }
#endif
    }
}
