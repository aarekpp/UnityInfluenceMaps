using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Renderuje warstwy map jako kolorowe tekstury na MeshRenderer. Obsługuje wiele warstw z osobnymi gradientami i przezroczystością</summary>
    [AddComponentMenu("Influence Maps/Influence Map Visualizer")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class InfluenceMapVisualizer : MonoBehaviour
    {
        /// <summary>Warstwy wizualizacji</summary>
        [Header("Warstwy wizualizacji")]
        [Tooltip("Lista warstw — każda warstwa wyświetla jedną mapę z własnym gradientem")]
        [SerializeField]
        private List<MapVisualizationLayer> layers = new List<MapVisualizationLayer>();

        /// <summary>Czy wizualizacja jest włączona</summary>
        [Header("Ustawienia")]
        [Tooltip("Włącz lub wyłącz całą wizualizację")]
        [SerializeField]
        private bool visualizationEnabled = true;

        /// <summary>Próg poniżej którego piksel jest przezroczysty</summary>
        [Tooltip("Wartości wpływu poniżej tego progu nie są rysowane")]
        [Min(0f)]
        [SerializeField]
        private float drawThreshold = InfluenceMapConstants.InfluenceValueEpsilon;

        /// <summary>Pokaż wartości liczbowe w oknie sceny (tylko edytor)</summary>
        [Header("Podgląd w scenie (tylko edytor)")]
        [Tooltip("Rysuj wartości liczbowe w oknie sceny")]
        [SerializeField]
        private bool showValues = false;

        /// <summary>Pokaż linie siatki w oknie sceny (tylko edytor)</summary>
        [Tooltip("Rysuj siatkę (linie komórek) w oknie sceny")]
        [SerializeField]
        private bool showGridLines = false;

        /// <summary>Operacja łączenia wartości z wielu map dla wyświetlanej liczby</summary>
        [Tooltip("Jak połączyć wartości z wielu map w jedną wyświetlaną liczbę")]
        [SerializeField]
        private CombineOperation valueCombineOperation = CombineOperation.Add;

        /// <summary>Kolor linii siatki</summary>
        [Tooltip("Kolor linii siatki w oknie sceny")]
        [SerializeField]
        private Color gridLineColor = new Color(1f, 1f, 1f, 0.25f);

        /// <summary>Tekstura renderowana na quad</summary>
        private Texture2D texture;

        /// <summary>Materiał z teksturą</summary>
        private Material material;

        /// <summary>MeshRenderer do renderowania</summary>
        private MeshRenderer meshRenderer;

        /// <summary>Referencja do pierwszej gotowej warstwy</summary>
        private InfluenceGrid referenceGrid;

        /// <summary>Czy subskrybowano eventy map</summary>
        private bool isSubscribed;

        /// <summary>Inicjalizacja komponentów renderowania</summary>
        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == null) meshFilter.sharedMesh = CreateQuadMesh();
            material = new Material(Shader.Find("Unlit/Transparent"));
            meshRenderer.material = material;
            meshRenderer.enabled = visualizationEnabled;
        }

        /// <summary>Subskrypcja eventów map</summary>
        private void OnEnable()
        {
            SubscribeToMaps();
        }

        /// <summary>Odsubskrybowanie eventów map</summary>
        private void OnDisable()
        {
            UnsubscribeFromMaps();
        }

        /// <summary>Czyszczenie zasobów</summary>
        private void OnDestroy()
        {
            if (texture != null) Destroy(texture);
            if (material != null) Destroy(material);
        }

        /// <summary>Subskrybuje OnMapUpdated ze wszystkich warstw</summary>
        private void SubscribeToMaps()
        {
            if (isSubscribed) return;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i]?.Map != null) layers[i].Map.OnMapUpdated += OnMapUpdated;
            }
            isSubscribed = true;
        }

        /// <summary>Odsubskrybowuje ze wszystkich map</summary>
        private void UnsubscribeFromMaps()
        {
            if (!isSubscribed) return;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i]?.Map != null) layers[i].Map.OnMapUpdated -= OnMapUpdated;
            }
            isSubscribed = false;
        }

        /// <summary>Callback wywoływany po aktualizacji pipeline dowolnej mapy</summary>
        private void OnMapUpdated(InfluenceMap map)
        {
            if (!visualizationEnabled) return;
            UpdateTexture();
            UpdateQuadTransform();
        }

        /// <summary>Aktualizuje teksturę na podstawie aktywnych warstw</summary>
        private void UpdateTexture()
        {
            referenceGrid = null;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].IsReady)
                {
                    referenceGrid = layers[i].Map.Grid;
                    break;
                }
            }
            if (referenceGrid == null)
            {
                if (meshRenderer != null) meshRenderer.enabled = false;
                return;
            }

            int width = referenceGrid.Width;
            int height = referenceGrid.Height;
            if (texture == null || texture.width != width || texture.height != height)
            {
                if (texture != null) Destroy(texture);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                material.mainTexture = texture;
            }

            Color[] pixels = new Color[width * height];
            Color clearColor = new Color(0f, 0f, 0f, 0f);
            Array.Fill(pixels, clearColor);
            for (int layerIdx = 0; layerIdx < layers.Count; layerIdx++)
            {
                MapVisualizationLayer layer = layers[layerIdx];
                if (layer == null || !layer.IsReady) continue;
                InfluenceGrid grid = layer.Map.Grid;
                if (grid.Width != width || grid.Height != height) continue;
                ComputeDynamicRange(grid, out float rangeMin, out float rangeMax);
                ReadOnlySpan<float> values = grid.Values;
                for (int i = 0; i < grid.CellCount; i++)
                {
                    float value = values[i];
                    if (Mathf.Abs(value) < drawThreshold) continue;
                    Color layerColor = layer.GetColor(value, rangeMin, rangeMax);
                    pixels[i] = BlendColors(pixels[i], layerColor);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            if (meshRenderer != null) meshRenderer.enabled = true;
        }

        /// <summary>Ustawia pozycję i skalę quada na podstawie siatki referencyjnej</summary>
        private void UpdateQuadTransform()
        {
            if (referenceGrid == null) return;
            float worldWidth = referenceGrid.Width * referenceGrid.CellSize;
            float worldHeight = referenceGrid.Height * referenceGrid.CellSize;
            Vector3 center = referenceGrid.Origin + new Vector3(worldWidth * 0.5f, 0.01f, worldHeight * 0.5f);
            transform.position = center;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            transform.localScale = new Vector3(worldWidth, worldHeight, 1f);
        }

        /// <summary>Oblicza symetryczny zakres wartości na siatce</summary>
        /// <param name="grid">Siatka do analizy</param>
        /// <param name="rangeMin">Dolna granica zakresu</param>
        /// <param name="rangeMax">Górna granica zakresu</param>
        private static void ComputeDynamicRange(InfluenceGrid grid, out float rangeMin, out float rangeMax)
        {
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
                rangeMin = -absMax;
                rangeMax = absMax;
            }
            else
            {
                if (foundMax < InfluenceMapConstants.InfluenceValueEpsilon) foundMax = 1f;
                rangeMin = 0f;
                rangeMax = foundMax;
            }
        }

        /// <summary>Mieszanie dwóch kolorów</summary>
        /// <param name="background">Istniejący kolor piksela</param>
        /// <param name="foreground">Kolor nowej warstwy</param>
        /// <returns>Wynikowy kolor po blendowaniu</returns>
        private static Color BlendColors(Color background, Color foreground)
        {
            float srcAlpha = foreground.a;
            float dstAlpha = background.a;
            float outAlpha = srcAlpha + dstAlpha * (1f - srcAlpha);
            if (outAlpha < 0.001f) return new Color(0f, 0f, 0f, 0f);
            float invOutAlpha = 1f / outAlpha;
            float r = (foreground.r * srcAlpha + background.r * dstAlpha * (1f - srcAlpha)) * invOutAlpha;
            float g = (foreground.g * srcAlpha + background.g * dstAlpha * (1f - srcAlpha)) * invOutAlpha;
            float b = (foreground.b * srcAlpha + background.b * dstAlpha * (1f - srcAlpha)) * invOutAlpha;
            return new Color(r, g, b, outAlpha);
        }

        /// <summary>Tworzy quad mesh</summary>
        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh
            {
                name = "InfluenceMapVisualizerQuad",
                vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f)
                },
                uv = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f)
                },
                triangles = new int[] { 0, 2, 1, 0, 3, 2 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Warstwy wizualizacji</summary>
        public List<MapVisualizationLayer> Layers => layers;

        /// <summary>Czy wizualizacja jest włączona</summary>
        public bool VisualizationEnabled
        {
            get => visualizationEnabled;
            set
            {
                visualizationEnabled = value;
                if (meshRenderer != null) meshRenderer.enabled = value;
            }
        }

        /// <summary>Przesubskrybowuje - wywoływane gdy zmieni się lista warstw</summary>
        public void RefreshSubscriptions()
        {
            UnsubscribeFromMaps();
            SubscribeToMaps();
        }

        /// <summary>Dodaje nową warstwę wizualizacji</summary>
        /// <param name="map">Mapa do wizualizacji</param>
        /// <param name="gradient">Gradient kolorów</param>
        /// <param name="alpha">Przezroczystość warstwy</param>
        /// <returns>Dodana warstwa</returns>
        public MapVisualizationLayer AddLayer(InfluenceMap map, Gradient gradient = null, float alpha = 0.5f)
        {
            var layer = new MapVisualizationLayer { Map = map, Alpha = alpha };
            if (gradient != null) layer.ColorGradient = gradient;
            layers.Add(layer);
            RefreshSubscriptions();
            return layer;
        }

        /// <summary>Usuwa warstwę wizualizacji</summary>
        /// <param name="layer">Warstwa do usunięcia</param>
        public void RemoveLayer(MapVisualizationLayer layer)
        {
            if (layer == null) return;
            layers.Remove(layer);
            RefreshSubscriptions();
        }

        /// <summary>Usuwa wszystkie warstwy</summary>
        public void ClearLayers()
        {
            UnsubscribeFromMaps();
            layers.Clear();
        }

        /// <summary>Wymusza natychmiastową aktualizację tekstury</summary>
        public void ForceRefresh()
        {
            UpdateTexture();
            UpdateQuadTransform();
        }

#if UNITY_EDITOR
        /// <summary>Bufor połączonych wartości dla podglądu w scenie</summary>
        private float[] gizmoCombineBuffer;
        private readonly List<InfluenceGrid> gizmoGrids = new List<InfluenceGrid>();

        /// <summary>Rysuje liczby i siatkę w oknie sceny (tylko edytor). W grze pozostają same kolorowe pola.</summary>
        private void OnDrawGizmos()
        {
            if (!showValues && !showGridLines) return;
            if (layers == null || layers.Count == 0) return;

            InfluenceGrid refGrid = null;
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null && layers[i].IsReady) { refGrid = layers[i].Map.Grid; break; }
            }
            if (refGrid == null) return;

            int width = refGrid.Width;
            int height = refGrid.Height;
            float cellSize = refGrid.CellSize;
            Vector3 origin = refGrid.Origin;
            float y = origin.y + 0.02f;

            if (showGridLines) DrawSceneGrid(origin, y, cellSize, width, height);
            if (showValues) DrawSceneValues(refGrid, origin, y, cellSize, width, height);
        }

        /// <summary>Rysuje linie siatki na całej szerokości/wysokości</summary>
        private void DrawSceneGrid(Vector3 origin, float y, float cellSize, int width, int height)
        {
            Gizmos.color = gridLineColor;
            float w = width * cellSize;
            float h = height * cellSize;
            for (int x = 0; x <= width; x++)
            {
                float px = origin.x + x * cellSize;
                Gizmos.DrawLine(new Vector3(px, y, origin.z), new Vector3(px, y, origin.z + h));
            }
            for (int z = 0; z <= height; z++)
            {
                float pz = origin.z + z * cellSize;
                Gizmos.DrawLine(new Vector3(origin.x, y, pz), new Vector3(origin.x + w, y, pz));
            }
        }

        /// <summary>Rysuje JEDNĄ połączoną wartość na komórkę (operacja z MapCombiner po gotowych warstwach)</summary>
        private void DrawSceneValues(InfluenceGrid refGrid, Vector3 origin, float y, float cellSize, int width, int height)
        {
            gizmoGrids.Clear();
            for (int i = 0; i < layers.Count; i++)
            {
                MapVisualizationLayer layer = layers[i];
                if (layer == null || !layer.IsReady) continue;
                InfluenceGrid g = layer.Map.Grid;
                if (g.Width == width && g.Height == height) gizmoGrids.Add(g);
            }
            if (gizmoGrids.Count == 0) return;

            int cellCount = refGrid.CellCount;
            if (gizmoCombineBuffer == null || gizmoCombineBuffer.Length < cellCount)
                gizmoCombineBuffer = new float[cellCount];

            if (!MapCombiner.CombineNonAlloc(gizmoGrids.ToArray(), valueCombineOperation, gizmoCombineBuffer)) return;

            GUIStyle style = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
            style.normal.textColor = Color.white;

            for (int cy = 0; cy < height; cy++)
            {
                for (int cx = 0; cx < width; cx++)
                {
                    float value = gizmoCombineBuffer[cy * width + cx];
                    if (Mathf.Abs(value) < drawThreshold) continue;
                    Vector3 pos = new Vector3(origin.x + (cx + 0.5f) * cellSize, y, origin.z + (cy + 0.5f) * cellSize);
                    UnityEditor.Handles.Label(pos, value.ToString("0.00"), style);
                }
            }
        }
#endif
    }
}
