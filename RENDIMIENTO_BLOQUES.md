# Actualización local del terreno

## Causa encontrada

Poner o quitar un bloque recorría todas las columnas del mapa de 192 × 192, generaba una malla nueva y volvía a preparar todas sus colisiones. Si se quitaba el suelo de una planta, también se destruían y creaban de nuevo las mallas y materiales de toda la vegetación.

## Cambios

- Terreno dividido en sectores de **16 × 16 columnas**, cada uno con su malla y colisión.
- Editar un bloque actualiza su sector y, si está en un límite, los sectores vecinos que necesitan mostrar u ocultar una cara. Las esquinas no generan huecos entre sectores.
- El mapa tiene 144 sectores. Una edición individual procesa **uno, dos o tres**, según su posición; no recorre los 144.
- Se reutilizan mallas, materiales y listas de trabajo. Las normales de las caras de los bloques se calculan directamente al generar cada cara.
- Los bloques colocados se indexan por sector. Tampoco se recorre la lista de todas las construcciones en cada edición.
- La vegetación está dividida en los mismos sectores y comparte cuatro materiales. Solo se actualiza la vegetación local cuando cambia su suelo o el bloque inmediatamente encima.
- Mesas, cofres y hornos se actualizan individualmente al colocarlos o quitarlos.
- Las explosiones agrupan los cambios y reconstruyen una vez cada sector afectado.
- La reconstrucción completa queda reservada a generar el mundo y cargar un guardado.

Las colisiones, grietas, contornos, recogida de bloques y comprobación del espacio al construir reconocen todos los sectores. Los datos del guardado no cambian de formato.

## Comprobación y medición

Los scripts se compilan con el compilador C# y las referencias de Unity 2023.2.20f1 del proyecto. No se ejecutaron pruebas en Play ni se midieron FPS durante este cambio; la reducción descrita corresponde al trabajo eliminado del código, no a un resultado de rendimiento medido.

En el Profiler de Unity quedan disponibles los marcadores `Voxel.ActualizarSector16` y `Voxel.ActualizarVegetacionLocal` para medir el coste de las actualizaciones locales en este equipo.

Las mallas reutilizadas usan [Mesh.MarkDynamic](https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Mesh.MarkDynamic.html). Las opciones de preparación de colisiones siguen la [documentación de MeshCollider de Unity 2023.2](https://docs.unity3d.com/2023.2/Documentation/Manual/prepare-mesh-for-mesh-collider.html); se omite la limpieza y soldadura porque las caras que genera el terreno no contienen triángulos degenerados.
