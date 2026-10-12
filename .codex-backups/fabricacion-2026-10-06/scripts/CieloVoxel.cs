using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CieloVoxel : MonoBehaviour
{
    private MundoVoxel mundo;
    private Camera camara;
    private Transform solVisual, lunaVisual, estrellas;
    private Material materialSol, materialLuna, materialEstrellas;
    private Mesh mallaEstrellas;
    private readonly List<Transform> nubes = new List<Transform>();
    private readonly List<Vector3> origenNubes = new List<Vector3>();

    void Start()
    {
        mundo = GetComponent<MundoVoxel>();
        camara = Camera.main;
        materialSol = CrearMaterial("sun", new Color(1f, .94f, .65f));
        materialLuna = CrearMaterial("full_moon", new Color(.75f, .81f, .87f));
        solVisual = CrearCuadro("Sol cuadrado", materialSol, 22f);
        lunaVisual = CrearCuadro("Luna cuadrada", materialLuna, 17f);
        CrearEstrellas();
        BuscarNubes();
    }

    public void BuscarNubes()
    {
        nubes.Clear(); origenNubes.Clear();
        foreach (Transform objeto in GetComponentsInChildren<Transform>())
            if (objeto.name == "Nube") { nubes.Add(objeto); origenNubes.Add(objeto.localPosition); }
    }

    Material CrearMaterial(string nombre, Color color)
    {
        Texture2D textura = Resources.Load<Texture2D>("Minecraft/Environment/" + nombre);
        Material material = new Material(Shader.Find(textura != null ? "Unlit/Transparent" : "Unlit/Color"));
        if (textura != null) { textura.filterMode = FilterMode.Point; material.mainTexture = textura; }
        material.color = textura != null ? Color.white : color;
        material.renderQueue = 1000;
        return material;
    }

    Transform CrearCuadro(string nombre, Material material, float escala)
    {
        GameObject cuadro = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cuadro.name = nombre;
        cuadro.transform.SetParent(transform, false);
        cuadro.transform.localScale = Vector3.one * escala;
        cuadro.GetComponent<Collider>().enabled = false;
        Destroy(cuadro.GetComponent<Collider>());
        Renderer aspecto = cuadro.GetComponent<Renderer>();
        aspecto.sharedMaterial = material;
        aspecto.shadowCastingMode = ShadowCastingMode.Off;
        aspecto.receiveShadows = false;
        return cuadro.transform;
    }

    void CrearEstrellas()
    {
        GameObject objeto = new GameObject("Estrellas cuadradas");
        objeto.transform.SetParent(transform, false);
        estrellas = objeto.transform;
        List<Vector3> vertices = new List<Vector3>();
        List<int> caras = new List<int>();
        System.Random azar = new System.Random(mundo.semilla + 73);
        for (int i = 0; i < 180; i++)
        {
            Vector3 direccion = new Vector3((float)azar.NextDouble() * 2f - 1f,
                .15f + (float)azar.NextDouble(), (float)azar.NextDouble() * 2f - 1f).normalized;
            Vector3 centro = direccion * 205f;
            Vector3 derecha = Vector3.Cross(direccion, Vector3.up).normalized * .25f;
            Vector3 arriba = Vector3.Cross(direccion, derecha).normalized * .25f;
            int inicio = vertices.Count;
            vertices.Add(centro - derecha - arriba); vertices.Add(centro + derecha - arriba);
            vertices.Add(centro + derecha + arriba); vertices.Add(centro - derecha + arriba);
            caras.AddRange(new[] { inicio, inicio + 1, inicio + 2, inicio, inicio + 2, inicio + 3,
                inicio + 2, inicio + 1, inicio, inicio + 3, inicio + 2, inicio });
        }
        mallaEstrellas = new Mesh { name = "Cielo estrellado" };
        mallaEstrellas.SetVertices(vertices); mallaEstrellas.SetTriangles(caras, 0);
        mallaEstrellas.RecalculateNormals();
        objeto.AddComponent<MeshFilter>().sharedMesh = mallaEstrellas;
        materialEstrellas = new Material(Shader.Find("Unlit/Color")) { color = new Color(.7f, .75f, .9f), renderQueue = 1000 };
        objeto.AddComponent<MeshRenderer>().sharedMaterial = materialEstrellas;
    }

    void LateUpdate()
    {
        if (mundo == null || camara == null || solVisual == null) return;
        float fase = Mathf.Repeat(Time.time / mundo.duracionDiaSegundos + .25f, 1f);
        float angulo = fase * Mathf.PI * 2f;
        Vector3 direccion = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), .15f).normalized;
        solVisual.position = camara.transform.position + direccion * 200f;
        lunaVisual.position = camara.transform.position - direccion * 200f;
        solVisual.LookAt(camara.transform); lunaVisual.LookAt(camara.transform);
        solVisual.Rotate(0f, 180f, 0f); lunaVisual.Rotate(0f, 180f, 0f);
        estrellas.position = camara.transform.position;
        estrellas.gameObject.SetActive(Mathf.Sin(angulo) < -.08f);
        float extension = mundo.ancho * mundo.TamanoBloque;
        for (int i = 0; i < nubes.Count; i++)
        {
            if (nubes[i] == null) continue;
            Vector3 posicion = origenNubes[i];
            posicion.x = Mathf.Repeat(posicion.x + Time.time * .3f + extension * .5f, extension) - extension * .5f;
            nubes[i].localPosition = posicion;
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = camara.backgroundColor;
        RenderSettings.fogStartDistance = 130f;
        RenderSettings.fogEndDistance = 250f;
    }

    void OnDestroy()
    {
        if (solVisual != null) Destroy(solVisual.gameObject);
        if (lunaVisual != null) Destroy(lunaVisual.gameObject);
        if (estrellas != null) Destroy(estrellas.gameObject);
        if (materialSol != null) Destroy(materialSol);
        if (materialLuna != null) Destroy(materialLuna);
        if (materialEstrellas != null) Destroy(materialEstrellas);
        if (mallaEstrellas != null) Destroy(mallaEstrellas);
    }
}
