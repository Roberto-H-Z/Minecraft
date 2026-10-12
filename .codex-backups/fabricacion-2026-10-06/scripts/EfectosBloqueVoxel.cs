using UnityEngine;

public static class EfectosBloqueVoxel
{
    public static void Emitir(Vector3 posicion, Material material)
    {
        if (material == null) return;
        for (int i = 0; i < 8; i++)
        {
            GameObject fragmento = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fragmento.name = "Fragmento del bloque";
            fragmento.layer = 2;
            fragmento.transform.position = posicion + Random.insideUnitSphere * .2f;
            fragmento.transform.localScale = Vector3.one * Random.Range(.06f, .13f);
            fragmento.GetComponent<Renderer>().sharedMaterial = material;
            Collider colision = fragmento.GetComponent<Collider>();
            colision.enabled = false;
            Object.Destroy(colision);
            Rigidbody fisica = fragmento.AddComponent<Rigidbody>();
            fisica.velocity = Random.insideUnitSphere * 2f + Vector3.up * 2f;
            fisica.angularVelocity = Random.insideUnitSphere * 8f;
            Object.Destroy(fragmento, .55f);
        }
    }
}
