using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class ImageReplacerWindow : EditorWindow
{
    private GameObject sourceObj;
    private Sprite sourceSprite;
    private GameObject targetObj;
    private Sprite targetSprite;
    private enum ReplaceMode { ReplaceSprite, ReplaceComponent }
    private ReplaceMode replaceMode = ReplaceMode.ReplaceSprite;
    private enum Scope { SelectedObjects, EntireScene, PrefabsInFolder }
    private Scope scope = Scope.SelectedObjects;
    private string prefabFolderPath = "Assets/Prefabs";

    [MenuItem("Tools/Image Replacer")]
    public static void ShowWindow()
    {
        GetWindow<ImageReplacerWindow>("Image Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("替换Image组件", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 源
        GUILayout.Label("源 (要被替换掉的样式)", EditorStyles.label);
        sourceObj = (GameObject)EditorGUILayout.ObjectField("源Image物体", sourceObj, typeof(GameObject), true);
        if (sourceObj != null)
        {
            Image img = sourceObj.GetComponent<Image>();
            if (img != null && img.sprite != null)
                sourceSprite = img.sprite;
            else
                EditorGUILayout.HelpBox("源物体上没有Image组件或Sprite为空", MessageType.Warning);
        }
        sourceSprite = (Sprite)EditorGUILayout.ObjectField("或直接指定源Sprite", sourceSprite, typeof(Sprite), false);

        EditorGUILayout.Space();
        // 目标
        GUILayout.Label("目标 (要替换成的新样式)", EditorStyles.label);
        targetObj = (GameObject)EditorGUILayout.ObjectField("目标Image物体", targetObj, typeof(GameObject), true);
        if (targetObj != null)
        {
            Image img = targetObj.GetComponent<Image>();
            if (img != null && img.sprite != null)
                targetSprite = img.sprite;
            else
                EditorGUILayout.HelpBox("目标物体上没有Image组件或Sprite为空", MessageType.Warning);
        }
        targetSprite = (Sprite)EditorGUILayout.ObjectField("或直接指定目标Sprite", targetSprite, typeof(Sprite), false);

        EditorGUILayout.Space();
        replaceMode = (ReplaceMode)EditorGUILayout.EnumPopup("替换模式", replaceMode);
        scope = (Scope)EditorGUILayout.EnumPopup("替换范围", scope);
        if (scope == Scope.PrefabsInFolder)
        {
            prefabFolderPath = EditorGUILayout.TextField("预制体文件夹路径", prefabFolderPath);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("执行替换", GUILayout.Height(30)))
        {
            ExecuteReplacement();
        }
    }

    private void ExecuteReplacement()
    {
        // 校验输入（省略，与原代码相同）
        if (replaceMode == ReplaceMode.ReplaceSprite)
        {
            if (sourceSprite == null && targetSprite == null)
            {
                EditorUtility.DisplayDialog("错误", "请至少指定源Sprite或目标Sprite", "确定");
                return;
            }
            if (targetSprite == null)
            {
                EditorUtility.DisplayDialog("错误", "替换模式为“仅替换Sprite”，必须指定目标Sprite", "确定");
                return;
            }
        }
        else // ReplaceComponent
        {
            if (targetObj == null || targetObj.GetComponent<Image>() == null)
            {
                EditorUtility.DisplayDialog("错误", "替换组件模式需要目标物体上带有Image组件", "确定");
                return;
            }
        }

        // 收集所有需要替换的Image组件（与原代码相同）
        List<Image> targetImages = new List<Image>();

        switch (scope)
        {
            case Scope.SelectedObjects:
                foreach (var go in Selection.gameObjects)
                {
                    targetImages.AddRange(go.GetComponentsInChildren<Image>(true));
                }
                break;
            case Scope.EntireScene:
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    targetImages.AddRange(root.GetComponentsInChildren<Image>(true));
                }
                break;
            case Scope.PrefabsInFolder:
                if (!Directory.Exists(prefabFolderPath))
                {
                    EditorUtility.DisplayDialog("错误", $"文件夹不存在: {prefabFolderPath}", "确定");
                    return;
                }
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        targetImages.AddRange(prefab.GetComponentsInChildren<Image>(true));
                    }
                }
                break;
        }

        if (targetImages.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "没有找到任何包含Image组件的物体", "确定");
            return;
        }

        // --- 核心修改：先解包所有预制体实例，然后执行替换 ---
        int replacedCount = 0;
        List<Image> processedImages = new List<Image>();

        foreach (var img in targetImages)
        {
            GameObject go = img.gameObject;
            // 检查是否为预制体实例（不是预制体资源本身）
            PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(go);
            if (status == PrefabInstanceStatus.Connected || status == PrefabInstanceStatus.Disconnected)
            {
                // 解包预制体实例（只解包最外层，保留子物体的预制体连接）
                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
                // 解包后重新获取Image组件（因为原引用可能失效）
                Image newImg = go.GetComponent<Image>();
                if (newImg == null) continue;
                processedImages.Add(newImg);
            }
            else
            {
                processedImages.Add(img);
            }
        }

        // 现在对解包后的Image列表执行替换
        foreach (var img in processedImages)
        {
            if (replaceMode == ReplaceMode.ReplaceSprite)
            {
                if (sourceSprite != null && img.sprite != sourceSprite)
                    continue;
                Undo.RecordObject(img, "Replace Sprite");
                img.sprite = targetSprite;
                EditorUtility.SetDirty(img);
                replacedCount++;
            }
            else // ReplaceComponent
            {
                Image sourceImg = targetObj.GetComponent<Image>();
                if (sourceImg == null) continue;
                Undo.RecordObject(img, "Replace Component");
                img.sprite = sourceImg.sprite;
                img.color = sourceImg.color;
                img.material = sourceImg.material;
                img.type = sourceImg.type;
                img.fillMethod = sourceImg.fillMethod;
                img.fillAmount = sourceImg.fillAmount;
                img.fillCenter = sourceImg.fillCenter;
                img.fillClockwise = sourceImg.fillClockwise;
                img.preserveAspect = sourceImg.preserveAspect;
                img.raycastTarget = sourceImg.raycastTarget;
                EditorUtility.SetDirty(img);
                replacedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", $"成功替换了 {replacedCount} 个Image组件", "确定");
    }
}