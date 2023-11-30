using System;
using UnityEngine;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
    public class Train_Property : MonoBehaviour
    {

        [SerializeField]
        [Tooltip("手ブレーキ性能（車両独立ブレーキとして使用している場合もあります）")]
        public float HandBrakeForce;

        [SerializeField]
        [Tooltip("貫通ブレーキ性能")]
        public float BrakeForce;

        [SerializeField]
        [Tooltip("停止中の抵抗")]
        public float static_friction;

        [SerializeField]
        [Tooltip("走行中の抵抗")]
        public float friction;

        [SerializeField]
        [Tooltip("前ブレーキ管の開放状態")]
        public bool BrakeOpenF;
        [SerializeField]
        [Tooltip("後ブレーキ管の開放状態")]
        public bool BrakeOpenB;

        [SerializeField]
        [Tooltip("前ボギーが載っているレール")]
        public UdonBehaviour BogieRail_F;

        [SerializeField]
        [Tooltip("後ボギーが載っているレール")]
        public UdonBehaviour BogieRail_B;

        [SerializeField]
        [Tooltip("前部連結車両")]
        public Train_Property connectedTrain_F;
        [SerializeField]
        [Tooltip("後部連結車両")]
        public Train_Property connectedTrain_B;



        [SerializeField]
        [Tooltip("制御UDON（パフォーマンスに大きな影響があるので非推奨）")]
        public UdonBehaviour controllerUdon;

        [SerializeField]
        [Tooltip("制御アニメーター")]
        public Animator controllerAnimator;

        [NonSerialized]public UdonBehaviour FCoupler;
        [NonSerialized] public UdonBehaviour BCoupler;

    }
}
