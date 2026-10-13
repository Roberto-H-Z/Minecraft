#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerificarAventuraVoxel
{
    static VerificarAventuraVoxel()
    {
        if(SessionState.GetBool("Voxel.VerificarAventura",false)) EditorApplication.update+=IniciarCuandoEsteListo;
    }
    public static void Ejecutar()
    {
        SessionState.SetBool("Voxel.VerificarAventura",true);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.update-=IniciarCuandoEsteListo; EditorApplication.update+=IniciarCuandoEsteListo;
        EditorApplication.isPlaying=true;
    }
    static void IniciarCuandoEsteListo()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isCompiling || UnityEngine.Object.FindFirstObjectByType<script>()==null) return;
        EditorApplication.update-=IniciarCuandoEsteListo;
        new GameObject("Verificacion temporal de aventura").AddComponent<PruebasAventuraVoxel>();
    }
}

#endif
