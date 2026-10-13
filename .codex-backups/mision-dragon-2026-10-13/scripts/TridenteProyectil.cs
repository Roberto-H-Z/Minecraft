using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class TridenteProyectil : MonoBehaviour
{
    public float Velocidad = 24f;
    public int dano = 1;
    public float duracion = 5f;
    public Transform propietario;
    private Rigidbody fisica;
    private Collider cuerpo;
    private Vector3 ultimaPosicion;
    private bool clavado;
    private bool regresando;
    private float volverEn;

    private Material materialMango;
    private Material materialMetal;

    void Awake()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        materialMango = new Material(shader) { color = new Color(0.16f, 0.30f, 0.31f) };
        materialMetal = new Material(shader) { color = new Color(0.12f, 0.79f, 0.75f) };
        CrearParte("Mango", new Vector3(0f, 0f, -0.20f), new Vector3(0.08f, 0.08f, 1.15f), materialMango);
        CrearParte("Base", new Vector3(0f, 0f, 0.40f), new Vector3(0.42f, 0.09f, 0.09f), materialMetal);
        CrearParte("Punta central", new Vector3(0f, 0f, 0.64f), new Vector3(0.08f, 0.08f, 0.46f), materialMetal);
        CrearParte("Punta izquierda", new Vector3(-0.17f, 0f, 0.58f), new Vector3(0.08f, 0.08f, 0.32f), materialMetal);
        CrearParte("Punta derecha", new Vector3(0.17f, 0f, 0.58f), new Vector3(0.08f, 0.08f, 0.32f), materialMetal);
    }

    void CrearParte(string nombre, Vector3 posicion, Vector3 tamano, Material material)
    {
        GameObject parte = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parte.name = nombre;
        parte.transform.SetParent(transform, false);
        parte.transform.localPosition = posicion;
        parte.transform.localScale = tamano;
        parte.GetComponent<Renderer>().sharedMaterial = material;
        parte.GetComponent<Collider>().enabled = false;
        Destroy(parte.GetComponent<Collider>());
    }

    void Start()
    {
        fisica = GetComponent<Rigidbody>();
        cuerpo = GetComponent<Collider>();
        fisica.constraints = RigidbodyConstraints.FreezeRotation;
        fisica.velocity = transform.forward * Velocidad + Vector3.up * 1.5f;
        ultimaPosicion = transform.position;
        volverEn = Time.time + duracion;
        Destroy(gameObject, 15f);
    }

    void OnCollisionEnter(Collision golpe)
    {
        Impactar(golpe.collider);
    }

    void Impactar(Collider golpe)
    {
        if (clavado || regresando || (propietario != null && golpe.transform.IsChildOf(propietario))) return;
        enemigo monstruo = golpe.GetComponentInParent<enemigo>();
        if (monstruo != null) monstruo.RecibirDano(dano, transform.position - transform.forward);
        AnimalVoxel animal = golpe.GetComponentInParent<AnimalVoxel>();
        if (animal != null) animal.RecibirDano(dano, transform.position - transform.forward);
        clavado = true;
        fisica.velocity = Vector3.zero;
        fisica.isKinematic = true;
        cuerpo.enabled = false;
        volverEn = Time.time + .65f;
        SonidosVoxel.Emitir("picar", transform.position);
    }

    void FixedUpdate()
    {
        if (fisica == null || clavado || regresando) return;
        Vector3 trayecto = fisica.position - ultimaPosicion;
        float distancia = trayecto.magnitude;
        float menor = float.PositiveInfinity;
        Collider elegido = null;
        Vector3 punto = Vector3.zero;
        if (distancia > .001f)
            foreach (RaycastHit impacto in Physics.SphereCastAll(ultimaPosicion, .08f, trayecto / distancia,
                distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (impacto.collider.transform.IsChildOf(transform) ||
                    propietario != null && impacto.collider.transform.IsChildOf(propietario) || impacto.distance >= menor) continue;
                menor = impacto.distance; elegido = impacto.collider; punto = impacto.point;
            }
        if (elegido != null) { fisica.position = punto; Impactar(elegido); }
        else if (fisica.velocity.sqrMagnitude > .01f)
            fisica.MoveRotation(Quaternion.LookRotation(fisica.velocity));
        ultimaPosicion = fisica.position;
    }

    void Update()
    {
        if (fisica == null || cuerpo == null) return;
        if (Time.time < volverEn) return;
        if (propietario == null) { Destroy(gameObject); return; }
        regresando = true;
        cuerpo.enabled = false;
        fisica.isKinematic = true;
        Vector3 destino = propietario.position + Vector3.up * .4f;
        transform.position = Vector3.MoveTowards(transform.position, destino, Time.deltaTime * 32f);
        if (Vector3.Distance(transform.position, destino) < .7f) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (materialMango != null) Destroy(materialMango);
        if (materialMetal != null) Destroy(materialMetal);
    }
}
