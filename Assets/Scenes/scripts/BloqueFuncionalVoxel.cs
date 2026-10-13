using System;
using UnityEngine;

[Serializable]
public class DatosBloqueFuncionalVoxel
{
    public Vector3Int celda;
    public TipoBloque tipo;
    public int giro;
    public bool delEnd;
    public PilaObjetoVoxel[] contenido;
    public float combustible, combustibleTotal, progreso;
    public int experiencia;
}

// El horno simula su trabajo aunque su ventana esté cerrada.
public class BloqueFuncionalVoxel : MonoBehaviour
{
    public TipoBloque tipo;
    public Vector3Int celda;
    public int giro;
    public bool delEnd;
    public PilaObjetoVoxel[] contenido;
    public float combustible, combustibleTotal, progreso;
    public int experiencia;
    public const float SegundosCoccion = 10f;
    public bool Abierto { get; set; }
    public bool Encendido => combustible > 0f;
    private MundoVoxel mundo;
    private MeshRenderer aspecto;
    private Transform tapa;
    private Light luz;
    private bool estadoVisual;
    private Mesh malla;
    private Material materialCofre;
    private IdObjetoVoxel cocinando;

    public void Configurar(MundoVoxel mundo, Vector3Int celda, TipoBloque tipo, int giro)
    {
        this.mundo = mundo; this.celda = celda; this.tipo = tipo; this.giro = giro;
        contenido = PilasVoxel.Crear(tipo == TipoBloque.Cofre ? 27 : tipo == TipoBloque.Horno ? 3 : 0);
        transform.localRotation = Quaternion.Euler(0f, giro * 90f, 0f);
        BoxCollider colision = gameObject.AddComponent<BoxCollider>();
        colision.size = Vector3.one * mundo.TamanoBloque;
        if(tipo==TipoBloque.Antorcha) gameObject.AddComponent<AntorchaVoxel>().Configurar(mundo,this);
        else if (tipo == TipoBloque.Cofre) CrearCofre();
        else
        {
            GameObject visual = new GameObject("Bloque texturizado");
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * mundo.TamanoBloque;
            malla = CrearCuboSeisCaras();
            visual.AddComponent<MeshFilter>().sharedMesh = malla;
            aspecto = visual.AddComponent<MeshRenderer>();
            aspecto.sharedMaterials = mundo.MaterialesEspeciales(tipo, false);
            if (tipo == TipoBloque.Horno)
            {
                luz = new GameObject("Luz del horno").AddComponent<Light>();
                luz.transform.SetParent(transform, false);
                luz.transform.localPosition = Vector3.forward * (mundo.TamanoBloque * .56f);
                luz.type = LightType.Point; luz.color = new Color(1f, .49f, .15f);
                luz.range = mundo.TamanoBloque * 3f; luz.intensity = 1f; luz.enabled = false;
            }
        }
    }

