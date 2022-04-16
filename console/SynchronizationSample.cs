using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Linq;

namespace console
{

    public class SynchronizationSample
    {
        private static Random _rnd = new Random();
        private int _i;

        public Messenger Messenger { get; }

        public SynchronizationSample()
        {
            Messenger = new Messenger();
        }

        public IObservable<(int increment, int delay)> GetObservableMerge()
        {
            var tasks = Enumerable
                .Range(0, 50)
                .Select(x => 
                    Observable.FromAsync(async () =>
                    {
                        var delay = _rnd.Next(10);
                        await Task.Delay(delay);
                        int callNumber = Interlocked.Increment(ref _i);
                        return (callNumber, delay);
                    }))
                .Merge();
            return tasks;
        }
        public IObservable<(int increment, int delay)> GetObservableConcat()
        {
            var tasks = Enumerable
                .Range(0, 50)
                .Select(x => 
                    Observable.FromAsync(async () =>
                    {
                        var delay = _rnd.Next(10);
                        await Task.Delay(delay);
                        int callNumber = Interlocked.Increment(ref _i);
                        return (callNumber, delay);
                    }))
                .Concat();
            return tasks;
        }

        public async Task SendMessagesAsync(int nmessages)
        {
            var tasks = Enumerable.Range(0, nmessages).Select(async x => {
                await Task.Delay(50);
                Messenger.OnMessage();
            });
            await Task.WhenAll(tasks);
        }

    }

    public class Message : EventArgs
    {
        public int MessageNumber { get; set; }
    }
    public class Messenger
    {
        private int _i;
        public event EventHandler<Message> MessageReceived;
        public void OnMessage()
        {
            Interlocked.Increment(ref _i);
            var args = new Message()
            {
                MessageNumber = _i
            };
            MessageReceived?.Invoke(this, args);

        }
    }


}