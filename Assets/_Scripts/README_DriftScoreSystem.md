# Drift Score System - Simplified UI Version

## Overview
The Drift Score System is a modular scoring system for kart drifting that calculates points based on drift duration, speed, control, and combo multipliers. The system features a simplified UI that displays only text with smooth grow/shrink animations.

## Key Features
- **Event-Driven Architecture**: Clean separation between score calculation and UI display
- **Simplified UI**: Only text display with TextMeshPro and grow/shrink animations
- **Speed Integration**: Pulls speed directly from the main KartController
- **Level Progression**: Bronze → Silver → Golden → Diamond drift levels
- **Audio & Visual Feedback**: Sound effects and particle systems for level-ups

## Setup Instructions

### 1. Add DriftScoreSystem to Kart
1. Add the `DriftScoreSystem` component to your kart GameObject
2. Configure the score settings in the Inspector:
   - **Base Score Per Second**: Base points earned per second while drifting
   - **Speed Multiplier**: Bonus multiplier for higher speeds
   - **Control Multiplier**: Bonus multiplier for better control
   - **Combo Multiplier**: Bonus multiplier for maintaining combo

### 2. Configure Level Thresholds
Set the score thresholds for each drift level:
- **Silver Drift Threshold**: Score needed for Silver level (default: 1000)
- **Golden Drift Threshold**: Score needed for Golden level (default: 5000)
- **Diamond Drift Threshold**: Score needed for Diamond level (default: 15000)

### 3. Link to KartController
1. In the `KartController` component, assign the `DriftScoreSystem` reference
2. The system will automatically receive speed data from the main controller

### 4. Create Simple UI
1. Create a Canvas in your scene
2. Add two TextMeshPro - Text (UI) elements:
   - **Score Text**: Displays current score
   - **Level Text**: Displays current drift level
3. Add the `DriftUI` component to a GameObject in the Canvas
4. Assign the TextMeshPro elements to the respective fields

### 5. Configure UI Animation
Adjust the animation settings in the `DriftUI` component:
- **Score Update Speed**: How fast the score number animates
- **Text Animation Duration**: Duration of grow/shrink animations
- **Text Scale Multiplier**: How much the text scales during animation

### 6. Optional: Audio & Particles
- Assign an AudioSource and level-up sound clips
- Add particle systems for visual feedback during level-ups

## How It Works

### Score Calculation
The system calculates drift score using:
```
Frame Score = Base Score × (1 + Speed Bonus + Control Bonus) × Combo Bonus
```

Where:
- **Base Score**: `baseScorePerSecond × timeSinceLastScore`
- **Speed Bonus**: `(speed / 30f) × speedMultiplier` (clamped to 0-1)
- **Control Bonus**: `control × controlMultiplier` (clamped to 0-1)
- **Combo Bonus**: `1 + (comboCount × comboMultiplier)`

### Speed Integration
The system receives speed data directly from the main `KartController`:
- **Speed Source**: `sphere.velocity.magnitude` from KartController
- **Control Value**: Absolute horizontal movement input
- **No Internal Speed Calculation**: The system relies entirely on the main controller

### Level Progression
- **Bronze**: Starting level (0 points)
- **Silver**: Achieved at 1000 points
- **Golden**: Achieved at 5000 points
- **Diamond**: Achieved at 15000 points

### UI Animation
- **Score Updates**: Smooth number animation with grow/shrink effect
- **Level Changes**: Text scales up then down when leveling up
- **Color Changes**: Level text changes color based on current level

## API Reference

### DriftScoreSystem Methods
```csharp
// Core drift methods
public void StartDrift()                    // Called when drift begins
public void EndDrift()                      // Called when drift ends
public void UpdateDriftScore(float speed, float control)  // Called during drift

// Getters
public float GetCurrentScore()              // Current drift score
public float GetTotalScore()                // Total accumulated score
public DriftLevel GetCurrentLevel()         // Current drift level
public bool IsDrifting()                    // Is currently drifting
public int GetComboCount()                  // Current combo count

// Configuration
public void SetScoreMultipliers(float baseScore, float speed, float control, float combo)
public void SetLevelThresholds(int silver, int golden, int diamond)
public void ResetScore()                    // Reset all scores to zero
```

### Events
```csharp
public System.Action<DriftLevel> OnLevelUp;     // Triggered when level increases
public System.Action<float> OnScoreChanged;     // Triggered when score changes
```

## Customization

### Score Multipliers
Adjust the multipliers to change scoring behavior:
- **Higher Base Score**: More points per second
- **Higher Speed Multiplier**: More emphasis on speed
- **Higher Control Multiplier**: More emphasis on control
- **Higher Combo Multiplier**: More emphasis on maintaining combo

### Level Thresholds
Modify the thresholds to change level progression:
- **Lower Thresholds**: Faster level progression
- **Higher Thresholds**: Slower, more challenging progression

### UI Animation
Customize the animation feel:
- **Faster Animation**: Lower duration values
- **More Dramatic**: Higher scale multiplier
- **Smoother**: Higher score update speed

## Troubleshooting

### Common Issues
1. **No Score Updates**: Ensure DriftScoreSystem is assigned in KartController
2. **No UI Display**: Check TextMeshPro assignments in DriftUI
3. **No Animations**: Verify DOTween is imported and working
4. **Wrong Speed Values**: Confirm KartController is passing correct speed data

### Debug Information
The system logs important events to the console:
- "Drift Started!" when drifting begins
- "Drift Ended! Total Score: X" when drifting ends
- "Level Up! X -> Y" when level increases

## Performance Notes
- The system is optimized for minimal performance impact
- UI animations use DOTween for smooth performance
- Event-driven architecture reduces unnecessary updates
- Score calculations are frame-rate independent

## Version History
- **v2.0**: Simplified UI with grow/shrink animations, removed complex UI elements
- **v1.0**: Initial implementation with full UI system 