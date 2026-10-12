using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class FlechaEnemiga : MonoBehaviour
{
    public Vector3 direccion;
    [Min(1f)] public float velocidad = 18f;

    private Rigidbody fisica;

    void Awake()
    {
        fisica = GetComponent<Rigidbody>();
        fisica.useGravity = false;
        fisica.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        fisica.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Start()
    {
        fisica.velocity = direccion.normalized * velocidad;
        Destroy(gameObject, 5f);
    }

    void OnCollisionEnter(Collision golpe)
    {
        script jugador = golpe.collider.GetComponent<script>();
        if (jugador != null) jugador.RecibirDano(1, transform.position - direccion);
        Destroy(gameObject);
    }
}
