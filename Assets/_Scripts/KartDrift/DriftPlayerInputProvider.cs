using UnityEngine;

public class DriftPlayerInputProvider : MonoBehaviour
{
    [SerializeField]
    private KartDriftController kart = null;

    [Header("Input Settings")]
    public bool enableKeyboardInput = true;
    public bool enableGamepadInput = true;
    public bool enableDriftTrackDebug = true;

    [Header("Drift Track Debug")]
    public KeyCode debugDriftAngle = KeyCode.F1;
    public KeyCode debugDriftScore = KeyCode.F2;
    public KeyCode resetDriftScore = KeyCode.F3;

    private void Update()
    {
        if (kart == null)
        {
            return;
        }

        // Handle input
        HandleMovementInput();
        HandleDriftInput();
        HandleDebugInput();
    }

    private void HandleMovementInput()
    {
        // Acceleration
        bool isAccelerating = false;
        
        if (enableKeyboardInput)
        {
            isAccelerating = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        }
        
        if (enableGamepadInput)
        {
            isAccelerating = isAccelerating || Input.GetAxis("Vertical") > 0.1f;
        }

        if (isAccelerating)
        {
            kart.Accelerate();
        }

        // Steering
        float horizontalMovement = 0f;
        
        if (enableKeyboardInput)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                horizontalMovement = -1f;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                horizontalMovement = 1f;
        }
        
        if (enableGamepadInput)
        {
            float gamepadInput = Input.GetAxis("Horizontal");
            if (Mathf.Abs(gamepadInput) > 0.1f)
            {
                horizontalMovement = gamepadInput;
            }
        }

        kart.Steer(horizontalMovement);
    }

    private void HandleDriftInput()
    {
        bool isDrifting = false;
        
        if (enableKeyboardInput)
        {
            isDrifting = Input.GetKey(KeyCode.Space);
        }
        
        if (enableGamepadInput)
        {
            isDrifting = isDrifting || Input.GetKey(KeyCode.Joystick1Button0);
        }

        if (isDrifting)
        {
            kart.Jump();
        }
    }

    private void HandleDebugInput()
    {
        if (!enableDriftTrackDebug) return;

        // Debug drift angle
        if (Input.GetKeyDown(debugDriftAngle))
        {
            float angle = kart.GetCurrentDriftAngle();
            Debug.Log($"Current Drift Angle: {angle:F1}°");
        }

        // Debug drift score
        if (Input.GetKeyDown(debugDriftScore))
        {
            float currentScore = kart.GetCurrentDriftScore();
            float totalScore = kart.GetTotalDriftScore();
            int combo = kart.GetDriftCombo();
            Debug.Log($"Drift Score - Current: {currentScore:F0}, Total: {totalScore:F0}, Combo: {combo}");
        }

        // Reset drift score (for testing)
        if (Input.GetKeyDown(resetDriftScore))
        {
            Debug.Log("Drift score reset requested (implement in KartDriftController if needed)");
        }
    }

    // Public methods for external control
    public void SetKart(KartDriftController newKart)
    {
        kart = newKart;
    }

    public KartDriftController GetKart()
    {
        return kart;
    }

    public bool IsDrifting()
    {
        return kart != null && kart.IsDrifting();
    }

    public float GetCurrentDriftAngle()
    {
        return kart != null ? kart.GetCurrentDriftAngle() : 0f;
    }

    public float GetTotalDriftScore()
    {
        return kart != null ? kart.GetTotalDriftScore() : 0f;
    }
} 