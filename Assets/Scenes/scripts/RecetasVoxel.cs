using System.Collections.Generic;
using UnityEngine;

public sealed class RecetaVoxel
{
    public string nombre;
    public int ancho, alto, cantidad;
    public IdObjetoVoxel[] ingredientes;
    public IdObjetoVoxel resultado;
    public bool Cabe(int rejilla) => ancho <= rejilla && alto <= rejilla;
    public bool Coincide(PilaObjetoVoxel[] rejilla, int lado)
    {
        if (!Cabe(lado)) return false;
        for (int y = 0; y <= lado - alto; y++)
        for (int x = 0; x <= lado - ancho; x++)
        for (int espejo = 0; espejo < 2; espejo++)
        {
            bool igual = true;
            for (int fila = 0; fila < lado && igual; fila++)
            for (int columna = 0; columna < lado; columna++)
            {
                int dx = columna - x, dy = fila - y;
                IdObjetoVoxel esperado = dx >= 0 && dx < ancho && dy >= 0 && dy < alto
                    ? ingredientes[dy * ancho + (espejo == 0 ? dx : ancho - 1 - dx)] : IdObjetoVoxel.Ninguno;
                PilaObjetoVoxel pila = rejilla[fila * lado + columna];
                if ((pila.Vacia ? IdObjetoVoxel.Ninguno : pila.id) != esperado || pila.permanente) { igual = false; break; }
            }
            if (igual) return true;
        }
        return false;
    }
}

public static class RecetasVoxel
{
    public static readonly List<RecetaVoxel> Todas = Crear();
    public static RecetaVoxel Buscar(PilaObjetoVoxel[] rejilla, int lado)
    { foreach (RecetaVoxel receta in Todas) if (receta.Coincide(rejilla, lado)) return receta; return null; }

    static List<RecetaVoxel> Crear()
    {
        var recetas = new List<RecetaVoxel>();
        Agregar(recetas, IdObjetoVoxel.Tablones, 4, 1, 1, IdObjetoVoxel.Tronco);
        Agregar(recetas, IdObjetoVoxel.Palo, 4, 1, 2, IdObjetoVoxel.Tablones, IdObjetoVoxel.Tablones);
        Agregar(recetas, IdObjetoVoxel.MesaTrabajo, 1, 2, 2, IdObjetoVoxel.Tablones, IdObjetoVoxel.Tablones, IdObjetoVoxel.Tablones, IdObjetoVoxel.Tablones);
        IdObjetoVoxel n = IdObjetoVoxel.Ninguno, palo = IdObjetoVoxel.Palo;
        Agregar(recetas, IdObjetoVoxel.Cofre, 1, 3, 3, IdObjetoVoxel.Tablones, IdObjetoVoxel.Tablones, IdObjetoVoxel.Tablones,
            IdObjetoVoxel.Tablones, n, IdObjetoVoxel.Tablones, IdObjetoVoxel.Tablones, IdObjetoVoxel.Tablones, IdObjetoVoxel.Tablones);
        Agregar(recetas, IdObjetoVoxel.Horno, 1, 3, 3, IdObjetoVoxel.Adoquin, IdObjetoVoxel.Adoquin, IdObjetoVoxel.Adoquin,
            IdObjetoVoxel.Adoquin, n, IdObjetoVoxel.Adoquin, IdObjetoVoxel.Adoquin, IdObjetoVoxel.Adoquin, IdObjetoVoxel.Adoquin);
        string[] materiales = { "Madera", "Piedra", "Hierro", "Diamante" };
        IdObjetoVoxel[] materias = { IdObjetoVoxel.Tablones, IdObjetoVoxel.Adoquin, IdObjetoVoxel.LingoteHierro, IdObjetoVoxel.Diamante };
        for (int i = 0; i < materiales.Length; i++)
        {
            IdObjetoVoxel m = materias[i];
            IdObjetoVoxel espada = (IdObjetoVoxel)System.Enum.Parse(typeof(IdObjetoVoxel), "Espada" + materiales[i]);
            IdObjetoVoxel pico = (IdObjetoVoxel)System.Enum.Parse(typeof(IdObjetoVoxel), "Pico" + materiales[i]);
            IdObjetoVoxel hacha = (IdObjetoVoxel)System.Enum.Parse(typeof(IdObjetoVoxel), "Hacha" + materiales[i]);
            IdObjetoVoxel pala = (IdObjetoVoxel)System.Enum.Parse(typeof(IdObjetoVoxel), "Pala" + materiales[i]);
            Agregar(recetas, espada, 1, 1, 3, m, m, palo);
            Agregar(recetas, pico, 1, 3, 3, m, m, m, n, palo, n, n, palo, n);
            Agregar(recetas, hacha, 1, 2, 3, m, m, m, palo, n, palo);
            Agregar(recetas, pala, 1, 1, 3, m, palo, palo);
        }
        return recetas;
    }
    static void Agregar(List<RecetaVoxel> recetas, IdObjetoVoxel resultado, int cantidad, int ancho, int alto, params IdObjetoVoxel[] ingredientes)
    { recetas.Add(new RecetaVoxel { nombre = CatalogoObjetosVoxel.De(resultado).nombre, resultado = resultado, cantidad = cantidad,
        ancho = ancho, alto = alto, ingredientes = ingredientes }); }

    public static IdObjetoVoxel Fundicion(IdObjetoVoxel entrada)
    {
        switch (entrada)
        {
            case IdObjetoVoxel.HierroBruto: case IdObjetoVoxel.MineralHierro: return IdObjetoVoxel.LingoteHierro;
            case IdObjetoVoxel.Tronco: return IdObjetoVoxel.CarbonVegetal;
            case IdObjetoVoxel.CerdoCrudo: return IdObjetoVoxel.CerdoCocinado;
            case IdObjetoVoxel.CorderoCrudo: return IdObjetoVoxel.CorderoCocinado;
            case IdObjetoVoxel.Adoquin: return IdObjetoVoxel.Piedra;
            case IdObjetoVoxel.Arena: return IdObjetoVoxel.Vidrio;
            default: return IdObjetoVoxel.Ninguno;
        }
    }
}
