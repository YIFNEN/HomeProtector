// 기존 ResourceObject 클래스에 DraggableResource 컴포넌트를 추가하는 확장 모듈
// ResourceObject.cs에 추가하거나 별도의 확장 메소드로 구현할 수 있습니다.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 에디터 전용: ResourceObject에 자동으로 드래그 기능 추가하는 커스텀 에디터
[CustomEditor(typeof(ResourceObject))]
public class ResourceObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ResourceObject resourceObj = (ResourceObject)target;

        // 기존 Inspector UI 표시
        DrawDefaultInspector();

        EditorGUILayout.Space();

        // 드래그 기능 추가 버튼
        if (resourceObj.GetComponent<DraggableResource>() == null)
        {
            if (GUILayout.Button("드래그 기능 추가"))
            {
                Undo.RecordObject(resourceObj.gameObject, "Add Draggable Component");
                resourceObj.gameObject.AddComponent<DraggableResource>();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("드래그 기능이 활성화되어 있습니다.", MessageType.Info);

            if (GUILayout.Button("드래그 기능 제거"))
            {
                Undo.RecordObject(resourceObj.gameObject, "Remove Draggable Component");
                DestroyImmediate(resourceObj.GetComponent<DraggableResource>());
            }
        }
    }
}
#endif