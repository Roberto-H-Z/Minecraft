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
    public List<DatosBloqueFuncionalVoxel> funcionales = new List<DatosBloqueFuncionalVoxel>();
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
    public int version = 3;
    public DatosMisionDragonVoxel mision;
    public PilaObjetoVoxel[] armadura;
    public float danoParcial, absorcion, duracionAbsorcion, duracionRegeneracion, duracionResistencia;
    public PilaObjetoVoxel[] inventario;
    public PilaObjetoVoxel cursor;
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
        foreach (var bloque in funcionales.Values) if (bloque != null) datos.funcionales.Add(bloque.Guardar());
        return datos;
    }

    public bool Compatible(DatosMundoVoxel datos)
    {
        if(datos!=null && datos.ancho==TerrenoOriginal && datos.largo==TerrenoOriginal &&
            ancho==TerrenoOriginal*2 && largo==TerrenoOriginal*2 && datos.quitados!=null && datos.colocados!=null && datos.plantas!=null)
        {
            foreach(var celda in datos.quitados) if(celda.x<0 || celda.x>=TerrenoOriginal || celda.z<0 || celda.z>=TerrenoOriginal) return false;
            foreach(var bloque in datos.colocados) if(bloque.celda.x<0 || bloque.celda.x>=TerrenoOriginal || bloque.celda.z<0 || bloque.celda.z>=TerrenoOriginal) return false;
            foreach(var celda in datos.plantas) if(celda.x<0 || celda.x>=TerrenoOriginal || celda.y<0 || celda.y>=TerrenoOriginal) return false;
            Vector3Int desplazamiento=new Vector3Int(DesplazamientoX,0,DesplazamientoZ);
            for(int i=0;i<datos.quitados.Count;i++) datos.quitados[i]+=desplazamiento;
            for(int i=0;i<datos.colocados.Count;i++) { var bloque=datos.colocados[i]; bloque.celda+=desplazamiento; datos.colocados[i]=bloque; }
            for(int i=0;i<datos.plantas.Count;i++) datos.plantas[i]+=new Vector2Int(DesplazamientoX,DesplazamientoZ);
            if(datos.funcionales!=null) foreach(var bloque in datos.funcionales) if(bloque!=null) bloque.celda+=desplazamiento;
            datos.ancho=ancho; datos.largo=largo;
        }
        bool compatible = datos != null && datos.semilla == semilla && datos.ancho == ancho && datos.largo == largo &&
            Mathf.Approximately(datos.tamanoBloque, tamanoBloque) && datos.quitados != null &&
            datos.colocados != null && datos.plantas != null && datos.troncos != null;
        if (!compatible) return false;
        foreach (Vector3Int celda in datos.quitados) if (!CeldaValida(celda)) return false;
        foreach (BloqueGuardadoVoxel bloque in datos.colocados)
            if (!CeldaValida(bloque.celda) || (int)bloque.tipo < 0 || (int)bloque.tipo > (int)TipoBloque.Antorcha) return false;
        foreach (Vector2Int celda in datos.plantas)
            if (celda.x < 0 || celda.x >= ancho || celda.y < 0 || celda.y >= largo) return false;
        foreach (Vector3 posicion in datos.troncos)
            if (!PosicionFinita(posicion)) return false;
        var celdas = new HashSet<Vector3Int>();
        foreach (var bloque in datos.colocados) if (!celdas.Add(bloque.celda)) return false;
        celdas.Clear();
        if (datos.funcionales != null) foreach (var bloque in datos.funcionales)
        {
            if (bloque == null || !celdas.Add(bloque.celda) ||
                !datos.colocados.Exists(p => p.celda == bloque.celda && p.tipo == bloque.tipo) ||
                (int)bloque.tipo < 8 || bloque.giro < 0 || bloque.giro > (bloque.tipo==TipoBloque.Antorcha ? 7 : 3) ||
                !PilasVoxel.Validas(bloque.contenido,bloque.tipo==TipoBloque.Cofre ? 27 : bloque.tipo==TipoBloque.Horno ? 3 : 0) ||
                !NumeroFinito(bloque.combustible) || !NumeroFinito(bloque.combustibleTotal) || !NumeroFinito(bloque.progreso) ||
                bloque.combustible < 0 || bloque.combustibleTotal < bloque.combustible || bloque.combustibleTotal > 80 ||
                bloque.progreso < 0 || bloque.progreso > BloqueFuncionalVoxel.SegundosCoccion || bloque.experiencia < 0) return false;
            foreach(var pila in bloque.contenido) if(pila.permanente) return false;
        }
        return true;
    }
    public static bool NumeroFinito(float numero) => !float.IsNaN(numero) && !float.IsInfinity(numero);

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
        if (datos.funcionales != null) foreach (var datosBloque in datos.funcionales)
            if (funcionales.TryGetValue(datosBloque.celda,out var bloque)) bloque.Cargar(datosBloque);
        CrearVegetacion(raizGenerada);
        CieloVoxel cielo = GetComponent<CieloVoxel>();
        if (cielo != null) cielo.BuscarNubes();
        OcultarGrietas();
        foreach (ObjetoRecogibleVoxel objeto in FindObjectsByType<ObjetoRecogibleVoxel>(FindObjectsSortMode.None)) Destroy(objeto.gameObject);
    }
}

