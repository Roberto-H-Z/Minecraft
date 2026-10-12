using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(-100)]
public partial class script : MonoBehaviour
{
    [Header("Movimiento")]
    [Min(0f)] public float velocidad = 8f;
    [Min(1f)] public float multiplicadorCarrera = 1.6f;
    [Min(0f)] public float fuerzaSalto = 10.5f;
    [Min(0f)] public float velocidadRotacion = 12f;
    [Min(1f)] public float gravedadSubida = 2.5f;
    [Min(1f)] public float gravedadBajada = 3.2f;
    public bool saltoAutomatico = true;

    [Header("Proyectil")]
    public GameObject prefabProyectil;
    [Min(0f)] public float velocidadProyectil = 14f;
    [Min(0f)] public float distanciaSalidaProyectil = 1.8f;
    [Min(0f)] public float duracionProyectil = 5f;

    [Header("Combate")]
    [Min(1)] public int danoEspada = 1;
    [Min(0.5f)] public float alcanceEspada = 7.5f;
    [Min(0.1f)] public float intervaloEspada = 0.4f;
    [Min(0.1f)] public float tiempoCargaTridente = 0.55f;

    [Header("Construccion")]
    [Min(1f)] public float alcancePico = 10f;
    [Min(0)] public int bloquesRecogidos;
    [Min(0)] public int bloquesPiedra;
    [Min(0)] public int bloquesMadera;
    [Min(0)] public int bloquesArena;
    [Min(0)] public int bloquesAdoquin;
    [Min(0)] public int bloquesCarbon;
    [Min(0)] public int bloquesHierro;
    [Min(0)] public int bloquesDiamante;

    [Header("Vidas y reaparición")]
    public float distanciaCaida = -30f;
    [Min(1)] public int vidas = 10;
    [Min(1)] public int vidaMaxima = 10;
    [Min(0.1f)] public float intervaloRegeneracion = 5f;
    [Range(0, 20)] public int hambre = 20;
    [Min(0)] public int comida = 0;
    [Range(0, 8)] public int slotSeleccionado = 0;

    [Header("Suelo")]
    [Min(0f)] public float margenSuelo = 0.2f;

    private Rigidbody rb;
    private Collider cuerpo;
    private Vector3 puntoReaparicion;
    private Vector2 entradaMovimiento;
    private bool correr;
    private bool enSuelo;
    private bool terminado;
    private bool cargando;
    private bool inventarioAbierto;
    private float inicioCarga;
    private int frameCierreInventario = -1;
    private float proximoDano;
    private float tiempoRegeneracion;
    private float proximoSaltoAutomatico;
    private float finDestello;
    private Renderer[] renderizadoresDestello;
    private Material[][] materialesAntesDelGolpe;
    private Material materialRojo;
    private Vector3 impulsoGolpe;
    private CamaraSeguir camaraSeguir;
    private VoxelCharacterVisual aspecto;
    private float proximoAtaque;
    private float progresoPicado;
    private float tiempoComiendo;
    private float inicioCargaTridente;
    private bool cargandoTridente;
    private bool ataqueEnEsteClic;
    private RaycastHit bloqueEnPicado;
    private MundoVoxel mundo;
    private Texture2D corazonLleno;
    private Texture2D corazonVacio;
    private Texture2D polloLleno;
    private Texture2D polloVacio;
    private Texture2D polloMitad;
    private Texture2D barraObjetos;
    private Texture2D seleccionObjeto;
    private Texture2D fondoInventario;
    private readonly List<Texture2D> iconosGenerados = new List<Texture2D>();
    private Texture2D iconoEspada;
    private Texture2D iconoPico;
    private Texture2D iconoTridente;
    private Texture2D iconoComida;
    private readonly Dictionary<TipoBotin, int> botines = new Dictionary<TipoBotin, int>();
    private readonly TipoBotin?[] botinesBarra = new TipoBotin?[9];
    private readonly Texture2D[] iconosBotin = new Texture2D[5];
    private Texture2D iconoTierra;
    private Texture2D iconoPiedra;
    private Texture2D iconoMadera;
    private Texture2D iconoArena;
    private Texture2D iconoAdoquin;
    private Texture2D iconoCarbon;
    private Texture2D iconoHierro;
    private Texture2D iconoDiamante;
    private readonly TipoBloque?[] bloquesBarra = new TipoBloque?[9];
    private static readonly TipoBotin[] OrdenBotines = {
        TipoBotin.CorderoCrudo, TipoBotin.CerdoCrudo, TipoBotin.CarnePodrida,
        TipoBotin.Hueso, TipoBotin.Polvora
    };
    private static readonly TipoBloque[] OrdenBloques = {
        TipoBloque.Tierra, TipoBloque.Piedra, TipoBloque.Madera, TipoBloque.Arena,
        TipoBloque.Adoquin, TipoBloque.Carbon, TipoBloque.Hierro, TipoBloque.Diamante
    };

