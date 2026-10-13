using UnityEngine;

public class AntorchaVoxel : MonoBehaviour
{
    public Light Luz {get;private set;}
    private MundoVoxel mundo;
    private BloqueFuncionalVoxel bloque;
    private Transform soporte;
    private Mesh malla;
    private float siguienteRevision;
    public static Mesh CrearMalla()
    {
        Mesh mesh=BloqueFuncionalVoxel.CrearCuboSeisCaras();
        Vector3[] v=mesh.vertices; Vector2[] uv=mesh.uv;
        for(int i=0;i<v.Length;i++) { v[i]=Vector3.Scale(v[i],new Vector3(.14f,.65f,.14f)); uv[i]=new Vector2((7f+uv[i].x*2f)/16f,uv[i].y*10f/16f); }
        mesh.vertices=v; mesh.uv=uv; mesh.RecalculateBounds(); return mesh;
    }
    public void Configurar(MundoVoxel propietario,BloqueFuncionalVoxel celda)
    {
        mundo=propietario; bloque=celda;
        soporte=new GameObject("Antorcha de madera").transform; soporte.SetParent(transform,false);
        malla=CrearMalla(); soporte.gameObject.AddComponent<MeshFilter>().sharedMesh=malla;
        soporte.gameObject.AddComponent<MeshRenderer>().sharedMaterials=mundo.MaterialesEspeciales(TipoBloque.Antorcha,false);
        soporte.localScale=Vector3.one*mundo.TamanoBloque;
        Luz=new GameObject("Luz calida de antorcha").AddComponent<Light>(); Luz.transform.SetParent(soporte,false);
        Luz.transform.localPosition=Vector3.up*.32f; Luz.type=LightType.Point; Luz.range=mundo.TamanoBloque*7;
        Luz.color=new Color(1,.65f,.27f); Luz.intensity=2.4f; Luz.shadows=LightShadows.None; Luz.renderMode=LightRenderMode.ForceVertex;
        AjustarMontaje(celda.giro);
    }
    public void AjustarMontaje(int giro)
    {
        bloque.giro=giro; transform.localRotation=Quaternion.Euler(0,(giro%4)*90,0);
        bool pared=giro>=4;
        soporte.localPosition=(pared ? new Vector3(0,-.04f,-.28f) : Vector3.down*.175f)*mundo.TamanoBloque;
        soporte.localRotation=Quaternion.Euler(pared ? -25 : 0,0,0);
        BoxCollider collider=GetComponent<BoxCollider>(); collider.size=new Vector3(.22f,.72f,.32f)*mundo.TamanoBloque; collider.center=soporte.localPosition;
    }
    void Update()
    {
        if(Time.time<siguienteRevision || mundo==null) return;
        siguienteRevision=Time.time+.3f;
        if(!mundo.AntorchaApoyada(bloque.celda,bloque.giro)) { mundo.RetirarAntorcha(bloque.celda); return; }
        Camera camara=Camera.main; Luz.enabled=camara!=null && Vector3.Distance(camara.transform.position,transform.position)<mundo.TamanoBloque*22;
        Luz.intensity=2.35f+Mathf.Sin(Time.time*7f+transform.position.x)*.13f;
    }
    void OnDestroy() { if(malla!=null) Destroy(malla); }
}

public partial class MundoVoxel
{
    bool OcupaCara(Vector3Int celda) => ExisteBloque(celda) && TipoEn(celda)!=TipoBloque.Antorcha;
    public bool AntorchaApoyada(Vector3Int celda,int giro)
    {
        Vector3 direccion=giro>=4 ? Quaternion.Euler(0,(giro%4)*90,0)*Vector3.back : Vector3.down;
        var apoyo=celda+Vector3Int.RoundToInt(direccion);
        if(ExisteBloque(apoyo) && TipoEn(apoyo)!=TipoBloque.Antorcha) return true;
        Vector3 origen=CentroCelda(celda);
        foreach(var impacto in Physics.RaycastAll(origen,direccion,tamanoBloque*.8f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if(impacto.collider.GetComponentInParent<ArenaEndVoxel>()!=null && impacto.collider.GetComponentInParent<AntorchaVoxel>()==null &&
                impacto.collider.GetComponentInParent<DragonEndVoxel>()==null && impacto.collider.GetComponentInParent<CristalEndVoxel>()==null) return true;
        return false;
    }
    public void RetirarAntorcha(Vector3Int celda)
    {
        if(!bloquesColocados.TryGetValue(celda,out var tipo) || tipo!=TipoBloque.Antorcha) return;
        bloquesColocados.Remove(celda); ObjetoRecogibleVoxel.SoltarObjeto(CentroCelda(celda),new PilaObjetoVoxel(IdObjetoVoxel.Antorcha));
        ActualizarCelda(celda);
    }
}
