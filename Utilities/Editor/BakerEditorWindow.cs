using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

#if UNITY_EDITOR
public class BakerEditorWindow : EditorWindow
{
    private VisualElement root;
    private VisualElement imageElement;
    private ObjectField materialField;
    private IntegerField width;
    private IntegerField height;
    private Button bakeButton;

    private const string SavePath = "Assets/BakedTextures/";

    [MenuItem("Window/Zorrendor/Material Baker")]
    public static void ShowWindow()
    {
        BakerEditorWindow wnd = GetWindow<BakerEditorWindow>();
        wnd.titleContent = new GUIContent("Material Baker");
    }

    public void CreateGUI()
    {
        root = rootVisualElement;
        
        materialField = new ObjectField("Material")
        {
            objectType = typeof(Material),
            allowSceneObjects = false
        };
        root.Add(materialField);

        width = new IntegerField("Width") { value = 256 };
        height = new IntegerField("Height") { value = 256 };

        root.Add(width);
        root.Add(height);

        bakeButton = new Button(OnBakeClicked) { text = "Bake" };
        root.Add(bakeButton);
        
        imageElement = new VisualElement
        {
            style =
            {
                width = new Length(100, LengthUnit.Percent),
                height = new Length(100, LengthUnit.Percent)
            }
        };
        root.Add(imageElement);
    }

    private void OnBakeClicked()
    {
        Material material = materialField.value as Material;
        if (material == null)
        {
            Debug.LogError("Material is null");
            return;
        }
        int textureWidth = width.value;
        int textureHeight = height.value;
        RenderTexture renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
        RenderTexture.active = renderTexture;

        Texture2D bakedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
        
        Graphics.Blit(null, renderTexture, material, 0);

        bakedTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        bakedTexture.Apply();
        
        byte[] bytes = bakedTexture.EncodeToPNG();
        if (!Directory.Exists(SavePath))
        {
            Directory.CreateDirectory(SavePath);
        }
        File.WriteAllBytes(SavePath + "BakedTexture.png", bytes);
        
        RenderTexture.active = null;
        renderTexture.Release();
        
        imageElement.style.backgroundImage = bakedTexture;

        Debug.Log("Material baked to texture and saved at: " + SavePath + "BakedTexture.png");
    }
}
#endif