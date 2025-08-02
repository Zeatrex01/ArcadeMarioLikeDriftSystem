using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;

public class DriftSystemSetupTool : EditorWindow
{
    private GameObject selectedKart;
    private KartDriftController driftController;
    private DriftPlayerInputProvider inputProvider;
    
    private bool autoSetupComponents = true;
    private bool setupParticleSystems = true;
    private bool setupPostProcessing = true;
    private bool createDebugUI = false;
    
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Drift System Setup Tool")]
    public static void ShowWindow()
    {
        GetWindow<DriftSystemSetupTool>("Drift System Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Drift System Setup Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Kart Selection
        EditorGUILayout.LabelField("Kart Selection", EditorStyles.boldLabel);
        selectedKart = (GameObject)EditorGUILayout.ObjectField("Selected Kart", selectedKart, typeof(GameObject), true);
        
        if (selectedKart != null)
        {
            driftController = selectedKart.GetComponent<KartDriftController>();
            inputProvider = selectedKart.GetComponent<DriftPlayerInputProvider>();
        }
        
        EditorGUILayout.Space();
        
        // Setup Options
        EditorGUILayout.LabelField("Setup Options", EditorStyles.boldLabel);
        autoSetupComponents = EditorGUILayout.Toggle("Auto Setup Components", autoSetupComponents);
        setupParticleSystems = EditorGUILayout.Toggle("Setup Particle Systems", setupParticleSystems);
        setupPostProcessing = EditorGUILayout.Toggle("Setup Post Processing", setupPostProcessing);
        createDebugUI = EditorGUILayout.Toggle("Create Debug UI", createDebugUI);
        
        EditorGUILayout.Space();
        
        // Status Display
        DisplayStatus();
        
        EditorGUILayout.Space();
        
        // Action Buttons
        DisplayActionButtons();
        
        EditorGUILayout.Space();
        
        // Manual Setup Section
        DisplayManualSetup();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DisplayStatus()
    {
        EditorGUILayout.LabelField("System Status", EditorStyles.boldLabel);
        
        if (selectedKart == null)
        {
            EditorGUILayout.HelpBox("No kart selected. Please select a kart GameObject.", MessageType.Warning);
            return;
        }
        
        // Drift Controller Status
        if (driftController != null)
        {
            EditorGUILayout.LabelField("✓ KartDriftController", EditorStyles.label);
        }
        else
        {
            EditorGUILayout.LabelField("✗ KartDriftController (Missing)", EditorStyles.label);
        }
        
        // Input Provider Status
        if (inputProvider != null)
        {
            EditorGUILayout.LabelField("✓ DriftPlayerInputProvider", EditorStyles.label);
        }
        else
        {
            EditorGUILayout.LabelField("✗ DriftPlayerInputProvider (Missing)", EditorStyles.label);
        }
        
        // Rigidbody Status
        Rigidbody rb = selectedKart.GetComponent<Rigidbody>();
        if (rb != null)
        {
            EditorGUILayout.LabelField("✓ Rigidbody", EditorStyles.label);
        }
        else
        {
            EditorGUILayout.LabelField("✗ Rigidbody (Missing)", EditorStyles.label);
        }
        
        // Sphere Collider Status
        SphereCollider sphereCollider = selectedKart.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            EditorGUILayout.LabelField("✓ Sphere Collider", EditorStyles.label);
        }
        else
        {
            EditorGUILayout.LabelField("✗ Sphere Collider (Missing)", EditorStyles.label);
        }
    }
    
    private void DisplayActionButtons()
    {
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Auto Setup Complete Drift System"))
        {
            AutoSetupCompleteSystem();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup KartDriftController Only"))
        {
            SetupDriftController();
        }
        
        if (GUILayout.Button("Setup Input Provider Only"))
        {
            SetupInputProvider();
        }
        
        if (GUILayout.Button("Setup Basic Components"))
        {
            SetupBasicComponents();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Auto Assign References"))
        {
            AutoAssignReferences();
        }
        
        if (GUILayout.Button("Create Debug UI"))
        {
            CreateDebugUI();
        }
    }
    
    private void DisplayManualSetup()
    {
        EditorGUILayout.LabelField("Manual Reference Assignment", EditorStyles.boldLabel);
        
        if (driftController != null)
        {
            EditorGUILayout.HelpBox("Select the KartDriftController component in the Inspector to manually assign references.", MessageType.Info);
            
            if (GUILayout.Button("Select KartDriftController"))
            {
                Selection.activeGameObject = selectedKart;
                EditorGUIUtility.PingObject(driftController);
            }
        }
        
        if (inputProvider != null)
        {
            if (GUILayout.Button("Select DriftPlayerInputProvider"))
            {
                Selection.activeGameObject = selectedKart;
                EditorGUIUtility.PingObject(inputProvider);
            }
        }
    }
    
    private void AutoSetupCompleteSystem()
    {
        if (selectedKart == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a kart GameObject first!", "OK");
            return;
        }
        
        // Setup basic components
        SetupBasicComponents();
        
        // Setup drift controller
        SetupDriftController();
        
        // Setup input provider
        SetupInputProvider();
        
        // Auto assign references
        AutoAssignReferences();
        
        // Setup additional features
        if (setupParticleSystems)
        {
            SetupParticleSystems();
        }
        
        if (setupPostProcessing)
        {
            SetupPostProcessing();
        }
        
        if (createDebugUI)
        {
            CreateDebugUI();
        }
        
        EditorUtility.DisplayDialog("Success", "Drift system setup completed successfully!", "OK");
    }
    
    private void SetupBasicComponents()
    {
        // Add Rigidbody if missing
        Rigidbody rb = selectedKart.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = selectedKart.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.drag = 0.1f;
            rb.angularDrag = 0.05f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        // Add Sphere Collider if missing
        SphereCollider sphereCollider = selectedKart.GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = selectedKart.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.5f;
            sphereCollider.center = Vector3.zero;
        }
        
        // Add Layer if needed
        if (selectedKart.layer == 0)
        {
            selectedKart.layer = LayerMask.NameToLayer("Default");
        }
    }
    
    private void SetupDriftController()
    {
        if (driftController == null)
        {
            driftController = selectedKart.AddComponent<KartDriftController>();
        }
        
        // Set default values
        SerializedObject so = new SerializedObject(driftController);
        so.FindProperty("acceleration").floatValue = 30f;
        so.FindProperty("steering").floatValue = 80f;
        so.FindProperty("gravity").floatValue = 10f;
        so.FindProperty("driftAngleSmoothing").floatValue = 2f;
        so.FindProperty("driftRotationSpeed").floatValue = 1f;
        so.FindProperty("driftVisualSmoothing").floatValue = 0.2f;
        so.FindProperty("enablePostProcessing").boolValue = true;
        so.FindProperty("chromaticAberrationIntensity").floatValue = 0.5f;
        so.FindProperty("autoAcceleration").boolValue = false;
        so.FindProperty("autoAccelerationIntensity").floatValue = 1f;
        so.FindProperty("enableDriftTracking").boolValue = true;
        so.FindProperty("minDriftAngle").floatValue = 15f;
        so.FindProperty("driftScoreMultiplier").floatValue = 100f;
        so.FindProperty("showDriftAngle").boolValue = true;
        so.FindProperty("showDriftScore").boolValue = true;
        so.ApplyModifiedProperties();
    }
    
    private void SetupInputProvider()
    {
        if (inputProvider == null)
        {
            inputProvider = selectedKart.AddComponent<DriftPlayerInputProvider>();
        }
        
        // Set default values
        SerializedObject so = new SerializedObject(inputProvider);
        so.FindProperty("enableKeyboardInput").boolValue = true;
        so.FindProperty("enableGamepadInput").boolValue = true;
        so.FindProperty("enableDriftTrackDebug").boolValue = true;
        so.FindProperty("debugDriftAngle").enumValueIndex = (int)KeyCode.F1;
        so.FindProperty("debugDriftScore").enumValueIndex = (int)KeyCode.F2;
        so.FindProperty("resetDriftScore").enumValueIndex = (int)KeyCode.F3;
        so.ApplyModifiedProperties();
    }
    
    private void AutoAssignReferences()
    {
        if (driftController == null) return;
        
        SerializedObject so = new SerializedObject(driftController);
        
        // Find kart model
        Transform kartModel = FindChildByName(selectedKart.transform, "KartModel");
        if (kartModel != null)
        {
            so.FindProperty("kartModel").objectReferenceValue = kartModel;
        }
        
        // Find kart normal
        Transform kartNormal = FindChildByName(selectedKart.transform, "KartNormal");
        if (kartNormal != null)
        {
            so.FindProperty("kartNormal").objectReferenceValue = kartNormal;
        }
        
        // Set sphere rigidbody
        Rigidbody sphereRb = selectedKart.GetComponent<Rigidbody>();
        if (sphereRb != null)
        {
            so.FindProperty("sphere").objectReferenceValue = sphereRb;
        }
        
        // Find wheel particles
        Transform wheelParticles = FindChildByName(selectedKart.transform, "WheelParticles");
        if (wheelParticles != null)
        {
            so.FindProperty("wheelParticles").objectReferenceValue = wheelParticles;
        }
        
        // Find flash particles
        Transform flashParticles = FindChildByName(selectedKart.transform, "FlashParticles");
        if (flashParticles != null)
        {
            so.FindProperty("flashParticles").objectReferenceValue = flashParticles;
        }
        
        // Find model parts
        Transform frontWheels = FindChildByName(selectedKart.transform, "FrontWheels");
        if (frontWheels != null)
        {
            so.FindProperty("frontWheels").objectReferenceValue = frontWheels;
        }
        
        Transform backWheels = FindChildByName(selectedKart.transform, "BackWheels");
        if (backWheels != null)
        {
            so.FindProperty("backWheels").objectReferenceValue = backWheels;
        }
        
        Transform steeringWheel = FindChildByName(selectedKart.transform, "SteeringWheel");
        if (steeringWheel != null)
        {
            so.FindProperty("steeringWheel").objectReferenceValue = steeringWheel;
        }
        
        // Set layer mask
        so.FindProperty("layerMask").intValue = LayerMask.GetMask("Default");
        
        // Set turbo colors
        SerializedProperty turboColorsProp = so.FindProperty("turboColors");
        if (turboColorsProp.arraySize == 0)
        {
            turboColorsProp.arraySize = 3;
            turboColorsProp.GetArrayElementAtIndex(0).colorValue = Color.yellow;
            turboColorsProp.GetArrayElementAtIndex(1).colorValue = Color.green;
            turboColorsProp.GetArrayElementAtIndex(2).colorValue = Color.red;
        }
        
        so.ApplyModifiedProperties();
        
        // Set input provider reference
        if (inputProvider != null)
        {
            SerializedObject inputSo = new SerializedObject(inputProvider);
            inputSo.FindProperty("kart").objectReferenceValue = driftController;
            inputSo.ApplyModifiedProperties();
        }
    }
    
    private void SetupParticleSystems()
    {
        // Create wheel particles if they don't exist
        Transform wheelParticles = FindChildByName(selectedKart.transform, "WheelParticles");
        if (wheelParticles == null)
        {
            wheelParticles = CreateChild(selectedKart.transform, "WheelParticles");
            
            // Create front wheels particle container
            Transform frontWheelParticles = CreateChild(wheelParticles, "FrontWheels");
            CreateParticleSystem(frontWheelParticles, "LeftWheel", new Vector3(-0.5f, 0, 0));
            CreateParticleSystem(frontWheelParticles, "RightWheel", new Vector3(0.5f, 0, 0));
            
            // Create back wheels particle container
            Transform backWheelParticles = CreateChild(wheelParticles, "BackWheels");
            CreateParticleSystem(backWheelParticles, "LeftWheel", new Vector3(-0.5f, 0, 0));
            CreateParticleSystem(backWheelParticles, "RightWheel", new Vector3(0.5f, 0, 0));
        }
        
        // Create flash particles if they don't exist
        Transform flashParticles = FindChildByName(selectedKart.transform, "FlashParticles");
        if (flashParticles == null)
        {
            flashParticles = CreateChild(selectedKart.transform, "FlashParticles");
            CreateParticleSystem(flashParticles, "FlashEffect", Vector3.zero);
        }
    }
    
    private void SetupPostProcessing()
    {
        // Check if post processing volume exists
        PostProcessVolume postVolume = Camera.main?.GetComponent<PostProcessVolume>();
        if (postVolume == null)
        {
            EditorUtility.DisplayDialog("Post Processing", "No Post Process Volume found on Main Camera. Please set up Post Processing manually.", "OK");
        }
    }
    
    private void CreateDebugUI()
    {
        // Create Canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Debug Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create debug UI panel
        GameObject debugPanel = new GameObject("Drift Debug Panel");
        debugPanel.transform.SetParent(canvas.transform, false);
        
        UnityEngine.UI.Image panelImage = debugPanel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        
        RectTransform panelRect = debugPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.8f);
        panelRect.anchorMax = new Vector2(0.3f, 1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Add debug text
        GameObject debugText = new GameObject("Debug Text");
        debugText.transform.SetParent(debugPanel.transform, false);
        
        UnityEngine.UI.Text text = debugText.AddComponent<UnityEngine.UI.Text>();
        text.text = "Drift Debug Info\nPress F1, F2, F3 for debug info";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        
        RectTransform textRect = debugText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
    }
    
    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;
            
            Transform found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
    
    private Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child.transform;
    }
    
    private void CreateParticleSystem(Transform parent, string name, Vector3 position)
    {
        GameObject particleGO = new GameObject(name);
        particleGO.transform.SetParent(parent);
        particleGO.transform.localPosition = position;
        
        ParticleSystem ps = particleGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.maxParticles = 50;
        
        var emission = ps.emission;
        emission.rateOverTime = 10f;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
    }
} 