    public float VelocidadActual => velocidad * (correr ? multiplicadorCarrera : 1f);
    public float CargaTridente => cargandoTridente && slotSeleccionado == 2
        ? Mathf.Clamp01((Time.time - inicioCargaTridente) / Mathf.Max(0.1f, tiempoCargaTridente)) : 0f;
    public bool InterfazAbierta => cargando || terminado || inventarioAbierto || pausado;
    public bool AcabaDeCerrarInventario => Time.frameCount == frameCierreInventario;
    public bool EsBloqueEnSlot(int slot) => slot >= 3 && slot < 9 &&
        bloquesBarra[slot].HasValue && CantidadBloques(slot) > 0;
    public bool EsBotinEnSlot(int slot) => slot >= 3 && slot < 9 && botinesBarra[slot].HasValue &&
        CantidadBotin(botinesBarra[slot].Value) > 0;
    public bool EsComidaEnSlot(int slot) => EsBotinEnSlot(slot) && EsComestible(botinesBarra[slot].Value);
    public bool SobreBotonInventario(Vector3 posicionRaton)
    {
        float escala = Mathf.Max(0.65f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
        float x = posicionRaton.x / escala;
        float y = (Screen.height - posicionRaton.y) / escala;
        float barraX = Screen.width / escala * 0.5f - 182f;
        float barraY = Screen.height / escala - 58f;
        return new Rect(barraX + 373f, barraY + 2f, 43f, 40f).Contains(new Vector2(x, y));
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cuerpo = GetComponent<Collider>();
        ConfigurarMedidas();
        // Evita que la física tumbe al personaje, pero permite girarlo en Y.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Oculta la cápsula visual y asegura que el modelo cúbico exista,
        // incluso si la escena que está abierta aún no se ha recargado.
        MeshRenderer renderizadorCapsula = GetComponent<MeshRenderer>();
        if (renderizadorCapsula != null)
            renderizadorCapsula.enabled = false;

        if (GetComponent<VoxelCharacterVisual>() == null)
            gameObject.AddComponent<VoxelCharacterVisual>();
    }

    void Start()
    {
        Camera camaraPrincipal = Camera.main;
        if (camaraPrincipal != null) camaraSeguir = camaraPrincipal.GetComponent<CamaraSeguir>();
        aspecto = GetComponent<VoxelCharacterVisual>();
        mundo = FindFirstObjectByType<MundoVoxel>();
        Physics.SyncTransforms();
        AlinearConElSuelo();
        puntoReaparicion = rb.position;
        if (GetComponent<SonidosVoxel>() == null) gameObject.AddComponent<SonidosVoxel>();
        vidaMaxima = Mathf.Max(1, vidaMaxima);
        vidas = Mathf.Clamp(vidas, 1, vidaMaxima);
        comida = 0;
        botines.Clear();
        System.Array.Clear(botinesBarra, 0, botinesBarra.Length);
        System.Array.Clear(bloquesBarra, 0, bloquesBarra.Length);
        bloquesRecogidos = bloquesPiedra = bloquesMadera = bloquesArena = 0;
        bloquesAdoquin = bloquesCarbon = bloquesHierro = bloquesDiamante = 0;
        slotSeleccionado = 0;
        inicioCarga = Time.realtimeSinceStartup;
        cargando = true;
        Time.timeScale = 0f;
    }

    void AlinearConElSuelo()
    {
        if (cuerpo == null) return;

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

    void Update()
    {
        if (cargando)
        {
            if (Time.realtimeSinceStartup - inicioCarga < 2f) return;
            cargando = false;
            Time.timeScale = 1f;
        }
        if (terminado) return;

        if (ProcesarInterfaz()) return;

        if (renderizadoresDestello != null && Time.time >= finDestello)
            LimpiarDestello();

        entradaMovimiento = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        entradaMovimiento = Vector2.ClampMagnitude(entradaMovimiento, 1f);
        ActualizarSupervivencia();

        for (int i = 0; i < 9; i++)
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) slotSeleccionado = i;
        float rueda = Input.mouseScrollDelta.y;
        if (rueda != 0f) slotSeleccionado = (slotSeleccionado + (rueda > 0f ? 8 : 1)) % 9;
        ActualizarSeleccion();

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (slotSeleccionado != 2) LanzarProyectil();
        }

        bool puedeUsarRaton = camaraSeguir == null || Cursor.lockState == CursorLockMode.Locked;
        if (!puedeUsarRaton)
        {
            DetenerPicado();
            tiempoComiendo = 0f;
            cargandoTridente = false;
            return;
        }

        if (slotSeleccionado != 2) cargandoTridente = false;
        if (Input.GetMouseButtonDown(1))
        {
            if (slotSeleccionado == 2)
            {
                inicioCargaTridente = Time.time;
                cargandoTridente = true;
            }
            else if (EsBloqueEnSlot(slotSeleccionado)) UsarPico(true);
        }
        if (Input.GetMouseButtonUp(1) && cargandoTridente)
        {
            bool tiroListo = CargaTridente >= 1f;
            cargandoTridente = false;
            if (tiroListo) LanzarTridente();
        }
        else if (cargandoTridente && !Input.GetMouseButton(1)) cargandoTridente = false;

        if (Input.GetMouseButton(1) && EsComidaEnSlot(slotSeleccionado) && hambre < 20)
        {
            tiempoComiendo += Time.deltaTime;
            if (tiempoComiendo >= 1.1f)
            {
                Comer();
                tiempoComiendo = 0f;
            }
        }
        else tiempoComiendo = 0f;

        if (!Input.GetMouseButton(0)) ataqueEnEsteClic = false;
        if (Input.GetMouseButtonDown(0))
        {
            if (BuscarSerVivoApuntado(out _, out _))
            {
                AtacarConEspada();
                ataqueEnEsteClic = true;
            }
            else if (aspecto != null) aspecto.AnimarGolpe();
        }

        bool puedePicar = !EsComidaEnSlot(slotSeleccionado) || hambre >= 20;
        if (Input.GetMouseButton(0) && !ataqueEnEsteClic && puedePicar)
        {
            RaycastHit impacto;
            if (BuscarBloqueApuntado(out impacto) && mundo != null && mundo.EsBloquePicable(impacto))
                PicarMientrasSeMantiene(impacto);
            else DetenerPicado();
        }
        else DetenerPicado();

    }

    void UsarPico(bool colocar)
    {
        if (mundo == null) mundo = FindFirstObjectByType<MundoVoxel>();
        if (mundo == null) return;
        if (aspecto != null && !colocar) aspecto.AnimarGolpe();

        RaycastHit impactoCercano;
        if (!BuscarBloqueApuntado(out impactoCercano)) return;
        if (colocar)
        {
            TipoBloque tipo = TipoEnSlot(slotSeleccionado);
            if (mundo.ColocarBloque(impactoCercano, tipo))
            {
                if (tipo == TipoBloque.Tierra) bloquesRecogidos--;
                else if (tipo == TipoBloque.Piedra) bloquesPiedra--;
                else if (tipo == TipoBloque.Madera) bloquesMadera--;
                else if (tipo == TipoBloque.Arena) bloquesArena--;
                else if (tipo == TipoBloque.Adoquin) bloquesAdoquin--;
                else if (tipo == TipoBloque.Carbon) bloquesCarbon--;
                else if (tipo == TipoBloque.Hierro) bloquesHierro--;
                else bloquesDiamante--;
                if (CantidadDe(tipo) <= 0)
                {
                    bloquesBarra[slotSeleccionado] = null;
                    CompletarBarra();
                }
            }
        }
        else mundo.QuitarBloque(impactoCercano);
    }

    public void RecogerBloque(TipoBloque tipo)
    {
        if (!PuedeRecoger(tipo)) return;
        if (tipo == TipoBloque.Tierra) bloquesRecogidos++;
        else if (tipo == TipoBloque.Piedra) bloquesPiedra++;
        else if (tipo == TipoBloque.Madera) bloquesMadera++;
        else if (tipo == TipoBloque.Arena) bloquesArena++;
        else if (tipo == TipoBloque.Adoquin) bloquesAdoquin++;
        else if (tipo == TipoBloque.Carbon) bloquesCarbon++;
        else if (tipo == TipoBloque.Hierro) bloquesHierro++;
        else bloquesDiamante++;
        CompletarBarra();
    }

    public void RecogerBotin(TipoBotin tipo)
    {
        if (!PuedeRecoger(tipo)) return;
        botines[tipo] = CantidadBotin(tipo) + 1;
        if (EsComestible(tipo)) comida++;
        CompletarBarra();
    }

    int CantidadBotin(TipoBotin tipo)
    {
        int cantidad;
        return botines.TryGetValue(tipo, out cantidad) ? cantidad : 0;
    }

    static bool EsComestible(TipoBotin tipo) => tipo == TipoBotin.CorderoCrudo ||
        tipo == TipoBotin.CerdoCrudo || tipo == TipoBotin.CarnePodrida;

    Texture2D IconoBotin(TipoBotin tipo) => iconosBotin[(int)tipo];

