namespace DvSceneTool;

public class ChangeValueCommand<T> : ICommand
{
    Action<T> apply;
    T oldValue;
    T newValue;

    public ChangeValueCommand(Action<T> apply, T oldValue, T newValue)
    {
        this.apply = apply;
        this.oldValue = oldValue;
        this.newValue = newValue;
    }

    public void Execute() => apply(newValue);
    public void Undo() => apply(oldValue);
}
