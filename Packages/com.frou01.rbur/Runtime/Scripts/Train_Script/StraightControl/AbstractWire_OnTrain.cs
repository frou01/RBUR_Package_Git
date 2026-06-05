using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
    public class AbstractWire_OnTrain : UdonSharpBehaviour
    {
        [SerializeField] AbstractSignalWrapper OriginalSignal;
        AbstractSignalWrapper ConnectedSignal;
        [SerializeField] AbstractWire_Connector[] Connectors;

        protected AbstractWire_Connector[] GetConnectors()
        {
            return Connectors;
        }
        public void onChangeConnection()
        {
            AbstractWire_OnTrain[] foundWires = new AbstractWire_OnTrain[0];
            AbstractWire_OnTrain[] newFoundWires;
            FindConnection(foundWires,out newFoundWires);

            int Safety = 1000;
            while(newFoundWires.Length > 0 && Safety > 0)
            {
                AbstractWire_OnTrain[] TempFoundWires = new AbstractWire_OnTrain[foundWires.Length + newFoundWires.Length];
                foundWires.CopyTo(TempFoundWires, 0);
                newFoundWires.CopyTo(TempFoundWires, foundWires.Length);

                foundWires = TempFoundWires;
#if UNITY_EDITOR
                foreach (AbstractWire_OnTrain foundWire in foundWires)
                {
                    Debug.Log($"current Found wire: {foundWire.GetInstanceID()}", foundWire);
                }
#endif
                FindConnection(foundWires, out newFoundWires);

#if UNITY_EDITOR
                foreach (AbstractWire_OnTrain foundWire in newFoundWires)
                {
                    Debug.Log($"new Found wire: {foundWire.GetInstanceID()}", foundWire);
                }
#endif
                Safety--;
            }

#if UNITY_EDITOR
            foreach (AbstractWire_OnTrain foundWire in foundWires)
            {
                Debug.Log($"Finally Found wire: {foundWire.GetInstanceID()}", foundWire);
            }
#endif

        }

        public void FindConnection(in AbstractWire_OnTrain[] FoundWires, out AbstractWire_OnTrain[] newFoundWires)
        {
            AbstractWire_Connector[] Connectors = GetConnectors();
            AbstractWire_OnTrain[]  tempFoundWires = new AbstractWire_OnTrain[Connectors.Length];
            int ignoredCount = 0;
            for(int idx = 0;idx < Connectors.Length;idx++)
            {
                tempFoundWires[idx - ignoredCount] = Connectors[idx].getOtherConnectedWire(this);
                if(tempFoundWires[idx - ignoredCount] != null)
                {
                    bool alreadyTouched = false;
                    foreach (AbstractWire_OnTrain foundWire in FoundWires)
                    {
                        if (tempFoundWires[idx - ignoredCount] != foundWire) continue;
                        else
                        {
                            alreadyTouched = true;
                            break;
                        }
                    }
                    if (alreadyTouched)
                    {
                        tempFoundWires[idx - ignoredCount] = null;
                        ignoredCount++;
                    }
                }
                else
                {
                    ignoredCount++;
                }
            }
            newFoundWires = new AbstractWire_OnTrain[tempFoundWires.Length - ignoredCount];
            for (int idx = 0; idx < tempFoundWires.Length - ignoredCount; idx++)
            {
                newFoundWires[idx] = tempFoundWires[idx];
            }

        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            Gizmos.DrawSphere(transform.position, 0.3f);
            foreach (AbstractWire_Connector abstractWire_Connector in GetConnectors())
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.5f);
                Gizmos.DrawLine(transform.position, abstractWire_Connector.transform.position);
            }
        }
#endif
    }
}