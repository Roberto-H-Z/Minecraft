using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class TridenteProyectil : MonoBehaviour
{
    public float Velocidad = 24f;
    public int dano = 1;
    public float duracion = 5f;

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
        Destroy(parte.GetComponent<Collider>());
    }

    void Start()
    {
        GetComponent<Rigidbody>().velocity = transform.forward * Velocidad + Vector3.up * 1.5f;
        Destroy(gameObject, duracion);
    }

    void OnCollisionEnter(Collision golpe)
    {
        enemigo monstruo = golpe.collider.GetComponentInParent<enemigo>();
        if (monstruo != null) monstruo.RecibirDano(dano, transform.position - transform.forward);
        AnimalVoxel animal = golpe.collider.GetComponentInParent<AnimalVoxel>();
        if (animal != null) animal.RecibirDano(dano, transform.position - transform.forward);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (materialMango != null) Destroy(materialMango);
        if (materialMetal != null) Destroy(materialMetal);
    }
}
