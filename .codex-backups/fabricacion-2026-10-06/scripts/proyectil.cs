using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class proyectil : MonoBehaviour
{
    [Min(0f)] public float velocidad = 14f;
    [Min(0.1f)] public float duracion = 5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Start()
    {
        rb.velocity = transform.forward * velocidad;
        Destroy(gameObject, duracion);
    }

    void OnCollisionEnter(Collision collision)
    {
        enemigo monstruo = collision.collider.GetComponent<enemigo>();
        if (monstruo != null) monstruo.RecibirDano(1, transform.position - transform.forward);
        AnimalVoxel animal = collision.collider.GetComponent<AnimalVoxel>();
        if (animal != null) animal.RecibirDano(1, transform.position - transform.forward);
        Destroy(gameObject);
    }
}
