using System;
using System.Collections.Generic;
using UnityEngine;

// Los valores son estables: se usan también en los archivos de guardado.
public enum IdObjetoVoxel
{
    Ninguno, Tierra, Piedra, Tablones, Arena, Adoquin, MineralCarbon, MineralHierro, MineralDiamante,
    CorderoCrudo, CerdoCrudo, CarnePodrida, Hueso, Polvora, EspadaDiamante, PicoDiamante, Tridente,
    Tronco, Palo, MesaTrabajo, Horno, Cofre, Carbon, CarbonVegetal, HierroBruto, LingoteHierro,
    Diamante, CerdoCocinado, CorderoCocinado, Vidrio,
    EspadaMadera, PicoMadera, HachaMadera, PalaMadera,
    EspadaPiedra, PicoPiedra, HachaPiedra, PalaPiedra,
    EspadaHierro, PicoHierro, HachaHierro, PalaHierro, HachaDiamante, PalaDiamante
}

public enum ClaseHerramientaVoxel { Ninguna, Espada, Pico, Hacha, Pala, Tridente }

public sealed class DefinicionObjetoVoxel
{
    public IdObjetoVoxel id;
    public string nombre, textura;
    public TipoBloque? bloque;
    public ClaseHerramientaVoxel herramienta;
    public int limite = 64, resistencia, nivel, alimento;
    public float rapidez = 1f, combustible, saturacion;
    private Texture2D icono;
    public Texture2D Icono
    {
        get
        {
            if (id == IdObjetoVoxel.Cofre) return ModeloCofreVoxel.Icono;
            if (icono == null && !string.IsNullOrEmpty(textura)) icono = Resources.Load<Texture2D>("Minecraft/" + textura);
            if (icono != null) icono.filterMode = FilterMode.Point;
            return icono;
        }
    }
}

public static class CatalogoObjetosVoxel
{
    public static readonly Dictionary<IdObjetoVoxel, DefinicionObjetoVoxel> Objetos = Crear();
    public static DefinicionObjetoVoxel De(IdObjetoVoxel id) => Objetos.ContainsKey(id) ? Objetos[id] : Objetos[IdObjetoVoxel.Ninguno];

