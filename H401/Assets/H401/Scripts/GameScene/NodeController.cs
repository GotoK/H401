﻿using UnityEngine;
using System.Collections;
using UniRx;

using DG.Tweening;
using System.Collections.Generic;
using RandExtension;
using BitArrayExtension;
/*  リストIDに関して
col のIDが奇数の行は +1 とする

◇◇・・・   (col, row)
・・・・◇◇  (col - 1, row + 1)
・・・・・
・・・・・・
◇◇◇・・
◇◇◇・・・
◇◇・・・
(0,0)
*/

public class NodeController : MonoBehaviour {

//    private const float ADJUST_PIXELS_PER_UNIT = 0.01f;     // Pixels Per Unit の調整値
//    private readonly Square GAME_AREA = new Square(0.0f, 0.0f, 5.0f * 2.0f * 750.0f / 1334.0f, 5.0f * 2.0f);    // ゲームの画面領域(パズル領域)
    private const float FRAME_POSZ_MARGIN = -1.0f;          // フレームとノードとの距離(Z座標)
    private const float NODE_EASE_STOP_THRESHOLD = 0.01f;    // ノードの easing を終了するための、タップ位置とノード位置との閾値

    [SerializeField] private int row = 0;       // 横配置数 リストIDが奇数の行は＋１とする
    [SerializeField] private int col = 0;       // 縦配置数
    [SerializeField] private string gameNodePrefabPath  = null;     // ノードのプレハブのパス
    [SerializeField] private string frameNodePrefabPath = null;     // フレームノードのプレハブのパス
    [SerializeField] private string floorNodePrefabPath = null;     // 下端ノードのプレハブのパス
    [SerializeField] private string treeControllerPrefabPath  = null;     // 完成ノードのプレハブのパス
    [SerializeField] private string unChainControllerPath = null;
    [SerializeField] private float widthMargin  = 0.0f;  // ノード位置の左右間隔の調整値
    [SerializeField] private float heightMargin = 0.0f;  // ノード位置の上下間隔の調整値
    [SerializeField] private float headerHeight = 0.0f;  // ヘッダーの高さ
//    [SerializeField] private string levelTableObjectPath = null;
//    [SerializeField] private string levelControllerObjectPath = null;
//    [SerializeField] private string pauseObjectPath = null;
    [SerializeField] private float repRotateTime = 0;//ノード再配置時の時間
    [SerializeField] private string floorLeftNodeMaterialPath = null;    // 左下端ノードのマテリアルのパス
    [SerializeField] private string floorRightNodeMaterialPath = null;   // 右下端ノードのマテリアルのパス
//    [SerializeField] private string[] nodeMaterialsPath = null;
    [SerializeField] private NodeTemplate[] NodeTemp = null;
    
    private GameObject gameNodePrefab   = null;     // ノードのプレハブ
    private GameObject frameNodePrefab  = null;     // フレームノードのプレハブ
    private GameObject floorNodePrefab  = null;     // 下端ノードのプレハブ
    private GameObject treeControllerPrefab   = null;     // 完成ノードのプレハブ
    private GameObject unChainControllerPrefab = null;

    private GameObject[][]  gameNodePrefabs;    // ノードのプレハブリスト
    private Node[][]        gameNodeScripts;        // ノードのnodeスクリプトリスト
    private Vector3[][]     nodePlacePosList;   // ノードの配置位置リスト
    private GameObject      frameController;    // フレームコントローラープレハブ

	private Square  gameArea = Square.zero;     // ゲームの画面領域(パズル領域)
	private Vector2 nodeSize = Vector2.zero;    // 描画するノードのサイズ

	private bool        isTap           = false;                // タップ成功フラグ
	private bool        isSlide         = false;                // ノードスライドフラグ
	private bool        isNodeAction    = false;                // ノードがアクション中かフラグ
	private bool        isSlideEnd      = false;                // ノードがスライド終了処理中かフラグ
	private Vec2Int     tapNodeID       = Vec2Int.zero;         // タップしているノードのID
	private _eSlideDir  slideDir        = _eSlideDir.NONE;      // スライド中の方向
	private Vector2     moveNodeInitPos = Vector2.zero;         // 移動中ノードの移動開始位置
	private Vector2     moveNodeDist    = Vector2.zero;         // 移動中ノードの基本移動量(移動方向ベクトル)
	private Vector2     moveNodeDistAbs = Vector2.zero;         // 移動中ノードの基本移動量の絶対値
    private Vector2     tapPosMoveNodePosDist = Vector2.zero;   // タップしているノードの位置と、タップしている位置との距離

	private Vector2 startTapPos = Vector2.zero;     // タップした瞬間の座標
	private Vector2 tapPos      = Vector2.zero;     // タップ中の座標
	private Vector2 prevTapPos  = Vector2.zero;     // 前フレームのタップ座標

	private Vector2 slideLeftUpPerNorm   = Vector2.zero;     // 左上ベクトルの垂線の単位ベクトル(Z軸を90度回転済み)
	private Vector2 slideLeftDownPerNorm = Vector2.zero;     // 左下ベクトルの垂線の単位ベクトル(Z軸を90度回転済み)
	
    private Vec2Int slidingLimitNodeID        = Vec2Int.zero;     // スライド方向の端ノードのID
    private Vec2Int slidingReverseLimitNodeID = Vec2Int.zero;     // スライド方向の逆端ノードのID

	private FieldLevelInfo fieldLevel;

	private float RatioSum = 0.0f;                           //合計割合
    
	private LevelTables _levelTableScript = null;
    public LevelTables levelTableScript
    {
        get
        {
            if (!_levelTableScript)
            {
                GameScene gameScene = transform.root.gameObject.GetComponent<AppliController>().GetCurrentScene().GetComponent<GameScene>();
                _levelTableScript = gameScene.levelTables;
            }
            return _levelTableScript;
        }
    }
    private LevelController _levelControllerScript;
    public LevelController levelControllerScript
    {
        get
        {
            if (!_levelControllerScript)
            {
                GameScene gameScene = transform.root.gameObject.GetComponent<AppliController>().GetCurrentScene().GetComponent<GameScene>();
                _levelControllerScript = gameScene.gameUI.levelCotroller;
            }
            return _levelControllerScript;
        }
    }
    private GameOption _pauseScript;
    public GameOption pauseScript
    {
        get
        {
            if (!_pauseScript)
            {
                GameScene gameScene = transform.root.gameObject.GetComponent<AppliController>().GetCurrentScene().GetComponent<GameScene>();
                _pauseScript = gameScene.gameUI.gamePause;
            }
            return _pauseScript;
        }
    }
    private UnChainController _unChainControllerScript;
    public UnChainController unChainController { get { return _unChainControllerScript; } }
    //ノードの配置割合を記憶しておく

    public int Row {
        get { return this.row; }
    }
    
    public int Col {
        get { return this.col; }
    }

    public _eSlideDir SlideDir {
        get { return slideDir; }
    }

    public Vector2 NodeSize {
        get { return nodeSize; }
    }
    
    private Score _scoreScript = null;          //スコアのスクリプト
    public Score scoreScript { 
        get {
            if(!_scoreScript)
            {
                GameScene gameScene = transform.root.gameObject.GetComponent<AppliController>().GetCurrentScene().GetComponent<GameScene>();
                _scoreScript = gameScene.gameUI.gameInfoCanvas.score;
            }
            return _scoreScript;
        } 
    }
    private LimitTime _timeScript = null;             //制限時間のスクリプト
    public LimitTime timeScript
    {
        get {
            if (!_timeScript)
            {
                GameScene gameScene = transform.root.gameObject.GetComponent<AppliController>().GetCurrentScene().GetComponent<GameScene>();
                _timeScript = gameScene.gameUI.gameInfoCanvas.limitTime;
            }
            return _timeScript;
        }
    }
    private FeverGauge _feverScript = null; 
    public FeverGauge feverScript
    {
        get
        {
            if (!_feverScript)
            {
                GameScene gameScene = transform.root.gameObject.GetComponent<AppliController>().GetCurrentScene().GetComponent<GameScene>();
                _feverScript = gameScene.gameUI.gameInfoCanvas.feverGauge;
            }
            return _feverScript;
        }
    }
    
    public delegate void Replace();             //回転再配置用のデリゲート

    private Material[] nodeMaterials = null;
    public Material GetMaterial(NodeTemplate nodeType){return nodeMaterials[nodeType.ID];}

