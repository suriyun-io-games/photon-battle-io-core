using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UIBase : MonoBehaviour
{
    public GameObject root;
    public bool hideOnAwake;
    public UnityEvent onShow;
    public UnityEvent onHide;
    private bool isAwaken;

    protected virtual void Awake()
    {
        if (isAwaken)
            return;
        isAwaken = true;
        ValidateRoot();
        if (hideOnAwake)
            Hide();
    }

    public void ValidateRoot()
    {
        if (root == null)
            root = gameObject;
    }

    public virtual void Show()
    {
        isAwaken = true;
        ValidateRoot();
        if (onShow != null)
            onShow.Invoke();
        root.SetActive(true);
    }

    public virtual void Hide()
    {
        isAwaken = true;
        ValidateRoot();
        if (onHide != null)
            onHide.Invoke();
        root.SetActive(false);
    }

    public virtual bool IsVisible()
    {
        ValidateRoot();
        return root.activeSelf;
    }
}
