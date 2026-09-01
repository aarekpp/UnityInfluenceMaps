using UnityEngine;

namespace InfluenceMaps
{
    /// <summary>
    /// Komponent źródła emitującego wpływ na siatkę mapy
    /// Jedno źródło należy do jednej mapy, jednostka gry może mieć wiele źródeł
    /// </summary>
    [AddComponentMenu("Influence Maps/Influence Source")]
    public class InfluenceSourceComponent : MonoBehaviour, IInfluenceSource
    {
        /// <summary>Mapa wpływów do której należy źródło</summary>
        [Header("Przynależność")]
        [Tooltip("Mapa do której to źródło jest rejestrowane")]
        [SerializeField]
        private InfluenceMap targetMap;

        /// <summary>Zasięg wpływu w jednostkach świata</summary>
        [Header("Parametry wpływu")]
        [Tooltip("Zasięg propagacji wpływu od pozycji źródła")]
        [Min(0f)]
        [SerializeField]
        private float radius = 5f;

        /// <summary>Siła wpływu</summary>
        [Tooltip("Bazowa siła wpływu. Dodatnia lub ujemna")]
        [SerializeField]
        private float intensity = 1f;

        /// <summary>Rejestracja w mapie gdy komponent się włącza</summary>
        private void OnEnable()
        {
            if (targetMap != null) targetMap.RegisterSource(this);
        }

        /// <summary>Wyrejestrowanie z mapy gdy komponent się wyłącza</summary>
        private void OnDisable()
        {
            if (targetMap != null) targetMap.UnregisterSource(this);
        }

        /// <summary>Rysowanie zasięgu wpływu gdy obiekt jest zaznaczony w edytorze</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = intensity >= 0 ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        /// <summary>Pozycja źródła</summary>
        public Vector3 Position => transform.position;

        /// <summary>Zasięg wpływu</summary>
        public float Radius => radius;

        /// <summary>Siła wpływu</summary>
        public float Intensity => intensity;

        /// <summary>Czy źródło jest aktywne w hierarchii</summary>
        public bool IsActive => isActiveAndEnabled;

        /// <summary>Mapa do której należy źródło</summary>
        public InfluenceMap TargetMap
        {
            get => targetMap;
            set
            {
                if (targetMap == value) return;
                if (isActiveAndEnabled && targetMap != null) targetMap.UnregisterSource(this);
                targetMap = value;
                if (isActiveAndEnabled && targetMap != null) targetMap.RegisterSource(this);
            }
        }

        /// <summary>Ustawienie zasięgu wpływu z kodu</summary>
        /// <param name="newRadius">Nowy zasięg</param>
        public void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0f, newRadius);
        }

        /// <summary>Ustawienie siły wpływu z kodu</summary>
        /// <param name="newIntensity">Nowa siła wpływu</param>
        public void SetIntensity(float newIntensity)
        {
            intensity = newIntensity;
        }
    }
}
