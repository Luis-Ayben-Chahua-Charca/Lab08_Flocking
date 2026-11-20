using System.Collections.Generic;
using UnityEngine;

public class EnemyGroupController : MonoBehaviour
{
    [SerializeField] private PlayerController m_Target;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float m_MoveSpeed = 6f;
    [SerializeField, Min(0f)] private float m_RotationSpeed = 540f;
    [SerializeField, Min(0f)] private float m_PursuitWeight = 2f;
    [SerializeField, Min(0.1f)] private float m_SlowdownDistance = 1.1f;

    [Header("Flocking")]
    [SerializeField, Min(0f)] private float m_NeighborRadius = 5f;
    [SerializeField, Min(0f)] private float m_SeparationDistance = 1.25f;
    [SerializeField, Min(0f)] private float m_CohesionWeight = 0.55f;
    [SerializeField, Min(0f)] private float m_SeparationWeight = 3f;
    [SerializeField, Min(0f)] private float m_AlignmentWeight = 0.8f;

    [Header("Acceptance")]
    [Tooltip("An enemy stops when it reaches this horizontal distance from the player.")]
    [SerializeField, Min(0.1f)] private float m_AcceptanceRadius = 3.5f;
    [Tooltip("Seconds the player must remain outside the waiting zone before pursuit resumes.")]
    [SerializeField, Min(0f)] private float m_ResumeDelay = 1f;
    [Tooltip("Extra distance before the waiting timer starts. Avoids reacting to small movements.")]
    [SerializeField, Min(0f)] private float m_ResumeMargin = 0.35f;

    private readonly List<EnemyController> m_SceneEnemies = new();
    private readonly List<EnemyController> m_Enemies = new();
    private Vector3[] m_NextVelocities;

    private void Start()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            m_SceneEnemies.AddRange(root.GetComponentsInChildren<EnemyController>(true));
        }

        m_NextVelocities = new Vector3[m_SceneEnemies.Count];
    }

    private void LateUpdate() => Step(Time.deltaTime);

    private void Step(float deltaTime)
    {
        if (m_Target == null || deltaTime <= 0f)
            return;

        Vector3 center = m_Target.transform.position;
        m_Enemies.Clear();
        foreach (EnemyController enemy in m_SceneEnemies)
        {
            if (enemy != null && enemy.isActiveAndEnabled)
                m_Enemies.Add(enemy);
        }

        for (int i = 0; i < m_Enemies.Count; i++)
        {
            m_NextVelocities[i] = CalculateVelocity(i, center, deltaTime);
        }

        // All agents read the same positions and velocities before anyone moves.
        for (int i = 0; i < m_Enemies.Count; i++)
        {
            m_Enemies[i].Move(m_NextVelocities[i], center, m_MoveSpeed, m_RotationSpeed, deltaTime);
        }
    }

    private Vector3 CalculateVelocity(int index, Vector3 center, float deltaTime)
    {
        EnemyController enemy = m_Enemies[index];
        Vector3 toTarget = (center - enemy.Position).WithNewY(0f);
        float distance = toTarget.magnitude;
        if (!enemy.ShouldPursue(distance, m_AcceptanceRadius, m_ResumeMargin, m_ResumeDelay, deltaTime)
            || m_MoveSpeed <= 0f || m_PursuitWeight <= 0f)
            return Vector3.zero;

        Vector3 forward = toTarget / distance;
        float approach = Mathf.Lerp(0.12f, 1f, Mathf.Clamp01((distance - m_AcceptanceRadius) / m_SlowdownDistance));
        Vector3 steering = forward * (m_PursuitWeight * approach) + CalculateFlocking(index, approach);
        Vector3 velocity = Vector3.ClampMagnitude(steering / m_PursuitWeight, 1f) * m_MoveSpeed;
        if (m_SeparationWeight <= 0f || m_SeparationDistance <= 0f)
            return velocity;

        velocity = LimitClosingSpeed(index, velocity, forward, deltaTime, out bool blockedByWaitingEnemy);
        float queueRadius = m_AcceptanceRadius + 2f * m_SeparationDistance;
        if (distance <= queueRadius && blockedByWaitingEnemy && Vector3.Dot(velocity, forward) < 0.3f
            && velocity.sqrMagnitude < 0.8f * 0.8f)
        {
            // A crowded approach can settle in a second row instead of looking for a slot behind the player.
            enemy.HoldPosition(distance);
            return Vector3.zero;
        }

        return velocity;
    }

    private Vector3 CalculateFlocking(int index, float approach)
    {
        Vector3 centerOffset = Vector3.zero;
        Vector3 directionSum = Vector3.zero;
        Vector3 separation = Vector3.zero;
        int neighbors = 0;
        int movingNeighbors = 0;
        Vector3 position = m_Enemies[index].Position;
        float radiusSquared = m_NeighborRadius * m_NeighborRadius;

        for (int i = 0; i < m_Enemies.Count; i++)
        {
            if (i == index)
                continue;

            EnemyController other = m_Enemies[i];
            Vector3 offset = (other.Position - position).WithNewY(0f);
            float squaredDistance = offset.sqrMagnitude;
            if (squaredDistance <= radiusSquared && m_NeighborRadius > 0f)
            {
                centerOffset += offset;
                neighbors++;
                if (other.Velocity.sqrMagnitude > 0.0001f)
                {
                    directionSum += other.Velocity.normalized;
                    movingNeighbors++;
                }
            }

            if (squaredDistance < m_SeparationDistance * m_SeparationDistance)
            {
                float distance = Mathf.Sqrt(squaredDistance);
                Vector3 away = distance > 0.0001f ? -offset / distance : (i < index ? Vector3.right : Vector3.left);
                separation += away * Mathf.Sqrt(1f - distance / m_SeparationDistance);
            }
        }

        Vector3 cohesion = neighbors > 0 ? centerOffset / (neighbors * m_NeighborRadius) : Vector3.zero;
        Vector3 alignment = movingNeighbors > 0 ? directionSum / movingNeighbors : Vector3.zero;
        return (cohesion * m_CohesionWeight + alignment * m_AlignmentWeight) * approach
            + Vector3.ClampMagnitude(separation, 1f) * m_SeparationWeight;
    }

    private Vector3 LimitClosingSpeed(int index, Vector3 velocity, Vector3 forward, float deltaTime, out bool blocked)
    {
        blocked = false;
        float minSpacing = m_SeparationDistance * 0.8f;
        float checkRadiusSquared = m_SeparationDistance * m_SeparationDistance;
        Vector3 position = m_Enemies[index].Position;
        for (int i = 0; i < m_Enemies.Count; i++)
        {
            if (i == index)
                continue;

            EnemyController other = m_Enemies[i];
            Vector3 offset = (other.Position - position).WithNewY(0f);
            float squaredDistance = offset.sqrMagnitude;
            if (squaredDistance >= checkRadiusSquared || squaredDistance <= 0.00000001f)
                continue;

            float distance = Mathf.Sqrt(squaredDistance);
            Vector3 towardNeighbor = offset / distance;
            blocked |= other.IsHolding && Vector3.Dot(forward, towardNeighbor) > 0.55f;

            // Both agents can approach at once, so each gets at most half the remaining space.
            float allowedSpeed = Mathf.Max(0f, (distance - minSpacing) / (2f * deltaTime));
            float excessSpeed = Vector3.Dot(velocity, towardNeighbor) - allowedSpeed;
            if (excessSpeed > 0f)
                velocity -= towardNeighbor * excessSpeed;
        }

        return velocity;
    }
}
