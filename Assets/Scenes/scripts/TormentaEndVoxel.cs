using UnityEngine;
using UnityEngine.Rendering;

// Un emisor de lluvia y un rayo reutilizable; sin colliders ni reconstrucciones del terreno.
public class TormentaEndVoxel : MonoBehaviour
{
    public ParticleSystem Lluvia { get; private set; }
    public bool RelampagoVisible => rayo != null && rayo.enabled;
    private ArenaEndVoxel arena;
    private Material materialLluvia, materialRayo;
    private LineRenderer rayo;
    private Light sol;
    private Color colorSol;
    private float intensidadSol, siguienteRayo, inicioRayo=-100f, truenoPendiente=-1f, reloj;
    private bool ambienteCapturado;
    private AudioSource audioTrueno;
    private AudioClip trueno;
    private readonly Vector3[] puntosRayo=new Vector3[13];
    private readonly System.Random azar=new System.Random(7241);

    public void Configurar(ArenaEndVoxel isla)
    {
        arena=isla;
        var gotas=new GameObject("Lluvia violeta del End"); gotas.transform.SetParent(transform,false);
        Lluvia=gotas.AddComponent<ParticleSystem>(); Lluvia.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var principal=Lluvia.main;
        principal.loop=true; principal.playOnAwake=false; principal.duration=3f;
        principal.startLifetime=1.5f; principal.startSpeed=0; principal.startSize=new ParticleSystem.MinMaxCurve(.025f,.055f);
        principal.startColor=new Color(.67f,.56f,.86f,.44f);
        principal.maxParticles=700; principal.simulationSpace=ParticleSystemSimulationSpace.World;
        principal.useUnscaledTime=false;
        var emision=Lluvia.emission; emision.rateOverTime=400f;
        var forma=Lluvia.shape; forma.shapeType=ParticleSystemShapeType.Box; forma.scale=new Vector3(52,1,52);
        var velocidad=Lluvia.velocityOverLifetime; velocidad.enabled=true; velocidad.space=ParticleSystemSimulationSpace.World;
        velocidad.x=4f; velocidad.y=-28f; velocidad.z=1.5f;
        materialLluvia=new Material(Shader.Find("Sprites/Default")) {name="Gotas violetas transparentes"};
        var renderer=gotas.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=materialLluvia;
        renderer.renderMode=ParticleSystemRenderMode.Stretch; renderer.velocityScale=.055f; renderer.lengthScale=1.4f;
        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;

        var objetoRayo=new GameObject("Relampago sobre la isla"); objetoRayo.transform.SetParent(transform,false);
        rayo=objetoRayo.AddComponent<LineRenderer>(); rayo.useWorldSpace=true; rayo.positionCount=puntosRayo.Length;
        rayo.startWidth=.30f; rayo.endWidth=.10f; rayo.numCornerVertices=0;
        materialRayo=new Material(Shader.Find("Unlit/Color")) {color=new Color(.87f,.72f,1)};
        rayo.sharedMaterial=materialRayo; rayo.shadowCastingMode=ShadowCastingMode.Off; rayo.receiveShadows=false; rayo.enabled=false;
        audioTrueno=gameObject.AddComponent<AudioSource>(); audioTrueno.playOnAwake=false;
        audioTrueno.spatialBlend=0f; audioTrueno.volume=.32f; audioTrueno.priority=180;
        CrearTrueno();
    }

