using System.Collections.Generic;
using UnityEngine;

public partial class MundoVoxel
{
    // La presión pierde energía al atravesar materiales. Se decide todo antes
    // de modificar la malla, para que los bloques exteriores protejan el interior.
    public int DestruirPorExplosion(Vector3 origen, float radioEnBloques)
    {
        if(!Application.isPlaying || alturas==null || EstaEnAgua(origen)) return 0;
        float potencia=Mathf.Clamp(radioEnBloques,1f,6f);
        float radio=potencia*tamanoBloque, paso=tamanoBloque*.3f;
        var afectados=new HashSet<Vector3Int>();
        var madera=new Dictionary<Vector3Int,Collider>();
        foreach(var tronco in troncos)
            if(tronco.Key!=null && Vector3.Distance(tronco.Key.ClosestPoint(origen),origen)<radio)
                madera[CeldaEn(transform.InverseTransformPoint(tronco.Value.transform.position))]=tronco.Key;
        var azar=new System.Random(GetInstanceID() ^ Time.frameCount ^ afectados.Count);
        const int muestras=10;
        for(int x=0;x<muestras;x++)
        for(int y=0;y<muestras;y++)
        for(int z=0;z<muestras;z++)
        {
            if(x!=0 && x!=muestras-1 && y!=0 && y!=muestras-1 && z!=0 && z!=muestras-1) continue;
            Vector3 rumbo=new Vector3(x/(muestras-1f)*2f-1f,y/(muestras-1f)*2f-1f,z/(muestras-1f)*2f-1f).normalized;
            float energia=potencia*(.7f+(float)azar.NextDouble()*.6f);
            for(float distancia=0;distancia<=radio && energia>0;distancia+=paso)
            {
                Vector3 punto=origen+rumbo*distancia;
                if(EstaEnAgua(punto)) break;
                Vector3Int celda=CeldaEn(transform.InverseTransformPoint(punto));
                bool tronco=madera.ContainsKey(celda);
                bool bloque=ExisteBloque(celda);
                if(bloque || tronco)
                {
                    if(celda.y<=FondoRoca) break;
                    float resistencia=ResistenciaExplosion(tronco ? TipoBloque.Tronco : TipoEn(celda));
                    energia-=(resistencia+.3f)*.3f;
                    if(energia>0) afectados.Add(celda);
                }
                energia-=.225f;
            }
        }
        var botin=new Dictionary<IdObjetoVoxel,int>();
        int fragmentos=0, destruidos=0;
        foreach(var celda in afectados)
        {
            TipoBloque tipo;
            Vector3 centro=CentroCelda(celda);
            if(madera.TryGetValue(celda,out var collider))
            {
                if(collider==null || !troncos.TryGetValue(collider,out var pieza)) continue;
                tipo=TipoBloque.Tronco; centro=pieza.transform.position;
                troncosQuitados.Add(transform.InverseTransformPoint(centro)); troncos.Remove(collider);
                pieza.SetActive(false); Destroy(pieza);
            }
            else
            {
                if(!ExisteBloque(celda)) continue;
                tipo=TipoEn(celda);
                if(funcionales.TryGetValue(celda,out var estacion))
                {
                    estacion.SoltarContenido(); estacion.gameObject.SetActive(false);
                    Destroy(estacion.gameObject); funcionales.Remove(celda);
                }
                if(!bloquesColocados.Remove(celda)) bloquesQuitados.Add(celda);
                MarcarCeldaModificada(celda);
                if(celda.y==alturas[celda.x,celda.z])
                {
                    plantasQuitadas.Add(new Vector2Int(celda.x,celda.z));
                }
            }
            destruidos++;
            if(fragmentos++<12) EfectosBloqueVoxel.Emitir(centro,MaterialDe(tipo));
            // Parte de los materiales se pierde; los contenidos de cofres y hornos se conservan.
            if(azar.NextDouble()>.33) continue;
            IdObjetoVoxel id=BotinExplosion(tipo);
            if(id!=IdObjetoVoxel.Ninguno) botin[id]=botin.TryGetValue(id,out int cantidad) ? cantidad+1 : 1;
        }
        if(destruidos==0) return 0;
        ActualizarSectoresModificados();
        foreach(var par in botin)
        {
            int cantidad=par.Value, limite=CatalogoObjetosVoxel.De(par.Key).limite;
            while(cantidad>0)
            {
                int pila=Mathf.Min(cantidad,limite); cantidad-=pila;
                ObjetoRecogibleVoxel.SoltarObjeto(origen+Vector3.up*.3f+Random.insideUnitSphere*tamanoBloque*.5f,new PilaObjetoVoxel(par.Key,pila));
            }
        }
        OcultarGrietas(); Physics.SyncTransforms(); return destruidos;
    }

