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

    [MenuItem("ShoninAssets/Shape Key Value Copy")]
    public static void Create()
    {
        var window = GetWindow<ShapeKeyValueCopy>("Shape Key Value Copy");
        window.minSize = new Vector2(300, 60);
    }

    private void OnGUI()
    {
        copyTransform = EditorGUILayout.ObjectField("Copy meshes root", copyTransform, typeof(Transform)) as Transform;
        pasteTransform = EditorGUILayout.ObjectField("Paste meshes root", pasteTransform, typeof(Transform)) as Transform;

        if (copyTransform && pasteTransform)
        {
            if (GUILayout.Button("Apply"))
            {
                List<SkinnedMeshRenderer> copyRenderers = new List<SkinnedMeshRenderer>();
                GetChildrenRenderers(copyTransform, ref copyRenderers);
                List<SkinnedMeshRenderer> pasteRenderers = new List<SkinnedMeshRenderer>();
                GetChildrenRenderers(pasteTransform, ref pasteRenderers);

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
