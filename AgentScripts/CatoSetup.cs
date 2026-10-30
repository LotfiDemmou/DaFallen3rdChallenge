using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CatoSetup
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string SrcPng = "Assets/Art/Character/cato-purple.png";
    const string WalkPng = "Assets/Art/Character/cato-walk.png";
    const string AnimDir = "Assets/Animations";
    const int FW = 32, FH = 64, Frames = 4;

    static readonly Vector2Int[] Offsets =
    {
        new Vector2Int(0, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(0, 2),
        new Vector2Int(1, 1),
    };

    public static string Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var root = GameObject.Find("Cato");
        if (root == null) return "ERROR: Cato not found";

        // --- generate walk sheet ---
        var srcImp = (TextureImporter)AssetImporter.GetAtPath(SrcPng);
        bool wasReadable = srcImp.isReadable;
        srcImp.isReadable = true;
        srcImp.SaveAndReimport();
        var src = AssetDatabase.LoadAssetAtPath<Texture2D>(SrcPng);
        if (src == null) return "ERROR: could not load " + SrcPng;

        var srcPx = src.GetPixels();
        int sw = src.width, sh = src.height;
        var sheet = new Color[FW * Frames * FH];
        for (int f = 0; f < Frames; f++)
        {
            var off = Offsets[f];
            for (int y = 0; y < sh; y++)
            for (int x = 0; x < sw; x++)
            {
                int dx = f * FW + x + off.x;
                int dy = y + off.y;
                if (dx < f * FW || dx >= (f + 1) * FW || dy < 0 || dy >= FH) continue;
                sheet[dy * (FW * Frames) + dx] = srcPx[y * sw + x];
            }
        }

        var sheetTex = new Texture2D(FW * Frames, FH, TextureFormat.RGBA32, false);
        sheetTex.filterMode = FilterMode.Point;
        sheetTex.wrapMode = TextureWrapMode.Clamp;
        sheetTex.SetPixels(sheet);
        sheetTex.Apply();
        File.WriteAllBytes(WalkPng, sheetTex.EncodeToPNG());
        Object.DestroyImmediate(sheetTex);

        srcImp.isReadable = wasReadable;
        srcImp.SaveAndReimport();

        AssetDatabase.ImportAsset(WalkPng, ImportAssetOptions.ForceUpdate);
        var walkImp = (TextureImporter)AssetImporter.GetAtPath(WalkPng);
        walkImp.textureType = TextureImporterType.Sprite;
        walkImp.spriteImportMode = SpriteImportMode.Multiple;
        walkImp.spritePixelsPerUnit = 32f;
        walkImp.filterMode = FilterMode.Point;
        walkImp.textureCompression = TextureImporterCompression.Uncompressed;
        walkImp.mipmapEnabled = false;
        walkImp.alphaIsTransparency = true;
        var metas = new SpriteMetaData[Frames];
        for (int i = 0; i < Frames; i++)
        {
            metas[i] = new SpriteMetaData
            {
                name = "CatoWalk_" + i,
                rect = new Rect(i * FW, 0, FW, FH),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
        walkImp.spritesheet = metas;
        walkImp.SaveAndReimport();

        var frames = AssetDatabase.LoadAllAssetsAtPath(WalkPng)
            .OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length != Frames)
            return "ERROR: expected " + Frames + " frames, got " + frames.Length;

        // --- animations ---
        if (!AssetDatabase.IsValidFolder(AnimDir))
            AssetDatabase.CreateFolder("Assets", "Animations");

        var walkClip = new AnimationClip { name = "CatoWalk", frameRate = 10f };
        var spriteBinding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        var keys = new ObjectReferenceKeyframe[Frames];
        for (int i = 0; i < Frames; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / 10f, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(walkClip, spriteBinding, keys);
        var ws = AnimationUtility.GetAnimationClipSettings(walkClip);
        ws.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(walkClip, ws);
        AssetDatabase.DeleteAsset(AnimDir + "/CatoWalk.anim");
        AssetDatabase.CreateAsset(walkClip, AnimDir + "/CatoWalk.anim");

        var idleClip = new AnimationClip { name = "CatoIdle", frameRate = 10f };
        var idleKeys = new[] { new ObjectReferenceKeyframe { time = 0f, value = frames[2] } };
        AnimationUtility.SetObjectReferenceCurve(idleClip, spriteBinding, idleKeys);
        var ids = AnimationUtility.GetAnimationClipSettings(idleClip);
        ids.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(idleClip, ids);
        AssetDatabase.DeleteAsset(AnimDir + "/CatoIdle.anim");
        AssetDatabase.CreateAsset(idleClip, AnimDir + "/CatoIdle.anim");

        // --- animator controller ---
        string ctrlPath = AnimDir + "/Cato.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        var sm = ctrl.layers[0].stateMachine;
        var idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        var walkState = sm.AddState("Walk");
        walkState.motion = walkClip;
        sm.defaultState = idleState;

        var toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.03f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

        var toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.03f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

        // --- restructure Cato: root + Visual child ---
        var rootSr = root.GetComponent<SpriteRenderer>();
        Sprite sprite = rootSr != null ? rootSr.sprite : null;
        Color color = rootSr != null ? rootSr.color : Color.white;
        bool flip = rootSr != null && rootSr.flipX;
        int order = rootSr != null ? rootSr.sortingOrder : 10;

        var visualT = root.transform.Find("Visual");
        GameObject visual = visualT != null ? visualT.gameObject : new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var vsr = visual.GetComponent<SpriteRenderer>();
        if (vsr == null) vsr = visual.AddComponent<SpriteRenderer>();
        if (sprite != null) vsr.sprite = sprite;
        vsr.color = color;
        vsr.flipX = flip;
        vsr.sortingOrder = order;
        if (rootSr != null) Object.DestroyImmediate(rootSr);

        var animator = visual.GetComponent<Animator>();
        if (animator == null) animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;
        animator.applyRootMotion = false;

        var rb = root.GetComponent<Rigidbody2D>();
        if (rb == null) rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var box = root.GetComponent<BoxCollider2D>();
        if (box == null) box = root.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.6f, 1.6f);
        box.offset = new Vector2(0f, -0.1f);

        var controller = root.GetComponent<CatoController>();
        if (controller == null) controller = root.AddComponent<CatoController>();
        var so = new SerializedObject(controller);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("spriteRenderer").objectReferenceValue = vsr;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.tag = "Player";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return string.Format(
            "Cato set up: Visual child + SpriteRenderer, Animator (Idle/Walk), Rigidbody2D, BoxCollider2D, CatoController. Walk sheet {0} ({1} frames).",
            WalkPng, Frames);
    }
}
