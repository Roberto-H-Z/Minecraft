using UnityEngine;

public class FlechaFuegoVoxel : MonoBehaviour
{
    private Vector3 velocidad;
    private Transform propietario;
    private int dano;
    private bool impacto;
    private Material material;
    public static FlechaFuegoVoxel Crear(Vector3 origen,Vector3 rumbo,Transform propietario,float rapidez,int dano)
    {
        var objeto=GameObject.CreatePrimitive(PrimitiveType.Cube); objeto.name="Flecha de fuego";
        objeto.layer=2; objeto.transform.position=origen; objeto.transform.rotation=Quaternion.LookRotation(rumbo);
        objeto.transform.localScale=new Vector3(.05f,.05f,.9f); objeto.GetComponent<Collider>().enabled=false; Destroy(objeto.GetComponent<Collider>());
        var flecha=objeto.AddComponent<FlechaFuegoVoxel>(); flecha.velocidad=rumbo*rapidez; flecha.propietario=propietario; flecha.dano=dano;
        flecha.material=new Material(Shader.Find("Standard")) {color=new Color(.9f,.55f,.15f)};
        flecha.material.EnableKeyword("_EMISSION"); flecha.material.SetColor("_EmissionColor",new Color(1,.22f,.015f)*2);
        objeto.GetComponent<Renderer>().sharedMaterial=flecha.material;
        var rastro=objeto.AddComponent<TrailRenderer>(); rastro.sharedMaterial=flecha.material; rastro.time=.12f; rastro.startWidth=.17f; rastro.endWidth=0;
        Destroy(objeto,8); return flecha;
    }
    void FixedUpdate()
    {
        if(impacto) return;
        Vector3 trayecto=velocidad*Time.fixedDeltaTime; float cerca=float.PositiveInfinity; Collider blanco=null; Vector3 punto=Vector3.zero;
        foreach(var hit in Physics.SphereCastAll(transform.position,.13f,trayecto.normalized,trayecto.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if((propietario==null || !hit.collider.transform.IsChildOf(propietario)) && hit.distance<cerca)
            { cerca=hit.distance; blanco=hit.collider; punto=hit.point; }
        if(blanco!=null)
        {
            transform.position=punto; impacto=true;
            var cristal=blanco.GetComponentInParent<CristalEndVoxel>(); var dragon=blanco.GetComponentInParent<DragonEndVoxel>();
            if(cristal!=null) cristal.RecibirDano();
            else if(dragon!=null) dragon.RecibirDano(dano,blanco,true);
            else
            {
                var monstruo=blanco.GetComponentInParent<enemigo>(); var animal=blanco.GetComponentInParent<AnimalVoxel>();
                if(monstruo!=null) { monstruo.RecibirDano(1,propietario!=null ? propietario.position : punto-velocidad.normalized); IncendioFlechaVoxel.Encender(monstruo.gameObject); }
                if(animal!=null) { animal.RecibirDano(1,propietario!=null ? propietario.position : punto-velocidad.normalized); IncendioFlechaVoxel.Encender(animal.gameObject); }
            }
            Destroy(gameObject,.2f); return;
        }
        transform.position+=trayecto; velocidad+=Physics.gravity*Time.fixedDeltaTime*.45f;
        transform.rotation=Quaternion.LookRotation(velocidad);
    }
    void OnDestroy() { if(material!=null) Destroy(material); }
}

public class IncendioFlechaVoxel : MonoBehaviour
{
    private float fin,siguiente;
    public static void Encender(GameObject objeto)
    {
        var incendio=objeto.GetComponent<IncendioFlechaVoxel>(); if(incendio==null) incendio=objeto.AddComponent<IncendioFlechaVoxel>();
        incendio.fin=Time.time+3.2f; incendio.siguiente=Time.time+1f;
    }
    void Update()
    {
        if(Time.time>fin) { Destroy(this); return; }
        if(Time.time<siguiente) return; siguiente=Time.time+1;
        var enemigo=GetComponent<enemigo>(); var animal=GetComponent<AnimalVoxel>();
        if(enemigo!=null) enemigo.RecibirDano(1,transform.position-Vector3.up);
        if(animal!=null) animal.RecibirDano(1,transform.position-Vector3.up);
    }
}
