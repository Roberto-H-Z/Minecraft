using UnityEngine;

public partial class script
{
    public readonly IdObjetoVoxel[] TiposArmadura={IdObjetoVoxel.CascoDiamante,IdObjetoVoxel.PetoDiamante,IdObjetoVoxel.PantalonesDiamante,IdObjetoVoxel.BotasDiamante};
    private PilaObjetoVoxel[] armadura=PilasVoxel.Crear(4);
    private float danoParcial, corazonesAbsorcion, absorcionHasta, regeneracionManzanaHasta, proteccionManzanaHasta, siguienteRegenManzana;
    private bool preparandoArco;
    private float inicioArco;
    public bool TieneArco => HerramientaActual.herramienta==ClaseHerramientaVoxel.Arco;
    public float CargaArco => preparandoArco ? Mathf.Clamp01((Time.time-inicioArco)/.8f) : 0f;
    public float VidaVisible => vidas-danoParcial;
    public float Absorcion => corazonesAbsorcion;
    public int PuntosArmadura
    {
        get { int total=0; int[] puntos={3,8,6,3}; for(int i=0;i<4;i++) if(!armadura[i].Vacia) total+=puntos[i]; return total; }
    }
    public bool PiezaArmadura(int indice) => !armadura[indice].Vacia;
    public bool PuedeComerSeleccionado => EsComidaEnSlot(slotSeleccionado) && (hambre<20 || PilaEnSlot(slotSeleccionado).id==IdObjetoVoxel.ManzanaEncantada);
    public Texture2D IconoHerramientaActual
    {
        get
        {
            var textura=TieneArco && preparandoArco ? Resources.Load<Texture2D>("Minecraft/Items/bow_pulling_"+Mathf.Min(2,(int)(CargaArco*3))) : IconoEnSlot(slotSeleccionado);
            if(textura!=null) textura.filterMode=FilterMode.Point; return textura;
        }
    }

