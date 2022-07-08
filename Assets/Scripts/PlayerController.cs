using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public FrameInput Input { get; private set; }
    public Vector3 Velocity { get; private set; }
    public Vector3 RawMovement { get; private set; }
    
    public bool Grounded => m_ColDown;
    public bool LandingThisFrame { get; private set; }
    public bool JumpingThisFrame { get; private set; }

    private Vector3 m_LastPosition;
    private float m_CurrentHorizontalSpeed, m_CurrentVerticalSpeed;

    private readonly NetworkVariable<float> m_XFromServer = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> m_JumpUpFromServer = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> m_JumpDownFromServer = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Owner);

    private bool m_Active;
    private void Activate() => m_Active = true;
    private void Awake() => Invoke(nameof(Activate), 0.5f);
    

    private void Update()
    {
        if (!m_Active) return;

        var currentPos = transform.position;
        Velocity = (currentPos - m_LastPosition) / Time.deltaTime;
        m_LastPosition = currentPos;

        GatherInput();
        RunCollisionChecks();

        CalculateWalk(); // Horizontal movement
        CalculateJumpApex(); // Affects fall speed, so calculate before gravity
        CalculateGravity(); // Vertical movement
        CalculateJump(); // Possibly overrides vertical

        MoveCharacter(); // Actually perform the axis movement
    }

    #region Gather Input
    private void GatherInput()
    {
        if (IsOwner)
            GatherInputFromOwner();
        else
            GatherInputFromServer();
    }

    private void GatherInputFromOwner()
    {
        Input = new FrameInput
        {
            JumpDown = UnityEngine.Input.GetButtonDown("Jump"),
            JumpUp = UnityEngine.Input.GetButtonUp("Jump"),
            X = UnityEngine.Input.GetAxisRaw("Horizontal")
        };
        
        if (Input.JumpDown)
            m_LastJumpPressed = Time.time;

        m_XFromServer.Value = Input.X;
        m_JumpUpFromServer.Value = Input.JumpUp;
        m_JumpDownFromServer.Value = Input.JumpDown;
    }

    private void GatherInputFromServer()
    {
        Input = new FrameInput
        {
            JumpDown = m_JumpDownFromServer.Value,
            JumpUp = m_JumpUpFromServer.Value,
            X = m_XFromServer.Value
        };
        
        if (Input.JumpDown)
            m_LastJumpPressed = Time.time;
    }
    
    #endregion
    
    #region Collisions
    [Header("COLLISION")] 
    [SerializeField] private int m_DetectorCount = 3;
    [SerializeField] private LayerMask m_GroundLayer;
    [SerializeField] private Bounds m_CharacterBounds;
    [SerializeField] private float m_DetectionRayLength = 0.1f;
    [SerializeField] private bool m_ColUp, m_ColRight, m_ColDown, m_ColLeft;
    [SerializeField] [Range(0.1f, 0.3f)] private float m_RayBuffer = 0.1f; // Prevents side detectors hitting the ground

    private float m_TimeLeftGrounded;
    private RayRange m_RaysUp, m_RaysRight, m_RaysDown, m_RaysLeft;

    private void RunCollisionChecks()
    {
        CalculateRays();

        LandingThisFrame = false;
        var groundedCheck = RunDetection(m_RaysDown);
        if (m_ColDown && !groundedCheck) 
            m_TimeLeftGrounded = Time.time; // Only trigger when first leaving
        else if (!m_ColDown && groundedCheck)
        {
            m_CoyoteUsable = true; // Only trigger when first touching
            LandingThisFrame = true;
        }

        m_ColDown = groundedCheck;
        m_ColUp = RunDetection(m_RaysUp);
        m_ColLeft = RunDetection(m_RaysLeft);
        m_ColRight = RunDetection(m_RaysRight);

        bool RunDetection(RayRange range)
        {
            return EvaluateRayPositions(range).Any(point => Physics2D.Raycast(point, range.Dir, m_DetectionRayLength, m_GroundLayer));
        }
    }

    private void CalculateRays()
    {
        var bounds = new Bounds(transform.position, m_CharacterBounds.size);
        m_RaysUp = new RayRange(bounds.min.x + m_RayBuffer, bounds.max.y, bounds.max.x - m_RayBuffer, bounds.max.y, Vector2.up);
        m_RaysDown = new RayRange(bounds.min.x + m_RayBuffer, bounds.min.y, bounds.max.x - m_RayBuffer, bounds.min.y, Vector2.down);
        m_RaysLeft = new RayRange(bounds.min.x, bounds.min.y + m_RayBuffer, bounds.min.x, bounds.max.y - m_RayBuffer, Vector2.left);
        m_RaysRight = new RayRange(bounds.max.x, bounds.min.y + m_RayBuffer, bounds.max.x, bounds.max.y - m_RayBuffer, Vector2.right);
    }


    private IEnumerable<Vector2> EvaluateRayPositions(RayRange range)
    {
        for (var i = 0; i < m_DetectorCount; i++)
        {
            var t = (float)i / (m_DetectorCount - 1);
            yield return Vector2.Lerp(range.Start, range.End, t);
        }
    }

    private void OnDrawGizmos()
    {
        // Bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + m_CharacterBounds.center, m_CharacterBounds.size);

        // Rays
        if (!Application.isPlaying)
        {
            CalculateRays();
            Gizmos.color = Color.blue;
            foreach (var range in new List<RayRange> { m_RaysUp, m_RaysRight, m_RaysDown, m_RaysLeft })
            {
                foreach (var point in EvaluateRayPositions(range))
                {
                    Gizmos.DrawRay(point, range.Dir * m_DetectionRayLength);
                }
            }
        }

        if (!Application.isPlaying) return;

        // Draw the future position. Handy for visualizing gravity
        Gizmos.color = Color.red;
        var move = new Vector3(m_CurrentHorizontalSpeed, m_CurrentVerticalSpeed) * Time.deltaTime;
        Gizmos.DrawWireCube(transform.position + move, m_CharacterBounds.size);
    }
    #endregion

    #region Walk
    [Header("WALKING")] 
    [SerializeField] private float m_ApexBonus = 2f;
    [SerializeField] private float m_MoveClamp = 13f;
    [SerializeField] private float m_Acceleration = 90f;
    [SerializeField] private float m_DeAcceleration = 60f;
    
    private void CalculateWalk()
    {
        if (Input.X != 0)
        {
            // Set horizontal move speed
            m_CurrentHorizontalSpeed += Input.X * m_Acceleration * Time.deltaTime;

            // clamped by max frame movement
            m_CurrentHorizontalSpeed = Mathf.Clamp(m_CurrentHorizontalSpeed, -m_MoveClamp, m_MoveClamp);

            // Apply bonus at the apex of a jump
            var apexBonus = Mathf.Sign(Input.X) * m_ApexBonus * m_ApexPoint;
            m_CurrentHorizontalSpeed += apexBonus * Time.deltaTime;
        }
        else
        {
            // No input. Let's slow the character down
            m_CurrentHorizontalSpeed = Mathf.MoveTowards(m_CurrentHorizontalSpeed, 0, m_DeAcceleration * Time.deltaTime);
        }

        if (m_CurrentHorizontalSpeed > 0 && m_ColRight || m_CurrentHorizontalSpeed < 0 && m_ColLeft)
            m_CurrentHorizontalSpeed = 0;
    }
    #endregion

    #region Gravity
    [Header("GRAVITY")] 
    [SerializeField] private float m_FallClamp = -40f;
    [SerializeField] private float m_MinFallSpeed = 80f;
    [SerializeField] private float m_MaxFallSpeed = 120f;
    
    private float m_FallSpeed;

    private void CalculateGravity()
    {
        if (m_ColDown)
        {
            // Move out of the ground
            if (m_CurrentVerticalSpeed < 0) m_CurrentVerticalSpeed = 0;
        }
        else
        {
            // Add downward force while ascending if we ended the jump early
            var fallSpeed = m_EndedJumpEarly && m_CurrentVerticalSpeed > 0
                ? m_FallSpeed * m_JumpEndEarlyGravityModifier
                : m_FallSpeed;

            // Fall
            m_CurrentVerticalSpeed -= fallSpeed * Time.deltaTime;

            if (m_CurrentVerticalSpeed < m_FallClamp) 
                m_CurrentVerticalSpeed = m_FallClamp;
        }
    }
    #endregion

    #region Jump
    [Header("JUMPING")] 
    [SerializeField] private float m_JumpBuffer = 0.1f;
    [SerializeField] private float m_JumpHeight = 30f;
    [SerializeField] private float m_JumpApexThreshold = 10f;
    [SerializeField] private float m_CoyoteTimeThreshold = 0.1f;
    [SerializeField] private float m_JumpEndEarlyGravityModifier = 3;
    
    private bool m_CoyoteUsable;
    private bool m_EndedJumpEarly = true;
    private float m_ApexPoint; // Becomes 1 at the apex of a jump
    private float m_LastJumpPressed;
    
    private bool HasBufferedJump => m_ColDown && m_LastJumpPressed + m_JumpBuffer > Time.time;
    private bool CanUseCoyote => m_CoyoteUsable && !m_ColDown && m_TimeLeftGrounded + m_CoyoteTimeThreshold > Time.time;

    private void CalculateJumpApex()
    {
        if (!m_ColDown)
        {
            // Gets stronger the closer to the top of the jump
            m_ApexPoint = Mathf.InverseLerp(m_JumpApexThreshold, 0, Mathf.Abs(Velocity.y));
            m_FallSpeed = Mathf.Lerp(m_MinFallSpeed, m_MaxFallSpeed, m_ApexPoint);
        }
        else
            m_ApexPoint = 0;
    }

    private void CalculateJump()
    {
        // Jump if: grounded or within coyote threshold || sufficient jump buffer
        if (Input.JumpDown && CanUseCoyote || HasBufferedJump)
        {
            m_CurrentVerticalSpeed = m_JumpHeight;
            m_EndedJumpEarly = false;
            m_CoyoteUsable = false;
            m_TimeLeftGrounded = float.MinValue;
            JumpingThisFrame = true;
        }
        else
            JumpingThisFrame = false;

        // End the jump early if button released
        if (!m_ColDown && Input.JumpUp && !m_EndedJumpEarly && Velocity.y > 0)
            m_EndedJumpEarly = true;

        if (m_ColUp && (m_CurrentVerticalSpeed > 0))
            m_CurrentVerticalSpeed = 0;
    }
    #endregion

    #region Move
    [Header("MOVE")]
    [SerializeField, Tooltip("Raising this value increases collision accuracy at the cost of performance.")]
    private int m_FreeColliderIterations = 10;

    // We cast our bounds before moving to avoid future collisions
    private void MoveCharacter()
    {
        var pos = transform.position;
        RawMovement = new Vector3(m_CurrentHorizontalSpeed, m_CurrentVerticalSpeed); // Used externally
        var move = RawMovement * Time.deltaTime;
        var furthestPoint = pos + move;

        // Check furthest movement. If nothing hit, move and don't do extra checks
        var hit = Physics2D.OverlapBox(furthestPoint, m_CharacterBounds.size, 0, m_GroundLayer);
        if (!hit)
        {
            transform.position += move;
            return;
        }

        // otherwise increment away from current pos; see what closest position we can move to
        var positionToMoveTo = transform.position;
        for (int i = 1; i < m_FreeColliderIterations; i++)
        {
            // increment to check all but furthestPoint - we did that already
            var t = (float)i / m_FreeColliderIterations;
            var posToTry = Vector2.Lerp(pos, furthestPoint, t);

            if (Physics2D.OverlapBox(posToTry, m_CharacterBounds.size, 0, m_GroundLayer))
            {
                transform.position = positionToMoveTo;

                // We've landed on a corner or hit our head on a ledge. Nudge the player gently
                if (i == 1)
                {
                    if (m_CurrentVerticalSpeed < 0) m_CurrentVerticalSpeed = 0;
                    var dir = transform.position - hit.transform.position;
                    transform.position += dir.normalized * move.magnitude;
                }
                return;
            }
            positionToMoveTo = posToTry;
        }
    }
    #endregion
}

public struct FrameInput
{
    public float X, Y;
    public bool JumpDown;
    public bool JumpUp;
}

public struct RayRange
{
    public readonly Vector2 Start, End, Dir;
    
    public RayRange(float x1, float y1, float x2, float y2, Vector2 dir)
    {
        Dir = dir;
        Start = new Vector2(x1, y1);
        End = new Vector2(x2, y2);
    }
}