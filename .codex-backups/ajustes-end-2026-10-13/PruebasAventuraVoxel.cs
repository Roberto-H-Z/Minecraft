#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class PruebasAventuraVoxel : MonoBehaviour
{
    private readonly List<string> resultados=new List<string>();
    private string Carpeta => Environment.GetEnvironmentVariable("VOXEL_REPORT_PATH") ?? Path.GetFullPath(".codex-backups/mision-dragon-2026-10-13/pruebas");
    private int errores;
    void Start() { Directory.CreateDirectory(Carpeta); Application.logMessageReceived+=Log; StartCoroutine(Supervisar()); }
    void Log(string mensaje,string stack,LogType tipo) { if(tipo==LogType.Exception || tipo==LogType.Error || tipo==LogType.Assert) { errores++; resultados.Add("ERROR: "+mensaje+"\n"+stack); } }
    static T Campo<T>(object objeto,string nombre) { return (T)objeto.GetType().GetField(nombre,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(objeto); }
    static void Poner(object objeto,string nombre,object valor) { objeto.GetType().GetField(nombre,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(objeto,valor); }
    static void Llamar(object objeto,string nombre) { objeto.GetType().GetMethod(nombre,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(objeto,null); }
    void Comprobar(bool valido,string nombre) { if(!valido) throw new Exception("FALLO: "+nombre); resultados.Add("OK: "+nombre); Debug.Log("VOXEL_TEST OK: "+nombre); }
    IEnumerator Supervisar()
    {
        IEnumerator pruebas=EjecutarPruebas();
        while(true)
        {
            object espera=null; bool continuar=false;
            try { continuar=pruebas.MoveNext(); if(continuar) espera=pruebas.Current; }
            catch(Exception error) { resultados.Add(error.ToString()); Finalizar(false); yield break; }
            if(!continuar) break;
            yield return espera;
        }
        Finalizar(errores==0);
    }
    void Finalizar(bool exito)
    {
        Application.logMessageReceived-=Log; SessionState.SetBool("Voxel.VerificarAventura",false);
        resultados.Add(exito ? "RESULTADO: TODAS LAS COMPROBACIONES PASARON" : "RESULTADO: FALLO");
        File.WriteAllLines(Path.Combine(Carpeta,"resultado.txt"),resultados);
        Debug.Log("VOXEL_TEST FIN: "+exito+" · "+resultados.Count+" registros"); EditorApplication.Exit(exito ? 0 : 1);
    }
    IEnumerator EjecutarPruebas()
    {
        yield return null; yield return null;
        script jugador=FindFirstObjectByType<script>();
        Comprobar(jugador!=null && jugador.MenuInicial,"Menu inicial visible antes de cargar");
        Comprobar(Time.timeScale==0,"Mundo detenido mientras se muestra el menu");
        Comprobar(jugador.PilaEnSlot(3).id==IdObjetoVoxel.ArcoFuego && jugador.PilaEnSlot(3).permanente,"Arco inicial con infinidad");
        Comprobar(jugador.PilaEnSlot(4).id==IdObjetoVoxel.Antorcha && jugador.PilaEnSlot(4).cantidad==64,"Stack inicial de 64 antorchas");
        Comprobar(jugador.PilaEnSlot(5).id==IdObjetoVoxel.ManzanaEncantada && jugador.PilaEnSlot(5).cantidad==16,"Manzanas encantadas iniciales");
        Comprobar(jugador.PuntosArmadura==20 && jugador.GetComponent<ArmaduraDiamanteVoxel>()!=null,"Armadura de diamante equipada");
        Comprobar(Campo<Renderer[]>(jugador.GetComponent<ArmaduraDiamanteVoxel>(),"piezas").Length>=8,"Modelo de armadura cubre cabeza, cuerpo, brazos, piernas y botas");
        jugador.IniciarJuego(); Comprobar(Campo<bool>(jugador,"cargando"),"Iniciar abre pantalla de carga");
        yield return new WaitForSecondsRealtime(1f); Comprobar(Campo<bool>(jugador,"cargando"),"Carga continua antes de dos segundos");
        yield return new WaitForSecondsRealtime(1.2f); Comprobar(!Campo<bool>(jugador,"cargando") && Time.timeScale==1,"Juego comienza tras dos segundos");
        Comprobar(Camera.main.GetComponent<CamaraSeguir>().EnPrimeraPersona,"Inicio en primera persona");
        jugador.slotSeleccionado=5; jugador.hambre=20; Llamar(jugador,"Comer");
        Comprobar(jugador.PilaEnSlot(5).cantidad==15 && jugador.Absorcion==8,"Comer manzana encantada con hambre llena y ganar absorcion");
        int vidas=jugador.vidas; jugador.RecibirDano(4,jugador.transform.position+Vector3.forward*2);
        Comprobar(jugador.vidas==vidas && jugador.Absorcion<8,"Armadura y absorcion protegen del dano");
        Poner(jugador,"corazonesAbsorcion",0f); Poner(jugador,"proteccionManzanaHasta",0f); Poner(jugador,"regeneracionManzanaHasta",0f); Poner(jugador,"danoParcial",0f);
        jugador.vidas=9; Llamar(jugador,"AplicarManzanaEncantada"); yield return new WaitForSeconds(.2f);
        Comprobar(jugador.vidas==10,"Regeneracion de la manzana restaura corazones");
        jugador.AbrirMenuMision(); Comprobar(Time.timeScale==0 && Campo<bool>(jugador,"menuMision"),"Boton de mision pausa y abre oferta");
        var mision=jugador.MisionActual; mision.Aceptar(); Comprobar(mision.EnEnd && mision.Cinematica,"Aceptar transporta al End e inicia cinematica");
        jugador.RecibirAlientoDragon(1000,mision.Dragon.transform.position); Comprobar(!jugador.Muerto,"Cinematica protege al jugador de dano pendiente");
        Comprobar(mision.Arena.Cristales.Length==10 && mision.Arena.CristalesVivos==10,"Diez cristales activos en la arena");
        Comprobar(mision.Dragon.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount==1480,"Modelo del dragon del usuario cargado completo");
        Capturar(mision.Dragon.transform.position+new Vector3(37,17,43),mision.Dragon.transform.position,"dragon-modelo.png");
        Vector3 camaraInicio=Camera.main.transform.position; yield return new WaitForSecondsRealtime(.25f);
        Comprobar(Vector3.Distance(camaraInicio,Camera.main.transform.position)>.1f,"Cinematica mueve la camara");
        mision.TerminarCinematica(); yield return null;
        Comprobar(mision.Dragon.Combate && !mision.Cinematica && Time.timeScale==1,"Cinematica termina y habilita combate");
        Vector3 inicio=mision.Dragon.transform.position; yield return new WaitForSeconds(.25f);
        Comprobar(Vector3.Distance(inicio,mision.Dragon.transform.position)>.1f,"Dragon vuela y se mueve");
        RaycastHit suelo; Vector3 cerca=jugador.transform.position+Vector3.right*jugador.Bloque*2;
        Comprobar(Physics.Raycast(cerca+Vector3.up*2,Vector3.down,out suelo,8,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"Suelo del End tiene colision");
        Comprobar(jugador.MundoActual.ColocarBloque(suelo,TipoBloque.Antorcha),"Colocar antorcha en la arena");
        Physics.SyncTransforms(); yield return new WaitForSeconds(.4f);
        var antorchas=FindObjectsByType<AntorchaVoxel>(FindObjectsSortMode.None);
        Comprobar(antorchas.Length>0 && antorchas[0].Luz.enabled && antorchas[0].Luz.intensity>0,"Antorcha ilumina y conserva su apoyo");
        jugador.AbrirMenuMision(); var regreso=mision.Guardar().retorno; mision.Aceptar();
        Comprobar(mision.EnEnd && !mision.Cinematica && (mision.Guardar().retorno-regreso).sqrMagnitude<.001f,"Continuar combate conserva el retorno al mundo");
        mision.Dragon.enabled=false;
        var cristal=mision.Arena.Cristales[0];
        FlechaFuegoVoxel.Crear(cristal.transform.position+Vector3.forward*12,Vector3.back,jugador.transform,70,20);
        yield return new WaitForSeconds(.35f); Comprobar(!cristal.Vivo && mision.Arena.CristalesVivos==9,"Flecha fisica destruye un cristal");
        mision.Dragon.RestaurarVida(100); mision.Dragon.enabled=true; yield return new WaitForSeconds(.4f);
        Comprobar(mision.Dragon.Vida>100,"Cristales regeneran la barra de vida del dragon");
        mision.Dragon.enabled=false;
        Collider cabeza=Campo<Collider>(mision.Dragon,"cabeza"); float antes=mision.Dragon.Vida;
        Vector3 objetivo=cabeza.bounds.center,origen=objetivo+mision.Dragon.transform.forward*15;
        FlechaFuegoVoxel.Crear(origen,(objetivo-origen).normalized,jugador.transform,70,20);
        yield return new WaitForSeconds(.35f); Comprobar(mision.Dragon.Vida<antes,"Flecha alcanza al dragon y reduce vida interactiva");
        Comprobar(jugador.PilaEnSlot(3).cantidad==1 && jugador.PilaEnSlot(3).resistencia==384,"Arco no consume flechas ni durabilidad inicial");
        foreach(var c in mision.Arena.Cristales) c.DesactivarSinExplosion(); mision.Dragon.RestaurarVida(112);
        float posado=mision.Dragon.Vida; Poner(mision.Dragon,"<Fase>k__BackingField",FaseDragonVoxel.Posado);
        mision.Dragon.RecibirDano(20,null,true); Comprobar(mision.Dragon.Vida==posado,"Dragon posado resiste flechas como en el juego");
        yield return new WaitForSeconds(.2f); mision.Dragon.RecibirDano(12,null,false);
        Comprobar(mision.Dragon.Vida<posado,"Espada puede herir al dragon posado");
        Poner(mision.Dragon,"<Fase>k__BackingField",FaseDragonVoxel.Volando); mision.Dragon.RestaurarVida(112);
        Poner(jugador,"rutaGuardadoPruebas",Path.Combine(Carpeta,"partida-prueba.json")); Llamar(jugador,"GuardarPartida");
        Comprobar(File.Exists(Path.Combine(Carpeta,"partida-prueba.json")),"Guardado aislado del mundo, equipo y mision");
        mision.Dragon.RestaurarVida(25); Llamar(jugador,"CargarPartida"); yield return null;
        mision=jugador.MisionActual;
        Comprobar(mision.EnEnd && Mathf.Abs(mision.Dragon.Vida-112)<1 && mision.Arena.CristalesVivos==0,"Carga restaura vida y cristales de la mision");
        Comprobar(jugador.PuntosArmadura==20 && jugador.PilaEnSlot(5).cantidad==15,"Carga conserva equipo y manzanas consumidas");
        var lucesRestauradas=FindObjectsByType<AntorchaVoxel>(FindObjectsSortMode.None);
        Comprobar(lucesRestauradas.Length>0 && lucesRestauradas[0].GetComponent<BloqueFuncionalVoxel>().delEnd,"Guardado restaura antorcha en la dimension correcta");
        mision.Dragon.enabled=true; float sinCristales=mision.Dragon.Vida; yield return new WaitForSeconds(.25f);
        Comprobar(mision.Dragon.Vida<=sinCristales+.01f,"Sin cristales el dragon deja de regenerarse");
        Poner(jugador,"corazonesAbsorcion",0f); Poner(jugador,"proteccionManzanaHasta",0f); Poner(jugador,"proximoDano",0f);
        jugador.RecibirAlientoDragon(1000,mision.Dragon.transform.position); Comprobar(jugador.Muerto && Time.timeScale==0,"Dragon puede matar y aparece estado de muerte");
        Llamar(jugador,"Reaparecer"); yield return null;
        Comprobar(!jugador.Muerto && !mision.EnEnd && Time.timeScale==1,"Regenerar devuelve al mundo con vida");
        mision.Aceptar(); mision.TerminarCinematica(); yield return null;
        mision.Dragon.RecibirDano(1000,null,false);
        Comprobar(mision.Ganada && mision.Dragon.Vida==0,"Jugador puede matar al dragon y completar la mision");
        jugador.RecibirAlientoDragon(1000,mision.Dragon.transform.position); Comprobar(!jugador.Muerto,"Victoria no se pierde por aliento pendiente del dragon");
        yield return new WaitForSecondsRealtime(3.3f);
        Comprobar(mision.Victoria && Time.timeScale==0,"Pantalla de victoria tras muerte del dragon");
        mision.Regresar(); yield return null;
        Comprobar(!mision.EnEnd && mision.Ganada && Time.timeScale==1,"Continuar vuelve al mundo y conserva victoria");
        jugador.slotSeleccionado=3; Poner(jugador,"preparandoArco",true); Llamar(jugador,"CambiarInventario");
        Comprobar(!Campo<bool>(jugador,"preparandoArco"),"Abrir inventario cancela correctamente la carga del arco"); Llamar(jugador,"CambiarInventario");
        RaycastHit sueloMundo; Vector3 puntoMundo=jugador.transform.position+Vector3.left*jugador.Bloque*3;
        Comprobar(Physics.Raycast(puntoMundo+Vector3.up*5,Vector3.down,out sueloMundo,15,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore),"Suelo del mundo original restaurado");
        Comprobar(jugador.MundoActual.ColocarBloque(sueloMundo,TipoBloque.Antorcha),"Antorcha se puede colocar tambien en el mundo original");
        yield return new WaitForSeconds(.4f);
        RaycastHit apoyoMundo;
        Comprobar(Physics.Raycast(sueloMundo.point+Vector3.up*2+Vector3.right*.6f,Vector3.down,out apoyoMundo,5,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore) && apoyoMundo.collider.GetComponentInParent<MundoVoxel>()!=null,"Antorcha no elimina la cara ni la colision del suelo");
        foreach(var torch in FindObjectsByType<AntorchaVoxel>(FindObjectsSortMode.None))
            if(!torch.GetComponent<BloqueFuncionalVoxel>().delEnd) { Capturar(torch.transform.position+new Vector3(6,3,5),torch.transform.position,"antorcha-mundo.png"); break; }
        jugador.slotSeleccionado=4; yield return null;
        Comprobar(Campo<Mesh>(jugador.GetComponent<VoxelCharacterVisual>(),"mallaAntorcha")!=null,"Antorcha sostenida usa su forma y textura, no un cubo completo");
        Poner(Camera.main.GetComponent<CamaraSeguir>(),"perspectiva",1); yield return null;
        int visiblesArmadura=0; foreach(var pieza in Campo<Renderer[]>(jugador.GetComponent<ArmaduraDiamanteVoxel>(),"piezas")) if(pieza!=null && pieza.enabled) visiblesArmadura++;
        Comprobar(visiblesArmadura>=8,"Armadura de diamante visible en tercera persona");
        Capturar(jugador.transform.position+new Vector3(4,3,-6),jugador.transform.position+Vector3.up*.5f,"armadura-diamante.png");
        Comprobar(errores==0,"Sin errores ni excepciones de Unity durante la partida de prueba");
    }
    void Capturar(Vector3 posicion,Vector3 foco,string nombre)
    {
        var objeto=new GameObject("Camara de verificacion"); var camara=objeto.AddComponent<Camera>(); camara.enabled=false;
        camara.transform.position=posicion; camara.transform.LookAt(foco); camara.clearFlags=CameraClearFlags.SolidColor; camara.backgroundColor=new Color(.16f,.12f,.24f); camara.fieldOfView=40;
        var destino=RenderTexture.GetTemporary(960,640,24); var anterior=RenderTexture.active;
        try { camara.targetTexture=destino; camara.Render(); RenderTexture.active=destino; var imagen=new Texture2D(960,640,TextureFormat.RGB24,false); imagen.ReadPixels(new Rect(0,0,960,640),0,0); imagen.Apply(); File.WriteAllBytes(Path.Combine(Carpeta,nombre),imagen.EncodeToPNG()); Destroy(imagen); }
        finally { RenderTexture.active=anterior; camara.targetTexture=null; RenderTexture.ReleaseTemporary(destino); Destroy(objeto); }
    }
}
#endif