    public Texture2D IconoBotinEnSlot(int slot) => EsBotinEnSlot(slot)
        ? IconoBotin(botinesBarra[slot].Value) : null;

    string NombreBotin(TipoBotin tipo)
    {
        switch (tipo)
        {
            case TipoBotin.CorderoCrudo: return "Cordero crudo";
            case TipoBotin.CerdoCrudo: return "Cerdo crudo";
            case TipoBotin.CarnePodrida: return "Carne podrida";
            case TipoBotin.Hueso: return "Hueso";
            default: return "Pólvora";
        }
    }

    void Comer()
    {
        if (!EsComidaEnSlot(slotSeleccionado) || hambre >= 20) return;
        TipoBotin tipo = botinesBarra[slotSeleccionado].Value;
        botines[tipo]--;
        comida--;
        hambre = Mathf.Min(20, hambre + (tipo == TipoBotin.CarnePodrida ? 2 : 6));
        saturacion = Mathf.Min(hambre, saturacion + (tipo == TipoBotin.CarnePodrida ? .4f : 3f));
        if (tipo == TipoBotin.CarnePodrida) hambrePodridaHasta = Time.time + 20f;
        SonidosVoxel.Emitir("comer", transform.position);
        if (CantidadBotin(tipo) == 0)
        {
            botinesBarra[slotSeleccionado] = null;
            CompletarBarra();
        }
    }

    public TipoBloque TipoEnSlot(int slot)
    {
        return slot >= 3 && slot < 9 && bloquesBarra[slot].HasValue
            ? bloquesBarra[slot].Value : TipoBloque.Tierra;
    }

    public int CantidadBloques(int slot)
    {
        return slot >= 3 && slot < 9 && bloquesBarra[slot].HasValue
            ? Mathf.Min(64, CantidadDe(bloquesBarra[slot].Value)) : 0;
    }

    int CantidadDe(TipoBloque tipo)
    {
        switch (tipo)
        {
            case TipoBloque.Tierra: return bloquesRecogidos;
            case TipoBloque.Piedra: return bloquesPiedra;
            case TipoBloque.Madera: return bloquesMadera;
            case TipoBloque.Arena: return bloquesArena;
            case TipoBloque.Adoquin: return bloquesAdoquin;
            case TipoBloque.Carbon: return bloquesCarbon;
            case TipoBloque.Hierro: return bloquesHierro;
            default: return bloquesDiamante;
        }
    }

    void CompletarBarra()
    {
        foreach (TipoBotin tipo in OrdenBotines)
        {
            if (CantidadBotin(tipo) <= 0 || System.Array.Exists(botinesBarra, t => t.HasValue && t.Value == tipo)) continue;
            for (int i = 3; i < 9; i++)
                if (!bloquesBarra[i].HasValue && !botinesBarra[i].HasValue)
                {
                    botinesBarra[i] = tipo;
                    break;
                }
        }
        foreach (TipoBloque tipo in OrdenBloques)
        {
            if (CantidadDe(tipo) <= 0 || System.Array.Exists(bloquesBarra, t => t.HasValue && t.Value == tipo)) continue;
            for (int i = 3; i < 9; i++)
                if (!bloquesBarra[i].HasValue && !botinesBarra[i].HasValue)
                {
                    bloquesBarra[i] = tipo;
                    break;
                }
        }
    }

    void AsignarBloque(TipoBloque tipo)
    {
        for (int i = 3; i < 9; i++)
            if (bloquesBarra[i] == tipo)
            {
                slotSeleccionado = i;
                return;
            }
        int destino = slotSeleccionado >= 3 ? slotSeleccionado : -1;
        if (destino < 0)
            for (int i = 3; i < 9; i++)
                if (!bloquesBarra[i].HasValue && !botinesBarra[i].HasValue) { destino = i; break; }
        if (destino < 0) destino = 3;
        botinesBarra[destino] = null;
        bloquesBarra[destino] = tipo;
        slotSeleccionado = destino;
    }

    void AsignarBotin(TipoBotin tipo)
    {
        if (CantidadBotin(tipo) <= 0) return;
        for (int i = 3; i < 9; i++)
            if (botinesBarra[i] == tipo) { slotSeleccionado = i; return; }
        int destino = slotSeleccionado >= 3 ? slotSeleccionado : -1;
        if (destino < 0)
            for (int i = 3; i < 9; i++)
                if (!bloquesBarra[i].HasValue && !botinesBarra[i].HasValue) { destino = i; break; }
        if (destino < 0) destino = 3;
        bloquesBarra[destino] = null;
        botinesBarra[destino] = tipo;
        slotSeleccionado = destino;
    }

    void DetenerPicado()
    {
        progresoPicado = 0f;
        bloqueEnPicado = new RaycastHit();
        if (mundo != null) mundo.OcultarGrietas();
    }

    void PicarMientrasSeMantiene(RaycastHit impacto)
    {
        if (bloqueEnPicado.collider == null || !mundo.EsMismoBloque(bloqueEnPicado, impacto))
        {
            progresoPicado = 0f;
            bloqueEnPicado = impacto;
        }
        float duracion = mundo.DuracionPicado(impacto, slotSeleccionado == 1);
        if (Time.time >= siguienteBalanceoPico)
        {
            siguienteBalanceoPico = Time.time + .24f;
            if (aspecto != null) aspecto.AnimarGolpe();
            SonidosVoxel.Emitir("picar", impacto.point);
        }
        progresoPicado += Time.deltaTime / duracion;
        mundo.MostrarGrietas(impacto, progresoPicado);
        if (progresoPicado < 1f) return;
        if (aspecto != null) aspecto.AnimarGolpe();
        mundo.QuitarBloque(impacto);
        DetenerPicado();
    }

    void CambiarInventario()
    {
        DetenerPicado();
        tiempoComiendo = 0f;
        ataqueEnEsteClic = false;
        cargandoTridente = false;
        solicitudSalto = -10f;
        inventarioAbierto = !inventarioAbierto;
        if (!inventarioAbierto) frameCierreInventario = Time.frameCount;
        if (camaraSeguir != null) camaraSeguir.MostrarCursorInterfaz(inventarioAbierto);
        entradaMovimiento = Vector2.zero;
    }

    void LateUpdate()
    {
        if (mundo == null) return;
        RaycastHit impacto = new RaycastHit();
        mundo.ActualizarContorno(!InterfazAbierta && BuscarBloqueApuntado(out impacto), impacto);
    }