    static Dictionary<IdObjetoVoxel, DefinicionObjetoVoxel> Crear()
    {
        var lista = new Dictionary<IdObjetoVoxel, DefinicionObjetoVoxel>();
        Agregar(lista, IdObjetoVoxel.Ninguno, "Mano vacía", null);
        Bloque(lista, IdObjetoVoxel.Tierra, "Tierra con pasto", TipoBloque.Tierra, "grass_block_side");
        Bloque(lista, IdObjetoVoxel.Piedra, "Piedra", TipoBloque.Piedra, "stone");
        Bloque(lista, IdObjetoVoxel.Tablones, "Tablones de roble", TipoBloque.Madera, "oak_planks", 15f);
        Bloque(lista, IdObjetoVoxel.Arena, "Arena", TipoBloque.Arena, "sand");
        Bloque(lista, IdObjetoVoxel.Adoquin, "Adoquín", TipoBloque.Adoquin, "cobblestone");
        Bloque(lista, IdObjetoVoxel.MineralCarbon, "Mineral de carbón", TipoBloque.Carbon, "coal_ore");
        Bloque(lista, IdObjetoVoxel.MineralHierro, "Mineral de hierro", TipoBloque.Hierro, "iron_ore");
        Bloque(lista, IdObjetoVoxel.MineralDiamante, "Mineral de diamante", TipoBloque.Diamante, "diamond_ore");
        Bloque(lista, IdObjetoVoxel.Tronco, "Tronco de roble", TipoBloque.Tronco, "oak_log", 15f);
        Bloque(lista, IdObjetoVoxel.MesaTrabajo, "Mesa de trabajo", TipoBloque.MesaTrabajo, "crafting_table_top", 15f);
        Bloque(lista, IdObjetoVoxel.Horno, "Horno", TipoBloque.Horno, "furnace_front");
        Bloque(lista, IdObjetoVoxel.Cofre, "Cofre", TipoBloque.Cofre, "chest_icon", 15f);
        Bloque(lista, IdObjetoVoxel.Vidrio, "Vidrio", TipoBloque.Vidrio, "glass");
        Comida(lista, IdObjetoVoxel.CorderoCrudo, "Cordero crudo", "mutton", 2, 1.2f);
        Comida(lista, IdObjetoVoxel.CerdoCrudo, "Cerdo crudo", "porkchop", 3, 1.8f);
        Comida(lista, IdObjetoVoxel.CarnePodrida, "Carne podrida", "rotten_flesh", 4, .8f);
        Comida(lista, IdObjetoVoxel.CerdoCocinado, "Chuleta cocinada", "cooked_porkchop", 8, 12.8f);
        Comida(lista, IdObjetoVoxel.CorderoCocinado, "Cordero cocinado", "cooked_mutton", 6, 9.6f);
        Agregar(lista, IdObjetoVoxel.Hueso, "Hueso", "Items/bone");
        Agregar(lista, IdObjetoVoxel.Polvora, "Pólvora", "Items/gunpowder");
        Agregar(lista, IdObjetoVoxel.Palo, "Palo", "Items/stick", 5f);
        Agregar(lista, IdObjetoVoxel.Carbon, "Carbón", "Items/coal", 80f);
        Agregar(lista, IdObjetoVoxel.CarbonVegetal, "Carbón vegetal", "Items/charcoal", 80f);
        Agregar(lista, IdObjetoVoxel.HierroBruto, "Hierro en bruto", "Items/raw_iron");
        Agregar(lista, IdObjetoVoxel.LingoteHierro, "Lingote de hierro", "Items/iron_ingot");
        Agregar(lista, IdObjetoVoxel.Diamante, "Diamante", "Items/diamond");
        string[] materiales = { "Madera", "Piedra", "Hierro", "Diamante" };
        string[] texturas = { "wooden", "stone", "iron", "diamond" };
        int[] resistencias = { 59, 131, 250, 1561 };
        float[] velocidades = { 2f, 4f, 6f, 8f };
        for (int nivel = 0; nivel < 4; nivel++)
        {
            foreach (ClaseHerramientaVoxel clase in new[] { ClaseHerramientaVoxel.Espada, ClaseHerramientaVoxel.Pico, ClaseHerramientaVoxel.Hacha, ClaseHerramientaVoxel.Pala })
            {
                IdObjetoVoxel id = (IdObjetoVoxel)Enum.Parse(typeof(IdObjetoVoxel), clase + materiales[nivel]);
                string sufijo = clase == ClaseHerramientaVoxel.Espada ? "sword" : clase == ClaseHerramientaVoxel.Pico ? "pickaxe" : clase == ClaseHerramientaVoxel.Hacha ? "axe" : "shovel";
                DefinicionObjetoVoxel def = Agregar(lista, id, clase + " de " + materiales[nivel].ToLowerInvariant(), "Items/" + texturas[nivel] + "_" + sufijo);
                def.herramienta = clase; def.resistencia = resistencias[nivel]; def.nivel = nivel;
                def.rapidez = velocidades[nivel]; def.limite = 1; def.combustible = nivel == 0 ? 10f : 0f;
            }
        }
        DefinicionObjetoVoxel tridente = Agregar(lista, IdObjetoVoxel.Tridente, "Tridente", "Items/trident");
        tridente.herramienta = ClaseHerramientaVoxel.Tridente; tridente.limite = 1; tridente.resistencia = 250;
        return lista;
    }

    static DefinicionObjetoVoxel Agregar(Dictionary<IdObjetoVoxel, DefinicionObjetoVoxel> lista, IdObjetoVoxel id, string nombre, string textura, float combustible = 0f)
    {
        var def = new DefinicionObjetoVoxel { id = id, nombre = nombre, textura = textura, combustible = combustible };
        lista[id] = def; return def;
    }
    static void Bloque(Dictionary<IdObjetoVoxel, DefinicionObjetoVoxel> lista, IdObjetoVoxel id, string nombre, TipoBloque tipo, string textura, float combustible = 0f)
    { Agregar(lista, id, nombre, "Blocks/" + textura, combustible).bloque = tipo; }
    static void Comida(Dictionary<IdObjetoVoxel, DefinicionObjetoVoxel> lista, IdObjetoVoxel id, string nombre, string textura, int hambre, float saturacion)
    { var def = Agregar(lista, id, nombre, "Items/" + textura); def.alimento = hambre; def.saturacion = saturacion; }

