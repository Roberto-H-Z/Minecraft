using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class VoxelCharacterVisual : MonoBehaviour
{
    [Header("Materiales del personaje")]
    public Material piel;
    public Material camisa;
    public Material pantalon;
    public Material cabello;
    public Material ojos;

    private const string NombreVisual = "VoxelVisual";
    private Transform brazoIzquierdo;
    private Transform brazoDerecho;
    private Transform piernaIzquierda;
    private Transform piernaDerecha;
    private GameObject[] objetosMano;
    private GameObject[] herramientasVista;
    private GameObject manoVista;
    private CamaraSeguir camaraSeguir;
    private float finGolpe;
    private Texture2D texturaEspada;
    private bool texturaEspadaGenerada;
    private Material materialEspada;
    private Texture2D texturaPico;
    private bool texturaPicoGenerada;
    private Material materialPico;
    private Material materialTridente;
    private Material materialBotinMano;
    private Material materialBotinVista;
    private Mesh mallaEspada;
    private Mesh mallaBotin;
    private Mesh mallaTierraMano;
    private Mesh mallaCubo;
    private Mesh mallaBloquesEspeciales;
    private Mesh mallaAntorcha;
    private Rigidbody fisica;
    private script controlador;
    private float cicloPaso;
    private Material materialArena;
    private Material materialPiedraBloque;
    private Material materialTierraBloque;
    private Material materialPielMinecraft;
    private MundoVoxel mundoVisual;
    private TipoBloque? ultimoBloqueMano;
    private TipoBloque? ultimoBloqueVista;
    private Renderer[] piezasCuerpo;
    private bool[] piezasVisibles;
    private Transform raizCuerpo;
    private float escalaCuerpo;
    private float pieModelo;

    void OnEnable()
    {
        if (piel == null) piel = CrearMaterial(new Color(0.69f, 0.43f, 0.27f));
        if (camisa == null) camisa = CrearMaterial(new Color(0.12f, 0.38f, 0.78f));
        if (pantalon == null) pantalon = CrearMaterial(new Color(0.12f, 0.20f, 0.48f));
        if (cabello == null) cabello = CrearMaterial(new Color(0.20f, 0.11f, 0.06f));
        if (ojos == null) ojos = CrearMaterial(new Color(0.08f, 0.05f, 0.03f));
        CrearVisualSiHaceFalta();
        if (!Application.isPlaying && GetComponent<script>() != null)
        {
            MundoVoxel mundo = FindFirstObjectByType<MundoVoxel>();
            float altura = (mundo != null ? mundo.TamanoBloque : 2f) * 2f;
            transform.localScale = Vector3.one;
            CapsuleCollider capsula = GetComponent<CapsuleCollider>();
            if (capsula != null)
            {
                capsula.height = altura;
                capsula.radius = altura * .15f;
                capsula.center = Vector3.zero;
            }
            Transform visual = transform.Find(NombreVisual);
            if (visual != null) { AplicarPielMinecraft(visual); AjustarAltura(visual, altura); }
        }
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        fisica = GetComponent<Rigidbody>();
        controlador = GetComponent<script>();
        Transform visual = transform.Find(NombreVisual);
        if (visual == null) return;
        AplicarPielMinecraft(visual);
        AjustarAltura(visual, controlador != null ? controlador.AlturaDePie : 4f);

        brazoIzquierdo = CrearArticulacion(visual, "Hombro izquierdo", new Vector3(-0.47f, 0.30f, 0f), "Brazo izquierdo", "Mano izquierda");
        brazoDerecho = CrearArticulacion(visual, "Hombro derecho", new Vector3(0.47f, 0.30f, 0f), "Brazo derecho", "Mano derecha");
        piernaIzquierda = CrearArticulacion(visual, "Cadera izquierda", new Vector3(-0.17f, -0.42f, 0f), "Pierna izquierda", "Bota izquierda");
        piernaDerecha = CrearArticulacion(visual, "Cadera derecha", new Vector3(0.17f, -0.42f, 0f), "Pierna derecha", "Bota derecha");

        objetosMano = new GameObject[9];
        Material[] colores = {
            CrearMaterial(new Color(1f, 0.72f, 0.12f)),
            CrearMaterial(new Color(0.34f, 0.65f, 0.17f)),
            CrearMaterial(new Color(0.45f, 0.28f, 0.12f)),
            CrearMaterial(new Color(0.83f, 0.16f, 0.11f))
        };
        materialTierraBloque = colores[1];
        for (int i = 0; i < 4; i++)
        {
            objetosMano[i] = GameObject.CreatePrimitive(i == 0 || i == 3 ? PrimitiveType.Sphere : PrimitiveType.Cube);
            objetosMano[i].name = "Objeto en mano " + (i + 1);
            objetosMano[i].transform.SetParent(brazoDerecho, false);
            objetosMano[i].transform.localPosition = new Vector3(0f, -0.72f, -0.32f);
            objetosMano[i].transform.localScale = i == 2 ? new Vector3(0.22f, 0.38f, 0.22f) : Vector3.one * 0.28f;
            objetosMano[i].GetComponent<Renderer>().sharedMaterial = colores[i];
            Collider colisionObjeto = objetosMano[i].GetComponent<Collider>();
            colisionObjeto.enabled = false;
            Destroy(colisionObjeto);
        }
        Destroy(objetosMano[3]);
        objetosMano[3] = CrearBotinPlano(brazoDerecho, "Botin en mano",
            new Vector3(0f, -0.72f, -0.33f), 0.48f, out materialBotinMano);

        texturaEspada = Resources.Load<Texture2D>("Minecraft/Items/diamond_sword");
        if (texturaEspada == null)
        {
            texturaEspada = CrearTexturaEspada();
            texturaEspadaGenerada = true;
        }
        texturaEspada.filterMode = FilterMode.Point;
        texturaEspada.wrapMode = TextureWrapMode.Clamp;
        materialEspada = CrearMaterialEspada(texturaEspada);
        texturaPico = Resources.Load<Texture2D>("Minecraft/Items/diamond_pickaxe");
        if (texturaPico == null)
        {
            texturaPico = CrearTexturaPico();
            texturaPicoGenerada = true;
        }
        texturaPico.filterMode = FilterMode.Point;
        texturaPico.wrapMode = TextureWrapMode.Clamp;
        materialPico = CrearMaterialEspada(texturaPico);
        materialPico.name = "Pico de diamante pixelado";
        Texture2D texturaTridente = Resources.Load<Texture2D>("Minecraft/Items/trident");
        if (texturaTridente != null)
        {
            texturaTridente.filterMode = FilterMode.Point;
            materialTridente = CrearMaterialEspada(texturaTridente);
            materialTridente.name = "Tridente pixelado";
        }
        mallaEspada = CrearMallaEspada();
        objetosMano[4] = CrearHerramientaPixelada(brazoDerecho, "Espada", "Espada de diamante",
            new Vector3(0f, -0.70f, -0.30f), Vector3.one * 0.85f, Quaternion.Euler(0f, 0f, -15f), materialEspada);
        objetosMano[5] = CrearHerramientaPixelada(brazoDerecho, "Pico", "Pico de diamante",
            new Vector3(0f, -0.70f, -0.30f), Vector3.one * 0.85f, Quaternion.Euler(0f, 0f, -15f), materialPico);
        objetosMano[6] = CrearBloqueEnMano(brazoDerecho, "Bloque en mano",
            new Vector3(0f, -0.72f, -0.32f), Vector3.one * 0.43f, colores[1]);
        MundoVoxel mundo = FindFirstObjectByType<MundoVoxel>();
        mundoVisual = mundo;
        if (mundo != null && mundo.MaterialTierra != null)
            materialTierraBloque = mundo.MaterialTierra;
        Material piedra = mundo != null && mundo.MaterialPiedra != null
            ? mundo.MaterialPiedra : CrearMaterial(new Color(.48f, .48f, .48f));
        Material madera = mundo != null && mundo.MaterialMadera != null
            ? mundo.MaterialMadera : CrearMaterial(new Color(.55f, .37f, .18f));
        materialArena = mundo != null && mundo.MaterialArena != null
            ? mundo.MaterialArena : CrearMaterial(new Color(.79f, .72f, .49f));
        materialPiedraBloque = piedra;
        objetosMano[7] = CrearBloqueEnMano(brazoDerecho, "Piedra en mano",
            new Vector3(0f, -0.72f, -0.32f), Vector3.one * 0.43f, piedra);
        objetosMano[8] = CrearBloqueEnMano(brazoDerecho, "Madera en mano",
            new Vector3(0f, -0.72f, -0.32f), Vector3.one * 0.43f, madera);
        mallaCubo = objetosMano[6].GetComponent<MeshFilter>().sharedMesh;
        mallaTierraMano = CrearMallaTierraEnMano();
        Destroy(objetosMano[2]);
        objetosMano[2] = CrearHerramientaPixelada(brazoDerecho, "Tridente", "Tridente en mano",
            new Vector3(0f, -0.70f, -0.30f), Vector3.one * 0.85f,
            Quaternion.Euler(0f, 0f, -15f), materialTridente != null ? materialTridente : materialPico);

        Camera camaraPrincipal = Camera.main;
        if (camaraPrincipal != null)
        {
            camaraSeguir = camaraPrincipal.GetComponent<CamaraSeguir>();
            manoVista = GameObject.CreatePrimitive(PrimitiveType.Cube);
            manoVista.name = "Mano primera persona";
            manoVista.transform.SetParent(camaraPrincipal.transform, false);
            manoVista.transform.localPosition = new Vector3(0.55f, -0.69f, 0.83f);
            manoVista.transform.localScale = new Vector3(0.25f, 0.42f, 0.30f);
            manoVista.GetComponent<Renderer>().sharedMaterial = piel;
            MinecraftSkinBox.Aplicar(manoVista.transform, materialPielMinecraft,
                40, 16, 4, 12, 4, true);
            Collider colisionMano = manoVista.GetComponent<Collider>();
            colisionMano.enabled = false;
            Destroy(colisionMano);
            manoVista.SetActive(false);
            herramientasVista = new GameObject[9];
            for (int i = 0; i < 4; i++)
            {
                GameObject objeto = GameObject.CreatePrimitive(i == 0 || i == 3 ? PrimitiveType.Sphere : PrimitiveType.Cube);
                objeto.name = "Objeto primera persona " + (i + 1);
                objeto.transform.SetParent(camaraPrincipal.transform, false);
                objeto.transform.localPosition = new Vector3(0.58f, -0.48f, 1.0f);
                objeto.transform.localScale = i == 2 ? new Vector3(0.20f, 0.36f, 0.20f) : Vector3.one * 0.29f;
                objeto.GetComponent<Renderer>().sharedMaterial = colores[i];
                Collider colisionVista = objeto.GetComponent<Collider>();
                colisionVista.enabled = false;
                Destroy(colisionVista);
                herramientasVista[i] = objeto;
            }
            Destroy(herramientasVista[3]);
            herramientasVista[3] = CrearBotinPlano(camaraPrincipal.transform, "Botin primera persona",
                new Vector3(0.58f, -0.48f, 1f), 0.52f, out materialBotinVista);
            herramientasVista[4] = CrearHerramientaPixelada(camaraPrincipal.transform, "Espada primera persona", "Espada de diamante",
                new Vector3(1.47f, -0.95f, 1.05f), new Vector3(-1.28f, 1.28f, 1.28f), Quaternion.identity, materialEspada);
            herramientasVista[5] = CrearHerramientaPixelada(camaraPrincipal.transform, "Pico primera persona", "Pico de diamante",
                new Vector3(1.35f, -0.92f, 1.05f), new Vector3(-1.22f, 1.22f, 1.22f), Quaternion.identity, materialPico);
            herramientasVista[6] = CrearBloqueEnMano(camaraPrincipal.transform, "Bloque primera persona",
                new Vector3(0.60f, -0.48f, 1f), Vector3.one * 0.43f, colores[1]);
            herramientasVista[7] = CrearBloqueEnMano(camaraPrincipal.transform, "Piedra primera persona",
                new Vector3(0.60f, -0.48f, 1f), Vector3.one * 0.43f, piedra);
            herramientasVista[8] = CrearBloqueEnMano(camaraPrincipal.transform, "Madera primera persona",
                new Vector3(0.60f, -0.48f, 1f), Vector3.one * 0.43f, madera);
            Destroy(herramientasVista[2]);
            herramientasVista[2] = CrearHerramientaPixelada(camaraPrincipal.transform,
                "Tridente primera persona", "Tridente", new Vector3(1.40f, -0.95f, 1.05f),
                new Vector3(-1.22f, 1.22f, 1.22f), Quaternion.identity,
                materialTridente != null ? materialTridente : materialPico);
            foreach (GameObject objeto in herramientasVista) objeto.SetActive(false);
        }
    }

    GameObject CrearBloqueEnMano(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject bloque = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bloque.name = nombre;
        bloque.transform.SetParent(padre, false);
        bloque.transform.localPosition = posicion;
        bloque.transform.localScale = escala;
        bloque.GetComponent<Renderer>().sharedMaterial = material;
        Collider colision = bloque.GetComponent<Collider>();
        colision.enabled = false;
        Destroy(colision);
        return bloque;
    }

    GameObject CrearBotinPlano(Transform padre, string nombre, Vector3 posicion, float tamano,
        out Material material)
    {
        Shader shader = Shader.Find("Unlit/Transparent Cutout");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        material = new Material(shader) { color = Color.white };
        if (mallaBotin == null)
        {
            mallaBotin = new Mesh { name = "Icono plano del botin" };
            mallaBotin.vertices = new[] {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
            };
            mallaBotin.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mallaBotin.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            mallaBotin.RecalculateNormals();
        }
        GameObject objeto = new GameObject(nombre);
        objeto.name = nombre;
        objeto.transform.SetParent(padre, false);
        objeto.transform.localPosition = posicion;
        objeto.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        objeto.transform.localScale = Vector3.one * tamano;
        objeto.AddComponent<MeshFilter>().sharedMesh = mallaBotin;
        objeto.AddComponent<MeshRenderer>().sharedMaterial = material;
        return objeto;
    }

    public static Mesh CrearMallaTierraEnMano()
    {
        Vector3[,] lados = {
            { new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f) },
            { new Vector3(.5f,-.5f,-.5f), new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(.5f,.5f,-.5f) },
            { new Vector3(.5f,-.5f,.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(.5f,.5f,.5f) },
            { new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,-.5f,.5f), new Vector3(-.5f,.5f,.5f), new Vector3(-.5f,.5f,-.5f) },
            { new Vector3(-.5f,.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f) },
            { new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,-.5f,.5f), new Vector3(-.5f,-.5f,.5f) }
        };
        Vector3[] puntos = new Vector3[24];
        Vector2[] uv = new Vector2[24];
        System.Collections.Generic.List<int>[] triangulos = {
            new System.Collections.Generic.List<int>(),
            new System.Collections.Generic.List<int>(),
            new System.Collections.Generic.List<int>()
        };
        for (int cara = 0; cara < 6; cara++)
        {
            int inicio = cara * 4;
            for (int i = 0; i < 4; i++) puntos[inicio + i] = lados[cara, i];
            uv[inicio] = Vector2.zero;
            uv[inicio + 1] = Vector2.right;
            uv[inicio + 2] = Vector2.one;
            uv[inicio + 3] = Vector2.up;
            int material = cara == 4 ? 0 : cara == 5 ? 2 : 1;
            triangulos[material].Add(inicio); triangulos[material].Add(inicio + 1);
            triangulos[material].Add(inicio + 2); triangulos[material].Add(inicio);
            triangulos[material].Add(inicio + 2); triangulos[material].Add(inicio + 3);
        }
        Mesh malla = new Mesh { name = "Bloque de tierra con pasto por cara" };
        malla.vertices = puntos;
        malla.uv = uv;
        malla.subMeshCount = 3;
        for (int i = 0; i < 3; i++) malla.SetTriangles(triangulos[i], i);
        malla.RecalculateNormals();
        malla.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return malla;
    }

    void ActualizarBloqueEnMano(GameObject bloque, TipoBloque tipo)
    {
        if (bloque == null || mundoVisual == null) return;
        MeshFilter filtro = bloque.GetComponent<MeshFilter>();
        MeshRenderer renderizador = bloque.GetComponent<MeshRenderer>();
        if (filtro == null || renderizador == null) return;
        if (tipo == TipoBloque.Tierra && mallaTierraMano != null &&
            mundoVisual.MaterialPastoSuperior != null)
        {
            filtro.sharedMesh = mallaTierraMano;
            renderizador.sharedMaterials = new[] { mundoVisual.MaterialPastoSuperior,
                mundoVisual.MaterialTierra, mundoVisual.MaterialTierraPura };
        }
        else if (tipo == TipoBloque.Cofre)
        {
            filtro.sharedMesh=ModeloCofreVoxel.Malla;
            renderizador.sharedMaterial=ModeloCofreVoxel.Material;
        }
        else if(tipo==TipoBloque.Antorcha)
        {
            if(mallaAntorcha==null) mallaAntorcha=AntorchaVoxel.CrearMalla();
            filtro.sharedMesh=mallaAntorcha; renderizador.sharedMaterials=mundoVisual.MaterialesEspeciales(tipo,false);
        }
        else if ((int)tipo >= 8)
        {
            if (mallaBloquesEspeciales == null) mallaBloquesEspeciales = BloqueFuncionalVoxel.CrearCuboSeisCaras();
            filtro.sharedMesh = mallaBloquesEspeciales;
            renderizador.sharedMaterials = mundoVisual.MaterialesEspeciales(tipo,false);
        }
        else
        {
            filtro.sharedMesh = mallaCubo;
            renderizador.sharedMaterial = mundoVisual.MaterialDe(tipo);
        }
    }

    Texture2D CrearTexturaEspada()
    {
        const int tamano = 32;
        Texture2D textura = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        textura.name = "Espada de diamante pixelada";
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        Color32 borde = new Color32(8, 48, 48, 255);
        Color32 sombra = new Color32(13, 104, 105, 255);
        Color32 turquesa = new Color32(24, 176, 182, 255);
        Color32 brillo = new Color32(93, 239, 239, 255);
        Color32 mango = new Color32(27, 75, 59, 255);
        Color32 envoltura = new Color32(32, 143, 127, 255);
        Color32[] pixeles = new Color32[tamano * tamano];

        for (int y = 0; y < tamano; y++)
        for (int x = 0; x < tamano; x++)
        {
            float largo = (x + y) * 0.5f;
            float ancho = Mathf.Abs(y - x);
            Color32 color = new Color32(0, 0, 0, 0);

            if (largo >= 2f && largo <= 4.5f && ancho <= 4.5f)
                color = ancho >= 3f || largo <= 2.5f ? borde : sombra;
            if (largo >= 4f && largo <= 11.5f && ancho <= 2.5f)
                color = ancho >= 1.5f ? borde : ((x + y) % 4 < 2 ? mango : envoltura);
            if (largo >= 10f && largo <= 13f && ancho <= 7f)
                color = ancho >= 5f ? borde : sombra;
            if (largo >= 12f && largo <= 30f)
            {
                float mediaAnchura = largo > 26f ? Mathf.Max(0.5f, 4.5f - (largo - 26f)) : 4.5f;
                if (ancho <= mediaAnchura)
                    color = ancho >= mediaAnchura - 1.5f ? borde :
                        y > x ? brillo : ((x + y) % 5 < 2 ? turquesa : sombra);
            }

            pixeles[y * tamano + x] = color;
        }

        textura.SetPixels32(pixeles);
        textura.Apply(false, true);
        return textura;
    }

    Texture2D CrearTexturaPico()
    {
        const int tamano = 32;
        Texture2D textura = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        textura.name = "Pico de diamante pixelado";
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        Color32 oscuro = new Color32(22, 24, 25, 255);
        Color32 madera = new Color32(118, 82, 47, 255);
        Color32 maderaClara = new Color32(159, 117, 72, 255);
        Color32 turquesa = new Color32(18, 187, 194, 255);
        Color32 brillo = new Color32(75, 232, 231, 255);
        Color32[] pixeles = new Color32[tamano * tamano];

        for (int y = 0; y < tamano; y++)
        for (int x = 0; x < tamano; x++)
        {
            Color32 color = new Color32(0, 0, 0, 0);
            float centroMango = 5f + (y - 2f) * 0.72f;
            float distanciaMango = Mathf.Abs(x - centroMango);
            if (y >= 2 && y <= 25 && distanciaMango <= 2.5f)
                color = distanciaMango >= 1.5f ? oscuro : ((x + y) % 5 < 2 ? maderaClara : madera);

            if (y >= 24 && y <= 29 && x >= 3 && x <= 25)
                color = y == 24 || y == 29 || x <= 4 || x >= 24 ? oscuro :
                    ((x + y) % 5 < 2 ? brillo : turquesa);
            if (x >= 23 && x <= 29 && y >= 15 && y <= 27)
                color = x >= 28 || y <= 16 || x == 23 ? oscuro :
                    ((x + y) % 5 < 2 ? brillo : turquesa);
            if (x >= 2 && x <= 6 && y >= 22 && y <= 25)
                color = oscuro;

            pixeles[y * tamano + x] = color;
        }

        textura.SetPixels32(pixeles);
        textura.Apply(false, true);
        return textura;
    }

    Material CrearMaterialEspada(Texture2D textura)
    {
        Shader shader = Shader.Find("Unlit/Transparent Cutout");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = "Diamante pixelado";
        material.mainTexture = textura;
        material.color = Color.white;
        material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return material;
    }

    Mesh CrearMallaEspada()
    {
        Mesh malla = new Mesh { name = "Plano de espada por ambos lados" };
        malla.vertices = new[] {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
        };
        malla.uv = new[] {
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
        };
        malla.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6 };
        malla.normals = new[] {
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.back, Vector3.back, Vector3.back, Vector3.back
        };
        malla.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return malla;
    }

    GameObject CrearHerramientaPixelada(Transform padre, string nombre, string nombrePlano,
        Vector3 posicion, Vector3 escala, Quaternion rotacion, Material material)
    {
        GameObject espada = new GameObject(nombre);
        espada.transform.SetParent(padre, false);
        espada.transform.localPosition = posicion;
        espada.transform.localRotation = rotacion;
        espada.transform.localScale = escala;

        GameObject hoja = new GameObject(nombrePlano);
        hoja.transform.SetParent(espada.transform, false);
        hoja.transform.localPosition = new Vector3(0.42f, 0.42f, 0f);
        hoja.AddComponent<MeshFilter>().sharedMesh = mallaEspada;
        MeshRenderer renderizador = hoja.AddComponent<MeshRenderer>();
        renderizador.sharedMaterial = material;
        renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return espada;
    }

    public void AnimarGolpe()
    {
        finGolpe = Time.time + 0.28f;
    }

    Transform CrearArticulacion(Transform visual, string nombre, Vector3 posicion, string pieza, string extremo)
    {
        GameObject pivote = new GameObject(nombre);
        pivote.transform.SetParent(visual, false);
        pivote.transform.localPosition = posicion;
        MoverPieza(visual, pivote.transform, pieza, posicion);
        MoverPieza(visual, pivote.transform, extremo, posicion);
        return pivote.transform;
    }

    static void MoverPieza(Transform origen, Transform destino, string nombre, Vector3 posicionPivote)
    {
        Transform pieza = origen.Find(nombre);
        if (pieza == null) return;
        Vector3 posicionOriginal = pieza.localPosition;
        pieza.SetParent(destino, false);
        pieza.localPosition = posicionOriginal - posicionPivote;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || fisica == null || brazoIzquierdo == null) return;
        Vector3 velocidad = fisica.velocity;
        velocidad.y = 0f;
        float rapidez = velocidad.magnitude;
        if (rapidez > 0.1f) cicloPaso += Time.deltaTime * Mathf.Min(15f, rapidez * 1.25f);
        float amplitud = Mathf.Clamp01(rapidez / 5f) * 32f;
        float balanceo = Mathf.Sin(cicloPaso) * amplitud;
        float avanceGolpe = Mathf.Clamp01((finGolpe - Time.time) / 0.28f);
        float golpe = Mathf.Sin((1f - avanceGolpe) * Mathf.PI) * 75f;
        float cargaTridente = controlador != null ? Mathf.Max(controlador.CargaTridente,controlador.CargaArco) : 0f;
        Animar(brazoIzquierdo, -balanceo);
        Animar(brazoDerecho, balanceo + golpe - cargaTridente * 35f);
        Animar(piernaIzquierda, balanceo);
        Animar(piernaDerecha, -balanceo);

        if (controlador != null && controlador.TipoVisualHerramienta == 0 && materialEspada != null)
            materialEspada.mainTexture = controlador.IconoEnSlot(controlador.slotSeleccionado);
        if (controlador != null && controlador.TipoVisualHerramienta == 1 && materialPico != null)
            materialPico.mainTexture = controlador.IconoHerramientaActual;
        bool primeraPersonaActiva = camaraSeguir != null && camaraSeguir.EnPrimeraPersona;
        if (piezasCuerpo != null)
            for (int i = 0; i < piezasCuerpo.Length; i++)
                if (piezasCuerpo[i] != null) piezasCuerpo[i].enabled = piezasVisibles[i] && !primeraPersonaActiva;
        if (raizCuerpo != null && controlador != null && escalaCuerpo > 0f)
        {
            float altura = controlador.Agachado ? .75f : 1f;
            raizCuerpo.localScale = new Vector3(escalaCuerpo, escalaCuerpo * altura, escalaCuerpo);
            raizCuerpo.localPosition = Vector3.up * (-controlador.AlturaDePie * .5f - pieModelo * escalaCuerpo * altura);
        }
        if (manoVista != null)
        {
            manoVista.SetActive(primeraPersonaActiva);
            manoVista.transform.localPosition = controlador != null && controlador.TipoVisualHerramienta >= 0
                ? new Vector3(1.25f, -0.88f, 0.83f)
                : new Vector3(0.55f, -0.69f, 0.83f);
            if (controlador != null) manoVista.transform.localPosition += Vector3.up * controlador.MovimientoMano;
        }
        if (objetosMano != null && controlador != null)
        {
            Texture2D iconoBotin = controlador.IconoBotinEnSlot(controlador.slotSeleccionado);
            if (materialBotinMano != null) materialBotinMano.mainTexture = iconoBotin;
            if (materialBotinVista != null) materialBotinVista.mainTexture = iconoBotin;
            if (controlador.EsBloqueEnSlot(controlador.slotSeleccionado) &&
                ultimoBloqueMano != controlador.TipoEnSlot(controlador.slotSeleccionado))
            {
                ultimoBloqueMano = controlador.TipoEnSlot(controlador.slotSeleccionado);
                ActualizarBloqueEnMano(objetosMano[6], ultimoBloqueMano.Value);
            }
            for (int i = 0; i < objetosMano.Length; i++)
                if (objetosMano[i] != null) objetosMano[i].SetActive(!primeraPersonaActiva &&
                    (i == 4 && controlador.TipoVisualHerramienta == 0 ||
                     i == 5 && controlador.TipoVisualHerramienta == 1 ||
                     i == 2 && controlador.TipoVisualHerramienta == 2 ||
                     i == 3 && controlador.EsBotinEnSlot(controlador.slotSeleccionado) ||
                     i == 6 && controlador.EsBloqueEnSlot(controlador.slotSeleccionado)));
        }

        if (herramientasVista != null && controlador != null)
        {
            if (controlador.EsBloqueEnSlot(controlador.slotSeleccionado) &&
                ultimoBloqueVista != controlador.TipoEnSlot(controlador.slotSeleccionado))
            {
                ultimoBloqueVista = controlador.TipoEnSlot(controlador.slotSeleccionado);
                ActualizarBloqueEnMano(herramientasVista[6], ultimoBloqueVista.Value);
            }
            for (int i = 0; i < herramientasVista.Length; i++)
                if (herramientasVista[i] != null) herramientasVista[i].SetActive(primeraPersonaActiva &&
                    (i == 4 && controlador.TipoVisualHerramienta == 0 ||
                     i == 5 && controlador.TipoVisualHerramienta == 1 ||
                     i == 2 && controlador.TipoVisualHerramienta == 2 ||
                     i == 3 && controlador.EsBotinEnSlot(controlador.slotSeleccionado) ||
                     i == 6 && controlador.EsBloqueEnSlot(controlador.slotSeleccionado)));
            herramientasVista[4].transform.localRotation = Quaternion.Euler(0f, 0f, -golpe * 0.75f);
            herramientasVista[5].transform.localRotation = Quaternion.Euler(0f, 0f, -golpe * 0.75f);
            herramientasVista[2].transform.localPosition = Vector3.Lerp(
                new Vector3(1.40f, -0.95f, 1.05f), new Vector3(1.60f, -0.83f, 0.90f), cargaTridente);
            herramientasVista[2].transform.localRotation = Quaternion.Euler(
                -18f * cargaTridente, 0f, -golpe * 0.75f);
        }
    }

    void AjustarAltura(Transform visual, float altura)
    {
        raizCuerpo = visual;
        visual.localScale = Vector3.one;
        visual.localPosition = Vector3.zero;
        piezasCuerpo = visual.GetComponentsInChildren<Renderer>(true);
        piezasVisibles = new bool[piezasCuerpo.Length];
        float minimo = float.PositiveInfinity, maximo = float.NegativeInfinity;
        for (int i = 0; i < piezasCuerpo.Length; i++)
        {
            Renderer pieza = piezasCuerpo[i];
            piezasVisibles[i] = pieza.enabled;
            MeshFilter filtro = pieza.GetComponent<MeshFilter>();
            if (!pieza.enabled || filtro == null || filtro.sharedMesh == null) continue;
            Bounds limites = filtro.sharedMesh.bounds;
            for (int esquina = 0; esquina < 8; esquina++)
            {
                Vector3 punto = limites.center + Vector3.Scale(limites.extents,
                    new Vector3((esquina & 1) == 0 ? -1 : 1, (esquina & 2) == 0 ? -1 : 1, (esquina & 4) == 0 ? -1 : 1));
                float y = visual.InverseTransformPoint(pieza.transform.TransformPoint(punto)).y;
                minimo = Mathf.Min(minimo, y);
                maximo = Mathf.Max(maximo, y);
            }
        }
        if (maximo <= minimo) return;
        pieModelo = minimo;
        escalaCuerpo = altura / (maximo - minimo);
        visual.localScale = Vector3.one * escalaCuerpo;
        visual.localPosition = Vector3.up * (-altura * .5f - minimo * escalaCuerpo);
    }

    static void Animar(Transform articulacion, float angulo)
    {
        if (articulacion == null) return;
        articulacion.localRotation = Quaternion.Slerp(articulacion.localRotation, Quaternion.Euler(angulo, 0f, 0f), Time.deltaTime * 12f);
    }

    void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (manoVista != null) Destroy(manoVista);
        if (herramientasVista != null)
            foreach (GameObject herramienta in herramientasVista)
                if (herramienta != null) Destroy(herramienta);
        if (materialEspada != null) Destroy(materialEspada);
        if (texturaEspadaGenerada && texturaEspada != null) Destroy(texturaEspada);
        if (materialPico != null) Destroy(materialPico);
        if (materialTridente != null) Destroy(materialTridente);
        if (materialBotinMano != null) Destroy(materialBotinMano);
        if (materialBotinVista != null) Destroy(materialBotinVista);
        if (texturaPicoGenerada && texturaPico != null) Destroy(texturaPico);
        if (mallaEspada != null) Destroy(mallaEspada);
        if (mallaBotin != null) Destroy(mallaBotin);
        if (mallaTierraMano != null) Destroy(mallaTierraMano);
        if (mallaBloquesEspeciales != null) Destroy(mallaBloquesEspeciales);
        if(mallaAntorcha!=null) Destroy(mallaAntorcha);
        if (materialPielMinecraft != null) Destroy(materialPielMinecraft);
    }

    void AplicarPielMinecraft(Transform visual)
    {
        if (materialPielMinecraft != null)
        {
            if (Application.isPlaying) Destroy(materialPielMinecraft);
            else DestroyImmediate(materialPielMinecraft);
        }
        materialPielMinecraft = MinecraftSkinBox.CrearMaterial("steve");
        if (materialPielMinecraft == null) return;
        MinecraftSkinBox.Aplicar(visual.Find("Cabeza"), materialPielMinecraft, 0, 0, 8, 8, 8, true);
        MinecraftSkinBox.Aplicar(visual.Find("Torso"), materialPielMinecraft, 16, 16, 8, 12, 4, true);
        MinecraftSkinBox.Aplicar(visual.Find("Brazo derecho"), materialPielMinecraft, 40, 16, 4, 12, 4, true);
        MinecraftSkinBox.Aplicar(visual.Find("Brazo izquierdo"), materialPielMinecraft, 32, 48, 4, 12, 4, true);
        MinecraftSkinBox.Aplicar(visual.Find("Pierna derecha"), materialPielMinecraft, 0, 16, 4, 12, 4, true);
        MinecraftSkinBox.Aplicar(visual.Find("Pierna izquierda"), materialPielMinecraft, 16, 48, 4, 12, 4, true);
        string[] ocultar = { "Cabello", "Ojo izquierdo", "Ojo derecho", "Boca",
            "Mano izquierda", "Mano derecha", "Bota izquierda", "Bota derecha" };
        foreach (string nombre in ocultar) MinecraftSkinBox.Ocultar(visual.Find(nombre));
        foreach (string nombre in new[] { "Brazo derecho", "Brazo izquierdo" })
        {
            Transform brazo = visual.Find(nombre);
            if (brazo == null) continue;
            brazo.localPosition = new Vector3(brazo.localPosition.x, -0.08f, brazo.localPosition.z);
            brazo.localScale = new Vector3(brazo.localScale.x, 0.74f, brazo.localScale.z);
        }
        foreach (string nombre in new[] { "Pierna derecha", "Pierna izquierda" })
        {
            Transform pierna = visual.Find(nombre);
            if (pierna == null) continue;
            pierna.localScale = new Vector3(pierna.localScale.x, 0.80f, pierna.localScale.z);
        }
    }

    Material CrearMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    void CrearVisualSiHaceFalta()
    {
        Transform visualExistente = transform.Find(NombreVisual);
        if (visualExistente != null)
        {
            // La cara fue modelada originalmente hacia -Z; alinea ese frente con +Z.
            visualExistente.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visualExistente.localPosition = new Vector3(0f, 0.16f, 0f);
            return;
        }

        GameObject visual = new GameObject(NombreVisual);
        visual.transform.SetParent(transform, false);
        visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        visual.transform.localPosition = new Vector3(0f, 0.16f, 0f);

        // Cabeza, cabello y cara
        CrearBloque(visual.transform, "Cabeza", new Vector3(0f, 0.62f, 0f), new Vector3(0.62f, 0.62f, 0.62f), piel);
        CrearBloque(visual.transform, "Cabello", new Vector3(0f, 0.94f, 0f), new Vector3(0.66f, 0.18f, 0.66f), cabello);
        CrearBloque(visual.transform, "Ojo izquierdo", new Vector3(-0.14f, 0.65f, -0.316f), new Vector3(0.08f, 0.09f, 0.025f), ojos);
        CrearBloque(visual.transform, "Ojo derecho", new Vector3(0.14f, 0.65f, -0.316f), new Vector3(0.08f, 0.09f, 0.025f), ojos);
        CrearBloque(visual.transform, "Boca", new Vector3(0f, 0.47f, -0.316f), new Vector3(0.13f, 0.035f, 0.025f), cabello);

        // Torso, brazos y piernas
        CrearBloque(visual.transform, "Torso", new Vector3(0f, -0.04f, 0f), new Vector3(0.62f, 0.68f, 0.36f), camisa);
        CrearBloque(visual.transform, "Brazo izquierdo", new Vector3(-0.47f, 0.07f, 0f), new Vector3(0.27f, 0.42f, 0.34f), camisa);
        CrearBloque(visual.transform, "Mano izquierda", new Vector3(-0.47f, -0.29f, 0f), new Vector3(0.25f, 0.32f, 0.33f), piel);
        CrearBloque(visual.transform, "Brazo derecho", new Vector3(0.47f, 0.07f, 0f), new Vector3(0.27f, 0.42f, 0.34f), camisa);
        CrearBloque(visual.transform, "Mano derecha", new Vector3(0.47f, -0.29f, 0f), new Vector3(0.25f, 0.32f, 0.33f), piel);
        CrearBloque(visual.transform, "Pierna izquierda", new Vector3(-0.17f, -0.76f, 0f), new Vector3(0.30f, 0.62f, 0.38f), pantalon);
        CrearBloque(visual.transform, "Pierna derecha", new Vector3(0.17f, -0.76f, 0f), new Vector3(0.30f, 0.62f, 0.38f), pantalon);
        CrearBloque(visual.transform, "Bota izquierda", new Vector3(-0.17f, -1.08f, -0.035f), new Vector3(0.31f, 0.16f, 0.42f), cabello);
        CrearBloque(visual.transform, "Bota derecha", new Vector3(0.17f, -1.08f, -0.035f), new Vector3(0.31f, 0.16f, 0.42f), cabello);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    void CrearBloque(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject bloque = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bloque.name = nombre;
        bloque.transform.SetParent(padre, false);
        bloque.transform.localPosition = posicion;
        bloque.transform.localScale = escala;

        Collider colision = bloque.GetComponent<Collider>();
        if (colision != null)
        {
            colision.enabled = false;
            if (Application.isPlaying) Destroy(colision);
            else DestroyImmediate(colision);
        }

        MeshRenderer renderizador = bloque.GetComponent<MeshRenderer>();
        if (renderizador != null && material != null)
            renderizador.sharedMaterial = material;
    }
}
