using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UnityEngine.UI.Image))]
public class FlexibleUIImage : FlexibleUI

{
    private UnityEngine.UI.Image image;

    void Awake()
    {
        image = GetComponent<UnityEngine.UI.Image>();
        base.Initialize();
    }

    protected override void OnSkinUI()
    {
        base.OnSkinUI();
        image.color = flexibleUIData.imageColor;
    }



}
