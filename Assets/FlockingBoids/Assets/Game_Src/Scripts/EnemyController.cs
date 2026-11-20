using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyController : MonoBehaviour
{
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private Animator m_Animator;
    private float m_HoldDistance;
    private float m_TimeOutside;

    public Vector3 Velocity { get; private set; }
    public Vector3 Position => transform.position;
    public bool IsHolding { get; private set; }

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

    public bool ShouldPursue(float distance, float acceptanceRadius, float margin, float delay, float deltaTime)
    {
        if (distance <= acceptanceRadius)
        {
            HoldPosition(0f);
            return false;
        }

        if (!IsHolding)
            return true;

        float resumeDistance = Mathf.Max(acceptanceRadius, m_HoldDistance) + margin;
        if (distance <= resumeDistance)
        {
            m_TimeOutside = 0f;
            return false;
        }

        m_TimeOutside += deltaTime;
        if (m_TimeOutside < delay)
            return false;

        IsHolding = false;
        return true;
    }

    public void HoldPosition(float distance)
    {
        IsHolding = true;
        m_HoldDistance = distance;
        m_TimeOutside = 0f;
    }

    private void OnDisable()
    {
        IsHolding = false;
        Velocity = Vector3.zero;
    }

    public void Move(Vector3 velocity, Vector3 target, float maxSpeed, float rotationSpeed, float deltaTime)
    {
        Velocity = velocity;
        transform.position += velocity * deltaTime;

        Vector3 direction = velocity.sqrMagnitude > 0.0001f
            ? velocity
            : (target - Position).WithNewY(0f);
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, rotationSpeed * deltaTime);
        }

        float animationSpeed = maxSpeed > 0f ? velocity.magnitude / maxSpeed : 0f;
        m_Animator.SetFloat(SpeedParameter, animationSpeed, 0.1f, deltaTime);
    }
}
