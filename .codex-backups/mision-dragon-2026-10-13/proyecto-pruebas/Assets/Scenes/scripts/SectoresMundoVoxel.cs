using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

public partial class MundoVoxel
{
    private const int LadoSector=16;
    private sealed class SectorVoxel
    {
        public Vector2Int indice;
        public MeshFilter filtro, filtroPlantas;
        public MeshRenderer renderizador, renderizadorPlantas;
        public MeshCollider colision;
        public Mesh malla, mallaPlantas;
        public readonly HashSet<Vector3Int> colocados=new HashSet<Vector3Int>();
        public readonly List<PlantaSectorVoxel> plantas=new List<PlantaSectorVoxel>();
    }
    private struct PlantaSectorVoxel { public Vector2Int celda; public int especie; }
    private readonly Dictionary<Vector2Int,SectorVoxel> sectores=new Dictionary<Vector2Int,SectorVoxel>();
    private readonly HashSet<Collider> colisionesTerreno=new HashSet<Collider>();
    private readonly HashSet<Vector2Int> sectoresSucios=new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> vegetacionSucia=new HashSet<Vector2Int>();
    private readonly List<Vector3> normales=new List<Vector3>(4096);
    private readonly List<Vector3> puntosPlantas=new List<Vector3>(2048);
    private readonly List<Vector2> uvPlantas=new List<Vector2>(2048);
    private readonly List<int>[] carasPlantas={new List<int>(),new List<int>(),new List<int>(),new List<int>()};
    private Material[] materialesVegetacion;
    private static readonly ProfilerMarker marcadorSector=new ProfilerMarker("Voxel.ActualizarSector16");
    private static readonly ProfilerMarker marcadorVegetacion=new ProfilerMarker("Voxel.ActualizarVegetacionLocal");

