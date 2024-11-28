using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class FacialBaker : EditorWindow
{
    [SerializeField]
    Vector2 ScrollPos = Vector2.zero;
    [SerializeField]
    GameObject m_targetRenderersRoot;
    List<SkinnedMeshRenderer> m_renderers;
    bool m_isAvatarAssigned;
    bool[] m_rendererToggles;
    [SerializeField]
    string m_animDirectoryPath = "ShoninAssets/FacialBaker/Bake";
    [SerializeField]
    string m_animFileName;
    [SerializeField]
    bool m_isIncludeZeroWeight;
    [SerializeField]
    AnimationClip m_referenceAnimationClip;
    bool m_rendererListOpen = false;

    [MenuItem("ShoninAssets/FacialBaker")]
    private static void Create()
    {
        // 生成
        FacialBaker window = GetWindow<FacialBaker>("FacialBaker");
        window.minSize = new Vector2(400, 150);
    }

    private void OnGUI()
    {
        ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);

        // オブジェクトが割り当てられたか、変更された時に初期化
        GameObject t_targetAvatar = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Root", "Assign Root Object."), m_targetRenderersRoot, typeof(GameObject), true);
        if (t_targetAvatar != m_targetRenderersRoot)
        {
            m_targetRenderersRoot = t_targetAvatar;
            m_isAvatarAssigned = m_targetRenderersRoot != null;
            if (m_isAvatarAssigned)
            {
                m_renderers = new List<SkinnedMeshRenderer>();
                SkinnedMeshRenderer[] renderers = m_targetRenderersRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    if (renderer.sharedMesh.blendShapeCount > 0)
                    {
                        m_renderers.Add(renderer);
                    }
                }
                m_rendererToggles = new bool[m_renderers.Count];
            }
        }

        // オブジェクトが割り当てられていればシェイプキーのあるオブジェクトを羅列
        if (!m_isAvatarAssigned)
        {
            m_rendererListOpen = false;
            EditorGUILayout.HelpBox("Assign Root Object.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.Space();

            m_rendererListOpen = EditorGUILayout.Foldout(m_rendererListOpen, "SkinnedMeshRenderers"); // 折りたたみ表示
            if (m_rendererListOpen)
            {
                for (int i = 0; i < m_renderers.Count; i++)
                {
                    m_rendererToggles[i] = EditorGUILayout.ToggleLeft(m_renderers[i].name, m_rendererToggles[i]);
                }
            }

            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            if (GUILayout.Button("All", GUILayout.Width(60)))
            {
                for (int i = 0; i < m_rendererToggles.Length; i++)
                {
                    m_rendererToggles[i] = true;
                }
            }
            if (GUILayout.Button("None", GUILayout.Width(60)))
            {
                for (int i = 0; i < m_rendererToggles.Length; i++)
                {
                    m_rendererToggles[i] = false;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // フォルダパス、ファイル名入力
            m_animDirectoryPath = EditorGUILayout.TextField("Directory Path", m_animDirectoryPath);
            m_animFileName = EditorGUILayout.TextField("File Name", m_animFileName);

            EditorGUILayout.Space();

            m_isIncludeZeroWeight = EditorGUILayout.Toggle("Include zero value", m_isIncludeZeroWeight);

            EditorGUILayout.Space();

            m_referenceAnimationClip = (AnimationClip)EditorGUILayout.ObjectField(new GUIContent("Reference animtion clip", "Assign reference animation clip."), m_referenceAnimationClip, typeof(AnimationClip));

            EditorGUILayout.Space();

            // アニメーションファイル作成処理
            if (GUILayout.Button("Bake"))
            {
                string animPath = "";
                AnimationClip animclip = new AnimationClip();
                // ファイルパスの作成
                string animDirectoryPath = "Assets/" + m_animDirectoryPath;

                string animFileName = m_animFileName;

                if (animFileName == null || animFileName.Length < 1)
                {
                    animFileName = DateTime.Now.ToString("yyyyMMddHHmmss");
                }
                animFileName += ".anim";

                if (!Directory.Exists(animDirectoryPath))
                {
                    Directory.CreateDirectory(animDirectoryPath);
                }

                animPath = Path.Combine(animDirectoryPath, animFileName);

                // 参照するアニメーションファイルがあれば内容をコピー
                if (m_referenceAnimationClip != null)
                {
                    var bindings = AnimationUtility.GetCurveBindings(m_referenceAnimationClip);
                    foreach (var binding in bindings)
                    {
                        var curve = AnimationUtility.GetEditorCurve(m_referenceAnimationClip, binding);
                        AnimationUtility.SetEditorCurve(animclip, binding, curve);
                    }
                }

                for (int i = 0; i < m_renderers.Count; i++)
                {
                    if (m_rendererToggles[i])
                    {
                        // オブジェクトパスの取得
                        string objectPath = m_renderers[i].name;
                        Transform parentTransform = m_renderers[i].transform.parent;

                        while (parentTransform.parent != null)
                        {
                            objectPath = parentTransform.name + "/" + objectPath;
                            parentTransform = parentTransform.parent;
                        }

                        // シェイプキーの追加
                        for (int j = 0; j < m_renderers[i].sharedMesh.blendShapeCount; j++)
                        {

                            AnimationCurve curve = new AnimationCurve();
                            float blendShapeWeight = m_renderers[i].GetBlendShapeWeight(j);

                            if (m_isIncludeZeroWeight || blendShapeWeight > 0)
                            {
                                EditorCurveBinding curveBinding = new EditorCurveBinding();
                                curveBinding.path = objectPath;
                                curveBinding.type = typeof(SkinnedMeshRenderer);
                                curveBinding.propertyName = "blendShape." + m_renderers[i].sharedMesh.GetBlendShapeName(j);
                                curve.AddKey(0f, m_renderers[i].GetBlendShapeWeight(j));
                                AnimationUtility.SetEditorCurve(animclip, curveBinding, curve);
                            }
                        }
                    }
                }

                AssetDatabase.CreateAsset(
                        animclip,
                        AssetDatabase.GenerateUniqueAssetPath(animPath)
                    );
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
