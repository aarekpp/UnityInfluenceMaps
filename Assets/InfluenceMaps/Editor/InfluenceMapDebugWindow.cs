#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InfluenceMaps.Editor
{
    /// <summary>Okno edytora do debugowania map wpływów</summary>
    public class InfluenceMapDebugWindow : EditorWindow
    {
        /// <summary>stałe wartości</summary>
        private const float PANEL_MIN_WIDTH = 590f;
        private const float DETAILS_LEFT_COL_WIDTH = 290f;
        private const float SECTION_SPACING = 10f;
        private const float HEATMAP_MAX_SIZE = 320f;

        /// <summary>Pozycja scrolla</summary>
        private Vector2 scrollPosition;

        /// <summary>Aktualnie wybrana mapa do szczegółowego podglądu</summary>
        private InfluenceMap selectedMap;

        /// <summary>Pozycja w świecie do odpytywania wartości</summary>
        private Vector3 queryPosition;

        /// <summary>Czy śledzić kliknięcie w Scene View</summary>
        private bool trackSceneClicks = true;

        /// <summary>Czy rysować markery MAX/MIN wybranej mapy w Scene View</summary>
        private bool showExtremesInScene = false;

        /// <summary>Stany sekcji</summary>
        private bool showMapList = true;
        private bool showSelectedMapDetails = true;
        private bool showHeatmap = true;
        private bool showPointQuery = true;
        private bool showAllMapsAtPoint = true;

        /// <summary>Tekstura heatmapy</summary>
        private Texture2D heatmapTex;
        private Color[] heatmapBuf;
        private float lastHeatMin, lastHeatMax;

        /// <summary>Otwiera okno z menu edytora</summary>
        [MenuItem("Window/Influence Maps/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<InfluenceMapDebugWindow>();
            window.titleContent = new GUIContent("Influence Maps Debug");
            window.minSize = new Vector2(PANEL_MIN_WIDTH, 400f);
        }

        /// <summary>Podpięcie rysowania w widoku sceny po włączeniu okna edytora</summary>
        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        /// <summary>Odpięcie rysowania w widoku sceny oraz zwolnienie tekstury mapy ciepła po wyłączeniu okna</summary>
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (heatmapTex != null)
            {
                DestroyImmediate(heatmapTex);
                heatmapTex = null;
            }
        }

        /// <summary>Odświeżanie okna co klatkę gdy gra jest uruchomiona</summary>
        private void Update()
        {
            if (Application.isPlaying) Repaint();
        }

        /// <summary>Obsługa kliknięcia w Scene View do wyboru punktu zapytania oraz wizualizacji</summary>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!Application.isPlaying) return;
            Event e = Event.current;
            if (trackSceneClicks && e.type == EventType.MouseDown && e.button == 1 && e.shift)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float distance))
                {
                    queryPosition = ray.GetPoint(distance);
                    Repaint();
                }
                e.Use();
            }
            if (queryPosition != Vector3.zero)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(queryPosition, Vector3.up, 0.5f);
                Handles.DrawLine(queryPosition, queryPosition + Vector3.up * 2f);
                Handles.Label(queryPosition + Vector3.up * 2.2f, "Query Point", EditorStyles.boldLabel);
            }
            if (selectedMap != null && selectedMap.IsInitialized && selectedMap.Grid != null)
            {
                InfluenceGrid grid = selectedMap.Grid;
                Vector2Int cell = grid.WorldToGrid(queryPosition);
                if (grid.IsInBounds(cell.x, cell.y))
                {
                    Vector3 ctr = grid.GridToWorld(cell.x, cell.y);
                    float hs = grid.CellSize * 0.5f;
                    Vector3[] rect =
                    {
                        ctr + new Vector3(-hs, 0f, -hs),
                        ctr + new Vector3( hs, 0f, -hs),
                        ctr + new Vector3( hs, 0f,  hs),
                        ctr + new Vector3(-hs, 0f,  hs),
                        ctr + new Vector3(-hs, 0f, -hs)
                    };
                    Handles.color = Color.cyan;
                    Handles.DrawAAPolyLine(3f, rect);
                    Vector3 g = InfluenceQuery.GetInfluenceGradient(grid, queryPosition);
                    if (g.sqrMagnitude > 1e-6f)
                    {
                        Vector3 dir = g.normalized * grid.CellSize * 1.5f;
                        Handles.color = Color.green;
                        Handles.DrawAAPolyLine(3f, new[] { ctr, ctr + dir });
                        Handles.ConeHandleCap(0, ctr + dir, Quaternion.LookRotation(dir), grid.CellSize * 0.3f, EventType.Repaint);
                    }
                }
                if (showExtremesInScene && grid.CellCount > 0)
                {
                    GetExtremes(grid, out Vector3 maxW, out Vector3 minW, out _, out _);
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(maxW, Vector3.up, grid.CellSize * 0.5f);
                    Handles.Label(maxW + Vector3.up * 0.6f, "MAX");
                    Handles.color = Color.red;
                    Handles.DrawWireDisc(minW, Vector3.up, grid.CellSize * 0.5f);
                    Handles.Label(minW + Vector3.up * 0.6f, "MIN");
                }
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawHeader();
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Uruchom grę aby zobaczyć dane map wpływów", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }
            if (InfluenceMapManager.Instance == null)
            {
                EditorGUILayout.HelpBox("Brak InfluenceMapManager na scenie", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }
            DrawMapListSection();
            GUILayout.Space(SECTION_SPACING);
            DrawSelectedMapSection();
            GUILayout.Space(SECTION_SPACING);
            DrawPointQuerySection();
            GUILayout.Space(SECTION_SPACING);
            DrawAllMapsAtPointSection();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>Nagłówek okna</summary>
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Influence Maps Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            trackSceneClicks = EditorGUILayout.ToggleLeft(new GUIContent("Śledzenie kliknięć (Shift+PPM)", "Shift + prawy przycisk myszy w Scene View ustawia punkt zapytania"), trackSceneClicks);
            showExtremesInScene = EditorGUILayout.ToggleLeft(new GUIContent("Markery MAX/MIN w Scene View", "Rysuje pozycje komórek o najwyższej i najniższej wartości wybranej mapy"), showExtremesInScene);
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Wymuś aktualizację", GUILayout.Width(150))) InfluenceMapManager.Instance?.ForceUpdateAll();
            if (GUILayout.Button("Wyczyść wszystkie", GUILayout.Width(150))) InfluenceMapManager.Instance?.ClearAll();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
            DrawSeparator();
        }

        /// <summary>Lista zarejestrowanych map</summary>
        private void DrawMapListSection()
        {
            showMapList = EditorGUILayout.Foldout(showMapList, "Zarejestrowane mapy", true, EditorStyles.foldoutHeader);
            if (!showMapList) return;
            EditorGUI.indentLevel++;
            IReadOnlyList<InfluenceMap> maps = InfluenceMapManager.Instance.Maps;
            if (maps.Count == 0)
            {
                EditorGUILayout.LabelField("Brak zarejestrowanych map");
                EditorGUI.indentLevel--;
                return;
            }
            for (int i = 0; i < maps.Count; i++)
            {
                InfluenceMap map = maps[i];
                if (map == null) continue;
                EditorGUILayout.BeginHorizontal();
                bool isSelected = selectedMap == map;
                GUIStyle style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                string status = map.IsInitialized ? "OK" : "---";
                string info = map.IsInitialized && map.Grid != null ? $"{map.Grid.Width}x{map.Grid.Height} | Źródła: {map.Sources.Count} | Przeszkody: {map.Obstacles.Count}" : "Niezainicjalizowana";
                if (GUILayout.Button($"{map.MapName} [{status}]", style, GUILayout.Width(180))) selectedMap = map;
                EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>Szczegóły wybranej mapy</summary>
        private void DrawSelectedMapSection()
        {
            showSelectedMapDetails = EditorGUILayout.Foldout(showSelectedMapDetails, "Szczegóły wybranej mapy", true, EditorStyles.foldoutHeader);
            if (!showSelectedMapDetails) return;
            if (selectedMap == null || !selectedMap.IsInitialized)
            {
                EditorGUILayout.LabelField("Wybierz mapę z listy powyżej");
                return;
            }
            InfluenceGrid grid = selectedMap.Grid;
            if (grid == null)
            {
                EditorGUILayout.LabelField("Siatka nie jest zainicjalizowana");
                return;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(DETAILS_LEFT_COL_WIDTH));
            DrawGridInfoColumn(grid);
            EditorGUILayout.Space(4);
            DrawValuesColumn(grid);
            EditorGUILayout.EndVertical();
            GUILayout.Space(12);
            EditorGUILayout.BeginVertical();
            DrawSourcesObstacles(selectedMap);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6);
            showHeatmap = EditorGUILayout.Foldout(showHeatmap, "Heatmapa", true);
            if (showHeatmap) DrawHeatmap(grid);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Zaznacz mapę", GUILayout.Width(100))) Selection.activeGameObject = selectedMap.gameObject;
        }

        /// <summary>Kolumna: informacje o siatce</summary>
        private void DrawGridInfoColumn(InfluenceGrid grid)
        {
            EditorGUILayout.LabelField("Siatka", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Wymiary: {grid.Width} x {grid.Height} ({grid.CellCount} komórek)");
            EditorGUILayout.LabelField($"  Rozmiar komórki: {grid.CellSize:F2}");
            EditorGUILayout.LabelField($"  Origin: {grid.Origin}");
        }

        /// <summary>Kolumna: statystyki wartości oraz skróty do punktów MIN/MAX</summary>
        private void DrawValuesColumn(InfluenceGrid grid)
        {
            EditorGUILayout.LabelField("Wartości", EditorStyles.boldLabel);
            ReadOnlySpan<float> values = grid.Values;
            float min = float.MaxValue, max = float.MinValue, sum = 0f;
            int nonZero = 0, maxI = 0, minI = 0;
            for (int i = 0; i < grid.CellCount; i++)
            {
                float v = values[i];
                if (v < min) { min = v; minI = i; }
                if (v > max) { max = v; maxI = i; }
                sum += v;
                if (Mathf.Abs(v) > InfluenceMapConstants.InfluenceValueEpsilon) nonZero++;
            }
            EditorGUILayout.LabelField($"  Min: {min:F4}  Max: {max:F4}  Avg: {sum / grid.CellCount:F4}");
            EditorGUILayout.LabelField($"  Niezerowe: {nonZero} / {grid.CellCount} ({100f * nonZero / grid.CellCount:F1}%)");
            int mx = maxI % grid.Width, my = maxI / grid.Width;
            int nx = minI % grid.Width, ny = minI / grid.Width;
            Vector3 maxWorld = grid.GridToWorld(mx, my);
            Vector3 minWorld = grid.GridToWorld(nx, ny);
            EditorGUILayout.LabelField($"  Max @ ({mx},{my})  świat ({maxWorld.x:F1}, {maxWorld.z:F1})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  Min @ ({nx},{ny})  świat ({minWorld.x:F1}, {minWorld.z:F1})", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("punktMAX", GUILayout.Width(80))) { queryPosition = maxWorld; SceneView.RepaintAll(); }
            if (GUILayout.Button("punktMIN", GUILayout.Width(80))) { queryPosition = minWorld; SceneView.RepaintAll(); }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Rysuje podgląd heatmapy mapy i obsługuje klik</summary>
        private void DrawHeatmap(InfluenceGrid grid)
        {
            if (grid.CellCount == 0) return;
            RebuildHeatmap(grid);
            int w = grid.Width, h = grid.Height;
            float aspect = (float)w / Mathf.Max(1, h);
            float drawW, drawH;
            if (aspect >= 1f)
            {
                drawW = HEATMAP_MAX_SIZE;
                drawH = HEATMAP_MAX_SIZE / aspect;
            }
            else
            {
                drawH = HEATMAP_MAX_SIZE;
                drawW = HEATMAP_MAX_SIZE * aspect;
            }
            Rect rect = GUILayoutUtility.GetRect(drawW, drawH, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(rect, heatmapTex, ScaleMode.StretchToFill, false);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(0, 0, 0, 0.4f));
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                Vector2 local = e.mousePosition - rect.position;
                int cx = Mathf.Clamp(Mathf.FloorToInt(local.x / rect.width * w), 0, w - 1);
                int cy = Mathf.Clamp(Mathf.FloorToInt((1f - local.y / rect.height) * h), 0, h - 1);
                queryPosition = grid.GridToWorld(cx, cy);
                e.Use();
                Repaint();
                SceneView.RepaintAll();
            }
            EditorGUILayout.LabelField($"  Zakres koloru: [{lastHeatMin:F3}, {lastHeatMax:F3}]  (klik = punkt zapytania)", EditorStyles.miniLabel);
        }

        /// <summary>Buduje/aktualizuje teksturę heatmapy z wartości siatki</summary>
        private void RebuildHeatmap(InfluenceGrid grid)
        {
            int w = grid.Width, h = grid.Height;
            if (heatmapTex == null || heatmapTex.width != w || heatmapTex.height != h)
            {
                if (heatmapTex != null) DestroyImmediate(heatmapTex);
                heatmapTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                heatmapBuf = new Color[w * h];
            }
            ReadOnlySpan<float> values = grid.Values;
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < values.Length; i++)
            {
                float v = values[i];
                if (v < min) min = v;
                if (v > max) max = v;
            }
            lastHeatMin = min;
            lastHeatMax = max;
            for (int i = 0; i < values.Length; i++) heatmapBuf[i] = ValueToColor(values[i], min, max);
            heatmapTex.SetPixels(heatmapBuf);
            heatmapTex.Apply(false);
        }

        /// <summary>Mapuje wartość na kolor: rozbieżnie wokół zera dla map ze znakiem, sekwencyjnie dla dodatnich</summary>
        private static Color ValueToColor(float v, float min, float max)
        {
            const float eps = 1e-6f;
            Color dark = new Color(0.07f, 0.07f, 0.10f);
            if (min < -eps && max > eps)
            {
                float m = Mathf.Max(-min, max);
                float t = Mathf.Clamp(v / m, -1f, 1f);
                return t >= 0f ? Color.Lerp(dark, new Color(0.95f, 0.25f, 0.15f), t) : Color.Lerp(dark, new Color(0.15f, 0.45f, 0.95f), -t);
            }
            float range = max - min;
            float s = range > eps ? Mathf.Clamp01((v - min) / range) : 0f;
            return s < 0.5f ? Color.Lerp(new Color(0.05f, 0.02f, 0.15f), new Color(0.85f, 0.20f, 0.30f), s * 2f) : Color.Lerp(new Color(0.85f, 0.20f, 0.30f), new Color(1f, 0.95f, 0.40f), (s - 0.5f) * 2f);
        }

        /// <summary>Lista źródeł i przeszkód wybranej mapy z możliwością zaznaczenia w hierarchii</summary>
        private void DrawSourcesObstacles(InfluenceMap map)
        {
            EditorGUILayout.LabelField($"Źródła ({map.Sources.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < map.Sources.Count; i++)
            {
                var s = map.Sources[i];
                if (s == null) continue;
                string off = s.IsActive ? "" : "  [off]";
                DrawElementRow($"  Int {s.Intensity:F2}  R {s.Radius:F1}  ({s.Position.x:F1}, {s.Position.z:F1}){off}", s as MonoBehaviour);
            }
            EditorGUILayout.LabelField($"Przeszkody ({map.Obstacles.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < map.Obstacles.Count; i++)
            {
                var o = map.Obstacles[i];
                if (o == null) continue;
                string off = o.IsActive ? "" : "  [off]";
                DrawElementRow($"  Block {o.BlockingFactor:F2}  ({o.Position.x:F1}, {o.Position.z:F1}){off}", o as MonoBehaviour);
            }
        }

        /// <summary>Wiersz: opis elementu o szerokości tekstu, mały odstęp i przycisk Zaznacz dosunięty do tekstu</summary>
        private static void DrawElementRow(string text, MonoBehaviour behaviour)
        {
            EditorGUILayout.BeginHorizontal();
            GUIContent content = new GUIContent(text);
            float textWidth = EditorStyles.miniLabel.CalcSize(content).x;
            EditorGUILayout.LabelField(content, EditorStyles.miniLabel, GUILayout.Width(textWidth));
            GUILayout.Space(8f);
            DrawSelectButton(behaviour);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Przycisk zaznaczający obiekt w hierarchii (wyszarzony gdy brak MonoBehaviour)</summary>
        private static void DrawSelectButton(MonoBehaviour behaviour)
        {
            using (new EditorGUI.DisabledScope(behaviour == null))
            {
                if (GUILayout.Button("Zaznacz", GUILayout.Width(80)) && behaviour != null) Selection.activeGameObject = behaviour.gameObject;
            }
        }

        /// <summary>Zwraca pozycje świata komórek o najwyższej i najniższej wartości</summary>
        private static void GetExtremes(InfluenceGrid grid, out Vector3 maxWorld, out Vector3 minWorld, out float maxV, out float minV)
        {
            ReadOnlySpan<float> values = grid.Values;
            int maxI = 0, minI = 0;
            maxV = values[0];
            minV = values[0];
            for (int i = 1; i < grid.CellCount; i++)
            {
                float v = values[i];
                if (v > maxV) { maxV = v; maxI = i; }
                if (v < minV) { minV = v; minI = i; }
            }
            maxWorld = grid.GridToWorld(maxI % grid.Width, maxI / grid.Width);
            minWorld = grid.GridToWorld(minI % grid.Width, minI / grid.Width);
        }

        /// <summary>Zapytanie o wartość w punkcie</summary>
        private void DrawPointQuerySection()
        {
            showPointQuery = EditorGUILayout.Foldout(showPointQuery, "Zapytanie o punkt", true, EditorStyles.foldoutHeader);
            if (!showPointQuery) return;
            EditorGUI.indentLevel++;
            queryPosition = EditorGUILayout.Vector3Field("Pozycja", queryPosition);
            EditorGUILayout.LabelField("Shift + PPM w Scene View aby ustawić punkt", EditorStyles.miniLabel);
            if (selectedMap != null && selectedMap.IsInitialized && selectedMap.Grid != null)
            {
                InfluenceGrid grid = selectedMap.Grid;
                Vector2Int cell = grid.WorldToGrid(queryPosition);
                float value = grid.GetValue(cell.x, cell.y);
                bool inBounds = grid.IsInBounds(cell.x, cell.y);
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"Mapa: {selectedMap.MapName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  Komórka: ({cell.x}, {cell.y}) {(inBounds ? "" : "[poza siatką]")}");
                EditorGUILayout.LabelField($"  Wartość: {value:F4}");
                Vector3 gradient = InfluenceQuery.GetInfluenceGradient(grid, queryPosition);
                EditorGUILayout.LabelField($"  Gradient: ({gradient.x:F3}, {gradient.z:F3})");
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>Wartości ze wszystkich map w punkcie zapytania</summary>
        private void DrawAllMapsAtPointSection()
        {
            showAllMapsAtPoint = EditorGUILayout.Foldout(showAllMapsAtPoint, "Wszystkie mapy w punkcie", true, EditorStyles.foldoutHeader);
            if (!showAllMapsAtPoint) return;
            EditorGUI.indentLevel++;
            if (InfluenceMapManager.Instance == null)
            {
                EditorGUI.indentLevel--;
                return;
            }
            IReadOnlyList<InfluenceMap> maps = InfluenceMapManager.Instance.Maps;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mapa", EditorStyles.boldLabel, GUILayout.Width(160));
            EditorGUILayout.LabelField("Wartość", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Komórka", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("W siatce", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < maps.Count; i++)
            {
                InfluenceMap map = maps[i];
                if (map == null || !map.IsInitialized || map.Grid == null) continue;
                InfluenceGrid grid = map.Grid;
                Vector2Int cell = grid.WorldToGrid(queryPosition);
                float value = grid.GetValue(cell.x, cell.y);
                bool inBounds = grid.IsInBounds(cell.x, cell.y);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(map.MapName, GUILayout.Width(160));
                EditorGUILayout.LabelField($"{value:F4}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"({cell.x},{cell.y})", GUILayout.Width(80));
                EditorGUILayout.LabelField(inBounds ? "Tak" : "Nie", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>Rysuje poziomą linię separatora</summary>
        private static void DrawSeparator()
        {
            EditorGUILayout.Space(2);
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            rect.height = 1f;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(2);
        }
    }
}
#endif