    bool EsColisionTerreno(Collider collider) => collider!=null && colisionesTerreno.Contains(collider);
    Vector2Int IndiceSector(int x,int z) => new Vector2Int(x/LadoSector,z/LadoSector);
    void CrearSectores()
    {
        if(sectores.Count>0) return;
        for(int x=0;x<ancho;x+=LadoSector)
        for(int z=0;z<largo;z+=LadoSector)
        {
            var indice=IndiceSector(x,z);
            var objeto=new GameObject("Sector de terreno "+indice.x+", "+indice.y);
            objeto.transform.SetParent(raizGenerada,false);
            var sector=new SectorVoxel {indice=indice};
            sector.filtro=objeto.AddComponent<MeshFilter>();
            sector.renderizador=objeto.AddComponent<MeshRenderer>();
            sector.renderizador.sharedMaterials=materiales;
            sector.colision=objeto.AddComponent<MeshCollider>();
            // Las caras generadas no tienen triángulos degenerados ni requieren soldar vértices.
            sector.colision.cookingOptions=MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.UseFastMidphase;
            sector.malla=new Mesh {name="Terreno sector "+indice,indexFormat=IndexFormat.UInt32,
                hideFlags=HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild};
            sector.malla.MarkDynamic(); sector.filtro.sharedMesh=sector.malla;
            sectores.Add(indice,sector); colisionesTerreno.Add(sector.colision);
        }
    }
    // Se usa solo al generar el mundo o cargar un guardado completo.
    void ReconstruirSuelo()
    {
        CrearSectores();
        foreach(var sector in sectores.Values) sector.colocados.Clear();
        foreach(var bloque in bloquesColocados)
            if(sectores.TryGetValue(IndiceSector(bloque.Key.x,bloque.Key.z),out var sector)) sector.colocados.Add(bloque.Key);
        foreach(var sector in sectores.Values) ReconstruirSector(sector);
        sectoresSucios.Clear(); vegetacionSucia.Clear();
        if(Application.isPlaying) ActualizarEspeciales();
    }
    void ReconstruirSector(SectorVoxel sector)
    {
        using(marcadorSector.Auto())
        {
            vertices.Clear(); coordenadasTextura.Clear(); normales.Clear();
            foreach(var indices in triangulos) indices.Clear();
            int x0=sector.indice.x*LadoSector,z0=sector.indice.y*LadoSector;
            for(int x=x0;x<Mathf.Min(x0+LadoSector,ancho);x++)
            for(int z=z0;z<Mathf.Min(z0+LadoSector,largo);z++)
            for(int y=FondoRoca;y<=alturas[x,z];y++)
            {
                var celda=new Vector3Int(x,y,z);
                if(!bloquesColocados.ContainsKey(celda)) AgregarBloqueVisible(celda);
            }
            foreach(var celda in sector.colocados) AgregarBloqueVisible(celda);
            sector.colision.sharedMesh=null;
            Mesh malla=sector.malla; malla.Clear();
            malla.SetVertices(vertices); malla.SetUVs(0,coordenadasTextura); malla.SetNormals(normales);
            malla.subMeshCount=triangulos.Length;
            for(int i=0;i<triangulos.Length;i++) malla.SetTriangles(triangulos[i],i,false);
            malla.RecalculateBounds();
            sector.renderizador.enabled=vertices.Count>0;
            if(vertices.Count>0) sector.colision.sharedMesh=malla;
        }
    }
    void MarcarSector(int x,int z)
    {
        if(x>=0 && x<ancho && z>=0 && z<largo) sectoresSucios.Add(IndiceSector(x,z));
    }
    void MarcarCeldaModificada(Vector3Int celda)
    {
        var indice=IndiceSector(celda.x,celda.z);
        if(sectores.TryGetValue(indice,out var sector))
        {
            if(bloquesColocados.ContainsKey(celda)) sector.colocados.Add(celda);
            else sector.colocados.Remove(celda);
            if(sector.plantas.Count>0 && celda.y>=alturas[celda.x,celda.z] && celda.y<=alturas[celda.x,celda.z]+1)
                vegetacionSucia.Add(indice);
        }
        MarcarSector(celda.x,celda.z);
        MarcarSector(celda.x-1,celda.z); MarcarSector(celda.x+1,celda.z);
        MarcarSector(celda.x,celda.z-1); MarcarSector(celda.x,celda.z+1);
    }
    void ActualizarSectoresModificados()
    {
        foreach(var indice in sectoresSucios)
            if(sectores.TryGetValue(indice,out var sector)) ReconstruirSector(sector);
        foreach(var indice in vegetacionSucia)
            if(sectores.TryGetValue(indice,out var sector)) ReconstruirVegetacionSector(sector);
        sectoresSucios.Clear(); vegetacionSucia.Clear();
    }
    void ActualizarCelda(Vector3Int celda)
    {
        ActualizarEspecial(celda); MarcarCeldaModificada(celda); ActualizarSectoresModificados();
    }

