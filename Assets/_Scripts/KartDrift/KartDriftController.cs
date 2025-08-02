using Cinemachine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class KartDriftController : MonoBehaviour
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
    float smoothedHorizontalInput = 0f;
    int driftDirection;
    float driftPower;
    int driftMode = 0;
    bool first, second, third;
    Color c;

    [Header("Bools")]
    public bool drifting;

    [Header("Parameters")]
    public float acceleration = 30f;
    public float steering = 80f;
    public float gravity = 10f;
    public LayerMask layerMask;

    [Header("Steering Smoothing")]
    [Range(0.1f, 10f)]
    public float normalSteeringSmoothing = 3f;
    [Range(0.1f, 10f)]
    public float driftSteeringSmoothing = 2f;
    [Range(0.1f, 5f)]
    public float steeringResponseSpeed = 2f;
    
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

    [Header("Drift Track System")]
    public bool enableDriftTracking = true;
    [Range(0f, 180f)]
    public float minDriftAngle = 15f;
    [Range(0f, 1000f)]
    public float driftScoreMultiplier = 100f;
    public bool showDriftAngle = true;
    public bool showDriftScore = true;

    [Header("Model Parts")]
    public Transform frontWheels;
    public Transform backWheels;
    public Transform steeringWheel;

    [Header("Particles")]
    public Transform wheelParticles;
    public Transform flashParticles;
    public Color[] turboColors;

    // Drift Track Variables
    private float currentDriftAngle = 0f;
    private float driftStartTime = 0f;
    private float totalDriftTime = 0f;
    private float driftDistance = 0f;
    private Vector3 driftStartPosition;
    private float currentDriftScore = 0f;
    private float totalDriftScore = 0f;
    private int driftCombo = 0;
    private bool isInDriftZone = false;

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
        
        // Smooth horizontal input
        smoothedHorizontalInput = Mathf.Lerp(smoothedHorizontalInput, horizontalMovementThisFrame, Time.deltaTime * steeringResponseSpeed);
        
        if (smoothedHorizontalInput != 0 && !drifting)
        {
            int dir = smoothedHorizontalInput > 0 ? 1 : -1;
            float amount = Mathf.Abs(smoothedHorizontalInput);
            Steer(dir, amount);
        }

        //Drift
        bool isJumping = shouldJump ?? false;
        bool jumpStateChangedThisFrame = isJumping != wasJumpingLastFrame;
        bool startedJumpingThisFrame = jumpStateChangedThisFrame && isJumping == true;
        if (startedJumpingThisFrame && !drifting && horizontalMovementThisFrame != 0)
        {
            StartDrift();
        }

        if (drifting)
        {
            UpdateDrift(horizontalMovementThisFrame);
        }

        bool stoppedJumpingThisFrame = jumpStateChangedThisFrame && isJumping == false;
        if (stoppedJumpingThisFrame && drifting)
        {
            EndDrift();
        }

        currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * 12f); speed = 0f;
        
        // Smooth rotation - daha yumuşak dönüş için
        float rotationSmoothing = drifting ? driftSteeringSmoothing : normalSteeringSmoothing;
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * rotationSmoothing);

        //Animations    
        UpdateAnimations(horizontalMovementThisFrame);

        // ZAS: Clear command states after use
        shouldAccelerate = null;
        horizontalMovement = null;
        shouldJump = null;

        // ZAS: Store the jump state each frame for reference in the next frame to detect changes in state
        wasJumpingLastFrame = isJumping;
    }

    private void StartDrift()
    {
        drifting = true;
        driftDirection = (horizontalMovement ?? 0f) > 0 ? 1 : -1;
        driftStartTime = Time.time;
        driftStartPosition = transform.position;
        currentDriftScore = 0f;

        foreach (ParticleSystem p in primaryParticles)
        {
            p.startColor = Color.clear;
            p.Play();
        }

        kartModel.parent.DOComplete();
        // Zıplama efekti kaldırıldı
    }

    private void UpdateDrift(float horizontalMovementThisFrame)
    {
        float control = (driftDirection == 1) ? ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, 0, 2) : ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, 2, 0);
        float powerControl = (driftDirection == 1) ? ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, .2f, 1) : ExtensionMethods.Remap(horizontalMovementThisFrame, -1, 1, 1, .2f);
        
        // Drift steering - kartın yönünü değiştirir
        float driftSteering = horizontalMovementThisFrame * steering * 0.5f; // Drift sırasında daha yumuşak steering
        Steer(horizontalMovementThisFrame > 0 ? 1 : -1, Mathf.Abs(horizontalMovementThisFrame));
        
        driftPower += powerControl;

        // Update drift tracking
        if (enableDriftTracking)
        {
            UpdateDriftTracking();
        }

        ColorDrift();
    }

    private void UpdateDriftTracking()
    {
        // Calculate drift angle
        Vector3 velocity = sphere.velocity;
        Vector3 forward = transform.forward;
        currentDriftAngle = Vector3.Angle(velocity.normalized, forward.normalized);

        // Update drift time
        totalDriftTime = Time.time - driftStartTime;

        // Update drift distance
        driftDistance = Vector3.Distance(transform.position, driftStartPosition);

        // Calculate drift score
        if (currentDriftAngle >= minDriftAngle)
        {
            float angleScore = currentDriftAngle * 0.5f;
            float timeScore = totalDriftTime * 10f;
            float distanceScore = driftDistance * 5f;
            
            currentDriftScore = (angleScore + timeScore + distanceScore) * driftScoreMultiplier * Time.deltaTime;
            totalDriftScore += currentDriftScore;
        }
    }

    private void EndDrift()
    {
        if (enableDriftTracking && currentDriftAngle >= minDriftAngle)
        {
            driftCombo++;
            Debug.Log($"Drift Ended! Angle: {currentDriftAngle:F1}°, Time: {totalDriftTime:F2}s, Distance: {driftDistance:F1}m, Score: {currentDriftScore:F0}");
        }

        Boost();
    }

    private void UpdateAnimations(float horizontalMovementThisFrame)
    {
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

        //Steering - daha yumuşak dönüş için
        float steeringSmoothing = drifting ? driftSteeringSmoothing : normalSteeringSmoothing;
        Vector3 targetRotation = new Vector3(0, transform.eulerAngles.y + currentRotate, 0);
        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, targetRotation, Time.deltaTime * steeringSmoothing);

        RaycastHit hitOn;
        RaycastHit hitNear;

        Physics.Raycast(transform.position + (transform.up*.1f), Vector3.down, out hitOn, 1.1f,layerMask);
        Physics.Raycast(transform.position + (transform.up * .1f)   , Vector3.down, out hitNear, 2.0f,layerMask);

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

        driftPower = 0;
        driftMode = 0;
        first = false; second = false; third = false;

        foreach (ParticleSystem p in primaryParticles)
        {
            p.startColor = Color.clear;
            p.Stop();
        }

        kartModel.parent.DOLocalRotate(Vector3.zero, .5f).SetEase(Ease.OutBack);
    }

    public void Steer(int direction, float amount)
    {
        // Direct steering - rotate değişkenini direkt set et
        rotate = (steering * direction) * amount;
    }

    public void ColorDrift()
    {
        if(!first)
            c = Color.clear;

        if (driftPower > 50 && driftPower < 100-1 && !first)
        {
            first = true;
            c = turboColors[0];
            driftMode = 1;

            PlayFlashParticle(c);
        }

        if (driftPower > 100 && driftPower < 150- 1 && !second)
        {
            second = true;
            c = turboColors[1];
            driftMode = 2;

            PlayFlashParticle(c);
        }

        if (driftPower > 150 && !third)
        {
            third = true;
            c = turboColors[2];
            driftMode = 3;

            PlayFlashParticle(c);
        }

        foreach (ParticleSystem p in primaryParticles)
        {
            var pmain = p.main;
            pmain.startColor = c;
        }

        foreach(ParticleSystem p in secondaryParticles)
        {
            var pmain = p.main;
            pmain.startColor = c;
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

    // Drift Track Public Methods
    public float GetCurrentDriftAngle() => currentDriftAngle;
    public float GetTotalDriftTime() => totalDriftTime;
    public float GetDriftDistance() => driftDistance;
    public float GetCurrentDriftScore() => currentDriftScore;
    public float GetTotalDriftScore() => totalDriftScore;
    public int GetDriftCombo() => driftCombo;
    public bool IsDrifting() => drifting;

    private void OnDrawGizmos()
    {
        if (showDriftAngle && drifting)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, sphere.velocity.normalized * 5f);
        }
    }
} 