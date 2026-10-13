using UnityEngine;

public partial class script
{
    private Vector2 scrollRecetas;
    private PilaObjetoVoxel pilaBajoCursor;
    private PilaObjetoVoxel[] origenArrastre;
    private int indiceArrastre = -1;
    private Vector2 inicioArrastre;
    private bool arrastrandoObjeto;
    private GUIStyle textoPilas, textoInterfaz;

    void DibujarPila(PilaObjetoVoxel pila, Rect zona)
    {
        if (pila == null || pila.Vacia) return;
        var definicion = CatalogoObjetosVoxel.De(pila.id);
        if (definicion.Icono != null) GUI.DrawTexture(zona, definicion.Icono, ScaleMode.ScaleToFit);
        if (textoPilas == null) textoPilas = new GUIStyle(GUI.skin.label)
        { alignment = TextAnchor.LowerRight, fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        if (pila.cantidad > 1)
        {
            GUI.color = Color.black; GUI.Label(new Rect(zona.x+1,zona.y+1,zona.width,zona.height),pila.cantidad.ToString(),textoPilas);
            GUI.color = Color.white; GUI.Label(zona,pila.cantidad.ToString(),textoPilas);
        }
        if (!pila.permanente && definicion.resistencia > 0 && pila.resistencia < definicion.resistencia)
        {
            float fraccion = Mathf.Clamp01((float)pila.resistencia/definicion.resistencia);
            PintarRect(new Rect(zona.x,zona.yMax-3,zona.width,3),Color.black);
            PintarRect(new Rect(zona.x,zona.yMax-3,zona.width*fraccion,2),Color.Lerp(Color.red,Color.green,fraccion));
        }
        GUI.color = Color.white;
    }
    void PintarRect(Rect zona, Color color)
    { Color anterior=GUI.color; GUI.color=color; GUI.DrawTexture(zona,Texture2D.whiteTexture); GUI.color=anterior; }

    bool AceptaPila(PilaObjetoVoxel[] grupo, int indice, PilaObjetoVoxel pila)
    {
        if (pila.Vacia) return true;
        if (grupo==inventario) return true;
        if(grupo==armadura) return pila.id==TiposArmadura[indice];
        if (pila.permanente) return false;
        if (estacionAbierta!=null && grupo==estacionAbierta.contenido) return estacionAbierta.Acepta(indice,pila);
        return true;
    }
    void CobrarExperienciaHorno(PilaObjetoVoxel[] grupo, int indice)
    {
        if (estacionAbierta==null || grupo!=estacionAbierta.contenido || estacionAbierta.tipo!=TipoBloque.Horno || indice!=2) return;
        experiencia+=estacionAbierta.experiencia; estacionAbierta.experiencia=0;
    }
    void MoverPila(PilaObjetoVoxel[] grupo, int indice, bool mitad)
    {
        var pila=grupo[indice];
        if (cursorObjeto.Vacia)
        {
            if (pila.Vacia) return;
            int cantidad=mitad ? (pila.cantidad+1)/2 : pila.cantidad;
            cursorObjeto=pila.Copia(); cursorObjeto.cantidad=cantidad;
            pila.cantidad-=cantidad; if(pila.Vacia) pila.Limpiar();
            CobrarExperienciaHorno(grupo,indice);
        }
        else
        {
            if (!AceptaPila(grupo,indice,cursorObjeto)) return;
            int limite=CatalogoObjetosVoxel.De(cursorObjeto.id).limite;
            if(pila.Vacia || pila.Compatible(cursorObjeto))
            {
                int cantidad=Mathf.Min(mitad ? 1 : cursorObjeto.cantidad,limite-(pila.Vacia ? 0 : pila.cantidad));
                if(cantidad<=0) return;
                if(pila.Vacia) { grupo[indice]=cursorObjeto.Copia(); grupo[indice].cantidad=cantidad; }
                else pila.cantidad+=cantidad;
                cursorObjeto.cantidad-=cantidad; if(cursorObjeto.Vacia) cursorObjeto.Limpiar();
            }
            else if(!mitad) { grupo[indice]=cursorObjeto; cursorObjeto=pila; }
        }
        SincronizarInventario();
    }
    void TransferirPila(PilaObjetoVoxel[] grupo,int indice)
    {
        var pila=grupo[indice]; if(pila.Vacia) return;
        int restante=pila.cantidad;
        if(grupo!=inventario) restante=PilasVoxel.Insertar(inventario,pila);
        else if(estacionAbierta!=null && estacionAbierta.tipo!=TipoBloque.MesaTrabajo)
        {
            if(pila.permanente) return;
            var destino=estacionAbierta.contenido;
            if(estacionAbierta.tipo==TipoBloque.Cofre) restante=PilasVoxel.Insertar(destino,pila);
            else
            {
                int entrada=RecetasVoxel.Fundicion(pila.id)!=IdObjetoVoxel.Ninguno ? 0 : 1;
                if(estacionAbierta.Acepta(entrada,pila)) restante=PilasVoxel.Insertar(destino,pila,entrada,entrada+1);
            }
        }
        else restante=PilasVoxel.Insertar(inventario,pila,indice<9 ? 9 : 0,indice<9 ? 36 : 9);
        if(restante<pila.cantidad) CobrarExperienciaHorno(grupo,indice);
        pila.cantidad=restante; if(pila.Vacia) pila.Limpiar(); SincronizarInventario();
    }
    void CasillaInventario(PilaObjetoVoxel[] grupo,int indice,Rect zona)
    {
        Event evento=Event.current; bool encima=zona.Contains(evento.mousePosition);
        PintarRect(zona,new Color(.22f,.22f,.22f));
        PintarRect(new Rect(zona.x+2,zona.y+2,zona.width-4,zona.height-4),encima ? new Color(.65f,.65f,.65f) : new Color(.48f,.48f,.48f));
        DibujarPila(grupo[indice],new Rect(zona.x+3,zona.y+3,zona.width-6,zona.height-6));
        if(encima && !grupo[indice].Vacia) pilaBajoCursor=grupo[indice];
        if(encima && evento.type==EventType.KeyDown && evento.keyCode>=KeyCode.Alpha1 && evento.keyCode<=KeyCode.Alpha9)
        {
            int destino=evento.keyCode-KeyCode.Alpha1;
            if(AceptaPila(grupo,indice,inventario[destino]))
            {
                var anterior=grupo[indice]; grupo[indice]=inventario[destino]; inventario[destino]=anterior;
                CobrarExperienciaHorno(grupo,indice); SincronizarInventario();
            }
            evento.Use();
        }
        if(encima && evento.type==EventType.MouseDown && (evento.button==0 || evento.button==1))
        {
            if(evento.shift) TransferirPila(grupo,indice);
            else
            {
                bool recoger=cursorObjeto.Vacia;
                MoverPila(grupo,indice,evento.button==1);
                if(recoger && evento.button==0 && !cursorObjeto.Vacia)
                { origenArrastre=grupo; indiceArrastre=indice; inicioArrastre=evento.mousePosition; }
            }
            evento.Use();
        }
        if(encima && evento.type==EventType.MouseUp && evento.button==0 && arrastrandoObjeto &&
            (grupo!=origenArrastre || indice!=indiceArrastre))
        { MoverPila(grupo,indice,false); arrastrandoObjeto=false; origenArrastre=null; evento.Use(); }
    }
    void DibujarPantallaInventario(float ancho, float alto)
    {
        Event evento=Event.current;
        if(evento.type==EventType.MouseDrag && origenArrastre!=null && Vector2.Distance(evento.mousePosition,inicioArrastre)>4f)
            arrastrandoObjeto=true;
        if(textoInterfaz==null) textoInterfaz=new GUIStyle(GUI.skin.label) { fontSize=14, normal={textColor=new Color(.18f,.18f,.18f)} };
        pilaBajoCursor=null;
        PintarRect(new Rect(0,0,ancho,alto),new Color(0,0,0,.68f));
        float escala=Mathf.Clamp(Mathf.Min((ancho-36f)/570f,(alto-90f)/166f),.6f,2f);
        float x=TieneFabricacion ? (ancho-381f*escala)/2f+205f*escala : (ancho-176f*escala)/2f;
        float y=(alto-166f*escala)/2f;
        Rect panel=new Rect(x,y,176f*escala,166f*escala);
        string fondo=estacionAbierta==null ? "inventory" : estacionAbierta.tipo==TipoBloque.MesaTrabajo ? "crafting_table" :
            estacionAbierta.tipo==TipoBloque.Horno ? "furnace" : "generic_54";
        var textura=Resources.Load<Texture2D>("Minecraft/Hud/"+fondo);
        PintarRect(panel,new Color(.78f,.78f,.78f));
        if(textura!=null) { textura.filterMode=FilterMode.Point; textura.wrapMode=TextureWrapMode.Clamp; }
        if(textura!=null && fondo!="generic_54") GUI.DrawTextureWithTexCoords(panel,textura,new Rect(0,1f-166f/textura.height,176f/textura.width,166f/textura.height));
        string titulo=estacionAbierta==null ? "Inventario" : CatalogoObjetosVoxel.De(CatalogoObjetosVoxel.DeBloque(estacionAbierta.tipo)).nombre;
        GUI.Label(new Rect(x+(estacionAbierta==null ? 98 : 8)*escala,y+4*escala,(estacionAbierta==null ? 74 : 150)*escala,20),estacionAbierta==null ? "Fabricación" : titulo,textoInterfaz);
        if(GUI.Button(new Rect(panel.xMax-160,y-25,130,22),"Misiones [M]")) { AbrirMenuMision(); return; }
        if(GUI.Button(new Rect(panel.xMax-20,y-25,20,22),"X")) { CambiarInventario(); return; }
        System.Func<float,float,Rect> celda=(cx,cy)=>new Rect(x+cx*escala,y+cy*escala,18*escala,18*escala);
        if(TieneFabricacion)
        {
            int lado=LadoRejilla; float cx=lado==3 ? 30 : 98, cy=lado==3 ? 17 : 18;
            for(int i=0;i<RejillaActiva.Length;i++) CasillaInventario(RejillaActiva,i,celda(cx+(i%lado)*18,cy+(i/lado)*18));
            Rect resultado=celda(lado==3 ? 124 : 154,lado==3 ? 35 : 28);
            PintarRect(resultado,new Color(.45f,.45f,.45f));
            var receta=RecetasVoxel.Buscar(RejillaActiva,lado);
            if(receta!=null)
            {
                var pila=new PilaObjetoVoxel(receta.resultado,receta.cantidad);
                DibujarPila(pila,new Rect(resultado.x+3,resultado.y+3,resultado.width-6,resultado.height-6));
                if(resultado.Contains(evento.mousePosition))
                {
                    pilaBajoCursor=pila;
                    if(evento.type==EventType.MouseDown && evento.button==0) { TomarFabricacion(evento.shift); evento.Use(); }
                }
            }
            if(lado==2) for(int i=0;i<4;i++) CasillaInventario(armadura,i,celda(8,8+i*18));
            DibujarRecetario(new Rect(x-205*escala,y,198*escala,166*escala));
        }
        else if(estacionAbierta.tipo==TipoBloque.Cofre)
        {
            for(int i=0;i<27;i++) CasillaInventario(estacionAbierta.contenido,i,celda(8+i%9*18,18+i/9*18));
        }
        else
        {
            CasillaInventario(estacionAbierta.contenido,0,celda(56,17));
            CasillaInventario(estacionAbierta.contenido,1,celda(56,53));
            CasillaInventario(estacionAbierta.contenido,2,celda(116,35));
            float coccion=Mathf.Clamp01(estacionAbierta.progreso/BloqueFuncionalVoxel.SegundosCoccion);
            PintarRect(new Rect(x+80*escala,y+39*escala,25*escala*coccion,8*escala),Color.white);
            float fuego=estacionAbierta.combustibleTotal>0 ? estacionAbierta.combustible/estacionAbierta.combustibleTotal : 0;
            PintarRect(new Rect(x+59*escala,y+(50-13*fuego)*escala,12*escala,13*escala*fuego),new Color(1,.5f,.05f));
        }
        for(int i=9;i<36;i++) CasillaInventario(inventario,i,celda(8+(i-9)%9*18,84+(i-9)/9*18));
        for(int i=0;i<9;i++) CasillaInventario(inventario,i,celda(8+i*18,142));
        GUI.color=Color.white;
        GUI.Label(new Rect(15,alto-45,ancho-30,24),"Clic: mover  |  Clic derecho: dividir / poner uno  |  Shift + clic: transferir  |  1–9: enviar a la barra  |  E / I / Esc: cerrar");
        if(Time.unscaledTime<mensajeHasta) GUI.Label(new Rect(15,alto-24,ancho-30,24),mensajeInventario);
        Vector2 raton=evento.mousePosition;
        if(!cursorObjeto.Vacia) DibujarPila(cursorObjeto,new Rect(raton.x-16,raton.y-16,32,32));
        else if(pilaBajoCursor!=null)
        {
            var def=CatalogoObjetosVoxel.De(pilaBajoCursor.id);
            string descripcion=def.nombre+(pilaBajoCursor.permanente ? "\nHerramienta inicial" : def.resistencia>0 ? "\nDurabilidad: "+pilaBajoCursor.resistencia+" / "+def.resistencia : "");
            Rect aviso=new Rect(Mathf.Min(raton.x+15,ancho-220),Mathf.Min(raton.y+15,alto-70),210,descripcion.Contains("\n")?52:30);
            PintarRect(aviso,new Color(.09f,.03f,.15f,.97f)); GUI.Label(aviso,descripcion);
        }
        if(evento.rawType==EventType.MouseUp) { origenArrastre=null; indiceArrastre=-1; arrastrandoObjeto=false; }
    }
    void DibujarRecetario(Rect zona)
    {
        PintarRect(zona,new Color(.78f,.78f,.78f));
        GUI.Label(new Rect(zona.x+8,zona.y+4,zona.width-16,22),"Recetas · pulsa para preparar",textoInterfaz);
        Rect ventana=new Rect(zona.x+5,zona.y+29,zona.width-10,zona.height-34);
        scrollRecetas=GUI.BeginScrollView(ventana,scrollRecetas,new Rect(0,0,ventana.width-20,RecetasVoxel.Todas.Count*45));
        int indice=0;
        foreach(var receta in RecetasVoxel.Todas)
        {
            float y=indice++*45; bool cabe=receta.Cabe(LadoRejilla);
            if(GUI.Button(new Rect(0,y,ventana.width-21,43),"")) PrepararReceta(receta);
            DibujarPila(new PilaObjetoVoxel(receta.resultado,receta.cantidad),new Rect(3,y+4,28,28));
            GUI.Label(new Rect(33,y,ventana.width-54,22),receta.nombre);
            if(!cabe) GUI.Label(new Rect(33,y+20,ventana.width-54,20),"Necesita mesa 3 × 3");
            else
            {
                float x=33;
                foreach(var id in receta.ingredientes)
                    if(id!=IdObjetoVoxel.Ninguno) { DibujarPila(new PilaObjetoVoxel(id),new Rect(x,y+23,15,15)); x+=16; }
            }
        }
        GUI.EndScrollView();
    }
}
