using UnityEngine;

// Las medidas se expresan en bloques; cambiar el tamaño del mundo no deforma al jugador.
public partial class script
{
    [Header("Supervivencia y comodidad")]
    public bool balanceoCamara = true;
    [Range(0f, 1f)] public float volumenEfectos = 0.45f;
    private CapsuleCollider capsula;
    private PhysicMaterial materialMovimiento;
    private bool agachado;
    private bool pausado;
    private bool mostrarAyuda;
    private bool mostrarDatos;
    private float ultimoSuelo = -10f;
    private float solicitudSalto = -10f;
    private float cumbreCaida;
    private bool cayendo;
    private float aire = 10f;
    private float tiempoAhogo;
    private float agotamiento;
    private float saturacion = 5f;
    private float hambrePodridaHasta;
    private float tiempoInanicion;
    private float siguienteBalanceoPico;
    private Vector3 velocidadMovimiento;
    private int ultimoSlot = -1;
    private float mostrarNombreHasta;
    private int experiencia;
    private string aviso = "";
    private float avisoHasta;

    public float Bloque => mundo != null ? mundo.TamanoBloque : 2f;
    public float AlturaDePie => Bloque * 2f;
    public bool Agachado => agachado;
    public bool EstaEnSuelo => enSuelo;
    public bool EstaCorriendo => correr && entradaMovimiento.sqrMagnitude > 0.01f;
    public Vector3 PosicionOjos => transform.position + Vector3.up *
        (-AlturaDePie * 0.5f + (agachado ? 1.28f : 1.78f) * Bloque);
    public float MovimientoMano => tiempoComiendo > 0f ? Mathf.Sin(tiempoComiendo * 20f) * .065f : 0f;

    void ConfigurarMedidas()
    {
        mundo = FindFirstObjectByType<MundoVoxel>();
        transform.localScale = Vector3.one;
        capsula = GetComponent<CapsuleCollider>();
        if (capsula == null) capsula = gameObject.AddComponent<CapsuleCollider>();
        cuerpo = capsula;
        capsula.direction = 1;
        capsula.height = AlturaDePie;
        capsula.radius = Bloque * .30f;
        capsula.center = Vector3.zero;
        materialMovimiento = new PhysicMaterial("Movimiento sin atascarse en paredes")
        {
            dynamicFriction = 0f, staticFriction = 0f, bounciness = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine = PhysicMaterialCombine.Minimum
        };
        capsula.sharedMaterial = materialMovimiento;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.mass = 70f;
        velocidad = Bloque * 4.3f;
        multiplicadorCarrera = 1.30f;
        gravedadSubida = gravedadBajada = 3.5f;
        fuerzaSalto = Mathf.Sqrt(2f * Physics.gravity.magnitude * gravedadSubida * Bloque * 1.18f);
        distanciaCaida = Mathf.Min(distanciaCaida, -30f);
        cumbreCaida = rb.position.y;
    }

