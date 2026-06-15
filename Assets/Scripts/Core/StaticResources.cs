using System.Collections.Generic;
using UnityEngine;

namespace RealmCommander.Core
{
    public static class StaticResources
    {
        private static readonly List<Material> materials = new List<Material>();
        private static readonly List<Mesh> meshes = new List<Mesh>();
        private static readonly List<Texture2D> textures = new List<Texture2D>();

        public static Material GetOrCreateMaterial(string shaderName, Color color)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader) { color = color };
            materials.Add(mat);
            return mat;
        }

        public static Mesh CreateRingMesh(float outerRadius, float innerRadius, int segments)
        {
            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[segments * 2];
            int[] triangles = new int[segments * 6];
            Vector2[] uv = new Vector2[segments * 2];

            float angleStep = 360f / segments;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;

                vertices[i * 2] = new Vector3(Mathf.Cos(angle) * outerRadius, Mathf.Sin(angle) * outerRadius, 0);
                uv[i * 2] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);

                vertices[i * 2 + 1] = new Vector3(Mathf.Cos(angle) * innerRadius, Mathf.Sin(angle) * innerRadius, 0);
                uv[i * 2 + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);

                int nextI = (i + 1) % segments;
                triangles[i * 6] = i * 2;
                triangles[i * 6 + 1] = nextI * 2;
                triangles[i * 6 + 2] = i * 2 + 1;
                triangles[i * 6 + 3] = nextI * 2;
                triangles[i * 6 + 4] = nextI * 2 + 1;
                triangles[i * 6 + 5] = i * 2 + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            meshes.Add(mesh);
            return mesh;
        }

        public static Texture2D CreatePixelTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            textures.Add(texture);
            return texture;
        }

        public static void Cleanup()
        {
            foreach (var mat in materials)
                if (mat != null) Object.Destroy(mat);
            materials.Clear();

            foreach (var mesh in meshes)
                if (mesh != null) Object.Destroy(mesh);
            meshes.Clear();

            foreach (var tex in textures)
                if (tex != null) Object.Destroy(tex);
            textures.Clear();
        }
    }
}
