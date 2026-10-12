using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum TipoBloque { Tierra, Piedra, Madera, Arena, Adoquin, Carbon, Hierro, Diamante, Tronco, MesaTrabajo, Horno, Cofre, Vidrio }

// Construye el paisaje al abrir la escena y antes de que aparezcan los personajes.
[ExecuteAlways]
[DefaultExecutionOrder(-200)]
public partial class MundoVoxel : MonoBehaviour
{
    [Range(32, 320)] public int ancho = 192;
    [Range(32, 320)] public int largo = 192;
    [Min(1f)] public float tamanoBloque = 2f;
    [Range(0, 640)] public int cantidadArboles = 400;
    [Min(0.5f)] public float distanciaRecogida = 3f;
    [Min(1f)] public float alturaArbolesEnRodolfos = 3f;
    [Min(60f)] public float duracionDiaSegundos = 600f;
    public int semilla = 2847;

    private const string NombreGenerado = "Terreno de bloques y arboles";
    private const int FondoRoca = -5;
    private const int TerrenoOriginal = 96;
    [SerializeField,HideInInspector] private int versionTamano;
    private int DesplazamientoX => (ancho - TerrenoOriginal) / 2;
    private int DesplazamientoZ => (largo - TerrenoOriginal) / 2;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector2> coordenadasTextura = new List<Vector2>();
    private readonly List<int>[] triangulos = {
        new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(),
        new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>()
    };
    private int[,] alturas;
    private bool[,] celdasAgua;
    private bool[,] celdasArena;
    private Material[] materiales;
    private Material materialAgua;
    private float escalaFotogramaAgua;
    private Light sol;
    private Quaternion rotacionInicialSol;
    private readonly HashSet<Vector3Int> bloquesQuitados = new HashSet<Vector3Int>();
    private readonly Dictionary<Vector3Int, TipoBloque> bloquesColocados = new Dictionary<Vector3Int, TipoBloque>();
    private readonly Dictionary<Collider, GameObject> troncos = new Dictionary<Collider, GameObject>();
    private readonly HashSet<Vector2Int> plantasQuitadas = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> celdasConPlantas = new HashSet<Vector2Int>();
    private Transform raizGenerada;
    private Transform vegetacionGenerada;
    private MeshFilter filtroSuelo;
    private MeshCollider colisionSuelo;
    private Mesh mallaSuelo;
    private script jugador;
    private GameObject contornoBloque;
    private Material materialContorno;
    private GameObject grietasBloque;
    private Material materialGrietas;
    private readonly Texture2D[] etapasGrietas = new Texture2D[10];
    public float TamanoBloque => tamanoBloque;
    public Material MaterialPiedra => materiales != null ? materiales[3] : null;
    public Material MaterialTierra => materiales != null ? materiales[1] : null;
    public Material MaterialPastoSuperior => materiales != null ? materiales[0] : null;
    public Material MaterialTierraPura => materiales != null ? materiales[2] : null;
    public Material MaterialMadera => materiales != null ? materiales[4] : null;
    public Material MaterialArena => materiales != null ? materiales[5] : null;
    public Material MaterialAdoquin => materiales != null ? materiales[6] : null;

    public Material MaterialDe(TipoBloque tipo)
    {
        if ((int)tipo >= 8) return MaterialesEspeciales(tipo,false)[0];
        if (materiales == null) return null;
        switch (tipo)
        {
            case TipoBloque.Piedra: return materiales[3];
            case TipoBloque.Madera: return materiales[4];
            case TipoBloque.Arena: return materiales[5];
            case TipoBloque.Adoquin: return materiales[6];
            case TipoBloque.Carbon: return materiales[7];
            case TipoBloque.Hierro: return materiales[8];
            case TipoBloque.Diamante: return materiales[9];
            default: return materiales[1];
        }
    }

    public bool EstaEnAgua(Vector3 posicionMundo)
    {
        if (celdasAgua == null) return false;
        Vector3Int celda = CeldaEn(transform.InverseTransformPoint(posicionMundo));
        return celda.x >= 0 && celda.x < ancho && celda.z >= 0 && celda.z < largo &&
            celdasAgua[celda.x, celda.z] && posicionMundo.y < transform.TransformPoint(
                new Vector3(0f, tamanoBloque * 0.4f, 0f)).y;
    }

