using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Siatka wartości wpływów z podwójnym buforowaniem</summary>
    public class InfluenceGrid
    {
        /// <summary>Bufor odczytu aktualnego stanu mapy</summary>
        private float[] readBuffer;

        /// <summary>Bufor zapisu obliczeń</summary>
        private float[] writeBuffer;

        /// <summary>Liczba komórek w poziomie</summary>
        public int Width { get; private set; }

        /// <summary>Liczba komórek w pionie</summary>
        public int Height { get; private set; }

        /// <summary>Rozmiar komórki</summary>
        public float CellSize { get; private set; }

        /// <summary>Lewy dolny róg siatki</summary>
        public Vector3 Origin { get; private set; }

        /// <summary>Łączna liczba komórek</summary>
        public int CellCount => Width * Height;

        /// <summary>Aktualny stan mapy tylko do odczytu</summary>
        public ReadOnlySpan<float> Values => readBuffer.AsSpan();

        /// <summary>Bezpośredni dostęp do tablicy odczytu</summary>
        public float[] GetRawReadBuffer() => readBuffer;

        /// <summary>Bezpośredni dostęp do tablicy zapisu</summary>
        public float[] GetRawWriteBuffer() => writeBuffer;

        /// <summary>Preferowany konstruktor tworzący siatkę z ustawień GridSettings</summary>
        /// <param name="settings">Ustawienia siatki</param>
        public InfluenceGrid(GridSettings settings) : this(settings.Width, settings.Height, settings.CellSize, settings.Origin) { }

        /// <summary>Tworzy siatkę z podanymi parametrami i alokuje oba bufory</summary>
        /// <param name="width">Liczba komórek w poziomie</param>
        /// <param name="height">Liczba komórek w pionie</param>
        /// <param name="cellSize">Rozmiar komórki</param>
        /// <param name="origin">Lewy dolny róg siatki</param>
        public InfluenceGrid(int width, int height, float cellSize, Vector3 origin)
        {
            Width = Mathf.Max(width, InfluenceMapConstants.MinGridDimension);
            Height = Mathf.Max(height, InfluenceMapConstants.MinGridDimension);
            CellSize = Mathf.Max(cellSize, InfluenceMapConstants.MinCellSize);
            Origin = origin;
            int totalCells = Width * Height;
            readBuffer = new float[totalCells];
            writeBuffer = new float[totalCells];
        }

        /// <summary>Zmiana koordynatów 2D na indeks flat array</summary>
        /// <param name="x">Kolumna</param>
        /// <param name="y">Wiersz</param>
        /// <returns>Indeks w tablicy float[]</returns>
        public int GetIndex(int x, int y)
        {
            return y * Width + x;
        }

        /// <summary>Zamienia indeks tablicy float[] na koordynaty 2D</summary>
        /// <param name="index">Indeks w flat array</param>
        /// <returns>Koordynaty w siatce</returns>
        public Vector2Int GetCoordinates(int index)
        {
            return new Vector2Int(index % Width, index / Width);
        }

        /// <summary>Sprawdza czy koordynaty mieszczą się w granicach siatki</summary>
        /// <param name="x">Kolumna w siatce</param>
        /// <param name="y">Wiersz w siatce</param>
        /// <returns>True jeśli koordynaty są w granicach</returns>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>Sprawdza czy pozycja w świecie gry mieści się w obszarze pokrywanym przez siatkę</summary>
        /// <param name="worldPosition">Pozycja w świecie</param>
        /// <returns>True jeśli pozycja jest w granicach siatki</returns>
        public bool IsInBounds(Vector3 worldPosition)
        {
            Vector2Int grid = WorldToGrid(worldPosition);
            return IsInBounds(grid.x, grid.y);
        }

        /// <summary>Pobieranie wartości wpływu z readBuffer</summary>
        /// <param name="x">Kolumna w siatce</param>
        /// <param name="y">Wiersz w siatce</param>
        /// <returns>Wartość wpływu lub 0 jeśli poza granicami</returns>
        public float GetValue(int x, int y)
        {
            if (!IsInBounds(x, y)) return 0f;
            return readBuffer[GetIndex(x, y)];
        }

        /// <summary>Pobiera wartość wpływu na podstawie pozycji w świecie</summary>
        /// <param name="worldPosition">Pozycja w świecie</param>
        /// <returns>Wartość wpływu lub 0 jeśli poza granicami</returns>
        public float GetValue(Vector3 worldPosition)
        {
            Vector2Int grid = WorldToGrid(worldPosition);
            return GetValue(grid.x, grid.y);
        }

        /// <summary>Zapis wartości do writeBuffer</summary>
        /// <param name="x">Kolumna w siatce</param>
        /// <param name="y">Wiersz w siatce</param>
        /// <param name="value">Wartość do zapisania</param>
        public void SetValue(int x, int y, float value)
        {
            if (!IsInBounds(x, y)) return;
            writeBuffer[GetIndex(x, y)] = value;
        }

        /// <summary>Zapisuje wartość na podstawie pozycji w świecie</summary>
        /// <param name="worldPosition">Pozycja w świecie gry</param>
        /// <param name="value">Wartość wpływu do zapisania</param>
        public void SetValue(Vector3 worldPosition, float value)
        {
            Vector2Int grid = WorldToGrid(worldPosition);
            SetValue(grid.x, grid.y, value);
        }

        /// <summary>Zamiana readBuffer z writeBuffer</summary>
        public void SwapBuffers()
        {
            (readBuffer, writeBuffer) = (writeBuffer, readBuffer);
        }

        /// <summary>Zerowanie obu buforów</summary>
        public void Clear()
        {
            Array.Clear(readBuffer, 0, CellCount);
            Array.Clear(writeBuffer, 0, CellCount);
        }

        /// <summary>Zerowanie tylko writeBuffer</summary>
        public void ClearWriteBuffer()
        {
            Array.Clear(writeBuffer, 0, CellCount);
        }

        /// <summary>Konwertuje pozycję w świecie na koordynaty siatki</summary>
        /// <param name="worldPosition">Pozycja w świecie</param>
        /// <returns>Koordynaty komórki</returns>
        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            float localX = worldPosition.x - Origin.x;
            float localZ = worldPosition.z - Origin.z;
            int gridX = Mathf.FloorToInt(localX / CellSize);
            int gridY = Mathf.FloorToInt(localZ / CellSize);
            return new Vector2Int(gridX, gridY);
        }

        /// <summary>Konwersja koordynatów siatki na pozycję środka komórki w świecie</summary>
        /// <param name="gridX">Kolumna w siatce</param>
        /// <param name="gridY">Wiersz w siatce</param>
        /// <returns>Środek komórki w świecie</returns>
        public Vector3 GridToWorld(int gridX, int gridY)
        {
            float halfCell = CellSize * 0.5f;
            float worldX = Origin.x + (gridX * CellSize) + halfCell;
            float worldZ = Origin.z + (gridY * CellSize) + halfCell;
            return new Vector3(worldX, Origin.y, worldZ);
        }

        /// <summary>Zmiana wymiarów siatki na podstawie ustawień GridSettings</summary>
        /// <param name="settings">Nowe ustawienia siatki</param>
        public void Resize(GridSettings settings)
        {
            Resize(settings.Width, settings.Height, settings.CellSize, settings.Origin);
        }

        /// <summary>Zmiana wymiarów siatki z jawnie podanym originem i alokacja nowych buforów</summary>
        /// <param name="newWidth">Nowa liczba komórek w poziomie</param>
        /// <param name="newHeight">Nowa liczba komórek w pionie</param>
        /// <param name="newCellSize">Nowy rozmiar komórki</param>
        /// <param name="newOrigin">Nowy lewy dolny róg siatki</param>
        public void Resize(int newWidth, int newHeight, float newCellSize, Vector3 newOrigin)
        {
            newWidth = Mathf.Max(newWidth, InfluenceMapConstants.MinGridDimension);
            newHeight = Mathf.Max(newHeight, InfluenceMapConstants.MinGridDimension);
            newCellSize = Mathf.Max(newCellSize, InfluenceMapConstants.MinCellSize);
            if (newWidth == Width && newHeight == Height && Mathf.Approximately(newCellSize, CellSize) && newOrigin == Origin) return;
            Width = newWidth;
            Height = newHeight;
            CellSize = newCellSize;
            Origin = newOrigin;
            int totalCells = Width * Height;
            readBuffer = new float[totalCells];
            writeBuffer = new float[totalCells];
        }
    }
}
