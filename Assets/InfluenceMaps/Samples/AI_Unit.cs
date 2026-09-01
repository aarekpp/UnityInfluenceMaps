using InfluenceMaps;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AI_Unit : MonoBehaviour
{
    public enum AgentRole { Chaser, Evader }

    [Header("Rola Agenta")]
    [SerializeField] private AgentRole role = AgentRole.Chaser;

    [Header("Referencje do map bazowych")]
    [SerializeField] private InfluenceMap threatMap;
    [SerializeField] private InfluenceMap targetMap;

    [Header("Ustawienia aktualizacji")]
    [SerializeField] private float updateInterval = 0.1f;

    [Header("Ucieczka")]
    [SerializeField] private float escapeCheckDistance = 5f;
    [SerializeField] private float momentumWeight = 3f;
    [SerializeField] private float edgePenaltyWeight = 3f;
    [SerializeField] private float borderSafetyMargin = 2f;
    [SerializeField] private float threatDeadzone = 0.02f;

    private static readonly Vector3[] EscapeDirections =
    {
        Vector3.forward,
        Vector3.forward + Vector3.right,
        Vector3.right,
        Vector3.back + Vector3.right,
        Vector3.back,
        Vector3.back + Vector3.left,
        Vector3.left,
        Vector3.forward + Vector3.left
    };

    private NavMeshAgent navAgent;
    private WorkingMap workingMap;
    private NavMeshPath reusablePath;
    private Vector3 currentTargetPosition;
    private float updateTimer;
    private Vector3 lastValidDirection = Vector3.forward;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        currentTargetPosition = transform.position;
        reusablePath = new NavMeshPath();
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval) return;
        updateTimer = 0f;
        if (role == AgentRole.Chaser) UpdateChaser();
        else UpdateEvader();
    }

    private void UpdateChaser()
    {
        if (targetMap == null || targetMap.Grid == null) return;
        if (workingMap == null) workingMap = new WorkingMap(targetMap.Grid);
        workingMap.ResizeToMap(targetMap.Grid);
        workingMap.Clear(transform.position);
        workingMap.AddFrom(targetMap.Grid, 1f);
        InfluenceCell highest = workingMap.GetHighestCell();
        if (Mathf.Abs(highest.Value) <= InfluenceMapConstants.InfluenceValueEpsilon) return;
        currentTargetPosition = workingMap.LocalToWorld(highest.X, highest.Y);
        if (navAgent.isOnNavMesh) navAgent.SetDestination(currentTargetPosition);
    }

    private void UpdateEvader()
    {
        if (threatMap == null || threatMap.Grid == null) return;
        float currentThreat = threatMap.Grid.GetValue(transform.position);
        if (currentThreat <= threatDeadzone)
        {
            currentTargetPosition = transform.position;
            if (navAgent.isOnNavMesh && navAgent.hasPath) navAgent.ResetPath();
            return;
        }
        if (navAgent.velocity.sqrMagnitude > 0.1f) lastValidDirection = navAgent.velocity.normalized;
        Vector3 bestEscapePoint = transform.position;
        float lowestPathCost = float.MaxValue;

        foreach (Vector3 dir in EscapeDirections)
        {
            Vector3 normalizedDir = dir.normalized;
            Vector3 targetTestPoint = transform.position + normalizedDir * escapeCheckDistance;
            if (!NavMesh.SamplePosition(targetTestPoint, out NavMeshHit hit, 2.0f, NavMesh.AllAreas)) continue;
            if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, reusablePath)) continue;
            if (reusablePath.status != NavMeshPathStatus.PathComplete) continue;

            float threatCost = EvaluatePathThreatDense(reusablePath);
            float alignment = Vector3.Dot(normalizedDir, lastValidDirection);
            float momentumCost = (1f - alignment) * momentumWeight;
            float edgeCost = CalculateEdgePenalty(hit.position);
            float totalCost = threatCost + momentumCost + edgeCost;

            if (totalCost < lowestPathCost)
            {
                lowestPathCost = totalCost;
                bestEscapePoint = hit.position;
            }
        }
        currentTargetPosition = bestEscapePoint;
        if (navAgent.isOnNavMesh && currentTargetPosition != transform.position) navAgent.SetDestination(currentTargetPosition);
    }

    private float EvaluatePathThreatDense(NavMeshPath path)
    {
        float totalThreat = 0f;
        if (path.corners.Length < 2) return totalThreat;
        float sampleStep = 1.0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            Vector3 start = path.corners[i - 1];
            Vector3 end = path.corners[i];
            float segmentLength = Vector3.Distance(start, end);
            Vector3 direction = (end - start).normalized;
            float currentDistance = 0f;
            while (currentDistance < segmentLength)
            {
                Vector3 samplePoint = start + direction * currentDistance;
                float threat = threatMap.Grid.GetValue(samplePoint);
                float distFromAgent = Vector3.Distance(transform.position, samplePoint);
                float distanceFactor = 1f / (1f + distFromAgent);
                totalThreat += threat * distanceFactor * 25f;
                currentDistance += sampleStep;
            }
        }
        totalThreat += GetPathLength(path) * 0.2f;
        return totalThreat;
    }

    private float CalculateEdgePenalty(Vector3 checkPoint)
    {
        if (threatMap == null || threatMap.Grid == null) return 0f;
        InfluenceGrid grid = threatMap.Grid;
        float minX = grid.Origin.x;
        float maxX = minX + (grid.Width * grid.CellSize);
        float minZ = grid.Origin.z;
        float maxZ = minZ + (grid.Height * grid.CellSize);
        float distToLeft = checkPoint.x - minX;
        float distToRight = maxX - checkPoint.x;
        float distToBottom = checkPoint.z - minZ;
        float distToTop = maxZ - checkPoint.z;
        float minDistanceToEdge = Mathf.Min(Mathf.Min(distToLeft, distToRight), Mathf.Min(distToBottom, distToTop));
        if (minDistanceToEdge < borderSafetyMargin)
        {
            float normalizedDanger = 1f - (Mathf.Max(0f, minDistanceToEdge) / borderSafetyMargin);
            return normalizedDanger * edgePenaltyWeight;
        }
        return 0f;
    }

    private float GetPathLength(NavMeshPath path)
    {
        float lng = 0f;
        if (path.corners.Length < 2) return lng;
        for (int i = 1; i < path.corners.Length; i++) lng += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return lng;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = (role == AgentRole.Chaser) ? Color.red : Color.green;
        Gizmos.DrawLine(transform.position, currentTargetPosition);
        Gizmos.DrawWireSphere(currentTargetPosition, 0.5f);
    }
}
