using UnityEngine;

// ═══════════════════════════════════════════════════════════════
// FILE: DoorStateMachine.cs
// Chứa: IDoorState, DoorStateContext, và tất cả Concrete States
// Pattern: State Pattern
// ═══════════════════════════════════════════════════════════════

#region State Context

/// <summary>
/// State context for door
/// </summary>
public class DoorStateContext
{
    public DoorController Controller { get; private set; }
    
    public DoorStateContext(DoorController controller)
    {
        Controller = controller;
    }
}

#endregion

#region State Interface

/// <summary>
/// Interface for Door States
/// Pattern: State Pattern
/// </summary>
public interface IDoorState
{
    void OnEnter(DoorStateContext context);
    void OnExit(DoorStateContext context);
    void Open(DoorStateContext context);
    void Close(DoorStateContext context);
}

#endregion

#region Concrete States

/// <summary>
/// Door is closed (idle)
/// </summary>
public class DoorClosedState : IDoorState
{
    public void OnEnter(DoorStateContext context)
    {
        // Door is now closed
    }

    public void OnExit(DoorStateContext context)
    {
    }

    public void Open(DoorStateContext context)
    {
        // Transition to Opening state
        context.Controller.SetState(new DoorOpeningState());
    }

    public void Close(DoorStateContext context)
    {
        // Already closed, do nothing
        Debug.LogWarning("[DoorState] Door is already closed!");
    }
}

/// <summary>
/// Door is opening (animating)
/// </summary>
public class DoorOpeningState : IDoorState
{
    public void OnEnter(DoorStateContext context)
    {
        // Play open animation
        context.Controller.PlayOpenAnimation();
    }

    public void OnExit(DoorStateContext context)
    {
    }

    public void Open(DoorStateContext context)
    {
        // Already opening, ignore
        Debug.LogWarning("[DoorState] Door is already opening!");
    }

    public void Close(DoorStateContext context)
    {
        // Cannot close while opening
        Debug.LogWarning("[DoorState] Cannot close while opening!");
    }
}

/// <summary>
/// Door is open (idle)
/// </summary>
public class DoorOpenState : IDoorState
{
    public void OnEnter(DoorStateContext context)
    {
        // Door is now open
    }

    public void OnExit(DoorStateContext context)
    {
    }

    public void Open(DoorStateContext context)
    {
        // Already open, do nothing
        Debug.LogWarning("[DoorState] Door is already open!");
    }

    public void Close(DoorStateContext context)
    {
        // Transition to Closing state
        context.Controller.SetState(new DoorClosingState());
    }
}

/// <summary>
/// Door is closing (animating)
/// </summary>
public class DoorClosingState : IDoorState
{
    public void OnEnter(DoorStateContext context)
    {
        // Play close animation
        context.Controller.PlayCloseAnimation();
    }

    public void OnExit(DoorStateContext context)
    {
    }

    public void Open(DoorStateContext context)
    {
        // Cannot open while closing
        Debug.LogWarning("[DoorState] Cannot open while closing!");
    }

    public void Close(DoorStateContext context)
    {
        // Already closing, ignore
        Debug.LogWarning("[DoorState] Door is already closing!");
    }
}

#endregion