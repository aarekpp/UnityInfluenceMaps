using System;
using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>Niezmienna komórka siatki wpływów używana jako snapshot stanu jednej komórki</summary>
    [Serializable]
    public readonly struct InfluenceCell : IEquatable<InfluenceCell>
    {
        /// <summary>Pozycja komórki w siatce w osi X</summary>
        public readonly int X;

        /// <summary>Pozycja komórki w siatce w osi Y/Z</summary>
        public readonly int Y;

        /// <summary>Wartość komórki</summary>
        public readonly float Value;

        /// <summary>Konstruktor z podanymi koordynatami i wartością</summary>
        /// <param name="x">Kolumna w siatce</param>
        /// <param name="y">Wiersz w siatce</param>
        /// <param name="value">Wartość wpływu</param>
        public InfluenceCell(int x, int y, float value)
        {
            X = x; Y = y; Value = value;
        }

        /// <summary>Koordynaty jako Vector2Int</summary>
        public Vector2Int Coordinates => new Vector2Int(X, Y);

        /// <inheritdoc/>
        public bool Equals(InfluenceCell other)
        {
            return X == other.X && Y == other.Y && Mathf.Approximately(Value, other.Value);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is InfluenceCell other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        /// <inheritdoc/>
        public static bool operator ==(InfluenceCell left, InfluenceCell right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(InfluenceCell left, InfluenceCell right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Cell({X}, {Y}): {Value:F3}";
        }
    }
}