using UnityEngine;

// Asigna las seis regiones correctas de una piel de Minecraft a un cubo de Unity.
public static class MinecraftSkinBox
{
    public static Material CrearMaterial(string nombre)
    {
        Texture2D textura = Resources.Load<Texture2D>("Minecraft/Entity/" + nombre);
        if (textura == null) return null;
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(shader) { mainTexture = textura, color = Color.white };
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
        if (nombre == "skeleton")
        {
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 1f);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0.45f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return material;
    }

    public static void Aplicar(Transform pieza, Material material, int u, int v,
        int ancho, int alto, int fondo, bool frenteNegativoZ = false)
    {
        if (pieza == null || material == null || material.mainTexture == null) return;
        MeshFilter filtro = pieza.GetComponent<MeshFilter>();
        MeshRenderer renderizador = pieza.GetComponent<MeshRenderer>();
        if (filtro == null || renderizador == null) return;

        Vector3[,] lados = {
            { new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f) },
            { new Vector3(.5f,-.5f,-.5f), new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(.5f,.5f,-.5f) },
            { new Vector3(.5f,-.5f,.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(.5f,.5f,.5f) },
            { new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,-.5f,.5f), new Vector3(-.5f,.5f,.5f), new Vector3(-.5f,.5f,-.5f) },
            { new Vector3(-.5f,.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f) },
            { new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,-.5f,.5f), new Vector3(-.5f,-.5f,.5f) }
        };
        int frente = u + fondo;
        int atras = u + fondo + ancho + fondo;
        int[,] regiones = {
            { frenteNegativoZ ? atras : frente, v + fondo, ancho, alto },
            { frenteNegativoZ ? frente : atras, v + fondo, ancho, alto },
            { u, v + fondo, fondo, alto },
            { u + fondo + ancho, v + fondo, fondo, alto },
            { u + fondo, v, ancho, fondo },
            { u + fondo + ancho, v, ancho, fondo }
        };
        Vector3[] vertices = new Vector3[24];
        Vector2[] uv = new Vector2[24];
        int[] triangulos = new int[36];
        float texturaAncho = material.mainTexture.width;
        float texturaAlto = material.mainTexture.height;
        for (int lado = 0; lado < 6; lado++)
        {
            int inicio = lado * 4;
            for (int i = 0; i < 4; i++) vertices[inicio + i] = lados[lado, i];
            float x0 = (regiones[lado, 0] + 0.02f) / texturaAncho;
            float x1 = (regiones[lado, 0] + regiones[lado, 2] - 0.02f) / texturaAncho;
            float y0 = 1f - (regiones[lado, 1] + regiones[lado, 3] - 0.02f) / texturaAlto;
            float y1 = 1f - (regiones[lado, 1] + 0.02f) / texturaAlto;
            uv[inicio] = new Vector2(x0, y0);
            uv[inicio + 1] = new Vector2(x1, y0);
            uv[inicio + 2] = new Vector2(x1, y1);
            uv[inicio + 3] = new Vector2(x0, y1);
            int indice = lado * 6;
            triangulos[indice] = inicio;
            triangulos[indice + 1] = inicio + 1;
            triangulos[indice + 2] = inicio + 2;
            triangulos[indice + 3] = inicio;
            triangulos[indice + 4] = inicio + 2;
            triangulos[indice + 5] = inicio + 3;
        }
        Mesh malla = new Mesh { name = "Cubo con piel " + pieza.name };
        malla.vertices = vertices;
        malla.uv = uv;
        malla.triangles = triangulos;
        malla.RecalculateNormals();
        malla.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        filtro.sharedMesh = malla;
        renderizador.sharedMaterial = material;
    }

    public static void Ocultar(Transform pieza)
    {
        if (pieza == null) return;
        Renderer renderizador = pieza.GetComponent<Renderer>();
        if (renderizador != null) renderizador.enabled = false;
    }
}
