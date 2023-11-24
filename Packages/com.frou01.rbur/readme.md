# RigidBody Udon Railway
## 概要
RigidBodyUdonRailwayはUdonとRigidBodyを用いてVRCワールドに操作可能な鉄道を敷くことを目的に開発さた、エディタスクリプト、U#スクリプト及びPrefabが含まれています。

## 目次
- [レール関係][レール関係]
    - [Editor向け][レール関係_Editor向け]
    - [Runtime(VRC)向け][レール関係_Runtime(VRC)向け]


## コンポーネント及びプレハブ
[レール関係]: #レール関係
- レール関係

        レールモデルを敷くために用意された編集用スクリプトと、VRC内で車両にレールデータを受け渡す物

    [レール関係_Editor向け]: #レール関係_Editor向け
    - Editor向け
        - railModelTiler
            - モデル敷設スクリプト。
            - <details>
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
            - モデル配置スクリプト
            - <details>
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
            - railModelTiler一括実行スクリプト。 指定されたpath全体にレールを生成します。
            - <details>
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
    [レール関係_Runtime(VRC)向け]: #レール関係_Runtime(VRC)向け
    - Runtime(VRC)向け
        - Rail_Script
            - レールスクリプト。始点と終点双方のレール参照を保持し、車両に次のレールを伝えます。
            

            - <details>
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
            - レール管理スクリプト。
            
            ビルド時自動設定が行われるため、設置のみ必要です。シーンルート直下に配置してください。
        - RailroadCrossing
            - 踏切スクリプト
            
            JointSoundPlayerを検出し、範囲内に列車が進入/退出するとAnimatorのパラメーターを書き換えます。
            

            - <details>
                <summary>設定値/仕様</summary>
                
                |設定値|概要|
                |---:|:---|
                animators|設定先Animator。複数指定可能です。
                paramName|書き換えるパラメーター(bool,in=true,out=false)
            </details>
        - RailroadCrossing
            - 踏切スクリプト
            
            JointSoundPlayerを検出し、範囲内に列車が進入/退出するとAnimatorのパラメーターを書き換えます。
            

            - <details>
                <summary>設定値/仕様</summary>
                
                |設定値|概要|
                |---:|:---|
                animators|設定先Animator。複数指定可能です。
                paramName|書き換えるパラメーター(bool,in=true,out=false)
            </details>