    public void Comenzar()
    {
        if(ambienteCapturado) return;
        foreach(var luz in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(luz.type==LightType.Directional) { sol=luz; break; }
        if(sol!=null) { colorSol=sol.color; intensidadSol=sol.intensity; }
        ambienteCapturado=true; reloj=0; inicioRayo=-100; siguienteRayo=2.4f; truenoPendiente=-1;
        Lluvia.Clear(true); Lluvia.Play(true);
    }

    // Se llama despues del ciclo del cielo y de colocar la camara cinematica.
    public void Actualizar(Camera camara,bool cinematica)
    {
        if(!ambienteCapturado || camara==null) return;
        float dt=cinematica ? Time.unscaledDeltaTime : Time.deltaTime;
        reloj+=dt;
        Lluvia.transform.position=camara.transform.position+Vector3.up*24f;
        // Durante la cinematica el juego esta detenido, pero la tormenta sigue animandose.
        var principal=Lluvia.main; principal.useUnscaledTime=cinematica;
        if(reloj>=siguienteRayo)
        {
            LanzarRelampago(); siguienteRayo=reloj+6f+(float)azar.NextDouble()*5f;
        }
        float t=reloj-inicioRayo;
        float destello=t>=0 && t<.30f ? (t<.09f ? 1f-t/.09f : t>.16f ? (1f-(t-.16f)/.14f)*.6f : 0) : 0;
        rayo.enabled=destello>.03f;
        camara.clearFlags=CameraClearFlags.SolidColor;
        camara.backgroundColor=Color.Lerp(new Color(.055f,.022f,.095f),new Color(.40f,.31f,.53f),destello*.7f);
        RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogColor=Color.Lerp(new Color(.16f,.095f,.23f),new Color(.46f,.35f,.57f),destello*.6f);
        RenderSettings.fogStartDistance=24f; RenderSettings.fogEndDistance=165f;
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=Color.Lerp(new Color(.53f,.44f,.65f),new Color(.82f,.74f,.93f),destello);
        if(sol!=null) { sol.color=new Color(.73f,.60f,.92f); sol.intensity=.65f+destello*.85f; }
        if(truenoPendiente>=0 && reloj>=truenoPendiente)
        { audioTrueno.PlayOneShot(trueno); truenoPendiente=-1; }
    }

    void LanzarRelampago()
    {
        if(arena==null) return;
        int indice=azar.Next(arena.Cristales.Length);
        Vector3 final=arena.Cristales[indice].transform.position;
        Vector3 cima=final+new Vector3((float)azar.NextDouble()*14f-7f,65f,(float)azar.NextDouble()*14f-7f);
        for(int i=0;i<puntosRayo.Length;i++)
        {
            float avance=i/(float)(puntosRayo.Length-1);
            Vector3 quiebre=i==0 || i==puntosRayo.Length-1 ? Vector3.zero
                : new Vector3((float)azar.NextDouble()*7f-3.5f,0,(float)azar.NextDouble()*7f-3.5f);
            puntosRayo[i]=Vector3.Lerp(cima,final,avance)+quiebre;
        }
        rayo.SetPositions(puntosRayo); inicioRayo=reloj; truenoPendiente=reloj+.6f;
        // Efecto ambiental: el rayo no modifica los cristales, los bloques ni la vida.
    }

    void CrearTrueno()
    {
        const int frecuencia=22050; var muestras=new float[frecuencia*2];
        var ruido=new System.Random(48); float grave=0;
        for(int i=0;i<muestras.Length;i++)
        {
            float tiempo=i/(float)frecuencia;
            grave=Mathf.Lerp(grave,(float)ruido.NextDouble()*2-1,.025f);
            float envolvente=Mathf.Clamp01(tiempo/.03f)*Mathf.Exp(-tiempo*2.2f);
            muestras[i]=(grave*2f+Mathf.Sin(tiempo*95f)*.10f)*envolvente;
        }
        trueno=AudioClip.Create("Trueno lejano del End",muestras.Length,1,frecuencia,false); trueno.SetData(muestras,0);
    }
    void OnDisable()
    {
        if(Lluvia!=null) Lluvia.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(rayo!=null) rayo.enabled=false;
        if(audioTrueno!=null) audioTrueno.Stop();
        if(ambienteCapturado && sol!=null) { sol.color=colorSol; sol.intensity=intensidadSol; }
        ambienteCapturado=false;
    }
    void OnDestroy()
    {
        if(materialLluvia!=null) Destroy(materialLluvia);
        if(materialRayo!=null) Destroy(materialRayo);
        if(trueno!=null) Destroy(trueno);
    }
}
