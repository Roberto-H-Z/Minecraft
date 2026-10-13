using UnityEngine;

public static class SaltoAutomaticoVoxel
{
    static bool EsSuelo(Collider otro, Collider cuerpo)
    {
        return otro != null && !otro.isTrigger && !otro.transform.IsChildOf(cuerpo.transform) &&
            otro.GetComponentInParent<script>() == null && otro.GetComponentInParent<enemigo>() == null &&
            otro.GetComponentInParent<AnimalVoxel>() == null && otro.GetComponentInParent<ObjetoRecogibleVoxel>() == null;
    }

    public static bool HaySueloDebajo(Vector3 origen, float distancia, Collider cuerpo)
    {
        foreach (RaycastHit golpe in Physics.RaycastAll(origen, Vector3.down, distancia,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (EsSuelo(golpe.collider, cuerpo) && golpe.normal.y > .6f) return true;
        return false;
    }

    public static bool EnSuelo(Rigidbody fisica, Collider cuerpo)
    {
        if (fisica == null || cuerpo == null || fisica.velocity.y > .15f) return false;
        float radio = Mathf.Min(cuerpo.bounds.extents.x, cuerpo.bounds.extents.z) * .70f;
        foreach (RaycastHit golpe in Physics.SphereCastAll(cuerpo.bounds.center, radio, Vector3.down,
            cuerpo.bounds.extents.y - radio + .10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (EsSuelo(golpe.collider, cuerpo) && golpe.normal.y > .6f) return true;
        return false;
    }

    public static bool HayBloque(Rigidbody fisica, Collider cuerpo, Vector3 movimiento, float tamanoBloque)
    {
        if (fisica == null || cuerpo == null || movimiento.sqrMagnitude < .01f || fisica.velocity.y > .15f) return false;
        Vector3 direccion = movimiento.normalized;
        float radio = Mathf.Min(cuerpo.bounds.extents.x, cuerpo.bounds.extents.z);
        Vector3 pies = cuerpo.bounds.center - Vector3.up * (cuerpo.bounds.extents.y - .12f);
        float alcance = radio + tamanoBloque * .65f;
        Vector3 lateral = Vector3.Cross(Vector3.up, direccion) * radio * .65f;
        for (int i = -1; i <= 1; i++)
        {
            Vector3 origen = pies + lateral * i;
            foreach (RaycastHit golpe in Physics.RaycastAll(origen, direccion, alcance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!EsSuelo(golpe.collider, cuerpo) || Mathf.Abs(golpe.normal.y) > .35f) continue;
                bool bloqueAlto = false;
                foreach (RaycastHit alto in Physics.RaycastAll(origen + Vector3.up * (tamanoBloque + .14f),
                    direccion, golpe.distance + .05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (EsSuelo(alto.collider, cuerpo)) { bloqueAlto = true; break; }
                if (bloqueAlto) continue;
                foreach (RaycastHit techo in Physics.SphereCastAll(cuerpo.bounds.center, radio * .85f,
                    Vector3.up, cuerpo.bounds.extents.y - radio + tamanoBloque * 1.2f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    if (EsSuelo(techo.collider, cuerpo)) return false;
                return true;
            }
        }
        return false;
    }
}
