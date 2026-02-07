using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Animation Retargeter - Transfer Animations Between Rigs
/// Version 1.0 | Animation Workflow Tool
/// </summary>
public class AnimationRetargeter : EditorWindow
{
    private Avatar sourceAvatar;
    private Avatar targetAvatar;
    private AnimationClip sourceClip;
    private GameObject sourceRig;
    private GameObject targetRig;
    
    private string newClipName = "RetargetedAnimation";
    private bool preserveRootMotion = true;
    private bool adjustScale = true;
    private float scaleMultiplier = 1f;
    
    private Vector2 scrollPosition;
    private List<BoneMapping> boneMappings = new List<BoneMapping>();
    
    [System.Serializable]
    private class BoneMapping
    {
        public string sourceBone;
        public string targetBone;
        public bool enabled = true;
    }
    
    [MenuItem("Tools/Animation Retargeter")]
    public static void ShowWindow()
    {
        AnimationRetargeter window = GetWindow<AnimationRetargeter>("Anim Retargeter");
        window.minSize = new Vector2(400, 550);
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Header
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 18;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.Space(10);
        GUILayout.Label("🎬 ANIMATION RETARGETER", headerStyle);
        GUILayout.Label("Transfer Animations Between Rigs", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(10);
        
        // Source Section
        GUILayout.Label("📤 Source (From)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        sourceRig = (GameObject)EditorGUILayout.ObjectField("Source Rig", sourceRig, typeof(GameObject), true);
        sourceAvatar = (Avatar)EditorGUILayout.ObjectField("Source Avatar", sourceAvatar, typeof(Avatar), false);
        sourceClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", sourceClip, typeof(AnimationClip), false);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Target Section
        GUILayout.Label("📥 Target (To)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        targetRig = (GameObject)EditorGUILayout.ObjectField("Target Rig", targetRig, typeof(GameObject), true);
        targetAvatar = (Avatar)EditorGUILayout.ObjectField("Target Avatar", targetAvatar, typeof(Avatar), false);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Settings
        GUILayout.Label("⚙️ Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        newClipName = EditorGUILayout.TextField("New Clip Name", newClipName);
        preserveRootMotion = EditorGUILayout.Toggle("Preserve Root Motion", preserveRootMotion);
        adjustScale = EditorGUILayout.Toggle("Adjust Scale", adjustScale);
        if (adjustScale)
        {
            scaleMultiplier = EditorGUILayout.Slider("Scale Multiplier", scaleMultiplier, 0.1f, 3f);
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Bone Mapping
        GUILayout.Label("🦴 Bone Mapping", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (GUILayout.Button("Auto-Detect Bone Mapping", GUILayout.Height(25)))
        {
            AutoDetectBoneMapping();
        }
        
        if (boneMappings.Count > 0)
        {
            EditorGUILayout.Space(5);
            GUILayout.Label("Mapped bones: " + boneMappings.Count, EditorStyles.miniLabel);
            
            // Show a few mappings
            int showCount = Mathf.Min(5, boneMappings.Count);
            for (int i = 0; i < showCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                boneMappings[i].enabled = EditorGUILayout.Toggle(boneMappings[i].enabled, GUILayout.Width(20));
                EditorGUILayout.LabelField(boneMappings[i].sourceBone, GUILayout.Width(120));
                EditorGUILayout.LabelField("→", GUILayout.Width(20));
                EditorGUILayout.LabelField(boneMappings[i].targetBone);
                EditorGUILayout.EndHorizontal();
            }
            
            if (boneMappings.Count > 5)
            {
                GUILayout.Label("... and " + (boneMappings.Count - 5) + " more", EditorStyles.miniLabel);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Click 'Auto-Detect' or set up Source/Target rigs", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(15);
        
        // Actions
        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.9f);
        if (GUILayout.Button("🎬 RETARGET ANIMATION", GUILayout.Height(45)))
        {
            RetargetAnimation();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview on Target", GUILayout.Height(30)))
        {
            PreviewAnimation();
        }
        if (GUILayout.Button("Validate Setup", GUILayout.Height(30)))
        {
            ValidateSetup();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(20);
        EditorGUILayout.EndScrollView();
    }
    
    private void AutoDetectBoneMapping()
    {
        boneMappings.Clear();
        
        if (sourceRig == null || targetRig == null)
        {
            EditorUtility.DisplayDialog("Missing Rigs", "Please assign both Source and Target rigs.", "OK");
            return;
        }
        
        // Get all transforms
        Transform[] sourceTransforms = sourceRig.GetComponentsInChildren<Transform>();
        Transform[] targetTransforms = targetRig.GetComponentsInChildren<Transform>();
        
        Dictionary<string, Transform> targetDict = new Dictionary<string, Transform>();
        foreach (Transform t in targetTransforms)
        {
            string normalizedName = NormalizeBoneName(t.name);
            if (!targetDict.ContainsKey(normalizedName))
            {
                targetDict[normalizedName] = t;
            }
        }
        
        foreach (Transform source in sourceTransforms)
        {
            string normalizedName = NormalizeBoneName(source.name);
            
            if (targetDict.TryGetValue(normalizedName, out Transform target))
            {
                boneMappings.Add(new BoneMapping
                {
                    sourceBone = source.name,
                    targetBone = target.name,
                    enabled = true
                });
            }
        }
        
        Debug.Log("✅ Auto-detected " + boneMappings.Count + " bone mappings!");
    }
    
    private string NormalizeBoneName(string name)
    {
        // Remove common prefixes/suffixes and normalize case
        name = name.ToLower();
        name = name.Replace("mixamorig:", "");
        name = name.Replace("bip001", "");
        name = name.Replace("bip01", "");
        name = name.Replace("_", "");
        name = name.Replace(" ", "");
        name = name.Replace("left", "l").Replace("right", "r");
        return name;
    }
    
    private void RetargetAnimation()
    {
        if (sourceClip == null)
        {
            EditorUtility.DisplayDialog("No Animation", "Please assign a source Animation Clip.", "OK");
            return;
        }
        
        if (boneMappings.Count == 0)
        {
            EditorUtility.DisplayDialog("No Mappings", "Please auto-detect or set up bone mappings first.", "OK");
            return;
        }
        
        // Create new clip
        AnimationClip newClip = new AnimationClip();
        newClip.name = newClipName;
        newClip.legacy = sourceClip.legacy;
        
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);
        
        foreach (EditorCurveBinding binding in bindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            
            // Find mapping for this path
            string newPath = RemapPath(binding.path);
            
            if (newPath != null)
            {
                EditorCurveBinding newBinding = binding;
                newBinding.path = newPath;
                
                // Apply scale if needed
                if (adjustScale && binding.propertyName.Contains("Position"))
                {
                    curve = ScaleCurve(curve, scaleMultiplier);
                }
                
                AnimationUtility.SetEditorCurve(newClip, newBinding, curve);
            }
        }
        
        // Save clip
        string path = "Assets/_RetargetedAnimations";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "_RetargetedAnimations");
        }
        
        string clipPath = path + "/" + newClipName + ".anim";
        AssetDatabase.CreateAsset(newClip, clipPath);
        AssetDatabase.SaveAssets();
        
        Selection.activeObject = newClip;
        
        Debug.Log("✅ Animation retargeted and saved: " + clipPath);
        EditorUtility.DisplayDialog("Success!", "Animation retargeted!\n\nSaved to: " + clipPath, "OK");
    }
    
    private string RemapPath(string sourcePath)
    {
        foreach (BoneMapping mapping in boneMappings)
        {
            if (!mapping.enabled) continue;
            
            if (sourcePath.Contains(mapping.sourceBone))
            {
                return sourcePath.Replace(mapping.sourceBone, mapping.targetBone);
            }
        }
        
        return preserveRootMotion ? sourcePath : null;
    }
    
    private AnimationCurve ScaleCurve(AnimationCurve curve, float scale)
    {
        Keyframe[] keys = curve.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].value *= scale;
        }
        return new AnimationCurve(keys);
    }
    
    private void PreviewAnimation()
    {
        if (targetRig == null || sourceClip == null)
        {
            EditorUtility.DisplayDialog("Missing Setup", "Assign target rig and source clip first.", "OK");
            return;
        }
        
        Animator animator = targetRig.GetComponent<Animator>();
        if (animator == null)
        {
            animator = targetRig.AddComponent<Animator>();
        }
        
        // Note: Full preview requires Animation window integration
        Debug.Log("Preview: Select the target rig and open the Animation window to preview.");
        Selection.activeGameObject = targetRig;
    }
    
    private void ValidateSetup()
    {
        List<string> issues = new List<string>();
        
        if (sourceRig == null) issues.Add("• Source Rig not assigned");
        if (targetRig == null) issues.Add("• Target Rig not assigned");
        if (sourceClip == null) issues.Add("• Source Animation Clip not assigned");
        if (boneMappings.Count == 0) issues.Add("• No bone mappings detected");
        
        if (sourceRig != null && sourceRig.GetComponent<Animator>() == null)
            issues.Add("• Source Rig has no Animator component");
        if (targetRig != null && targetRig.GetComponent<Animator>() == null)
            issues.Add("• Target Rig has no Animator component");
        
        if (issues.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Passed ✅", "Setup looks good! Ready to retarget.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Issues Found", string.Join("\n", issues), "OK");
        }
    }
}
