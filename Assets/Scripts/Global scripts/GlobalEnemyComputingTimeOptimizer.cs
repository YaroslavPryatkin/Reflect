using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-150)]
public class GlobalEnemyComputingTimeOptimizer : SceneLocalSingleton<GlobalEnemyComputingTimeOptimizer>
{
    public const float CalculatingTargetDuration = 0.7f;
    public static bool CanStartCalculation(int index)
    {
        if (index == Instance._currentIndex.Value && !Instance._returnedTrue)
        {
            Instance._returnedTrue = true;
            return true;
        }

        return false;
    }

    private readonly List<EnemyAI> _enemies = new();
    private readonly UtilityTimers.FractionBlockingValueTimer<int> _currentIndex = 0;
    private float _timeForOneEnemy;
    private bool _haveEnemies=false;
    private bool _returnedTrue = false;

    public static int AddEnemy(EnemyAI enemy)
    {
        return Instance.AddEnemyInstance(enemy);
    }
    
    private int AddEnemyInstance(EnemyAI enemy)
    {
        _enemies.Add(enemy);
        UpdateTime();
        return _enemies.Count - 1;
    }

    public static void DeleteEnemy(int index)
    {
        Instance.DeleteEnemyInstance(index);
    }
    
    private void DeleteEnemyInstance(int index)
    {
        _enemies.RemoveAt(index);
        for (; index < _enemies.Count; ++index)
        {
            _enemies[index].ChangeComputingIndex(index);
        }
        UpdateTime();
    }

    private void UpdateTime()
    {
        if (_enemies.Count > 0)
        {
            _timeForOneEnemy = CalculatingTargetDuration / _enemies.Count;
            _currentIndex.SetForce(0, _timeForOneEnemy);
            _returnedTrue = false;
            _haveEnemies = true;
        }
        else
        {
            _haveEnemies = false;
        }
    }

    private void Update()
    {
        if (_haveEnemies)
        {
            if(_currentIndex.TrySet((_currentIndex.Value + 1) % _enemies.Count, _timeForOneEnemy))
                _returnedTrue = false;
        }
    }
}
