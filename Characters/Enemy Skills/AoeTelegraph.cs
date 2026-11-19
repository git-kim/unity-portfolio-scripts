using UnityEngine;

namespace Characters.EnemySkills
{
    /// <summary>Ground marker for an area attack: a fixed outer ring and an inner disc that grows with cast progress.</summary>
    public class AoeTelegraph : MonoBehaviour
    {
        private const int Segments = 64;
        private const float GroundOffset = 0.06f;

        private Transform fill;

        public float Progress
        {
            set
            {
                var s = Mathf.Clamp01(value);
                fill.localScale = new Vector3(s, 1f, s);
            }
        }

        public static AoeTelegraph Create(Vector3 center, float radius, Material material, Color color)
        {
            var root = new GameObject("AOE Telegraph").AddComponent<AoeTelegraph>();
            root.transform.position = center + Vector3.up * GroundOffset;

            var ringColor = color;
            ringColor.a = Mathf.Min(1f, color.a * 2f);
            CreatePart("Ring", root.transform, BuildDisc(radius * 0.95f, radius), material, ringColor);
            var fillColor = color;
            fillColor.a *= 0.6f;
            root.fill = CreatePart("Fill", root.transform, BuildDisc(0f, radius), material, fillColor).transform;
            root.Progress = 0f;
            return root;
        }

        private static GameObject CreatePart(string name, Transform parent, Mesh mesh, Material material, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(block);
            return go;
        }

        // A flat annulus (a disc when innerRadius is 0) facing up, built once per telegraph.
        private static Mesh BuildDisc(float innerRadius, float outerRadius)
        {
            var vertices = new Vector3[(Segments + 1) * 2];
            var triangles = new int[Segments * 6];
            for (var i = 0; i <= Segments; i++)
            {
                var angle = i * Mathf.PI * 2f / Segments;
                var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = dir * innerRadius;
                vertices[i * 2 + 1] = dir * outerRadius;
            }
            for (var i = 0; i < Segments; i++)
            {
                var v = i * 2;
                var t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 3; triangles[t + 2] = v + 1;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            var colors = new Color32[vertices.Length];
            for (var i = 0; i < colors.Length; i++)
                colors[i] = new Color32(255, 255, 255, 255); // the shader multiplies vertex colour by _Color
            var mesh = new Mesh { name = "AOE Disc", vertices = vertices, triangles = triangles, colors32 = colors };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            foreach (var meshFilter in GetComponentsInChildren<MeshFilter>())
                Destroy(meshFilter.sharedMesh);
        }
    }
}
