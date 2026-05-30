using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[Serializable]
public class ShapeKeyValueCopy : EditorWindow
{
    public static Transform copyTransform;
    public static Transform pasteTransform;

    public static bool exactMatch;
    public static bool includeChildren;

    [MenuItem("ShoninAssets/Shape Key Value Copy")]
    public static void Create()
    {
        var window = GetWindow<ShapeKeyValueCopy>("Shape Key Value Copy");
        window.minSize = new Vector2(300, 60);
    }

    private void OnGUI()
    {
        copyTransform = EditorGUILayout.ObjectField("コピー元オブジェクト", copyTransform, typeof(Transform)) as Transform;

        pasteTransform = EditorGUILayout.ObjectField("ペースト先オブジェクト", pasteTransform, typeof(Transform)) as Transform;

        EditorGUIUtility.labelWidth = position.width - 25;
        exactMatch = EditorGUILayout.Toggle("オブジェクト名とブレンドシェイプが完全に一致している場合のみコピーする", exactMatch);
        includeChildren = EditorGUILayout.Toggle("子オブジェクトを含める", includeChildren);

        if (copyTransform && pasteTransform)
        {
            if (GUILayout.Button("Apply"))
            {
                List<SkinnedMeshRenderer> copyRenderers = new List<SkinnedMeshRenderer>();
                List<SkinnedMeshRenderer> pasteRenderers = new List<SkinnedMeshRenderer>();

                {
                    SkinnedMeshRenderer renderer = copyTransform.GetComponent<SkinnedMeshRenderer>();
                    if(renderer != null)
                    {
                        copyRenderers.Add(renderer);
                    }

                    renderer = pasteTransform.GetComponent<SkinnedMeshRenderer>();
                    if (renderer != null)
                    {
                        pasteRenderers.Add(renderer);
                    }
                }

                if (!includeChildren)
                {
                    GetChildrenRenderers(copyTransform, ref copyRenderers);
                    GetChildrenRenderers(pasteTransform, ref pasteRenderers);
                }

                if (exactMatch)
                {
                    foreach (SkinnedMeshRenderer copyRenderer in copyRenderers)
                    {
                        foreach (SkinnedMeshRenderer pasteRenderer in pasteRenderers)
                        {
                            if (pasteRenderer.transform.name == copyRenderer.transform.name)
                            {
                                Undo.RecordObject(pasteRenderer, "Copy Shape Key Values");
                                int blendShapeCount = copyRenderer.sharedMesh.blendShapeCount;
                                for (int i = 0; i < blendShapeCount; i++)
                                {
                                    pasteRenderer.SetBlendShapeWeight(i, copyRenderer.GetBlendShapeWeight(i));
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (SkinnedMeshRenderer pasteRenderer in pasteRenderers)
                    {
                        Undo.RecordObject(pasteRenderer, "Copy Shape Key Values");
                        foreach (SkinnedMeshRenderer copyRenderer in copyRenderers)
                        {
                            int copyBlendShapeCount = copyRenderer.sharedMesh.blendShapeCount;
                            int pasteBlendShapeCount = pasteRenderer.sharedMesh.blendShapeCount;

                            for (int i = 0; i < copyBlendShapeCount; i++)
                            {
                                for (int j = 0; j < pasteBlendShapeCount; j++)
                                {
                                    if (copyRenderer.sharedMesh.GetBlendShapeName(i) == pasteRenderer.sharedMesh.GetBlendShapeName(j))
                                    {
                                        pasteRenderer.SetBlendShapeWeight(j, copyRenderer.GetBlendShapeWeight(i));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static void GetChildrenRenderers(Transform obj, ref List<SkinnedMeshRenderer> allChildren)
    {
        Transform children = obj.GetComponentInChildren<Transform>();
        if (children.childCount == 0)
        {
            return;
        }
        foreach (Transform ob in children)
        {
            SkinnedMeshRenderer renderer = ob.GetComponent<SkinnedMeshRenderer>();
            if (renderer)
            {
                allChildren.Add(renderer);
            }
            GetChildrenRenderers(ob, ref allChildren);
        }
    }

}
