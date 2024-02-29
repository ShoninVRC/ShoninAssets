using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class MaterialChanger : EditorWindow
{
    public enum Mode
    {
        _SkinnedMeshRenderer,
        AvatarRoot
    }
    GameObject targetAvatar;                        // アバターのフィールド
    public SkinnedMeshRenderer[] renderers;         // 複数のSkinned Mesh Rendererのフィールド
    DefaultAsset searchFolder = null;               // 変更マテリアルフォルダのフィールド
    string dir_path = "";                           // 変更マテリアルフォルダのパスのフィールド
    bool _nameListOpen;                             // 折りたたみ表示のフィールド
    Vector2 ScrollPos = Vector2.zero;
    Mode SelectMode = Mode._SkinnedMeshRenderer;

    List<string> nameList = new List<string>();     // マテリアル名リストのフィールド

    [MenuItem("ShoninAssets/MaterialChanger")]
    private static void Create()
    {
        // 生成
        MaterialChanger window = GetWindow<MaterialChanger>("MaterialChanger");
        window.maxSize = window.minSize = new Vector2(400,150);
    }

    private void OnGUI()
    {
        SelectMode = (Mode)EditorGUILayout.EnumPopup("SelectMode", SelectMode);
        switch(SelectMode)
        {
            case Mode._SkinnedMeshRenderer :
                ScriptableObject target = this;
                SerializedObject so = new SerializedObject(target);
                SerializedProperty stringsProperty = so.FindProperty("renderers");
                EditorGUILayout.PropertyField(stringsProperty, true); 
                so.ApplyModifiedProperties(); 
                break;
            case Mode.AvatarRoot :
                targetAvatar = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Avatar", "Assign an avatar root"), targetAvatar, typeof(GameObject), true);
                break;
        }

        bool isAvatarAssigned = targetAvatar != null || isRendererArrayContainsNotNull(renderers);
        if(!isAvatarAssigned)
        {
            EditorGUILayout.HelpBox("Assign an Avatar.", MessageType.Warning);
        }

        searchFolder = (DefaultAsset)EditorGUILayout.ObjectField("Target folder", searchFolder, typeof(DefaultAsset), true);
        bool isFolderAssigned = searchFolder != null;
        if (isFolderAssigned)
        {
            dir_path = AssetDatabase.GetAssetOrScenePath(searchFolder);
            string[] folderList = dir_path.Split('/');
            if (folderList[folderList.Length - 1].Contains("."))
                searchFolder = null;
            int idx = dir_path.IndexOf("Assets");
            if(idx >= 0)
            {
                dir_path = dir_path.Remove(idx, "Assets".Length);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a Material Folder.", MessageType.Warning);
        }

        if(isAvatarAssigned && isFolderAssigned)
        {
            // Applyボタン
            if (GUILayout.Button("Apply"))
            {
                switch(SelectMode)
                {
                    case Mode._SkinnedMeshRenderer :
                        foreach(SkinnedMeshRenderer renderer in renderers)
                        {
                            ChangeSkinnedMeshRendererMaterials(renderer);
                        }
                        break;
                    case Mode.AvatarRoot :
                        foreach(SkinnedMeshRenderer renderer in targetAvatar.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            ChangeSkinnedMeshRendererMaterials(renderer);
                        }
                        break;
                }
            }
        }
    }

    void ChangeSkinnedMeshRendererMaterials(SkinnedMeshRenderer renderer)
    {
        if(renderer == null)
        {
            return;
        }
        int materialsize = renderer.sharedMaterials.Length;     // Skinned Mesh Rendererのマテリアル数のフィールド
        GUILayout.Label("Material Size : " + materialsize);     // Skinned Mesh Rendererのマテリアル数

        for (int i = 0; i < materialsize; i++)                  //
        {                                                       // マテリアル名リスト
            nameList.Add(renderer.sharedMaterials[i].name);     //
        }                                                       //

        _nameListOpen = EditorGUILayout.Foldout(_nameListOpen, "Material name list"); // 折りたたみ表示
        if (_nameListOpen)
        {
            ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
            for (int i = 0; i < materialsize; i++)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(nameList[i]);     // マテリアル名
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndScrollView();
        }

        int materialIndex = 0;
        Material[] newMaterials = renderer.sharedMaterials;
        foreach (Material material in newMaterials)
        {
            string materialName = dir_path + "/" + material.name + ".mat";

            // マテリアルがディレクトリに存在するか確認
            if (File.Exists(Application.dataPath + materialName))   // マテリアルがあれば
            {
                Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets" + materialName);
                if (newMaterial != null)    // マテリアルの取得に成功したら
                {
                    nameList[materialIndex] = "'" + material.name + "'の変更に成功しました";
                    newMaterials[materialIndex] = newMaterial;
                }
                else                        // マテリアルの取得に失敗したら
                    nameList[materialIndex] = "'Assets" + materialName + "'の変更に失敗しました";
            }
            else                                                    // マテリアルがなければ
            {
                nameList[materialIndex] = "'" + Application.dataPath + materialName + "'はディレクトリ内に存在しません";
                newMaterials[materialIndex] = material;
            }
            materialIndex++;
        }
        Undo.RecordObject(renderer, "Changed Materials"); // Undo操作対応
        renderer.sharedMaterials = newMaterials;
    }

    bool isRendererArrayContainsNotNull(SkinnedMeshRenderer[] renderers)
    {
        if(renderers == null)
        {
            return false;
        }
        foreach(SkinnedMeshRenderer renderer in renderers)
        {
            if(renderer != null)
            {
                return true;
            }
        }
        return false;
    }
}