    public static IdObjetoVoxel DeBloque(TipoBloque tipo)
    {
        foreach (var def in Objetos.Values) if (def.bloque == tipo) return def.id;
        return IdObjetoVoxel.Ninguno;
    }
    public static IdObjetoVoxel DeBotin(TipoBotin tipo) => (IdObjetoVoxel)((int)IdObjetoVoxel.CorderoCrudo + (int)tipo);
    public static bool EsEstacion(TipoBloque tipo) => tipo == TipoBloque.MesaTrabajo || tipo == TipoBloque.Horno || tipo == TipoBloque.Cofre;
}

[Serializable]
public class PilaObjetoVoxel
{
    public IdObjetoVoxel id;
    public int cantidad, resistencia;
    public bool permanente;
    public bool Vacia => id == IdObjetoVoxel.Ninguno || cantidad <= 0;
    public PilaObjetoVoxel() { }
    public PilaObjetoVoxel(IdObjetoVoxel objeto, int cantidad = 1, bool permanente = false)
    { id = objeto; this.cantidad = cantidad; this.permanente = permanente; resistencia = CatalogoObjetosVoxel.De(id).resistencia; }
    public PilaObjetoVoxel Copia() => new PilaObjetoVoxel { id = id, cantidad = cantidad, resistencia = resistencia, permanente = permanente };
    public void Limpiar() { id = IdObjetoVoxel.Ninguno; cantidad = resistencia = 0; permanente = false; }
    public bool Compatible(PilaObjetoVoxel otra) => !Vacia && !otra.Vacia && id == otra.id && resistencia == otra.resistencia && permanente == otra.permanente;
}

public static class PilasVoxel
{
    public static PilaObjetoVoxel[] Crear(int cantidad)
    { var pilas = new PilaObjetoVoxel[cantidad]; for (int i = 0; i < cantidad; i++) pilas[i] = new PilaObjetoVoxel(); return pilas; }
    public static PilaObjetoVoxel[] Copiar(PilaObjetoVoxel[] origen)
    { var copia = Crear(origen.Length); for (int i = 0; i < origen.Length; i++) copia[i] = origen[i].Copia(); return copia; }
    // Devuelve las unidades que NO caben. El llamador conserva siempre ese resto.
    public static int Insertar(PilaObjetoVoxel[] destino, PilaObjetoVoxel origen, int inicio = 0, int fin = -1)
    {
        if (origen == null || origen.Vacia) return 0;
        fin = fin < 0 ? destino.Length : Mathf.Min(fin, destino.Length);
        int restante = origen.cantidad, limite = CatalogoObjetosVoxel.De(origen.id).limite;
        for (int fase = 0; fase < 2; fase++)
        for (int i = inicio; i < fin && restante > 0; i++)
        {
            PilaObjetoVoxel pila = destino[i];
            if (fase == 0 ? !pila.Compatible(origen) : !pila.Vacia) continue;
            int transferir = Mathf.Min(restante, limite - (pila.Vacia ? 0 : pila.cantidad));
            if (transferir <= 0) continue;
            if (pila.Vacia) { destino[i] = origen.Copia(); destino[i].cantidad = transferir; }
            else pila.cantidad += transferir;
            restante -= transferir;
        }
        return restante;
    }
    public static bool Validas(PilaObjetoVoxel[] pilas, int tamano)
    {
        if (pilas == null || pilas.Length != tamano) return false;
        foreach (PilaObjetoVoxel pila in pilas)
            if (pila == null || !CatalogoObjetosVoxel.Objetos.ContainsKey(pila.id) || pila.cantidad < 0 ||
                pila.cantidad > CatalogoObjetosVoxel.De(pila.id).limite || pila.resistencia < 0 ||
                pila.resistencia > CatalogoObjetosVoxel.De(pila.id).resistencia ||
                (!pila.Vacia && CatalogoObjetosVoxel.De(pila.id).herramienta != ClaseHerramientaVoxel.Ninguna && pila.resistencia <= 0)) return false;
        return true;
    }
}
