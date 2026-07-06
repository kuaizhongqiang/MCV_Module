using System;
using UnityEngine;

public interface IObj
{
    public T GetObj<T>() where T : Component;
    public bool IsInteractable { get; }
    public event Action MoEnter;
    public event Action MoExit;
    public event Action MoClick;
    public event Action MoClickRight;
    public event Action MoClickDouble;
    public event Action MoDown;
    public event Action MoUp;
    public event Action<Vector2> MoMove;
}