using UnityEngine;
using System.Collections.Generic;

namespace InfluenceMaps
{
    /// <summary>Komponent przeszkody blokującej lub osłabiającej propagację we wskazanych mapach</summary>
    [AddComponentMenu("Influence Maps/Influence Obstacle")]
    public class InfluenceObstacleComponent : MonoBehaviour, IInfluenceObstacle
    {
        /// <summary>Mapy wpływów na które oddziałuje ta przeszkoda</summary>
        [Header("Przynależność")]
        [SerializeField] private List<InfluenceMap> targetMaps = new List<InfluenceMap>();

        /// <summary>Stopień blokowania wpływu</summary>
        [Header("Blokowanie")]
        [Range(0f, 1f)]
        [SerializeField] private float blockingFactor = 1f;

        /// <summary>Długość strefy martwej za murem </summary>
        [Tooltip("Odległość strefy martwej za murem (0 = nieskończony cień)")]
        [Min(0f)]
        [SerializeField] private float shadowDistance = 0f;

        /// <summary>Połowa wymiarów przeszkody w osiach X i Z pobierana automatycznie z BoxCollidera</summary>
        [Header("Wymiary (automatyczne z BoxCollidera)")]
        [SerializeField] private Vector2 halfSize = new Vector2(0.5f, 0.5f);

        /// <summary>Zapamiętana macierz przekształcenia ze świata do lokalnej przestrzeni</summary>
        private Matrix4x4 cachedWorldToLocal;

        /// <summary>Zapamiętana pozycja środka przeszkody w przestrzeni świata</summary>
        private Vector3 cachedPosition;

        /// <summary>Zapamiętany środek collidera w przestrzeni lokalnej</summary>
        private Vector2 cachedLocalCenterXZ;

        /// <summary>Przybliżony promień przeszkody używany do szybkiego odrzucania testów przecięcia</summary>
        private float wallPhysicalRadius;

        /// <summary>Czy zapamiętane dane są aktualne</summary>
        private bool isCacheValid = false;

        /// <summary>Collider typu Box z którego pobierane są wymiary przeszkody</summary>
        private BoxCollider boxCollider;

        /// <summary>Czy przeszkoda jest aktywna</summary>
        public bool IsActive => isActiveAndEnabled;

        /// <summary>Stopień blokowania wpływu w zakresie 0-1</summary>
        public float BlockingFactor => blockingFactor;

        /// <summary>Lista tylko do odczytu map wpływów na które oddziałuje ta przeszkoda</summary>
        public IReadOnlyList<InfluenceMap> TargetMaps => targetMaps;

        /// <summary>Pozycja przeszkody w przestrzeni świata</summary>
        public Vector3 Position => transform.position;

        /// <summary>Pobranie collidera i wstępne wyliczenie cache przy inicjalizacji</summary>
        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
            RefreshCache();
        }

        /// <summary>Odświeżenie cache w klatce, w której transform przeszkody uległ zmianie</summary>
        private void Update()
        {
            if (!transform.hasChanged) return;
            RefreshCache();
            transform.hasChanged = false;
        }

        /// <summary>Przeliczenie i zapamiętanie danych przeszkody</summary>
        private void RefreshCache()
        {
            cachedWorldToLocal = transform.worldToLocalMatrix;
            Vector3 localCenter = boxCollider != null ? boxCollider.center : Vector3.zero;
            cachedPosition = transform.TransformPoint(localCenter);
            cachedLocalCenterXZ = new Vector2(localCenter.x, localCenter.z);
            if (boxCollider != null) halfSize = new Vector2(boxCollider.size.x * 0.5f, boxCollider.size.z * 0.5f);
            Vector3 worldScale = transform.lossyScale;
            float scaledHalfX = halfSize.x * Mathf.Abs(worldScale.x);
            float scaledHalfZ = halfSize.y * Mathf.Abs(worldScale.z);
            wallPhysicalRadius = Mathf.Max(scaledHalfX, scaledHalfZ) * 1.5f;
            isCacheValid = true;
        }

        /// <summary>Rejestracja przeszkody we wszystkich docelowych mapach po włączeniu komponentu</summary>
        private void OnEnable()
        {
            foreach (var map in targetMaps)
            {
                if (map != null) map.RegisterObstacle(this);
            }
        }

        /// <summary>Wyrejestrowanie przeszkody ze wszystkich docelowych map po wyłączeniu komponentu</summary>
        private void OnDisable()
        {
            foreach (var map in targetMaps)
            {
                if (map != null) map.UnregisterObstacle(this);
            }
        }

