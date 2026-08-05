namespace HardwareTempWidget.Core;

public interface IAutostartService
{
    bool IsEnabled { get; }

    void Enable();

    void Disable();
}
