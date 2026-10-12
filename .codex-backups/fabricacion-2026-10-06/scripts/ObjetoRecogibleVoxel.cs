using UnityEngine;

// El cuerpo cae y colisiona; sólo el modelo gira y flota ligeramente sobre él.
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class ObjetoRecogibleVoxel : MonoBehaviour
{
    private TipoBloque? bloque;
    private TipoBotin? botin;
    private int experiencia;
    private float recogerDesde;
    private script jugador;
    private Rigidbody fisica;
    private Transform modelo;
    private Material materialPropio;
    private Mesh mallaPropia;

    public static void SoltarBloque(MundoVoxel mundo, Vector3 posicion, TipoBloque tipo, float demora = .45f)
    {
        ObjetoRecogibleVoxel objeto = Crear(posicion, demora);
        objeto.bloque = tipo;
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Modelo del bloque";
        cubo.transform.SetParent(objeto.transform, false);
        cubo.transform.localScale = Vector3.one * mundo.TamanoBloque * .23f;
        cubo.layer = 2;
        cubo.GetComponent<Collider>().enabled = false;
        Destroy(cubo.GetComponent<Collider>());
        Renderer aspecto = cubo.GetComponent<Renderer>();
        if (tipo == TipoBloque.Tierra)
        {
            objeto.mallaPropia = VoxelCharacterVisual.CrearMallaTierraEnMano();
            cubo.GetComponent<MeshFilter>().sharedMesh = objeto.mallaPropia;
            aspecto.sharedMaterials = new[] { mundo.MaterialPastoSuperior, mundo.MaterialTierra, mundo.MaterialTierraPura };
        }
        else aspecto.sharedMaterial = mundo.MaterialDe(tipo);
        objeto.modelo = cubo.transform;
    }

    public static void SoltarBotin(Vector3 posicion, TipoBotin tipo, int experiencia = 1, float demora = .45f)
    {
        ObjetoRecogibleVoxel objeto = Crear(posicion, demora);
        objeto.botin = tipo;
        objeto.experiencia = experiencia;
        string[] nombres = { "mutton", "porkchop", "rotten_flesh", "bone", "gunpowder" };
        Texture2D textura = Resources.Load<Texture2D>("Minecraft/Items/" + nombres[(int)tipo]);
        objeto.materialPropio = new Material(Shader.Find("Unlit/Transparent Cutout"));
        objeto.materialPropio.mainTexture = textura;
        objeto.mallaPropia = new Mesh { name = "Botín pixelado de dos caras" };
        objeto.mallaPropia.vertices = new[] { new Vector3(-.3f, -.3f, 0), new Vector3(.3f, -.3f, 0),
            new Vector3(.3f, .3f, 0), new Vector3(-.3f, .3f, 0) };
        objeto.mallaPropia.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        objeto.mallaPropia.triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 };
        objeto.mallaPropia.RecalculateNormals();
        GameObject plano = new GameObject("Modelo del botín");
        plano.layer = 2;
        plano.transform.SetParent(objeto.transform, false);
        plano.AddComponent<MeshFilter>().sharedMesh = objeto.mallaPropia;
        plano.AddComponent<MeshRenderer>().sharedMaterial = objeto.materialPropio;
        objeto.modelo = plano.transform;
    }

    static ObjetoRecogibleVoxel Crear(Vector3 posicion, float demora)
    {
        GameObject raiz = new GameObject("Objeto para recoger");
        raiz.layer = 2;
        raiz.transform.position = posicion;
        ObjetoRecogibleVoxel objeto = raiz.AddComponent<ObjetoRecogibleVoxel>();
        objeto.recogerDesde = Time.time + demora;
        objeto.jugador = FindFirstObjectByType<script>();
        BoxCollider colision = raiz.GetComponent<BoxCollider>();
        colision.size = Vector3.one * .35f;
        objeto.fisica = raiz.GetComponent<Rigidbody>();
        objeto.fisica.mass = .15f;
        objeto.fisica.drag = 1.2f;
        objeto.fisica.constraints = RigidbodyConstraints.FreezeRotation;
        objeto.fisica.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        objeto.fisica.velocity = Vector3.up * 2f + Random.insideUnitSphere * .6f;
        if (objeto.jugador != null) Physics.IgnoreCollision(colision, objeto.jugador.GetComponent<Collider>());
        Destroy(raiz, 300f);
        return objeto;
    }

    void FixedUpdate()
    {
        fisica.useGravity = true;
        if (jugador == null || jugador.InterfazAbierta || Time.time < recogerDesde) return;
        if ((bloque.HasValue && !jugador.PuedeRecoger(bloque.Value)) ||
            (botin.HasValue && !jugador.PuedeRecoger(botin.Value))) return;
        Collider cuerpo = jugador.GetComponent<Collider>();
        Vector3 cercano = cuerpo.ClosestPoint(transform.position);
        float distancia = Vector3.Distance(cercano, transform.position);
        float alcance = jugador.Bloque * 1.6f;
        if (distancia > alcance) return;
        Vector3 destino = cuerpo.bounds.center;
        Vector3 direccion = destino - transform.position;
        RaycastHit impacto;
        if (Physics.Raycast(transform.position, direccion.normalized, out impacto, direccion.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && !impacto.collider.transform.IsChildOf(jugador.transform)) return;
        if (distancia > .5f)
        {
            fisica.useGravity = false;
            fisica.velocity = direccion.normalized * Mathf.Min(12f, 4f + direccion.magnitude * 3f);
            return;
        }
        if (bloque.HasValue) jugador.RecogerBloque(bloque.Value);
        if (botin.HasValue) jugador.RecogerBotin(botin.Value);
        jugador.GanarExperiencia(experiencia);
        SonidosVoxel.Emitir("recoger", transform.position);
        Destroy(gameObject);
    }

    void Update()
    {
        if (modelo == null) return;
        modelo.localPosition = Vector3.up * (.13f + Mathf.Sin(Time.time * 3f) * .06f);
        modelo.Rotate(0f, 75f * Time.deltaTime, 0f, Space.World);
    }

    void OnDestroy()
    {
        if (materialPropio != null) Destroy(materialPropio);
        if (mallaPropia != null) Destroy(mallaPropia);
    }
}
