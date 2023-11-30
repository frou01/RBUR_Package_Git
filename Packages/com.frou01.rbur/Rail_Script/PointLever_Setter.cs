
using Cinemachine;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using static Cinemachine.CinemachinePathBase;

namespace frou01.RigidBodyTrain
{
    public class PointLever_Setter : UdonSharpBehaviour
    {
        public Rail_Script from1;
        public Rail_Script from2;
        public bool changeType1;//true:next  false:prev
        public bool changeType2;//true:next  false:prev
        public Rail_Script to1;
        public Rail_Script to2;

        [UdonSynced] public bool state;

        void Start()
        {
            if (!state)
            {
                //Debug.Log("debug1 " + to1.name);
                applyChange(to1);
            }
            else
            {
                //Debug.Log("debug2 " + to2.name);
                applyChange(to2);
            }
        }

        public void SetRoute1()
        {
            if (Networking.IsOwner(gameObject)) owner_SetRoute1();
        }
        public void SetRoute2()
        {
            if (Networking.IsOwner(gameObject)) owner_SetRoute2();
        }

        public void owner_SetRoute1()
        {
            //Debug.Log("debug1 " + to1.name);
            applyChange(to1);
            state = false;
            RequestSerialization();
        }
        public void owner_SetRoute2()
        {
            //Debug.Log("debug2 " + to2.name);
            applyChange(to2);
            state = true;
            RequestSerialization();
        }

        public override void OnDeserialization()
        {

            //Debug.Log("debug_PointRecieve");
            if (!state)
            {
                //Debug.Log("debug1 " + to1.name);
                applyChange(to1);
            }
            else
            {
                //Debug.Log("debug2 " + to2.name);
                applyChange(to2);
            }
        }

        public void applyChange(Rail_Script target)
        {
            if(from1 != null)
            {
                if (changeType1) from1.next = target;
                else from1.prev = target;
            }
            if (from2 != null)
            {
                if (changeType2) from2.next = target;
                else from2.prev = target;
            }
        }

        Vector3 offset = new Vector3(0, 1, 0);
        void OnDrawGizmos()
        {
            DrawGizmo(0.1f);
        }
        void OnDrawGizmosSelected()
        {
            DrawGizmo(1f);
        }

