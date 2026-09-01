using System;

namespace InfluenceMaps
{
    /// <summary>Interfejs pipeline aktualizacji mapy wpływów opartych o Unity Jobs</summary>
    public interface IJobsInfluenceMapPipeline : IInfluenceMapPipeline, IDisposable
    {
        /// <summary>Wymusza przebudowę krzywych po zmianie AnimationCurve w edytorze</summary>
        void InvalidateCurves();
    }
}