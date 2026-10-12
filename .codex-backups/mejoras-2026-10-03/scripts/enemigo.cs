using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class enemigo : MonoBehaviour
{
    [Header("Persecución")]
    public Transform objetivo;
    [Min(0f)] public float distanciaDeteccion = 26f;
    [Min(0f)] public float distanciaParada = 1.4f;
    [Min(0f)] public float velocidad = 30f;
    [Min(0f)] public float velocidadRotacion = 8f;
    [Range(0.1f, 1f)] public float factorVelocidad = 0.82f;

    [Header("Vida")]
    [Min(1)] public int vida = 4;

    [Header("Explosion del Creeper")]
    public GameObject efectoExplosion;
    [Min(0.1f)] public float distanciaExplosion = 2.7f;
    [Min(0f)] public float tiempoMecha = 0.9f;
    [Min(0.1f)] public float radioExplosion = 3f;

    [Header("Aspecto")]
    public bool esZombie;
    public bool esEsqueleto;
    public Material materialVerde;
    public Material materialCara;
    [Range(0.4f, 1f)] public float factorTamano = 0.85f;

    [Header("Ataque de esqueleto")]
    [Min(0.2f)] public float intervaloFlechas = 2.2f;

    private string NombreVisual => esEsqueleto ? "VisualEsqueleto" : esZombie ? "VisualZombie" : "VisualCreeper";
    private Rigidbody rb;
    private Renderer renderizadorOriginal;
    private float tiempoBusqueda;
    private bool persiguiendo;
    private Vector3 direccionPaseo;
    private float siguienteGiroPaseo;
    private bool tamanoAjustado;
    private float siguienteSalto;
    private Transform[] articulaciones;
    private float cicloPaso;
    private bool muerto;
    private float finDestelloGolpe;
    private Renderer[] renderizadoresGolpe;
    private Material[][] materialesAntesDelGolpe;
    private Material materialGolpe;
    private Material materialPielMinecraft;
    private Vector3 impulsoGolpe;
    private float finRetroceso;
    private float finMecha = -1f;
    private float siguienteFlecha;
    private MundoVoxel mundo;

    void Awake()
    {
        if (gameObject.name.StartsWith("Esqueleto")) esEsqueleto = true;
        ConfigurarFisica();
        CrearAspecto();
    }

    void OnEnable()
    {
        ConfigurarFisica();
        CrearAspecto();
    }

    void Start()
    {
        mundo = FindObjectOfType<MundoVoxel>();
        if (objetivo == null) BuscarRodolfo();
        else AjustarTamanoAObjetivo();
        AlinearConElSuelo();
        if (Application.isPlaying)
        {
            AplicarPielMinecraft();
            PrepararAnimacion();
        }
    }

    void ConfigurarFisica()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        BoxCollider colisionador = GetComponent<BoxCollider>();
        if (colisionador != null && transform.Find(NombreVisual) == null)
        {
            colisionador.center = Vector3.zero;
            colisionador.size = new Vector3(0.8f, 2f, 0.8f);
        }
    }

    void CrearAspecto()
    {
        Transform visualExistente = transform.Find(NombreVisual);
        if (visualExistente != null)
        {
            renderizadorOriginal = GetComponent<Renderer>();
            if (renderizadorOriginal != null) renderizadorOriginal.enabled = false;
            return;
        }

        if (esEsqueleto)
        {
            CrearAspectoEsqueleto();
            return;
        }
        if (esZombie)
        {
            CrearAspectoZombie();
            return;
        }

        renderizadorOriginal = GetComponent<Renderer>();
        if (renderizadorOriginal != null)
        {
            if (materialVerde == null) materialVerde = renderizadorOriginal.sharedMaterial;
            renderizadorOriginal.enabled = false;
        }

        if (materialVerde == null)
            materialVerde = CrearMaterial(new Color(0.22f, 0.62f, 0.18f));

        if (materialCara == null)
            materialCara = Resources.Load<Material>("Creeper/creeper_face");
        if (materialCara == null)
            materialCara = CrearMaterial(new Color(0.04f, 0.16f, 0.04f));

        GameObject visual = new GameObject(NombreVisual);
        visual.transform.SetParent(transform, false);

        // Cabeza cuadrada y cuerpo verde.
        CrearBloque(visual.transform, "Cabeza", new Vector3(0f, 0.53f, 0f), new Vector3(0.8f, 0.8f, 0.8f), materialVerde);
        CrearBloque(visual.transform, "Cuerpo", new Vector3(0f, -0.28f, 0f), new Vector3(0.62f, 0.85f, 0.42f), materialVerde);

        // Cuatro patas cortas, características del Creeper.
        CrearBloque(visual.transform, "Pata delantera izquierda", new Vector3(-0.22f, -0.73f, 0.17f), new Vector3(0.22f, 0.58f, 0.22f), materialVerde);
        CrearBloque(visual.transform, "Pata delantera derecha", new Vector3(0.22f, -0.73f, 0.17f), new Vector3(0.22f, 0.58f, 0.22f), materialVerde);
        CrearBloque(visual.transform, "Pata trasera izquierda", new Vector3(-0.22f, -0.73f, -0.17f), new Vector3(0.22f, 0.58f, 0.22f), materialVerde);
        CrearBloque(visual.transform, "Pata trasera derecha", new Vector3(0.22f, -0.73f, -0.17f), new Vector3(0.22f, 0.58f, 0.22f), materialVerde);

        // Cara pixelada al frente (+Z).
        CrearBloque(visual.transform, "Ojo izquierdo", new Vector3(-0.17f, 0.63f, 0.411f), new Vector3(0.18f, 0.18f, 0.025f), materialCara);
        CrearBloque(visual.transform, "Ojo derecho", new Vector3(0.17f, 0.63f, 0.411f), new Vector3(0.18f, 0.18f, 0.025f), materialCara);
        CrearBloque(visual.transform, "Cara centro", new Vector3(0f, 0.43f, 0.411f), new Vector3(0.16f, 0.16f, 0.025f), materialCara);
        CrearBloque(visual.transform, "Cara izquierda", new Vector3(-0.14f, 0.36f, 0.411f), new Vector3(0.12f, 0.22f, 0.025f), materialCara);
        CrearBloque(visual.transform, "Cara derecha", new Vector3(0.14f, 0.36f, 0.411f), new Vector3(0.12f, 0.22f, 0.025f), materialCara);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    void CrearAspectoZombie()
    {
        renderizadorOriginal = GetComponent<Renderer>();
        if (renderizadorOriginal != null) renderizadorOriginal.enabled = false;

        Material piel = CrearMaterial(new Color(0.31f, 0.53f, 0.28f));
        Material camisa = CrearMaterial(new Color(0.12f, 0.50f, 0.56f));
        Material pantalon = CrearMaterial(new Color(0.16f, 0.22f, 0.48f));
        Material cara = CrearMaterial(new Color(0.06f, 0.14f, 0.08f));
        GameObject visual = new GameObject(NombreVisual);
        visual.transform.SetParent(transform, false);
        visual.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        CrearBloque(visual.transform, "Cabeza", new Vector3(0f, 0.69f, 0f), new Vector3(0.66f, 0.68f, 0.66f), piel);
        CrearBloque(visual.transform, "Ojo izquierdo", new Vector3(-0.14f, 0.73f, 0.342f), new Vector3(0.10f, 0.11f, 0.025f), cara);
        CrearBloque(visual.transform, "Ojo derecho", new Vector3(0.14f, 0.73f, 0.342f), new Vector3(0.10f, 0.11f, 0.025f), cara);
        CrearBloque(visual.transform, "Boca", new Vector3(0f, 0.48f, 0.342f), new Vector3(0.19f, 0.045f, 0.025f), cara);
        CrearBloque(visual.transform, "Torso", new Vector3(0f, -0.04f, 0f), new Vector3(0.64f, 0.70f, 0.38f), camisa);
        CrearBloque(visual.transform, "Brazo izquierdo", new Vector3(-0.48f, -0.08f, 0f), new Vector3(0.27f, 0.74f, 0.34f), piel);
        CrearBloque(visual.transform, "Brazo derecho", new Vector3(0.48f, -0.08f, 0f), new Vector3(0.27f, 0.74f, 0.34f), piel);
        CrearBloque(visual.transform, "Manga izquierda", new Vector3(-0.48f, 0.18f, 0f), new Vector3(0.28f, 0.23f, 0.35f), camisa);
        CrearBloque(visual.transform, "Manga derecha", new Vector3(0.48f, 0.18f, 0f), new Vector3(0.28f, 0.23f, 0.35f), camisa);
        CrearBloque(visual.transform, "Pierna izquierda", new Vector3(-0.17f, -0.79f, 0f), new Vector3(0.30f, 0.75f, 0.37f), pantalon);
        CrearBloque(visual.transform, "Pierna derecha", new Vector3(0.17f, -0.79f, 0f), new Vector3(0.30f, 0.75f, 0.37f), pantalon);
    }

    void CrearAspectoEsqueleto()
    {
        renderizadorOriginal = GetComponent<Renderer>();
        if (renderizadorOriginal != null) renderizadorOriginal.enabled = false;
        Material hueso = CrearMaterial(new Color(0.86f, 0.84f, 0.76f));
        Material sombra = CrearMaterial(new Color(0.18f, 0.19f, 0.19f));
        Material arco = CrearMaterial(new Color(0.38f, 0.23f, 0.10f));
        materialCara = arco;
        GameObject visual = new GameObject(NombreVisual);
        visual.transform.SetParent(transform, false);
        visual.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        CrearBloque(visual.transform, "Cabeza", new Vector3(0f, 0.68f, 0f),
            new Vector3(0.67f, 0.68f, 0.67f), hueso);
        CrearBloque(visual.transform, "Ojo izquierdo", new Vector3(-0.15f, 0.74f, 0.345f),
            new Vector3(0.12f, 0.13f, 0.025f), sombra);
        CrearBloque(visual.transform, "Ojo derecho", new Vector3(0.15f, 0.74f, 0.345f),
            new Vector3(0.12f, 0.13f, 0.025f), sombra);
        CrearBloque(visual.transform, "Boca", new Vector3(0f, 0.47f, 0.345f),
            new Vector3(0.25f, 0.07f, 0.025f), sombra);
        CrearBloque(visual.transform, "Torso", new Vector3(0f, -0.06f, 0f),
            new Vector3(0.54f, 0.68f, 0.28f), hueso);
        for (int i = 0; i < 3; i++)
            CrearBloque(visual.transform, "Costilla " + i,
                new Vector3(0f, 0.17f - i * 0.17f, 0.15f),
                new Vector3(0.60f, 0.055f, 0.055f), sombra);
        CrearBloque(visual.transform, "Brazo izquierdo", new Vector3(-0.42f, -0.08f, 0f),
            new Vector3(0.18f, 0.75f, 0.18f), hueso);
        CrearBloque(visual.transform, "Brazo derecho", new Vector3(0.42f, -0.08f, 0f),
            new Vector3(0.18f, 0.75f, 0.18f), hueso);
        CrearBloque(visual.transform, "Arco", new Vector3(0.55f, -0.23f, 0.28f),
            new Vector3(0.13f, 0.73f, 0.12f), arco);
        CrearBloque(visual.transform, "Pierna izquierda", new Vector3(-0.16f, -0.78f, 0f),
            new Vector3(0.18f, 0.76f, 0.18f), hueso);
        CrearBloque(visual.transform, "Pierna derecha", new Vector3(0.16f, -0.78f, 0f),
            new Vector3(0.18f, 0.76f, 0.18f), hueso);
    }

    void AplicarPielMinecraft()
    {
        string nombre = esEsqueleto ? "skeleton" : esZombie ? "zombie" : "creeper";
        materialPielMinecraft = MinecraftSkinBox.CrearMaterial(nombre);
        if (materialPielMinecraft == null) return;
        Transform visual = transform.Find(NombreVisual);
        if (visual == null) return;
        MinecraftSkinBox.Aplicar(visual.Find("Cabeza"), materialPielMinecraft,
            0, 0, 8, 8, 8);
        MinecraftSkinBox.Aplicar(visual.Find("Cuerpo"), materialPielMinecraft,
            16, 16, 8, 12, 4);
        MinecraftSkinBox.Aplicar(visual.Find("Torso"), materialPielMinecraft,
            16, 16, 8, 12, 4);
        if (esZombie || esEsqueleto)
        {
            MinecraftSkinBox.Aplicar(visual.Find("Brazo derecho"), materialPielMinecraft,
                40, 16, 4, 12, 4);
            MinecraftSkinBox.Aplicar(visual.Find("Brazo izquierdo"), materialPielMinecraft,
                40, 16, 4, 12, 4);
            MinecraftSkinBox.Aplicar(visual.Find("Pierna derecha"), materialPielMinecraft,
                0, 16, 4, 12, 4);
            MinecraftSkinBox.Aplicar(visual.Find("Pierna izquierda"), materialPielMinecraft,
                0, 16, 4, 12, 4);
            MinecraftSkinBox.Ocultar(visual.Find("Manga izquierda"));
            MinecraftSkinBox.Ocultar(visual.Find("Manga derecha"));
        }
        else
        {
            foreach (string pata in new[] { "Pata delantera izquierda", "Pata delantera derecha",
                "Pata trasera izquierda", "Pata trasera derecha" })
                MinecraftSkinBox.Aplicar(visual.Find(pata), materialPielMinecraft,
                    0, 16, 4, 6, 4);
        }
        foreach (string nombrePieza in new[] { "Ojo izquierdo", "Ojo derecho", "Boca",
            "Cara centro", "Cara izquierda", "Cara derecha" })
            MinecraftSkinBox.Ocultar(visual.Find(nombrePieza));
    }

    Material CrearMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;

        Material material = new Material(shader);
        material.color = color;
        material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return material;
    }

    void CrearBloque(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject bloque = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bloque.name = nombre;
        bloque.transform.SetParent(padre, false);
        bloque.transform.localPosition = posicion;
        bloque.transform.localScale = escala;

        Collider colisionador = bloque.GetComponent<Collider>();
        if (colisionador != null)
        {
            if (Application.isPlaying) Destroy(colisionador);
            else DestroyImmediate(colisionador);
        }

        Renderer renderizador = bloque.GetComponent<Renderer>();
        if (renderizador != null && material != null)
            renderizador.sharedMaterial = material;
    }

    void PrepararAnimacion()
    {
        Transform visual = transform.Find(NombreVisual);
        if (visual == null) return;
        articulaciones = new Transform[4];
        if (esZombie || esEsqueleto)
        {
            float brazoX = esZombie ? 0.48f : 0.42f;
            float piernaX = esZombie ? 0.17f : 0.16f;
            articulaciones[0] = CrearArticulacion(visual, "Hombro izquierdo", new Vector3(-brazoX, 0.29f, 0f), "Brazo izquierdo", "Manga izquierda");
            articulaciones[1] = CrearArticulacion(visual, "Hombro derecho", new Vector3(brazoX, 0.29f, 0f), "Brazo derecho", "Manga derecha", "Arco");
            articulaciones[2] = CrearArticulacion(visual, "Cadera izquierda", new Vector3(-piernaX, -0.41f, 0f), "Pierna izquierda");
            articulaciones[3] = CrearArticulacion(visual, "Cadera derecha", new Vector3(piernaX, -0.41f, 0f), "Pierna derecha");
        }
        else
        {
            articulaciones[0] = CrearArticulacion(visual, "Pata delantera izquierda pivote", new Vector3(-0.22f, -0.45f, 0.17f), "Pata delantera izquierda");
            articulaciones[1] = CrearArticulacion(visual, "Pata delantera derecha pivote", new Vector3(0.22f, -0.45f, 0.17f), "Pata delantera derecha");
            articulaciones[2] = CrearArticulacion(visual, "Pata trasera izquierda pivote", new Vector3(-0.22f, -0.45f, -0.17f), "Pata trasera izquierda");
            articulaciones[3] = CrearArticulacion(visual, "Pata trasera derecha pivote", new Vector3(0.22f, -0.45f, -0.17f), "Pata trasera derecha");
        }
    }

    static Transform CrearArticulacion(Transform visual, string nombre, Vector3 posicion, params string[] partes)
    {
        GameObject pivote = new GameObject(nombre);
        pivote.transform.SetParent(visual, false);
        pivote.transform.localPosition = posicion;
        foreach (string nombreParte in partes)
        {
            Transform parte = visual.Find(nombreParte);
            if (parte == null) continue;
            Vector3 posicionOriginal = parte.localPosition;
            parte.SetParent(pivote.transform, false);
            parte.localPosition = posicionOriginal - posicion;
        }
        return pivote.transform;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || rb == null || articulaciones == null) return;
        Vector3 movimiento = rb.velocity;
        movimiento.y = 0f;
        float rapidez = movimiento.magnitude;
        if (rapidez > 0.1f) cicloPaso += Time.deltaTime * Mathf.Min(15f, rapidez * 1.25f);
        float balanceo = Mathf.Sin(cicloPaso) * Mathf.Clamp01(rapidez / 5f) * 28f;
        for (int i = 0; i < articulaciones.Length; i++)
            if (articulaciones[i] != null)
            {
                float signo = i == 0 || i == 3 ? 1f : -1f;
                if (esZombie || esEsqueleto) signo = -signo;
                float angulo = esZombie && i < 2 ? -65f + balanceo * signo * 0.45f :
                    esEsqueleto && i < 2 ? -35f + balanceo * signo * 0.35f : balanceo * signo;
                articulaciones[i].localRotation = Quaternion.Slerp(articulaciones[i].localRotation, Quaternion.Euler(angulo, 0f, 0f), Time.deltaTime * 12f);
            }
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (renderizadoresGolpe != null && Time.time >= finDestelloGolpe)
            LimpiarDestelloGolpe();

        if (objetivo != null && !tamanoAjustado)
        {
            AjustarTamanoAObjetivo();
            AlinearConElSuelo();
        }

        if (objetivo == null)
        {
            tiempoBusqueda -= Time.deltaTime;
            if (tiempoBusqueda <= 0f)
            {
                BuscarRodolfo();
                tiempoBusqueda = 1f;
            }
        }
    }

    void BuscarRodolfo()
    {
        script controlador = FindObjectOfType<script>();
        if (controlador != null)
        {
            objetivo = controlador.transform;
            AjustarTamanoAObjetivo();
            return;
        }

        GameObject rodolfo = GameObject.Find("Rodolfo");
        if (rodolfo != null)
        {
            objetivo = rodolfo.transform;
            AjustarTamanoAObjetivo();
        }
    }

    void AjustarTamanoAObjetivo()
    {
        if (objetivo == null) return;

        Collider colisionadorObjetivo = objetivo.GetComponent<Collider>();
        if (colisionadorObjetivo == null) return;

        float alturaModelo = esZombie || esEsqueleto ? 2.2f : 1.95f;
        float escalaRaizY = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.y));
        float escalaVisual = colisionadorObjetivo.bounds.size.y * factorTamano / (alturaModelo * escalaRaizY);

        Transform visual = transform.Find(NombreVisual);
        if (visual != null)
        {
            visual.localScale = Vector3.one * escalaVisual;
            visual.localPosition = new Vector3(0f, 0.045f * escalaVisual, 0f);
        }

        BoxCollider colisionador = GetComponent<BoxCollider>();
        if (colisionador != null)
        {
            colisionador.center = Vector3.zero;
            colisionador.size = new Vector3(0.8f, alturaModelo, 0.8f) * escalaVisual;
        }

        tamanoAjustado = true;
    }

    void AlinearConElSuelo()
    {
        Collider cuerpo = GetComponent<Collider>();
        if (rb == null || cuerpo == null) return;

        Vector3 posicion = rb.position;
        Vector3 origen = new Vector3(posicion.x, posicion.y + 100f, posicion.z);
        RaycastHit[] impactos = Physics.RaycastAll(origen, Vector3.down, 200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        bool encontroSuelo = false;
        float alturaSuelo = float.NegativeInfinity;
        foreach (RaycastHit impacto in impactos)
        {
            if (impacto.collider == cuerpo || impacto.collider.transform.IsChildOf(transform)) continue;
            if (!(impacto.collider is MeshCollider)) continue;
            if (impacto.point.y <= alturaSuelo) continue;

            alturaSuelo = impacto.point.y;
            encontroSuelo = true;
        }

        if (!encontroSuelo) return;

        rb.position = new Vector3(posicion.x, alturaSuelo + cuerpo.bounds.extents.y + 0.02f, posicion.z);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying || muerto || rb == null || objetivo == null) return;

        Vector3 haciaObjetivo = objetivo.position - rb.position;
        haciaObjetivo.y = 0f;
        float distancia = haciaObjetivo.magnitude;

        rb.angularVelocity = Vector3.zero;

        if (!esZombie && !esEsqueleto && GestionarExplosion(distancia)) return;

        if (Time.time < finRetroceso)
        {
            rb.velocity = new Vector3(impulsoGolpe.x, rb.velocity.y, impulsoGolpe.z);
            impulsoGolpe = Vector3.MoveTowards(impulsoGolpe, Vector3.zero, Time.fixedDeltaTime * 12f);
            return;
        }

        if (!persiguiendo)
        {
            if (distancia > distanciaDeteccion)
            {
                Pasear();
                return;
            }

            persiguiendo = true;
        }

        if (distancia <= distanciaParada)
        {
            rb.velocity = new Vector3(impulsoGolpe.x, rb.velocity.y, impulsoGolpe.z);
            impulsoGolpe = Vector3.MoveTowards(impulsoGolpe, Vector3.zero, Time.fixedDeltaTime * 18f);
            if (esEsqueleto && distancia > 0.01f)
            {
                rb.MoveRotation(Quaternion.Slerp(rb.rotation,
                    Quaternion.LookRotation(haciaObjetivo), velocidadRotacion * Time.fixedDeltaTime));
                DispararFlecha();
            }
            return;
        }

        Vector3 direccion = haciaObjetivo / distancia;
        Collider cuerpo = GetComponent<Collider>();
        if (cuerpo != null && Time.time >= siguienteSalto)
        {
            bool enSuelo = SaltoAutomaticoVoxel.EnSuelo(rb, cuerpo);
            if (enSuelo && HayBloqueParaSaltar(direccion, cuerpo))
            {
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                float alturaBloque = mundo != null ? mundo.TamanoBloque : 1f;
                float fuerzaSalto = Mathf.Sqrt(2f * Physics.gravity.magnitude * (alturaBloque + 0.5f));
                rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.VelocityChange);
                siguienteSalto = Time.time + 0.75f;
            }
        }
        script controlador = objetivo.GetComponent<script>();
        float velocidadSeguimiento = (controlador != null ? controlador.VelocidadActual : velocidad) * factorVelocidad;
        Vector3 velocidadHorizontal = direccion * velocidadSeguimiento + impulsoGolpe;
        rb.velocity = new Vector3(velocidadHorizontal.x, rb.velocity.y, velocidadHorizontal.z);
        impulsoGolpe = Vector3.MoveTowards(impulsoGolpe, Vector3.zero, Time.fixedDeltaTime * 18f);

        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion, Vector3.up);
        Quaternion rotacionSuave = Quaternion.Slerp(rb.rotation, rotacionObjetivo, velocidadRotacion * Time.fixedDeltaTime);
        rb.MoveRotation(rotacionSuave);
    }

    void DispararFlecha()
    {
        if (Time.time < siguienteFlecha || objetivo == null) return;
        Collider blanco = objetivo.GetComponent<Collider>();
        Vector3 destino = blanco != null ? blanco.bounds.center : objetivo.position;
        Vector3 origen = transform.position + Vector3.up * 0.5f;
        Vector3 direccion = (destino - origen).normalized;
        RaycastHit impacto;
        if (Physics.Raycast(origen, direccion, out impacto, Vector3.Distance(origen, destino),
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && impacto.collider != blanco)
            return;
        siguienteFlecha = Time.time + intervaloFlechas;
        GameObject flecha = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flecha.name = "Flecha del esqueleto";
        flecha.transform.SetPositionAndRotation(origen + direccion * 1.2f,
            Quaternion.LookRotation(direccion));
        flecha.transform.localScale = new Vector3(0.09f, 0.09f, 0.75f);
        flecha.GetComponent<Renderer>().sharedMaterial = materialCara != null ? materialCara : materialVerde;
        FlechaEnemiga proyectil = flecha.AddComponent<FlechaEnemiga>();
        proyectil.direccion = direccion;
        Collider origenColision = GetComponent<Collider>();
        if (origenColision != null) Physics.IgnoreCollision(origenColision, flecha.GetComponent<Collider>());
    }

    void Pasear()
    {
        if (Time.time >= siguienteGiroPaseo || direccionPaseo.sqrMagnitude < 0.1f)
        {
            direccionPaseo = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            siguienteGiroPaseo = Time.time + Random.Range(2f, 5f);
        }
        Collider cuerpo = GetComponent<Collider>();
        Vector3 delante = rb.position + direccionPaseo * 1.5f;
        bool obstaculo = Physics.Raycast(rb.position, direccionPaseo, 1.4f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RaycastHit impactoSuelo;
        bool haySuelo = Physics.Raycast(delante + Vector3.up * 1.5f, Vector3.down,
            out impactoSuelo, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bool agua = mundo != null && cuerpo != null &&
            mundo.EstaEnAgua(delante - Vector3.up * cuerpo.bounds.extents.y);
        bool sobreSuelo = cuerpo != null && SaltoAutomaticoVoxel.EnSuelo(rb, cuerpo);
        bool saltoPosible = sobreSuelo &&
            Time.time >= siguienteSalto && HayBloqueParaSaltar(direccionPaseo, cuerpo);
        if (saltoPosible)
        {
            float alturaBloque = mundo != null ? mundo.TamanoBloque : 2f;
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * Mathf.Sqrt(2f * Physics.gravity.magnitude *
                (alturaBloque + 0.5f)), ForceMode.VelocityChange);
            siguienteSalto = Time.time + 0.75f;
        }
        if ((obstaculo && sobreSuelo && !saltoPosible && Time.time >= siguienteSalto) ||
            !haySuelo || agua)
        {
            siguienteGiroPaseo = 0f;
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }
        float velocidadPaseo = velocidad * factorVelocidad * 0.08f;
        Vector3 avance = direccionPaseo * velocidadPaseo;
        rb.velocity = new Vector3(avance.x, rb.velocity.y, avance.z);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation,
            Quaternion.LookRotation(direccionPaseo), velocidadRotacion * Time.fixedDeltaTime));
    }

    bool GestionarExplosion(float distancia)
    {
        if (distancia <= distanciaExplosion && finMecha < 0f)
            finMecha = Time.time + tiempoMecha;
        else if (distancia > distanciaExplosion * 1.6f)
            finMecha = -1f;

        if (finMecha < 0f || Time.time < finMecha) return false;
        Explotar();
        return true;
    }

    void Explotar()
    {
        if (muerto) return;
        muerto = true;

        if (efectoExplosion != null)
        {
            GameObject particulas = Instantiate(efectoExplosion, transform.position, efectoExplosion.transform.rotation);
            Destroy(particulas, 5f);
        }

        script jugador = objetivo != null ? objetivo.GetComponent<script>() : null;
        if (jugador != null && Vector3.Distance(transform.position, objetivo.position) <= radioExplosion)
            jugador.RecibirExplosion(transform.position);

        Destroy(gameObject);
    }

    bool HayBloqueParaSaltar(Vector3 direccion, Collider cuerpo)
    {
        return SaltoAutomaticoVoxel.HayBloque(rb, cuerpo, direccion,
            mundo != null ? mundo.TamanoBloque : 2f);
    }

    void OnCollisionStay(Collision contacto)
    {
        if (!Application.isPlaying || muerto) return;
        if (!esZombie) return;
        script jugador = contacto.collider.GetComponent<script>();
        if (jugador != null) jugador.RecibirDano(1, transform.position);
    }

    public void RecibirDano(int cantidad)
    {
        RecibirDano(cantidad, transform.position);
    }

    public void RecibirDano(int cantidad, Vector3 posicionAtacante)
    {
        if (!Application.isPlaying || muerto) return;
        vida = Mathf.Max(0, vida - Mathf.Max(1, cantidad));
        Vector3 alejamiento = rb.position - posicionAtacante;
        alejamiento.y = 0f;
        if (alejamiento.sqrMagnitude > 0.01f)
            impulsoGolpe = alejamiento.normalized * 4f;
        rb.velocity = new Vector3(impulsoGolpe.x, Mathf.Max(rb.velocity.y, 3.5f), impulsoGolpe.z);
        finRetroceso = Time.time + 0.18f;
        MostrarDestelloGolpe();
        finDestelloGolpe = Time.time + 0.35f;
        if (vida > 0)
        {
            return;
        }
        muerto = true;
        Collider colision = GetComponent<Collider>();
        if (colision != null) colision.enabled = false;
        script jugador = FindObjectOfType<script>();
        if (jugador != null)
            jugador.RecogerBotin(esEsqueleto ? TipoBotin.Hueso : esZombie ? TipoBotin.CarnePodrida : TipoBotin.Polvora);
        Destroy(gameObject, 0.35f);
    }

    void MostrarDestelloGolpe()
    {
        Transform visual = transform.Find(NombreVisual);
        if (visual == null) return;
        if (renderizadoresGolpe != null) LimpiarDestelloGolpe();
        if (materialGolpe == null)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            materialGolpe = new Material(shader) { color = new Color(1f, 0.20f, 0.20f) };
        }
        renderizadoresGolpe = visual.GetComponentsInChildren<Renderer>();
        materialesAntesDelGolpe = new Material[renderizadoresGolpe.Length][];
        for (int i = 0; i < renderizadoresGolpe.Length; i++)
        {
            Renderer pieza = renderizadoresGolpe[i];
            if (pieza == null) continue;
            materialesAntesDelGolpe[i] = pieza.sharedMaterials;
            pieza.sharedMaterial = materialGolpe;
        }
        finDestelloGolpe = Time.time + 0.35f;
    }

    void LimpiarDestelloGolpe()
    {
        for (int i = 0; i < renderizadoresGolpe.Length; i++)
            if (renderizadoresGolpe[i] != null && materialesAntesDelGolpe[i] != null)
                renderizadoresGolpe[i].sharedMaterials = materialesAntesDelGolpe[i];
        renderizadoresGolpe = null;
        materialesAntesDelGolpe = null;
    }

    void OnDestroy()
    {
        if (materialPielMinecraft != null)
        {
            if (Application.isPlaying) Destroy(materialPielMinecraft);
            else DestroyImmediate(materialPielMinecraft);
        }
        if (materialGolpe != null)
        {
            if (Application.isPlaying) Destroy(materialGolpe);
            else DestroyImmediate(materialGolpe);
        }
    }
}
