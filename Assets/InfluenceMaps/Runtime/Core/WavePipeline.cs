using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace InfluenceMaps
{
    public class WavePipeline : IInfluenceMapPipeline
    {
        /// <summary>Stan fali pojedynczego źródła</summary>
        private class WaveState
        {
            /// <summary>Czy stan został już zainicjalizowany</summary>
            public bool IsInitialized;
            /// <summary>Komórka położenia źródła z ostatniej aktualizacji</summary>
            public Vector2Int SourceCell;
            /// <summary>Flagi odwiedzenia komórek przez falę</summary>
            public bool[] Visited;
            /// <summary>Lista aktywnych komórek fali</summary>
            public List<int> ActiveCells;
            /// <summary>Zasięg źródła z ostatniej aktualizacji</summary>
            public float LastRadius;
        }

        /// <summary>Stany fal per źródło</summary>
        private readonly Dictionary<IInfluenceSource, WaveState> waveStates = new Dictionary<IInfluenceSource, WaveState>();

        /// <summary>Suma składowych zaniku ze wszystkich źródeł docierających do komórki</summary>
        private float[] decayAccumBuffer;

        /// <summary>Czy komórkę pod wpływem jakiegokolwiek źródła</summary>
        private bool[] reachedThisFrame;

        /// <summary>Mnożniki blokowania per komórka</summary>
        private float[] cellMultipliers;

        /// <summary>Flagi zablokowanych komórek</summary>
        private bool[] cellSolidWall;

        /// <summary>Bufor nowo odkrytych komórek frontu</summary>
        private readonly List<int> newFrontier = new List<int>(512);

        /// <summary>Tymczasowa lista źródeł których stan należy usunąć</summary>
        private readonly List<IInfluenceSource> deadSources = new List<IInfluenceSource>();

        /// <summary>Kolejka BFS używana przy sprawdzaniu spójności fali po odcięciu przeszkody</summary>
        private readonly Queue<int> bfsQueue = new Queue<int>();

        /// <summary>Flagi odwiedzenia w BFS spójności</summary>
        private bool[] bfsVisited;

        /// <summary>Flaga czy komórka ma niezerową wartość lub otrzymała propagację</summary>
        private bool[] isTracked;

        /// <summary>Lista śledzonych komórek</summary>
        private readonly List<int> trackedCells = new List<int>(1024);

        /// <summary>Przesunięcia X góra, dół, lewo, prawo</summary>
        private static readonly int[] DX = { 0, 0, -1, 1 };

        /// <summary>Przesunięcia Y góra, dół, lewo, prawo</summary>
        private static readonly int[] DY = { -1, 1, 0, 0 };

        /// <summary>Liczba komórek przy ostatniej alokacji buforów</summary>
        private int lastCellCount;

        private readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary>Alokacja buforów roboczych przy zmianie liczby komórek siatki</summary>
        /// <param name="cellCount">Aktualna liczba komórek siatki</param>
        private void EnsureBuffers(int cellCount)
        {
            if (lastCellCount == cellCount && decayAccumBuffer != null) return;
            decayAccumBuffer = new float[cellCount];
            reachedThisFrame = new bool[cellCount];
            cellMultipliers = new float[cellCount];
            cellSolidWall = new bool[cellCount];
            bfsVisited = new bool[cellCount];
            isTracked = new bool[cellCount];
            trackedCells.Clear();
            lastCellCount = cellCount;
        }

        /// <summary>Pobiera istniejący stan fali źródła lub tworzy nowy. Realokuje jeśli liczba komórek się zmieniła</summary>
        /// <param name="source">Źródło wpływu</param>
        /// <param name="cellCount">Aktualna liczba komórek siatki</param>
        /// <returns>Stan fali dla danego źródła</returns>
        private WaveState EnsureWaveState(IInfluenceSource source, int cellCount)
        {
            if (waveStates.TryGetValue(source, out WaveState state))
                if (state.Visited != null && state.Visited.Length == cellCount) return state;
            
            state = new WaveState
            {
                IsInitialized = false,
                SourceCell = new Vector2Int(-1, -1),
                Visited = new bool[cellCount],
                ActiveCells = new List<int>(512)
            };
            waveStates[source] = state;
            return state;
        }

        /// <summary>Usuwa stany fal dla usuniętych źródeł</summary>
        /// <param name="currentSources">Bieżąca lista aktywnych źródeł</param>
        private void CleanUpDeadSources(IReadOnlyList<IInfluenceSource> currentSources)
        {
            deadSources.Clear();
            foreach (var kvp in waveStates)
            {
                bool found = false;
                for (int i = 0; i < currentSources.Count; i++)
                    if (ReferenceEquals(kvp.Key, currentSources[i])) 
                    { 
                        found = true;
                        break; 
                    }
                if (!found) deadSources.Add(kvp.Key);
            }
            for (int i = 0; i < deadSources.Count; i++) waveStates.Remove(deadSources[i]);
        }

        /// <summary>Usuwa element z listy przez zamianę z ostatnim i usunięcie ostatniego. Zmienia kolejność elementów</summary>
        /// <param name="list">Lista z której usuwany jest element</param>
        /// <param name="index">Indeks elementu do usunięcia</param>
        private static void SwapRemoveAt(List<int> list, int index)
        {
            int last = list.Count - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }

        /// <summary>
        /// Wykonuje pełny cykl aktualizacji mapy
        /// Propagacja falowa źródeł, stempel wpływu z blokowaniem przeszkód, łączenie z poprzednim stanem mapy z zanikiem oraz zamianę buforów
        /// </summary>
        /// <param name="ctx">Kontekst pipeline aktualizacji</param>
        public void Execute(PipelineContext ctx)
        {
            stopwatch.Restart();

            InfluenceGrid grid = ctx.Grid;
            float[] readBuffer = grid.GetRawReadBuffer();
            float[] writeBuffer = grid.GetRawWriteBuffer();
            int width = grid.Width;
            int height = grid.Height;
            int cellCount = grid.CellCount;
            float maxGlobalRadius = 1f;

            EnsureBuffers(cellCount);

            var sources = ctx.Sources;
            var obstacles = ctx.Obstacles;
            bool hasObstacles = obstacles != null && obstacles.Count > 0;
            IPropagationFunction propagation = ctx.Propagation;
            IDecayFunction decay = ctx.Decay;

            CleanUpDeadSources(sources);

            for (int t = 0; t < trackedCells.Count; t++)
            {
                int cell = trackedCells[t];
                writeBuffer[cell] = 0f;
                decayAccumBuffer[cell] = 0f;
                reachedThisFrame[cell] = false;
            }
            for (int s = 0; s < sources.Count; s++)
            {
                IInfluenceSource source = sources[s];
                if (!source.IsActive) continue;
                Vector2Int sourceCell = grid.WorldToGrid(source.Position);
                if (!grid.IsInBounds(sourceCell.x, sourceCell.y)) continue;
                float intensity = source.Intensity;
                float radius = source.Radius;
                if (radius > maxGlobalRadius) maxGlobalRadius = radius;
                float radiusSq = radius * radius;
                Vector3 sourceCellCenter = grid.GridToWorld(sourceCell.x, sourceCell.y);
                int sourceIndex = sourceCell.y * width + sourceCell.x;
                WaveState state = EnsureWaveState(source, cellCount);

                if (!state.IsInitialized)
                {
                    state.IsInitialized = true;
                    state.SourceCell = sourceCell;
                    state.LastRadius = radius;
                    state.Visited[sourceIndex] = true;
                    state.ActiveCells.Add(sourceIndex);
                }
                else if (state.SourceCell != sourceCell || state.LastRadius != radius)
                {
                    state.SourceCell = sourceCell;
                    state.LastRadius = radius;
                    for (int i = state.ActiveCells.Count - 1; i >= 0; i--)
                    {
                        int cellIndex = state.ActiveCells[i];
                        int cx = cellIndex % width;
                        int cy = cellIndex / width;
                        Vector3 cellWorld = grid.GridToWorld(cx, cy);
                        float dx = cellWorld.x - sourceCellCenter.x;
                        float dz = cellWorld.z - sourceCellCenter.z;
                        if (radius > 0f && (dx * dx + dz * dz) > radiusSq)
                        {
                            state.Visited[cellIndex] = false;
                            SwapRemoveAt(state.ActiveCells, i);
                        }
                    }
                }

                if (!state.Visited[sourceIndex])
                {
                    state.Visited[sourceIndex] = true;
                    state.ActiveCells.Add(sourceIndex);
                }

                if (hasObstacles)
                {
                    for (int i = 0; i < state.ActiveCells.Count; i++)
                    {
                        int cellIndex = state.ActiveCells[i];
                        int cx = cellIndex % width;
                        int cy = cellIndex / width;
                        Vector3 cellWorld = grid.GridToWorld(cx, cy);
                        float combinedMultiplier = 1f;
                        bool isSolid = false;
                        for (int o = 0; o < obstacles.Count; o++)
                        {
                            IInfluenceObstacle obstacle = obstacles[o];
                            if (!obstacle.IsActive) continue;
                            float m = obstacle.EvaluateBlocking(sourceCellCenter, cellWorld);
                            combinedMultiplier *= m;
                            if (m <= 0f && obstacle.BlockingFactor >= 0.999f)
                            {
                                isSolid = true;
                                break;
                            }
                        }
                        cellMultipliers[cellIndex] = combinedMultiplier;
                        cellSolidWall[cellIndex] = isSolid;
                    }
                }

                newFrontier.Clear();
                bool anySevered = false;
                for (int i = state.ActiveCells.Count - 1; i >= 0; i--)
                {
                    int cellIndex = state.ActiveCells[i];
                    if (hasObstacles && cellSolidWall[cellIndex])
                    {
                        state.Visited[cellIndex] = false;
                        SwapRemoveAt(state.ActiveCells, i);
                        anySevered = true;
                        continue;
                    }
                    int cx = cellIndex % width;
                    int cy = cellIndex / width;
                    Vector3 cellWorld = grid.GridToWorld(cx, cy);
                    float dx = cellWorld.x - sourceCellCenter.x;
                    float dz = cellWorld.z - sourceCellCenter.z;
                    float waveDist = Mathf.Sqrt(dx * dx + dz * dz);
                    float p = (propagation != null && radius > 0f) ? propagation.Evaluate(intensity, waveDist, radius) : intensity;
                    
                    if (hasObstacles) p *= cellMultipliers[cellIndex];
                    if (!isTracked[cellIndex])
                    {
                        isTracked[cellIndex] = true;
                        trackedCells.Add(cellIndex);
                        decayAccumBuffer[cellIndex] = 0f;
                        reachedThisFrame[cellIndex] = false;
                    }

                    writeBuffer[cellIndex] += p;
                    reachedThisFrame[cellIndex] = true;
                    if (decay != null) decayAccumBuffer[cellIndex] += decay.Evaluate(readBuffer[cellIndex], waveDist, radius);

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + DX[d];
                        int ny = cy + DY[d];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int ni = ny * width + nx;
                        if (state.Visited[ni]) continue;
                        Vector3 neighborWorld = grid.GridToWorld(nx, ny);
                        float ndx = neighborWorld.x - sourceCellCenter.x;
                        float ndz = neighborWorld.z - sourceCellCenter.z;
                        if (radius > 0f && (ndx * ndx + ndz * ndz) > radiusSq) continue;
                        bool blocked = false;
                        if (hasObstacles)
                        {
                            for (int o = 0; o < obstacles.Count; o++)
                            {
                                IInfluenceObstacle obstacle = obstacles[o];
                                if (!obstacle.IsActive) continue;
                                float m = obstacle.EvaluateBlocking(sourceCellCenter, neighborWorld);
                                if (m <= 0f && obstacle.BlockingFactor >= 0.999f)
                                {
                                    blocked = true;
                                    break;
                                }
                            }
                        }
                        if (!blocked)
                        {
                            state.Visited[ni] = true;
                            newFrontier.Add(ni);
                        }
                    }
                }
                for (int f = 0; f < newFrontier.Count; f++) state.ActiveCells.Add(newFrontier[f]);
                if (anySevered && state.Visited[sourceIndex])
                {
                    bfsQueue.Clear();
                    bfsVisited[sourceIndex] = true;
                    bfsQueue.Enqueue(sourceIndex);
                    while (bfsQueue.Count > 0)
                    {
                        int curr = bfsQueue.Dequeue();
                        int cx = curr % width;
                        int cy = curr / width;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx + DX[d];
                            int ny = cy + DY[d];
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                            int ni = ny * width + nx;
                            if (!state.Visited[ni] || bfsVisited[ni]) continue;
                            bfsVisited[ni] = true;
                            bfsQueue.Enqueue(ni);
                        }
                    }
                    for (int i = state.ActiveCells.Count - 1; i >= 0; i--)
                    {
                        int cellIndex = state.ActiveCells[i];
                        if (!bfsVisited[cellIndex])
                        {
                            state.Visited[cellIndex] = false;
                            SwapRemoveAt(state.ActiveCells, i);
                        }
                    }
                    foreach (int cellIndex in state.ActiveCells) bfsVisited[cellIndex] = false;
                    bfsVisited[sourceIndex] = false;
                }
            }
            float clampMin = ctx.ApplyClamp ? ctx.MinValue : float.MinValue;
            float clampMax = ctx.ApplyClamp ? ctx.MaxValue : float.MaxValue;
            float eps = InfluenceMapConstants.InfluenceValueEpsilon;
            bool useOutOfRangeFade = ctx.UseOutOfRangeFade;
            float outOfRangeFadeFactor = Mathf.Clamp01(ctx.OutOfRangeFadeFactor);

            for (int t = trackedCells.Count - 1; t >= 0; t--)
            {
                int i = trackedCells[t];
                float currentState = readBuffer[i];
                float p_total = writeBuffer[i];
                if (Mathf.Abs(currentState) <= eps && Mathf.Abs(p_total) <= eps)
                {
                    writeBuffer[i] = 0f;
                    readBuffer[i] = 0f;
                    isTracked[i] = false;
                    SwapRemoveAt(trackedCells, t);
                    continue;
                }
                float d = 0f;
                bool outOfInfluence = !reachedThisFrame[i];
                if (outOfInfluence)
                {
                    if (useOutOfRangeFade) d = Mathf.Abs(currentState) * outOfRangeFadeFactor;
                    else if (decay != null) d = decay.Evaluate(currentState, maxGlobalRadius, maxGlobalRadius);
                }
                else d = decayAccumBuffer[i];

                float decayMagnitude = Mathf.Min(d, Mathf.Abs(currentState));
                float decayedState = currentState - Mathf.Sign(currentState) * decayMagnitude;
                float newValue = decayedState + p_total;
                writeBuffer[i] = Mathf.Clamp(newValue, clampMin, clampMax);
            }
            grid.SwapBuffers();
            stopwatch.Stop();
        }
    }
}