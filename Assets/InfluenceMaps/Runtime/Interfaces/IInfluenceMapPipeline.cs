namespace InfluenceMaps
{
    /// <summary>Interfejs pipeline aktualizacji mapy wpływów</summary>
    public interface IInfluenceMapPipeline
    {
        /// <summary>Wykonuje pełny cykl aktualizacji mapy</summary>
        /// <param name="context">Dane potrzebne do wykonania pipeline</param>
        void Execute(PipelineContext context);
    }
}
