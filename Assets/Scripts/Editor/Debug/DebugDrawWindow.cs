using UnityEditor;
using UnityEngine;

namespace SDFNav.Editor
{
    public class DebugDrawWindow : EditorWindow
    {
        public EdgeData Edge;

        public Transform TestPoint;
        public EdgeSDFResult SDFResult;
        public SDFPreviewRender SDFPreview = new SDFPreviewRender();
        public static void DrawEdge(EdgeData edge)
        {
            var window = GetWindow<DebugDrawWindow>();
            window.Edge = edge;
            window.SDFTest();
        }

        public static void DrawSDF(SDFData data)
        {
            var window = GetWindow<DebugDrawWindow>();
            window.SDFPreview.Build(data);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        private void SDFTest()
        {
            if (TestPoint)
            {
                Vector2 pt = NavMeshExportUtil.ToV2(TestPoint.position);
                SDFResult = SDFExportUtil.SDF(pt, Edge);
            }
        }

        private void OnGUI()
        {
            TestPoint = EditorGUILayout.ObjectField(TestPoint, typeof(Transform), true) as Transform;
            if (TestPoint && Edge != null)
            {
                //if (GUILayout.Button("刷新"))
                {
                    SDFTest();
                }
            }
        }

        private void OnSceneGUI(SceneView view)
        {
            if (Edge != null)
            {
                DebugDrawUtil.DrawEdgeOnScene(Edge);
                if (TestPoint)
                {
                    DebugDrawUtil.DrawSDFResult(SDFResult, Edge);
                }
            }
            SDFPreview.OnSceneGUI();
        }
    }
}