using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class DatosMisionDragonVoxel
{
    public bool aceptada, ganada, enEnd;
    public float vidaDragon=200;
    public bool[] cristales;
    public Vector3 retorno;
}

[DefaultExecutionOrder(400)]
public class MisionDragonVoxel : MonoBehaviour
{
    public script jugador;
    public DragonEndVoxel Dragon {get;private set;}
    public ArenaEndVoxel Arena {get;private set;}
    public bool Aceptada {get;private set;}
    public bool Ganada {get;private set;}
    public bool EnEnd {get;private set;}
    public bool Cinematica {get;private set;}
    public bool Victoria {get;private set;}
    private float inicioCinematica, finMuerte;
    private Vector3 retorno;
    private readonly List<GameObject> pausados=new List<GameObject>();
    private bool nieblaAnterior;
    private Color colorNieblaAnterior;
    private float distanciaNieblaAnterior;
    private float comienzoNieblaAnterior, densidadNieblaAnterior;
    private FogMode modoNieblaAnterior;
    private AmbientMode modoAmbienteAnterior;
    private Color ambienteAnterior, fondoAnterior;
    private CameraClearFlags limpiezaAnterior;
    public void Configurar(script propietario) { jugador=propietario; }

    public void Aceptar()
    {
        if(Ganada || jugador==null || jugador.Muerto) return;
        if(EnEnd) { jugador.CerrarMenuMision(); return; }
        retorno=jugador.transform.position;
        if(Arena==null)
        {
            var raiz=new GameObject("Dimension End: mision del dragon"); Arena=raiz.AddComponent<ArenaEndVoxel>();
            Arena.Configurar(jugador.Bloque,this); Dragon=Arena.Dragon;
        }
        Aceptada=true; Entrar(); Cinematica=true; inicioCinematica=Time.unscaledTime;
        jugador.CerrarMenuMision(); jugador.BloquearAventura(true); Time.timeScale=0;
    }
    void Entrar()
    {
        if(EnEnd) return;
        EnEnd=true; Arena.gameObject.SetActive(true);
        nieblaAnterior=RenderSettings.fog; colorNieblaAnterior=RenderSettings.fogColor; distanciaNieblaAnterior=RenderSettings.fogEndDistance;
        comienzoNieblaAnterior=RenderSettings.fogStartDistance; densidadNieblaAnterior=RenderSettings.fogDensity;
        modoNieblaAnterior=RenderSettings.fogMode; modoAmbienteAnterior=RenderSettings.ambientMode; ambienteAnterior=RenderSettings.ambientLight;
        if(Camera.main!=null) { fondoAnterior=Camera.main.backgroundColor; limpiezaAnterior=Camera.main.clearFlags; }
        var cielo=jugador.MundoActual.GetComponent<CieloVoxel>(); if(cielo!=null) cielo.Mostrar(false);
        Arena.Tormenta.Comenzar();
        foreach(var enemigo in FindObjectsByType<enemigo>(FindObjectsSortMode.None)) if(enemigo.gameObject.activeSelf) { pausados.Add(enemigo.gameObject); enemigo.gameObject.SetActive(false); }
        foreach(var animal in FindObjectsByType<AnimalVoxel>(FindObjectsSortMode.None)) if(animal.gameObject.activeSelf) { pausados.Add(animal.gameObject); animal.gameObject.SetActive(false); }
        jugador.MundoActual.MostrarDimensionEnd(Arena.transform);
        jugador.MoverA(Arena.Entrada,true);
    }
    public void TerminarCinematica()
    {
        if(!Cinematica) return;
        Cinematica=false; Time.timeScale=1; jugador.BloquearAventura(false);
        var camara=Camera.main!=null ? Camera.main.GetComponent<CamaraSeguir>() : null;
        if(camara!=null) { camara.ForzarPrimeraPersona(); camara.RestablecerMirada(0,-8); }
        Dragon.IniciarCombate();
    }
    public void DragonDerrotado()
    {
        if(Ganada) return;
        Ganada=true; finMuerte=Time.time+3f; jugador.BloquearAventura(true);
        jugador.SumarExperiencia(1200);
    }
    public void Regresar()
    {
        if(!EnEnd) return;
        EnEnd=false; Cinematica=Victoria=false;
        if(Arena!=null) Arena.gameObject.SetActive(false);
        jugador.MundoActual.MostrarDimensionEnd(null);
        foreach(var objeto in pausados) if(objeto!=null) objeto.SetActive(true); pausados.Clear();
        RenderSettings.fog=nieblaAnterior; RenderSettings.fogColor=colorNieblaAnterior; RenderSettings.fogEndDistance=distanciaNieblaAnterior;
        RenderSettings.fogStartDistance=comienzoNieblaAnterior; RenderSettings.fogDensity=densidadNieblaAnterior; RenderSettings.fogMode=modoNieblaAnterior;
        RenderSettings.ambientMode=modoAmbienteAnterior; RenderSettings.ambientLight=ambienteAnterior;
        if(Camera.main!=null) { Camera.main.backgroundColor=fondoAnterior; Camera.main.clearFlags=limpiezaAnterior; }
        var cielo=jugador.MundoActual.GetComponent<CieloVoxel>(); if(cielo!=null) cielo.Mostrar(true);
        jugador.MoverA(retorno,true); jugador.BloquearAventura(false); Time.timeScale=1;
    }
    void Update()
    {
        if(!EnEnd || jugador==null) return;
        if(Cinematica && (Time.unscaledTime-inicioCinematica>=6f || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))) TerminarCinematica();
        if(!Cinematica && !Ganada && !jugador.Muerto && jugador.transform.position.y<Arena.Centro.y-12)
            jugador.RecibirAlientoDragon(vidaLetal,jugador.transform.position);
        if(Ganada && !Victoria && Time.time>=finMuerte) { Victoria=true; Time.timeScale=0; }
    }
    const int vidaLetal=1000;
    void LateUpdate()
    {
        if(!EnEnd || Arena==null) return;
        Camera camara=Camera.main; if(camara==null) return;
        if(Cinematica)
        {
            float t=Mathf.Clamp01((Time.unscaledTime-inicioCinematica)/6);
            Vector3 foco=Dragon.transform.position;
            Vector3 posicion=Arena.Centro+new Vector3(Mathf.Sin(t*2f-1)*45,Mathf.Lerp(22,12,t),-65+t*20);
            camara.transform.SetPositionAndRotation(posicion,Quaternion.LookRotation(foco-posicion)); camara.fieldOfView=Mathf.Lerp(62,48,t);
        }
        Arena.Tormenta.Actualizar(camara,Cinematica);
    }
    public void Dibujar(float ancho,float alto)
    {
        if(!EnEnd) return;
        GUIStyle centro=new GUIStyle(GUI.skin.label) {alignment=TextAnchor.MiddleCenter,fontSize=18,fontStyle=FontStyle.Bold};
        if(Cinematica)
        {
            Fondo(new Rect(0,0,ancho,alto*.12f),Color.black); Fondo(new Rect(0,alto*.83f,ancho,alto*.17f),Color.black);
            GUI.Label(new Rect(0,alto*.85f,ancho,38),"EL FINAL · DERROTA AL DRAGÓN",centro);
            GUI.Label(new Rect(0,alto*.92f,ancho,25),"Destruye sus cristales. Después ataca al dragón.   [Espacio: omitir]",centro); return;
        }
        if(Victoria)
        {
            Fondo(new Rect(0,0,ancho,alto),new Color(.02f,.01f,.08f,.94f)); centro.fontSize=48; centro.normal.textColor=new Color(.7f,1,.45f);
            GUI.Label(new Rect(0,alto*.23f,ancho,90),"¡VICTORIA!",centro); centro.fontSize=20; centro.normal.textColor=Color.white;
            GUI.Label(new Rect(0,alto*.43f,ancho,80),"Has derrotado al Dragón del End\nMisión completada · +1200 de experiencia",centro);
            if(GUI.Button(new Rect(ancho*.5f-155,alto*.68f,310,44),"Continuar en mi mundo")) Regresar(); return;
        }
        if(Dragon==null) return;
        float x=ancho*.5f-220;
        GUI.Label(new Rect(x,4,440,25),"Dragón del End",centro);
        Fondo(new Rect(x,33,440,15),new Color(.15f,.025f,.2f)); Fondo(new Rect(x+2,35,436*Dragon.Vida/DragonEndVoxel.VidaMaxima,11),new Color(.78f,.12f,.88f));
        centro.fontSize=12; GUI.Label(new Rect(x,49,440,22),Mathf.CeilToInt(Dragon.Vida)+" / "+DragonEndVoxel.VidaMaxima+" · Cristales: "+Arena.CristalesVivos+" / 10",centro);
        if(!Ganada) GUI.Label(new Rect(15,85,440,25),Dragon.Fase==FaseDragonVoxel.Posado ? "Dragon posado: las flechas no le afectan. Usa la espada." : Arena.CristalesVivos>0 ? "Los cristales regeneran al dragón: destrúyelos con el arco." : "Sin cristales: alcanza al dragón con tus flechas.");
    }
    static void Fondo(Rect r,Color c) { Color anterior=GUI.color; GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=anterior; }
    public DatosMisionDragonVoxel Guardar()
    {
        var datos=new DatosMisionDragonVoxel {aceptada=Aceptada,ganada=Ganada,enEnd=EnEnd,retorno=retorno,vidaDragon=Dragon!=null ? Dragon.Vida : 200};
        if(Arena!=null) { datos.cristales=new bool[10]; for(int i=0;i<10;i++) datos.cristales[i]=Arena.Cristales[i]!=null && Arena.Cristales[i].Vivo; }
        return datos;
    }
    public void Restaurar(DatosMisionDragonVoxel datos)
    {
        Regresar(); if(Arena!=null) Destroy(Arena.gameObject); Arena=null; Dragon=null;
        Aceptada=datos!=null && datos.aceptada; Ganada=datos!=null && datos.ganada;
        if(!Aceptada) return;
        retorno=datos.retorno; var raiz=new GameObject("Dimension End: mision restaurada"); Arena=raiz.AddComponent<ArenaEndVoxel>(); Arena.Configurar(jugador.Bloque,this); Dragon=Arena.Dragon;
        Dragon.RestaurarVida(datos.vidaDragon);
        if(datos.cristales!=null) for(int i=0;i<Mathf.Min(10,datos.cristales.Length);i++) if(!datos.cristales[i]) Arena.Cristales[i].DesactivarSinExplosion();
        Arena.gameObject.SetActive(false);
        jugador.MundoActual.MostrarDimensionEnd(null);
        if(datos.enEnd && !Ganada) { Entrar(); Dragon.IniciarCombate(); } else jugador.BloquearAventura(false);
    }
    void OnDestroy() { if(Arena!=null) Destroy(Arena.gameObject); }
}