    void OnEnable()
    {
        // Adapta tambien la escena que ya estaba abierta en el editor.
        if(versionTamano==0)
        {
            bool ampliar=ancho==TerrenoOriginal && largo==TerrenoOriginal;
            if(ampliar) { ancho=largo=TerrenoOriginal*2; cantidadArboles=Mathf.Max(cantidadArboles,400); }
            versionTamano=1;
#if UNITY_EDITOR
            if(ampliar && !Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        }
        MeshRenderer original = GetComponent<MeshRenderer>();
        MeshCollider colisionOriginal = GetComponent<MeshCollider>();
        if (original != null) original.enabled = false;
        if (colisionOriginal != null) colisionOriginal.enabled = false;
        Generar();
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        if (GetComponent<CieloVoxel>() == null) gameObject.AddComponent<CieloVoxel>();
        foreach (Light luz in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (luz.type == LightType.Directional)
            {
                sol = luz;
                rotacionInicialSol = luz.transform.rotation;
                break;
            }
    }

    void Generar()
    {
        Transform anterior = transform.Find(NombreGenerado);
        if (anterior != null)
        {
            anterior.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(anterior.gameObject);
            else DestroyImmediate(anterior.gameObject);
        }

        GameObject raiz = new GameObject(NombreGenerado);
        raiz.transform.SetParent(transform, false);
        raiz.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        raizGenerada = raiz.transform;
        vegetacionGenerada = null;
        plantasQuitadas.Clear();
        celdasConPlantas.Clear();
        Material verde = CrearMaterialBloque("grass_block_top", new Color(0.55f, 0.83f, 0.39f));
        Material verdeLateral = CrearMaterialBloque("grass_block_side", Color.white);
        Material tierra = CrearMaterialBloque("dirt", Color.white);
        Material piedra = CrearMaterialBloque("stone", Color.white);
        Material madera = CrearMaterialBloque("oak_planks", Color.white);
        Material arena = CrearMaterialBloque("sand", Color.white);
        Material adoquin = CrearMaterialBloque("cobblestone", Color.white);
        Material carbon = CrearMaterialBloque("coal_ore", Color.white);
        Material hierro = CrearMaterialBloque("iron_ore", Color.white);
        Material diamante = CrearMaterialBloque("diamond_ore", Color.white);
        Material tronco = CrearMaterialBloque("oak_log", Color.white);
        Material hojas = CrearMaterialBloque("oak_leaves", new Color(0.48f, 0.75f, 0.36f), true);
        Material nubes = CrearMaterial(new Color(0.96f, 0.98f, 1f));
        materiales = new[] { verde, verdeLateral, tierra, piedra, madera, arena, adoquin,
            carbon, hierro, diamante };
        bloquesQuitados.Clear();
        bloquesColocados.Clear();
        troncos.Clear();
        funcionales.Clear();
        troncosQuitados.Clear();

        alturas = new int[ancho, largo];
        float desplazamiento = semilla * 0.013f;
        for (int x = 0; x < ancho; x++)
        for (int z = 0; z < largo; z++)
        {
            int muestraX=x-DesplazamientoX, muestraZ=z-DesplazamientoZ;
            float colinas = Mathf.PerlinNoise((muestraX + desplazamiento) * 0.044f, (muestraZ + desplazamiento) * 0.044f);
            float detalle = Mathf.PerlinNoise((muestraX + desplazamiento) * 0.13f, (muestraZ - desplazamiento) * 0.13f);
            alturas[x, z] = Mathf.Clamp(Mathf.FloorToInt(colinas * 3.5f + detalle * 0.55f), 0, 3);
        }

        celdasAgua = new bool[ancho, largo];
        for (int x = 0; x < ancho; x++)
        for (int z = 0; z < largo; z++)
        {
            float lagoA = Mathf.Pow((x - DesplazamientoX - TerrenoOriginal * 0.76f) / 6f, 2f) +
                Mathf.Pow((z - DesplazamientoZ - TerrenoOriginal * 0.73f) / 5f, 2f);
            float lagoB = Mathf.Pow((x - DesplazamientoX - TerrenoOriginal * 0.20f) / 5f, 2f) +
                Mathf.Pow((z - DesplazamientoZ - TerrenoOriginal * 0.62f) / 4f, 2f);
            float lagoC = Mathf.Pow((x - DesplazamientoX - TerrenoOriginal * 0.72f) / 4f, 2f) +
                Mathf.Pow((z - DesplazamientoZ - TerrenoOriginal * 0.20f) / 5f, 2f);
            if (Mathf.Min(lagoA, lagoB, lagoC) >= 1f) continue;
            celdasAgua[x, z] = true;
            alturas[x, z] = -1;
        }
        celdasArena = new bool[ancho, largo];
        for (int x = 1; x < ancho - 1; x++)
        for (int z = 1; z < largo - 1; z++)
        {
            if (celdasAgua[x, z]) continue;
            if (!celdasAgua[x - 1, z] && !celdasAgua[x + 1, z] &&
                !celdasAgua[x, z - 1] && !celdasAgua[x, z + 1]) continue;
            celdasArena[x, z] = true;
            alturas[x, z] = 0;
        }
        HashSet<Vector2Int> celdasRoca = new HashSet<Vector2Int>();
        System.Random azarRoca = new System.Random(semilla + 403);
        for (int i = 0; i < 80; i++)
        {
            int x = ancho>=TerrenoOriginal ? azarRoca.Next(3,TerrenoOriginal-3)+DesplazamientoX : azarRoca.Next(3,ancho-3);
            int z = largo>=TerrenoOriginal ? azarRoca.Next(3,TerrenoOriginal-3)+DesplazamientoZ : azarRoca.Next(3,largo-3);
            if (celdasAgua[x, z] || celdasArena[x, z]) continue;
            float mundoX = (x - ancho * 0.5f + 0.5f) * tamanoBloque;
            float mundoZ = (z - largo * 0.5f + 0.5f) * tamanoBloque;
            if (new Vector2(mundoX + 3.37f, mundoZ + 4.8f).sqrMagnitude < 144f) continue;
            bloquesColocados[new Vector3Int(x, alturas[x, z] + 1, z)] = TipoBloque.Adoquin;
            celdasRoca.Add(new Vector2Int(x, z));
        }

        GameObject suelo = new GameObject("Suelo de pasto, tierra y piedra");
        suelo.transform.SetParent(raiz.transform, false);
        filtroSuelo = suelo.AddComponent<MeshFilter>();
        suelo.AddComponent<MeshRenderer>().sharedMaterials = materiales;
        colisionSuelo = suelo.AddComponent<MeshCollider>();
        ReconstruirSuelo();
        CrearAgua(raiz.transform);

        jugador = FindFirstObjectByType<script>();
        float alturaRodolfo = tamanoBloque * 2f;
        // La copa termina a alto + 3.05; el arbol completo mide tres Rodolfos.
        float altoTronco = Mathf.Max(4f, alturaRodolfo * alturaArbolesEnRodolfos - 3.05f);
        System.Random azar = new System.Random(semilla);
        HashSet<Vector2Int> celdasArbol = new HashSet<Vector2Int>();
        System.Random azarExterior = new System.Random(semilla + 192);
        for (int i = 0; i < cantidadArboles; i++)
        {
            bool nucleo=i<100 && ancho>=TerrenoOriginal && largo>=TerrenoOriginal;
            int x=nucleo ? azar.Next(4,TerrenoOriginal-4)+DesplazamientoX : azarExterior.Next(4,ancho-4);
            int z=nucleo ? azar.Next(4,TerrenoOriginal-4)+DesplazamientoZ : azarExterior.Next(4,largo-4);
            if(!nucleo && ancho>TerrenoOriginal && largo>TerrenoOriginal &&
                x>=DesplazamientoX && x<DesplazamientoX+TerrenoOriginal && z>=DesplazamientoZ && z<DesplazamientoZ+TerrenoOriginal) continue;
            if(celdasArbol.Contains(new Vector2Int(x,z))) continue;
            if (celdasAgua[x, z] || celdasArena[x, z] ||
                celdasRoca.Contains(new Vector2Int(x, z))) continue;
            float mundoX = (x - ancho * 0.5f + 0.5f) * tamanoBloque;
            float mundoZ = (z - largo * 0.5f + 0.5f) * tamanoBloque;
            if (Vector2.Distance(new Vector2(mundoX, mundoZ), new Vector2(-3.37f, -4.8f)) < 9f ||
                Vector2.Distance(new Vector2(mundoX, mundoZ), new Vector2(10.07f, -0.04f)) < 5f ||
                (mundoX > -12f && mundoX < -3f && mundoZ > -20f && mundoZ < -9f) ||
                Vector2.Distance(new Vector2(mundoX, mundoZ), new Vector2(-10f, 4f)) < 4f ||
                Vector2.Distance(new Vector2(mundoX, mundoZ), new Vector2(9f, 9f)) < 4f ||
                Vector2.Distance(new Vector2(mundoX, mundoZ), new Vector2(-17f, -9f)) < 4f) continue;
            CrearArbol(raiz.transform, mundoX, mundoZ, (0.5f + alturas[x, z]) * tamanoBloque,
                altoTronco, tamanoBloque, tronco, hojas);
            celdasArbol.Add(new Vector2Int(x, z));
        }
        CrearVegetacion(raiz.transform);
        CrearAnimales(raiz.transform, celdasArbol);
        CrearEsqueletos(raiz.transform, celdasArbol);

        for (int i = 0; i < 22; i++)
        {
            float x = -ancho * tamanoBloque * 0.44f + (i * 31) % (ancho * 2 - 16);
            float z = -largo * tamanoBloque * 0.42f + (i * 43) % (largo * 2 - 16);
            float y = 36f + (i % 3) * 4f;
            CrearCubo(raiz.transform, "Nube", new Vector3(x, y, z), new Vector3(9f, 0.65f, 3f), nubes, false);
            CrearCubo(raiz.transform, "Nube", new Vector3(x + 3f, y, z + 2f), new Vector3(6f, 0.65f, 3f), nubes, false);
        }

        Camera camara = Camera.main;
        if (camara != null)
        {
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.48f, 0.72f, 0.97f);
            camara.farClipPlane = 300f;
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.70f, 0.76f, 0.82f);
    }

    void Update()
    {
        if (Application.isPlaying) ActualizarCielo();
        if (Application.isPlaying && materialAgua != null && escalaFotogramaAgua > 0f)
            materialAgua.mainTextureOffset = new Vector2(0f,
                Mathf.Floor(Time.time * 6f) * escalaFotogramaAgua);
    }

    void ActualizarCielo()
    {
        float fase = Mathf.Repeat(Time.time / Mathf.Max(60f, duracionDiaSegundos) + 0.25f, 1f);
        float claridad = Mathf.Clamp01(Mathf.Sin(fase * Mathf.PI * 2f) * 1.2f + 0.18f);
        if (sol != null)
        {
            sol.transform.rotation = rotacionInicialSol * Quaternion.Euler(
                (fase - 0.25f) * 360f, 0f, 0f);
            sol.intensity = Mathf.Lerp(0.12f, 1f, claridad);
        }
        Camera camara = Camera.main;
        if (camara != null)
            camara.backgroundColor = Color.Lerp(new Color(0.07f, 0.10f, 0.22f),
                new Color(0.48f, 0.72f, 0.97f), claridad);
        RenderSettings.ambientLight = Color.Lerp(new Color(0.24f, 0.27f, 0.34f),
            new Color(0.70f, 0.76f, 0.82f), claridad);
        if (materialAgua != null)
            materialAgua.color = Color.Lerp(new Color(0.07f, 0.16f, 0.32f, 0.8f),
                new Color(0.22f, 0.58f, 0.91f, 0.78f), claridad);
    }

    void CrearAgua(Transform padre)
    {
        Texture2D textura = Resources.Load<Texture2D>("Minecraft/Blocks/water_still");
        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        materialAgua = new Material(shader)
        {
            color = new Color(0.22f, 0.58f, 0.91f, 0.78f),
            mainTexture = textura,
            hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
        };
        if (textura != null)
        {
            textura.filterMode = FilterMode.Point;
            textura.wrapMode = TextureWrapMode.Repeat;
            escalaFotogramaAgua = Mathf.Clamp01((float)textura.width / textura.height);
            materialAgua.mainTextureScale = new Vector2(1f, escalaFotogramaAgua);
        }

        List<Vector3> puntos = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> caras = new List<int>();
        float y = tamanoBloque * 0.4f;
        for (int x = 0; x < ancho; x++)
        for (int z = 0; z < largo; z++)
        {
            if (!celdasAgua[x, z]) continue;
            float x0 = (x - ancho * 0.5f) * tamanoBloque;
            float z0 = (z - largo * 0.5f) * tamanoBloque;
            int inicio = puntos.Count;
            puntos.Add(new Vector3(x0, y, z0));
            puntos.Add(new Vector3(x0, y, z0 + tamanoBloque));
            puntos.Add(new Vector3(x0 + tamanoBloque, y, z0 + tamanoBloque));
            puntos.Add(new Vector3(x0 + tamanoBloque, y, z0));
            uv.Add(Vector2.zero); uv.Add(Vector2.up); uv.Add(Vector2.one); uv.Add(Vector2.right);
            caras.Add(inicio); caras.Add(inicio + 1); caras.Add(inicio + 2);
            caras.Add(inicio); caras.Add(inicio + 2); caras.Add(inicio + 3);
        }
        Mesh malla = new Mesh { name = "Superficie de lagos" };
        malla.SetVertices(puntos);
        malla.SetUVs(0, uv);
        malla.SetTriangles(caras, 0);
        malla.RecalculateNormals();
        malla.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        GameObject agua = new GameObject("Lagos");
        agua.transform.SetParent(padre, false);
        agua.AddComponent<MeshFilter>().sharedMesh = malla;
        agua.AddComponent<MeshRenderer>().sharedMaterial = materialAgua;
    }

    void CrearVegetacion(Transform padre)
    {
        if (vegetacionGenerada != null)
        {
            foreach (MeshFilter filtro in vegetacionGenerada.GetComponentsInChildren<MeshFilter>())
            {
                if (filtro.sharedMesh == null) continue;
                if (Application.isPlaying) Destroy(filtro.sharedMesh);
                else DestroyImmediate(filtro.sharedMesh);
            }
            foreach (MeshRenderer renderizador in vegetacionGenerada.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderizador.sharedMaterial == null) continue;
                if (Application.isPlaying) Destroy(renderizador.sharedMaterial);
                else DestroyImmediate(renderizador.sharedMaterial);
            }
            if (Application.isPlaying) Destroy(vegetacionGenerada.gameObject);
            else DestroyImmediate(vegetacionGenerada.gameObject);
        }
        GameObject raizVegetacion = new GameObject("Pasto y flores");
        raizVegetacion.transform.SetParent(padre, false);
        vegetacionGenerada = raizVegetacion.transform;
        celdasConPlantas.Clear();
        string[] especies = { "short_grass", "fern", "poppy", "dandelion" };
        float escalaTerreno = ancho * largo / (64f * 64f);
        int[] cantidades = { Mathf.RoundToInt(340 * escalaTerreno),
            Mathf.RoundToInt(95 * escalaTerreno), Mathf.RoundToInt(90 * escalaTerreno),
            Mathf.RoundToInt(80 * escalaTerreno) };
        System.Random azar = new System.Random(semilla + 812);
        for (int especie = 0; especie < especies.Length; especie++)
        {
            List<Vector3> puntos = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> caras = new List<int>();
            for (int i = 0; i < cantidades[especie]; i++)
            {
                int x = azar.Next(1, ancho - 1);
                int z = azar.Next(1, largo - 1);
                Vector2Int celdaPlanta = new Vector2Int(x, z);
                if (celdasAgua[x, z] || celdasArena[x, z] ||
                    plantasQuitadas.Contains(celdaPlanta) ||
                    !ExisteBloque(new Vector3Int(x,alturas[x,z],z)) ||
                    bloquesColocados.ContainsKey(new Vector3Int(x, alturas[x, z] + 1, z))) continue;
                celdasConPlantas.Add(celdaPlanta);
                float centroX = (x - ancho * 0.5f + 0.5f) * tamanoBloque;
                float centroZ = (z - largo * 0.5f + 0.5f) * tamanoBloque;
                float suelo = (alturas[x, z] + 0.5f) * tamanoBloque;
                float anchoPlanta = tamanoBloque * (especie < 2 ? 0.55f : 0.48f);
                float altoPlanta = tamanoBloque * (especie < 2 ? 0.68f : 0.75f);
                AgregarPlanta(puntos, uv, caras, new Vector3(centroX, suelo, centroZ),
                    anchoPlanta, altoPlanta);
            }
            Mesh malla = new Mesh { name = "Vegetacion " + especies[especie] };
            malla.SetVertices(puntos);
            malla.SetUVs(0, uv);
            malla.SetTriangles(caras, 0);
            malla.RecalculateNormals();
            malla.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            GameObject grupo = new GameObject("Vegetacion " + especies[especie]);
            grupo.transform.SetParent(vegetacionGenerada, false);
            grupo.AddComponent<MeshFilter>().sharedMesh = malla;
            Material material = CrearMaterialPlanta(especies[especie],
                especie < 2 ? new Color(0.55f, 0.82f, 0.39f) : Color.white);
            MeshRenderer renderizador = grupo.AddComponent<MeshRenderer>();
            renderizador.sharedMaterial = material;
            renderizador.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static void AgregarPlanta(List<Vector3> puntos, List<Vector2> uv, List<int> caras,
        Vector3 centro, float anchoPlanta, float altoPlanta)
    {
        for (int eje = 0; eje < 2; eje++)
        {
            Vector3 desplazamiento = eje == 0 ? Vector3.right : Vector3.forward;
            desplazamiento *= anchoPlanta * 0.5f;
            int inicio = puntos.Count;
            puntos.Add(centro - desplazamiento);
            puntos.Add(centro + desplazamiento);
            puntos.Add(centro + desplazamiento + Vector3.up * altoPlanta);
            puntos.Add(centro - desplazamiento + Vector3.up * altoPlanta);
            uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
            caras.Add(inicio); caras.Add(inicio + 1); caras.Add(inicio + 2);
            caras.Add(inicio); caras.Add(inicio + 2); caras.Add(inicio + 3);
            // La cara opuesta necesita sus propios vertices para conservar una normal iluminada.
            puntos.Add(centro - desplazamiento);
            puntos.Add(centro + desplazamiento);
            puntos.Add(centro + desplazamiento + Vector3.up * altoPlanta);
            puntos.Add(centro - desplazamiento + Vector3.up * altoPlanta);
            uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
            caras.Add(inicio + 4); caras.Add(inicio + 6); caras.Add(inicio + 5);
            caras.Add(inicio + 4); caras.Add(inicio + 7); caras.Add(inicio + 6);
        }
    }

    void CrearAnimales(Transform padre, HashSet<Vector2Int> celdasArbol)
    {
        System.Random azar = new System.Random(semilla + 1942);
        for (int i = 0; i < 24; i++)
        {
            for (int intento = 0; intento < 35; intento++)
            {
                int x = azar.Next(3, ancho - 3);
                int z = azar.Next(3, largo - 3);
                if (celdasAgua[x, z] || celdasArbol.Contains(new Vector2Int(x, z)) ||
                    bloquesColocados.ContainsKey(new Vector3Int(x, alturas[x, z] + 1, z))) continue;
                float mundoX = (x - ancho * 0.5f + 0.5f) * tamanoBloque;
                float mundoZ = (z - largo * 0.5f + 0.5f) * tamanoBloque;
                if (new Vector2(mundoX + 3.37f, mundoZ + 4.8f).sqrMagnitude < 100f) continue;
                GameObject animal = new GameObject(i % 2 == 0 ? "Oveja" : "Cerdo");
                animal.transform.SetParent(padre, false);
                animal.transform.localPosition = new Vector3(mundoX,
                    (alturas[x, z] + 0.5f) * tamanoBloque + 1.02f, mundoZ);
                animal.AddComponent<BoxCollider>();
                animal.AddComponent<Rigidbody>();
                AnimalVoxel comportamiento = animal.AddComponent<AnimalVoxel>();
                comportamiento.esOveja = i % 2 == 0;
                break;
            }
        }
    }

    void CrearEsqueletos(Transform padre, HashSet<Vector2Int> celdasArbol)
    {
        System.Random azar = new System.Random(semilla + 2751);
        for (int i = 0; i < 6; i++)
        {
            for (int intento = 0; intento < 35; intento++)
            {
                int x = azar.Next(3, ancho - 3);
                int z = azar.Next(3, largo - 3);
                if (celdasAgua[x, z] || celdasArena[x, z] ||
                    celdasArbol.Contains(new Vector2Int(x, z)) ||
                    bloquesColocados.ContainsKey(new Vector3Int(x, alturas[x, z] + 1, z))) continue;
                float mundoX = (x - ancho * 0.5f + 0.5f) * tamanoBloque;
                float mundoZ = (z - largo * 0.5f + 0.5f) * tamanoBloque;
                if (new Vector2(mundoX + 3.37f, mundoZ + 4.8f).sqrMagnitude < 324f) continue;
                GameObject esqueleto = new GameObject("Esqueleto " + (i + 1));
                esqueleto.transform.SetParent(padre, false);
                esqueleto.transform.localPosition = new Vector3(mundoX,
                    (alturas[x, z] + 0.5f) * tamanoBloque + 2.3f, mundoZ);
                esqueleto.AddComponent<BoxCollider>();
                esqueleto.AddComponent<Rigidbody>();
                enemigo comportamiento = esqueleto.AddComponent<enemigo>();
                comportamiento.distanciaDeteccion = 26f;
                comportamiento.distanciaParada = 6f;
                comportamiento.factorTamano = 0.95f;
                comportamiento.factorVelocidad = 0.75f;
                comportamiento.vida = 4;
                break;
            }
        }
    }

    public bool QuitarBloque(RaycastHit impacto)
    {
        if (!Application.isPlaying) return false;
        if (QuitarEspecial(impacto)) return true;
        GameObject tronco;
        if (troncos.TryGetValue(impacto.collider, out tronco))
        {
            troncosQuitados.Add(transform.InverseTransformPoint(tronco.transform.position));
            troncos.Remove(impacto.collider);
            SonidosVoxel.Emitir("romper", impacto.point);
            EfectosBloqueVoxel.Emitir(impacto.point, MaterialMadera);
            SoltarExtraccion(impacto.point + impacto.normal * 0.42f, TipoBloque.Tronco);
            Destroy(tronco);
            return true;
        }
        if (impacto.collider != colisionSuelo) return false;
        Vector3 local = transform.InverseTransformPoint(impacto.point - impacto.normal * 0.02f);
        Vector3Int celda = CeldaEn(local);
        if (celda.y <= FondoRoca || !ExisteBloque(celda)) return false;

        TipoBloque tipo = TipoEn(celda);
        SonidosVoxel.Emitir("romper", impacto.point);
        EfectosBloqueVoxel.Emitir(impacto.point, MaterialDe(tipo));
        if (!bloquesColocados.Remove(celda)) bloquesQuitados.Add(celda);
        Vector2Int posicionPlanta = new Vector2Int(celda.x, celda.z);
        bool quitarPlantas = celda.y == alturas[celda.x, celda.z] &&
            celdasConPlantas.Contains(posicionPlanta);
        if (quitarPlantas) plantasQuitadas.Add(posicionPlanta);
        SoltarExtraccion(impacto.point + impacto.normal * 0.42f, tipo);
        ReconstruirSuelo();
        if (quitarPlantas && raizGenerada != null) CrearVegetacion(raizGenerada);
        return true;
    }

    public bool EsBloquePicable(RaycastHit impacto)
    {
        if (impacto.collider == null) return false;
        if (FuncionalApuntado(impacto) != null) return true;
        if (troncos.ContainsKey(impacto.collider)) return true;
        if (impacto.collider != colisionSuelo) return false;
        Vector3 local = transform.InverseTransformPoint(impacto.point - impacto.normal * 0.02f);
        Vector3Int celda = CeldaEn(local);
        return celda.y > FondoRoca && ExisteBloque(celda);
    }

    public bool EsMismoBloque(RaycastHit anterior, RaycastHit actual)
    {
        if (anterior.collider == null || actual.collider == null || anterior.collider != actual.collider)
            return false;
        if (actual.collider != colisionSuelo) return true;
        Vector3Int a = CeldaEn(transform.InverseTransformPoint(anterior.point - anterior.normal * 0.02f));
        Vector3Int b = CeldaEn(transform.InverseTransformPoint(actual.point - actual.normal * 0.02f));
        return a == b;
    }

    public void MostrarGrietas(RaycastHit impacto, float progreso)
    {
        if (!EsBloquePicable(impacto)) { OcultarGrietas(); return; }
        if (grietasBloque == null)
        {
            Shader shader = Shader.Find("Unlit/Transparent Cutout");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            materialGrietas = new Material(shader);
            materialGrietas.color = Color.white;
            materialGrietas.renderQueue = 3000;
            materialGrietas.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            grietasBloque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grietasBloque.name = "Grietas del bloque";
            grietasBloque.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            Collider colisionGrietas = grietasBloque.GetComponent<Collider>();
            colisionGrietas.enabled = false;
            Destroy(colisionGrietas);
            grietasBloque.GetComponent<Renderer>().sharedMaterial = materialGrietas;
        }
        if (impacto.collider == colisionSuelo)
        {
            Vector3 local = transform.InverseTransformPoint(impacto.point - impacto.normal * 0.02f);
            Vector3Int celda = CeldaEn(local);
            grietasBloque.transform.position = transform.TransformPoint(new Vector3(
                (celda.x - ancho * 0.5f + 0.5f) * tamanoBloque,
                celda.y * tamanoBloque,
                (celda.z - largo * 0.5f + 0.5f) * tamanoBloque));
            grietasBloque.transform.localScale = Vector3.one * tamanoBloque * 1.012f;
        }
        else
        {
            grietasBloque.transform.position = impacto.collider.bounds.center;
            grietasBloque.transform.localScale = impacto.collider.bounds.size * 1.012f;
        }
        int etapa = Mathf.Clamp(Mathf.FloorToInt(progreso * 10f), 0, 9);
        if (etapasGrietas[etapa] == null)
            etapasGrietas[etapa] = Resources.Load<Texture2D>("Minecraft/Breaking/destroy_stage_" + etapa);
        materialGrietas.mainTexture = etapasGrietas[etapa];
        if (etapasGrietas[etapa] != null) etapasGrietas[etapa].filterMode = FilterMode.Point;
        grietasBloque.SetActive(true);
    }

    public void OcultarGrietas()
    {
        if (grietasBloque != null) grietasBloque.SetActive(false);
    }

    void SoltarBloque(Vector3 posicion, TipoBloque tipo)
    {
        ObjetoRecogibleVoxel.SoltarBloque(this, posicion, tipo);
    }

    public float DuracionPicado(RaycastHit impacto, bool pico)
    {
        if (troncos.ContainsKey(impacto.collider)) return pico ? 1.5f : 2.2f;
        if (impacto.collider != colisionSuelo) return 1f;
        TipoBloque tipo = TipoEn(CeldaEn(transform.InverseTransformPoint(impacto.point - impacto.normal * .02f)));
        switch (tipo)
        {
            case TipoBloque.Tierra: return pico ? .5f : .8f;
            case TipoBloque.Arena: return pico ? .4f : .6f;
            case TipoBloque.Madera: return pico ? 1.5f : 2.2f;
            case TipoBloque.Piedra: case TipoBloque.Adoquin: return pico ? .65f : 3.2f;
            default: return pico ? 1f : 4.5f;
        }
    }

    public void ActualizarContorno(bool apuntando, RaycastHit impacto)
    {
        if (!apuntando || !ImpactoValido(impacto))
        {
            if (contornoBloque != null) contornoBloque.SetActive(false);
            return;
        }

        Vector3 local = transform.InverseTransformPoint(impacto.point - impacto.normal * 0.02f);
        Vector3Int celda = CeldaEn(local);
        if (impacto.collider == colisionSuelo && !ExisteBloque(celda))
        {
            if (contornoBloque != null) contornoBloque.SetActive(false);
            return;
        }

        if (contornoBloque == null) CrearContorno();
        contornoBloque.transform.localPosition = new Vector3(
            (celda.x - ancho * 0.5f + 0.5f) * tamanoBloque,
            celda.y * tamanoBloque,
            (celda.z - largo * 0.5f + 0.5f) * tamanoBloque);
        if (impacto.collider != colisionSuelo)
        {
            contornoBloque.transform.position = impacto.collider.bounds.center;
            contornoBloque.transform.localScale = impacto.collider.bounds.size / tamanoBloque;
        }
        else contornoBloque.transform.localScale = Vector3.one;
        contornoBloque.SetActive(true);
    }

    void CrearContorno()
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        materialContorno = new Material(shader) { color = Color.white };
        materialContorno.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        contornoBloque = new GameObject("Contorno blanco del bloque");
        contornoBloque.transform.SetParent(transform, false);
        contornoBloque.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        float borde = tamanoBloque * 0.505f;
        Vector3[] esquinas = {
            new Vector3(-borde, -borde, -borde), new Vector3(borde, -borde, -borde),
            new Vector3(borde, -borde, borde), new Vector3(-borde, -borde, borde),
            new Vector3(-borde, borde, -borde), new Vector3(borde, borde, -borde),
            new Vector3(borde, borde, borde), new Vector3(-borde, borde, borde)
        };
        int[] aristas = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };
        for (int i = 0; i < aristas.Length; i += 2)
        {
            GameObject linea = new GameObject("Arista " + (i / 2 + 1));
            linea.transform.SetParent(contornoBloque.transform, false);
            LineRenderer trazo = linea.AddComponent<LineRenderer>();
            trazo.sharedMaterial = materialContorno;
            trazo.useWorldSpace = false;
            trazo.positionCount = 2;
            trazo.startWidth = tamanoBloque * 0.012f;
            trazo.endWidth = tamanoBloque * 0.012f;
            trazo.startColor = Color.white;
            trazo.endColor = Color.white;
            trazo.shadowCastingMode = ShadowCastingMode.Off;
            trazo.receiveShadows = false;
            trazo.SetPosition(0, esquinas[aristas[i]]);
            trazo.SetPosition(1, esquinas[aristas[i + 1]]);
        }
    }

