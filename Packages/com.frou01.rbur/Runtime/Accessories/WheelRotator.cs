
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WheelRotator : UdonSharpBehaviour
{
    [SerializeField] Transform targetWheel;
    [SerializeField] Rigidbody targetRigidBody;
    [SerializeField] float multiply;
    Vector3 relVec;
    Vector3 axis = Vector3.right;
    private void Update()
    {
        relVec = Quaternion.Inverse(targetRigidBody.rotation) * targetRigidBody.velocity;
        targetWheel.Rotate(axis, relVec.z * multiply);
    }
}
