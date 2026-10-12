using UnityEngine;

public static class SaltoAutomaticoVoxel
{
    public static bool EnSuelo(Rigidbody fisica, Collider cuerpo)
    {
        if (fisica == null || cuerpo == null) return false;
        float distancia = cuerpo.bounds.extents.y + 0.32f;
        foreach (RaycastHit impacto in Physics.RaycastAll(fisica.position, Vector3.down, distancia,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (impacto.collider != cuerpo && impacto.collider is MeshCollider && impacto.normal.y > 0.45f)
                return true;
        return false;
    }

    public static bool HayBloque(Rigidbody fisica, Collider cuerpo, Vector3 movimiento, float tamanoBloque)
    {
        if (fisica == null || cuerpo == null || movimiento.sqrMagnitude < 0.01f) return false;
        Vector3 direccion = movimiento.normalized;
        Vector3 pies = fisica.position - Vector3.up * (cuerpo.bounds.extents.y - 0.35f);
        Vector3 lateral = Vector3.Cross(Vector3.up, direccion) *
            Mathf.Min(0.4f, Mathf.Min(cuerpo.bounds.extents.x, cuerpo.bounds.extents.z) * 0.55f);
        float alcance = Mathf.Max(cuerpo.bounds.extents.x, cuerpo.bounds.extents.z) + 1f;

        for (int i = -1; i <= 1; i++)
        {
            Vector3 origen = pies + lateral * i;
            foreach (RaycastHit golpe in Physics.RaycastAll(origen, direccion, alcance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (golpe.collider == cuerpo || !(golpe.collider is MeshCollider) ||
                    Mathf.Abs(golpe.normal.y) > 0.35f) continue;
                bool bloqueMasAlto = false;
                foreach (RaycastHit alto in Physics.RaycastAll(origen + Vector3.up * (tamanoBloque + 0.45f),
                    direccion, golpe.distance + 0.15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (alto.collider is MeshCollider && alto.collider != cuerpo)
                    {
                        bloqueMasAlto = true;
                        break;
                    }
                if (!bloqueMasAlto) return true;
            }
        }
        return false;
    }
}