    void CrearVegetacion(Transform padre)
    {
        if(vegetacionGenerada!=null)
        {
            vegetacionGenerada.gameObject.SetActive(false);
            DestruirRecurso(vegetacionGenerada.gameObject);
        }
        vegetacionGenerada=new GameObject("Pasto y flores por sectores").transform;
        vegetacionGenerada.SetParent(padre,false);
        celdasConPlantas.Clear();
        foreach(var sector in sectores.Values)
        {
            sector.plantas.Clear(); DestruirRecurso(sector.mallaPlantas);
            sector.mallaPlantas=null; sector.filtroPlantas=null; sector.renderizadorPlantas=null;
        }
        if(materialesVegetacion==null)
        {
            string[] especies={"short_grass","fern","poppy","dandelion"};
            materialesVegetacion=new Material[4];
            for(int i=0;i<4;i++) materialesVegetacion[i]=CrearMaterialPlanta(especies[i],i<2 ? new Color(.55f,.82f,.39f) : Color.white);
        }
        float escala=ancho*largo/(64f*64f);
        int[] cantidades={Mathf.RoundToInt(340*escala),Mathf.RoundToInt(95*escala),Mathf.RoundToInt(90*escala),Mathf.RoundToInt(80*escala)};
        var azar=new System.Random(semilla+812);
        for(int especie=0;especie<4;especie++)
        for(int i=0;i<cantidades[especie];i++)
        {
            int x=azar.Next(1,ancho-1),z=azar.Next(1,largo-1);
            if(celdasAgua[x,z] || celdasArena[x,z]) continue;
            sectores[IndiceSector(x,z)].plantas.Add(new PlantaSectorVoxel {celda=new Vector2Int(x,z),especie=especie});
        }
        foreach(var sector in sectores.Values)
            if(sector.plantas.Count>0) ReconstruirVegetacionSector(sector);
    }
    void ReconstruirVegetacionSector(SectorVoxel sector)
    {
        using(marcadorVegetacion.Auto())
        {
            puntosPlantas.Clear(); uvPlantas.Clear(); foreach(var indices in carasPlantas) indices.Clear();
            foreach(var planta in sector.plantas) celdasConPlantas.Remove(planta.celda);
            foreach(var planta in sector.plantas)
            {
                int x=planta.celda.x,z=planta.celda.y;
                if(plantasQuitadas.Contains(planta.celda) || !ExisteBloque(new Vector3Int(x,alturas[x,z],z)) ||
                    bloquesColocados.ContainsKey(new Vector3Int(x,alturas[x,z]+1,z))) continue;
                celdasConPlantas.Add(planta.celda);
                var centro=new Vector3((x-ancho*.5f+.5f)*tamanoBloque,(alturas[x,z]+.5f)*tamanoBloque,(z-largo*.5f+.5f)*tamanoBloque);
                AgregarPlanta(puntosPlantas,uvPlantas,carasPlantas[planta.especie],centro,
                    tamanoBloque*(planta.especie<2 ? .55f : .48f),tamanoBloque*(planta.especie<2 ? .68f : .75f));
            }
            if(sector.mallaPlantas==null)
            {
                var objeto=new GameObject("Vegetacion sector "+sector.indice);
                objeto.transform.SetParent(vegetacionGenerada,false);
                sector.filtroPlantas=objeto.AddComponent<MeshFilter>(); sector.renderizadorPlantas=objeto.AddComponent<MeshRenderer>();
                sector.renderizadorPlantas.sharedMaterials=materialesVegetacion;
                sector.renderizadorPlantas.shadowCastingMode=ShadowCastingMode.Off;
                sector.mallaPlantas=new Mesh {name="Vegetacion sector "+sector.indice,
                    hideFlags=HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild};
                sector.mallaPlantas.MarkDynamic(); sector.filtroPlantas.sharedMesh=sector.mallaPlantas;
            }
            Mesh malla=sector.mallaPlantas; malla.Clear();
            malla.SetVertices(puntosPlantas); malla.SetUVs(0,uvPlantas); malla.subMeshCount=4;
            for(int i=0;i<4;i++) malla.SetTriangles(carasPlantas[i],i,false);
            malla.RecalculateNormals(); malla.RecalculateBounds(); sector.renderizadorPlantas.enabled=puntosPlantas.Count>0;
        }
    }
    void LiberarSectores()
    {
        foreach(var sector in sectores.Values) { DestruirRecurso(sector.malla); DestruirRecurso(sector.mallaPlantas); }
        sectores.Clear(); colisionesTerreno.Clear(); sectoresSucios.Clear(); vegetacionSucia.Clear();
        if(materialesVegetacion!=null) foreach(var material in materialesVegetacion) DestruirRecurso(material);
        materialesVegetacion=null;
    }
    void DestruirRecurso(Object recurso)
    {
        if(recurso==null) return;
        if(Application.isPlaying) Destroy(recurso); else DestroyImmediate(recurso);
    }
}
