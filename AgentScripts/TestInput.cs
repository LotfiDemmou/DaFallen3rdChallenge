using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class TestInput
{
    public static string PressD()
    {
        var kb = Keyboard.current;
        if (kb == null) return "no keyboard device";
        InputSystem.QueueStateEvent(kb, new KeyboardState(Key.D));
        InputSystem.Update();
        return "queued D down";
    }

    public static string Release()
    {
        var kb = Keyboard.current;
        if (kb == null) return "no keyboard device";
        InputSystem.QueueStateEvent(kb, new KeyboardState());
        InputSystem.Update();
        return "queued release";
    }
}
