using UnityEngine;
using TMPro;
using DG.Tweening;

public class DriftUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI levelText;
    
    [Header("Level Colors")]
    [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
    [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color goldenColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color diamondColor = new Color(0.5f, 0.8f, 1f);
    
    [Header("Animation Settings")]
    [SerializeField] private float scoreUpdateSpeed = 0.1f;
    [SerializeField] private float textAnimationDuration = 0.3f;
    [SerializeField] private float textScaleMultiplier = 1.2f;
    
    [Header("Particle Synchronization")]
    [SerializeField] private bool syncWithParticleColors = true;
    
    [Header("Visibility Control")]
    [SerializeField] private bool enableVisibilityControl = true;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    [SerializeField] private DriftScoreSystem driftScoreSystem;
    private float displayedScore = 0f;
    private float displayedCurrentScore = 0f;
    private DriftScoreSystem.DriftLevel currentLevel = DriftScoreSystem.DriftLevel.Bronze;
    private CanvasGroup canvasGroup;
    
    private void Start()
    {
        // Find drift score system
        driftScoreSystem = FindObjectOfType<DriftScoreSystem>();
        
        if (driftScoreSystem != null)
        {
            // Subscribe to events
            driftScoreSystem.OnScoreChanged += OnScoreChanged;
            driftScoreSystem.OnLevelUp += OnLevelUp;
            driftScoreSystem.OnParticleColorChange += OnParticleColorChange;
            driftScoreSystem.OnDriftUIStart += OnDriftUIStart;
            driftScoreSystem.OnDriftUIEnd += OnDriftUIEnd;
            driftScoreSystem.OnCurrentScoreChanged += OnCurrentScoreChanged;
        }
        
        // Get or create CanvasGroup for fade effects
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Initialize UI
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        if (scoreText != null)
            scoreText.text = "Total Score: 0";
        
        if (levelText != null)
            levelText.text = "Bronze Drift";
        
        UpdateLevelColors(DriftScoreSystem.DriftLevel.Bronze);
        
        // Start with UI hidden if visibility control is enabled
        if (enableVisibilityControl)
        {
            HideDriftUI();
        }
    }
    
    private void OnScoreChanged(float newScore)
    {
        // Animate total score change with grow/shrink effect
        DOTween.To(() => displayedScore, x => {
            displayedScore = x;
            if (scoreText != null && !driftScoreSystem.IsDrifting())
            {
                scoreText.text = $"Total Score: {Mathf.FloorToInt(x)}";
                
                // Grow/shrink animation for score text
                scoreText.transform.DOScale(Vector3.one * textScaleMultiplier, textAnimationDuration * 0.5f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        scoreText.transform.DOScale(Vector3.one, textAnimationDuration * 0.5f)
                            .SetEase(Ease.InQuad);
                    });
            }
        }, newScore, scoreUpdateSpeed).SetEase(Ease.OutQuad);
    }
    
    private void OnCurrentScoreChanged(float newCurrentScore)
    {
        // Update current score during drift
        displayedCurrentScore = newCurrentScore;
        if (scoreText != null && driftScoreSystem.IsDrifting())
        {
            scoreText.text = $"Drift Score: {Mathf.FloorToInt(newCurrentScore)}";
        }
    }
    
    private void OnLevelUp(DriftScoreSystem.DriftLevel newLevel)
    {
        currentLevel = newLevel;
        
        // Update level text with grow/shrink animation
        if (levelText != null)
        {
            levelText.text = GetLevelDisplayName(newLevel);
            
            // Grow/shrink animation for level text
            levelText.transform.DOScale(Vector3.one * textScaleMultiplier, textAnimationDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    levelText.transform.DOScale(Vector3.one, textAnimationDuration * 0.5f)
                        .SetEase(Ease.InQuad);
                });
        }
        
        // Update colors
        UpdateLevelColors(newLevel);
    }
    
    private void OnParticleColorChange(DriftScoreSystem.DriftLevel level)
    {
        // Synchronize UI colors with particle colors if enabled
        if (syncWithParticleColors)
        {
            UpdateLevelColors(level);
        }
    }
    
    // Event handlers for UI visibility
    private void OnDriftUIStart()
    {
        if (enableVisibilityControl)
        {
            ShowDriftUI();
        }
    }
    
    private void OnDriftUIEnd()
    {
        if (enableVisibilityControl)
        {
            HideDriftUI();
        }
    }
    
    private void UpdateLevelColors(DriftScoreSystem.DriftLevel level)
    {
        Color levelColor = GetLevelColor(level);
        
        if (levelText != null)
            levelText.color = levelColor;
    }
    
    private Color GetLevelColor(DriftScoreSystem.DriftLevel level)
    {
        switch (level)
        {
            case DriftScoreSystem.DriftLevel.Bronze:
                return bronzeColor;
            case DriftScoreSystem.DriftLevel.Silver:
                return silverColor;
            case DriftScoreSystem.DriftLevel.Golden:
                return goldenColor;
            case DriftScoreSystem.DriftLevel.Diamond:
                return diamondColor;
            default:
                return bronzeColor;
        }
    }
    
    private string GetLevelDisplayName(DriftScoreSystem.DriftLevel level)
    {
        switch (level)
        {
            case DriftScoreSystem.DriftLevel.Bronze:
                return "Bronze Drift";
            case DriftScoreSystem.DriftLevel.Silver:
                return "Silver Drift";
            case DriftScoreSystem.DriftLevel.Golden:
                return "Golden Drift";
            case DriftScoreSystem.DriftLevel.Diamond:
                return "Diamond Drift";
            default:
                return "Bronze Drift";
        }
    }
    
    // Visibility control methods
    public void ShowDriftUI()
    {
        if (!enableVisibilityControl) return;
        
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
    
    public void HideDriftUI()
    {
        if (!enableVisibilityControl) return;
        
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    // Public method to toggle visibility control
    public void SetVisibilityControl(bool enabled)
    {
        enableVisibilityControl = enabled;
        if (!enabled && canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (driftScoreSystem != null)
        {
            driftScoreSystem.OnScoreChanged -= OnScoreChanged;
            driftScoreSystem.OnLevelUp -= OnLevelUp;
            driftScoreSystem.OnParticleColorChange -= OnParticleColorChange;
            driftScoreSystem.OnDriftUIStart -= OnDriftUIStart;
            driftScoreSystem.OnDriftUIEnd -= OnDriftUIEnd;
            driftScoreSystem.OnCurrentScoreChanged -= OnCurrentScoreChanged;
        }
    }
} 