    void OnDestroy()
    {
        foreach(var material in materialesEspeciales.Values)
            if(material!=null) { if(Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        materialesEspeciales.Clear();
        if (grietasBloque != null)
        {
            if (Application.isPlaying) Destroy(grietasBloque);
            else DestroyImmediate(grietasBloque);
        }
        if (materialGrietas != null)
        {
            if (Application.isPlaying) Destroy(materialGrietas);
            else DestroyImmediate(materialGrietas);
        }
        if (materialContorno != null)
        {
            if (Application.isPlaying) Destroy(materialContorno);
            else DestroyImmediate(materialContorno);
        }
    }

    public bool ColocarBloque(RaycastHit impacto, TipoBloque tipo)
    {
        if (!Application.isPlaying || !ImpactoValido(impacto)) return false;
        Vector3Int celda = CeldaImpacto(impacto) + DireccionCelda(transform.InverseTransformDirection(impacto.normal));
        if (celda.x < 0 || celda.x >= ancho || celda.z < 0 || celda.z >= largo ||
            celda.y <= FondoRoca || celda.y > 32 || ExisteBloque(celda)) return false;

        Vector3 centro = transform.TransformPoint(new Vector3(
            (celda.x - ancho * 0.5f + 0.5f) * tamanoBloque,
            celda.y * tamanoBloque,
            (celda.z - largo * 0.5f + 0.5f) * tamanoBloque));
        foreach (Collider obstaculo in Physics.OverlapBox(centro, Vector3.one * (tamanoBloque * 0.48f)))
            if (obstaculo != colisionSuelo && !obstaculo.isTrigger &&
                obstaculo.GetComponentInParent<ObjetoRecogibleVoxel>() == null) return false;

        bloquesColocados[celda] = tipo;
        SonidosVoxel.Emitir("poner", centro);
        ReconstruirSuelo();
        return true;
    }

    Vector3Int CeldaEn(Vector3 posicionLocal)
    {
        return new Vector3Int(Mathf.FloorToInt(posicionLocal.x / tamanoBloque + ancho * 0.5f),
            Mathf.FloorToInt(posicionLocal.y / tamanoBloque + 0.5f),
            Mathf.FloorToInt(posicionLocal.z / tamanoBloque + largo * 0.5f));
    }

    static Vector3Int DireccionCelda(Vector3 normal)
    {
        Vector3 absoluto = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
        if (absoluto.y >= absoluto.x && absoluto.y >= absoluto.z)
            return new Vector3Int(0, normal.y > 0f ? 1 : -1, 0);
        if (absoluto.x >= absoluto.z)
            return new Vector3Int(normal.x > 0f ? 1 : -1, 0, 0);
        return new Vector3Int(0, 0, normal.z > 0f ? 1 : -1);
    }

    bool ExisteBloque(Vector3Int celda)
    {
        if (bloquesColocados.ContainsKey(celda)) return true;
        return celda.x >= 0 && celda.x < ancho && celda.z >= 0 && celda.z < largo &&
            celda.y >= FondoRoca && celda.y <= alturas[celda.x, celda.z] && !bloquesQuitados.Contains(celda);
    }

    TipoBloque TipoEn(Vector3Int celda)
    {
        TipoBloque colocado;
        if (bloquesColocados.TryGetValue(celda, out colocado)) return colocado;
        if (celda.y == alturas[celda.x, celda.z] && celdasArena[celda.x, celda.z])
            return TipoBloque.Arena;
        if (celda.y > alturas[celda.x, celda.z] - 2) return TipoBloque.Tierra;
        uint mezcla = unchecked((uint)((celda.x-DesplazamientoX) * 73856093 ^ celda.y * 19349663 ^
            (celda.z-DesplazamientoZ) * 83492791 ^ semilla));
        mezcla ^= mezcla >> 13;
        mezcla = unchecked(mezcla * 1274126177u);
        mezcla ^= mezcla >> 16;
        float valor = (mezcla % 10000u) / 10000f;
        if (celda.y <= -4 && valor < 0.008f) return TipoBloque.Diamante;
        if (valor < 0.045f) return TipoBloque.Hierro;
        if (valor < 0.125f) return TipoBloque.Carbon;
        return TipoBloque.Piedra;
    }

    void ReconstruirSuelo()
    {
        vertices.Clear();
        coordenadasTextura.Clear();
        foreach (List<int> indices in triangulos) indices.Clear();
        for (int x = 0; x < ancho; x++)
        for (int z = 0; z < largo; z++)
        for (int y = FondoRoca; y <= alturas[x, z]; y++)
        {
            Vector3Int celda = new Vector3Int(x, y, z);
            if (!bloquesColocados.ContainsKey(celda)) AgregarBloqueVisible(celda);
        }
        foreach (KeyValuePair<Vector3Int, TipoBloque> bloque in bloquesColocados)
            AgregarBloqueVisible(bloque.Key);

        Mesh nuevaMalla = new Mesh { name = "Terreno voxel editable" };
        nuevaMalla.indexFormat = IndexFormat.UInt32;
        nuevaMalla.SetVertices(vertices);
        nuevaMalla.SetUVs(0, coordenadasTextura);
        nuevaMalla.subMeshCount = triangulos.Length;
        for (int i = 0; i < triangulos.Length; i++) nuevaMalla.SetTriangles(triangulos[i], i);
        nuevaMalla.RecalculateNormals();
        nuevaMalla.RecalculateBounds();
        nuevaMalla.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        colisionSuelo.sharedMesh = null;
        filtroSuelo.sharedMesh = nuevaMalla;
        colisionSuelo.sharedMesh = nuevaMalla;
        if (mallaSuelo != null)
        {
            if (Application.isPlaying) Destroy(mallaSuelo);
            else DestroyImmediate(mallaSuelo);
        }
        mallaSuelo = nuevaMalla;
        if (Application.isPlaying) ActualizarEspeciales();
    }

    void AgregarBloqueVisible(Vector3Int celda)
    {
        if (!ExisteBloque(celda)) return;
        if ((int)TipoEn(celda) >= 8) return;
        int x = celda.x, y = celda.y, z = celda.z;
        float x0 = (x - ancho * 0.5f) * tamanoBloque;
        float z0 = (z - largo * 0.5f) * tamanoBloque;
        float x1 = x0 + tamanoBloque, z1 = z0 + tamanoBloque;
        float abajo = (y - 0.5f) * tamanoBloque, arriba = (y + 0.5f) * tamanoBloque;
        TipoBloque tipo = TipoEn(celda);
        bool conPasto = tipo == TipoBloque.Tierra &&
            (y == alturas[x, z] || bloquesColocados.ContainsKey(celda));
        int materialLateral = tipo == TipoBloque.Piedra ? 3 : tipo == TipoBloque.Madera ? 4 :
            tipo == TipoBloque.Arena ? 5 : tipo == TipoBloque.Adoquin ? 6 :
            tipo == TipoBloque.Carbon ? 7 : tipo == TipoBloque.Hierro ? 8 :
            tipo == TipoBloque.Diamante ? 9 :
            conPasto ? 1 : 2;
        if (!ExisteBloque(celda + Vector3Int.up))
        {
            int tono = conPasto ? 0 : materialLateral;
            Quad(new Vector3(x0, arriba, z0), new Vector3(x0, arriba, z1),
                new Vector3(x1, arriba, z1), new Vector3(x1, arriba, z0), tono, true);
        }
        if (y > FondoRoca && !ExisteBloque(celda + Vector3Int.down))
            Quad(new Vector3(x0, abajo, z0), new Vector3(x0, abajo, z1),
                new Vector3(x1, abajo, z1), new Vector3(x1, abajo, z0),
                tipo == TipoBloque.Tierra ? 2 : materialLateral);
        if (!ExisteBloque(celda + Vector3Int.left))
            Quad(new Vector3(x0, abajo, z0), new Vector3(x0, arriba, z0),
                new Vector3(x0, arriba, z1), new Vector3(x0, abajo, z1), materialLateral);
        if (!ExisteBloque(celda + Vector3Int.right))
            Quad(new Vector3(x1, abajo, z1), new Vector3(x1, arriba, z1),
                new Vector3(x1, arriba, z0), new Vector3(x1, abajo, z0), materialLateral);
        if (!ExisteBloque(celda + new Vector3Int(0, 0, -1)))
            Quad(new Vector3(x1, abajo, z0), new Vector3(x1, arriba, z0),
                new Vector3(x0, arriba, z0), new Vector3(x0, abajo, z0), materialLateral);
        if (!ExisteBloque(celda + new Vector3Int(0, 0, 1)))
            Quad(new Vector3(x0, abajo, z1), new Vector3(x0, arriba, z1),
                new Vector3(x1, arriba, z1), new Vector3(x1, abajo, z1), materialLateral);
    }

    void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material, bool caraSuperior = false)
    {
        int inicio = vertices.Count;
        vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
        coordenadasTextura.Add(new Vector2(0f, 0f));
        coordenadasTextura.Add(new Vector2(0f, 1f));
        coordenadasTextura.Add(new Vector2(1f, 1f));
        coordenadasTextura.Add(new Vector2(1f, 0f));
        if (!caraSuperior)
        {
            triangulos[material].Add(inicio); triangulos[material].Add(inicio + 2); triangulos[material].Add(inicio + 1);
            triangulos[material].Add(inicio); triangulos[material].Add(inicio + 3); triangulos[material].Add(inicio + 2);
        }
        else
        {
            triangulos[material].Add(inicio); triangulos[material].Add(inicio + 1); triangulos[material].Add(inicio + 2);
            triangulos[material].Add(inicio); triangulos[material].Add(inicio + 2); triangulos[material].Add(inicio + 3);
        }
    }

    static Material CrearMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(shader) { color = color };
        material.enableInstancing = true;
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
        material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return material;
    }

