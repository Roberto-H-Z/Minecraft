using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class DatosMundoVoxel
{
    public int semilla, ancho, largo;
    public float tamanoBloque;
    public List<Vector3Int> quitados = new List<Vector3Int>();
    public List<BloqueGuardadoVoxel> colocados = new List<BloqueGuardadoVoxel>();
    public List<Vector2Int> plantas = new List<Vector2Int>();
    public List<Vector3> troncos = new List<Vector3>();
}

[Serializable]
public struct BloqueGuardadoVoxel
{
    public Vector3Int celda;
    public TipoBloque tipo;
}

[Serializable]
public class PartidaVoxel
{
    public int version = 1;
    public DatosMundoVoxel mundo;
    public Vector3 posicion, reaparicion;
    public float angulo, saturacion;
    public bool agachado;
    public int vidas, hambre, experiencia, seleccion;
    public int[] bloques, botines, barraBloques, barraBotines;
}

public partial class MundoVoxel
{
    private readonly HashSet<Vector3> troncosQuitados = new HashSet<Vector3>();

    public DatosMundoVoxel ExportarMundo()
    {
        DatosMundoVoxel datos = new DatosMundoVoxel
        { semilla = semilla, ancho = ancho, largo = largo, tamanoBloque = tamanoBloque };
        datos.quitados.AddRange(bloquesQuitados);
        foreach (KeyValuePair<Vector3Int, TipoBloque> bloque in bloquesColocados)
            datos.colocados.Add(new BloqueGuardadoVoxel { celda = bloque.Key, tipo = bloque.Value });
        datos.plantas.AddRange(plantasQuitadas);
        datos.troncos.AddRange(troncosQuitados);
        return datos;
    }

    public bool Compatible(DatosMundoVoxel datos)
    {
        bool compatible = datos != null && datos.semilla == semilla && datos.ancho == ancho && datos.largo == largo &&
            Mathf.Approximately(datos.tamanoBloque, tamanoBloque) && datos.quitados != null &&
            datos.colocados != null && datos.plantas != null && datos.troncos != null;
        if (!compatible) return false;
        foreach (Vector3Int celda in datos.quitados) if (!CeldaValida(celda)) return false;
        foreach (BloqueGuardadoVoxel bloque in datos.colocados)
            if (!CeldaValida(bloque.celda) || (int)bloque.tipo < 0 || (int)bloque.tipo > 7) return false;
        foreach (Vector2Int celda in datos.plantas)
            if (celda.x < 0 || celda.x >= ancho || celda.y < 0 || celda.y >= largo) return false;
        foreach (Vector3 posicion in datos.troncos)
            if (!PosicionFinita(posicion)) return false;
        return true;
    }

    bool CeldaValida(Vector3Int celda) => celda.x >= 0 && celda.x < ancho && celda.z >= 0 &&
        celda.z < largo && celda.y > FondoRoca && celda.y <= 32;
    public static bool PosicionFinita(Vector3 posicion) => !float.IsNaN(posicion.x) && !float.IsInfinity(posicion.x) &&
        !float.IsNaN(posicion.y) && !float.IsInfinity(posicion.y) && !float.IsNaN(posicion.z) && !float.IsInfinity(posicion.z);

    public void ImportarMundo(DatosMundoVoxel datos)
    {
        // Regenera la misma semilla antes de restaurar, incluso si se talaron otros árboles tras guardar.
        Generar();
        bloquesQuitados.Clear();
        bloquesQuitados.UnionWith(datos.quitados);
        bloquesColocados.Clear();
        foreach (BloqueGuardadoVoxel bloque in datos.colocados) bloquesColocados[bloque.celda] = bloque.tipo;
        plantasQuitadas.Clear();
        plantasQuitadas.UnionWith(datos.plantas);
        troncosQuitados.UnionWith(datos.troncos);
        List<Collider> quitar = new List<Collider>();
        foreach (KeyValuePair<Collider, GameObject> tronco in troncos)
        {
            Vector3 posicion = transform.InverseTransformPoint(tronco.Value.transform.position);
            if (!troncosQuitados.Contains(posicion)) continue;
            tronco.Value.SetActive(false);
            Destroy(tronco.Value);
            quitar.Add(tronco.Key);
        }
        foreach (Collider tronco in quitar) troncos.Remove(tronco);
        ReconstruirSuelo();
        CrearVegetacion(raizGenerada);
        CieloVoxel cielo = GetComponent<CieloVoxel>();
        if (cielo != null) cielo.BuscarNubes();
        OcultarGrietas();
        foreach (ObjetoRecogibleVoxel objeto in FindObjectsByType<ObjetoRecogibleVoxel>(FindObjectsSortMode.None)) Destroy(objeto.gameObject);
    }
}

public partial class script
{
    private string RutaGuardado => Path.Combine(Application.persistentDataPath, "mundo-rodolfo.json");

