namespace PowerSupplyApp.TUI
{
    public class ConsoleKeyEventQueue
    {
        private readonly Queue<ConsoleKeyInfo> queue = new Queue<ConsoleKeyInfo>();
        private readonly HashSet<ConsoleKeyInfo> eventSet = new HashSet<ConsoleKeyInfo>();

        public void SendKey(ConsoleKeyInfo eventMessage)
        {
            if (!eventSet.Contains(eventMessage))
            {
                queue.Enqueue(eventMessage);
                eventSet.Add(eventMessage);
            }
        }

        public ConsoleKeyInfo? ReceiveKey()
        {
            if (queue.Count > 0)
            {
                var eventMessage = queue.Dequeue();
                eventSet.Remove(eventMessage);
                return eventMessage;
            }
            return null;
        }
    }
}
