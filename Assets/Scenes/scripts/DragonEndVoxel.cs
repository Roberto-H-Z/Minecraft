using System.IO;
using UnityEngine;

public enum FaseDragonVoxel { Volando, Embistiendo, Posado, Muerto }

public class DragonEndVoxel : MonoBehaviour
{
    public const float VidaMaxima=200;
    public float Vida {get;private set;}=VidaMaxima;
    public FaseDragonVoxel Fase {get;private set;}
    public bool Combate {get;private set;}
    private MisionDragonVoxel mision;
    private ArenaEndVoxel arena;
    private float bloque, finFase, proximoAliento, proximoContacto, ultimoDano=-10, finRojo;
    private Vector3 destino;
    private Vector3 velocidadVuelo;
    private bool saliendoDeEmbestida;
    private Material material;
    private Mesh malla;
    private Vector3[] baseVertices, animados, baseNormales, normales;
    private Collider cabeza;

    public void Configurar(MisionDragonVoxel propietario,ArenaEndVoxel isla,float escala)
    {
        mision=propietario; arena=isla; bloque=escala;
        var recurso=Resources.Load<TextAsset>("Dragon/model");
        if(recurso==null) throw new System.InvalidOperationException("No se encontro el modelo del dragon proporcionado por el usuario.");
        using(var lector=new BinaryReader(new MemoryStream(recurso.bytes)))
        {
            if(lector.ReadUInt32()!=0x44525856) throw new System.IO.InvalidDataException("Modelo de dragon invalido.");
            int cantidad=lector.ReadInt32(),cantidadIndices=lector.ReadInt32(); baseVertices=new Vector3[cantidad]; baseNormales=new Vector3[cantidad]; var uv=new Vector2[cantidad];
            float ancho=0;
            for(int i=0;i<cantidad;i++)
            {
                baseVertices[i]=new Vector3(-lector.ReadSingle(),lector.ReadSingle(),-lector.ReadSingle()); ancho=Mathf.Max(ancho,Mathf.Abs(baseVertices[i].x)*2);
                baseNormales[i]=new Vector3(-lector.ReadSingle(),lector.ReadSingle(),-lector.ReadSingle()); uv[i]=new Vector2(lector.ReadSingle(),1f-lector.ReadSingle());
            }
            float factor=bloque*14f/ancho;
            for(int i=0;i<cantidad;i++) baseVertices[i]*=factor;
            var indices=new int[cantidadIndices*2];
            for(int i=0;i<cantidadIndices;i++) indices[i]=lector.ReadInt32();
            for(int i=0;i<cantidadIndices;i+=3) { indices[cantidadIndices+i]=indices[i]; indices[cantidadIndices+i+1]=indices[i+2]; indices[cantidadIndices+i+2]=indices[i+1]; }
            animados=(Vector3[])baseVertices.Clone(); normales=(Vector3[])baseNormales.Clone();
            malla=new Mesh {name="Dragon End: modelo del usuario"}; malla.MarkDynamic(); malla.vertices=baseVertices; malla.normals=baseNormales; malla.uv=uv; malla.triangles=indices; malla.RecalculateBounds();
            malla.bounds=new Bounds(Vector3.zero,malla.bounds.size+Vector3.up*bloque*14f);
        }
        var visual=new GameObject("Modelo animado del dragon"); visual.transform.SetParent(transform,false);
        visual.AddComponent<MeshFilter>().sharedMesh=malla;
        material=new Material(Shader.Find("Standard")) {mainTexture=Resources.Load<Texture2D>("Dragon/texture"),color=Color.white};
        material.mainTexture.filterMode=FilterMode.Point; material.SetFloat("_Glossiness",0);
        material.SetFloat("_Mode",1); material.SetFloat("_Cutoff",.2f); material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue=2450;
        visual.AddComponent<MeshRenderer>().sharedMaterial=material;
        Rigidbody cuerpo=gameObject.AddComponent<Rigidbody>(); cuerpo.isKinematic=true; cuerpo.useGravity=false;
        CrearZona("Cuerpo",new Vector3(0,-.5f*bloque,-bloque),new Vector3(4*bloque,2.5f*bloque,7*bloque));
        cabeza=CrearZona("Cabeza vulnerable",new Vector3(0,.5f*bloque,7.5f*bloque),new Vector3(2.5f*bloque,2.5f*bloque,4*bloque));
        CrearZona("Ala izquierda",new Vector3(-4.8f*bloque,.5f*bloque,-bloque),new Vector3(5*bloque,1.5f*bloque,5*bloque));
        CrearZona("Ala derecha",new Vector3(4.8f*bloque,.5f*bloque,-bloque),new Vector3(5*bloque,1.5f*bloque,5*bloque));
        CrearZona("Cola",new Vector3(0,0,-6*bloque),new Vector3(2*bloque,2*bloque,7*bloque));
    }
    Collider CrearZona(string nombre,Vector3 centro,Vector3 tamano)
    {
        var objeto=new GameObject(nombre); objeto.transform.SetParent(transform,false);
        var colision=objeto.AddComponent<BoxCollider>(); colision.center=centro; colision.size=tamano;
        if(mision.jugador!=null) Physics.IgnoreCollision(colision,mision.jugador.GetComponent<Collider>());
        return colision;
    }
    public void IniciarCombate() { if(Vida<=0) return; Combate=true; CambiarFase(FaseDragonVoxel.Volando,7); proximoAliento=Time.time+3; }
    public void RestaurarVida(float valor) { Vida=Mathf.Clamp(valor,0,VidaMaxima); if(Vida<=0) { Fase=FaseDragonVoxel.Muerto; foreach(var c in GetComponentsInChildren<Collider>()) c.enabled=false; } }
    void CambiarFase(FaseDragonVoxel fase,float duracion)
    {
        Fase=fase; finFase=Time.time+duracion;
        if(fase==FaseDragonVoxel.Embistiendo)
        {
            destino=mision.jugador.transform.position+Vector3.up*bloque;
            saliendoDeEmbestida=false;
        }
    }
    void Update()
    {
        AnimarAlas();
        if(Vida<=0 || !Combate || mision.jugador==null || mision.jugador.Muerto || !mision.EnEnd) return;
        if(Time.time>=finFase)
        {
            if(Fase==FaseDragonVoxel.Volando) CambiarFase(FaseDragonVoxel.Embistiendo,4.5f);
            else if(Fase==FaseDragonVoxel.Embistiendo) CambiarFase(FaseDragonVoxel.Posado,8);
            else CambiarFase(FaseDragonVoxel.Volando,9);
        }
        Vector3 objetivo;
        if(Fase==FaseDragonVoxel.Volando)
        {
            float angulo=Time.time*.3f; objetivo=arena.Centro+new Vector3(Mathf.Sin(angulo)*bloque*19,bloque*(16+Mathf.Sin(angulo*2)*3),Mathf.Cos(angulo)*bloque*19);
        }
        else if(Fase==FaseDragonVoxel.Embistiendo) objetivo=destino;
        else objetivo=arena.Centro+Vector3.up*bloque*3.5f;
        MoverDragon(objetivo);
        if(arena.CristalesVivos>0) Vida=Mathf.Min(VidaMaxima,Vida+Time.deltaTime*3);
        if(Time.time>=proximoAliento)
        {
            proximoAliento=Time.time+(Fase==FaseDragonVoxel.Posado ? 2.3f : 4.5f);
            Vector3 origen=transform.TransformPoint(new Vector3(0,.5f*bloque,7*bloque));
            AlientoDragonVoxel.Lanzar(origen,mision.jugador.PosicionOjos,arena,mision.jugador);
        }
        if(Time.time>=proximoContacto && Vector3.Distance(transform.position,mision.jugador.transform.position)<bloque*4)
        { proximoContacto=Time.time+1.25f; mision.jugador.RecibirDano(6,transform.position); }
        material.color=Time.time<finRojo ? new Color(1,.15f,.15f) : Color.white;
    }
    void MoverDragon(Vector3 objetivo)
    {
        float dt=Time.deltaTime;
        if(dt<=0f) return;
        Vector3 rumbo=objetivo-transform.position;
        if(Fase==FaseDragonVoxel.Embistiendo && !saliendoDeEmbestida && rumbo.magnitude<bloque*2.5f)
        {
            // Despues de atacar sigue volando y asciende; no se queda inmovil sobre Rodolfo.
            Vector3 salida=Vector3.ProjectOnPlane(velocidadVuelo,Vector3.up).normalized;
            if(salida.sqrMagnitude<.1f) salida=transform.forward;
            destino=arena.Centro+salida*bloque*24+Vector3.up*bloque*16;
            saliendoDeEmbestida=true; objetivo=destino; rumbo=objetivo-transform.position;
        }
        bool aterrizando=Fase==FaseDragonVoxel.Posado;
        if(aterrizando && rumbo.magnitude<bloque*.35f && velocidadVuelo.magnitude<bloque)
        {
            transform.position=Vector3.MoveTowards(transform.position,objetivo,dt*bloque);
            velocidadVuelo=Vector3.zero;
            Vector3 mirada=Vector3.ProjectOnPlane(mision.jugador.transform.position-transform.position,Vector3.up);
            if(mirada.sqrMagnitude>.01f)
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(mirada),dt*100f);
            return;
        }
        float rapidez=bloque*(Fase==FaseDragonVoxel.Embistiendo ? 18f : 12f);
        if(aterrizando) rapidez=Mathf.Min(rapidez,rumbo.magnitude*1.8f);
        Vector3 deseada=rumbo.normalized*rapidez;
        velocidadVuelo=Vector3.Lerp(velocidadVuelo,deseada,1f-Mathf.Exp(-dt*(aterrizando ? 3.5f : 2.8f)));
        Vector3 paso=velocidadVuelo*dt;
        if(aterrizando && paso.magnitude>rumbo.magnitude) paso=rumbo;
        if(paso.sqrMagnitude<.000001f) return;
        Vector3 anterior=transform.forward;
        transform.position+=paso;
        // La direccion ya se suaviza en la velocidad. Orientar con el desplazamiento real evita deslizarse de costado.
        Quaternion orientacion=Quaternion.LookRotation(paso.normalized,Vector3.up);
        float inclinacion=aterrizando ? 0f : Mathf.Clamp(-Vector3.SignedAngle(anterior,paso,Vector3.up)/dt*.10f,-18f,18f);
        transform.rotation=orientacion*Quaternion.Euler(0,0,inclinacion);
    }
    void AnimarAlas()
    {
        if(malla==null || baseVertices==null || Fase==FaseDragonVoxel.Muerto) return;
        float aleteo=Mathf.Sin(Time.unscaledTime*(Fase==FaseDragonVoxel.Posado ? 2.4f : 5.5f))*.38f;
        float pivote=bloque*1.6f;
        for(int i=0;i<animados.Length;i++)
        {
            Vector3 p=baseVertices[i]; Vector3 n=baseNormales[i]; float lado=Mathf.Sign(p.x);
            if(Mathf.Abs(p.x)>pivote)
            {
                float c=Mathf.Cos(aleteo*lado),s=Mathf.Sin(aleteo*lado),x=p.x-lado*pivote;
                p=new Vector3(lado*pivote+x*c-p.y*s,x*s+p.y*c,p.z); n=new Vector3(n.x*c-n.y*s,n.x*s+n.y*c,n.z);
            }
            animados[i]=p; normales[i]=n;
        }
        malla.SetVertices(animados); malla.SetNormals(normales);
    }
    public void RecibirDano(float cantidad,Collider parte=null,bool proyectil=false)
    {
        if(!Combate || Vida<=0 || Time.time-ultimoDano<.15f) return;
        if(proyectil && Fase==FaseDragonVoxel.Posado) { SonidosVoxel.Emitir("picar",transform.position); return; }
        ultimoDano=Time.time; finRojo=Time.time+.25f;
        Vida=Mathf.Max(0,Vida-cantidad*(parte==cabeza ? 1.5f : 1));
        SonidosVoxel.Emitir("herida",transform.position);
        if(Vida>0) return;
        Combate=false; Fase=FaseDragonVoxel.Muerto;
        foreach(var c in GetComponentsInChildren<Collider>()) c.enabled=false;
        EfectosBloqueVoxel.Emitir(transform.position,material); mision.DragonDerrotado();
        var muerte=gameObject.AddComponent<MuerteDragonVoxel>(); muerte.Configurar(material);
    }
    void OnDestroy() { if(malla!=null) Destroy(malla); if(material!=null) Destroy(material); }
}