public class ArenaEndVoxel : MonoBehaviour
{
    public Vector3 Centro => transform.position;
    public Vector3 Entrada => Centro+new Vector3(0,bloque*1.5f+.1f,-bloque*22);
    public DragonEndVoxel Dragon {get;private set;}
    public CristalEndVoxel[] Cristales {get;private set;}
    public TormentaEndVoxel Tormenta {get;private set;}
    public int CristalesVivos {get { int total=0; if(Cristales!=null) foreach(var cristal in Cristales) if(cristal!=null && cristal.Vivo) total++; return total; } }
    public CristalEndVoxel CristalCurador()
    {
        CristalEndVoxel mejor=null; float distancia=float.PositiveInfinity;
        if(Cristales==null || Dragon==null) return null;
        foreach(var cristal in Cristales) if(cristal!=null && cristal.Vivo)
        { float d=(cristal.transform.position-Dragon.transform.position).sqrMagnitude; if(d<distancia) { mejor=cristal; distancia=d; } }
        return mejor;
    }
    private float bloque;
    private Material piedra,obsidiana;
    private Mesh malla;
    public void Configurar(float tamano,MisionDragonVoxel mision)
    {
        bloque=tamano; transform.position=new Vector3(0,40,0);
        piedra=MaterialBloque("end_stone",Color.white); obsidiana=MaterialBloque("obsidian",Color.white);
        var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var indices=new List<int>();
        const int radio=38;
        for(int x=-radio;x<=radio;x++) for(int z=-radio;z<=radio;z++)
        {
            if(x*x+z*z>radio*radio) continue;
            int o=vertices.Count; float x0=x*bloque,z0=z*bloque;
            vertices.Add(new Vector3(x0,bloque*.5f,z0)); vertices.Add(new Vector3(x0,bloque*.5f,z0+bloque));
            vertices.Add(new Vector3(x0+bloque,bloque*.5f,z0+bloque)); vertices.Add(new Vector3(x0+bloque,bloque*.5f,z0));
            uv.Add(Vector2.zero); uv.Add(Vector2.up); uv.Add(Vector2.one); uv.Add(Vector2.right);
            indices.Add(o); indices.Add(o+1); indices.Add(o+2); indices.Add(o); indices.Add(o+2); indices.Add(o+3);
        }
        var suelo=new GameObject("Isla de piedra del End"); suelo.transform.SetParent(transform,false);
        malla=new Mesh {name="Isla End",indexFormat=IndexFormat.UInt32}; malla.SetVertices(vertices); malla.SetUVs(0,uv); malla.SetTriangles(indices,0); malla.RecalculateNormals();
        suelo.AddComponent<MeshFilter>().sharedMesh=malla; suelo.AddComponent<MeshRenderer>().sharedMaterial=piedra; suelo.AddComponent<MeshCollider>().sharedMesh=malla;
        Cristales=new CristalEndVoxel[10];
        for(int i=0;i<10;i++)
        {
            float angulo=i*Mathf.PI*2/10; float alto=(10+i%4*3)*bloque;
            Vector3 basePilar=new Vector3(Mathf.Sin(angulo)*bloque*28,alto*.5f+bloque*.5f,Mathf.Cos(angulo)*bloque*28);
            var pilar=GameObject.CreatePrimitive(PrimitiveType.Cube); pilar.name="Torre de obsidiana "+(i+1); pilar.transform.SetParent(transform,false);
            pilar.transform.localPosition=basePilar; pilar.transform.localScale=new Vector3(bloque*3,alto,bloque*3); pilar.GetComponent<Renderer>().sharedMaterial=obsidiana;
            var objeto=new GameObject("Cristal del End "+(i+1)); objeto.transform.SetParent(transform,false);
            objeto.transform.localPosition=new Vector3(basePilar.x,alto+1.6f*bloque,basePilar.z);
            Cristales[i]=objeto.AddComponent<CristalEndVoxel>(); Cristales[i].Configurar(this,bloque);
        }
        var portal=GameObject.CreatePrimitive(PrimitiveType.Cylinder); portal.name="Fuente central"; portal.transform.SetParent(transform,false); portal.transform.localPosition=Vector3.up*bloque*.5f;
        portal.transform.localScale=new Vector3(bloque*5,bloque*.6f,bloque*5); portal.GetComponent<Renderer>().sharedMaterial=obsidiana;
        var dragon=new GameObject("Dragon del End"); dragon.transform.SetParent(transform,false); dragon.transform.localPosition=new Vector3(0,bloque*18,bloque*10);
        Dragon=dragon.AddComponent<DragonEndVoxel>(); Dragon.Configurar(mision,this,bloque);
        Tormenta=gameObject.AddComponent<TormentaEndVoxel>(); Tormenta.Configurar(this);
    }
    static Material MaterialBloque(string nombre,Color color)
    {
        var material=new Material(Shader.Find("Standard")) {mainTexture=Resources.Load<Texture2D>("Minecraft/Blocks/"+nombre),color=color};
        if(material.mainTexture!=null) material.mainTexture.filterMode=FilterMode.Point;
        material.SetFloat("_Glossiness",0); return material;
    }
    void OnDestroy() { if(malla!=null) Destroy(malla); if(piedra!=null) Destroy(piedra); if(obsidiana!=null) Destroy(obsidiana); }
}

