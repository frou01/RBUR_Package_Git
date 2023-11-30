# RigidBody Udon Railway
## 概要
RigidBodyUdonRailwayはUdonとRigidBodyを用いてVRCワールドに操作可能な鉄道を敷くことを目的に開発さた、エディタスクリプト、U#スクリプト及びPrefabが含まれています。

## 目次
- [レール関係](#レール関係)
    - [Editor](#--editor)
    - [Runtime(VRC)](#--Runtime(VRC))
- [車両関係](#車両関係)
    - [Editor](#Editor-1)
    - [Runtime(VRC)](#Runtime(VRC)-1)


## コンポーネント及びプレハブ
## レール関係

- レールモデルを敷くために用意された編集用スクリプトと、VRC内で車両にレールデータを受け渡す物

### - Editor
- railModelTiler
    
    モデル敷設スクリプト。
    <details>
    <summary>設定値及び機能</summary>

    |設定値|概要|
    |---:|:---|
    cinemachinePath|敷設先Path。これに沿ってモデル変形し、子オブジェクトを配置します
    meshrendererObjectPrefab|敷設する元メッシュ。<br>Rootオブジェクトのメッシュが変形されます<br>FBXモデルを用いる場合、Read/Writeにチェックを入れること。<br>なお、敷設時、Rootオブジェクトの回転は無視されます。
    modelLength|meshrendererObjectPrefabで設定したメッシュの長さ([m])。
    TilingStart|敷設開始点
    TilingEnd|敷設終点
    isZinverted|MeshRendererObjectが-Zに伸びているか。<br>-Z方向に伸びている場合にチェックを入れないと始端終端がおかしくなります。
    ignoreRoll|ロール無視。<br>CinemachinePathが捻られている時、それを無視してワールドXZ平面に垂直を保って敷設します。
    ignorePitch|ピッチ無視。<br>CinemachinePathが捻られている時、それを無視してワールドXZ平面に並行を保って敷設します。<br>位置変位は保たれるため、平行四辺形変形された形になります。
    disbaleInstancedThreshold|インスタンス解除頂点変位閾値([m])。<br>この設定値を越えて頂点が移動された場合、インスタンス解除され頂点をPathに沿って変形したモデルが設定されます。<br>※インスタンス：元モデルメッシュを参照複製して用いる。生成メッシュが減り容量が軽くなる、GPUInstancingが効く等の恩恵あり）
    saveFolder|セーブ先フォルダ。/で階層を表す
    veticesTransformSteps|1ループで扱う頂点量<br>エディタ拘束を避けるため、多ループに跨がらせています。上げることで高速化が見込めますが、敷設中頻繁にフリーズするようになります。
    root|Cinemachine一括オフセット機能で用いる親オブジェクト
    offset|レールモデル位置オフセット<br>Cinemachine一括オフセット量

    |機能|概要|
    |---:|:---|
    TilingRails|TilingStart->TilingEndの区間でレール生成を行います
    TilingRailAll|cinemachinePathの始点から終点までの区間でレール生成を行います
    Cancel|生成を強制停止します<br>GameObject削除は行われず、生成途中の物が残りますが、Meshはセーブされていないためこの状態で保存しても消えてしまいます。
    SelectFolder|フォルダ選択画面を開きます。<br>新規フォルダ生成をこの先の画面で行うとUnityがフォルダを認識できず無限ループしてしまう不具合が報告されています。
    Offset|Cinemachine一括オフセットを行います。
    </details>
- railModelLocator
    
    モデル配置スクリプト
    <details>
    <summary>設定値及び機能</summary>

    |設定値|概要|
    |---:|:---|
    cinemachinePath|敷設先Path。これに沿ってオブジェクトを配置します
    objectPrefab|敷設するオブジェクト。プレハブ化必須。
    modelLength|モデル配置間隔
    TilingStart|配置始点
    TilingEnd|配置終点
    isZinverted|（このスクリプトではあまり意味の無いパラメーターです）
    ignoreRoll|ロール無視。<br>CinemachinePathが捻られている時、それを無視してワールドXZ平面に垂直を保って設置します。
    ignorePitch|ピッチ無視。<br>CinemachinePathが捻られている時、それを無視してワールドXZ平面に並行を保って設置します。


    |機能|概要|
    |---:|:---|
    TilingRails|TilingStart->TilingEndの区間で配置を開始します
    Cancel|配置を強制停止します。残ったオブジェクトはそのまま使用可能です。
    </details>
- railModelTiler_Batcher
    
    railModelTiler一括実行スクリプト。 指定されたpath全体にレールを生成します。
    <details>
    <summary>設定値及び機能</summary>

    |設定値|概要|
    |---:|:---|
    railModelTiler|バッチ処理を担当するrailModelTiler
    batchingRoot|バッチ処理されるpathたちの親オブジェクト<br>設定されていると子にあるpath全てに対し生成を行います。
    batchingObject|バッチ対象のpath。batchingRootが設定されていると無視されます。

    |機能|概要|
    |---:|:---|
    Batching|バッチ処理を開始します。
    Cancel|バッチ処理をキャンセルします。その時点で動いている敷設処理はそのまま動き続けます。
    </details>
### - Runtime(VRC)
- Rail_Script
    
    レールスクリプト。始点と終点双方のレール参照を保持し、車両に次のレールを伝えます。
    
    <details>
    <summary>設定値/仕様</summary>
    
    |設定値|概要|
    |---:|:---|
    cinemachinePath|レールとするcinemachinePath。Smooth/通常両方使えます。
    moveableRail|移動するレールか。<br>チェックされていると載っている列車は常に情報を更新します。
    next|path終点側接続レール
    prev|path始点側接続レール

    Gizmoについて：
    
    前後レールの設定がある場合、Gizmoは \\/ (レール部分) \\/ のような形になります。

    前後レールの設定が無い場合、Gizmoは◯--◯ (レール部分) ◯\\のような形になります。

    設定したレール間が離れていると、巨大な球が描画されます。
    </details>
- RailsManager
    
    レール管理スクリプト。
    
    ビルド時自動設定が行われるため、設置のみ必要です。シーンルート直下に配置してください。
- RailroadCrossing
    
    踏切スクリプト
    
    JointSoundPlayerを検出し、範囲内に列車が進入/退出するとAnimatorのパラメーターを書き換えます。
    
    <details>
    <summary>設定値/仕様</summary>
    
    |設定値|概要|
    |---:|:---|
    animators|設定先Animator。複数指定可能です。
    paramName|書き換えるパラメーター(bool,in=true,out=false)
    </details>
- RailroadCrossing
    
    踏切スクリプト
    
    JointSoundPlayerを検出し、範囲内に列車が進入/退出するとAnimatorのパラメーターを書き換えます。
    
    <details>
    <summary>設定値/仕様</summary>
    
    |設定値|概要|
    |---:|:---|
    animators|設定先Animator。複数指定可能です。
    paramName|書き換えるパラメーター(bool,in=true,out=false)
    </details>
- PointLever_Setter
    
    Animator連携式ポイントスクリプト
    
    Animatorからのイベント発火で駆動し、レールの参照を書き換えて分岐切り替えを行います
    
    <details>
    <summary>設定値/仕様</summary>
    
    |設定値|概要|
    |---:|:---|
    from1|分岐元レール<br>changeTypeで指定した側のレールが切り替えになります
    from2|分岐元レール(サブ)
    changeType1 | bool , next=true , prev=false
    changeType2 | bool , next=true , prev=false
    to1|切り替え先レール(state=false)
    to2|切り替え先レール(state=true)
    state|分岐状態保存変数。この値は同期されます
    </details>
- SoundDetector
    
    ジョイント音再生コライダースクリプト(Tag代わり)
    
    これ自体は無機能で、JointSoundPlayerと組み合わせることで機能します。

- TurnTable_Controller
    
    Animator連携式転車台スクリプト
    
    Animatorの"mortorTorque"[float]パラメーターを参照し、mortorTorqueの速度と正負でレール(正確にはGameObject)を旋回、前後レールの参照を自動設定します。

    同じオブジェクトにVRCStationをアタッチする必要があります。

    Interactで動作開始します。
    
    <details>
    <summary>設定値/仕様</summary>
    
    |設定値|概要|
    |---:|:---|
    targetTable|旋回するオブジェクト
    mine|旋回するレールのスクリプト
    targets|転車台周囲にあるレール<br>mineにむけ参照を持っている想定で組まれています。
    animator|"mortorTorque"[float]パラメーターの参照先になるAnimator
    syncedTableRotation|初期回転位置からの差分。同期に使用
    Active|動作状態。同期に使用
    </details>

## 車両関係
- 走行・連結用スクリプトと、独立した音源他アクセサリスクリプト
### - Editor
### - Runtime(VRC)
- Train
    
    列車基本スクリプト
    <details>
    <summary>設定値及び仕様</summary>

    |設定値|概要|
    |---:|:---|
    TrainID|車両識別番号（ビルド時自動設定）
    TrainManager|車両管理スクリプト（ビルド時自動設定）
    RailsManager|レール管理スクリプト（ビルド時自動設定）
    Started|初期化フラグ(False必須)
    HandBrakeState|手ブレーキ状態。trueになるとHandBrakeForce[N]のブレーキ力が掛かります。
    HandBrakeForce|手ブレーキ力[N]
    BrakeMultiplier|ブレーキ力係数 max=BrakeMultiplier[N]
    BrakeFactor|実効ブレーキ力。実行時debug用
    brakeUpdateBypass|ブレーキ更新バイパス設定。trueの場合他車からのブレーキ圧の影響を受けなくなります（別スクリプトでブレーキ圧制御を行う場合に用います）
    friction|摩擦力。動静通して掛かる。[N]
    static_friction|静止時摩擦力。[N]
    brakePressure|ブレーキ力指示伝達Transform。このオブジェクトのLocal座標を用いて他車や操作スクリプトからの影響を受けています。
    CenterOfMass|重心位置設定
    BrakeOpenF|+Z側のブレーキ開放状態。連結時ブレーキ圧の伝達を受けるかどうか
    BrakeOpenB|-Z側のブレーキ解放状態。連結時ブレーキ圧の伝達を受けるかどうか
    CouplerF|+Z側連結器。オーナー同期に用います。
    CouplerB|-Z側連結器
    controllerAnimator|設定の入出力Animator。
    connectedTrain_F|+Z側連結車両(初期化時自動設定)
    connectedTrain_B|-Z側連結車両
    Bogie_F|+Z側台車中心
    BogieWheel_F|+Z側台車オブジェクト
    RailID_F|+Z側台車が載っているレールの識別子。実行時debug用
    BogieRail_F|+Z側台車が載っているレール
    Bogie_B|-Z側台車中心
    BogieWheel_B|-Z側台車オブジェクト
    RailID_B|-Z側台車が載っているレールの識別子。実行時debug用
    BogieRail_B|-Z側台車が載っているレール
    InitsyncRecieveMode|初期化同期受信モード(True必須)

    |機能|概要|
    |---:|:---|
    controllerAnimatorについて|必須パラメータと機能を示します。<br>inは入力、outは出力を表します
    in)RigidBodySpeed|車両の速度です。+Z方向を正、単位は[m/s]/100です。(motionTimeでの扱いを想定しているため)
    in)BrakePressure|ブレーキ圧です。緩解圧を1としています。
    out)HandBrakeState|手ブレーキ圧設定値です。
    out)HandBrakeForce|手ブレーキ力設定値です。
    ----------|---------
    連結器処理の仕様|連結が行われると、Train.csをアタッチしたオブジェクトのConfigurable Jointに自分の連結器位置と連結対象車両の連結器位置を用いて自動でAnchorの設定が行われ、またOwnerが移行されます。
    ----------|---------
    BogieWheel、Bogie|BogieWheelオブジェクトは実行中常にBogieに最も近いレール上に合わせられます。この時車両をレール上に拘束するのはRigidBodyのJointを用いています。
    ----------|---------
    Owner周りの仕様|Owner変更が行われると、前後の車両に直ちに伝播します。
    ----------|---------
    同期について|同期は連結された前後1両の車両のみが行います。

    </details>
- CouplerObj
    
    連結スクリプト
    <details>
    <summary>設定値及び仕様</summary>

    |設定値|概要|
    |---:|:---|
    TrainScript|自身を装備している車両本体のスクリプト
    Knuckle_Closed|ナックル開閉状態
    state|連結器状態 0=lock, 1=unlock, 2=open
    CouplerAudioSource|音源再生に用いるAudioSource。
    カプラ音源|対応イベント時にCouplerAudioSourceで再生されるAudioClip。
    FrontOrBack|連結器方向。 +Z=Front
    disconnectForce|連結器開放に必要な力
    connectedCoupler|連結している連結器
    knuckleModel|ナックルのモデルオブジェクト。閉y=0,開y=90に回転されます。
    knuckleKey|予約済みフィールド（未使用）

    |機能|概要|
    |---:|:---|
    Editor上での連結|Editor上でconnectedCouplerを指定しておくことで連結状態を設定しておくことが出来ます。
    突放貨車向けの調整|disconnectForceを大きめにすることで、動き出し衝動で外れるのをある程度防止できます
    
    </details>
- FlangeSoundPlayer
    
    フランジ音を再生するスクリプト
    <details>
    <summary>設定値及び仕様</summary>

    |設定値|概要|
    |---:|:---|
    trainBody|列車のRigidBody
    wheelBody_transform|Trainで指定しているWheelオブジェクト
    FlangeSound|フランジの音源
    DotThreshould|フランジ音の再生を開始する押圧閾値
    MagnitudeThreshould|フランジ音の再生を開始する速度閾値

    |機能|概要|
    |---:|:---|
    仕様|Wheelの方向=レール方向と車両の移動方向の差から、内積を計算し、横方向の速度を計算します。単位はm/s
    ----------|DotThreshouldは横方向の閾値、MagnitudeThreshouldは前後方向の閾値。両方を超えているとフランジ音が再生されます。
    ----------|音量は横方向の速度に比例しますが、音量はDot-DotThreshouldになっています。
    
    </details>
- JointSoundPlayer
    
    ジョイント音を再生するスクリプト
    SoundDetectorの付いたトリガーコライダーと接触することで、ジョイント音を再生します。
    <details>
    <summary>設定値及び仕様</summary>

    |設定値|概要|
    |---:|:---|
    sound|再生する音声

    |機能|概要|
    |---:|:---|
    仕様|このスクリプトを付けるオブジェクトにもコライダーが必要です。またコライダーはRigidBodyの影響下である必要があります。
    ONSP系Spatializationのバグ|RigidBody影響下の音源をSpatializationありで再生するとノイズが発生します。SpatialAudioSourceをつけろとSDKはうるさく言ってきますが無視して構いません。
    
    </details>
