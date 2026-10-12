using System.Collections.Generic;
using UnityEngine;

public partial class script
{
    private struct Reserva
    {
        public TipoBloque? bloque;
        public TipoBotin? botin;
        public int cantidad;
    }

    List<Reserva> Reservas()
    {
        List<Reserva> resultado = new List<Reserva>();
        foreach (TipoBloque tipo in OrdenBloques)
        {
            int cantidad = CantidadDe(tipo);
            if (System.Array.Exists(bloquesBarra, t => t == tipo)) cantidad -= Mathf.Min(64, cantidad);
            while (cantidad > 0)
            {
                resultado.Add(new Reserva { bloque = tipo, cantidad = Mathf.Min(64, cantidad) });
                cantidad -= 64;
            }
        }
        foreach (TipoBotin tipo in OrdenBotines)
        {
            int cantidad = CantidadBotin(tipo);
            if (System.Array.Exists(botinesBarra, t => t == tipo)) cantidad -= Mathf.Min(64, cantidad);
            while (cantidad > 0)
            {
                resultado.Add(new Reserva { botin = tipo, cantidad = Mathf.Min(64, cantidad) });
                cantidad -= 64;
            }
        }
        return resultado;
    }

    public bool PuedeRecoger(TipoBloque tipo) => PuedeRecogerCantidad(CantidadDe(tipo),
        System.Array.Exists(bloquesBarra, t => t == tipo));
    public bool PuedeRecoger(TipoBotin tipo) => PuedeRecogerCantidad(CantidadBotin(tipo),
        System.Array.Exists(botinesBarra, t => t == tipo));

    bool PuedeRecogerCantidad(int cantidad, bool enBarra)
    {
        if (cantidad > 0 && cantidad % 64 != 0) return true;
        if (cantidad == 0)
            for (int i = 3; i < 9; i++)
                if (!EsBloqueEnSlot(i) && !EsBotinEnSlot(i)) return true;
        return Reservas().Count < 27;
    }

    void DibujarReservas(Rect panel, float escalaPanel)
    {
        List<Reserva> reservas = Reservas();
        string ayuda = "";
        for (int i = 0; i < Mathf.Min(27, reservas.Count); i++)
        {
            Reserva reserva = reservas[i];
            float celda = 18f * escalaPanel;
            Rect zona = new Rect(panel.x + (8f + i % 9 * 18f) * escalaPanel,
                panel.y + (84f + i / 9 * 18f) * escalaPanel, celda, celda);
            if (GUI.Button(zona, GUIContent.none, GUIStyle.none))
            {
                if (reserva.bloque.HasValue) AsignarBloque(reserva.bloque.Value);
                else AsignarBotin(reserva.botin.Value);
            }
            Texture2D textura = reserva.bloque.HasValue ? IconoBloque(reserva.bloque.Value) : IconoBotin(reserva.botin.Value);
            if (textura != null) GUI.DrawTexture(new Rect(zona.x + 2f, zona.y + 2f, celda - 4f, celda - 4f), textura);
            if (reserva.cantidad > 1)
                DibujarTextoSombra(new Rect(zona.x, zona.y + celda - 16f, celda - 2f, 16f), reserva.cantidad.ToString(), true);
            if (zona.Contains(Event.current.mousePosition))
            {
                DibujarRectangulo(zona, new Color(1, 1, 1, .18f));
                ayuda = reserva.bloque.HasValue ? reserva.bloque.Value.ToString() : NombreBotin(reserva.botin.Value);
            }
        }
        if (ayuda.Length > 0)
        {
            Vector2 raton = Event.current.mousePosition;
            Rect tooltip = new Rect(raton.x + 12f, raton.y - 30f, 150f, 26f);
            DibujarRectangulo(tooltip, new Color(.08f, .02f, .13f, .95f));
            DibujarTextoSombra(new Rect(tooltip.x + 5f, tooltip.y + 3f, 140f, 20f), ayuda);
        }
    }
}
