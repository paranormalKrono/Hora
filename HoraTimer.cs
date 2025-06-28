using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class HoraTimer: MonoBehaviour
{

    private float _value = 0f;
    private IEnumerator _ITimer;
    private bool _isITimer = false;

    public delegate void Callback();

    public UnityAction<float> OnTimerValueChanged;
    public UnityAction OnTimerValueChangedOne;
    public float Value { get => _value; }

    public void Change(float value)
    {
        _value += value;
        if (_value < 0f)
        {
            value -= -_value;
            _value = 0f;
        }
        OnTimerValueChanged?.Invoke(value);
    }

    public void SetValue(float value)
    {
        float diff = value - _value;
        _value = value;
        if (_value < 0f)
        {
            diff -= -_value;
            _value = 0f;
        }
        OnTimerValueChanged?.Invoke(diff);
    }

    public void StartTimer(float target_value, float speed, Callback OnTimerEnd = null)
    {
        if (_isITimer)
        {
            _isITimer = false;
            StopCoroutine(_ITimer);
        }
        _ITimer = IStart(target_value, speed, OnTimerEnd);
        StartCoroutine(_ITimer);
    }

    public void Stop()
    {
        if (_isITimer)
        {
            _isITimer = false;
            StopCoroutine(_ITimer);
        }
    }

    private IEnumerator IStart(float target_value, float speed, Callback OnTimerEnd)
    {
        _isITimer = true;
        float sign = math.sign(target_value - _value);
        float change = speed * sign;
        WaitForSeconds wait = new WaitForSeconds(1f / speed);
        while (sign * (target_value - _value) > 0.1f) // 0.1, because sign can be 0 == target = value
        {
            _value += sign;
            OnTimerValueChangedOne?.Invoke();
            yield return wait;
        }
        _isITimer = false;
        OnTimerEnd?.Invoke();
    }
}