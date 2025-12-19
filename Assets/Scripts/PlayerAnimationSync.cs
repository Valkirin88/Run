using Unity.Netcode;
using UnityEngine;

public class PlayerAnimationSync : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    
    private NetworkVariable<bool> isGrounded = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    
    private NetworkVariable<bool> isJumping = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );
    
    private NetworkVariable<float> horizontalSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private Rigidbody rb;
    
    // Animation parameter names
    private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
    private static readonly int IsJumpingParam = Animator.StringToHash("IsJumping");
    private static readonly int HorizontalSpeedParam = Animator.StringToHash("HorizontalSpeed");
    private static readonly int VerticalSpeedParam = Animator.StringToHash("VerticalSpeed");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to network variable changes
        isGrounded.OnValueChanged += OnIsGroundedChanged;
        isJumping.OnValueChanged += OnIsJumpingChanged;
        horizontalSpeed.OnValueChanged += OnHorizontalSpeedChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        isGrounded.OnValueChanged -= OnIsGroundedChanged;
        isJumping.OnValueChanged -= OnIsJumpingChanged;
        horizontalSpeed.OnValueChanged -= OnHorizontalSpeedChanged;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Update network variables based on local state
        if (rb != null)
        {
            horizontalSpeed.Value = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
            
            // Update animator for local player
            UpdateAnimator();
        }
    }

    public void SetGrounded(bool grounded)
    {
        if (IsOwner)
        {
            isGrounded.Value = grounded;
        }
    }

    public void SetJumping(bool jumping)
    {
        if (IsOwner)
        {
            isJumping.Value = jumping;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool(IsGroundedParam, isGrounded.Value);
        animator.SetBool(IsJumpingParam, isJumping.Value);
        animator.SetFloat(HorizontalSpeedParam, horizontalSpeed.Value);
        
        if (rb != null)
        {
            animator.SetFloat(VerticalSpeedParam, rb.linearVelocity.y);
        }
    }

    private void OnIsGroundedChanged(bool previous, bool current)
    {
        UpdateAnimator();
    }

    private void OnIsJumpingChanged(bool previous, bool current)
    {
        UpdateAnimator();
    }

    private void OnHorizontalSpeedChanged(float previous, float current)
    {
        UpdateAnimator();
    }
}

