﻿using UnityEngine;
using System.Collections;

public class TitleScene : MonoBehaviour {
    
    [SerializeField] private string mainCameraPath;
    [SerializeField] private string subCameraPath;
    [SerializeField] private string lightPath;
    [SerializeField] private string renderTexturePath;
    [SerializeField] private string titleNodeControllerPath;
    [SerializeField] private string backGroundPath;
    [SerializeField] private string titleCanvasPath;
    [SerializeField] private string eventSystemPath;
    
    private GameObject mainCameraObject;
    private GameObject subCameraObject;
    private GameObject lightObject;
    private GameObject renderTextureObject;
    private GameObject titleNodeControllerObject;
    private GameObject backGroundObject;
    private GameObject titleCanvasObject;
    private GameObject eventSystemObject;

	// Use this for initialization
	void Start () {
	    mainCameraObject = Instantiate(Resources.Load<GameObject>(mainCameraPath));
        mainCameraObject.transform.SetParent(transform);

	    subCameraObject = Instantiate(Resources.Load<GameObject>(subCameraPath));
        subCameraObject.transform.SetParent(transform);

	    lightObject = Instantiate(Resources.Load<GameObject>(lightPath));
        lightObject.transform.SetParent(transform);

	    renderTextureObject = Instantiate(Resources.Load<GameObject>(renderTexturePath));
        renderTextureObject.transform.SetParent(transform);

	    titleNodeControllerObject = Instantiate(Resources.Load<GameObject>(titleNodeControllerPath));
        titleNodeControllerObject.transform.SetParent(transform);

	    backGroundObject = Instantiate(Resources.Load<GameObject>(backGroundPath));
        backGroundObject.transform.SetParent(transform);

	    titleCanvasObject = Instantiate(Resources.Load<GameObject>(titleCanvasPath));
        titleCanvasObject.transform.SetParent(transform);

	    eventSystemObject = Instantiate(Resources.Load<GameObject>(eventSystemPath));
        eventSystemObject.transform.SetParent(transform);
    }
}
