using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float m_Speed = 5f;
    [SerializeField] private float m_RotationSpeed = 10f;
    [SerializeField] private LayerMask m_FloorMask;

    private CharacterController m_CharacterController;
    private Animator m_Animator;

    private Vector3? m_MovePoint = null;

    private float m_MovementSpeedBlend;

    public Vector3 Velocity => new Vector3(m_CharacterController.velocity.x, 0, m_CharacterController.velocity.z);

    private void Awake()
    {
        m_CharacterController = GetComponent<CharacterController>();
        m_Animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            StopMove();
            ComputeRotation();
        }
        else if (Input.GetMouseButton(0))
        {
            ComputeTarget();
        }

        if (m_MovePoint.HasValue)
        {
            Vector3 remaining = m_MovePoint.Value - transform.position.WithNewY(0f);
            if (remaining.sqrMagnitude > 0.05f * 0.05f)
            {
                Move(remaining);
                return;
            }
        }

        StopMove();
    }

    private void Move(Vector3 remaining)
    {
        var stepSpeed = m_Speed * Time.deltaTime;
        var movement = Vector3.ClampMagnitude(remaining, stepSpeed);

        m_MovementSpeedBlend = Mathf.Lerp(m_MovementSpeedBlend, 1, stepSpeed);
        m_CharacterController.Move(movement);

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(remaining), Time.deltaTime * m_RotationSpeed);
        m_Animator.SetFloat("Speed", m_MovementSpeedBlend);
    }

    private void StopMove()
    {
        m_MovePoint = null;
        m_CharacterController.Move(Vector3.zero);
        m_MovementSpeedBlend = Mathf.Lerp(m_MovementSpeedBlend, 0, Time.deltaTime * m_Speed);
        m_Animator.SetFloat("Speed", m_MovementSpeedBlend);
    }

    private void ComputeTarget()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100, m_FloorMask))
        {
            m_MovePoint = new Vector3(hit.point.x, 0.0f, hit.point.z);
        }
    }

    private void ComputeRotation()
    {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100, m_FloorMask))
        {
            var direction = new Vector3(hit.point.x, 0.0f, hit.point.z) - transform.position.WithNewY(0);
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * m_RotationSpeed);
        }
    }
}