    bool BuscarBloqueApuntado(out RaycastHit impactoCercano)
    {
        impactoCercano = new RaycastHit();
        Camera camara = Camera.main;
        if (camara == null) return false;
        Ray rayo = camara.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));

        float menorDistancia = float.PositiveInfinity;
        foreach (RaycastHit impacto in Physics.RaycastAll(rayo, 300f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (impacto.collider.transform.IsChildOf(transform) || impacto.distance >= menorDistancia) continue;
            impactoCercano = impacto;
            menorDistancia = impacto.distance;
        }

        return menorDistancia != float.PositiveInfinity &&
            Vector3.Distance(transform.position, impactoCercano.point) <= alcancePico;
    }

    bool BuscarSerVivoApuntado(out enemigo monstruo, out AnimalVoxel animal)
    {
        monstruo = null;
        animal = null;
        RaycastHit impacto;
        if (!BuscarBloqueApuntado(out impacto) ||
            Vector3.Distance(transform.position, impacto.point) > alcanceEspada) return false;
        monstruo = impacto.collider.GetComponentInParent<enemigo>();
        animal = impacto.collider.GetComponentInParent<AnimalVoxel>();
        return monstruo != null || animal != null;
    }

    void AtacarConEspada()
    {
        if (Time.time < proximoAtaque) return;
        proximoAtaque = Time.time + intervaloEspada;
        if (aspecto != null) aspecto.AnimarGolpe();
        SonidosVoxel.Emitir("golpe", PosicionOjos);

        enemigo monstruoApuntado;
        AnimalVoxel animalApuntado;
        if (BuscarSerVivoApuntado(out monstruoApuntado, out animalApuntado))
        {
            if (animalApuntado != null) animalApuntado.RecibirDano(danoEspada, transform.position);
            else monstruoApuntado.RecibirDano(danoEspada, transform.position);
            return;
        }

        bool primeraPersona = camaraSeguir != null && camaraSeguir.EnPrimeraPersona;
        Vector3 origen = primeraPersona ? camaraSeguir.transform.position : transform.position + Vector3.up * 0.25f;
        Vector3 direccion = camaraSeguir != null ? camaraSeguir.DireccionApuntado : transform.forward;
        enemigo elegido = null;
        AnimalVoxel animalElegido = null;
        float distanciaElegida = float.PositiveInfinity;

        foreach (enemigo monstruo in FindObjectsByType<enemigo>(FindObjectsSortMode.None))
        {
            Collider objetivo = monstruo.GetComponent<Collider>();
            if (objetivo == null) continue;
            Vector3 haciaMonstruo = objetivo.bounds.center - origen;
            float distancia = haciaMonstruo.magnitude;
            if (distancia > alcanceEspada + objetivo.bounds.extents.x || distancia < 0.01f) continue;
            if (Vector3.Dot(direccion, haciaMonstruo / distancia) < 0.35f) continue;
            RaycastHit bloqueo;
            if (Physics.Raycast(origen, haciaMonstruo / distancia, out bloqueo, distancia + 0.05f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && bloqueo.collider != objetivo) continue;
            if (distancia >= distanciaElegida) continue;
            elegido = monstruo;
            distanciaElegida = distancia;
        }

        foreach (AnimalVoxel animal in FindObjectsByType<AnimalVoxel>(FindObjectsSortMode.None))
        {
            Collider objetivo = animal.GetComponent<Collider>();
            if (objetivo == null) continue;
            Vector3 haciaAnimal = objetivo.bounds.center - origen;
            float distancia = haciaAnimal.magnitude;
            if (distancia >= distanciaElegida || distancia > alcanceEspada + objetivo.bounds.extents.x ||
                distancia < 0.01f) continue;
            if (Vector3.Dot(direccion, haciaAnimal / distancia) < 0.35f) continue;
            RaycastHit bloqueo;
            if (Physics.Raycast(origen, haciaAnimal / distancia, out bloqueo, distancia + 0.05f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && bloqueo.collider != objetivo) continue;
            animalElegido = animal;
            distanciaElegida = distancia;
        }

        if (animalElegido != null) animalElegido.RecibirDano(danoEspada, transform.position);
        else if (elegido != null) elegido.RecibirDano(danoEspada, transform.position);
    }

    void LanzarProyectil()
    {
        bool primeraPersona = camaraSeguir != null && camaraSeguir.EnPrimeraPersona;
        Vector3 direccion = camaraSeguir != null ? camaraSeguir.DireccionApuntado : transform.forward;
        Vector3 posicion = primeraPersona
            ? camaraSeguir.transform.position + direccion * 1.1f
            : transform.position + direccion * distanciaSalidaProyectil + Vector3.up * 0.2f;
        Quaternion rotacionProyectil = Quaternion.LookRotation(direccion, Vector3.up);

        GameObject objetoProyectil;
        if (prefabProyectil != null)
        {
            objetoProyectil = Instantiate(prefabProyectil, posicion, rotacionProyectil);
        }
        else
        {
            objetoProyectil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            objetoProyectil.transform.SetPositionAndRotation(posicion, rotacionProyectil);
            objetoProyectil.transform.localScale = Vector3.one * 0.4f;

            Renderer renderizador = objetoProyectil.GetComponent<Renderer>();
            if (renderizador != null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null)
                {
                    renderizador.material = new Material(shader);
                    renderizador.material.color = new Color(1f, 0.72f, 0.12f);
                }
            }
        }

        proyectil componenteProyectil = objetoProyectil.GetComponent<proyectil>();
        if (componenteProyectil == null)
            componenteProyectil = objetoProyectil.AddComponent<proyectil>();

        componenteProyectil.velocidad = velocidadProyectil;
        componenteProyectil.duracion = duracionProyectil;

        Collider colisionProyectil = objetoProyectil.GetComponent<Collider>();
        if (cuerpo != null && colisionProyectil != null)
            Physics.IgnoreCollision(cuerpo, colisionProyectil);
    }

    void LanzarTridente()
    {
        proximoAtaque = Time.time + intervaloEspada;
        if (aspecto != null) aspecto.AnimarGolpe();
        bool primeraPersona = camaraSeguir != null && camaraSeguir.EnPrimeraPersona;
        Vector3 direccion = camaraSeguir != null ? camaraSeguir.DireccionApuntado : transform.forward;
        Vector3 posicion = primeraPersona
            ? camaraSeguir.transform.position + direccion * 1.4f - Vector3.up * 0.1f
            : transform.position + direccion * 1.9f + Vector3.up * 0.3f;
        GameObject objeto = new GameObject("Tridente lanzado");
        objeto.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(direccion));
        BoxCollider colision = objeto.AddComponent<BoxCollider>();
        colision.size = new Vector3(0.42f, 0.42f, 1.35f);
        Rigidbody fisicaTridente = objeto.AddComponent<Rigidbody>();
        fisicaTridente.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        TridenteProyectil tridente = objeto.AddComponent<TridenteProyectil>();
        tridente.propietario = transform;
        tridente.Velocidad = Mathf.Max(24f, velocidadProyectil);
        SonidosVoxel.Emitir("golpe", PosicionOjos);
        if (cuerpo != null) Physics.IgnoreCollision(cuerpo, colision);
    }

    void FixedUpdate()
    {
        if (terminado || cargando || pausado) return;
        MoverConFisica();
    }

    void RevisarSuelo()
    {
        enSuelo = SaltoAutomaticoVoxel.EnSuelo(rb, cuerpo);
    }

    bool HayBloqueParaSaltar(Vector3 movimiento)
    {
        return SaltoAutomaticoVoxel.HayBloque(rb, cuerpo, movimiento,
            mundo != null ? mundo.TamanoBloque : 2f);
    }

    void PerderVidaYReaparecer()
    {
        RecibirDano(1);
        if (vidas <= 0)
            return;

        rb.position = puntoReaparicion;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void RecibirDano(int cantidad)
    {
        AplicarDano(cantidad, false, Vector3.zero);
    }

    public void RecibirDano(int cantidad, Vector3 posicionAtacante)
    {
        AplicarDano(cantidad, true, posicionAtacante);
    }

    public void RecibirExplosion(Vector3 posicionExplosion)
    {
        AplicarDano(Mathf.CeilToInt(vidaMaxima * 0.5f), true, posicionExplosion, true);
    }

    void AplicarDano(int cantidad, bool golpeMonstruo, Vector3 posicionAtacante, bool ignorarInvulnerabilidad = false)
    {
        if (terminado || (!ignorarInvulnerabilidad && Time.time < proximoDano)) return;
        proximoDano = Time.time + 1.2f;
        vidas = Mathf.Max(0, vidas - Mathf.Max(1, cantidad));
        tiempoRegeneracion = 0f;
        if (golpeMonstruo)
        {
            Vector3 alejamiento = rb.position - posicionAtacante;
            alejamiento.y = 0f;
            if (alejamiento.sqrMagnitude > 0.01f)
                impulsoGolpe = alejamiento.normalized * 4f;
        }
        rb.velocity = new Vector3(rb.velocity.x, (golpeMonstruo ? Mathf.Max(rb.velocity.y, Bloque * 1.75f) : rb.velocity.y), rb.velocity.z);
        MostrarDestello();
        SonidosVoxel.Emitir("herida", transform.position);
        if (vidas > 0) return;
        terminado = true;
        DetenerPicado();
        tiempoComiendo = 0f;
        ataqueEnEsteClic = false;
        cargandoTridente = false;
        inventarioAbierto = false;
        entradaMovimiento = Vector2.zero;
        rb.velocity = Vector3.zero;
        Time.timeScale = 0f;
        if (camaraSeguir != null) camaraSeguir.MostrarCursorInterfaz(true);
    }

    void Reaparecer()
    {
        vidas = vidaMaxima;
        hambre = 20;
        saturacion = 5f;
        aire = 10f;
        hambrePodridaHasta = 0f;
        cayendo = false;
        terminado = false;
        tiempoRegeneracion = 0f;
        impulsoGolpe = Vector3.zero;
        velocidadMovimiento = Vector3.zero;
        rb.position = puntoReaparicion;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();
        AlinearConElSuelo();
        proximoDano = Time.time + 3f;
        LimpiarDestello();
        Time.timeScale = 1f;
        if (camaraSeguir != null) camaraSeguir.MostrarCursorInterfaz(false);
    }

    void MostrarDestello()
    {
        Transform visual = transform.Find("VoxelVisual");
        if (visual == null) return;
        if (renderizadoresDestello != null) LimpiarDestello();
        if (materialRojo == null)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            materialRojo = new Material(shader);
            materialRojo.color = new Color(1f, 0.12f, 0.12f);
        }
        renderizadoresDestello = visual.GetComponentsInChildren<Renderer>();
        materialesAntesDelGolpe = new Material[renderizadoresDestello.Length][];
        for (int i = 0; i < renderizadoresDestello.Length; i++)
        {
            Renderer renderizador = renderizadoresDestello[i];
            if (renderizador == null) continue;
            materialesAntesDelGolpe[i] = renderizador.sharedMaterials;
            renderizador.sharedMaterial = materialRojo;
        }
        finDestello = Time.time + 0.35f;
    }

    void LimpiarDestello()
    {
        if (renderizadoresDestello == null) return;
        for (int i = 0; i < renderizadoresDestello.Length; i++)
            if (renderizadoresDestello[i] != null && materialesAntesDelGolpe[i] != null)
                renderizadoresDestello[i].sharedMaterials = materialesAntesDelGolpe[i];
        renderizadoresDestello = null;
        materialesAntesDelGolpe = null;
    }

    void OnGUI()
    {
        CrearIconosSiHaceFalta();
        float escala = Mathf.Max(0.65f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
        Matrix4x4 matrizAnterior = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));
        float anchoPantalla = Screen.width / escala;
        float altoPantalla = Screen.height / escala;
        float centro = anchoPantalla * 0.5f;
        float barraX = centro - 182f;
        float barraY = altoPantalla - 58f;

        if (cargando)
        {
            DibujarCarga(anchoPantalla, altoPantalla);
            GUI.matrix = matrizAnterior;
            return;
        }
        if (terminado)
        {
            DibujarMuerte(anchoPantalla, altoPantalla);
            GUI.matrix = matrizAnterior;
            return;
        }
        if (pausado)
        {
            DibujarPausa(anchoPantalla, altoPantalla);
            GUI.matrix = matrizAnterior;
            return;
        }
        if (inventarioAbierto)
        {
            DibujarInventario(anchoPantalla, altoPantalla);
            GUI.matrix = matrizAnterior;
            return;
        }

        if (camaraSeguir != null && camaraSeguir.EnPrimeraPersona && Time.time < finDestello)
            DibujarRectangulo(new Rect(0f, 0f, anchoPantalla, altoPantalla), new Color(1f, 0.07f, 0.07f, 0.18f));

        for (int i = 0; i < 10; i++)
        {
            Rect corazon = new Rect(barraX + i * 18f, barraY - 26f, 18f, 18f);
            Rect comidaIcono = new Rect(barraX + 184f + i * 18f, barraY - 26f, 18f, 18f);
            GUI.DrawTexture(corazon, corazonVacio);
            if (i < vidas) GUI.DrawTexture(corazon, corazonLleno);
            GUI.DrawTexture(comidaIcono, polloVacio);
            int hambreEnIcono = hambre - (9 - i) * 2;
            if (hambreEnIcono >= 2) GUI.DrawTexture(comidaIcono, polloLleno);
            else if (hambreEnIcono == 1) GUI.DrawTexture(comidaIcono, polloMitad);
        }

        if (barraObjetos != null)
            GUI.DrawTexture(new Rect(barraX, barraY, 364f, 44f), barraObjetos);
        for (int i = 0; i < 9; i++)
        {
            Rect casilla = new Rect(barraX + 4f + i * 40f, barraY + 2f, 40f, 40f);
            if (barraObjetos == null)
            {
                DibujarRectangulo(casilla, new Color(.1f, .1f, .1f, .9f));
                DibujarRectangulo(new Rect(casilla.x + 3f, casilla.y + 3f, 34f, 34f),
                    new Color(.28f, .28f, .28f, .94f));
            }
            if (i < 3 || EsBloqueEnSlot(i) || EsBotinEnSlot(i))
                DibujarObjeto(i, casilla);
            if (i >= 3 && CantidadBloques(i) > 0)
                DibujarTextoSombra(new Rect(casilla.x + 17f, casilla.y + 22f, 23f, 18f),
                    CantidadBloques(i).ToString(), true);
            if (EsBotinEnSlot(i))
                DibujarTextoSombra(new Rect(casilla.x + 17f, casilla.y + 22f, 23f, 18f),
                    Mathf.Min(64, CantidadBotin(botinesBarra[i].Value)).ToString(), true);
        }
        if (seleccionObjeto != null)
            GUI.DrawTexture(new Rect(barraX + slotSeleccionado * 40f, barraY - 2f, 48f, 46f),
                seleccionObjeto);
        else DibujarRectangulo(new Rect(barraX + slotSeleccionado * 40f, barraY, 44f, 44f),
            new Color(.95f, .92f, .75f, .3f));

        Rect botonInventario = new Rect(barraX + 373f, barraY + 2f, 43f, 40f);
        if (GUI.Button(botonInventario, GUIContent.none)) CambiarInventario();
        for (int fila = 0; fila < 3; fila++)
        for (int columna = 0; columna < 3; columna++)
            DibujarRectangulo(new Rect(botonInventario.x + 10f + columna * 8f,
                botonInventario.y + 7f + fila * 8f, 6f, 6f), new Color(.55f, .55f, .55f));

        GUI.color = Color.white;
        DibujarSupervivencia(anchoPantalla, altoPantalla, barraX, barraY);
        if (mostrarAyuda)
        {
            DibujarRectangulo(new Rect(5f, 5f, Mathf.Min(anchoPantalla - 10f, 900f), 64f), new Color(0, 0, 0, .65f));
            DibujarTextoSombra(new Rect(14f, 8f, 890f, 20f), "WASD mover | Shift correr | Ctrl agacharse | Espacio saltar/nadar | E / I inventario");
            DibujarTextoSombra(new Rect(14f, 28f, 890f, 20f), "Izquierdo atacar / mantener picar | Derecho colocar / mantener comer | Tridente: mantener y soltar derecho");
            DibujarTextoSombra(new Rect(14f, 48f, 890f, 20f), "1–9 / rueda seleccionar | Q soltar objeto | F5 / V cámara | F3 coordenadas | Esc pausa | F1 ocultar ayuda");
        }
        if (Time.time < mostrarNombreHasta)
        {
        if (EsBloqueEnSlot(slotSeleccionado))
            DibujarTextoSombra(new Rect(centro - 90f, barraY - 48f, 180f, 20f),
                TipoEnSlot(slotSeleccionado).ToString());
        else if (EsBotinEnSlot(slotSeleccionado))
            DibujarTextoSombra(new Rect(centro - 90f, barraY - 48f, 180f, 20f),
                NombreBotin(botinesBarra[slotSeleccionado].Value));
        else DibujarTextoSombra(new Rect(centro - 90f, barraY - 48f, 180f, 20f),
            slotSeleccionado == 0 ? "Espada de diamante" : slotSeleccionado == 1 ? "Pico de diamante" : slotSeleccionado == 2 ? "Tridente" : "");
        }
        if (camaraSeguir != null)
        {
            float medioY = altoPantalla * 0.5f;
            DibujarRectangulo(new Rect(centro - 8f, medioY - 2f, 16f, 4f), Color.black);
            DibujarRectangulo(new Rect(centro - 2f, medioY - 8f, 4f, 16f), Color.black);
            DibujarRectangulo(new Rect(centro - 6f, medioY - 1f, 12f, 2f), Color.white);
            DibujarRectangulo(new Rect(centro - 1f, medioY - 6f, 2f, 12f), Color.white);
        }
        if (cargandoTridente)
        {
            float carga = CargaTridente;
            Rect fondoCarga = new Rect(centro - 43f, altoPantalla * 0.5f + 20f, 86f, 10f);
            DibujarRectangulo(fondoCarga, new Color(0f, 0f, 0f, 0.75f));
            DibujarRectangulo(new Rect(fondoCarga.x + 2f, fondoCarga.y + 2f,
                (fondoCarga.width - 4f) * carga, fondoCarga.height - 4f),
                carga >= 1f ? new Color(0.25f, 0.95f, 0.85f) : new Color(0.3f, 0.65f, 0.75f));
        }
        GUI.matrix = matrizAnterior;
    }

    void DibujarCarga(float ancho, float alto)
    {
        float progreso = Mathf.Clamp01((Time.realtimeSinceStartup - inicioCarga) / 2f);
        DibujarRectangulo(new Rect(0f, 0f, ancho, alto), new Color(.07f, .09f, .11f));
        GUIStyle titulo = new GUIStyle(GUI.skin.label);
        titulo.alignment = TextAnchor.MiddleCenter;
        titulo.fontSize = 29;
        titulo.fontStyle = FontStyle.Bold;
        titulo.normal.textColor = Color.white;
        GUI.Label(new Rect(0f, alto * .5f - 75f, ancho, 48f), "Generando mundo...", titulo);
        float barraX = (ancho - 370f) * .5f;
        float barraY = alto * .5f + 4f;
        DibujarRectangulo(new Rect(barraX, barraY, 370f, 26f), new Color(.02f, .02f, .02f));
        DibujarRectangulo(new Rect(barraX + 3f, barraY + 3f, 364f, 20f), new Color(.31f, .31f, .31f));
        DibujarRectangulo(new Rect(barraX + 3f, barraY + 3f, 364f * progreso, 20f),
            new Color(.38f, .76f, .31f));
        DibujarTextoSombra(new Rect(barraX, barraY + 30f, 370f, 22f),
            Mathf.RoundToInt(progreso * 100f) + "%");
    }

    void DibujarMuerte(float ancho, float alto)
    {
        DibujarRectangulo(new Rect(0f, 0f, ancho, alto), new Color(.18f, .02f, .02f, .82f));
        GUIStyle titulo = new GUIStyle(GUI.skin.label);
        titulo.alignment = TextAnchor.MiddleCenter;
        titulo.fontSize = 38;
        titulo.fontStyle = FontStyle.Bold;
        titulo.normal.textColor = new Color(1f, .33f, .33f);
        GUI.Label(new Rect(0f, alto * .27f, ancho, 60f), "¡Has muerto!", titulo);
        GUIStyle subtitulo = new GUIStyle(GUI.skin.label);
        subtitulo.alignment = TextAnchor.MiddleCenter;
        subtitulo.fontSize = 16;
        subtitulo.normal.textColor = Color.white;
        GUI.Label(new Rect(0f, alto * .27f + 62f, ancho, 26f), "Rodolfo puede volver al mundo", subtitulo);
        Rect boton = new Rect((ancho - 260f) * .5f, alto * .59f, 260f, 45f);
        if (GUI.Button(boton, "Regenerar")) Reaparecer();
    }

    void DibujarInventario(float ancho, float alto)
    {
        DibujarRectangulo(new Rect(0f, 0f, ancho, alto), new Color(0f, 0f, 0f, .68f));
        float escalaPanel = Mathf.Min(2f, (ancho - 24f) / 176f, (alto - 24f) / 166f);
        Rect panel = new Rect((ancho - 176f * escalaPanel) * .5f,
            (alto - 166f * escalaPanel) * .5f, 176f * escalaPanel, 166f * escalaPanel);
        if (fondoInventario != null)
            GUI.DrawTextureWithTexCoords(panel, fondoInventario,
                new Rect(0f, 1f - 166f / 256f, 176f / 256f, 166f / 256f));
        else DibujarRectangulo(panel, new Color(.72f, .72f, .72f));

        float celda = 18f * escalaPanel;
        DibujarReservas(panel, escalaPanel);
        for (int i = 0; i < 9; i++)
        {
            Rect casilla = new Rect(panel.x + (8f + i * 18f) * escalaPanel,
                panel.y + 142f * escalaPanel, celda, celda);
            if (GUI.Button(casilla, GUIContent.none, GUIStyle.none)) slotSeleccionado = i;
            Texture2D icono = i == 0 ? iconoEspada : i == 1 ? iconoPico :
                i == 2 ? iconoTridente : EsBotinEnSlot(i) ? IconoBotin(botinesBarra[i].Value) :
                EsBloqueEnSlot(i) ? IconoBloque(TipoEnSlot(i)) : null;
            if (icono != null)
                GUI.DrawTexture(new Rect(casilla.x + 2f, casilla.y + 2f, celda - 4f, celda - 4f),
                    icono, ScaleMode.ScaleToFit, true);
            if (i >= 3 && CantidadBloques(i) > 0)
                DibujarTextoSombra(new Rect(casilla.x + 2f, casilla.y + celda - 15f, celda - 2f, 14f),
                    CantidadBloques(i).ToString(), true);
            if (EsBotinEnSlot(i))
                DibujarTextoSombra(new Rect(casilla.x + 2f, casilla.y + celda - 15f, celda - 2f, 14f),
                    Mathf.Min(64, CantidadBotin(botinesBarra[i].Value)).ToString(), true);
            if (i == slotSeleccionado)
                DibujarRectangulo(new Rect(casilla.x, casilla.y, celda, 2f), Color.white);
        }
        // Vista sencilla de Rodolfo en el recuadro del inventario.
        DibujarRectangulo(new Rect(panel.x + 46f * escalaPanel, panel.y + 18f * escalaPanel,
            13f * escalaPanel, 14f * escalaPanel), new Color(.45f, .25f, .15f));
        DibujarRectangulo(new Rect(panel.x + 43f * escalaPanel, panel.y + 33f * escalaPanel,
            19f * escalaPanel, 20f * escalaPanel), new Color(.12f, .46f, .69f));
        DibujarRectangulo(new Rect(panel.x + 46f * escalaPanel, panel.y + 54f * escalaPanel,
            6f * escalaPanel, 20f * escalaPanel), new Color(.19f, .19f, .54f));
        DibujarRectangulo(new Rect(panel.x + 54f * escalaPanel, panel.y + 54f * escalaPanel,
            6f * escalaPanel, 20f * escalaPanel), new Color(.19f, .19f, .54f));
        if (GUI.Button(new Rect(panel.x + panel.width - 24f, panel.y + 3f, 20f, 20f), "X"))
            CambiarInventario();
        DibujarTextoSombra(new Rect(panel.x, panel.y - 25f, panel.width, 20f),
            "Inventario — selecciona un material para la barra (I para cerrar)");
    }

    static void DibujarTextoSombra(Rect zona, string contenido, bool alinearDerecha = false)
    {
        GUIStyle estilo = new GUIStyle(GUI.skin.label);
        estilo.fontSize = 12;
        estilo.fontStyle = FontStyle.Bold;
        estilo.alignment = alinearDerecha ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        estilo.normal.textColor = Color.black;
        GUI.Label(new Rect(zona.x + 1f, zona.y + 1f, zona.width, zona.height), contenido, estilo);
        estilo.normal.textColor = Color.white;
        GUI.Label(zona, contenido, estilo);
    }

    static void DibujarRectangulo(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DibujarObjeto(int tipo, Rect casilla)
    {
        float x = casilla.x + 9f;
        float y = casilla.y + 9f;
        Texture2D imagen = tipo == 0 ? iconoEspada : tipo == 1 ? iconoPico :
            tipo == 2 ? iconoTridente :
            EsBotinEnSlot(tipo) ? IconoBotin(botinesBarra[tipo].Value) :
            tipo >= 3 ? IconoBloque(TipoEnSlot(tipo)) : null;
        if (imagen != null)
        {
            GUI.DrawTexture(new Rect(x - 1f, y + 1f, 24f, 24f), imagen, ScaleMode.ScaleToFit, true);
            return;
        }
        if (tipo == 0)
        {
            DibujarRectangulo(new Rect(x + 4f, y + 2f, 14f, 16f), new Color(1f, .68f, .09f));
            DibujarRectangulo(new Rect(x + 1f, y + 6f, 20f, 8f), new Color(1f, .78f, .18f));
        }
        else if (tipo == 1)
        {
            DibujarRectangulo(new Rect(x, y + 5f, 22f, 17f), new Color(.42f, .27f, .13f));
            DibujarRectangulo(new Rect(x, y + 2f, 22f, 7f), new Color(.30f, .65f, .16f));
        }
        else if (tipo == 2)
        {
            DibujarRectangulo(new Rect(x + 2f, y + 2f, 18f, 20f), new Color(.50f, .30f, .13f));
            DibujarRectangulo(new Rect(x + 8f, y + 2f, 3f, 20f), new Color(.34f, .20f, .10f));
        }
        else if (tipo == 3)
        {
            DibujarRectangulo(new Rect(x + 3f, y + 5f, 16f, 15f), new Color(.81f, .12f, .10f));
            DibujarRectangulo(new Rect(x + 9f, y + 2f, 5f, 5f), new Color(.23f, .54f, .14f));
        }
        else if (tipo == 4)
        {
            DibujarRectangulo(new Rect(x + 9f, y + 15f, 4f, 8f), new Color(.48f, .30f, .13f));
            DibujarRectangulo(new Rect(x + 5f, y + 13f, 12f, 3f), new Color(.70f, .76f, .80f));
            DibujarRectangulo(new Rect(x + 9f, y + 2f, 4f, 12f), new Color(.84f, .89f, .91f));
        }
        else if (tipo == 5)
        {
            DibujarRectangulo(new Rect(x + 9f, y + 9f, 4f, 15f), new Color(.42f, .28f, .15f));
            DibujarRectangulo(new Rect(x + 2f, y + 4f, 20f, 5f), new Color(.06f, .24f, .25f));
            DibujarRectangulo(new Rect(x + 4f, y + 5f, 16f, 3f), new Color(.13f, .82f, .84f));
            DibujarRectangulo(new Rect(x + 18f, y + 7f, 4f, 8f), new Color(.09f, .62f, .67f));
        }
        else if (tipo == 6)
        {
            DibujarRectangulo(new Rect(x - 1f, y + 1f, 25f, 24f), new Color(.39f, .25f, .12f));
            DibujarRectangulo(new Rect(x - 1f, y + 1f, 25f, 6f), new Color(.26f, .63f, .15f));
        }
        else if (tipo == 7)
        {
            DibujarRectangulo(new Rect(x - 1f, y + 1f, 25f, 24f), new Color(.38f, .38f, .38f));
            DibujarRectangulo(new Rect(x + 2f, y + 3f, 8f, 6f), new Color(.61f, .61f, .61f));
            DibujarRectangulo(new Rect(x + 13f, y + 10f, 9f, 7f), new Color(.55f, .55f, .55f));
            DibujarRectangulo(new Rect(x + 4f, y + 17f, 8f, 6f), new Color(.28f, .28f, .28f));
        }
        else if (tipo == 8)
        {
            DibujarRectangulo(new Rect(x - 1f, y + 1f, 25f, 24f), new Color(.54f, .37f, .18f));
            DibujarRectangulo(new Rect(x - 1f, y + 7f, 25f, 3f), new Color(.29f, .18f, .08f));
            DibujarRectangulo(new Rect(x - 1f, y + 15f, 25f, 3f), new Color(.31f, .20f, .09f));
            DibujarRectangulo(new Rect(x + 12f, y + 1f, 2f, 6f), new Color(.35f, .22f, .10f));
        }
    }

    Texture2D IconoBloque(TipoBloque tipo)
    {
        switch (tipo)
        {
            case TipoBloque.Arena: return iconoArena;
            case TipoBloque.Piedra: return iconoPiedra;
            case TipoBloque.Adoquin: return iconoAdoquin;
            case TipoBloque.Carbon: return iconoCarbon;
            case TipoBloque.Hierro: return iconoHierro;
            case TipoBloque.Diamante: return iconoDiamante;
            case TipoBloque.Madera: return iconoMadera;
            default: return iconoTierra;
        }
    }

    void CrearIconosSiHaceFalta()
    {
        if (corazonLleno != null) return;
        string[] corazon = { ".RR.RR.", "RRRRRRR", "RRRRRRR", "RRRRRRR", ".RRRRR.", "..RRR..", "...R..." };
        string[] pollo = { "..MMM..", ".MMMMM.", "MMMMMMM", "MMMMMM.", ".MMMM..", "..MB...", "...BB..", "....B.." };
        corazonLleno = CargarHud("heart_full", corazon,
            new Color(.88f, .10f, .13f), new Color(.22f, .04f, .05f));
        corazonVacio = CargarHud("heart_container", corazon,
            new Color(.23f, .20f, .20f), new Color(.12f, .10f, .10f));
        polloLleno = CargarHud("food_full", pollo,
            new Color(.76f, .38f, .12f), new Color(.96f, .91f, .76f));
        polloMitad = CargarHud("food_half", pollo,
            new Color(.76f, .38f, .12f), new Color(.96f, .91f, .76f));
        polloVacio = CargarHud("food_empty", pollo,
            new Color(.28f, .25f, .22f), new Color(.50f, .48f, .45f));
        barraObjetos = Resources.Load<Texture2D>("Minecraft/Hud/hotbar");
        seleccionObjeto = Resources.Load<Texture2D>("Minecraft/Hud/hotbar_selection");
        fondoInventario = Resources.Load<Texture2D>("Minecraft/Hud/inventory");
        iconoEspada = Resources.Load<Texture2D>("Minecraft/Items/diamond_sword");
        iconoPico = Resources.Load<Texture2D>("Minecraft/Items/diamond_pickaxe");
        iconoTridente = Resources.Load<Texture2D>("Minecraft/Items/trident");
        iconoComida = Resources.Load<Texture2D>("Minecraft/Items/apple");
        iconosBotin[(int)TipoBotin.CorderoCrudo] = Resources.Load<Texture2D>("Minecraft/Items/mutton");
        iconosBotin[(int)TipoBotin.CerdoCrudo] = Resources.Load<Texture2D>("Minecraft/Items/porkchop");
        iconosBotin[(int)TipoBotin.CarnePodrida] = Resources.Load<Texture2D>("Minecraft/Items/rotten_flesh");
        iconosBotin[(int)TipoBotin.Hueso] = Resources.Load<Texture2D>("Minecraft/Items/bone");
        iconosBotin[(int)TipoBotin.Polvora] = Resources.Load<Texture2D>("Minecraft/Items/gunpowder");
        iconoTierra = Resources.Load<Texture2D>("Minecraft/Blocks/grass_block_side");
        iconoPiedra = Resources.Load<Texture2D>("Minecraft/Blocks/stone");
        iconoMadera = Resources.Load<Texture2D>("Minecraft/Blocks/oak_planks");
        iconoArena = Resources.Load<Texture2D>("Minecraft/Blocks/sand");
        iconoAdoquin = Resources.Load<Texture2D>("Minecraft/Blocks/cobblestone");
        iconoCarbon = Resources.Load<Texture2D>("Minecraft/Blocks/coal_ore");
        iconoHierro = Resources.Load<Texture2D>("Minecraft/Blocks/iron_ore");
        iconoDiamante = Resources.Load<Texture2D>("Minecraft/Blocks/diamond_ore");
        foreach (Texture2D imagen in new[] { iconoEspada, iconoPico, iconoTridente, iconoComida, iconoTierra, iconoPiedra,
            iconoMadera, iconoArena, iconoAdoquin, iconoCarbon, iconoHierro, iconoDiamante,
            barraObjetos, seleccionObjeto, fondoInventario })
            if (imagen != null) imagen.filterMode = FilterMode.Point;
        foreach (Texture2D imagen in iconosBotin)
            if (imagen != null) imagen.filterMode = FilterMode.Point;
    }

    Texture2D CargarHud(string nombre, string[] dibujo, Color principal, Color secundario)
    {
        Texture2D imagen = Resources.Load<Texture2D>("Minecraft/Hud/" + nombre);
        if (imagen != null)
        {
            imagen.filterMode = FilterMode.Point;
            return imagen;
        }
        imagen = CrearIcono(dibujo, principal, secundario);
        iconosGenerados.Add(imagen);
        return imagen;
    }

    static Texture2D CrearIcono(string[] filas, Color colorPrincipal, Color colorSecundario)
    {
        Texture2D textura = new Texture2D(filas[0].Length, filas.Length, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        for (int y = 0; y < filas.Length; y++)
        for (int x = 0; x < filas[y].Length; x++)
        {
            char pixel = filas[filas.Length - 1 - y][x];
            textura.SetPixel(x, y, pixel == '.' ? Color.clear : pixel == 'B' ? colorSecundario : colorPrincipal);
        }
        textura.Apply();
        return textura;
    }

    void OnDestroy()
    {
        if (Application.isPlaying) Time.timeScale = 1f;
        if (materialMovimiento != null) Destroy(materialMovimiento);
        foreach (Texture2D imagen in iconosGenerados)
            if (imagen != null) Destroy(imagen);
        iconosGenerados.Clear();
        if (materialRojo != null) Destroy(materialRojo);
    }
}
