using UnityEngine;

[DefaultExecutionOrder(100)]
public class CamaraSeguir : MonoBehaviour
{
    [Min(0f)] public float suavidad = 12f;
    [Range(.3f, 5f)] public float sensibilidadRaton = 2.2f;
    public bool EnPrimeraPersona => perspectiva == 0;
    public float AnguloHorizontal => anguloHorizontal;
    public Vector3 DireccionApuntado => Quaternion.Euler(anguloVertical, anguloHorizontal, 0f) * Vector3.forward * (perspectiva == 2 ? -1f : 1f);
    private script jugador;
    private Camera camara;
    private int perspectiva;
    private float anguloHorizontal;
    private float anguloVertical;
    private float ciclo;
    private float alturaOjos;
    private Rigidbody fisica;

    void Awake()
    {
        jugador = FindFirstObjectByType<script>();
        camara = GetComponent<Camera>();
        if (jugador == null) return;
        fisica = jugador.GetComponent<Rigidbody>();
        anguloHorizontal = jugador.transform.eulerAngles.y;
        alturaOjos = jugador.PosicionOjos.y - jugador.transform.position.y;
        if (camara != null) { camara.fieldOfView = 72f; camara.nearClipPlane = .04f; }
        MostrarCursorInterfaz(false);
    }

    void Update()
    {
        if (jugador == null || jugador.InterfazAbierta) return;
        if (Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown(KeyCode.V)) perspectiva = (perspectiva + 1) % 3;
        if (Cursor.lockState != CursorLockMode.Locked) return;
        anguloHorizontal += Input.GetAxisRaw("Mouse X") * sensibilidadRaton;
        anguloVertical = Mathf.Clamp(anguloVertical - Input.GetAxisRaw("Mouse Y") * sensibilidadRaton, -89f, 89f);
    }

    public void MostrarCursorInterfaz(bool mostrar)
    {
        Cursor.lockState = mostrar ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = mostrar;
    }

    public void RestablecerMirada(float horizontal, float vertical = 0f)
    {
        anguloHorizontal = horizontal;
        anguloVertical = Mathf.Clamp(vertical, -89f, 89f);
    }

    void LateUpdate()
    {
        if (jugador == null) return;
        float mezcla = 1f - Mathf.Exp(-suavidad * Time.deltaTime);
        alturaOjos = Mathf.Lerp(alturaOjos, jugador.PosicionOjos.y - jugador.transform.position.y, mezcla);
        Vector3 ojos = jugador.transform.position + Vector3.up * alturaOjos;
        Quaternion mirada = Quaternion.Euler(anguloVertical, anguloHorizontal, 0f);
        if (EnPrimeraPersona)
        {
            Vector3 balanceo = Vector3.zero;
            float rapidez = fisica != null ? new Vector2(fisica.velocity.x, fisica.velocity.z).magnitude : 0f;
            if (jugador.balanceoCamara && jugador.EstaEnSuelo && !jugador.InterfazAbierta)
            {
                ciclo += Time.deltaTime * rapidez * 2.5f / jugador.Bloque;
                float amplitud = Mathf.Clamp01(rapidez / jugador.velocidad) * jugador.Bloque * .018f;
                balanceo = mirada * new Vector3(Mathf.Sin(ciclo) * amplitud, Mathf.Abs(Mathf.Cos(ciclo)) * amplitud, 0f);
            }
            transform.SetPositionAndRotation(ojos + balanceo, mirada);
        }
        else
        {
            Vector3 direccion = mirada * Vector3.forward * (perspectiva == 1 ? -1f : 1f);
            float distancia = jugador.Bloque * 3f;
            foreach (RaycastHit golpe in Physics.SphereCastAll(ojos, jugador.Bloque * .14f, direccion,
                distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (golpe.collider.transform.IsChildOf(jugador.transform)) continue;
                distancia = Mathf.Min(distancia, Mathf.Max(.1f, golpe.distance - .08f));
            }
            transform.position = ojos + direccion * distancia;
            transform.rotation = perspectiva == 1 ? mirada : Quaternion.LookRotation(-direccion, Vector3.up);
        }
        if (camara != null) camara.fieldOfView = Mathf.Lerp(camara.fieldOfView, jugador.EstaCorriendo ? 80f : 72f, mezcla);
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
