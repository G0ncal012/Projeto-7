using System.Collections;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRender;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture)
    {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        Mesh mesh = meshData.CreateMesh();
        meshFilter.sharedMesh = mesh;

        // ✅ Cria um material novo por código para evitar conflitos
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = texture;
        meshRenderer.sharedMaterial = mat;

        if (mesh.triangles.Length >= 3)
        {
            MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }
}