using System.Collections.Generic;
using UnityEngine;

// Efectos sintéticos breves: no requieren archivos de sonido ni música externa.
public class SonidosVoxel : MonoBehaviour
{
    private static SonidosVoxel instancia;
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private AudioSource fuente;
    private script jugador;
    private Rigidbody fisica;
    private Vector3 ultimoPaso;

    void Awake()
    {
        instancia = this;
        jugador = GetComponent<script>();
        fisica = GetComponent<Rigidbody>();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        ultimoPaso = transform.position;
        Crear("paso", .095f, 125f, .85f);
        Crear("picar", .07f, 180f, .85f);
        Crear("romper", .19f, 95f, .95f);
        Crear("poner", .09f, 100f, .6f);
        Crear("golpe", .14f, 260f, .85f);
        Crear("herida", .19f, 90f, .55f);
        Crear("comer", .11f, 210f, .85f);
        Crear("recoger", .11f, 1000f, .05f);
        Crear("explosion", .65f, 40f, .95f);
    }

    void Crear(string nombre, float duracion, float tono, float ruido)
    {
        const int muestras = 22050;
        float[] datos = new float[Mathf.CeilToInt(duracion * muestras)];
        System.Random azar = new System.Random(nombre.Length * 971 + Mathf.RoundToInt(tono));
        float filtro = 0f;
        for (int i = 0; i < datos.Length; i++)
        {
            float t = i / (float)muestras;
            float amplitud = Mathf.Pow(1f - i / (float)datos.Length, 2f) * Mathf.Min(1f, t * 500f);
            filtro = Mathf.Lerp(filtro, (float)azar.NextDouble() * 2f - 1f, .22f);
            datos[i] = (filtro * ruido + Mathf.Sin(t * tono * Mathf.PI * 2f) * (1f - ruido)) * amplitud * .5f;
        }
        AudioClip clip = AudioClip.Create(nombre, datos.Length, 1, muestras, false);
        clip.SetData(datos, 0);
        clips[nombre] = clip;
    }

    public static void Emitir(string nombre, Vector3 posicion)
    {
        if (instancia == null || instancia.jugador == null || !instancia.clips.ContainsKey(nombre)) return;
        float distancia = Vector3.Distance(posicion, instancia.transform.position);
        float volumen = instancia.jugador.volumenEfectos / (1f + distancia * .08f);
        instancia.fuente.pitch = nombre == "recoger" ? 1f : Random.Range(.9f, 1.1f);
        instancia.fuente.PlayOneShot(instancia.clips[nombre], volumen);
    }

    void Update()
    {
        if (jugador == null || jugador.InterfazAbierta || !jugador.EstaEnSuelo || jugador.Agachado ||
            fisica.velocity.sqrMagnitude < .2f) { ultimoPaso = transform.position; return; }
        Vector3 recorrido = transform.position - ultimoPaso;
        recorrido.y = 0f;
        if (recorrido.magnitude < jugador.Bloque * .65f) return;
        ultimoPaso = transform.position;
        Emitir("paso", transform.position);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
        foreach (AudioClip clip in clips.Values) Destroy(clip);
    }
}