    private int _currentLevel;
    public int currentLevel
    {
        get { return _currentLevel; }
        set { _currentLevel = value;
        fieldLevel = levelTableScript.GetFieldLevel(_currentLevel);
        RatioSum = fieldLevel.Ratio_Cap + fieldLevel.Ratio_Path2 + fieldLevel.Ratio_Path3 + fieldLevel.Ratio_Path4;
        
        StartCoroutine(ReplaceRotate(ReplaceNodeAll));
        }
    }

	void Awake() {
        gameNodePrefabs  = new GameObject[col][];
        gameNodeScripts  = new Node[col][];
        nodePlacePosList = new Vector3[col][];
        for(int i = 0; i < col; ++i) {
            int adjustRow = AdjustRow(i);
            gameNodePrefabs[i]  = new GameObject[adjustRow];
            gameNodeScripts[i]  = new Node[adjustRow];
            nodePlacePosList[i] = new Vector3[adjustRow];
        }
        nodeMaterials = new Material[NodeTemp.Length];
    }

    // Use this for initialization
    void Start () {
		// ----- プレハブデータを Resources から取得
        gameNodePrefab =  Resources.Load<GameObject>(gameNodePrefabPath);
        frameNodePrefab =  Resources.Load<GameObject>(frameNodePrefabPath);
        floorNodePrefab =  Resources.Load<GameObject>(floorNodePrefabPath);
        treeControllerPrefab = Resources.Load<GameObject>(treeControllerPrefabPath);

        // ノードのテンプレからマテリアルを取得
        NodeTemplate.AllCalc(NodeTemp);
        int n = 0;
        for (int i = 0; i < NodeTemp.Length; ++i)
        {
            if(NodeTemp[i].Ready) {
                nodeMaterials[n] = Resources.Load<Material>(NodeTemp[i].MaterialName);
                NodeTemp[i].ID = n;
                n++;
            } else {
                Debug.LogWarning("Failed Load Material No." + i.ToString());
            }
        }
        

        //unChainCubeList = new ArrayList;

        //levelControllerScript = appController.gameScene.gameUI.levelCotroller;
        //pauseScript = appController.gameScene.gameUI.gamePause;
        //levelTableScript = appController.gameScene.levelTables;


        // ----- ゲームの画面領域を設定(コライダーから取得)
        BoxCollider2D gameAreaInfo = transform.parent.GetComponent<BoxCollider2D>();    // ゲームの画面領域を担うコライダー情報を取得
        gameArea.x      = gameAreaInfo.offset.x;
        gameArea.y      = gameAreaInfo.offset.y;
        gameArea.width  = gameAreaInfo.size.x;
        gameArea.height = gameAreaInfo.size.y;
        
        // ----- ノード準備
        // 描画するノードの大きさを取得
        MeshFilter nodeMeshInfo = gameNodePrefab.GetComponent<MeshFilter>();    // ノードのメッシュ情報を取得
        Vector3 pos = transform.position;
        nodeSize.x = nodeMeshInfo.sharedMesh.bounds.size.x * gameNodePrefab.transform.localScale.x;
        nodeSize.y = nodeMeshInfo.sharedMesh.bounds.size.y * gameNodePrefab.transform.localScale.y;
        nodeSize.x -= widthMargin;
        nodeSize.y -= heightMargin;

        //途切れ表示用のアレを生成
        unChainControllerPrefab = Instantiate(Resources.Load<GameObject>(unChainControllerPath));
        unChainControllerPrefab.transform.SetParent(transform);
        _unChainControllerScript = unChainControllerPrefab.GetComponent<UnChainController>();

        // フレームを生成
        frameController = new GameObject();
        frameController.transform.parent = transform.parent;
        frameController.name = "FrameController";

        Node.SetNodeController(this); //ノードにコントローラーを設定

        fieldLevel = levelTableScript.GetFieldLevel(0);
        RatioSum = fieldLevel.Ratio_Cap + fieldLevel.Ratio_Path2 + fieldLevel.Ratio_Path3 + fieldLevel.Ratio_Path4;  //全体割合を記憶

        

        // ノードを生成
        for(int i = 0; i < col; ++i) {
            GameObject  frameObject     = null;

            // ノードの配置位置を調整(Y座標)
            pos.y = transform.position.y - headerHeight + nodeSize.y * -(col * 0.5f - (i + 0.5f));
            for (int j = 0; j < AdjustRow(i); ++j) {
                // ノードの配置位置を調整(X座標)
                //pos.x = i % 2 == 0 ? transform.position.x + nodeSize.x * -(AdjustRow(i) * 0.5f - j) : transform.position.x + nodeSize.x * -(AdjustRow(i) * 0.5f - (j + 0.5f));
                pos.x = transform.position.x + nodeSize.x * -(AdjustRow(i) * 0.5f - (j + 0.5f));
                pos.z = transform.position.z;

                // 生成
        	    gameNodePrefabs[i][j] = (GameObject)Instantiate(gameNodePrefab, pos, transform.rotation);
                gameNodeScripts[i][j] = gameNodePrefabs[i][j].GetComponent<Node>();
                gameNodePrefabs[i][j].transform.SetParent(transform);
                nodePlacePosList[i][j] = gameNodePrefabs[i][j].transform.position;

                // ノードの位置(リストID)を登録
                gameNodeScripts[i][j].RegistNodeID(j, i);

                //ランダムでノードの種類と回転を設定
                ReplaceNode(gameNodeScripts[i][j]);
                
                // フレーム生成(上端)
                if(i >= col - 1) {
                    Vector3 framePos = pos;
                    framePos.z = transform.position.z + FRAME_POSZ_MARGIN;
                    frameObject = (GameObject)Instantiate(frameNodePrefab, framePos, transform.rotation);
                    frameObject.transform.parent = frameController.transform;
                }
                // フレーム生成(下端)
                if(i <= 0) {
                    Vector3 framePos = pos;
                    framePos.z = transform.position.z + FRAME_POSZ_MARGIN;
                    frameObject = (GameObject)Instantiate(floorNodePrefab, framePos, transform.rotation);
                    frameObject.transform.parent = frameController.transform;

                    // 左端
                    if(j <= 1) {
                        frameObject.GetComponent<MeshRenderer>().material = Resources.Load<Material>(floorLeftNodeMaterialPath);
                    }
                    // 右端
                    if(j >= AdjustRow(i) - 2) {
                        frameObject.GetComponent<MeshRenderer>().material = Resources.Load<Material>(floorRightNodeMaterialPath);
                    }
                }
            }
           
            if(i > 0 && i < col - 1) {
                // フレーム生成(左端)
                pos.x = transform.position.x + nodeSize.x * -(AdjustRow(i) * 0.5f - (0 + 0.5f));
                pos.z = transform.position.z + FRAME_POSZ_MARGIN;
                frameObject = (GameObject)Instantiate(frameNodePrefab, pos, transform.rotation);
                frameObject.transform.parent = frameController.transform;
                // フレーム生成(右端)
                pos.x = transform.position.x + nodeSize.x * -(AdjustRow(i) * 0.5f - (AdjustRow(i) - 1 + 0.5f));
                frameObject = (GameObject)Instantiate(frameNodePrefab, pos, transform.rotation);
                frameObject.transform.parent = frameController.transform;
            }
        }
        //開始演出が終わるまでは操作を受け付けない
        SetActionAll(true);
        
        // スライドベクトルの垂線を算出
        Vector3 leftUp = gameNodePrefabs[0][1].transform.position - gameNodePrefabs[0][0].transform.position;
        Vector3 leftDown = gameNodePrefabs[1][1].transform.position - gameNodePrefabs[0][0].transform.position;
        Matrix4x4 mtx = Matrix4x4.identity;
        mtx.SetTRS(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Euler(0.0f, 0.0f, 90.0f), new Vector3(1.0f, 1.0f, 1.0f));
        leftUp = mtx.MultiplyVector(leftUp).normalized;
        leftDown = mtx.MultiplyVector(leftDown).normalized;
        slideLeftUpPerNorm = new Vector2(leftUp.x, leftUp.y);
        slideLeftDownPerNorm = new Vector2(leftDown.x, leftDown.y);

        // ----- インプット処理
        Observable
            .EveryUpdate()
            .Where(_ => Input.GetMouseButton(0))
            .Subscribe(_ => {
                // タップに成功していなければ未処理
                if(!isTap)
                    return;

                Vector3 worldTapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                
                // スライド処理
                if(isSlide) {
                    prevTapPos = tapPos;
                    tapPos = new Vector2(worldTapPos.x, worldTapPos.y);

                    SlideNodes();
                    StopSlideStartEasing();
                    CheckSlideOutLimitNode();
                    LoopBackNode();

                    return;
                }
                
                // スライド判定
                if(tapNodeID.x > -1) {
                    _eSlideDir dir = CheckSlideDir(startTapPos, new Vector2(worldTapPos.x, worldTapPos.y));
                    Vec2Int nextNodeID = GetDirNode(tapNodeID, dir);
                    if(nextNodeID.x > -1) {
                        if(Vector3.Distance(gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position, worldTapPos) >
                            Vector3.Distance(gameNodePrefabs[nextNodeID.y][nextNodeID.x].transform.position, worldTapPos)) {
                            isSlide = true;
                            isNodeAction = true;
                            StartSlideNodes(nextNodeID, dir);
                        }
                    }
                }
            })
            .AddTo(this.gameObject);
        Observable
            .EveryUpdate()
            .Where(_ => Input.GetMouseButtonDown(0))
            .Subscribe(_ => {
                // スライド中なら未処理
                if(isSlide)
                    return;
                if(pauseScript.pauseState == _ePauseState.PAUSE)
                    return;

                if(levelControllerScript.LevelState != _eLevelState.STAND)
                    return;
                
                // タップ成功
                isTap = true;

                Vector3 worldTapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                startTapPos = new Vector2(worldTapPos.x, worldTapPos.y);
                
                // タップしているノードのIDを検索
                tapNodeID = SearchNearNodeRemoveFrame(worldTapPos);
            })
            .AddTo(this.gameObject);
        Observable
            .EveryUpdate()
            .Where(_ => Input.GetMouseButtonUp(0))
            .Subscribe(_ => {
                // タップに成功していなければ未処理
                if(!isTap)
                    return;
                
                // タップ終了
                isTap = false;

                if(isSlide) {
                    AdjustNodeStop();
                    
                    isSlideEnd = true;
                    tapNodeID = Vec2Int.zero;
                } else {
                    if(tapNodeID.x > -1) {
                        gameNodeScripts[tapNodeID.y][tapNodeID.x].RotationNode();
                        isNodeAction = true;
                    }
                }
            })
            .AddTo(gameObject);

        // ノードのアニメーション終了と同時に接続チェック
        Observable
            .EveryUpdate()
            .Select(_ => !(isNodeAction | isSlide))
            .DistinctUntilChanged()
            .Where(x => x)
            .Subscribe(_ => {
                //RemoveUnChainCube();
                CheckLink();
                unChainController.Remove();
            })
            .AddTo(gameObject);

        // ノードがアクション中かチェック
        Observable
            .EveryUpdate()
            .Where(_ => isNodeAction)
            .Subscribe(_ => {
		        for(int i = 0; i < col; ++i) {
			        foreach (var nodes in gameNodeScripts[i]) {
                        if(nodes.IsAction)
                            return;
	                }
                }
                isNodeAction = false;
            })
            .AddTo(gameObject);
        
        // ノードがスライド終了処理中かチェック
        Observable
            .EveryUpdate()
            .Where(_ => isSlideEnd)
            .Subscribe(_ => {
		        for(int i = 0; i < col; ++i) {
			        foreach (var nodes in gameNodeScripts[i]) {
                        if(nodes.isSlideEnd)
                            return;
                    }
                }
                isSlideEnd = false;
                if(isSlide) {
                    isSlide   = false;
                    slideDir = _eSlideDir.NONE;
                }
            })
            .AddTo(gameObject);
	}
	
