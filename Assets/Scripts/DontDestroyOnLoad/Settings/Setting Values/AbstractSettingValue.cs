using System;
using UnityEngine;
using System.Collections.Generic;

public abstract class AbstractSettingValue : ScriptableObject, ISaveValue
{
    public virtual void SettingReset()
    {
        OnExternalValueUpdated?.Invoke();
    }
    
    public abstract void Set(string data);
    public abstract string Get();

    public Action OnExternalValueUpdated;
}