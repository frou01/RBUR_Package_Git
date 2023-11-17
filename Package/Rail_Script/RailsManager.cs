
using UdonSharp;
using Unity.Collections;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace frou01.RigidBodyTrain
{
    public class RailsManager : UdonSharpBehaviour
    {

        public Rail_Script[] Rails;

        [System.NonSerialized] public int railsNum = 0;

        [System.NonSerialized] public int id;

        void Start()
        {
            if(Rails == null)
            {
                CountRailOnChild(transform);
                Rails = new Rail_Script[railsNum];
                id = 0;
                SetRailOnChild(transform);
            }
        }

        [RecursiveMethod]
        public void CountRailOnChild(Transform currentTransform)
        {
            //Debug.Log("SearchingOn " + currentTransform + " Child Num " + currentTransform.childCount);
            foreach (Transform child in currentTransform)
            {
                //Debug.Log("SearchingOn " + currentTransform + " , now seeing " + child);
                if (child.gameObject.GetComponent<Rail_Script>() != null)
                {
                    railsNum++;
                }
                CountRailOnChild(child);
            }
            //Debug.Log("SearchingOn " + currentTransform + " Child Num " + currentTransform.childCount);
            //for (int i = 0; i < currentTransform.childCount; i++)
            //{
            //    if (currentTransform.GetChild(i).childCount > 0) CountRailOnChild(currentTransform.GetChild(i));
            //    Debug.Log("SearchingOn " + currentTransform + " , " + i);
            //    if(i < currentTransform.childCount)
            //    {
            //        Transform child = currentTransform.GetChild(i);
            //        if (child.gameObject.GetComponent<Rail_Script>() != null)
            //        {
            //            railsNum++;
            //        }
            //    }
            //}
        }
        [RecursiveMethod]
        public void SetRailOnChild(Transform currentTransform)
        {
            foreach (Transform child in currentTransform)
            {
                if (child.gameObject.GetComponent<Rail_Script>() != null)
                {
                    Rails[id] = child.gameObject.GetComponent<Rail_Script>();
                    child.gameObject.GetComponent<Rail_Script>().RailID = id;
                    id++;
                }
                SetRailOnChild(child);
            }
        }
    }
}
