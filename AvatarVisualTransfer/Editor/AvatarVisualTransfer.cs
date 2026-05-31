using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AvatarVisualTransfer : EditorWindow
{
    public static Transform copyTransform;
    public static Transform pasteTransform;

    public static bool exactMatch;
    public static bool includeChildren;

    public static bool transferMaterials;
    public static bool transferBlendShapes;
    public static bool transferPositions;
    public static bool onlyChildrenPositions;
    public static bool transferRotations;
    public static bool transferScales;

    [MenuItem("ShoninAssets/Avatar Visual Transfer")]
    public static void Create()
    {
        var window = GetWindow<AvatarVisualTransfer>("Avatar Visual Transfer");
        window.minSize = new Vector2(400, 280);
    }

    private void OnGUI()
    {
        copyTransform = EditorGUILayout.ObjectField("コピー元オブジェクト", copyTransform, typeof(Transform)) as Transform;
        pasteTransform = EditorGUILayout.ObjectField("コピー先オブジェクト", pasteTransform, typeof(Transform)) as Transform;

        GUILayout.Space(10);

        GUILayout.Label("対象");
        EditorGUI.indentLevel++;
        transferMaterials = EditorGUILayout.ToggleLeft("マテリアル", transferMaterials);
        transferBlendShapes = EditorGUILayout.ToggleLeft("ブレンドシェイプ", transferBlendShapes);
        transferPositions = EditorGUILayout.ToggleLeft("位置", transferPositions);
        if(transferPositions)
        {
            EditorGUI.indentLevel++;
            onlyChildrenPositions = EditorGUILayout.ToggleLeft("子オブジェクトのみ", onlyChildrenPositions);
            EditorGUI.indentLevel--;
        }
        transferRotations = EditorGUILayout.ToggleLeft("回転", transferRotations);
        transferScales = EditorGUILayout.ToggleLeft("大きさ", transferScales);
        EditorGUI.indentLevel--;

        GUILayout.Space(10);

        GUILayout.Label("ブレンドシェイプ転送オプション");
        EditorGUI.indentLevel++;
        exactMatch = EditorGUILayout.ToggleLeft("オブジェクト名とブレンドシェイプが完全に一致している場合のみコピーする", exactMatch);
        includeChildren = EditorGUILayout.ToggleLeft("子オブジェクトを含める", includeChildren);
        EditorGUI.indentLevel--;

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup((!copyTransform || !pasteTransform) || (!transferMaterials && !transferBlendShapes && !transferPositions && !transferRotations && !transferScales));
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        if (GUILayout.Button("転送"))
        {
            TransferMaterialsSpecify();
            TransferBlendShapeValues();
            TransferTransforms();
        }
        GUILayout.Space(10);
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
    }

    private static void GetChildrenRenderers<T>(Transform obj, ref List<T> allChildren) where T : Component
    {
        Transform children = obj.GetComponentInChildren<Transform>();
        if (children.childCount == 0)
        {
            return;
        }
        foreach (Transform ob in children)
        {
            T renderer = ob.GetComponent<T>();
            if (renderer)
            {
                allChildren.Add(renderer);
            }
            GetChildrenRenderers<T>(ob, ref allChildren);
        }
    }

    private static void TransferMaterialsSpecify()
    {
        if(!transferMaterials)
        {
            return;
        }

        List<SkinnedMeshRenderer> copyRenderers = new List<SkinnedMeshRenderer>();
        GetChildrenRenderers<SkinnedMeshRenderer>(copyTransform, ref copyRenderers);
        List<SkinnedMeshRenderer> pasteRenderers = new List<SkinnedMeshRenderer>();
        GetChildrenRenderers<SkinnedMeshRenderer>(pasteTransform, ref pasteRenderers);

        foreach (SkinnedMeshRenderer copyRenderer in copyRenderers)
        {
            foreach (SkinnedMeshRenderer pasteRenderer in pasteRenderers)
            {
                if (pasteRenderer.transform.name == copyRenderer.transform.name)
                {
                    Undo.RecordObject(pasteRenderer, "Copy Materials Specify");
                    pasteRenderer.sharedMaterials = copyRenderer.sharedMaterials;
                }
            }
        }
    }

    private static void TransferBlendShapeValues()
    {
        if(!transferBlendShapes)
        {
            return;
        }

        List<SkinnedMeshRenderer> copyRenderers = new List<SkinnedMeshRenderer>();
        List<SkinnedMeshRenderer> pasteRenderers = new List<SkinnedMeshRenderer>();

        if (includeChildren)
        {
            GetChildrenRenderers<SkinnedMeshRenderer>(copyTransform, ref copyRenderers);
            GetChildrenRenderers<SkinnedMeshRenderer>(pasteTransform, ref pasteRenderers);
        }
        else
        {
            SkinnedMeshRenderer renderer = copyTransform.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                copyRenderers.Add(renderer);
            }

            renderer = pasteTransform.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                pasteRenderers.Add(renderer);
            }
        }

        if (exactMatch)
        {
            foreach (SkinnedMeshRenderer copyRenderer in copyRenderers)
            {
                foreach (SkinnedMeshRenderer pasteRenderer in pasteRenderers)
                {
                    if (pasteRenderer.transform.name == copyRenderer.transform.name)
                    {
                        Undo.RecordObject(pasteRenderer, "Copy BlendShape Values");
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
                Undo.RecordObject(pasteRenderer, "Copy BlendShape Values");
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

    private static void TransferTransforms()
    {
        if(!transferPositions && !transferRotations && !transferScales)
        {
            return;
        }

        if (transferPositions && !onlyChildrenPositions)
        {
            Undo.RecordObject(pasteTransform, "Transfer all children transforms");
            pasteTransform.localPosition = copyTransform.localPosition;
        }
        if (transferRotations)
        {
            Undo.RecordObject(pasteTransform, "Transfer all children transforms");
            pasteTransform.localRotation = copyTransform.localRotation;
        }
        if (transferScales)
        {
            Undo.RecordObject(pasteTransform, "Transfer all children transforms");
            pasteTransform.localScale = copyTransform.localScale;
        }

        List<Transform> copyTransforms = new List<Transform>();
        GetChildrenRenderers(AvatarVisualTransfer.copyTransform, ref copyTransforms);
        List<Transform> pasteTransforms = new List<Transform>();
        GetChildrenRenderers<Transform>(pasteTransform, ref pasteTransforms);

        foreach (Transform copyTransform in copyTransforms)
        {
            foreach (Transform pasteTransform in pasteTransforms)
            {
                if (pasteTransform.name == copyTransform.name)
                {
                    Undo.RecordObject(pasteTransform, "Transfer all children transforms");

                    if (transferPositions)
                    {
                        pasteTransform.localPosition = copyTransform.localPosition;
                    }

                    if (transferRotations)
                    {
                        pasteTransform.localRotation = copyTransform.localRotation;
                    }

                    if (transferScales)
                    {
                        pasteTransform.localScale = copyTransform.localScale;
                    }
                }
            }
        }
    }
}
