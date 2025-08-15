using Cinemachine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class KartController : MonoBehaviour
{
    private PostProcessVolume postVolume;
    private PostProcessProfile postProfile;

    public Transform kartModel;
    public Transform kartNormal;
    public Rigidbody sphere;

    public List<ParticleSystem> primaryParticles = new List<ParticleSystem>();
    public List<ParticleSystem> secondaryParticles = new List<ParticleSystem>();

    float speed, currentSpeed;
    float rotate, currentRotate;
    int driftDirection;
    int driftMode = 0;
    Color c;

    [Header("Bools")]
    public bool drifting;

    [Header("Parameters")]

    public float acceleration = 30f;
    public float steering = 80f;
    public float gravity = 10f;
    public LayerMask layerMask;

    [Header("Drift Smoothing")]
    [Range(0.1f, 10f)]
    public float driftAngleSmoothing = 2f;
    [Range(0.1f, 5f)]
    public float driftRotationSpeed = 1f;
    [Range(0.1f, 3f)]
    public float driftVisualSmoothing = 0.2f;

    [Header("Post Processing")]
    public bool enablePostProcessing = true;
    [Range(0f, 1f)]
    public float chromaticAberrationIntensity = 0.5f;

    [Header("Auto Controls")]
    public bool autoAcceleration = false;
    [Range(0f, 1f)]
    public float autoAccelerationIntensity = 1f;

    [Header("Drift Score System")]
    [SerializeField] private DriftScoreSystem driftScoreSystem;

    [Header("Drift Angle Control")]
    [SerializeField] private float minimumDriftAngle = 15f;
    [SerializeField] private float driftAngleThreshold = 0.8f;
    [SerializeField] private bool enableDriftAngleControl = true;

    [Header("Model Parts")]

    public Transform frontWheels;
    public Transform backWheels;
    public Transform steeringWheel;

    [Header("Particles")]
    public Transform wheelParticles;
    public Transform flashParticles;
    public Color[] turboColors;

    void Start()
    {
        postVolume = Camera.main.GetComponent<PostProcessVolume>();
        postProfile = postVolume.profile;

        for (int i = 0; i < wheelParticles.GetChild(0).childCount; i++)
        {
            primaryParticles.Add(wheelParticles.GetChild(0).GetChild(i).GetComponent<ParticleSystem>());
        }

        for (int i = 0; i < wheelParticles.GetChild(1).childCount; i++)
        {
            primaryParticles.Add(wheelParticles.GetChild(1).GetChild(i).GetComponent<ParticleSystem>());
        }

        foreach(ParticleSystem p in flashParticles.GetComponentsInChildren<ParticleSystem>())
        {
            secondaryParticles.Add(p);
        }

        // Initialize post processing
        if (enablePostProcessing && postProfile != null)
        {
            var chromaticAberration = postProfile.GetSetting<ChromaticAberration>();
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = 0f;
            }
        }
        
        // Setup DriftScoreSystem event subscription
        if (driftScoreSystem != null)
        {
            driftScoreSystem.OnParticleColorChange += UpdateDriftParticles;
        }
    }

    // ZAS: Nullable so that if no command is sent it is clear that no value is set
    private bool? shouldAccelerate = null;
    private float? horizontalMovement = null;
    private bool? shouldJump = null;

    // ZAS: These variables help us identify the one frame when jumping changes... similar to how GetButtonUp behaves
    private bool wasJumpingLastFrame = false;

    // ZAS: Informs the kart to accelerate during the next update call
    public void Accelerate()
    {
        shouldAccelerate = true;
    }

    // ZAS: Informs the kart to jump during the next update call
    public void Jump()
    {
        shouldJump = true;
    }

    // ZAS: Informs the kart to steer during the next update call. Range is -1f to 1f
    public void Steer(float amount)
    {
        horizontalMovement = amount;
    }

    void Update()
    {
        //Follow Collider
        transform.position = sphere.transform.position - new Vector3(0, 0.4f, 0);

        //Accelerate
        bool isAccelerating = shouldAccelerate ?? false;
        
        // Auto acceleration
        if (autoAcceleration)
        {
            speed = acceleration * autoAccelerationIntensity;
        }
        else if (isAccelerating)
        {
            speed = acceleration;
        }

        //Steer
        float horizontalMovementThisFrame = horizontalMovement ?? 0f;
        if (horizontalMovementThisFrame != 0 && !drifting)
        {
            int dir = horizontalMovementThisFrame > 0 ? 1 : -1;
            float amount = Mathf.Abs((horizontalMovementThisFrame));
            Steer(dir, amount);
        }

        //Drift
        bool isJumping = shouldJump ?? false;
        bool jumpStateChangedThisFrame = isJumping != wasJumpingLastFrame;
        bool startedJumpingThisFrame = jumpStateChangedThisFrame && isJumping == true;
        
        // Check if drift should start (with angle control)
        if (startedJumpingThisFrame && !drifting && horizontalMovementThisFrame != 0)
        {
            // Check if drift angle is valid
            if (enableDriftAngleControl && !IsValidDriftAngle(horizontalMovementThisFrame))
            {
                // Drift angle too small, don't start drift
                Debug.Log("Drift angle too small, drift not started");
            }
            else
            {
                // Valid drift angle, start drift
                drifting = true;
                driftDirection = horizontalMovementThisFrame > 0 ? 1 : -1;

                kartModel.parent.DOComplete();

                // Start drift scoring
                if (driftScoreSystem != null)
                {
                    driftScoreSystem.StartDrift();
                }
            }
        }

        if (drifting)
        {
            float control = (driftDirection == 1) ? ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, 0, 2) : ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, 2, 0);
            float powerControl = (driftDirection == 1) ? ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, .2f, 1) : ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, 1, .2f);
            
            // Drift steering - kartın yönünü değiştirir
            float driftSteering = horizontalMovementThisFrame * steering * 0.5f; // Drift sırasında daha yumuşak steering
            Steer(horizontalMovementThisFrame > 0 ? 1 : -1, Mathf.Abs(horizontalMovementThisFrame));

            // Update drift score
            if (driftScoreSystem != null)
            {
                float currentSpeed = sphere.velocity.magnitude;
                float controlValue = Mathf.Clamp01(Mathf.Abs(horizontalMovementThisFrame));
                driftScoreSystem.UpdateDriftScore(currentSpeed, controlValue);
            }
        }

        bool stoppedJumpingThisFrame = jumpStateChangedThisFrame && isJumping == false;
        if (stoppedJumpingThisFrame && drifting)
        {
            Boost();
            
            // End drift scoring
            if (driftScoreSystem != null)
            {
                driftScoreSystem.EndDrift();
            }
        }

        currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * 12f); speed = 0f;
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * 4f); rotate = 0f;

        //Animations    

        //a) Kart
        if (!drifting)
        {
            kartModel.localEulerAngles = Vector3.Lerp(kartModel.localEulerAngles, new Vector3(0, 90 + (horizontalMovementThisFrame * 15), kartModel.localEulerAngles.z), driftVisualSmoothing);
        }
        else
        {
            float control = (driftDirection == 1) ? ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, .5f, 2) : ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, 2, .5f);
            float targetDriftAngle = (control * 15) * driftDirection;
            float smoothedAngle = Mathf.LerpAngle(kartModel.parent.localEulerAngles.y, targetDriftAngle, Time.deltaTime * driftRotationSpeed);
            kartModel.parent.localRotation = Quaternion.Euler(0, smoothedAngle, 0);
        }

        //b) Wheels
        frontWheels.localEulerAngles = new Vector3(0, (horizontalMovementThisFrame * 15), frontWheels.localEulerAngles.z);
        frontWheels.localEulerAngles += new Vector3(0, 0, sphere.velocity.magnitude/2);
        backWheels.localEulerAngles += new Vector3(0, 0, sphere.velocity.magnitude/2);

        //c) Steering Wheel
        steeringWheel.localEulerAngles = new Vector3(-25, 90, ((horizontalMovementThisFrame * 45)));

        // ZAS: Clear command states after use
        shouldAccelerate = null;
        horizontalMovement = null;
        shouldJump = null;

        // ZAS: Store the jump state each frame for reference in the next frame to detect changes in state
        wasJumpingLastFrame = isJumping;
    }

    private void FixedUpdate()
    {
        //Forward Acceleration
        if(!drifting)
            sphere.AddForce(-kartModel.transform.right * currentSpeed, ForceMode.Acceleration);
        else
            sphere.AddForce(transform.forward * currentSpeed, ForceMode.Acceleration);

        //Gravity
        sphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

        //Steering
        float steeringSmoothing = drifting ? driftAngleSmoothing : 5f;
        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(0, transform.eulerAngles.y + currentRotate, 0), Time.deltaTime * steeringSmoothing);

        RaycastHit hitOn;
        RaycastHit hitNear;

        Physics.Raycast(transform.position + (transform.up*.1f), Vector3.down, out hitOn, 1.1f,layerMask);
        Physics.Raycast(transform.position + (transform.up * .1f)   , Vector3.down, out hitNear, 2.0f, layerMask);

        //Normal Rotation
        kartNormal.up = Vector3.Lerp(kartNormal.up, hitNear.normal, Time.deltaTime * 8.0f);
        kartNormal.Rotate(0, transform.eulerAngles.y, 0);
    }

    public void Boost()
    {
        drifting = false;

        if (driftMode > 0)
        {
            DOVirtual.Float(currentSpeed * 3, currentSpeed, .3f * driftMode, Speed);
            
            // Post processing effect only if enabled
            if (enablePostProcessing)
            {
                DOVirtual.Float(0, chromaticAberrationIntensity, .5f, ChromaticAmount).OnComplete(() => DOVirtual.Float(chromaticAberrationIntensity, 0, .5f, ChromaticAmount));
            }
            
            kartModel.Find("Tube001").GetComponentInChildren<ParticleSystem>().Play();
            kartModel.Find("Tube002").GetComponentInChildren<ParticleSystem>().Play();
        }

        driftMode = 0;

        kartModel.parent.DOLocalRotate(Vector3.zero, .5f).SetEase(Ease.OutBack);
    }

    public void Steer(int direction, float amount)
    {
        rotate = (steering * direction) * amount;
    }

    // New method to check if drift angle is valid
    private bool IsValidDriftAngle(float horizontalInput)
    {
        if (!enableDriftAngleControl) return true;
        
        // Calculate the drift angle based on horizontal input
        float driftAngle = Mathf.Abs(horizontalInput) * minimumDriftAngle;
        
        // Check if the angle meets the threshold
        return driftAngle >= (minimumDriftAngle * driftAngleThreshold);
    }

    // New method to handle particle color changes from DriftScoreSystem
    private void UpdateDriftParticles(DriftScoreSystem.DriftLevel level)
    {
        // This method is called by DriftScoreSystem when level changes
        // The actual particle color update is now handled by DriftScoreSystem
        // This method can be used for additional kart-specific effects if needed
        
        // Example: Update kart model effects based on level
        switch (level)
        {
            case DriftScoreSystem.DriftLevel.Bronze:
                // Bronze level effects
                break;
            case DriftScoreSystem.DriftLevel.Silver:
                // Silver level effects
                break;
            case DriftScoreSystem.DriftLevel.Golden:
                // Golden level effects
                break;
            case DriftScoreSystem.DriftLevel.Diamond:
                // Diamond level effects
                break;
        }
    }

    void PlayFlashParticle(Color c)
    {
        GameObject.Find("CM vcam1").GetComponent<CinemachineImpulseSource>().GenerateImpulse();

        foreach (ParticleSystem p in secondaryParticles)
        {
            var pmain = p.main;
            pmain.startColor = c;
            p.Play();
        }
    }

    private void Speed(float x)
    {
        currentSpeed = x;
    }

    void ChromaticAmount(float x)
    {
        if (enablePostProcessing && postProfile != null)
        {
            var chromaticAberration = postProfile.GetSetting<ChromaticAberration>();
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = x;
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from DriftScoreSystem events
        if (driftScoreSystem != null)
        {
            driftScoreSystem.OnParticleColorChange -= UpdateDriftParticles;
        }
    }
}
