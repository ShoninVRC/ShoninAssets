using UnityEngine;
using UnityEditor;

namespace ShoninAssets
{
    public class DBRootSet : EditorWindow
    {
        public GameObject target = null;
        private bool isCompleted = false;

        [MenuItem("ShoninAssets/DBRootSet")]
        public static void ShowWindow()
        {
            EditorWindow window = EditorWindow.GetWindow(typeof(DBRootSet));
            window.maxSize = window.minSize = new Vector2(250, 70);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Select an Avatar and push the button");

            GameObject newTarget = (GameObject)EditorGUILayout.ObjectField(target, typeof(GameObject), true);
            if (target != newTarget)
            {
                target = newTarget;
                isCompleted = false;
            }

            if (target != null)
            {
                //元のInspector部分の下にボタンを表示
                if (GUILayout.Button("Set Dynamic Bone root object"))
                {
                    if (target == null)
                    {
                        return;
                    }
                    foreach (DynamicBone db in target.GetComponentsInChildren<DynamicBone>())
                    {
                        if (db.m_Root == null || db.m_Root != db.transform)
                        {
                            db.m_Root = db.transform;
                        }
                    }
                    isCompleted = true;
                }
                if (isCompleted)
                {
                    EditorGUILayout.LabelField("Completed!");
                }
            }
        }
    }
}