    void GuardarPartida()
    {
        if (mundo == null) return;
        PartidaVoxel datos = new PartidaVoxel
        {
            mundo = mundo.ExportarMundo(), posicion = rb.position, reaparicion = puntoReaparicion,
            angulo = camaraSeguir != null ? camaraSeguir.AnguloHorizontal : transform.eulerAngles.y,
            vidas = vidas, hambre = hambre, saturacion = saturacion, experiencia = experiencia,
            agachado = agachado,
            seleccion = slotSeleccionado, bloques = new int[OrdenBloques.Length],
            botines = new int[OrdenBotines.Length], barraBloques = new int[9], barraBotines = new int[9]
        };
        for (int i = 0; i < OrdenBloques.Length; i++) datos.bloques[i] = CantidadDe(OrdenBloques[i]);
        for (int i = 0; i < OrdenBotines.Length; i++) datos.botines[i] = CantidadBotin(OrdenBotines[i]);
        for (int i = 0; i < 9; i++)
        {
            datos.barraBloques[i] = bloquesBarra[i].HasValue ? (int)bloquesBarra[i].Value : -1;
            datos.barraBotines[i] = botinesBarra[i].HasValue ? (int)botinesBarra[i].Value : -1;
        }
        try
        {
            string temporal = RutaGuardado + ".tmp";
            File.WriteAllText(temporal, JsonUtility.ToJson(datos, true));
            if (File.Exists(RutaGuardado)) File.Replace(temporal, RutaGuardado, null);
            else File.Move(temporal, RutaGuardado);
            MostrarAviso("Mundo e inventario guardados.");
        }
        catch (Exception error) { MostrarAviso("No se pudo guardar."); Debug.LogException(error); }
    }

    void CargarPartida()
    {
        try
        {
            if (!File.Exists(RutaGuardado)) { MostrarAviso("Todavía no hay un mundo guardado."); return; }
            PartidaVoxel datos = JsonUtility.FromJson<PartidaVoxel>(File.ReadAllText(RutaGuardado));
            if (datos == null || datos.version != 1 || !mundo.Compatible(datos.mundo) ||
                datos.bloques == null || datos.bloques.Length != OrdenBloques.Length ||
                datos.botines == null || datos.botines.Length != OrdenBotines.Length ||
                datos.barraBloques == null || datos.barraBloques.Length != 9 ||
                datos.barraBotines == null || datos.barraBotines.Length != 9 ||
                !MundoVoxel.PosicionFinita(datos.posicion) || !MundoVoxel.PosicionFinita(datos.reaparicion) ||
                float.IsNaN(datos.angulo) || float.IsInfinity(datos.angulo) ||
                float.IsNaN(datos.saturacion) || float.IsInfinity(datos.saturacion))
            { MostrarAviso("El guardado no corresponde a este terreno."); return; }
            mundo.ImportarMundo(datos.mundo);
            for (int i = 0; i < OrdenBloques.Length; i++)
                CambiarCantidadBloque(OrdenBloques[i], Mathf.Max(0, datos.bloques[i]) - CantidadDe(OrdenBloques[i]));
            botines.Clear();
            comida = 0;
            for (int i = 0; i < OrdenBotines.Length; i++)
            {
                botines[OrdenBotines[i]] = Mathf.Max(0, datos.botines[i]);
                if (EsComestible(OrdenBotines[i])) comida += botines[OrdenBotines[i]];
            }
            for (int i = 3; i < 9; i++)
            {
                int bloque = datos.barraBloques[i], botin = datos.barraBotines[i];
                bloquesBarra[i] = bloque >= 0 && bloque < OrdenBloques.Length ? (TipoBloque?)bloque : null;
                botinesBarra[i] = !bloquesBarra[i].HasValue && botin >= 0 && botin < OrdenBotines.Length ? (TipoBotin?)botin : null;
            }
            vidas = Mathf.Clamp(datos.vidas, 1, vidaMaxima);
            hambre = Mathf.Clamp(datos.hambre, 0, 20);
            saturacion = Mathf.Clamp(datos.saturacion, 0f, hambre);
            experiencia = Mathf.Max(0, datos.experiencia);
            slotSeleccionado = Mathf.Clamp(datos.seleccion, 0, 8);
            agachado = datos.agachado;
            capsula.height = (agachado ? 1.5f : 2f) * Bloque;
            capsula.center = Vector3.down * (agachado ? Bloque * .25f : 0f);
            puntoReaparicion = datos.reaparicion;
            rb.position = datos.posicion;
            rb.velocity = impulsoGolpe = Vector3.zero;
            velocidadMovimiento = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            cayendo = false;
            aire = 10f;
            hambrePodridaHasta = 0f;
            solicitudSalto = ultimoSuelo = -10f;
            proximoDano = Time.time + 2f;
            LimpiarDestello();
            if (camaraSeguir != null) camaraSeguir.RestablecerMirada(datos.angulo);
            Physics.SyncTransforms();
            CompletarBarra();
            MostrarAviso("Mundo cargado. Los animales se vuelven a generar.");
        }
        catch (Exception error) { MostrarAviso("No se pudo cargar el guardado."); Debug.LogException(error); }
    }

    void MostrarAviso(string texto) { aviso = texto; avisoHasta = Time.unscaledTime + 4f; }
}
