using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// ═══════════════════════════════════════════════════════
//  GameEventSO  —  Event không tham số
//  Tạo: chuột phải → Create → ZombieGame → Events → Game Event
// ═══════════════════════════════════════════════════════
[CreateAssetMenu(fileName = "Event_", menuName = "ZombieGame/Events/Game Event")]
public class GameEventSO : ScriptableObject
{
    private readonly List<GameEventListenerSO> _listeners = new();

    public void Raise()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i].OnEventRaised();
    }

    public void Register(GameEventListenerSO listener) => _listeners.Add(listener);
    public void Unregister(GameEventListenerSO listener) => _listeners.Remove(listener);
}

// ═══════════════════════════════════════════════════════
//  GameEventListenerSO  —  Component gắn vào GameObject muốn lắng nghe
// ═══════════════════════════════════════════════════════
public class GameEventListenerSO : MonoBehaviour
{
    [SerializeField] private GameEventSO _event;
    [SerializeField] private UnityEvent _response;

    private void OnEnable() => _event?.Register(this);
    private void OnDisable() => _event?.Unregister(this);

    public void OnEventRaised() => _response?.Invoke();
}

// ═══════════════════════════════════════════════════════
//  FloatGameEventSO  —  Event kèm 1 float (HP, damage, ...)
//  Tạo: chuột phải → Create → ZombieGame → Events → Float Event
// ═══════════════════════════════════════════════════════
[CreateAssetMenu(fileName = "FloatEvent_", menuName = "ZombieGame/Events/Float Event")]
public class FloatGameEventSO : ScriptableObject
{
    private readonly List<FloatGameEventListenerSO> _listeners = new();

    public void Raise(float value)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i].OnEventRaised(value);
    }

    public void Register(FloatGameEventListenerSO listener) => _listeners.Add(listener);
    public void Unregister(FloatGameEventListenerSO listener) => _listeners.Remove(listener);
}

public class FloatGameEventListenerSO : MonoBehaviour
{
    [SerializeField] private FloatGameEventSO _event;
    [SerializeField] private UnityEvent<float> _response;

    private void OnEnable() => _event?.Register(this);
    private void OnDisable() => _event?.Unregister(this);

    public void OnEventRaised(float value) => _response?.Invoke(value);
}