public interface IUndoableAction
{
    void Execute();
    void Undo();
    void Redo();
    string Description { get; }
}