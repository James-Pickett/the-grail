using UnityEditor;
using UnityEngine;

public class TextureArrayWizard : ScriptableWizard
{
    public Texture2D[] textures;

    [MenuItem(itemName: "Assets/Create/Texture Array")]
    private static void CreateWizard()
    {
        DisplayWizard<TextureArrayWizard>(
            title: "Create Texture Array", createButtonName: "Create"
        );
    }

    private void OnWizardCreate()
    {
        if (textures.Length == 0)
        {
            return;
        }

        var path = EditorUtility.SaveFilePanelInProject(
            title: "Save Texture Array", defaultName: "Texture Array", extension: "asset", message: "Save Texture Array"
        );
        if (path.Length == 0)
        {
            return;
        }

        var t = textures[0];
        var textureArray = new Texture2DArray(
            width: t.width, height: t.height, depth: textures.Length, textureFormat: t.format,
            mipChain: t.mipmapCount > 1
        );
        textureArray.anisoLevel = t.anisoLevel;
        textureArray.filterMode = t.filterMode;
        textureArray.wrapMode = t.wrapMode;

        for (var i = 0; i < textures.Length; i++)
        {
            for (var m = 0; m < t.mipmapCount; m++)
            {
                Graphics.CopyTexture(src: textures[i], srcElement: 0, srcMip: m, dst: textureArray, dstElement: i,
                    dstMip: m);
            }
        }

        AssetDatabase.CreateAsset(asset: textureArray, path: path);
    }
}