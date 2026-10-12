using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class AnimalVoxel : MonoBehaviour
{
    public bool esOveja;
    [Min(0f)] public float velocidadPaseo = 1.8f;
    [Min(1)] public int vida = 4;

    private Rigidbody fisica;
    private MundoVoxel mundo;
    private Transform[] patas;
    private Vector3 direccion;
    private float siguienteGiro;
    private float cicloPaso;
    private float finDestello;
    private float finRetroceso;
    private float siguienteSalto;
    private Renderer[] piezas;
    private Material[] materialesOriginales;
    private Material rojo;
    private Material materialPiel;
    private bool muerto;

    void Awake()
    {
        if (gameObject.name == "Oveja") esOveja = true;
        else if (gameObject.name == "Cerdo") esOveja = false;
        fisica = GetComponent<Rigidbody>();
        fisica.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        fisica.interpolation = RigidbodyInterpolation.Interpolate;
        fisica.mass = 2f;
        GetComponent<BoxCollider>().size = new Vector3(1.35f, 2f, 2.5f);
        CrearAspecto();
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        mundo = FindObjectOfType<MundoVoxel>();
        AplicarPielMinecraft();
        CambiarDireccion();
    }

    void AplicarPielMinecraft()
    {
        materialPiel = MinecraftSkinBox.CrearMaterial(esOveja ? "sheep" : "pig");
        if (materialPiel == null) return;
        Transform visual = transform.Find("Animal de bloques");
        if (visual == null) return;
        MinecraftSkinBox.Aplicar(visual.Find("Cabeza"), materialPiel, 0, 0,
            esOveja ? 6 : 8, esOveja ? 6 : 8, 8);
        if (!esOveja)
            MinecraftSkinBox.Aplicar(visual.Find("Cuerpo"), materialPiel, 28, 8, 10, 16, 8);
        for (int i = 0; i < 4; i++)
            MinecraftSkinBox.Aplicar(visual.Find("Pata " + (i + 1) + "/Pierna"),
                materialPiel, 0, 16, 4, 6, 4);
        MinecraftSkinBox.Ocultar(visual.Find("Ojo izquierdo"));
        MinecraftSkinBox.Ocultar(visual.Find("Ojo derecho"));
    }

    void CrearAspecto()
    {
        Transform anterior = transform.Find("Animal de bloques");
        if (anterior != null) return;
        Material cuerpo = CrearMaterial(esOveja ? "white_wool" : "pink_wool",
            esOveja ? Color.white : new Color(1f, 0.78f, 0.82f));
        Material hocico = CrearMaterial(esOveja ? "white_terracotta" : "pink_terracotta", Color.white);
        Material ojos = CrearMaterial(null, new Color(0.08f, 0.06f, 0.05f));
        GameObject visual = new GameObject("Animal de bloques");
        visual.transform.SetParent(transform, false);
        CrearParte(visual.transform, "Cuerpo", new Vector3(0f, 0.03f, -0.18f),
            new Vector3(1.35f, 1.12f, 1.75f), cuerpo);
        CrearParte(visual.transform, "Cabeza", new Vector3(0f, 0.35f, 0.93f),
            esOveja ? new Vector3(0.78f, 0.78f, 1.05f) : new Vector3(0.90f, 0.92f, 0.82f),
            esOveja ? cuerpo : hocico);
        CrearParte(visual.transform, "Hocico", new Vector3(0f, 0.11f, esOveja ? 1.53f : 1.39f),
            new Vector3(0.50f, 0.35f, 0.17f), hocico);
        CrearParte(visual.transform, "Ojo izquierdo", new Vector3(-0.25f, 0.53f, 1.35f),
            new Vector3(0.12f, 0.13f, 0.03f), ojos);
        CrearParte(visual.transform, "Ojo derecho", new Vector3(0.25f, 0.53f, 1.35f),
            new Vector3(0.12f, 0.13f, 0.03f), ojos);
        patas = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject pivote = new GameObject("Pata " + (i + 1));
            pivote.transform.SetParent(visual.transform, false);
            pivote.transform.localPosition = new Vector3(i % 2 == 0 ? -0.43f : 0.43f,
                -0.40f, i < 2 ? 0.46f : -0.82f);
            CrearParte(pivote.transform, "Pierna", new Vector3(0f, -0.31f, 0f),
                new Vector3(0.39f, 0.62f, 0.43f), cuerpo);
            patas[i] = pivote.transform;
        }
    }

    static Material CrearMaterial(string textura, Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(shader) { color = color };
        if (textura != null)
        {
            Texture2D imagen = Resources.Load<Texture2D>("Minecraft/Blocks/" + textura);
            if (imagen != null)
            {
                imagen.filterMode = FilterMode.Point;
                material.mainTexture = imagen;
            }
        }
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
        material.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return material;
    }

    static void CrearParte(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject parte = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parte.name = nombre;
        parte.transform.SetParent(padre, false);
        parte.transform.localPosition = posicion;
        parte.transform.localScale = escala;
        parte.GetComponent<Renderer>().sharedMaterial = material;
        Collider colision = parte.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(colision);
        else DestroyImmediate(colision);
    }

    void CambiarDireccion()
    {
        float angulo = Random.Range(0f, 360f);
        direccion = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;
        siguienteGiro = Time.time + Random.Range(2.5f, 6f);
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying || muerto || fisica == null) return;
        if (Time.time < finRetroceso) return;
        if (Time.time >= siguienteGiro) CambiarDireccion();
        Vector3 adelante = fisica.position + direccion * 1.7f;
        bool obstaculo = Physics.Raycast(fisica.position, direccion, 1.65f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RaycastHit suelo;
        bool haySuelo = Physics.Raycast(adelante + Vector3.up * 1.5f, Vector3.down,
            out suelo, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bool agua = mundo != null && mundo.EstaEnAgua(adelante - Vector3.up);
        Collider cuerpoAnimal = GetComponent<Collider>();
        bool sobreSuelo = cuerpoAnimal != null && SaltoAutomaticoVoxel.EnSuelo(fisica, cuerpoAnimal);
        bool saltoPosible = sobreSuelo &&
            Time.time >= siguienteSalto && SaltoAutomaticoVoxel.HayBloque(fisica, cuerpoAnimal,
                direccion, mundo != null ? mundo.TamanoBloque : 2f);
        if (saltoPosible)
        {
            float alturaBloque = mundo != null ? mundo.TamanoBloque : 2f;
            fisica.velocity = new Vector3(fisica.velocity.x, 0f, fisica.velocity.z);
            fisica.AddForce(Vector3.up * Mathf.Sqrt(2f * Physics.gravity.magnitude *
                (alturaBloque + 0.5f)), ForceMode.VelocityChange);
            siguienteSalto = Time.time + 0.8f;
        }
        if ((obstaculo && sobreSuelo && !saltoPosible && Time.time >= siguienteSalto) ||
            !haySuelo || agua || suelo.point.y < fisica.position.y - 2.3f)
            CambiarDireccion();
        Vector3 avance = direccion * velocidadPaseo;
        fisica.velocity = new Vector3(avance.x, fisica.velocity.y, avance.z);
        fisica.angularVelocity = Vector3.zero;
        fisica.MoveRotation(Quaternion.Slerp(fisica.rotation,
            Quaternion.LookRotation(direccion), Time.fixedDeltaTime * 5f));
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || patas == null || fisica == null) return;
        Vector3 velocidad = fisica.velocity;
        velocidad.y = 0f;
        if (velocidad.sqrMagnitude > 0.04f) cicloPaso += Time.deltaTime * 8f;
        float balanceo = Mathf.Sin(cicloPaso) * Mathf.Clamp01(velocidad.magnitude) * 24f;
        for (int i = 0; i < patas.Length; i++)
            if (patas[i] != null)
                patas[i].localRotation = Quaternion.Euler((i == 0 || i == 3 ? 1f : -1f) * balanceo, 0f, 0f);
    }

    void Update()
    {
        if (piezas == null || Time.time < finDestello) return;
        for (int i = 0; i < piezas.Length; i++)
            if (piezas[i] != null) piezas[i].sharedMaterial = materialesOriginales[i];
        piezas = null;
        materialesOriginales = null;
    }

    public void RecibirDano(int cantidad, Vector3 atacante)
    {
        if (!Application.isPlaying || muerto) return;
        vida -= Mathf.Max(1, cantidad);
        Vector3 retroceso = transform.position - atacante;
        retroceso.y = 0f;
        fisica.velocity = retroceso.sqrMagnitude > 0.01f
            ? retroceso.normalized * 3f + Vector3.up * 2.5f
            : Vector3.up * 2.5f;
        finRetroceso = Time.time + 0.18f;
        if (piezas != null)
        {
            for (int i = 0; i < piezas.Length; i++)
                if (piezas[i] != null) piezas[i].sharedMaterial = materialesOriginales[i];
        }
        if (rojo == null)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            rojo = new Material(shader) { color = new Color(1f, 0.20f, 0.20f) };
        }
        piezas = GetComponentsInChildren<Renderer>();
        materialesOriginales = new Material[piezas.Length];
        for (int i = 0; i < piezas.Length; i++)
        {
            materialesOriginales[i] = piezas[i].sharedMaterial;
            piezas[i].sharedMaterial = rojo;
        }
        finDestello = Time.time + (vida <= 0 ? 0.45f : 0.3f);
        if (vida > 0) return;
        muerto = true;
        Collider colision = GetComponent<Collider>();
        if (colision != null) colision.enabled = false;
        script jugador = FindObjectOfType<script>();
        if (jugador != null) jugador.RecogerBotin(esOveja ? TipoBotin.CorderoCrudo : TipoBotin.CerdoCrudo);
        Destroy(gameObject, 0.35f);
    }

    void OnDestroy()
    {
        if (materialPiel != null)
        {
            if (Application.isPlaying) Destroy(materialPiel);
            else DestroyImmediate(materialPiel);
        }
        if (rojo != null)
        {
            if (Application.isPlaying) Destroy(rojo);
            else DestroyImmediate(rojo);
        }
    }
}