        /// <summary>Wyznaczenie współczynnika blokowania wpływu na odcinku od źródła do celu</summary>
        /// <param name="source">Punkt początkowy odcinka</param>
        /// <param name="target">Punkt końcowy odcinka</param>
        /// <returns>1 = brak blokady, 0 = pełna blokada</returns>
        public float EvaluateBlocking(Vector3 source, Vector3 target)
        {
            if (!isCacheValid) return 1f;

            float minX = source.x < target.x ? source.x : target.x;
            float maxX = source.x > target.x ? source.x : target.x;
            float minZ = source.z < target.z ? source.z : target.z;
            float maxZ = source.z > target.z ? source.z : target.z;

            if (cachedPosition.x + wallPhysicalRadius < minX || cachedPosition.x - wallPhysicalRadius > maxX ||
                cachedPosition.z + wallPhysicalRadius < minZ || cachedPosition.z - wallPhysicalRadius > maxZ)
            {
                return 1f;
            }

            Vector3 localStart = cachedWorldToLocal.MultiplyPoint3x4(source);
            Vector3 localEnd = cachedWorldToLocal.MultiplyPoint3x4(target);
            Vector2 origin = new Vector2(localStart.x - cachedLocalCenterXZ.x, localStart.z - cachedLocalCenterXZ.y);
            Vector2 dir = new Vector2(localEnd.x - localStart.x, localEnd.z - localStart.z);

            if (FastSegmentBoxIntersect2D(origin, dir, halfSize, out float tmax))
            {
                if (shadowDistance > 0f)
                {
                    float segmentLength = Vector3.Distance(source, target);
                    float distanceBehindWall = segmentLength * (1f - tmax);
                    if (distanceBehindWall <= shadowDistance) return 0f;
                }
                return 1f - blockingFactor;
            }
            return 1f;
        }

        /// <summary>Algorytm Slab z parametrem wyjściowym tmaxOut do obliczania długości cienia</summary>
        /// <param name="origin">Początek odcinka w lokalnej przestrzeni przeszkody</param>
        /// <param name="dir">Wektor kierunku odcinka w przestrzeni lokalnej</param>
        /// <param name="extents">Połowa wymiarów prostokąta przeszkody</param>
        /// <param name="tmaxOut">Parametr wyjścia odcinka z prostokąta</param>
        /// <returns>True, jeśli odcinek przecina prostokąt przeszkody</returns>
        private bool FastSegmentBoxIntersect2D(Vector2 origin, Vector2 dir, Vector2 extents, out float tmaxOut)
        {
            tmaxOut = 0f;

            float invDirX = 1f / (dir.x == 0f ? 0.00001f : dir.x);
            float invDirY = 1f / (dir.y == 0f ? 0.00001f : dir.y);

            float t0x = (-extents.x - origin.x) * invDirX;
            float t1x = (extents.x - origin.x) * invDirX;

            float tminX = t0x < t1x ? t0x : t1x;
            float tmaxX = t0x > t1x ? t0x : t1x;

            float t0y = (-extents.y - origin.y) * invDirY;
            float t1y = (extents.y - origin.y) * invDirY;

            float tminY = t0y < t1y ? t0y : t1y;
            float tmaxY = t0y > t1y ? t0y : t1y;

            float tmin = tminX > tminY ? tminX : tminY;
            float tmax = tmaxX < tmaxY ? tmaxX : tmaxY;

            if (tmax >= tmin && tmin <= 1.0f && tmax >= 0.0f)
            {
                tmaxOut = tmax;
                return true;
            }
            return false;
        }

        /// <summary>Dodanie mapy do listy docelowych i rejestracja przeszkody, jeśli komponent jest aktywny</summary>
        /// <param name="map">Mapa wpływu do dodania</param>
        public void AddTargetMap(InfluenceMap map)
        {
            if (map == null || targetMaps.Contains(map)) return;
            targetMaps.Add(map);
            if (isActiveAndEnabled) map.RegisterObstacle(this);
        }

        /// <summary>Usunięcie mapy z listy docelowych i wyrejestrowanie z niej przeszkody</summary>
        /// <param name="map">Mapa wpływu do usunięcia</param>
        public void RemoveTargetMap(InfluenceMap map)
        {
            if (map == null || !targetMaps.Contains(map)) return;
            if (isActiveAndEnabled) map.UnregisterObstacle(this);
            targetMaps.Remove(map);
        }

        /// <summary>Ustawienie współczynnika blokowania</summary>
        /// <param name="factor">Nowy współczynnik blokowania</param>
        public void SetBlockingFactor(float factor) => blockingFactor = Mathf.Clamp01(factor);

        /// <summary>Ustawienie długości cienia za murem</summary>
        /// <param name="distance">Nowa długość strefy cienia</param>
        public void SetShadowDistance(float distance) => shadowDistance = Mathf.Max(0f, distance);
    }
}