	// Update is called once per frame
	void Update () {
        // @デバッグ用
        if(Input.GetKeyDown(KeyCode.A)) { StartCoroutine(ReplaceRotate(ReplaceNodeFever)); }; 
        //for(int i = 0; i < col; ++i) {
        //    for(int j = 0; j < AdjustRow(i); ++j) {
        //        if(gameNodeScripts[i][j].IsOutScreen)
        //            gameNodeScripts[i][j].MeshRenderer.material.color = new Color(0.1f, 0.1f, 1.0f);
        //        else
        //            gameNodeScripts[i][j].MeshRenderer.material.color = new Color(1.0f, 1.0f, 1.0f);
        //    }
        //}
    }

    // ノードのスライド移動処理
    void SlideNodes() {
        // スライド対象となるノードの準備
        Vector2 deltaSlideDist = tapPos - prevTapPos;   // 前回フレームからのスライド量
        float checkDir = 0.0f;                     // スライド方向チェック用

        switch(slideDir) {
            case _eSlideDir.LEFT:
            case _eSlideDir.RIGHT:
                // スライド方向を再計算
                if(tapPos.x - prevTapPos.x < 0.0f) {
                    // スライド方向が前フレームと違ったら更新
                    if(slideDir != _eSlideDir.LEFT) {
                        Vec2Int tmp = slidingLimitNodeID;
                        slidingLimitNodeID = slidingReverseLimitNodeID;
                        slidingReverseLimitNodeID = tmp;

                        slideDir = _eSlideDir.LEFT;
                    }
                } else if(tapPos.x - prevTapPos.x > 0.0f) {
                    if(slideDir != _eSlideDir.RIGHT) {
                        Vec2Int tmp = slidingLimitNodeID;
                        slidingLimitNodeID = slidingReverseLimitNodeID;
                        slidingReverseLimitNodeID = tmp;

                        slideDir = _eSlideDir.RIGHT;
                    }
                }

                // ノードを移動
                AdjustSlideNodePosition();

                break;

            case _eSlideDir.LEFTUP:
            case _eSlideDir.RIGHTDOWN:
                // スライド方向を再計算
                checkDir = AddVectorFunctions.Vec2Cross(deltaSlideDist, slideLeftUpPerNorm);
                if(checkDir < 0.0f) {
                    if(slideDir != _eSlideDir.LEFTUP) {
                        Vec2Int tmp = slidingLimitNodeID;
                        slidingLimitNodeID = slidingReverseLimitNodeID;
                        slidingReverseLimitNodeID = tmp;

                        slideDir = _eSlideDir.LEFTUP;
                    }
                } else if(checkDir > 0.0f) {
                    if(slideDir != _eSlideDir.RIGHTDOWN) {
                        Vec2Int tmp = slidingLimitNodeID;
                        slidingLimitNodeID = slidingReverseLimitNodeID;
                        slidingReverseLimitNodeID = tmp;

                        slideDir = _eSlideDir.RIGHTDOWN;
                    }
                }

                // ノードを移動
                AdjustSlideNodePosition();

                break;

            case _eSlideDir.RIGHTUP:
            case _eSlideDir.LEFTDOWN:
                // スライド方向を再計算
                checkDir = AddVectorFunctions.Vec2Cross(deltaSlideDist, slideLeftDownPerNorm);
                if(checkDir < 0.0f) {
                    if(slideDir != _eSlideDir.LEFTDOWN) {
                        Vec2Int tmp = slidingLimitNodeID;
                        slidingLimitNodeID = slidingReverseLimitNodeID;
                        slidingReverseLimitNodeID = tmp;

                        slideDir = _eSlideDir.LEFTDOWN;
                    }
                } else if(checkDir > 0.0f) {
                    if(slideDir != _eSlideDir.RIGHTUP) {
                        Vec2Int tmp = slidingLimitNodeID;
                        slidingLimitNodeID = slidingReverseLimitNodeID;
                        slidingReverseLimitNodeID = tmp;

                        slideDir = _eSlideDir.RIGHTUP;
                    }
                }

                // ノードを移動
                AdjustSlideNodePosition();

                break;

            default:
                break;
        }
    }

	//移動したいノードを確定
	//ドラッグを算出し移動したい方向列を確定
	//ドラッグされている間、列ごと移動、
		//タップ点からスワイプ地点まで座標の差分を算出し
		//列のすべてのノードをその位置へ移動させる
	//離すと一番近いノード確定位置まで調整