    bool ProcesarInterfaz()
    {
        if (Input.GetKeyDown(KeyCode.F1)) mostrarAyuda = !mostrarAyuda;
        if (Input.GetKeyDown(KeyCode.F3)) mostrarDatos = !mostrarDatos;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inventarioAbierto) CambiarInventario();
            else CambiarPausa();
        }
        if (pausado) return true;
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.E)) CambiarInventario();
        if (inventarioAbierto) return true;

        if (Input.GetKeyDown(KeyCode.Q)) SoltarSeleccionado();
        return false;
    }

    private IdObjetoVoxel ultimoObjetoSeleccionado;
    void ActualizarSeleccion()
    {
        if (ultimoSlot != slotSeleccionado || ultimoObjetoSeleccionado != PilaEnSlot(slotSeleccionado).id)
        {
            ultimoSlot = slotSeleccionado;
            ultimoObjetoSeleccionado = PilaEnSlot(slotSeleccionado).id;
            mostrarNombreHasta = Time.time + 2.3f;
            DetenerPicado();
            tiempoComiendo = 0f;
            cargandoTridente = false;
        }
    }

    void CambiarPausa()
    {
        pausado = !pausado;
        DetenerPicado();
        tiempoComiendo = 0f;
        cargandoTridente = false;
        entradaMovimiento = Vector2.zero;
        solicitudSalto = -10f;
        Time.timeScale = pausado ? 0f : 1f;
        if (camaraSeguir != null) camaraSeguir.MostrarCursorInterfaz(pausado);
    }

    void ActualizarPostura()
    {
        bool deseaAgacharse = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (!deseaAgacharse && agachado)
        {
            // Revisa sólo el espacio que se gana al levantarse, ignorando el propio cuerpo.
            Vector3 centro = rb.position + Vector3.up * (Bloque * .73f);
            foreach (Collider obstaculo in Physics.OverlapBox(centro,
                new Vector3(capsula.radius * .95f, Bloque * .24f, capsula.radius * .95f),
                Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!obstaculo.transform.IsChildOf(transform)) { deseaAgacharse = true; break; }
        }
        agachado = deseaAgacharse;
        capsula.height = (agachado ? 1.5f : 2f) * Bloque;
        capsula.center = Vector3.down * (agachado ? Bloque * .25f : 0f);
    }

    void ActualizarSupervivencia()
    {
        ActualizarPostura();
        correr = !agachado && hambre > 6 &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        if (entradaMovimiento.sqrMagnitude > .01f)
            agotamiento += Time.deltaTime * (correr ? .5f : .09f);
        if (Time.time < hambrePodridaHasta) agotamiento += Time.deltaTime * .4f;
        while (agotamiento >= 4f)
        {
            agotamiento -= 4f;
            if (saturacion > 0f) saturacion = Mathf.Max(0f, saturacion - 1f);
            else hambre = Mathf.Max(0, hambre - 1);
        }
        if (vidas < vidaMaxima && hambre >= 18)
        {
            tiempoRegeneracion += Time.deltaTime;
            if (tiempoRegeneracion >= intervaloRegeneracion)
            {
                vidas++;
                tiempoRegeneracion = 0f;
                agotamiento += 2f;
            }
        }
        else tiempoRegeneracion = 0f;
        if (hambre == 0 && vidas > 1)
        {
            tiempoInanicion += Time.deltaTime;
            if (tiempoInanicion >= 4f) { RecibirDano(1); tiempoInanicion = 0f; }
        }
        else tiempoInanicion = 0f;

        bool sumergido = mundo != null && mundo.EstaEnAgua(PosicionOjos);
        aire = Mathf.Clamp(aire + Time.deltaTime * (sumergido ? -1f : 3f), 0f, 10f);
        if (aire <= 0f)
        {
            tiempoAhogo += Time.deltaTime;
            if (tiempoAhogo >= 2f) { RecibirDano(1); tiempoAhogo = 0f; }
        }
        else tiempoAhogo = 0f;
        if (Input.GetKeyDown(KeyCode.Space)) solicitudSalto = Time.time;
    }

    void MoverConFisica()
    {
        bool estabaEnSuelo = enSuelo;
        RevisarSuelo();
        bool agua = mundo != null && mundo.EstaEnAgua(cuerpo.bounds.min + Vector3.up * .15f);
        if (enSuelo && rb.velocity.y <= .1f) ultimoSuelo = Time.time;
        if (agua) cayendo = false;
        else if (!enSuelo)
        {
            if (!cayendo) { cayendo = true; cumbreCaida = rb.position.y; }
            cumbreCaida = Mathf.Max(cumbreCaida, rb.position.y);
        }
        else if (cayendo && !estabaEnSuelo)
        {
            float bloquesCaidos = (cumbreCaida - rb.position.y) / Bloque;
            cayendo = false;
            if (bloquesCaidos > 3.2f) RecibirDano(Mathf.CeilToInt((bloquesCaidos - 3f) * .5f));
        }
        else if (enSuelo) cayendo = false;

        Vector3 movimiento = new Vector3(entradaMovimiento.x, 0f, entradaMovimiento.y);
        if (camaraSeguir != null)
            movimiento = Quaternion.Euler(0f, camaraSeguir.AnguloHorizontal, 0f) * movimiento;
        float rapidez = VelocidadActual * (agachado ? .30f : 1f) * (agua ? .52f : 1f);
        Vector3 deseada = Time.time < finControlExplosion ? Vector3.zero : movimiento * rapidez;
        velocidadMovimiento = Vector3.MoveTowards(velocidadMovimiento, deseada,
            Bloque * (agua ? 9f : enSuelo ? 32f : 15f) * Time.fixedDeltaTime);
        Vector3 actual = velocidadMovimiento;
        if (agachado && enSuelo && !agua && impulsoGolpe.sqrMagnitude < .01f && impulsoExplosion.sqrMagnitude < .01f)
        {
            Vector3 futuro = cuerpo.bounds.center + actual * .12f;
            float borde = cuerpo.bounds.extents.y;
            if (!SaltoAutomaticoVoxel.HaySueloDebajo(futuro, borde + Bloque * .18f, cuerpo))
                actual = velocidadMovimiento = Vector3.zero;
        }
        actual += impulsoGolpe + impulsoExplosion;
        rb.velocity = new Vector3(actual.x, rb.velocity.y, actual.z);
        impulsoGolpe = Vector3.MoveTowards(impulsoGolpe, Vector3.zero, Time.fixedDeltaTime * 18f);
        impulsoExplosion = Vector3.MoveTowards(impulsoExplosion,Vector3.zero,Time.fixedDeltaTime*Bloque*(enSuelo ? 4f : 2.2f));
        rb.angularVelocity = Vector3.zero;
        if (camaraSeguir != null && camaraSeguir.EnPrimeraPersona)
            rb.MoveRotation(Quaternion.Euler(0f, camaraSeguir.AnguloHorizontal, 0f));
        else if (movimiento.sqrMagnitude > .001f)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(movimiento),
                1f - Mathf.Exp(-velocidadRotacion * Time.fixedDeltaTime)));

        bool auto = enSuelo && !agachado && !agua && saltoAutomatico &&
            Time.time >= proximoSaltoAutomatico && HayBloqueParaSaltar(movimiento);
        bool manual = Time.time - solicitudSalto <= .14f && Time.time - ultimoSuelo <= .09f;
        if (!agua && (auto || manual) && rb.velocity.y <= .15f)
        {
            rb.velocity = new Vector3(rb.velocity.x, fuerzaSalto, rb.velocity.z);
            solicitudSalto = ultimoSuelo = -10f;
            enSuelo = false;
            proximoSaltoAutomatico = Time.time + .35f;
            agotamiento += correr ? .2f : .05f;
        }
        if (agua)
        {
            float vertical = !inventarioAbierto && Input.GetKey(KeyCode.Space) ? Bloque * 2f : agachado ? -Bloque * 1.5f : -Bloque * .35f;
            rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
            rb.velocity = new Vector3(rb.velocity.x,
                Mathf.MoveTowards(rb.velocity.y, vertical, Bloque * 7f * Time.fixedDeltaTime), rb.velocity.z);
        }
        else if (!enSuelo)
            rb.AddForce(Physics.gravity * (gravedadSubida - 1f), ForceMode.Acceleration);
        if (rb.position.y < distanciaCaida) AplicarDano(vidaMaxima, false, Vector3.zero, true);
    }

    public void GanarExperiencia(int cantidad) { experiencia += Mathf.Max(0, cantidad); }

    void DibujarPausa(float ancho, float alto)
    {
        DibujarRectangulo(new Rect(0, 0, ancho, alto), new Color(0f, 0f, 0f, .72f));
        float x = ancho * .5f - 170f, y = alto * .5f - 160f;
        GUIStyle titulo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26 };
        GUI.Label(new Rect(x, y, 340f, 45f), "Juego en pausa", titulo);
        if (GUI.Button(new Rect(x, y + 54f, 340f, 34f), "Volver al juego")) CambiarPausa();
        saltoAutomatico = GUI.Toggle(new Rect(x, y + 101f, 340f, 24f), saltoAutomatico, "Salto automático de bloques");
        balanceoCamara = GUI.Toggle(new Rect(x, y + 131f, 340f, 24f), balanceoCamara, "Balanceo al caminar");
        GUI.Label(new Rect(x, y + 167f, 340f, 22f), "Sensibilidad del ratón");
        if (camaraSeguir != null)
            camaraSeguir.sensibilidadRaton = GUI.HorizontalSlider(new Rect(x, y + 196f, 340f, 20f),
                camaraSeguir.sensibilidadRaton, .3f, 5f);
        GUI.Label(new Rect(x, y + 224f, 340f, 22f), "Volumen de efectos");
        volumenEfectos = GUI.HorizontalSlider(new Rect(x, y + 251f, 340f, 20f), volumenEfectos, 0f, 1f);
        if (GUI.Button(new Rect(x, y + 281f, 165f, 30f), "Guardar mundo")) GuardarPartida();
        if (GUI.Button(new Rect(x + 175f, y + 281f, 165f, 30f), "Cargar guardado")) CargarPartida();
        if (Time.unscaledTime < avisoHasta) GUI.Label(new Rect(x, y + 321f, 340f, 36f), aviso);
    }

    void DibujarSupervivencia(float ancho, float alto, float barraX, float barraY)
    {
        float centro = ancho * .5f;
        DibujarRectangulo(new Rect(barraX + 2f, barraY - 7f, 360f, 5f), new Color(.07f, .1f, .02f, .9f));
        DibujarRectangulo(new Rect(barraX + 2f, barraY - 7f, 360f * (experiencia % 10) / 10f, 5f), new Color(.42f, .9f, .1f));
        if (experiencia >= 10)
            DibujarTextoSombra(new Rect(centro - 10f, barraY - 25f, 40f, 18f), (experiencia / 10).ToString());
        if (aire < 9.95f)
            for (int i = 0; i < Mathf.CeilToInt(aire); i++)
                DibujarTextoSombra(new Rect(barraX + 184f + i * 18f, barraY - 46f, 18f, 18f), "○");
        if (mundo != null && mundo.EstaEnAgua(PosicionOjos))
            DibujarRectangulo(new Rect(0, 0, ancho, alto), new Color(.03f, .25f, .65f, .20f));
        if (agachado) DibujarTextoSombra(new Rect(12f, alto - 28f, 180f, 20f), "Agachado");
        float listo = Mathf.Clamp01(1f - (proximoAtaque - Time.time) / intervaloEspada);
        if (listo < 1f)
        {
            DibujarRectangulo(new Rect(centro - 11f, alto * .5f + 13f, 22f, 3f), Color.black);
            DibujarRectangulo(new Rect(centro - 11f, alto * .5f + 13f, 22f * listo, 2f), Color.white);
        }
        if (mostrarDatos)
            DibujarTextoSombra(new Rect(12f, 70f, 440f, 40f),
                "XYZ (bloques): " + (transform.position / Bloque).ToString("F1") +
                "\nAltura: 2 bloques | Semilla: " + (mundo != null ? mundo.semilla.ToString() : "—"));
        if (!mostrarAyuda) DibujarTextoSombra(new Rect(12f, 8f, 360f, 20f), "F1 controles · Esc pausa · E inventario");
    }
}