    static Material CrearMaterialBloque(string nombreTextura, Color tinte, bool recortarAlfa = false)
    {
        Texture2D textura = Resources.Load<Texture2D>("Minecraft/Blocks/" + nombreTextura);
        if (textura != null)
        {
            textura.filterMode = FilterMode.Point;
            textura.wrapMode = TextureWrapMode.Repeat;
        }
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(shader) { color = tinte, mainTexture = textura };
        material.enableInstancing = true;
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
        if (recortarAlfa)
        {
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 1f);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0.45f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }
        material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return material;
    }

    static Material CrearMaterialPlanta(string nombreTextura, Color tinte)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return CrearMaterialBloque(nombreTextura, tinte, true);
        Texture2D textura = Resources.Load<Texture2D>("Minecraft/Blocks/" + nombreTextura);
        if (textura != null) textura.filterMode = FilterMode.Point;
        Material material = new Material(shader) { mainTexture = textura, color = tinte };
        material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return material;
    }

    static Texture2D CrearTexturaPiedra(int semillaTextura)
    {
        Texture2D textura = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        textura.name = "Piedra pixelada";
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Repeat;
        textura.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        Color[] grises = {
            new Color(.29f, .29f, .29f), new Color(.38f, .38f, .38f),
            new Color(.47f, .47f, .47f), new Color(.57f, .57f, .57f),
            new Color(.66f, .66f, .66f)
        };
        System.Random azar = new System.Random(semillaTextura + 103);
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
        {
            float mancha = Mathf.PerlinNoise((x + semillaTextura) * .36f, (y + semillaTextura) * .36f);
            int tono = Mathf.Clamp(Mathf.FloorToInt(mancha * grises.Length) + azar.Next(-1, 2), 0, grises.Length - 1);
            textura.SetPixel(x, y, grises[tono]);
        }
        textura.Apply();
        return textura;
    }

    static Texture2D CrearTexturaMadera(int semillaTextura)
    {
        Texture2D textura = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        textura.name = "Tablas de madera pixeladas";
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Repeat;
        textura.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        Color[] marrones = {
            new Color(.28f, .18f, .08f), new Color(.39f, .26f, .12f),
            new Color(.52f, .36f, .18f), new Color(.61f, .44f, .24f),
            new Color(.70f, .53f, .31f)
        };
        System.Random azar = new System.Random(semillaTextura + 211);
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
        {
            bool junta = y % 5 == 0 || (x == (y / 5 % 2 == 0 ? 7 : 12) && y % 5 != 0);
            int tono = junta ? 0 : Mathf.Clamp(2 + azar.Next(-1, 3) + (y % 5 == 1 ? -1 : 0), 1, 4);
            textura.SetPixel(x, y, marrones[tono]);
        }
        textura.Apply();
        return textura;
    }

    static Texture2D CrearTexturaHojas(int semillaTextura)
    {
        Texture2D textura = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        textura.name = "Hojas pixeladas";
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Repeat;
        textura.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        Color[] tonos = {
            new Color(0.08f, 0.30f, 0.07f),
            new Color(0.12f, 0.39f, 0.09f),
            new Color(0.17f, 0.48f, 0.11f),
            new Color(0.23f, 0.55f, 0.14f),
            new Color(0.10f, 0.35f, 0.07f)
        };
        System.Random azar = new System.Random(semillaTextura + 17);
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
            textura.SetPixel(x, y, tonos[azar.Next(tonos.Length)]);
        textura.Apply();
        return textura;
    }

    static Texture2D CrearTexturaTronco(int semillaTextura)
    {
        Texture2D textura = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        textura.name = "Corteza pixelada";
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Repeat;
        textura.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        Color claro = new Color(0.49f, 0.32f, 0.16f);
        Color medio = new Color(0.38f, 0.23f, 0.10f);
        Color oscuro = new Color(0.25f, 0.14f, 0.06f);
        System.Random azar = new System.Random(semillaTextura + 51);
        for (int x = 0; x < 16; x++)
        {
            bool veta = azar.NextDouble() < 0.42;
            for (int y = 0; y < 16; y++)
            {
                if (azar.NextDouble() < 0.12) veta = !veta;
                textura.SetPixel(x, y, veta ? oscuro : azar.NextDouble() < 0.24 ? claro : medio);
            }
        }
        textura.Apply();
        return textura;
    }

    void CrearArbol(Transform padre, float x, float z, float baseY, float alto,
        float tamanoBloque, Material tronco, Material hojas)
    {
        GameObject arbol = new GameObject("Arbol de bloques");
        arbol.transform.SetParent(padre, false);
        arbol.transform.localPosition = new Vector3(x, baseY, z);
        int cantidadTroncos = Mathf.Max(3, Mathf.RoundToInt(alto / tamanoBloque));
        alto = cantidadTroncos * tamanoBloque;
        float copa = tamanoBloque * 0.7f;
        for (int i = 0; i < cantidadTroncos; i++)
        {
            GameObject pieza = CrearCubo(arbol.transform, "Tronco de madera", new Vector3(0, (i + 0.5f) * tamanoBloque, 0),
                Vector3.one * tamanoBloque, tronco, true);
            troncos[pieza.GetComponent<Collider>()] = pieza;
        }
        CrearCubo(arbol.transform, "Follaje base", new Vector3(0, alto - 3.8f, 0), new Vector3(7f * copa, 1.4f, 7f * copa), hojas, false);
        CrearCubo(arbol.transform, "Follaje ancho", new Vector3(0, alto - 2.7f, 0), new Vector3(7.2f * copa, 1.4f, 7.2f * copa), hojas, false);
        CrearCubo(arbol.transform, "Follaje medio", new Vector3(0, alto - 1.5f, 0), new Vector3(6.4f * copa, 1.6f, 6.4f * copa), hojas, false);
        CrearCubo(arbol.transform, "Follaje superior", new Vector3(0, alto - 0.2f, 0), new Vector3(5f * copa, 1.5f, 5f * copa), hojas, false);
        CrearCubo(arbol.transform, "Copa inferior", new Vector3(0, alto + 1.1f, 0), new Vector3(3.5f * copa, 1.4f, 3.5f * copa), hojas, false);
        CrearCubo(arbol.transform, "Copa alta", new Vector3(0, alto + 2.4f, 0), new Vector3(2.5f * copa, 1.3f, 2.5f * copa), hojas, false);
    }

    static GameObject CrearCubo(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material, bool colision)
    {
        GameObject bloque = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bloque.name = nombre;
        bloque.transform.SetParent(padre, false);
        bloque.transform.localPosition = posicion;
        bloque.transform.localScale = escala;
        bloque.GetComponent<Renderer>().sharedMaterial = material;
        if (!colision)
        {
            Collider c = bloque.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
        return bloque;
    }
}
