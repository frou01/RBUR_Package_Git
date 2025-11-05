
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Legacy_BrakeModule : AbstractBrake
{
    protected float[] train_legacy_brakePressure_float = new float[1];
    protected override void Start()
    {
        base.Start();

        train_legacy_brakePressure_float = train.legacy_brakePressure_float;
    }
    protected override void Update()
    {
        base.Update();
        train_legacy_brakePressure_float[0] = m_straightBrakePressure;
    }
}
