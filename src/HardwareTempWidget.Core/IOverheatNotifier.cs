namespace HardwareTempWidget.Core;

public interface IOverheatNotifier
{
    void Notify(string title, string message);
}
