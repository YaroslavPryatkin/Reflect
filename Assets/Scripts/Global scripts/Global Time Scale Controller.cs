using UnityEngine;
using System.Collections.Generic;

public static class GlobalTimeScaleController
{
    private static Dictionary<object, float> _timeScales = new ();
    private static float currentTimeScale = 1;
    
    public static void ChangeTimePace(object caller, float scale)
    {
        if (_timeScales.TryGetValue(caller, out var previousScale))
        {
            _timeScales[caller] = scale * previousScale;
        }
        else
        {
            _timeScales.Add(caller, scale);
        }
        currentTimeScale *= scale;
        SwitchTimeScale();
    }

    public static void ReturnTimePace(object caller)
    {
        if (!_timeScales.Remove(caller, out var scale)) return;
        
        if (_timeScales.Count == 0)
        {
            currentTimeScale = 1;
        }
        else
        {
            currentTimeScale /= scale;
        }

        SwitchTimeScale();
    }

    private static void SwitchTimeScale()
    {
        Time.timeScale = currentTimeScale;
        Time.fixedDeltaTime = 0.02f * currentTimeScale;
    }

}
