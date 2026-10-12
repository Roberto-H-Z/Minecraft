using UnityEngine;

public class CamaraSeguir : MonoBehaviour
{
    [Min(0f)] public float suavidad = 8f;
    [Min(0f)] public float sensibilidadRaton = 2.2f;
    public bool EnPrimeraPersona { get; private set; }
    public float AnguloHorizontal => anguloHorizontal;

    private Transform objetivo;
    private script jugador;
    private Vector3 desplazamiento;
    private Quaternion rotacionTerceraPersona;
    private Camera camara;
    private float campoVisionOriginal;
    private float planoCercanoOriginal;
    private float anguloHorizontal;
    private float anguloVertical;

    void Awake()
    {
        jugador = FindObjectOfType<script>();
        if (jugador == null) return;
        objetivo = jugador.transform;
        desplazamiento = transform.position - objetivo.position;
        rotacionTerceraPersona = transform.rotation;
        camara = GetComponent<Camera>();
        if (camara != null)
        {
            campoVisionOriginal = camara.fieldOfView;
            planoCercanoOriginal = camara.nearClipPlane;
        }
        CambiarPerspectiva();
    }

    void Update()
    {
        if (objetivo == null) return;
        if (jugador != null && jugador.InterfazAbierta) return;
        if (Input.GetKeyDown(KeyCode.F5) || Input.GetKeyDown(KeyCode.V))
            CambiarPerspectiva();

        if (!EnPrimeraPersona) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (jugador != null && jugador.AcabaDeCerrarInventario) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked &&
            (jugador == null || !jugador.SobreBotonInventario(Input.mousePosition)))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            anguloHorizontal += Input.GetAxisRaw("Mouse X") * sensibilidadRaton;
            anguloVertical = Mathf.Clamp(anguloVertical - Input.GetAxisRaw("Mouse Y") * sensibilidadRaton, -80f, 80f);
        }
    }

    void CambiarPerspectiva()
    {
        EnPrimeraPersona = !EnPrimeraPersona;
        if (EnPrimeraPersona)
        {
            anguloHorizontal = objetivo.eulerAngles.y;
            anguloVertical = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (camara != null)
            {
                camara.fieldOfView = 72f;
                camara.nearClipPlane = 0.05f;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            transform.position = objetivo.position + desplazamiento;
            transform.rotation = rotacionTerceraPersona;
            if (camara != null)
            {
                camara.fieldOfView = campoVisionOriginal;
                camara.nearClipPlane = planoCercanoOriginal;
            }
        }
    }

    public void MostrarCursorInterfaz(bool mostrar)
    {
        Cursor.lockState = mostrar || !EnPrimeraPersona ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = mostrar || !EnPrimeraPersona;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;
        if (EnPrimeraPersona)
        {
            transform.position = objetivo.TransformPoint(new Vector3(0f, 0.84f, 0.42f));
            transform.rotation = Quaternion.Euler(anguloVertical, anguloHorizontal, 0f);
            return;
        }
        Vector3 destino = objetivo.position + desplazamiento;
        transform.position = Vector3.Lerp(transform.position, destino, 1f - Mathf.Exp(-suavidad * Time.deltaTime));
    }

    void OnDisable()
    {
        if (!EnPrimeraPersona) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