	public void StartSlideNodes(Vec2Int nextNodeID, _eSlideDir newSlideDir) {
		moveNodeDist = new Vector2(gameNodePrefabs[nextNodeID.y][nextNodeID.x].transform.position.x, gameNodePrefabs[nextNodeID.y][nextNodeID.x].transform.position.y)
					 - new Vector2(gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position.x, gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position.y);   // スライド方向ベクトル兼移動量を算出
		moveNodeInitPos = gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position;      // ノードの移動開始位置を保存
		
		// スライド方向を設定
		slideDir = newSlideDir;

		// 絶対値を算出
		moveNodeDistAbs.x = Mathf.Abs(moveNodeDist.x);
		moveNodeDistAbs.y = Mathf.Abs(moveNodeDist.y);
		
        // スライド開始準備
        Vec2Int nextID = SearchLimitNode(tapNodeID, ConvertSlideDirToLinkDir(slideDir));
        _eSlideDir reverseDir = ReverseDirection(slideDir);
        while(nextID.x > -1) {
            gameNodeScripts[nextID.y][nextID.x].StartSlide();
            nextID = GetDirNode(nextID, reverseDir);
	}

        // スライド方向の端のノードIDを算出
        slidingLimitNodeID = SearchLimitNode(tapNodeID, ConvertSlideDirToLinkDir(slideDir));
        slidingReverseLimitNodeID = SearchLimitNode(tapNodeID, ConvertSlideDirToLinkDir(ReverseDirection(slideDir)));

        // タップしているノードの位置と、タップしている位置との距離を算出
		tapPosMoveNodePosDist = moveNodeDist.normalized * Vector2.Dot(moveNodeDist.normalized, startTapPos - moveNodeInitPos);
					}

	// ゲームの画面外にはみ出したノードを逆側に移動する
	void LoopBackNode() {
		if (gameNodeScripts[slidingLimitNodeID.y][slidingLimitNodeID.x].IsOutScreen) {
            gameNodeScripts[slidingLimitNodeID.y][slidingLimitNodeID.x].IsOutScreen = false;

            SortOutNode(slideDir, slidingLimitNodeID);
				
            Vec2Int copyInNodeID  = SearchLimitNodeRemoveFrame(tapNodeID, ConvertSlideDirToLinkDir(SlideDir));
            Vec2Int copyOutNodeID = SearchLimitNodeRemoveFrame(tapNodeID, ConvertSlideDirToLinkDir(ReverseDirection(slideDir)));
            copyOutNodeID = GetDirNode(copyOutNodeID, ReverseDirection(slideDir));
        	gameNodeScripts[copyOutNodeID.y][copyOutNodeID.x].CopyParameter(gameNodeScripts[copyInNodeID.y][copyInNodeID.x]);
					}
					}

	// 移動を終了するノードの位置を調整する
	void AdjustNodeStop() {
		Vec2Int nearNodeID    = SearchNearNode(gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position);
        Vec2Int nextNodeID    = SearchLimitNode(tapNodeID, ConvertSlideDirToLinkDir(slideDir));
        _eSlideDir reverseDir = ReverseDirection(slideDir);
        Vector2 pos           = new Vector2(gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position.x, gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position.y);
        Vector2 standardPos   = new Vector2(nodePlacePosList[nearNodeID.y][nearNodeID.x].x, nodePlacePosList[nearNodeID.y][nearNodeID.x].y);

        // スライド方向を更新
        if(pos.x != standardPos.x || pos.y != standardPos.y) {
            _eSlideDir checkDir = CheckSlideDir(pos, standardPos);
            if(slideDir != checkDir) {
                slideDir = checkDir;

                Vec2Int tmp = slidingLimitNodeID;
                slidingLimitNodeID = slidingReverseLimitNodeID;
                slidingReverseLimitNodeID = tmp;
            }
        }

        // スライド方向のノードに、スライド終了を通知
        while(nextNodeID.x > -1) {
			gameNodeScripts[nextNodeID.y][nextNodeID.x].EndSlide();
            nextNodeID = GetDirNode(nextNodeID, reverseDir);
		}

        // 回り込み処理
        CheckSlideOutLimitNode();
        LoopBackNode();

        // 移動処理
		switch (slideDir) {
			case _eSlideDir.LEFT:
			case _eSlideDir.RIGHT:
                pos = standardPos;

				// タップしているノードを移動
				gameNodeScripts[tapNodeID.y][tapNodeID.x].SlideNode(slideDir, standardPos);

				// タップしているノードより左側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.L);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x - moveNodeDistAbs.x * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.L);
				}
				// タップしているノードより右側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.R);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x + moveNodeDistAbs.x * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.R);
				}

				break;
				
			case _eSlideDir.LEFTUP:
			case _eSlideDir.RIGHTDOWN:
				// タップしているノードを移動
				gameNodeScripts[tapNodeID.y][tapNodeID.x].SlideNode(slideDir, standardPos);
				
