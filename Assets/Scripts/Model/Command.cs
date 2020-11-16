namespace CFO.Model
{
    public abstract class Command : ICommand
    {
        public abstract void Execute(string[] args);
    }
}