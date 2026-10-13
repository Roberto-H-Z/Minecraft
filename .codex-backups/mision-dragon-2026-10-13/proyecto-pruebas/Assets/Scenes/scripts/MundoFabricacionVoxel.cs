using System.Collections.Generic;
using UnityEngine;

public partial class MundoVoxel
{
    private readonly Dictionary<Vector3Int, BloqueFuncionalVoxel> funcionales = new Dictionary<Vector3Int, BloqueFuncionalVoxel>();
    private readonly Dictionary<string, Material> materialesEspeciales = new Dictionary<string, Material>();
    public Vector3 CentroCelda(Vector3Int celda) => transform.TransformPoint(new Vector3(
        (celda.x-ancho*.5f+.5f)*tamanoBloque,celda.y*tamanoBloque,(celda.z-largo*.5f+.5f)*tamanoBloque));
    public Material MaterialEspecial(string textura)
    {
        Material material;
        if (!materialesEspeciales.TryGetValue(textura,out material) || material == null)
        {
            material = CrearMaterialBloque(textura,Color.white,textura == "glass");
            materialesEspeciales[textura] = material;
        }
        return material;
    }
    public Material[] MaterialesEspeciales(TipoBloque tipo, bool encendido)
    {
        string frente, atras, lado, arriba, abajo;
        if (tipo == TipoBloque.MesaTrabajo)
        { frente=atras="crafting_table_front"; lado="crafting_table_side"; arriba="crafting_table_top"; abajo="oak_planks"; }
        else if (tipo == TipoBloque.Horno)
        { frente=encendido?"furnace_front_on":"furnace_front"; atras=lado="furnace_side"; arriba=abajo="furnace_top"; }
        else if (tipo == TipoBloque.Tronco)
        { frente=atras=lado="oak_log"; arriba=abajo="oak_log_top"; }
        else if (tipo == TipoBloque.Cofre)
        { frente=atras=lado=arriba=abajo="oak_planks"; }
        else if(tipo==TipoBloque.Antorcha) { frente=atras=lado=arriba=abajo="torch"; }
        else { frente=atras=lado=arriba=abajo="glass"; }
        return new[] {MaterialEspecial(frente),MaterialEspecial(atras),MaterialEspecial(lado),MaterialEspecial(lado),MaterialEspecial(arriba),MaterialEspecial(abajo)};
    }
    void ActualizarEspeciales()
    {
        var borrar=new List<Vector3Int>();
        foreach(var par in funcionales) if(!bloquesColocados.ContainsKey(par.Key)) borrar.Add(par.Key);
        foreach(var celda in borrar) ActualizarEspecial(celda);
        foreach(var par in bloquesColocados) if((int)par.Value>=8) ActualizarEspecial(par.Key);
    }
    void ActualizarEspecial(Vector3Int celda)
    {
        bool especial=bloquesColocados.TryGetValue(celda,out var tipo) && (int)tipo>=8;
        if(funcionales.TryGetValue(celda,out var existente))
        {
            if(especial && existente!=null && existente.tipo==tipo) return;
            if(existente!=null) { existente.gameObject.SetActive(false); Destroy(existente.gameObject); }
            funcionales.Remove(celda);
        }
        if(!especial || !Application.isPlaying) return;
        var objeto=new GameObject(CatalogoObjetosVoxel.De(CatalogoObjetosVoxel.DeBloque(tipo)).nombre);
        objeto.transform.SetParent(PadreEspecial(celda),false); objeto.transform.position=CentroCelda(celda);
        var bloque=objeto.AddComponent<BloqueFuncionalVoxel>();
        int giro=jugador!=null ? Mathf.RoundToInt(jugador.transform.eulerAngles.y/90f+2f)%4 : 0;
        bloque.Configurar(this,celda,tipo,giro); bloque.delEnd=raizDimensionEnd!=null; funcionales[celda]=bloque;
    }
    public BloqueFuncionalVoxel FuncionalApuntado(RaycastHit impacto) => impacto.collider != null ? impacto.collider.GetComponentInParent<BloqueFuncionalVoxel>() : null;
    public bool PuedeAbrirCofre(BloqueFuncionalVoxel cofre)
    {
        if(ExisteBloque(cofre.celda+Vector3Int.up)) return false;
        Vector3 centro=cofre.transform.position+Vector3.up*tamanoBloque*.65f;
        foreach(Collider otro in Physics.OverlapBox(centro,new Vector3(.42f,.14f,.42f)*tamanoBloque,
            Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if(!otro.transform.IsChildOf(cofre.transform) && otro.GetComponentInParent<ObjetoRecogibleVoxel>()==null &&
                otro.GetComponentInParent<script>()==null && otro.GetComponentInParent<enemigo>()==null &&
                otro.GetComponentInParent<AnimalVoxel>()==null) return false;
        return true;
    }
    bool ImpactoValido(RaycastHit impacto) => impacto.collider != null &&
        (EsColisionTerreno(impacto.collider) || impacto.collider.GetComponentInParent<ArenaEndVoxel>()!=null || troncos.ContainsKey(impacto.collider) || FuncionalApuntado(impacto) != null);
    Vector3Int CeldaImpacto(RaycastHit impacto)
    {
        BloqueFuncionalVoxel funcional = FuncionalApuntado(impacto);
        return funcional != null ? funcional.celda : CeldaEn(transform.InverseTransformPoint(impacto.point-impacto.normal*.02f));
    }
    public TipoBloque TipoApuntado(RaycastHit impacto)
    {
        if (troncos.ContainsKey(impacto.collider)) return TipoBloque.Tronco;
        return TipoEn(CeldaImpacto(impacto));
    }
    bool QuitarEspecial(RaycastHit impacto)
    {
        BloqueFuncionalVoxel bloque = FuncionalApuntado(impacto);
        if (bloque == null) return false;
        bloque.SoltarContenido();
        TipoBloque tipo = bloque.tipo;
        Vector3Int celda=bloque.celda;
        bloquesColocados.Remove(celda);
        funcionales.Remove(bloque.celda);
        bloque.gameObject.SetActive(false);
        Destroy(bloque.gameObject);
        SoltarExtraccion(impacto.point+impacto.normal*.42f,tipo);
        SonidosVoxel.Emitir("romper",impacto.point);
        ActualizarCelda(celda); return true;
    }
    void SoltarExtraccion(Vector3 posicion, TipoBloque tipo)
    {
        if (jugador == null) jugador = FindFirstObjectByType<script>();
        var herramienta = jugador != null ? jugador.HerramientaActual : CatalogoObjetosVoxel.De(IdObjetoVoxel.Ninguno);
        bool piedra = tipo == TipoBloque.Piedra || tipo == TipoBloque.Adoquin || tipo == TipoBloque.Carbon ||
            tipo == TipoBloque.Hierro || tipo == TipoBloque.Diamante || tipo == TipoBloque.Horno;
        int requerido = tipo == TipoBloque.Hierro ? 1 : tipo == TipoBloque.Diamante ? 2 : 0;
        if (piedra && (herramienta.herramienta != ClaseHerramientaVoxel.Pico || herramienta.nivel < requerido)) return;
        IdObjetoVoxel id = tipo == TipoBloque.Piedra ? IdObjetoVoxel.Adoquin : tipo == TipoBloque.Carbon ? IdObjetoVoxel.Carbon :
            tipo == TipoBloque.Hierro ? IdObjetoVoxel.HierroBruto : tipo == TipoBloque.Diamante ? IdObjetoVoxel.Diamante : CatalogoObjetosVoxel.DeBloque(tipo);
        ObjetoRecogibleVoxel.SoltarObjeto(posicion,new PilaObjetoVoxel(id));
    }
    public float DuracionPicado(RaycastHit impacto, DefinicionObjetoVoxel herramienta)
    {
        TipoBloque tipo = TipoApuntado(impacto);
        bool madera = tipo == TipoBloque.Tronco || tipo == TipoBloque.Madera || tipo == TipoBloque.MesaTrabajo || tipo == TipoBloque.Cofre;
        bool tierra = tipo == TipoBloque.Tierra || tipo == TipoBloque.Arena;
        bool adecuada = herramienta.herramienta == (madera ? ClaseHerramientaVoxel.Hacha : tierra ? ClaseHerramientaVoxel.Pala : ClaseHerramientaVoxel.Pico);
        float baseTiempo = tipo==TipoBloque.Antorcha ? .12f : madera ? 2.2f : tierra ? .8f : tipo == TipoBloque.Vidrio ? .3f : 3.2f;
        return Mathf.Max(.12f,baseTiempo/(adecuada ? herramienta.rapidez : 1f));
    }
}