public class MuerteDragonVoxel : MonoBehaviour
{
    private float inicio; private Material material;
    public void Configurar(Material mat) { material=mat; inicio=Time.time; }
    void Update()
    {
        float t=(Time.time-inicio)/3f; transform.position+=Vector3.up*Time.deltaTime*2;
        transform.Rotate(0,Time.deltaTime*40,0); if(material!=null) material.color=Color.Lerp(new Color(1,.15f,.5f),Color.white,t);
    }
}

public class CristalEndVoxel : MonoBehaviour
{
    public bool Vivo {get;private set;}=true;
    private ArenaEndVoxel arena;
    private float bloque;
    private Material material,cristal;
    private Transform visual;
    private LineRenderer rayo;
    public void Configurar(ArenaEndVoxel isla,float escala)
    {
        arena=isla; bloque=escala; gameObject.AddComponent<BoxCollider>().size=Vector3.one*bloque*1.5f;
        material=new Material(Shader.Find("Unlit/Color")) {color=new Color(1,.16f,.9f)};
        cristal=new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse")) {color=new Color(.72f,.5f,1,.26f)};
        visual=new GameObject("Cristal giratorio").transform; visual.SetParent(transform,false);
        Cubo(Vector3.one*bloque*.75f,material); Cubo(Vector3.one*bloque*1.35f,cristal);
        rayo=new GameObject("Rayo de regeneracion").AddComponent<LineRenderer>(); rayo.transform.SetParent(transform,false);
        rayo.sharedMaterial=material; rayo.positionCount=2; rayo.startWidth=.3f; rayo.endWidth=.15f;
    }
    void Cubo(Vector3 tamano,Material mat)
    {
        var pieza=GameObject.CreatePrimitive(PrimitiveType.Cube); pieza.transform.SetParent(visual,false); pieza.transform.localScale=tamano;
        pieza.GetComponent<Collider>().enabled=false; Destroy(pieza.GetComponent<Collider>()); pieza.GetComponent<Renderer>().sharedMaterial=mat;
    }
    void Update()
    {
        if(!Vivo) return;
        visual.localRotation=Quaternion.Euler(Time.unscaledTime*18,Time.unscaledTime*40,45);
        visual.localPosition=Vector3.up*Mathf.Sin(Time.unscaledTime*2)*bloque*.15f;
        if(arena.Dragon!=null) { rayo.enabled=arena.Dragon.Vida>0 && arena.CristalCurador()==this; rayo.SetPosition(0,transform.position); rayo.SetPosition(1,arena.Dragon.transform.position); }
    }
    public void RecibirDano()
    {
        if(!Vivo) return;
        DesactivarSinExplosion(); EfectosBloqueVoxel.Emitir(transform.position,material); SonidosVoxel.Emitir("explosion",transform.position);
        script jugador=arena.Dragon!=null ? FindFirstObjectByType<script>() : null;
        if(jugador!=null && Vector3.Distance(jugador.transform.position,transform.position)<bloque*5) jugador.RecibirExplosion(transform.position,.65f,bloque*5,bloque*3);
        if(arena.Dragon!=null && Vector3.Distance(arena.Dragon.transform.position,transform.position)<bloque*18) arena.Dragon.RecibirDano(10,null,false);
    }
    public void DesactivarSinExplosion() { Vivo=false; GetComponent<Collider>().enabled=false; if(visual!=null) visual.gameObject.SetActive(false); if(rayo!=null) rayo.enabled=false; }
    void OnDestroy() { if(material!=null) Destroy(material); if(cristal!=null) Destroy(cristal); }
}