public partial class script
{
#if UNITY_EDITOR
    private string rutaGuardadoPruebas;
#endif
    private string RutaGuardado
    {
        get
        {
#if UNITY_EDITOR
            if(!string.IsNullOrEmpty(rutaGuardadoPruebas)) return rutaGuardadoPruebas;
#endif
            return Path.Combine(Application.persistentDataPath,"mundo-rodolfo.json");
        }
    }

    void GuardarPartida()
    {
        if (mundo == null) return;
        PartidaVoxel datos = new PartidaVoxel
        {
            mision=misionDragon!=null ? misionDragon.Guardar() : null, armadura=PilasVoxel.Copiar(armadura),
            danoParcial=danoParcial, absorcion=corazonesAbsorcion,duracionAbsorcion=Mathf.Max(0,absorcionHasta-Time.time),
            duracionRegeneracion=Mathf.Max(0,regeneracionManzanaHasta-Time.time),duracionResistencia=Mathf.Max(0,proteccionManzanaHasta-Time.time),
            mundo = mundo.ExportarMundo(), posicion = rb.position, reaparicion = puntoReaparicion,
            angulo = camaraSeguir != null ? camaraSeguir.AnguloHorizontal : transform.eulerAngles.y,
            vidas = vidas, hambre = hambre, saturacion = saturacion, experiencia = experiencia,
            agachado = agachado,
            seleccion = slotSeleccionado, inventario = PilasVoxel.Copiar(inventario), cursor = cursorObjeto.Copia()
        };
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
            if (datos == null || (datos.version <1 || datos.version>3) || !mundo.Compatible(datos.mundo) ||
                !MundoVoxel.PosicionFinita(datos.posicion) || !MundoVoxel.PosicionFinita(datos.reaparicion) ||
                !MundoVoxel.NumeroFinito(datos.angulo) || !MundoVoxel.NumeroFinito(datos.saturacion))
            { MostrarAviso("El guardado no corresponde a este terreno."); return; }
            PilaObjetoVoxel[] restaurado = datos.version==1 ? MigrarInventario(datos) : datos.inventario;
            PilaObjetoVoxel cursor = datos.version==1 || datos.cursor==null ? new PilaObjetoVoxel() : datos.cursor;
            if(!PilasVoxel.Validas(restaurado,36) || !PilasVoxel.Validas(new[]{cursor},1) || !HerramientasInicialesValidas(restaurado,cursor,datos.version<3))
            { MostrarAviso("El inventario del guardado no es valido."); return; }
            if(datos.version>=3 && (!PilasVoxel.Validas(datos.armadura,4) || !MisionGuardadaValida(datos.mision) ||
                !MundoVoxel.NumeroFinito(datos.danoParcial) || !MundoVoxel.NumeroFinito(datos.absorcion) ||
                !MundoVoxel.NumeroFinito(datos.duracionAbsorcion) || !MundoVoxel.NumeroFinito(datos.duracionRegeneracion) || !MundoVoxel.NumeroFinito(datos.duracionResistencia)))
            { MostrarAviso("El equipo o la mision del guardado no son validos."); return; }
            if(datos.version>=3) for(int i=0;i<4;i++) if(!datos.armadura[i].Vacia && datos.armadura[i].id!=TiposArmadura[i]) { MostrarAviso("Equipo de armadura invalido."); return; }
            CerrarContenedor();
            if(misionDragon!=null && misionDragon.EnEnd) misionDragon.Regresar();
            mundo.ImportarMundo(datos.mundo);
            inventario=PilasVoxel.Copiar(restaurado); cursorObjeto=cursor.Copia();
            if(datos.version>=3 && PilasVoxel.Validas(datos.armadura,4)) armadura=PilasVoxel.Copiar(datos.armadura);
            else for(int i=0;i<4;i++) armadura[i]=new PilaObjetoVoxel(TiposArmadura[i]);
            if(datos.version<3) ActualizarEquipoGuardadoAnterior();
            danoParcial=Mathf.Clamp(datos.danoParcial,0,.999f); corazonesAbsorcion=Mathf.Clamp(datos.absorcion,0,8);
            absorcionHasta=Time.time+Mathf.Clamp(datos.duracionAbsorcion,0,120);
            regeneracionManzanaHasta=Time.time+Mathf.Clamp(datos.duracionRegeneracion,0,20); proteccionManzanaHasta=Time.time+Mathf.Clamp(datos.duracionResistencia,0,300);
            if(misionDragon!=null) misionDragon.Restaurar(datos.mision);
            SincronizarInventario();
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
            rb.velocity = impulsoGolpe = impulsoExplosion = Vector3.zero;
            finControlExplosion=0f;
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

    bool HerramientasInicialesValidas(PilaObjetoVoxel[] pilas, PilaObjetoVoxel cursor,bool anterior=false)
    {
        var ids = new HashSet<IdObjetoVoxel>();
        foreach(var pila in pilas)
            if(pila.permanente && (!EsInicial(pila.id) || pila.cantidad!=1 || !ids.Add(pila.id))) return false;
        if(cursor.permanente && (!EsInicial(cursor.id) || cursor.cantidad!=1 || !ids.Add(cursor.id))) return false;
        return ids.Contains(IdObjetoVoxel.EspadaDiamante) && ids.Contains(IdObjetoVoxel.PicoDiamante) && ids.Contains(IdObjetoVoxel.Tridente) && (anterior || ids.Contains(IdObjetoVoxel.ArcoFuego));
    }
    bool EsInicial(IdObjetoVoxel id) => id==IdObjetoVoxel.EspadaDiamante || id==IdObjetoVoxel.PicoDiamante || id==IdObjetoVoxel.Tridente || id==IdObjetoVoxel.ArcoFuego;
    PilaObjetoVoxel[] MigrarInventario(PartidaVoxel datos)
    {
        if(datos.bloques==null || datos.bloques.Length!=OrdenBloques.Length || datos.botines==null || datos.botines.Length!=OrdenBotines.Length ||
            datos.barraBloques==null || datos.barraBloques.Length!=9 || datos.barraBotines==null || datos.barraBotines.Length!=9) return null;
        var nuevo=PilasVoxel.Crear(36);
        nuevo[0]=new PilaObjetoVoxel(IdObjetoVoxel.EspadaDiamante,1,true);
        nuevo[1]=new PilaObjetoVoxel(IdObjetoVoxel.PicoDiamante,1,true);
        nuevo[2]=new PilaObjetoVoxel(IdObjetoVoxel.Tridente,1,true);
        var restantes=new Dictionary<IdObjetoVoxel,int>();
        for(int i=0;i<datos.bloques.Length;i++) restantes[CatalogoObjetosVoxel.DeBloque(OrdenBloques[i])]=datos.bloques[i];
        for(int i=0;i<datos.botines.Length;i++) restantes[CatalogoObjetosVoxel.DeBotin(OrdenBotines[i])]=datos.botines[i];
        foreach(int cantidad in restantes.Values) if(cantidad<0 || cantidad>64*36) return null;
        for(int i=3;i<9;i++)
        {
            int bloque=datos.barraBloques[i], botin=datos.barraBotines[i];
            IdObjetoVoxel id=bloque>=0 && bloque<OrdenBloques.Length ? CatalogoObjetosVoxel.DeBloque((TipoBloque)bloque) :
                botin>=0 && botin<OrdenBotines.Length ? CatalogoObjetosVoxel.DeBotin((TipoBotin)botin) : IdObjetoVoxel.Ninguno;
            if(id==IdObjetoVoxel.Ninguno || restantes[id]==0) continue;
            int cantidad=Mathf.Min(64,restantes[id]); nuevo[i]=new PilaObjetoVoxel(id,cantidad); restantes[id]-=cantidad;
        }
        foreach(var par in restantes) if(par.Value>0 && PilasVoxel.Insertar(nuevo,new PilaObjetoVoxel(par.Key,par.Value))>0) return null;
        return nuevo;
    }

    bool MisionGuardadaValida(DatosMisionDragonVoxel datos)
    {
        if(datos==null) return true;
        return MundoVoxel.PosicionFinita(datos.retorno) && MundoVoxel.NumeroFinito(datos.vidaDragon) && datos.vidaDragon>=0 && datos.vidaDragon<=200 &&
            (!datos.aceptada || datos.cristales!=null && datos.cristales.Length==10) && (!datos.ganada || datos.aceptada && datos.vidaDragon<=0);
    }
    void ActualizarEquipoGuardadoAnterior()
    {
        var arco=new PilaObjetoVoxel(IdObjetoVoxel.ArcoFuego,1,true);
        if(PilasVoxel.Insertar(inventario,arco)>0)
        {
            int sitio=System.Array.FindIndex(inventario,p=>!p.permanente);
            if(sitio>=0) { ObjetoRecogibleVoxel.SoltarObjeto(rb.position,inventario[sitio].Copia()); inventario[sitio]=arco; }
        }
        foreach(var pila in new[]{new PilaObjetoVoxel(IdObjetoVoxel.Antorcha,64),new PilaObjetoVoxel(IdObjetoVoxel.ManzanaEncantada,16)})
        { int resto=PilasVoxel.Insertar(inventario,pila); if(resto>0) { pila.cantidad=resto; ObjetoRecogibleVoxel.SoltarObjeto(rb.position,pila); } }
    }

    void MostrarAviso(string texto) { aviso = texto; avisoHasta = Time.unscaledTime + 4f; }
}
