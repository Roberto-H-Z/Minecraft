using System.Collections.Generic;
using UnityEngine;

public partial class script
{
    private PilaObjetoVoxel[] inventario = PilasVoxel.Crear(36);
    private PilaObjetoVoxel[] rejillaPersonal = PilasVoxel.Crear(4);
    private PilaObjetoVoxel[] rejillaMesa = PilasVoxel.Crear(9);
    private PilaObjetoVoxel cursorObjeto = new PilaObjetoVoxel();
    private BloqueFuncionalVoxel estacionAbierta;
    private string mensajeInventario = "";
    private float mensajeHasta;
    public PilaObjetoVoxel PilaEnSlot(int slot) => slot >= 0 && slot < inventario.Length ? inventario[slot] : new PilaObjetoVoxel();
    public DefinicionObjetoVoxel HerramientaActual => CatalogoObjetosVoxel.De(PilaEnSlot(slotSeleccionado).id);
    public bool TieneTridente => HerramientaActual.herramienta == ClaseHerramientaVoxel.Tridente;
    public int TipoVisualHerramienta => HerramientaActual.herramienta == ClaseHerramientaVoxel.Espada ? 0 :
        HerramientaActual.herramienta == ClaseHerramientaVoxel.Tridente ? 2 :
        HerramientaActual.herramienta != ClaseHerramientaVoxel.Ninguna ? 1 : -1;
    public Texture2D IconoEnSlot(int slot) => CatalogoObjetosVoxel.De(PilaEnSlot(slot).id).Icono;
    public string NombreEnSlot(int slot) => CatalogoObjetosVoxel.De(PilaEnSlot(slot).id).nombre;
    public bool PuedeRecoger(TipoBloque tipo) => PuedeRecoger(new PilaObjetoVoxel(CatalogoObjetosVoxel.DeBloque(tipo)));
    public bool PuedeRecoger(TipoBotin tipo) => PuedeRecoger(new PilaObjetoVoxel(CatalogoObjetosVoxel.DeBotin(tipo)));
    public bool PuedeRecoger(PilaObjetoVoxel pila)
    {
        var copia = PilasVoxel.Copiar(inventario);
        return PilasVoxel.Insertar(copia,pila) < pila.cantidad;
    }
    public int RecogerObjeto(PilaObjetoVoxel pila)
    {
        int restante = PilasVoxel.Insertar(inventario,pila);
        SincronizarInventario(); return restante;
    }
    int TotalObjeto(IdObjetoVoxel id)
    { int total=0; foreach(var pila in inventario) if (!pila.Vacia && pila.id==id) total+=pila.cantidad; return total; }
    void InicializarInventario()
    {
        inventario = PilasVoxel.Crear(36);
        inventario[0] = new PilaObjetoVoxel(IdObjetoVoxel.EspadaDiamante,1,true);
        inventario[1] = new PilaObjetoVoxel(IdObjetoVoxel.PicoDiamante,1,true);
        inventario[2] = new PilaObjetoVoxel(IdObjetoVoxel.Tridente,1,true);
        cursorObjeto.Limpiar(); SincronizarInventario();
    }
    void SincronizarInventario()
    {
        bloquesRecogidos=TotalObjeto(IdObjetoVoxel.Tierra); bloquesPiedra=TotalObjeto(IdObjetoVoxel.Piedra);
        bloquesMadera=TotalObjeto(IdObjetoVoxel.Tablones); bloquesArena=TotalObjeto(IdObjetoVoxel.Arena);
        bloquesAdoquin=TotalObjeto(IdObjetoVoxel.Adoquin); bloquesCarbon=TotalObjeto(IdObjetoVoxel.MineralCarbon);
        bloquesHierro=TotalObjeto(IdObjetoVoxel.MineralHierro); bloquesDiamante=TotalObjeto(IdObjetoVoxel.MineralDiamante);
        comida=0; botines.Clear();
        foreach (TipoBotin tipo in OrdenBotines) botines[tipo]=TotalObjeto(CatalogoObjetosVoxel.DeBotin(tipo));
        foreach (var pila in inventario) if (!pila.Vacia && CatalogoObjetosVoxel.De(pila.id).alimento>0) comida+=pila.cantidad;
        for(int i=0;i<9;i++)
        {
            bloquesBarra[i]=CatalogoObjetosVoxel.De(inventario[i].id).bloque;
            int valor=(int)inventario[i].id-(int)IdObjetoVoxel.CorderoCrudo;
            botinesBarra[i]=!inventario[i].Vacia && valor>=0 && valor<5 ? (TipoBotin?)valor : null;
        }
    }
    void ConsumirSeleccionado(int cantidad)
    {
        PilaObjetoVoxel pila=inventario[slotSeleccionado];
        if(pila.Vacia || pila.permanente) return;
        pila.cantidad=Mathf.Max(0,pila.cantidad-cantidad); if(pila.Vacia) pila.Limpiar();
        SincronizarInventario();
    }
    void DesgastarHerramienta(int cantidad=1)
    {
        PilaObjetoVoxel pila=inventario[slotSeleccionado];
        if(pila.Vacia || pila.permanente || CatalogoObjetosVoxel.De(pila.id).resistencia<=0) return;
        pila.resistencia-=cantidad;
        if(pila.resistencia<=0) { pila.Limpiar(); SonidosVoxel.Emitir("romper",PosicionOjos); }
        SincronizarInventario();
    }
    void SoltarSeleccionado()
    {
        PilaObjetoVoxel pila=inventario[slotSeleccionado];
        if(pila.Vacia || pila.permanente) return;
        PilaObjetoVoxel unidad=pila.Copia(); unidad.cantidad=1;
        ObjetoRecogibleVoxel.SoltarObjeto(PosicionOjos+transform.forward*Bloque*.65f,unidad,0,1.2f);
        ConsumirSeleccionado(1);
    }
    void AvisarInventario(string mensaje) { mensajeInventario=mensaje; mensajeHasta=Time.unscaledTime+4f; }
    bool IntentarAbrirEstacion()
    {
        if(agachado || mundo==null) return false;
        RaycastHit golpe;
        if(!BuscarBloqueApuntado(out golpe)) return false;
        var bloque=mundo.FuncionalApuntado(golpe);
        if(bloque==null || !CatalogoObjetosVoxel.EsEstacion(bloque.tipo)) return false;
        if(bloque.tipo==TipoBloque.Cofre && !mundo.PuedeAbrirCofre(bloque))
        { AvisarInventario("Hay un bloque sobre el cofre."); return true; }
        estacionAbierta=bloque; bloque.Abierto=true;
        inventarioAbierto=true; DetenerPicado(); tiempoComiendo=0f; cargandoTridente=false;
        entradaMovimiento=Vector2.zero; solicitudSalto=-10f;
        if(camaraSeguir!=null) camaraSeguir.MostrarCursorInterfaz(true);
        return true;
    }
    void DevolverPila(PilaObjetoVoxel pila)
    {
        if(pila.Vacia) return;
        int resto=RecogerObjeto(pila);
        if(resto>0)
        {
            var sobrante=pila.Copia(); sobrante.cantidad=resto;
            if(sobrante.permanente)
            {
                int espacio=System.Array.FindIndex(inventario,p=>!p.permanente);
                if(espacio>=0)
                {
                    if(!inventario[espacio].Vacia) ObjetoRecogibleVoxel.SoltarObjeto(PosicionOjos+transform.forward*.8f,inventario[espacio].Copia(),0,1f);
                    inventario[espacio]=sobrante; pila.Limpiar(); SincronizarInventario(); return;
                }
                pila.cantidad=resto; return;
            }
            ObjetoRecogibleVoxel.SoltarObjeto(PosicionOjos+transform.forward*.8f,sobrante,0,1f);
        }
        pila.Limpiar();
    }
    void CerrarContenedor()
    {
        if(estacionAbierta!=null) estacionAbierta.Abierto=false;
        estacionAbierta=null;
        DevolverPila(cursorObjeto);
        origenArrastre=null; indiceArrastre=-1; arrastrandoObjeto=false;
        foreach(var pila in rejillaPersonal) DevolverPila(pila);
        foreach(var pila in rejillaMesa) DevolverPila(pila);
    }
    void ActualizarContenedorAbierto()
    {
        if(!inventarioAbierto || estacionAbierta==null) return;
        if(Vector3.Distance(transform.position,estacionAbierta.transform.position)>alcancePico+Bloque)
            CambiarInventario();
    }
    PilaObjetoVoxel[] RejillaActiva => estacionAbierta!=null && estacionAbierta.tipo==TipoBloque.MesaTrabajo ? rejillaMesa : rejillaPersonal;
    int LadoRejilla => estacionAbierta!=null && estacionAbierta.tipo==TipoBloque.MesaTrabajo ? 3 : 2;
    bool TieneFabricacion => estacionAbierta==null || estacionAbierta.tipo==TipoBloque.MesaTrabajo;

