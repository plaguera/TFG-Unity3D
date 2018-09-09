using System.Linq;
using System.Collections.Generic;

public class Image
{
    public static List<Image> Instances = new List<Image>();

    public static Image Instantiate(string name, UnityEngine.Texture2D data)
    {
        Instances.Add(new Image(name, data));
        return Instances.Last();
    }

    public static void DestroyInstances(string image)
    {
        List<Image> aux = Instances.FindAll((obj) => obj.Name == image);
        foreach (Image i in aux) i.Destroy();
    }

    string name;
    UnityEngine.Texture2D data;
    UnityEngine.GameObject gameObject;
    UnityEngine.UI.Toggle toggle;

    public string Name { get { return name; } }
    public UnityEngine.Texture2D Data { get { return data; } }
    public UnityEngine.GameObject GameObject { get { return gameObject; } }
    public UnityEngine.UI.Toggle Toggle { get { return toggle; } }

    Image(string name_, UnityEngine.Texture2D data_)
    {
        name = name_;
        data = data_;
    }

    public void SetGameObject(UnityEngine.GameObject @object)
    {
        gameObject = @object;
        toggle = GameObject.GetComponent<UnityEngine.UI.Toggle>();
        GameObject.transform.Find("Content").GetComponent<UnityEngine.UI.RawImage>().texture = Data;
        toggle.onValueChanged.AddListener(delegate { ToggleValueChanged(Toggle); });
    }

    void ToggleValueChanged(UnityEngine.UI.Toggle change)
    {
        if (toggle.isOn) GameManager.Instance.ImageSelection = this;
        else GameManager.Instance.ImageSelection = null;
    }

    public void Destroy()
    {
        UnityEngine.Object.Destroy(GameObject);
        Instances.Remove(this);
    }
}
