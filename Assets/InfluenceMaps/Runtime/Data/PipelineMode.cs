namespace InfluenceMaps
{
    /// <summary>Tryb obliczania propagacji wpływu w pipeline</summary>
    public enum PipelineMode
    {
        /// <summary>Cykl aktualizacji z metodą falową propagacji</summary>
        Wave,

        /// <summary>Własna metoda cyklu aktualizacji</summary>
        Custom
    }
}