    bool PrepararReceta(RecetaVoxel receta)
    {
        if(!receta.Cabe(LadoRejilla)) { AvisarInventario("Esta receta necesita una mesa de trabajo."); return false; }
        if(!cursorObjeto.Vacia) { AvisarInventario("Coloca primero el objeto del cursor."); return false; }
        var copia=PilasVoxel.Copiar(inventario);
        foreach(var pila in RejillaActiva)
            if(!pila.Vacia && PilasVoxel.Insertar(copia,pila)>0) { AvisarInventario("No hay espacio para devolver los ingredientes."); return false; }
        var nueva=PilasVoxel.Crear(LadoRejilla*LadoRejilla);
        for(int y=0;y<receta.alto;y++)
        for(int x=0;x<receta.ancho;x++)
        {
            IdObjetoVoxel id=receta.ingredientes[y*receta.ancho+x]; if(id==IdObjetoVoxel.Ninguno) continue;
            int fuente=System.Array.FindIndex(copia,p=>!p.Vacia && !p.permanente && p.id==id);
            if(fuente<0) { AvisarInventario("Faltan materiales para "+receta.nombre+"."); return false; }
            nueva[y*LadoRejilla+x]=new PilaObjetoVoxel(id);
            copia[fuente].cantidad--; if(copia[fuente].Vacia) copia[fuente].Limpiar();
        }
        inventario=copia;
        if(LadoRejilla==3) rejillaMesa=nueva; else rejillaPersonal=nueva;
        SincronizarInventario(); return true;
    }
    void TomarFabricacion(bool varias)
    {
        int hechas=0;
        for(int iteracion=0;iteracion<(varias?64:1);iteracion++)
        {
            RecetaVoxel receta=RecetasVoxel.Buscar(RejillaActiva,LadoRejilla); if(receta==null) break;
            var salida=new PilaObjetoVoxel(receta.resultado,receta.cantidad);
            if(varias)
            {
                var copia=PilasVoxel.Copiar(inventario);
                if(PilasVoxel.Insertar(copia,salida)>0) break;
                inventario=copia;
            }
            else
            {
                if(cursorObjeto.Vacia) cursorObjeto=salida;
                else if(cursorObjeto.Compatible(salida) && cursorObjeto.cantidad+salida.cantidad<=CatalogoObjetosVoxel.De(salida.id).limite)
                    cursorObjeto.cantidad+=salida.cantidad;
                else break;
            }
            foreach(var pila in RejillaActiva)
                if(!pila.Vacia) { pila.cantidad--; if(pila.Vacia) pila.Limpiar(); }
            hechas++;
        }
        if(hechas>0) { SonidosVoxel.Emitir("recoger",PosicionOjos); SincronizarInventario(); }
    }
}