public partial class script
{
    private bool menuInicial=true,menuMision,bloqueoAventura;
    private MisionDragonVoxel misionDragon;
    public MisionDragonVoxel MisionActual => misionDragon;
    public MundoVoxel MundoActual => mundo;
    public bool MenuInicial => menuInicial;
    public void SumarExperiencia(int cantidad) { experiencia+=cantidad; }
    public void IniciarJuego()
    {
        menuInicial=false; inicioCarga=Time.realtimeSinceStartup; cargando=true; Time.timeScale=0;
        if(camaraSeguir!=null) camaraSeguir.MostrarCursorInterfaz(true);
    }
    public void BloquearAventura(bool bloquear)
    {
        bloqueoAventura=bloquear; preparandoArco=cargandoTridente=false; entradaMovimiento=Vector2.zero;
        if(camaraSeguir!=null) camaraSeguir.MostrarCursorInterfaz(bloquear);
    }
    public void AbrirMenuMision()
    {
        if(menuInicial || cargando || terminado || bloqueoAventura) return;
        if(inventarioAbierto) CambiarInventario(); if(pausado) CambiarPausa();
        menuMision=true; BloquearAventura(true); Time.timeScale=0;
    }
    public void CerrarMenuMision() { menuMision=false; BloquearAventura(false); Time.timeScale=1; }
    void DibujarInicio(float ancho,float alto)
    {
        DibujarRectangulo(new Rect(0,0,ancho,alto),new Color(.055f,.09f,.12f));
        var estilo=new GUIStyle(GUI.skin.label) {fontSize=48,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
        estilo.normal.textColor=new Color(.65f,.95f,.7f); GUI.Label(new Rect(0,alto*.17f,ancho,90),"RODOLFO · AVENTURA VOXEL",estilo);
        estilo.fontSize=19; estilo.normal.textColor=Color.white; GUI.Label(new Rect(0,alto*.37f,ancho,50),"Construye, explora y derrota al Dragón del End",estilo);
        if(GUI.Button(new Rect(ancho*.5f-150,alto*.56f,300,55),"INICIAR")) IniciarJuego();
        estilo.fontSize=14; GUI.Label(new Rect(0,alto*.78f,ancho,50),"Equipo inicial: diamante · arco de fuego · antorchas · manzanas encantadas",estilo);
    }
    void DibujarMenuMision(float ancho,float alto)
    {
        DibujarRectangulo(new Rect(0,0,ancho,alto),new Color(.04f,.015f,.075f,.97f));
        var estilo=new GUIStyle(GUI.skin.label) {alignment=TextAnchor.MiddleCenter,fontSize=32,fontStyle=FontStyle.Bold};
        GUI.Label(new Rect(0,alto*.15f,ancho,55),"MISIÓN · EL FINAL",estilo); estilo.fontSize=18;
        GUI.Label(new Rect(ancho*.12f,alto*.30f,ancho*.76f,110),misionDragon.Ganada ? "¡Misión completada!\nDerrotaste al Dragón del End." : "Viaja al End y derrota al dragón.\nDestruye los 10 cristales que regeneran su vida.\nUsa el arco con fuego e infinidad y tus manzanas encantadas.",estilo);
        if(!misionDragon.Ganada && GUI.Button(new Rect(ancho*.5f-150,alto*.59f,300,45),misionDragon.EnEnd ? "Continuar combate" : misionDragon.Aceptada ? "Retomar misión" : "Aceptar misión")) misionDragon.Aceptar();
        if(misionDragon.EnEnd && GUI.Button(new Rect(ancho*.5f-150,alto*.70f,300,36),"Regresar al mundo")) { CerrarMenuMision(); misionDragon.Regresar(); }
        if(GUI.Button(new Rect(ancho*.5f-150,alto*.82f,300,35),"Cerrar")) CerrarMenuMision();
    }
}

public partial class MundoVoxel
{
    private Transform raizDimensionEnd;
    public void MostrarDimensionEnd(Transform raiz)
    {
        raizDimensionEnd=raiz; if(raizGenerada!=null) raizGenerada.gameObject.SetActive(raiz==null);
        foreach(var bloque in funcionales.Values)
            if(bloque!=null && bloque.delEnd)
            { if(raiz!=null) bloque.transform.SetParent(raiz,true); bloque.gameObject.SetActive(raiz!=null); }
    }
    Transform PadreEspecial(Vector3Int celda)
    { return raizDimensionEnd!=null && CentroCelda(celda).y>38 ? raizDimensionEnd : raizGenerada; }
}
