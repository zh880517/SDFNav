using UnityEngine;
using UnityEngine.Rendering;

namespace SDFNav.Editor
{
    [System.Serializable]
    public class SDFPreviewRender
    {
        public Texture SDFTexture;
        public Mesh PlaneMesh;
        public Material Mat;

        public void Build(SDFData data)
        {
            Clear();
            SDFTexture = SDFExportUtil.ToTextureWithDistance(data);
            SDFTexture.hideFlags = HideFlags.HideAndDontSave;
            Vector3 origin = new Vector3(data.Origin.x, 0, data.Origin.y);
            float width = data.Width * data.Grain;
            float height = data.Height * data.Grain;
            PlaneMesh = new Mesh
            {
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new Vector3[]
                {
                    origin,
                    origin + new Vector3(0, 0, height),
                    origin + new Vector3(width, 0, height),
                    origin + new Vector3(width, 0, 0),
                },
                uv = new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(1, 0),
                },
                triangles = new int[]
                {
                    0, 1, 2,
                    2, 3, 0,
                }
            };
            PlaneMesh.RecalculateNormals();
            var shader = Shader.Find("SDFPreview");
            if (shader)
            {
                Mat = new Material(shader);
                Mat.hideFlags = HideFlags.HideAndDontSave;
                Mat.SetTexture("_MainTex", SDFTexture);
                Mat.doubleSidedGI = true;
            }
        }

        public void OnGUI()
        {
            if (SDFTexture)
            {
                GUILayout.Label("SDF图：", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Label(SDFTexture, GUILayout.MinHeight(512), GUILayout.MinWidth(512));
            }
        }

        public void OnSceneGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;
            if (Mat && PlaneMesh)
            {
                CommandBuffer commandBuffer = new CommandBuffer();
                commandBuffer.DrawMesh(PlaneMesh, Matrix4x4.identity, Mat);
                Graphics.ExecuteCommandBuffer(commandBuffer);
            }
        }

        public void Clear()
        {
            if (SDFTexture)
                Object.DestroyImmediate(SDFTexture);
            if (PlaneMesh)
                Object.DestroyImmediate(PlaneMesh);
            if (Mat)
                Object.DestroyImmediate(Mat);
        }
    }
}