﻿using UnityEngine;
using System.Collections;

public class LevelChange : MonoBehaviour {


    [SerializeField]private string fieldMapPath = null;
    [SerializeField]private string subCameraPath = null;
    [SerializeField]private string levelPanelPath = null;

    private LevelController _levelController;
    public LevelController levelController { get { return _levelController; } set { _levelController = value; } }
    private MapField[] _mapFields;
    public MapField[] mapField { get { return _mapFields; } }
    private LevelPanel _levelPanel;
    public LevelPanel levelPanel { get { return _levelPanel; } }

	// Use this for initialization
    void Start()
    {
        Instantiate(Resources.Load<GameObject>(fieldMapPath)).transform.SetParent(transform);
        GameObject subCamera = Instantiate(Resources.Load<GameObject>(subCameraPath));//.transform.SetParent(transform);
        GameObject lPanel = Instantiate(Resources.Load<GameObject>(levelPanelPath));

        lPanel.transform.SetParent(transform);
        subCamera.transform.SetParent(transform);
        subCamera.transform.Rotate(new Vector3(0.0f,0.0f,-_levelController.LyingAngle));

        _mapFields = GetComponentsInChildren<MapField>();
        _levelPanel = lPanel.GetComponentInChildren<LevelPanel>();
        _levelPanel.SetLevelController(_levelController);

        LevelCanvas lCanvas = lPanel.GetComponent<LevelCanvas>();
        lCanvas.SetCamera(subCamera.GetComponent<Camera>(),_levelController.LyingAngle);
    }
	// Update is called once per frame
	void Update () {
        
	}
    public void Delete()
    {
        GameScene gameScene = transform.root.gameObject.GetComponent<AppliController>().GetCurrentScene().GetComponent<GameScene>();
        gameScene.mainCamera.enabled = true;
        _levelController.EndComplete();
        
    }
}