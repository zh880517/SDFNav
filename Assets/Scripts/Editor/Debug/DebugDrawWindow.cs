using UnityEditor;
namespace SDFNav.Editor
{
    public class DebugDrawWindow : EditorWindow
    {
        public EdgeData Edge;

        public static void DrawEdge(EdgeData edge)
        {
            GetWindow<DebugDrawWindow>().Edge = edge;
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        private void OnSceneGUI(SceneView view)
        {
            if (Edge != null)
            {
                DebugDrawUtil.DrawEdgeOnScene(Edge);
            }
        }
    }
}