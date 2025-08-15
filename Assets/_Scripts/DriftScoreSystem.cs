using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DriftScoreSystem : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField] private float baseScorePerSecond = 10f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float controlMultiplier = 2f;
    [SerializeField] private float comboMultiplier = 1.2f;
    
    [Header("Drift Levels")]
    [SerializeField] private int silverDriftThreshold = 1000;
    [SerializeField] private int goldenDriftThreshold = 5000;
    [SerializeField] private int diamondDriftThreshold = 15000;
    
    [Header("Particle Control")]
    [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
    [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color goldenColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color diamondColor = new Color(0.5f, 0.8f, 1f);
    [SerializeField] private bool enableParticleControl = true;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip scoreTickSound;
    
    // Private variables
    private float currentScore = 0f;
    private float totalScore = 0f;
    private float driftStartTime;
    private float lastScoreTime;
    private int comboCount = 0;
    private bool isDrifting = false;
    //summary: This is the list of particles that will be used to display the drift score
    [SerializeField] private List<ParticleSystem> driftParticles;
    //summary: This is the list of particles that will be used to display the drift score
    [SerializeField] private KartController kartController;
    
    // Drift levels
    public enum DriftLevel
    {
        Bronze,
        Silver,
        Golden,
        Diamond
    }
    
    private DriftLevel currentLevel = DriftLevel.Bronze;
    
    // Events
    public System.Action<DriftLevel> OnLevelUp;
    public System.Action<DriftLevel> OnParticleColorChange;
    public System.Action<float> OnScoreChanged;
    public System.Action OnDriftUIStart;
    public System.Action OnDriftUIEnd;
    public System.Action<float> OnCurrentScoreChanged;  // Current score during drift
    
    private void Start()
    {
        // Find KartController and get particle references automatically
        kartController = GetComponent<KartController>();
        if (kartController == null)
        {
            kartController = FindObjectOfType<KartController>();
        }
        
        if (kartController != null)
        {
            // Get wheelParticles transform from KartController
            var wheelParticlesField = typeof(KartController).GetField("wheelParticles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (wheelParticlesField != null)
            {
                Transform wheelParticles = wheelParticlesField.GetValue(kartController) as Transform;
                if (wheelParticles != null)
                {
                    // Get all particle systems from wheelParticles and its children
                    driftParticles = new List<ParticleSystem>();
                    
                    // Get particles from wheelParticles itself
                    ParticleSystem[] particles = wheelParticles.GetComponentsInChildren<ParticleSystem>();
                    driftParticles.AddRange(particles);
                    
                    Debug.Log($"DriftScoreSystem: Found {driftParticles.Count} particles from wheelParticles");
                }
            }
        }
        
        // Initialize with events
        OnScoreChanged?.Invoke(totalScore);
        
        // Initialize particle colors
        if (enableParticleControl)
        {
            UpdateParticleColors(DriftLevel.Bronze);
        }
    }
    
    public void StartDrift()
    {
        if (!isDrifting)
        {
            isDrifting = true;
            driftStartTime = Time.time;
            lastScoreTime = Time.time;
            comboCount = 0;
            
            // Reset current drift score
            currentScore = 0f;
            
            // Start particles
            if (enableParticleControl)
            {
                StartDriftParticles();
            }
            
            // Trigger UI start event
            OnDriftUIStart?.Invoke();
            
            Debug.Log("Drift Started!");
        }
    }
    
    public void EndDrift()
    {
        if (isDrifting)
        {
            isDrifting = false;
            
            // Add current score to total
            totalScore += currentScore;
            
            // Check for level up
            CheckLevelUp();
            
            // Stop particles
            if (enableParticleControl)
            {
                StopDriftParticles();
            }
            
            // Trigger score changed event
            OnScoreChanged?.Invoke(totalScore);
            
            // Trigger UI end event
            OnDriftUIEnd?.Invoke();
            
            // Reset current score
            currentScore = 0f;
            
            Debug.Log($"Drift Ended! Score: {currentScore}, Total: {totalScore}");
        }
    }
    
    public void UpdateDriftScore(float speed, float control)
    {
        if (!isDrifting) return;
        
        float timeSinceLastScore = Time.time - lastScoreTime;
        
        // Base score calculation
        float baseScore = baseScorePerSecond * timeSinceLastScore;
        
        // Speed multiplier (higher speed = more points)
        float speedBonus = Mathf.Clamp01(speed / 30f) * speedMultiplier;
        
        // Control multiplier (better control = more points)
        float controlBonus = Mathf.Clamp01(control) * controlMultiplier;
        
        // Combo multiplier
        float comboBonus = 1f + (comboCount * comboMultiplier);
        
        // Calculate final score for this frame
        float frameScore = baseScore * (1f + speedBonus + controlBonus) * comboBonus;
        
        currentScore += frameScore;
        
        // Update combo
        if (control > 0.7f) // Good control increases combo
        {
            comboCount++;
        }
        else if (control < 0.3f) // Poor control resets combo
        {
            comboCount = 0;
        }
        
        lastScoreTime = Time.time;
        
        // Trigger current score changed event (for UI during drift)
        OnCurrentScoreChanged?.Invoke(currentScore);
        
        // Play score tick sound
        if (audioSource != null && scoreTickSound != null && Time.time % 0.5f < 0.1f)
        {
            audioSource.PlayOneShot(scoreTickSound, 0.3f);
        }
    }
    
    private void CheckLevelUp()
    {
        DriftLevel newLevel = currentLevel;
        
        if (totalScore >= diamondDriftThreshold && currentLevel != DriftLevel.Diamond)
        {
            newLevel = DriftLevel.Diamond;
        }
        else if (totalScore >= goldenDriftThreshold && currentLevel != DriftLevel.Golden)
        {
            newLevel = DriftLevel.Golden;
        }
        else if (totalScore >= silverDriftThreshold && currentLevel != DriftLevel.Silver)
        {
            newLevel = DriftLevel.Silver;
        }
        
        if (newLevel != currentLevel)
        {
            LevelUp(newLevel);
        }
    }
    
    private void LevelUp(DriftLevel newLevel)
    {
        DriftLevel oldLevel = currentLevel;
        currentLevel = newLevel;
        
        // Play level up sound
        if (audioSource != null && levelUpSound != null)
        {
            audioSource.PlayOneShot(levelUpSound);
        }
        
        // Update particle colors
        if (enableParticleControl)
        {
            UpdateParticleColors(newLevel);
        }
        
        // Trigger events
        OnLevelUp?.Invoke(newLevel);
        OnParticleColorChange?.Invoke(newLevel);
        
        Debug.Log($"Level Up! {oldLevel} -> {newLevel}");
    }
    
    private void UpdateParticleColors(DriftLevel level)
    {
        if (!enableParticleControl || driftParticles == null) return;
        
        Color targetColor = GetLevelColor(level);
        
        foreach (ParticleSystem particle in driftParticles)
        {
            if (particle != null)
            {
                var main = particle.main;
                main.startColor = targetColor;
            }
        }
    }
    
    private Color GetLevelColor(DriftLevel level)
    {
        switch (level)
        {
            case DriftLevel.Bronze:
                return bronzeColor;
            case DriftLevel.Silver:
                return silverColor;
            case DriftLevel.Golden:
                return goldenColor;
            case DriftLevel.Diamond:
                return diamondColor;
            default:
                return bronzeColor;
        }
    }
    
    private void StartDriftParticles()
    {
        if (!enableParticleControl || driftParticles == null) return;
        
        foreach (ParticleSystem particle in driftParticles)
        {
            if (particle != null)
            {
                particle.Play();
            }
        }
    }
    
    private void StopDriftParticles()
    {
        if (!enableParticleControl || driftParticles == null) return;
        
        foreach (ParticleSystem particle in driftParticles)
        {
            if (particle != null)
            {
                particle.Stop();
            }
        }
    }
    
    // Public getters
    public float GetCurrentScore() => currentScore;
    public float GetTotalScore() => totalScore;
    public DriftLevel GetCurrentLevel() => currentLevel;
    public bool IsDrifting() => isDrifting;
    public int GetComboCount() => comboCount;
    
    // Public setters for customization
    public void SetScoreMultipliers(float baseScore, float speed, float control, float combo)
    {
        baseScorePerSecond = baseScore;
        speedMultiplier = speed;
        controlMultiplier = control;
        comboMultiplier = combo;
    }
    
    public void SetLevelThresholds(int silver, int golden, int diamond)
    {
        silverDriftThreshold = silver;
        goldenDriftThreshold = golden;
        diamondDriftThreshold = diamond;
    }
    
    // Reset function
    public void ResetScore()
    {
        currentScore = 0f;
        totalScore = 0f;
        currentLevel = DriftLevel.Bronze;
        comboCount = 0;
        OnScoreChanged?.Invoke(totalScore);
        
        if (enableParticleControl)
        {
            UpdateParticleColors(DriftLevel.Bronze);
        }
    }
    
    // Particle control methods
    public void SetParticleControl(bool enabled)
    {
        enableParticleControl = enabled;
    }
    
    // Manual particle assignment (if needed)
    public void SetDriftParticles(List<ParticleSystem> particles)
    {
        driftParticles = particles;
        Debug.Log($"DriftScoreSystem: Manually assigned {particles?.Count ?? 0} particles");
    }
} 