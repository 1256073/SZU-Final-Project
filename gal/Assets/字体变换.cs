using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

public class TMPFontReplaceAll : EditorWindow
{
    TMP_FontAsset targetYaHeiFont;

    [MenuItem("Tools/TMP全量替换微软雅黑(场景+预制体)")]
    static void OpenWin()
    {
        GetWindow<TMPFontReplaceAll>("TMP批量改字体");
    }

    void OnGUI()
    {
        GUILayout.Space(12);
        targetYaHeiFont = (TMP_FontAsset)EditorGUILayout.ObjectField("绑定微软雅黑TMP字体", targetYaHeiFont, typeof(TMP_FontAsset), false);
        GUILayout.Space(10);

        if (GUILayout.Button("1、替换当前场景所有TMP", GUILayout.Height(28)))
        {
            ReplaceSceneTmp();
        }
        if (GUILayout.Button("2、批量修改全工程所有Prefab(关键！解决运行还原)", GUILayout.Height(28)))
        {
            ReplaceAllPrefabTmp();
        }
    }

    // 修改Hierarchy场景物体
    void ReplaceSceneTmp()
    {
        if (targetYaHeiFont == null) { EditorUtility.DisplayDialog("提示", "先赋值TMP字体资源！", "OK"); return; }
        int cnt = 0;
        // TMP_Text
        TMP_Text[] allText = Object.FindObjectsOfType<TMP_Text>(includeInactive: true);
        foreach (var t in allText)
        {
            Undo.RecordObject(t, "更换TMP字体");
            t.font = targetYaHeiFont;
            EditorUtility.SetDirty(t);
            cnt++;
        }
        // TMP输入框自带字体（极易遗漏）
        TMP_InputField[] allInput = Object.FindObjectsOfType<TMP_InputField>(includeInactive: true);
        foreach (var input in allInput)
        {
            Undo.RecordObject(input, "修改输入框字体");
            input.fontAsset = targetYaHeiFont;
            EditorUtility.SetDirty(input);
            cnt++;
        }
        EditorUtility.DisplayDialog("场景替换完成", $"共修改 {cnt} 处TMP文字", "OK");
    }

    // 遍历Project全部预制体、永久保存资源（根治运行变回默认）
    void ReplaceAllPrefabTmp()
    {
        if (targetYaHeiFont == null) { EditorUtility.DisplayDialog("提示", "先赋值TMP字体资源！", "OK"); return; }
        // 查找项目内所有prefab资源
        string[] prefabGUID = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;

        foreach (string guid in prefabGUID)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // 加载预制体原始资源
            GameObject prefabSource = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;

            // 查找所有子物体TMP
            TMP_Text[] tmpTexts = prefabSource.GetComponentsInChildren<TMP_Text>(includeInactive: true);
            foreach (var t in tmpTexts)
            {
                t.font = targetYaHeiFont;
                changed = true;
                total++;
            }
            // 输入框
            TMP_InputField[] tmpInput = prefabSource.GetComponentsInChildren<TMP_InputField>(includeInactive: true);
            foreach (var input in tmpInput)
            {
                input.fontAsset = targetYaHeiFont;
                changed = true;
                total++;
            }

            // 有修改才保存预制体
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabSource, path);
            }
            // 释放加载
            PrefabUtility.UnloadPrefabContents(prefabSource);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("预制体替换完毕", $"全项目预制体共修改 {total} 个TMP组件", "OK");
    }
}