        void DrawGizmo(float alpha)
        {


            if (from1 != null)
            {
                CinemachinePathBase fromPath = from1.cinemachinePath;

                float fromChangeUnit;
                float fromChangeLineStart;
                if (changeType1)
                {
                    fromChangeUnit = fromPath.MaxPos;
                    fromChangeLineStart = fromPath.FromPathNativeUnits(fromChangeUnit, PositionUnits.Distance);
                    fromChangeLineStart -= 10;
                    fromChangeLineStart = fromPath.ToNativePathUnits(fromChangeLineStart, PositionUnits.Distance);
                }
                else
                {
                    fromChangeUnit = fromPath.MinPos;
                    fromChangeLineStart = fromPath.FromPathNativeUnits(fromChangeUnit, PositionUnits.Distance);
                    fromChangeLineStart += 10;
                    fromChangeLineStart = fromPath.ToNativePathUnits(fromChangeLineStart, PositionUnits.Distance);
                }

                Vector3 changeLinePoint = fromPath.EvaluatePosition(fromChangeUnit);
                Vector3 changeLineStart = fromPath.EvaluatePosition(fromChangeLineStart);
                Gizmos.color = new Color(0f, 1f, 0f, alpha);
                Gizmos.DrawLine(changeLineStart + offset * 2, changeLinePoint);

                Gizmos.color = new Color(0f, 1f, 1f, alpha);
                CinemachinePathBase toPath = to1.cinemachinePath;
                float nextClosestUnit = toPath.FindClosestPoint(changeLinePoint, 0, -1, 10);
                nextClosestUnit = toPath.FromPathNativeUnits(nextClosestUnit, PositionUnits.Distance);
                if (nextClosestUnit > toPath.PathLength / 2) nextClosestUnit -= 10;
                else nextClosestUnit += 10;
                nextClosestUnit = toPath.ToNativePathUnits(nextClosestUnit, PositionUnits.Distance);
                Vector3 toClosestPoint = toPath.EvaluatePosition(nextClosestUnit);
                Gizmos.DrawLine(changeLinePoint, toClosestPoint + offset);

                Gizmos.color = new Color(1f, 1f, 0f, alpha);
                toPath = to2.cinemachinePath;
                nextClosestUnit = toPath.FindClosestPoint(changeLinePoint, 0, -1, 10);
                nextClosestUnit = toPath.FromPathNativeUnits(nextClosestUnit, PositionUnits.Distance);
                if (nextClosestUnit > toPath.PathLength / 2) nextClosestUnit -= 10;
                else nextClosestUnit += 10;
                nextClosestUnit = toPath.ToNativePathUnits(nextClosestUnit, PositionUnits.Distance);
                toClosestPoint = toPath.EvaluatePosition(nextClosestUnit);
                Gizmos.DrawLine(changeLinePoint, toClosestPoint + offset);
            }
            if (from2 != null)
            {
                CinemachinePathBase fromPath = from2.cinemachinePath;

                float fromChangeUnit;
                float fromChangeLineStart;
                if (changeType2)
                {
                    fromChangeUnit = fromPath.MaxPos;
                    fromChangeLineStart = fromPath.FromPathNativeUnits(fromChangeUnit, PositionUnits.Distance);
                    fromChangeLineStart -= 10;
                    fromChangeLineStart = fromPath.ToNativePathUnits(fromChangeLineStart, PositionUnits.Distance);
                }
                else
                {
                    fromChangeUnit = fromPath.MinPos;
                    fromChangeLineStart = fromPath.FromPathNativeUnits(fromChangeUnit, PositionUnits.Distance);
                    fromChangeLineStart += 10;
                    fromChangeLineStart = fromPath.ToNativePathUnits(fromChangeLineStart, PositionUnits.Distance);
                }

                Vector3 changeLinePoint = fromPath.EvaluatePosition(fromChangeUnit);
                Vector3 changeLineStart = fromPath.EvaluatePosition(fromChangeLineStart);
                Gizmos.color = new Color(0f, 1f, 0f, alpha);
                Gizmos.DrawLine(changeLineStart + offset * 2, changeLinePoint);

                Gizmos.color = new Color(0f, 1f, 1f, alpha);
                CinemachinePathBase toPath = to1.cinemachinePath;
                float nextClosestUnit = toPath.FindClosestPoint(changeLinePoint, 0, -1, 10);
                nextClosestUnit = toPath.FromPathNativeUnits(nextClosestUnit, PositionUnits.Distance);
                if (nextClosestUnit > toPath.PathLength / 2) nextClosestUnit -= 10;
                else nextClosestUnit += 10;
                nextClosestUnit = toPath.ToNativePathUnits(nextClosestUnit, PositionUnits.Distance);
                Vector3 toClosestPoint = toPath.EvaluatePosition(nextClosestUnit);
                Gizmos.DrawLine(changeLinePoint, toClosestPoint + offset);

                Gizmos.color = new Color(1f, 1f, 0f, alpha);
                toPath = to2.cinemachinePath;
                nextClosestUnit = toPath.FindClosestPoint(changeLinePoint, 0, -1, 10);
                nextClosestUnit = toPath.FromPathNativeUnits(nextClosestUnit, PositionUnits.Distance);
                if (nextClosestUnit > toPath.PathLength / 2) nextClosestUnit -= 10;
                else nextClosestUnit += 10;
                nextClosestUnit = toPath.ToNativePathUnits(nextClosestUnit, PositionUnits.Distance);
                toClosestPoint = toPath.EvaluatePosition(nextClosestUnit);
                Gizmos.DrawLine(changeLinePoint, toClosestPoint + offset);
            }
        }
    }
}
