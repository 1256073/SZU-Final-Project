using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class ImageReplacer : EditorWindow
{
    [MenuItem("Tools/批量替换UI Image的材质/精灵")]
    public static void ShowWindow()
    {
        GetWindow<ImageReplacer>("UI Image批量替换工具");
    }

    // 替换模式选择
    public enum ReplaceMode { Material, Sprite }
    private ReplaceMode replaceMode = ReplaceMode.Material;

    // 替换目标
    private Material oldMaterial;
    private Material newMaterial;
    private Sprite oldSprite;
    private Sprite newSprite;

    // 输出设置
    private string outputFolder = "Assets/ModifiedPrefabs";
    private bool replaceInScenes = true;
    private List<string> logMessages = new List<string>();

    void OnGUI()
    {
        GUILayout.Label("⚠️ 只读Prefab UI Image替换工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 替换模式
        replaceMode = (ReplaceMode)EditorGUILayout.EnumPopup("替换模式", replaceMode);
        GUILayout.Space(5);

        if (replaceMode == ReplaceMode.Material)
        {
            oldMaterial = (Material)EditorGUILayout.ObjectField("旧材质（要替换的）", oldMaterial, typeof(Material), false);
            newMaterial = (Material)EditorGUILayout.ObjectField("新材质（替换成这个）", newMaterial, typeof(Material), false);
        }
        else
        {
            oldSprite = (Sprite)EditorGUILayout.ObjectField("旧Sprite（要替换的）", oldSprite, typeof(Sprite), false);
            newSprite = (Sprite)EditorGUILayout.ObjectField("新Sprite（替换成这个）", newSprite, typeof(Sprite), false);
        }

        GUILayout.Space(5);
        outputFolder = EditorGUILayout.TextField("输出Prefab的目录", outputFolder);
        replaceInScenes = EditorGUILayout.Toggle("同时替换当前场景中的实例", replaceInScenes);

        GUILayout.Space(10);
        if (GUILayout.Button("开始批量替换", GUILayout.Height(30)))
        {
            // 校验参数
            if (replaceMode == ReplaceMode.Material)
            {
                if (oldMaterial == null || newMaterial == null)
                {
                    EditorUtility.DisplayDialog("错误", "请先选择旧材质和新材质！", "确定");
                    return;
                }
                if (oldMaterial == newMaterial)
                {
                    EditorUtility.DisplayDialog("错误", "新旧材质不能是同一个！", "确定");
                    return;
                }
            }
            else
            {
                if (oldSprite == null || newSprite == null)
                {
                    EditorUtility.DisplayDialog("错误", "请先选择旧Sprite和新Sprite！", "确定");
                    return;
                }
                if (oldSprite == newSprite)
                {
                    EditorUtility.DisplayDialog("错误", "新旧Sprite不能是同一个！", "确定");
                    return;
                }
            }

            ReplaceImages();
        }

        GUILayout.Space(10);
        GUILayout.Label("操作日志：", EditorStyles.boldLabel);
        foreach (var msg in logMessages)
        {
            EditorGUILayout.HelpBox(msg, MessageType.Info);
        }
    }

    void ReplaceImages()
    {
        logMessages.Clear();
        logMessages.Add("=== 开始批量替换 ===");

        // 1. 确保输出目录存在
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
            logMessages.Add($"创建输出目录：{outputFolder}");
        }

        // 2. 处理项目中所有Prefab（跳过只读Package目录的原始Prefab）
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int prefabCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // 跳过Package目录的原始Prefab，避免修改只读文件
            if (path.StartsWith("Packages/"))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool modified = false;

            // 获取所有Image组件（用for循环，避免foreach迭代变量问题）
            Image[] images = prefab.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                bool needReplace = false;

                // 判断是否需要替换
                if (replaceMode == ReplaceMode.Material)
                {
                    if (image.material == oldMaterial)
                        needReplace = true;
                }
                else
                {
                    if (image.sprite == oldSprite)
                        needReplace = true;
                }

                if (needReplace)
                {
                    // 复制Prefab到输出目录（如果不是已经复制过的）
                    if (!path.StartsWith(outputFolder))
                    {
                        string fileName = Path.GetFileName(path);
                        string newPath = Path.Combine(outputFolder, fileName);
                        // 修复API错误，使用正确的GenerateUniqueAssetPath
                        newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);
                        AssetDatabase.CopyAsset(path, newPath);
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
                        images = prefab.GetComponentsInChildren<Image>(true);
                        image = images[i];
                        path = newPath;
                        logMessages.Add($"复制Prefab到：{newPath}");
                    }

                    // 执行替换
                    if (replaceMode == ReplaceMode.Material)
                    {
                        image.material = newMaterial;
                        logMessages.Add($"替换Image材质：{path} 中的 {image.gameObject.name}");
                    }
                    else
                    {
                        image.sprite = newSprite;
                        logMessages.Add($"替换Image Sprite：{path} 中的 {image.gameObject.name}");
                    }

                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefab);
                prefabCount++;
            }
        }

        // 3. 替换当前场景中的实例
        if (replaceInScenes)
        {
            GameObject[] sceneObjects = Object.FindObjectsOfType<GameObject>(true);
            int sceneCount = 0;

            foreach (var go in sceneObjects)
            {
                Image[] images = go.GetComponentsInChildren<Image>(true);
                foreach (var image in images)
                {
                    bool needReplace = false;
                    if (replaceMode == ReplaceMode.Material)
                    {
                        if (image.material == oldMaterial)
                            needReplace = true;
                    }
                    else
                    {
                        if (image.sprite == oldSprite)
                            needReplace = true;
                    }

                    if (needReplace)
                    {
                        if (replaceMode == ReplaceMode.Material)
                            image.material = newMaterial;
                        else
                            image.sprite = newSprite;

                        EditorUtility.SetDirty(image);
                        sceneCount++;
                    }
                }
            }
            logMessages.Add($"场景中替换了 {sceneCount} 个Image实例");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"处理完成！\n修改了 {prefabCount} 个Prefab", "确定");
        logMessages.Add("=== 替换完成 ===");
        Repaint();
    }
}