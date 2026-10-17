using System.Windows;

namespace AccessibleOCRStudio;

internal static class DialogGuard
{
    private static bool _active;

    public static bool TryEnter()
    {
        if (_active) return false;
        _active = true;
        return true;
    }

    public static void Exit() => _active = false;

    public static MessageBoxResult Show(Window owner, string text, string caption = "Clear Read",
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.OK)
    {
        if (!TryEnter()) return Absorb(buttons);
        try { return MessageBox.Show(owner, text, caption, buttons, image, defaultResult); }
        finally { Exit(); }
    }

    public static bool? ShowDialog(Window dialog)
    {
        if (!TryEnter()) return null;
        try { return dialog.ShowDialog(); }
        finally { Exit(); }
    }

    public static bool? Run(Func<bool?> opener)
    {
        if (!TryEnter()) return false;
        try { return opener(); }
        finally { Exit(); }
    }

    internal static MessageBoxResult Absorb(MessageBoxButton buttons) =>
        buttons == MessageBoxButton.OK ? MessageBoxResult.OK
        : buttons == MessageBoxButton.YesNo ? MessageBoxResult.No
        : MessageBoxResult.Cancel;
}
