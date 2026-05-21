using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BFX
{
    public abstract class IScriptInstance : MonoBehaviour
    {
        internal bool CanUpdate;
        
        internal virtual void OnEnable()
        {
            GlobalUpdate.CreateInstanceIfRequired();
            GlobalUpdate.ScriptInstances.Add(this);
            CanUpdate = true;
            OnEnableExtended();
        }

        internal virtual void OnDisable()
        {
            GlobalUpdate.ScriptInstances.Remove(this);
            CanUpdate = false;
            OnDisableExtended();
        }

        internal abstract void OnEnableExtended();
        internal abstract void OnDisableExtended();

        internal abstract void ManualUpdate();
    }
}