public class AlientoDragonVoxel : MonoBehaviour
{
    private script jugador;
    private ArenaEndVoxel arena;
    private Vector3 velocidad;
    private bool nube;
    private float siguienteDano,fin;
    private Material material;
    public static void Lanzar(Vector3 origen,Vector3 destino,ArenaEndVoxel arena,script jugador)
    {
        var objeto=GameObject.CreatePrimitive(PrimitiveType.Sphere); objeto.name="Aliento violeta del dragon"; objeto.transform.position=origen;
        objeto.transform.SetParent(arena.transform,true); objeto.transform.localScale=Vector3.one*.9f; objeto.GetComponent<Collider>().enabled=false; Destroy(objeto.GetComponent<Collider>());
        var aliento=objeto.AddComponent<AlientoDragonVoxel>(); aliento.jugador=jugador; aliento.arena=arena; aliento.velocidad=(destino-origen).normalized*24;
        aliento.material=new Material(Shader.Find("Unlit/Color")) {color=new Color(.72f,.07f,.8f)};
        objeto.GetComponent<Renderer>().sharedMaterial=aliento.material; aliento.fin=Time.time+8;
    }
    void Update()
    {
        if(Time.time>=fin) { Destroy(gameObject); return; }
        if(jugador==null || jugador.Muerto) return;
        if(!nube)
        {
            Vector3 paso=velocidad*Time.deltaTime;
            RaycastHit impacto=new RaycastHit(); float cercano=float.PositiveInfinity;
            foreach(var hit in Physics.SphereCastAll(transform.position,.6f,paso.normalized,paso.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(hit.collider.GetComponentInParent<DragonEndVoxel>()==null && hit.distance<cercano) { impacto=hit; cercano=hit.distance; }
            if(impacto.collider!=null)
            {
                nube=true; transform.position=impacto.point; transform.localScale=new Vector3(jugador.Bloque*5,.18f,jugador.Bloque*5); fin=Time.time+6;
                if(impacto.collider.GetComponentInParent<script>()!=null) jugador.RecibirAlientoDragon(2,transform.position-velocidad);
                transform.position=new Vector3(transform.position.x,arena.Centro.y+jugador.Bloque*.55f,transform.position.z);
            }
            else transform.position+=paso;
        }
        else if(Time.time>=siguienteDano && Vector3.Distance(jugador.GetComponent<Collider>().ClosestPoint(transform.position),transform.position)<jugador.Bloque*2.7f)
        { siguienteDano=Time.time+.8f; jugador.RecibirAlientoDragon(2,transform.position); }
    }
    void OnDestroy() { if(material!=null) Destroy(material); }
}