    public static Mesh CrearCuboSeisCaras()
    {
        Vector3[,] lados = {
            {new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)},
            {new Vector3(.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f)},
            {new Vector3(.5f,-.5f,.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f)},
            {new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,.5f,-.5f)},
            {new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f)},
            {new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f)} };
        Vector3[] vertices = new Vector3[24]; Vector2[] uv = new Vector2[24];
        Mesh malla = new Mesh { name = "Cubo: una textura por cara", subMeshCount = 6 };
        for (int cara = 0; cara < 6; cara++)
        {
            int o = cara * 4;
            for (int i = 0; i < 4; i++) vertices[o+i] = lados[cara,i];
            uv[o] = Vector2.zero; uv[o+1] = Vector2.right; uv[o+2] = Vector2.one; uv[o+3] = Vector2.up;
        }
        malla.vertices = vertices; malla.uv = uv;
        for (int cara = 0; cara < 6; cara++)
        { int o = cara * 4; malla.SetTriangles(new[] { o,o+1,o+2,o,o+2,o+3 },cara); }
        malla.RecalculateNormals(); malla.RecalculateBounds(); return malla;
    }

    Transform PiezaCofre(string nombre, Vector3 posicion, Vector3 escala, int u, int v, int w, int h, int d, Transform padre = null)
    {
        GameObject pieza = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pieza.name = nombre; pieza.transform.SetParent(padre != null ? padre : transform, false);
        pieza.transform.localPosition = posicion * mundo.TamanoBloque;
        pieza.transform.localScale = escala * mundo.TamanoBloque;
        pieza.GetComponent<Collider>().enabled = false; Destroy(pieza.GetComponent<Collider>());
        MinecraftSkinBox.Aplicar(pieza.transform, materialCofre, u,v,w,h,d);
        return pieza.transform;
    }

    void CrearCofre()
    {
        materialCofre = MinecraftSkinBox.CrearMaterial("chest/normal");
        PiezaCofre("Caja del cofre", new Vector3(0,-.1875f,0), new Vector3(.875f,.625f,.875f),0,19,14,10,14);
        tapa = new GameObject("Bisagra de la tapa").transform;
        tapa.SetParent(transform,false); tapa.localPosition = new Vector3(0,.125f,-.4375f) * mundo.TamanoBloque;
        PiezaCofre("Tapa", new Vector3(0,.15625f,.4375f),new Vector3(.875f,.3125f,.875f),0,0,14,5,14,tapa);
        PiezaCofre("Cerradura",new Vector3(0,.04f,.89f),new Vector3(.125f,.25f,.0625f),0,0,2,4,1,tapa);
    }

    public bool Acepta(int slot, PilaObjetoVoxel pila)
    {
        if (pila == null || pila.Vacia) return true;
        if (pila.permanente) return false;
        if (tipo == TipoBloque.Cofre) return true;
        if (tipo != TipoBloque.Horno) return false;
        return slot == 0 ? RecetasVoxel.Fundicion(pila.id) != IdObjetoVoxel.Ninguno :
            slot == 1 && CatalogoObjetosVoxel.De(pila.id).combustible > 0f;
    }

    void Update()
    {
        if (tipo != TipoBloque.Horno) return;
        PilaObjetoVoxel entrada = contenido[0], fuel = contenido[1], salida = contenido[2];
        IdObjetoVoxel resultado = entrada.Vacia ? IdObjetoVoxel.Ninguno : RecetasVoxel.Fundicion(entrada.id);
        bool puede = resultado != IdObjetoVoxel.Ninguno && (salida.Vacia ||
            salida.id == resultado && salida.cantidad < CatalogoObjetosVoxel.De(resultado).limite);
        if (cocinando != entrada.id) { progreso = 0f; cocinando = entrada.id; }
        if (combustible <= 0f && puede && !fuel.Vacia && !fuel.permanente)
        {
            combustibleTotal = combustible = CatalogoObjetosVoxel.De(fuel.id).combustible;
            if (combustible > 0f) { fuel.cantidad--; if (fuel.Vacia) fuel.Limpiar(); }
        }
        float disponible = Mathf.Min(Time.deltaTime, combustible);
        combustible = Mathf.Max(0f, combustible - Time.deltaTime);
        if (puede && disponible > 0f)
        {
            progreso += disponible;
            if (progreso >= SegundosCoccion)
            {
                progreso -= SegundosCoccion;
                entrada.cantidad--; if (entrada.Vacia) entrada.Limpiar();
                if (salida.Vacia) contenido[2] = new PilaObjetoVoxel(resultado);
                else salida.cantidad++;
                experiencia++;
            }
        }
        else if (!puede) progreso = 0f;
        else progreso = Mathf.Max(0f, progreso - Time.deltaTime * 2f);
        if (estadoVisual != Encendido)
        {
            estadoVisual = Encendido;
            aspecto.sharedMaterials = mundo.MaterialesEspeciales(tipo, Encendido);
            if (luz != null) luz.enabled = Encendido;
        }
    }

    void LateUpdate()
    {
        if (tapa != null) tapa.localRotation = Quaternion.Slerp(tapa.localRotation,
            Quaternion.Euler(Abierto ? -100f : 0f,0,0),1f - Mathf.Exp(-10f * Time.deltaTime));
    }

    public void SoltarContenido()
    {
        for(int i=0;i<contenido.Length;i++)
            if(!contenido[i].Vacia)
            {
                ObjetoRecogibleVoxel.SoltarObjeto(transform.position+Vector3.up*.4f,contenido[i].Copia(),tipo==TipoBloque.Horno && i==2 ? experiencia : 0);
                contenido[i].Limpiar();
            }
        experiencia=0;
    }
    public DatosBloqueFuncionalVoxel Guardar() => new DatosBloqueFuncionalVoxel
    { celda = celda, tipo = tipo, giro = giro, delEnd=delEnd, contenido = PilasVoxel.Copiar(contenido), combustible = combustible,
        combustibleTotal = combustibleTotal, progreso = progreso, experiencia = experiencia };
    public void Cargar(DatosBloqueFuncionalVoxel datos)
    {
        contenido = PilasVoxel.Copiar(datos.contenido); giro = datos.giro; delEnd=datos.delEnd;
        transform.localRotation = Quaternion.Euler(0,giro * 90f,0);
        if(tipo==TipoBloque.Antorcha) GetComponent<AntorchaVoxel>().AjustarMontaje(giro);
        combustible = datos.combustible; combustibleTotal = datos.combustibleTotal;
        progreso = datos.progreso; experiencia = datos.experiencia;
        cocinando = contenido.Length > 0 ? contenido[0].id : IdObjetoVoxel.Ninguno;
    }
    void OnDestroy()
    {
        if (malla != null) Destroy(malla);
        if (materialCofre != null) Destroy(materialCofre);
        if (tipo == TipoBloque.Cofre)
            foreach (MeshFilter filtro in GetComponentsInChildren<MeshFilter>())
                if (filtro.sharedMesh != null && filtro.sharedMesh.name.StartsWith("Cubo con piel")) Destroy(filtro.sharedMesh);
    }
}
