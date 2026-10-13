using UnityEngine;

// El mismo atlas del paquete se usa en el mundo, en la mano y en el icono.
public static class ModeloCofreVoxel
{
    private static Mesh malla;
    private static Material material;
    private static Texture2D icono;
    public static Material Material
    {
        get { if(material==null) material=MinecraftSkinBox.CrearMaterial("chest/normal"); return material; }
    }
    public static Mesh Malla
    {
        get
        {
            if(malla!=null) return malla;
            GameObject raiz=new GameObject("Modelo temporal del cofre");
            raiz.SetActive(false);
            var piezas=new CombineInstance[3];
            Vector3[] posiciones={new Vector3(0,-.1875f,0),new Vector3(0,.28125f,0),new Vector3(0,.165f,.4525f)};
            Vector3[] escalas={new Vector3(.875f,.625f,.875f),new Vector3(.875f,.3125f,.875f),new Vector3(.125f,.25f,.0625f)};
            for(int i=0;i<3;i++)
            {
                var pieza=new GameObject("Pieza"); pieza.transform.SetParent(raiz.transform,false);
                pieza.AddComponent<MeshFilter>(); pieza.AddComponent<MeshRenderer>();
                MinecraftSkinBox.Aplicar(pieza.transform,Material,0,i==0 ? 19 : 0,i==2 ? 2 : 14,i==0 ? 10 : i==1 ? 5 : 4,i==2 ? 1 : 14);
                piezas[i]=new CombineInstance { mesh=pieza.GetComponent<MeshFilter>().sharedMesh,transform=Matrix4x4.TRS(posiciones[i],Quaternion.identity,escalas[i]) };
            }
            malla=new Mesh {name="Cofre completo con atlas"}; malla.CombineMeshes(piezas,true,true);
            foreach(var pieza in piezas) Object.Destroy(pieza.mesh);
            Object.Destroy(raiz);
            return malla;
        }
    }
    public static Texture2D Icono
    {
        get
        {
            if(icono!=null) return icono;
            GameObject raiz=new GameObject("Render de icono"); raiz.layer=30;
            raiz.transform.position=new Vector3(0,-30000,0);
            raiz.AddComponent<MeshFilter>().sharedMesh=Malla;
            var unlit=new Material(Shader.Find("Unlit/Texture")) {mainTexture=Material.mainTexture};
            raiz.AddComponent<MeshRenderer>().sharedMaterial=unlit;
            var objetoCamara=new GameObject("Camara de icono");
            var camara=objetoCamara.AddComponent<Camera>(); camara.enabled=false;
            camara.clearFlags=CameraClearFlags.SolidColor; camara.backgroundColor=Color.clear;
            camara.cullingMask=1<<30; camara.orthographic=true; camara.orthographicSize=.78f;
            camara.nearClipPlane=.01f; camara.farClipPlane=10; camara.allowHDR=false;
            camara.transform.position=raiz.transform.position+new Vector3(1.6f,1.2f,2);
            camara.transform.LookAt(raiz.transform.position);
            var destino=RenderTexture.GetTemporary(64,64,24,RenderTextureFormat.ARGB32);
            var anterior=RenderTexture.active;
            try
            {
                camara.targetTexture=destino; camara.Render(); RenderTexture.active=destino;
                icono=new Texture2D(64,64,TextureFormat.RGBA32,false) {name="Icono del cofre",filterMode=FilterMode.Point};
                icono.ReadPixels(new Rect(0,0,64,64),0,0); icono.Apply();
            }
            finally
            {
                RenderTexture.active=anterior; camara.targetTexture=null;
                RenderTexture.ReleaseTemporary(destino); raiz.SetActive(false);
                Object.Destroy(raiz); Object.Destroy(objetoCamara); Object.Destroy(unlit);
            }
            return icono;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        if(malla!=null) Object.Destroy(malla); if(material!=null) Object.Destroy(material); if(icono!=null) Object.Destroy(icono);
        malla=null; material=null; icono=null;
    }
}