    void InicializarEquipoAventura()
    {
        for(int i=0;i<4;i++) armadura[i]=new PilaObjetoVoxel(TiposArmadura[i]);
        inventario[3]=new PilaObjetoVoxel(IdObjetoVoxel.ArcoFuego,1,true);
        inventario[4]=new PilaObjetoVoxel(IdObjetoVoxel.Antorcha,64);
        inventario[5]=new PilaObjetoVoxel(IdObjetoVoxel.ManzanaEncantada,16);
    }
    void ActualizarEquipoAventura()
    {
        if(Time.time>absorcionHasta) corazonesAbsorcion=0;
        if(Time.time<regeneracionManzanaHasta && Time.time>=siguienteRegenManzana)
        { vidas=Mathf.Min(vidaMaxima,vidas+1); danoParcial=0; siguienteRegenManzana=Time.time+1f; }
    }
    void AplicarManzanaEncantada()
    {
        corazonesAbsorcion=8f; absorcionHasta=Time.time+120f;
        regeneracionManzanaHasta=Time.time+20f; proteccionManzanaHasta=Time.time+300f;
        siguienteRegenManzana=Time.time; hambre=Mathf.Min(20,hambre+4); danoParcial=0;
    }
    int CalcularDanoProtegido(int cantidad,bool omitirArmadura)
    {
        float dano=cantidad*(omitirArmadura ? 1f : 1f-PuntosArmadura*.04f);
        if(Time.time<proteccionManzanaHasta) dano*=.8f;
        float absorbido=Mathf.Min(corazonesAbsorcion,dano); corazonesAbsorcion-=absorbido; dano-=absorbido;
        danoParcial+=dano; int perder=Mathf.FloorToInt(danoParcial+0.0001f); danoParcial-=perder;
        if(!omitirArmadura) foreach(var pieza in armadura)
            if(!pieza.Vacia) { pieza.resistencia-=Mathf.Max(1,cantidad/2); if(pieza.resistencia<=0) pieza.Limpiar(); }
        return perder;
    }
    void RecibirDanoAmbiental(int cantidad) { AplicarDano(cantidad,false,Vector3.zero,false,true); }
    public void RecibirAlientoDragon(int cantidad,Vector3 atacante)
    { AplicarDano(cantidad,true,atacante,false,true); }
    public void MoverA(Vector3 posicion,bool reaparicion=false)
    {
        rb.position=posicion; transform.position=posicion; rb.velocity=Vector3.zero;
        impulsoGolpe=impulsoExplosion=velocidadMovimiento=Vector3.zero;
        entradaMovimiento=Vector2.zero; solicitudSalto=ultimoSuelo=-10f; cayendo=false;
        if(reaparicion) puntoReaparicion=posicion;
        Physics.SyncTransforms();
    }
    public bool Muerto => terminado;
    public void DispararArco(float carga)
    {
        if(carga<.15f || terminado) return;
        Camera camara=Camera.main; Vector3 direccion=camara!=null ? camara.transform.forward : transform.forward;
        Vector3 origen=PosicionOjos+direccion*Bloque*.45f;
        FlechaFuegoVoxel.Crear(origen,direccion,transform,Mathf.Lerp(25,70,carga)*Bloque/2f,Mathf.RoundToInt(Mathf.Lerp(5,20,carga)));
        proximoAtaque=Time.time+.25f; SonidosVoxel.Emitir("lanzar",origen);
        if(aspecto!=null) aspecto.AnimarGolpe();
    }
    void ActualizarArco()
    {
        if(!TieneArco || InterfazAbierta) { preparandoArco=false; return; }
        if(Input.GetMouseButtonDown(1)) { preparandoArco=true; inicioArco=Time.time; }
        if(Input.GetMouseButtonUp(1) && preparandoArco)
        { float carga=CargaArco; preparandoArco=false; DispararArco(carga); }
    }
    bool AtacarObjetivoEnd()
    {
        Camera camara=Camera.main; if(camara==null) return false;
        RaycastHit impacto;
        if(!Physics.Raycast(camara.transform.position,camara.transform.forward,out impacto,alcanceEspada,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) return false;
        var dragon=impacto.collider.GetComponentInParent<DragonEndVoxel>();
        var cristal=impacto.collider.GetComponentInParent<CristalEndVoxel>();
        if(dragon==null && cristal==null) return false;
        if(Time.time<proximoAtaque) return true;
        proximoAtaque=Time.time+intervaloEspada; if(aspecto!=null) aspecto.AnimarGolpe();
        int dano=HerramientaActual.herramienta==ClaseHerramientaVoxel.Espada ? 12 : 4;
        if(dragon!=null) dragon.RecibirDano(dano,impacto.collider,false);
        else cristal.RecibirDano();
        DesgastarHerramienta(); return true;
    }
    void DibujarEquipoAventura(float ancho,float alto,float barraX,float barraY)
    {
        Texture2D icono=Resources.Load<Texture2D>("Minecraft/Hud/armor_full");
        for(int i=0;i<10;i++)
            if(i*2<PuntosArmadura && icono!=null) GUI.DrawTexture(new Rect(barraX+i*18,barraY-48,18,18),icono);
        if(corazonesAbsorcion>0)
        {
            Color previo=GUI.color; GUI.color=new Color(1,.85f,.22f);
            for(int i=0;i<Mathf.CeilToInt(corazonesAbsorcion);i++) GUI.DrawTexture(new Rect(barraX+i*18,barraY-68,18,18),corazonLleno);
            GUI.color=previo;
        }
        if(TieneArco && preparandoArco)
        { DibujarRectangulo(new Rect(ancho*.5f-32,alto*.5f+28,64*CargaArco,4),new Color(1,.55f,.12f)); }
        if(GUI.Button(new Rect(ancho-172,14,158,31),"Misiones [M]")) AbrirMenuMision();
    }
}

// Se coloca como hijo de las partes existentes, conservando sus animaciones.
[DefaultExecutionOrder(300)]
public class ArmaduraDiamanteVoxel : MonoBehaviour
{
    private Renderer[] piezas;
    private int[] indices;
    private script jugador;
    private Material material, pantalon;
    public void Configurar(script propietario)
    {
        jugador=propietario;
        material=MinecraftSkinBox.CrearMaterial("diamond_armor"); pantalon=MinecraftSkinBox.CrearMaterial("diamond_leggings");
        foreach(var mat in new[]{material,pantalon}) if(mat!=null)
        { mat.SetFloat("_Mode",1); mat.SetFloat("_Cutoff",.1f); mat.EnableKeyword("_ALPHATEST_ON"); mat.renderQueue=2450; }
        string[] nombres={"Cabeza","Torso","Brazo derecho","Brazo izquierdo","Pierna derecha","Pierna izquierda"};
        var lista=new System.Collections.Generic.List<Renderer>(); var ids=new System.Collections.Generic.List<int>();
        foreach(string nombre in nombres)
        {
            Transform padre=null;
            foreach(var pieza in propietario.transform.GetComponentsInChildren<Transform>()) if(pieza.name==nombre) { padre=pieza; break; }
            if(padre==null) continue;
            bool pierna=nombre.StartsWith("Pierna"), cabeza=nombre=="Cabeza", brazo=nombre.StartsWith("Brazo");
            int id=cabeza ? 0 : pierna ? 2 : 1;
            var cubo=GameObject.CreatePrimitive(PrimitiveType.Cube); cubo.name="Armadura "+nombre; cubo.transform.SetParent(padre,false);
            cubo.transform.localScale=Vector3.one*1.035f; cubo.GetComponent<Collider>().enabled=false; Destroy(cubo.GetComponent<Collider>());
            MinecraftSkinBox.Aplicar(cubo.transform,pierna ? pantalon : material,cabeza ? 0 : brazo ? 40 : pierna ? 0 : 16,cabeza ? 0 : 16,cabeza ? 8 : brazo || pierna ? 4 : 8,cabeza ? 8 : 12,cabeza ? 8 : 4,true);
            lista.Add(cubo.GetComponent<Renderer>()); ids.Add(id);
            if(pierna)
            {
                var botas=GameObject.CreatePrimitive(PrimitiveType.Cube); botas.name="Armadura botas"; botas.transform.SetParent(padre,false); botas.transform.localScale=Vector3.one*1.055f;
                botas.GetComponent<Collider>().enabled=false; Destroy(botas.GetComponent<Collider>());
                MinecraftSkinBox.Aplicar(botas.transform,material,0,16,4,12,4,true); lista.Add(botas.GetComponent<Renderer>()); ids.Add(3);
            }
        }
        piezas=lista.ToArray(); indices=ids.ToArray();
    }
    void LateUpdate()
    {
        if(jugador==null || piezas==null) return;
        var camara=Camera.main!=null ? Camera.main.GetComponent<CamaraSeguir>() : null;
        bool visible=camara==null || !camara.EnPrimeraPersona;
        for(int i=0;i<piezas.Length;i++) if(piezas[i]!=null) piezas[i].enabled=visible && jugador.PiezaArmadura(indices[i]);
    }
    void OnDestroy()
    {
        if(material!=null) Destroy(material); if(pantalon!=null) Destroy(pantalon);
        if(piezas!=null) foreach(var pieza in piezas) if(pieza!=null) { Destroy(pieza.GetComponent<MeshFilter>().sharedMesh); Destroy(pieza.gameObject); }
    }
}
