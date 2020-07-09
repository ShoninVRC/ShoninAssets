using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BoneUniquer : EditorWindow
{
    int tryCount = 0;
    bool isCompleted;
    GameObject targetArmature;
    Vector2 _scrollPosition = Vector2.zero;
    [MenuItem("BoneUniquer/BoneUniquer")]
    private static void Create()
    {
        // 生成
        BoneUniquer window = GetWindow<BoneUniquer>("BoneUniquer");
        window.maxSize = window.minSize = new Vector2(400,80);
    }

    private void OnGUI()
    {
        targetArmature = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Armature", "Assign an avatar Armature"), targetArmature, typeof(GameObject), true);
        if(targetArmature == null)
        {
            EditorGUILayout.HelpBox("Assign an Avatar Armature.", MessageType.Warning);
        }
        else
        {
            Transform[] allBones = targetArmature.GetComponentsInChildren<Transform>();
            for(int i = 0; i < allBones.Length - 1; i++)
            {
                allBones[i] = allBones[i + 1];
            }
            Array.Resize(ref allBones, allBones.Length - 1);
            if(allBones != null)
            {
                string boneNames = "Bone Counts: " + allBones.Length;
                /* コメントアウトを外すとボーンの名前を列挙します
                if(allBones != null)
                {
                    foreach(Transform bone in allBones)
                    {
                        if(bone != null)
                        {
                            boneNames += "\r\n" + bone.name;
                        }
                    }

                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                    EditorGUILayout.LabelField(boneNames, GUILayout.Height((allBones.Length + 1) * 13.01f));
                    EditorGUILayout.EndScrollView();
                }
                */
            }

            if (GUILayout.Button("Apply"))
            {
                tryCount = 0;

                isCompleted = IsAllGameObjectsAreUnique(allBones);;

                while(!isCompleted && tryCount++ <= 100)
                {
                    GibeAllBonesNamesDiffirentFromParent(allBones);

                    MakeAllNamesOfObjectsUnique(allBones);

                    isCompleted = IsAllGameObjectsAreUnique(allBones);
                }
            }

            if(isCompleted)
            {
                EditorGUILayout.HelpBox("Made all names of bones unique.", MessageType.Info);
            }
            
            if(tryCount > 100)
            {
                EditorGUILayout.HelpBox("Could not make all names of bones unique. Please try again.", MessageType.Error);
            }
        }
    }

    void GibeAllBonesNamesDiffirentFromParent(Transform[] objects)
    {
        foreach(Transform bone in objects)
        {
            if(bone.name == bone.parent.name)
            {
                Undo.RecordObject(bone.gameObject, "Changed Bone Name");
                bone.name = bone.name + "1";
            }
        }
    }

    void MakeAllNamesOfObjectsUnique(Transform[] objects)
    {
        for(int i = 0; i < objects.Length - 1; i++)
        {
            for(int j = i + 1; j < objects.Length; j++)
            {
                if(objects[i].name == objects[j].name && i != j)
                {
                    int currentindex = 1;
                    bool isNameUnique = false;
                    string newName = null;
                    int tryCount = 0;
                    while(!isNameUnique && tryCount++ <= 100)
                    {
                        isNameUnique = true;
                        newName = objects[i].name + currentindex++;
                        for(int k = 0; k < objects.Length; k++)
                        {
                            if(newName == objects[k].name && i != k)
                            {
                                isNameUnique = false;
                            }
                        }
                    }
                    Undo.RecordObject(objects[i].gameObject, "Changed Bone Name");
                    objects[i].name = newName;
                }
            }
        }
    }

    bool IsAllGameObjectsAreUnique(Transform[] objects)
    {
        for(int i = 0; i < objects.Length; i++)
        {
            for(int j = i; j < objects.Length; j++)
            {
                if(objects[i].name == objects[j].name && i != j)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