    static float ResistenciaExplosion(TipoBloque tipo)
    {
        switch(tipo)
        {
            case TipoBloque.Tierra: case TipoBloque.Arena: return .5f;
            case TipoBloque.Vidrio: return .3f;
            case TipoBloque.Madera: case TipoBloque.Tronco: case TipoBloque.MesaTrabajo: return 2f;
            case TipoBloque.Cofre: return 2.5f;
            default: return 6f;
        }
    }
    static IdObjetoVoxel BotinExplosion(TipoBloque tipo)
    {
        switch(tipo)
        {
            case TipoBloque.Piedra: return IdObjetoVoxel.Adoquin;
            case TipoBloque.Carbon: return IdObjetoVoxel.Carbon;
            case TipoBloque.Hierro: return IdObjetoVoxel.HierroBruto;
            case TipoBloque.Diamante: return IdObjetoVoxel.Diamante;
            case TipoBloque.Vidrio: return IdObjetoVoxel.Ninguno;
            default: return CatalogoObjetosVoxel.DeBloque(tipo);
        }
    }

    // Muestrea varias partes del cuerpo: una pared reduce la exposición,
    // y un personaje parcialmente asomado recibe solo parte de la onda.
    public float ExposicionExplosion(Vector3 origen, Collider cuerpo)
    {
        if(cuerpo==null) return 0f;
        Bounds limites=cuerpo.bounds; int visibles=0, muestras=0;
        for(int y=0;y<3;y++)
        for(int x=-1;x<=1;x+=2)
        {
            Vector3 punto=limites.center+new Vector3(limites.extents.x*x*.6f,limites.extents.y*(y-1)*.75f,0);
            Vector3 rumbo=punto-origen; bool libre=true; muestras++;
            foreach(var impacto in Physics.RaycastAll(origen,rumbo.normalized,rumbo.magnitude,
                Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                Collider otro=impacto.collider;
                if(otro==cuerpo || otro.transform.IsChildOf(cuerpo.transform) ||
                    otro.GetComponentInParent<enemigo>()!=null || otro.GetComponentInParent<AnimalVoxel>()!=null ||
                    otro.GetComponentInParent<script>()!=null || otro.GetComponentInParent<ObjetoRecogibleVoxel>()!=null) continue;
                libre=false; break;
            }
            if(libre) visibles++;
        }
        return (float)visibles/muestras;
    }
}

public partial class script
{
    private Vector3 impulsoExplosion;
    private float finControlExplosion;
    public void RecibirExplosion(Vector3 origen,float intensidad,float empujeHorizontal,float elevacion)
    {
        if(terminado || intensidad<=0f) return;
        intensidad=Mathf.Clamp01(intensidad);
        AplicarDano(Mathf.CeilToInt(vidaMaxima*.5f*intensidad),false,origen,true);
        if(terminado) return;
        Vector3 rumbo=rb.position-origen; rumbo.y=0;
        if(rumbo.sqrMagnitude<.001f) rumbo=-transform.forward;
        velocidadMovimiento=Vector3.zero;
        finControlExplosion=Time.time+Mathf.Lerp(.12f,.45f,intensidad);
        impulsoExplosion+=rumbo.normalized*empujeHorizontal*intensidad;
        impulsoExplosion=Vector3.ClampMagnitude(impulsoExplosion,Bloque*9f);
        rb.velocity=new Vector3(velocidadMovimiento.x+impulsoGolpe.x+impulsoExplosion.x,
            Mathf.Max(rb.velocity.y,0f)+elevacion*intensidad,
            velocidadMovimiento.z+impulsoGolpe.z+impulsoExplosion.z);
        solicitudSalto=ultimoSuelo=-10f; proximoSaltoAutomatico=Time.time+.5f;
    }
}