				// タップしているノードより左上側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.LU);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x - moveNodeDistAbs.x * i;
					pos.y = standardPos.y + moveNodeDistAbs.y * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.LU);
				}
				// タップしているノードより右下側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.RD);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x + moveNodeDistAbs.x * i;
					pos.y = standardPos.y - moveNodeDistAbs.y * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.RD);
				}
				break;
				
			case _eSlideDir.RIGHTUP:
			case _eSlideDir.LEFTDOWN:
				// タップしているノードを移動
				gameNodeScripts[tapNodeID.y][tapNodeID.x].SlideNode(slideDir, standardPos);

				// タップしているノードより右上側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.RU);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x + moveNodeDistAbs.x * i;
					pos.y = standardPos.y + moveNodeDistAbs.y * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.RU);
				}
				// タップしているノードより左下側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.LD);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x - moveNodeDistAbs.x * i;
					pos.y = standardPos.y - moveNodeDistAbs.y * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.LD);
				}
				break;

			default:
				break;
		}
	}

	// 任意のノード情報をコピーする
	void CopyNodeInfo(int x, int y, GameObject prefab, Node script) {
		gameNodePrefabs[y][x] = prefab;
		gameNodeScripts[y][x] = script;
		gameNodeScripts[y][x].RegistNodeID(x, y);
	}

    // はみ出たノードを逆側に移動し、ノード情報をソートする
    void SortOutNode(_eSlideDir dir, Vec2Int outNodeID) {
		GameObject outNode = gameNodePrefabs[outNodeID.y][outNodeID.x];
		Node outNodeScript = gameNodeScripts[outNodeID.y][outNodeID.x];
        _eSlideDir reverseDir = ReverseDirection(dir);
        Vector3 pos = Vector3.zero;

		// ノード入れ替え処理(スライド方向に置換していく)
		Vec2Int limitNodeID  = outNodeID;
		Vec2Int prevSearchID = outNodeID;
		while(GetDirNode(limitNodeID, reverseDir).x > -1) {
			prevSearchID = limitNodeID;
			limitNodeID = GetDirNode(limitNodeID, reverseDir);
			CopyNodeInfo(prevSearchID.x, prevSearchID.y, gameNodePrefabs[limitNodeID.y][limitNodeID.x], gameNodeScripts[limitNodeID.y][limitNodeID.x]);
		}
		CopyNodeInfo(limitNodeID.x, limitNodeID.y, outNode, outNodeScript);

		// 位置を調整
		prevSearchID = GetDirNode(limitNodeID, dir);
		pos = gameNodePrefabs[prevSearchID.y][prevSearchID.x].transform.position;
        switch(dir) {
            case _eSlideDir.LEFT:
                pos = gameNodePrefabs[tapNodeID.y][AdjustRow(tapNodeID.y) - 2].transform.position;
		pos.x += moveNodeDistAbs.x;
                break;
		
            case _eSlideDir.RIGHT:
                pos = gameNodePrefabs[tapNodeID.y][1].transform.position;
        		pos.x -= moveNodeDistAbs.x;
                break;

            case _eSlideDir.LEFTUP:
		        pos.x += moveNodeDistAbs.x;
		        pos.y -= moveNodeDistAbs.y;
                break;

            case _eSlideDir.LEFTDOWN:
		        pos.x += moveNodeDistAbs.x;
		        pos.y += moveNodeDistAbs.y;
                break;

            case _eSlideDir.RIGHTUP:
		pos.x -= moveNodeDistAbs.x;
		        pos.y -= moveNodeDistAbs.y;
                break;

            case _eSlideDir.RIGHTDOWN:
		        pos.x -= moveNodeDistAbs.x;
		pos.y += moveNodeDistAbs.y;
                break;
        }
		gameNodeScripts[limitNodeID.y][limitNodeID.x].StopTween();
		gameNodePrefabs[limitNodeID.y][limitNodeID.x].transform.position = pos;
		
		// 選択中のノードIDを更新
		tapNodeID = GetDirNode(tapNodeID, dir);
	}

    // タップノードを中心に、スライド方向のノードの位置を設定する
    void AdjustSlideNodePosition() {
		// スライド対象となるノードの準備
		Vector2 pos         = tapPos;      // 移動位置
        Vector2 standardPos = tapPos;
		Vec2Int nextNodeID  = Vec2Int.zero;     // 検索用ノードIDテンポラリ

		switch (slideDir) {
			case _eSlideDir.LEFT:
			case _eSlideDir.RIGHT:
				// タップしているノードを移動
				pos = AdjustNodeLinePosition(slideDir);
				gameNodeScripts[tapNodeID.y][tapNodeID.x].SlideNode(slideDir, pos);

				// タップしているノードより左側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.L);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x - moveNodeDistAbs.x * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.L);
				}
				// タップしているノードより右側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.R);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x + moveNodeDistAbs.x * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.R);
		}

				break;

			case _eSlideDir.LEFTUP:
			case _eSlideDir.RIGHTDOWN:
				// タップしているノードを移動
                standardPos = AdjustNodeLinePosition(slideDir);
				gameNodeScripts[tapNodeID.y][tapNodeID.x].SlideNode(slideDir, standardPos);
		
				// タップしているノードより左上側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.LU);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x - moveNodeDistAbs.x * i;
					pos.y = standardPos.y + moveNodeDistAbs.y * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.LU);
				}
				// タップしているノードより右下側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.RD);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x + moveNodeDistAbs.x * i;
					pos.y = standardPos.y - moveNodeDistAbs.y * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.RD);
	}
				break;

			case _eSlideDir.RIGHTUP:
			case _eSlideDir.LEFTDOWN:
				// タップしているノードを移動
                standardPos = AdjustNodeLinePosition(slideDir);
				gameNodeScripts[tapNodeID.y][tapNodeID.x].SlideNode(slideDir, standardPos);

				// タップしているノードより右上側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.RU);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x + moveNodeDistAbs.x * i;
					pos.y = standardPos.y + moveNodeDistAbs.y * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.RU);
				}
				// タップしているノードより左下側のノードを移動
				nextNodeID = GetDirNode(tapNodeID, _eLinkDir.LD);
				for(int i = 1; nextNodeID.x > -1; ++i) {
					pos.x = standardPos.x - moveNodeDistAbs.x * i;
					pos.y = standardPos.y - moveNodeDistAbs.y * i;
					gameNodeScripts[nextNodeID.y][nextNodeID.x].SlideNode(slideDir, pos);
					nextNodeID = GetDirNode(nextNodeID, _eLinkDir.LD);
		}
				break;
		
			default:
				break;
		}
	}

    // 任意の座標に最も近いノードのIDを、座標を基準に検索する
    Vec2Int SearchNearNode(Vector3 pos) {
        Vec2Int id = new Vec2Int(-1, -1);
        float minDist = 99999.0f;

        // 検索処理
        for(int i = 0; i < col; ++i) {
            for(int j = 0; j < AdjustRow(i); ++j) {
                float dist = Vector3.Distance(pos, nodePlacePosList[i][j]);
                if(dist < minDist) {
                    minDist = dist;
                    id.x = j;
                    id.y = i;
                }
            }
        }

        return id;
    }

    // 任意の座標に最も近いノードのIDを、座標を基準に検索する(フレームノードを除く)
    Vec2Int SearchNearNodeRemoveFrame(Vector3 pos) {
        Vec2Int id = SearchNearNode(pos);

        // フレームなら -1 にする
        if(id.x <= 0 || id.x >= AdjustRow(id.y) || id.y <= 0 || id.y >= col - 1) {
            id.x = -1;
            id.y = -1;
        }

        return id;
    }

    // スライドしている列が画面外にはみ出ているかチェックする(フレームノードより外側にいるかどうか)
    void CheckSlideOutLimitNode() {
        switch(slideDir) {
            case _eSlideDir.LEFT:
                if (gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.x < nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].x) {
                    gameNodeScripts[slidingLimitNodeID.y][slidingLimitNodeID.x].IsOutScreen = true;
			    }
					break;
            case _eSlideDir.RIGHT:
                if (gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.x > nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].x) {
                    gameNodeScripts[slidingLimitNodeID.y][slidingLimitNodeID.x].IsOutScreen = true;
		        }
                break;
            case _eSlideDir.LEFTUP:
                if (gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.x < nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].x &&
                    gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.y > nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].y) {
                    gameNodeScripts[slidingLimitNodeID.y][slidingLimitNodeID.x].IsOutScreen = true;
	            }
                break;
            case _eSlideDir.LEFTDOWN:
                if (gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.x < nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].x &&
                    gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.y < nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].y) {
                    gameNodeScripts[slidingLimitNodeID.y][slidingLimitNodeID.x].IsOutScreen = true;
	            }
                break;
            case _eSlideDir.RIGHTUP:
                if (gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.x > nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].x &&
                    gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.y > nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].y) {
                    gameNodeScripts[slidingLimitNodeID.y][slidingLimitNodeID.x].IsOutScreen = true;
	            }
                break;
            case _eSlideDir.RIGHTDOWN:
                if (gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.x > nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].x &&
                    gameNodePrefabs[slidingLimitNodeID.y][slidingLimitNodeID.x].transform.position.y < nodePlacePosList[slidingLimitNodeID.y][slidingLimitNodeID.x].y) {
                    gameNodeScripts[slidingLimitNodeID.y][slidingLimitNodeID.x].IsOutScreen = true;
			    }
                break;
            default:
                break;
		}
	}

	// 検索したい col に合わせた row を返す
	public int AdjustRow(int col) {
		return col % 2 == 0 ? row + 1 : row;
	}
	
	// リンク方向の端のノードIDを算出する
	Vec2Int SearchLimitNode(Vec2Int id, _eLinkDir dir) {
		Vec2Int limitNodeID = id;
        Vec2Int limitNodeIDTmp = GetDirNode(limitNodeID, dir);
		while(limitNodeIDTmp.x > -1) {
			limitNodeID = limitNodeIDTmp;
            limitNodeIDTmp = GetDirNode(limitNodeID, dir);
		}

		return limitNodeID;
	}
	
	// リンク方向の端のノードIDを算出する
	Vec2Int SearchLimitNode(int x, int y, _eLinkDir dir) {
		return SearchLimitNode(new Vec2Int(x, y), dir);
	}
	
	// リンク方向の端のノードIDを算出する(フレームノードを除く)
	Vec2Int SearchLimitNodeRemoveFrame(Vec2Int id, _eLinkDir dir) {
		Vec2Int limitNodeID = id;
        Vec2Int limitNodeIDTmp = GetDirNodeRemoveFrame(limitNodeID, dir);
		while(limitNodeIDTmp.x > -1) {
			limitNodeID = limitNodeIDTmp;
            limitNodeIDTmp = GetDirNodeRemoveFrame(limitNodeID, dir);
		}

		return limitNodeID;
	}
	
	// リンク方向の端のノードIDを算出する(フレームノードを除く)
	Vec2Int SearchLimitNodeRemoveFrame(int x, int y, _eLinkDir dir) {
		return SearchLimitNodeRemoveFrame(new Vec2Int(x, y), dir);
	}

	// スライド方向を算出
	_eSlideDir CheckSlideDir(Vector2 pos, Vector2 toPos) {
		float angle = Mathf.Atan2(toPos.y - pos.y, toPos.x - pos.x);
		angle *= 180.0f / Mathf.PI;

		// スライド方向を算出
		_eSlideDir dir = _eSlideDir.NONE;
		if(angle < 30.0f && angle >= -30.0f) {          // 右
			dir = _eSlideDir.RIGHT;
		} else if(angle < 90.0f && angle >= 30.0f) {    // 右上
			dir = _eSlideDir.RIGHTUP;
		} else if(angle < 150.0f && angle >= 90.0f) {   // 左上
			dir = _eSlideDir.LEFTUP;
		} else if(angle < -150.0f || angle > 150.0f) {  // 左
			dir = _eSlideDir.LEFT;
		} else if(angle < -90.0f && angle >= -150.0f) { // 左下
			dir = _eSlideDir.LEFTDOWN;
		} else if(angle < -30.0f && angle >= -90.0f) {  // 右下
			dir = _eSlideDir.RIGHTDOWN;
		}

		return dir;
	}

	// ゲーム画面内に収まるよう座標を調整
	Vector2 AdjustGameScreen(Vector2 pos) {
		Vector2 newPos = pos;

		if(newPos.x < gameArea.left)
			newPos.x = gameArea.left;
		if(newPos.x > gameArea.right)
			newPos.x = gameArea.right;
		if(newPos.y > gameArea.top)
			newPos.y = gameArea.top;
		if(newPos.y < gameArea.bottom)
			newPos.y = gameArea.bottom;

		return newPos;
	}

	#region // ノードとノードが繋がっているかを確認する
	// 接続に関するデバックログのON/OFF
	static bool bNodeLinkDebugLog = false;

	// ノードの接続を確認するチェッカー
    public class NodeLinkTaskChecker : System.IDisposable {
		static int IDCnt = 0;           // 管理用IDの発行に使用
		static public List<NodeLinkTaskChecker> Collector = new List<NodeLinkTaskChecker>();    // 動いているチェッカをしまっておくリスト

		public int ID = 0;              // 管理用ID
		public int Branch = 0;          // 枝の数
		public bool NotFin = false;     // 枝の"非"完成フラグ
		public int SumNode = 0;         // 合計ノード数(下の数取得で良いような。)
		public List<Node> NodeList = new List<Node>();    // 枝に含まれるノード。これを永続させて、クリック判定と組めば大幅な負荷軽減できるかも、
		private string Log = "";

		// コンストラクタ
        public NodeLinkTaskChecker() {
			// IDを発行し、コレクタに格納
			ID = ++IDCnt;
			Collector.Add(this);
			Log += "ID : " + ID.ToString() + "\n";
		}

		// Disposeできるように
        public void Dispose() {
            Collector.Remove(this);     // コレクタから削除
		}

		// デバック用ToString
        public override string ToString() {
			string str = "";
            if(bNodeLinkDebugLog) {
				str += Log + "\n--------\n";
			}
			str += 
				"ID : " + ID.ToString() + "  " + NotFin + "\n" + 
				" Branch : " + Branch.ToString() + "\n" + 
				"SumNode : " + SumNode.ToString() + "\n";
            foreach(var it in NodeList) {
				str += it.ToString() + "\n";
			}
			return str;
		}

		// デバック用ログに書き出す
        static public NodeLinkTaskChecker operator +(NodeLinkTaskChecker Ins, string str) {
			Ins.Log += str + "\n";
			return Ins;
		}
			}


    // 接続をチェックする関数
    public void CheckLink(bool NoCheckLeftCallback = false) {
        if(Debug.isDebugBuild && bNodeLinkDebugLog)
            Debug.Log("CheckLink");

        // ノードチェッカが帰ってきてないかチェック。これは結構クリティカルなんでログOFFでも出る仕様。
        if(NodeLinkTaskChecker.Collector.Count != 0 && Debug.isDebugBuild && !NoCheckLeftCallback) {
            string str = "Left Callback :" + NodeLinkTaskChecker.Collector.Count.ToString() + "\n";
            foreach(var it in NodeLinkTaskChecker.Collector) {
                str += it.ToString();
            }
            Debug.LogWarning(str);
        }

        ResetCheckedFragAll();          // 接続フラグを一度クリア

        // 根っこ分繰り返し
        for(int i = 1; i < row-1; i++) {
            // チェッカを初期化
            var Checker = new NodeLinkTaskChecker();

            // 根っこを叩いて処理スタート
            Observable
                .Return(i)
                .Subscribe(x => {
                    Checker += "firstNodeAct_Subscribe [" + Checker.ID + "]";
                    Checker.Branch++;                                               // 最初に枝カウンタを1にしておく(規定値が0なので+でいいはず)
                    gameNodeScripts[1][x].NodeCheckAction(Checker, _eLinkDir.NONE);     // 下から順にチェックスタート。来た方向はNONEにしておいて根っこを識別。
                }).AddTo(this);

            // キャッチャを起動
            Observable
                .NextFrame()
                .Repeat()
                .First(_ => Checker.Branch == 0)    // 処理中の枝が0なら終了
                .Subscribe(_ => {
                    if(Debug.isDebugBuild && bNodeLinkDebugLog)
                        Debug.Log("CheckedCallback_Subscribe [" + Checker.ID + "]" + Checker.SumNode.ToString() + "/" + (Checker.NotFin ? "" : "Fin") + "\n" + Checker.ToString());

                    // ノード数3以上、非完成フラグが立ってないなら
                    if(Checker.SumNode >= 1 && Checker.NotFin == false) {
                        // その枝のノードに完成フラグを立てる
                        //foreach(Node Nodes in Checker.NodeList) {
                        //    Nodes.CompleteFlag = true;
                        //};
                        ReplaceNodeTree(Checker.NodeList);   // 消去処理
                        //CheckLink(false);
                        if(Debug.isDebugBuild && bNodeLinkDebugLog)
                            print("枝が完成しました！");
                    }
                    Checker.Dispose();      // チェッカは役目を終えたので消す
                }).AddTo(this);
        }
    }

    //閲覧済みフラグを戻す処理
    public void ResetCheckedFragAll() {
        for(int i = 0; i < col; ++i) {
            foreach(var nodes in gameNodeScripts[i]) {
                nodes.ChangeEmissionColor(0);  //繋がりがない枝は色をここでもどす
                nodes.CheckFlag = false;

            }
        }
    }
	#endregion

	//位置と方向から、指定ノードに隣り合うノードのrowとcolを返す
	//なければ、(-1,-1)を返す
	public Vec2Int GetDirNode(int x, int y, _eLinkDir toDir)
	{
		//走査方向のノードのcolとrow

		Vec2Int nextNodeID;

		nextNodeID.x = x;
		nextNodeID.y = y;

		bool Odd = ((y % 2) == 0) ? true : false;

		//次のノード番号の計算
		switch(toDir)
		{
			case _eLinkDir.RU:
				if (!Odd)
					nextNodeID.x++;
				nextNodeID.y++;
				break;
			case _eLinkDir.R:
				nextNodeID.x++;
				break;
			case _eLinkDir.RD:
				if (!Odd)
					nextNodeID.x++;
				nextNodeID.y--;
				break;
			case _eLinkDir.LD:
				if(Odd)
					nextNodeID.x--;
				nextNodeID.y--;
				break;
			case _eLinkDir.L:
				nextNodeID.x--;
				break;
			case _eLinkDir.LU:
				if (Odd)
					nextNodeID.x--;
				nextNodeID.y++;

				break;
		}

		if (nextNodeID.x < 0 || nextNodeID.x > AdjustRow(nextNodeID.y) - 1 ||nextNodeID.y < 0 || nextNodeID.y > col - 1)
		{
			nextNodeID.x = -1;
			nextNodeID.y = -1;
		}

		return nextNodeID;
	}

	//位置と方向から、指定ノードに隣り合うノードのrowとcolを返す
	//なければ、(-1,-1)を返す
	public Vec2Int GetDirNode(Vec2Int nodeID, _eLinkDir toDir)
	{
		return GetDirNode(nodeID.x, nodeID.y, toDir);
	}

	//位置と方向から、指定ノードに隣り合うノードのrowとcolを返す
	//なければ、(-1,-1)を返す
	public Vec2Int GetDirNode(int x, int y, _eSlideDir toSlideDir)
	{
        _eLinkDir toDir = ConvertSlideDirToLinkDir(toSlideDir);

		return GetDirNode(x, y, toDir);
	}

	//位置と方向から、指定ノードに隣り合うノードのrowとcolを返す
	//なければ、(-1,-1)を返す
	public Vec2Int GetDirNode(Vec2Int nodeID, _eSlideDir toSlideDir)
	{
		return GetDirNode(nodeID.x, nodeID.y, toSlideDir);
	}
	

	//位置と方向から、指定ノードに隣り合うノードのrowとcolを返す
	//なければ、(-1,-1)を返す
    // フレームノードを除く
	public Vec2Int GetDirNodeRemoveFrame(int x, int y, _eLinkDir toDir)
	{
        Vec2Int id = GetDirNode(x, y, toDir);

		if (id.x <= 0 || id.x >= AdjustRow(id.y) - 1 ||id.y <= 0 || id.y >= col - 1) {
			id.x = -1;
			id.y = -1;
		}

		return id;
	}

	//位置と方向から、指定ノードに隣り合うノードのrowとcolを返す
	//なければ、(-1,-1)を返す
    // フレームノードを除く
	public Vec2Int GetDirNodeRemoveFrame(Vec2Int nodeID, _eLinkDir toDir)
	{
		return GetDirNodeRemoveFrame(nodeID.x, nodeID.y, toDir);
	}

	//位置と方向から、指定ノードに隣り合うノードのrowとcolを返す
	//なければ、(-1,-1)を返す
    // フレームノードを除く
	public Vec2Int GetDirNodeRemoveFrame(int x, int y, _eSlideDir toSlideDir)
	{
        _eLinkDir toDir = ConvertSlideDirToLinkDir(toSlideDir);

        return GetDirNodeRemoveFrame(x, y, toDir);
	}

	//位置と方向から、指定ノードに隣り合うノードのrowとcolを返す
	//なければ、(-1,-1)を返す
    // フレームノードを除く
	public Vec2Int GetDirNodeRemoveFrame(Vec2Int nodeID, _eSlideDir toSlideDir)
	{
		return GetDirNodeRemoveFrame(nodeID.x, nodeID.y, toSlideDir);
	}
	
	//指定した位置のノードのスクリプトをもらう
	public Node GetNodeScript(Vec2Int nodeID)
	{
		return gameNodeScripts[nodeID.y][nodeID.x];
	}

	//ノードの配置 割合は指定できるが完全ランダムなので再考の余地あり
	void ReplaceNode(Node node)
	{
		//node.CompleteFlag = false;
		node.CheckFlag = false;
		node.ChainFlag = false;

		float rand;
		rand = Random.Range(0.0f, RatioSum);

        int branchNum =
            (rand <= fieldLevel.Ratio_Cap) ? 1 :
            (rand <= fieldLevel.Ratio_Cap + fieldLevel.Ratio_Path2) ? 2 :
            (rand <= fieldLevel.Ratio_Cap + fieldLevel.Ratio_Path2 + fieldLevel.Ratio_Path3) ? 3 :
            (rand <= fieldLevel.Ratio_Cap + fieldLevel.Ratio_Path2 + fieldLevel.Ratio_Path3 + fieldLevel.Ratio_Path4) ? 4 :
            1;

        node.SetNodeType(NodeTemplate.GetTempFromBranchRandom(NodeTemp, branchNum));
        node.MeshRenderer.material.color = levelTableScript.GetFieldLevel(_currentLevel).NodeColor;
	}

    private List<List<Node>> FinishNodeList = new List<List<Node>>();

    //完成した枝に使用しているノードを再配置する
    void ReplaceNodeTree(List<Node> List) {
        if(List.Count > 2) {
            FinishNodeList.Add(List);
        }

        NodeCountInfo nodeCount = new NodeCountInfo();

        //完成時演出のためにマテリアルをコピーして
        List<GameObject> treeNodes = new List<GameObject>();
        foreach(Node obj in List) {
            treeNodes.Add(obj.gameObject);
        }
        GameObject newTree = (GameObject)Instantiate(treeControllerPrefab, transform.position, transform.rotation);
        newTree.GetComponent<treeController>().SetTree(treeNodes);
        
        //ノードを再配置
        foreach(Node obj in List) {
            switch(obj.GetLinkNum()) {
                case 1:
                    nodeCount.cap++;
                    break;
                case 2:
                    nodeCount.path2++;
                    break;
                case 3:
                    nodeCount.path3++;
                    break;
                case 4:
                    nodeCount.path4++;
                    break;
            }
            ReplaceNode(obj);
        }

        nodeCount.nodes = List.Count;
        scoreScript.PlusScore(nodeCount);
        timeScript.PlusTime(nodeCount);
        feverScript.Gain(nodeCount);
    }

    public void ReplaceNodeAll() {
        foreach(var xList in gameNodeScripts) {
            foreach(var it in xList) {
                ReplaceNode(it);
			}
		}
	}


    public void ReplaceNodeFever() {
        foreach(var xList in gameNodeScripts) {
            foreach(var it in xList) {
                it.SetNodeType(NodeTemplate.GetTempFromBranchRandom(NodeTemp, 1), 0);
            }
        }

        #region // 自己生成版
        /*
        string str = "ReplaceNodeFeverLog\n";
        int targetNum = RandomEx.RangeforInt(3, 9);
        str += "Target : " + targetNum;

//        int Counter = 0;
        List<Node> lstTree = new List<Node>();
        int x = RandomEx.RangeforInt(2, 5);
        int y = 1;
        _eLinkDir Dir = _eLinkDir.LD;
        int RotationNum;
        while(lstTree.Count < targetNum) {
            str += "\nCheck" + gameNodeScripts[y][x].ToString() + "\n";

            gameNodeScripts[y][x].SetNodeType(NodeTemplate.GetTempFromBranchRandom(NodeTemp, 2), 0);
            str += "SetNode : " + gameNodeScripts[y][x].bitLink.ToStringEx() + " / " + gameNodeScripts[y][x].Temp.ToString() + "\n";

            int CheckLinkNum = 0;
            bool Decide = false;
            ///<summary>
            ///     不正な回転 or 回転できなかった処理が存在  
            /// </summary>
            RotationNum = 0;
            while(!Decide && gameNodeScripts[y][x].bitLink.Count > CheckLinkNum) {
                while(!gameNodeScripts[y][x].bitLink[(int)Dir]) {
                    gameNodeScripts[y][x].RotationNode(true);
                    RotationNum++;
                }
                str += "Rot : " + RotationNum + " / 　" + gameNodeScripts[y][x].bitLink.ToStringEx() + "\n";

                Vec2Int nextNode = new Vec2Int(-1, -1);
                _eLinkDir NextDir = Dir;
                for(int n = 0; n < 6; n++) {
                    if(n == (int)Dir) { continue; }
                    if(gameNodeScripts[y][x].bitLink[n]) {
                        nextNode = GetDirNode(x, y, (_eLinkDir)n);
                        if(nextNode.x == -1 || gameNodeScripts[nextNode.y][nextNode.x].IsOutPuzzle) {
                            nextNode.x = x;
                            nextNode.y = y;
                            CheckLinkNum++;
                            if(n == 5) {
                                str += "Failed";
                                break;
                            }
                        }
                        str += "Find" + ((_eLinkDir)n).ToString();
                        Decide = true;
                        NextDir = ReverseDirection((_eLinkDir)n);
                    }
                }
                gameNodeScripts[y][x].SetNodeType(gameNodeScripts[y][x].Temp, RotationNum);
                x = nextNode.x;
                y = nextNode.y;
                Dir = NextDir;
                lstTree.Add(gameNodeScripts[y][x]);
            }
        }
        // 枝に蓋をする
        gameNodeScripts[y][x].SetNodeType(NodeTemplate.GetTempFromBranchRandom(NodeTemp, 1), 0);
        RotationNum = 0;
        while(!gameNodeScripts[y][x].bitLink[(int)Dir]) {
            gameNodeScripts[y][x].RotationNode(true);
            RotationNum++;
        }
        gameNodeScripts[y][x].RotationNode(true, true);
        gameNodeScripts[y][x].SetNodeType(gameNodeScripts[y][x].Temp, RotationNum == 0 ? 5 : RotationNum - 1);
        lstTree.Add(gameNodeScripts[y][x]);
        Debug.Log(str);
        */
        #endregion

        var UseTree = FinishNodeList[RandomEx.RangeforInt(0, FinishNodeList.Count)];
        foreach(var it in UseTree) {
            gameNodeScripts[it.NodeID.y][it.NodeID.x].SetNodeType(it.Temp, it.RotCounter);
            
        };
        int mut = RandomEx.RangeforInt(0, UseTree.Count);
        gameNodeScripts[UseTree[mut].NodeID.y][UseTree[mut].NodeID.x].RotationNode(true, true);
        if(FinishNodeList.Count > 15) {
            FinishNodeList.RemoveRange(0, 10);
        }

        CheckLink();
        unChainController.Remove();
    }


    //ノードにテーブルもたせたくなかったので
    public Color GetNodeColor(int colorNum)
	{
		return levelTableScript.GetNodeColor(colorNum);
	}

	//ノード全変更時の演出
    public void RotateAllNode(float movedAngle, Ease easeType) {
        foreach(var xList in gameNodeScripts) {
            foreach(var it in xList) {
                Vector3 angle = it.transform.localEulerAngles;
				angle.y += 90.0f;
                it.transform.DORotate(angle, (repRotateTime / 2.0f))
					.OnComplete(() => {
						//終了時の角度がずれたので無理やり補正するように
                        Vector3 movedVec = it.transform.localEulerAngles;
						movedVec.x = 0.0f;
						movedVec.y = movedAngle;
                        it.transform.rotation = Quaternion.identity;
                        it.transform.Rotate(movedVec);
					})
					.SetEase(easeType);
			}
		}
	}

    public void SetActionAll(bool action) {
        /*foreach(var xList in gameNodeScripts) {
            foreach(var it in xList) {
                it.IsAction = action;
			}
		}*/
        
        isSlide = action;
	}

    //全ノードがくるっと回転して状態遷移するやつ 再配置関数を引数に
    public IEnumerator ReplaceRotate(Replace repMethod) {
        //全ノードを90°回転tween
        SetActionAll(true);

        RotateAllNode(90.0f, Ease.InSine);

        yield return new WaitForSeconds(repRotateTime / 2.0f);
        //置き換え処理
        repMethod();
        //全ノードを-180°回転
        foreach(var xList in gameNodeScripts) {
            foreach(var it in xList) {
                Vector3 angle = it.transform.localEulerAngles;
                angle.y -= 180.0f;
                it.transform.rotation = Quaternion.identity;
                it.transform.Rotate(angle);
                it.MeshRenderer.material.color = levelTableScript.GetFieldLevel(_currentLevel).NodeColor;
            }
        }

        //全ノードを90°回転
        RotateAllNode(0.0f, Ease.OutSine);
        yield return new WaitForSeconds(repRotateTime / 2.0f);
        SetActionAll(false);
        CheckLink();
    }

	//操作終了時の処理をここで
	public void TouchEnd()
	{
		//状況に応じて別の処理をする
		switch(feverScript.feverState)
		{
			case _eFeverState.NORMAL:
				break;

			case _eFeverState.FEVER:
				break;
		}
	}
    
    // _eSlideDir を _eLinkDir に変換する
    _eLinkDir ConvertSlideDirToLinkDir(_eSlideDir dir) {
        _eLinkDir convert = _eLinkDir.NONE;

        switch (dir) {
            case _eSlideDir.LEFT:
                convert = _eLinkDir.L;
                break;
            case _eSlideDir.RIGHT:
                convert = _eLinkDir.R;
                break;
            case _eSlideDir.LEFTUP:
                convert = _eLinkDir.LU;
                break;
            case _eSlideDir.LEFTDOWN:
                convert = _eLinkDir.LD;
                break;
            case _eSlideDir.RIGHTUP:
                convert = _eLinkDir.RU;
                break;
            case _eSlideDir.RIGHTDOWN:
                convert = _eLinkDir.RD;
                break;
        }

        return convert;
    }

    // _eLinkDir を _eSlideDir に変換する
    _eSlideDir ConvertLinkDirToSlideDir(_eLinkDir dir) {
        _eSlideDir convert = _eSlideDir.NONE;

        switch (dir) {
            case _eLinkDir.L:
                convert = _eSlideDir.LEFT;
                break;
            case _eLinkDir.R:
                convert = _eSlideDir.RIGHT;
                break;
            case _eLinkDir.LU:
                convert = _eSlideDir.LEFTUP;
                break;
            case _eLinkDir.LD:
                convert = _eSlideDir.LEFTDOWN;
                break;
            case _eLinkDir.RU:
                convert = _eSlideDir.RIGHTUP;
                break;
            case _eLinkDir.RD:
                convert = _eSlideDir.RIGHTDOWN;
                break;
        }

        return convert;
    }

    // スライド方向の逆方向を求める
    _eSlideDir ReverseDirection(_eSlideDir dir) {
        _eSlideDir reverse = _eSlideDir.NONE;
        
        switch (dir) {
            case _eSlideDir.LEFT:
                reverse = _eSlideDir.RIGHT;
                break;
            case _eSlideDir.RIGHT:
                reverse = _eSlideDir.LEFT;
                break;
            case _eSlideDir.LEFTUP:
                reverse = _eSlideDir.RIGHTDOWN;
                break;
            case _eSlideDir.LEFTDOWN:
                reverse = _eSlideDir.RIGHTUP;
                break;
            case _eSlideDir.RIGHTUP:
                reverse = _eSlideDir.LEFTDOWN;
                break;
            case _eSlideDir.RIGHTDOWN:
                reverse = _eSlideDir.LEFTUP;
                break;
        }

        return reverse;
    }

    // リンク方向の逆方向を求める
    _eLinkDir ReverseDirection(_eLinkDir dir) {
        _eLinkDir reverse = _eLinkDir.NONE;
        
        switch (dir) {
            case _eLinkDir.L:
                reverse = _eLinkDir.R;
                break;
            case _eLinkDir.R:
                reverse = _eLinkDir.L;
                break;
            case _eLinkDir.LU:
                reverse = _eLinkDir.RD;
                break;
            case _eLinkDir.LD:
                reverse = _eLinkDir.RU;
                break;
            case _eLinkDir.RU:
                reverse = _eLinkDir.LD;
                break;
            case _eLinkDir.RD:
                reverse = _eLinkDir.LU;
                break;
        }

        return reverse;
    }

    // タップ位置から、タップしているノードの、移動ライン上の座標を算出する
    Vector2 AdjustNodeLinePosition(_eSlideDir dir) {
        Vector2 adjustPos = tapPos;
		Vector2 slideDist = tapPos - startTapPos;     // スライド量
		Vector2 moveDist  = moveNodeDist.normalized * Vector2.Dot(moveNodeDist.normalized, slideDist);      // 斜め移動量

        switch (slideDir) {
			case _eSlideDir.LEFT:
			case _eSlideDir.RIGHT:
				// タップしているノードの位置を調整
				adjustPos = AdjustGameScreen(tapPos);
				adjustPos.y = gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position.y;
				break;

			case _eSlideDir.LEFTUP:
			case _eSlideDir.RIGHTDOWN:
				// タップしているノードの位置を調整
				Vec2Int lu = SearchLimitNode(tapNodeID, _eLinkDir.LU);
				Vec2Int rd = SearchLimitNode(tapNodeID, _eLinkDir.RD);
				adjustPos = moveNodeInitPos + moveDist + tapPosMoveNodePosDist;
				if (adjustPos.x < nodePlacePosList[lu.y][lu.x].x)
					adjustPos.x = nodePlacePosList[lu.y][lu.x].x;
				if (adjustPos.x > nodePlacePosList[rd.y][rd.x].x)
					adjustPos.x = nodePlacePosList[rd.y][rd.x].x;
				if (adjustPos.y > nodePlacePosList[lu.y][lu.x].y)
					adjustPos.y = nodePlacePosList[lu.y][lu.x].y;
				if (adjustPos.y < nodePlacePosList[rd.y][rd.x].y)
					adjustPos.y = nodePlacePosList[rd.y][rd.x].y;
				break;
				
			case _eSlideDir.RIGHTUP:
			case _eSlideDir.LEFTDOWN:
				// タップしているノードの位置を調整
				Vec2Int ru = SearchLimitNode(tapNodeID, _eLinkDir.RU);
				Vec2Int ld = SearchLimitNode(tapNodeID, _eLinkDir.LD);
				adjustPos = moveNodeInitPos + moveDist + tapPosMoveNodePosDist;
				if (adjustPos.x > nodePlacePosList[ru.y][ru.x].x)
					adjustPos.x = nodePlacePosList[ru.y][ru.x].x;
				if (adjustPos.x < nodePlacePosList[ld.y][ld.x].x)
					adjustPos.x = nodePlacePosList[ld.y][ld.x].x;
				if (adjustPos.y > nodePlacePosList[ru.y][ru.x].y)
					adjustPos.y = nodePlacePosList[ru.y][ru.x].y;
				if (adjustPos.y < nodePlacePosList[ld.y][ld.x].y)
					adjustPos.y = nodePlacePosList[ld.y][ld.x].y;
				break;

			default:
				break;
		}

        return adjustPos;
	}

    // スライド開始時の easing をストップする
    void StopSlideStartEasing() {
        if (gameNodeScripts[tapNodeID.y][tapNodeID.x].isSlideStart) {
            Vector2 nodePos2D = new Vector2(gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position.x, gameNodePrefabs[tapNodeID.y][tapNodeID.x].transform.position.y);

            // タップ位置とノードとの距離が閾値を下回っていたらストップする
            if(Vector2.Distance(nodePos2D, AdjustNodeLinePosition(slideDir)) < NODE_EASE_STOP_THRESHOLD) {
                Vec2Int nextID = SearchLimitNode(tapNodeID, ConvertSlideDirToLinkDir(slideDir));
                _eSlideDir reverseDir = ReverseDirection(slideDir);

                while(nextID.x > -1) {
                    gameNodeScripts[nextID.y][nextID.x].isSlideStart = false;
                    nextID = GetDirNode(nextID, reverseDir);
                }
            }
        }
    }
}