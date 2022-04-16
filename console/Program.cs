using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace console
{
    class Program
    {
        private static Random _rnd = new Random();
        static void Main(string[] args)
        {
            //FirstDemo();
            //SubjectBasicsDemo();
            //DemoHotVsCold();
            //EventSample(10);
            //ComplexEventWithAsyncSampleAsync().GetAwaiter().GetResult();
            //ObservableGenerateFromFile();
            //SynchronizationDemo();
            //CancellationDemo();
            //LifestyleDemo();
            // SchedulingDemo();
            Console.ReadLine();
        }

        private static void SchedulingDemo()
        {
            Scheduling.ScheduleWorkThreads();
        }

        private static void SubjectBasicsDemo()
        {
            //SubjectBasics.PassingDataToSubject();
            // SubjectBasics.BehaviorSubject();
            // SubjectBasics.UsingAsyncSubjects();
            SubjectBasics.ReplaySubjectExample();
            SubjectBasics.CommonPitfall();
        }

        public static void FirstDemo()
        {
            Console.WriteLine("First demo: observable from enumerable");
            Console.WriteLine("Enter to start");
            Console.ReadLine();

            // IObservable<int> observable = Enumerable.Range(0, 10).ToObservable();
            IObservable<int> observable = Observable.Range(0, 10);
            var squared = observable.Select(x => x * x);

            IObserver<int> observer = Observer.Create<int>(
                onNext: next => Console.WriteLine($"Next value is {next}"),
                onCompleted: () => Console.WriteLine("Done processing"),
                onError: ex => Console.WriteLine(ex));
            
            observable.Subscribe(observer);
            squared.SubscribeConsole();
        }

        public static void DemoHotVsCold()
        {
            ColdObservable();
            HotObservable();
            HotObservableWithDynamicStopping();
        }

        private static void HotObservableWithDynamicStopping()
        {
            var stopIt = new Subject<int>();

            var continuos = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(250)) //emit data every 250 ms
                .Select(x => $"continuous hot interval {x}")
                .TakeUntil(stopIt)
                .Publish()
                .RefCount();

            Console.WriteLine("------------------ Hot emitting until stoppped --------------------");
            //continuous observable will emit values on background thread until console input
            continuos.SubscribeConsole();

            Console.ReadLine();
            stopIt.OnNext(0);
        }

        private static void HotObservable()
        {
            var counter = 0;

            var hot = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(250)) //emit data every 250 ms
                .Select(x => $"hot interval {x}")
                .Do(_ => counter++) //introduce side effects so we can confirm only one underlying stream exists
                .Take(15)
                .Publish() //heat the observable here with publish
                .RefCount(); //use refcount to automatically connect the observable on 
                             //first subscription and disconnect after all subscriptions are disposed
            
            Console.WriteLine("------------------ hot --------------------");
            var s1 = hot.SubscribeConsole();
            var s2 = hot.SubscribeConsole();
            var s3 = hot.Subscribe();

            Thread.Sleep(250 * 6);
            Console.WriteLine($"Shared counter is at {counter}");
            s1.Dispose();
            s2.Dispose();
            Console.WriteLine("Observable still emitting since a subscription exists...");
            Thread.Sleep(250 * 5);
            Console.WriteLine($"Counter is at {counter}");
            Console.WriteLine($"Disposing last connection");
            s3.Dispose();
            Thread.Sleep(250 * 8);
            Console.WriteLine($"Shared counter is at {counter}");
            Console.WriteLine("Enter to run next demo:");
            Console.ReadLine();

        }

        public static void ColdObservable()
        {
            var counter = 0;
            var cold = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(250))
                .Select(x => $"cold interval {x}")
                .Do(_ => counter++)
                .Take(5);

            Console.WriteLine("------------------ cold demo --------------------");
            Console.WriteLine("Creates a new observable for each subscribe call");
            Console.WriteLine("Enter to start");
            Console.ReadLine();
            cold.SubscribeConsole();
            cold.SubscribeConsole();

            //sleep on main thread while observable is executing for demo purposes
            Thread.Sleep(250 * 6); 

            Console.WriteLine("Enter to continue");
            Console.WriteLine($"Shared counter is at {counter}");
            Console.ReadLine();

        }


        public static void EventSample(int nJobs)
        {
            Console.WriteLine("Wrapping C# event demo");
            Console.WriteLine("Enter to start");
            Console.ReadLine();
            var jobs = new[] {"Muck code", "Call investors", "Check r/programmerhumor", "Deploy to prod", "Test in prod"};
            var fromEventSample = new FromEventSample();
            var messages = fromEventSample.Messages.Select(x => $"Message: {x.Message} \r\nPayload: {x.Data}");
            using (var subx = messages.SubscribeConsole())
            {
                for (int i = 0; i <= nJobs; i++)
                {
                    var job = jobs[_rnd.Next(jobs.Length)];
                    fromEventSample.DoSomeWork(job);
                }
            }
        }

        private static void SynchronizationDemo()
        {
            Console.WriteLine("Synchronization Demo");
            Console.WriteLine("Unsynchronized");
            Console.WriteLine("Enter to start");
            Console.ReadLine();

            var sample = new SynchronizationSample();
            sample
                .GetObservableMerge()
                .Select(x => $"{x.increment} {x.delay}")
                .SubscribeConsole();

            Console.ReadLine();
            Console.WriteLine("Unsynchronized");
            Console.WriteLine("Enter to start");
            Console.ReadLine();

            var sample2 = new SynchronizationSample();
            sample2
                .GetObservableConcat()
                .Select(x => $"{x.increment} {x.delay}")
                .SubscribeConsole();

            NextDemo("Synchronize incoming - unynchronized consumer");
            var sample3 = new SynchronizationSample();
            var messages = Observable.FromEventPattern<Message>(
                h => sample3.Messenger.MessageReceived += h,
                h => sample3.Messenger.MessageReceived -= h
            );
            var subx = messages.Select(x => x.EventArgs)
                .Subscribe(msg => {
                    Console.WriteLine($"Message {msg.MessageNumber} arrived");
                    //simulate CPU bound work
                    Thread.Sleep(1000);
                    Console.WriteLine($"Message {msg.MessageNumber} exit");
                });
            sample3.SendMessagesAsync(5).GetAwaiter().GetResult();

            NextDemo("Synchronize incoming - synchronized consumer");
            subx.Dispose();

            var sample4 = new SynchronizationSample();
            var messages2 = Observable.FromEventPattern<Message>(
                h => sample4.Messenger.MessageReceived += h,
                h => sample4.Messenger.MessageReceived -= h
            );
            subx = messages2.Select(x => x.EventArgs)
                .Synchronize()
                .Subscribe(msg => {
                    Console.WriteLine($"Message {msg.MessageNumber} arrived");
                    //simulate CPU bound work
                    Thread.Sleep(1000);
                    Console.WriteLine($"Message {msg.MessageNumber} exit");
                });
            sample4.SendMessagesAsync(5).GetAwaiter().GetResult();
            Console.ReadLine();
            subx.Dispose();

        }
        private static void LifestyleDemo()
        {
            var lifestyle = new LifestyleManagement();
            NextDemo("Take until interval");
            lifestyle.TakeUntilElapsed(3).SubscribeConsole();
            Console.ReadLine();

            NextDemo("Take until method called");
            lifestyle.TakeUntilMethodCalled().SubscribeConsole();
            Thread.Sleep(2000);
            lifestyle.StopTaking();

            NextDemo("Merging obsevables");
            lifestyle.AllMessages().SubscribeConsole();

            Console.ReadLine();
            NextDemo("Skip until start");

            lifestyle.SkipUntilStart().SubscribeConsole();

            Console.ReadLine();
            NextDemo("Skip until start, wait until restart");

            lifestyle.SkipUntilStartWithRepeat().SubscribeConsole();
        }

        private static void NextDemo(string demoName)
        {
            Console.WriteLine(demoName);
            Console.WriteLine("Enter to start");
            Console.ReadLine();
        }

        private static async Task ComplexEventWithAsyncSampleAsync()
        {
            Console.WriteLine("Wrapping complex C# event with TPL");
            Console.WriteLine("Enter to start");
            Console.ReadLine();

            var jobs = new[] {"Train ML model", "Find higgs boson", "Implement exactly once delivery"};
            var fromEventSample = new FromEventSample();
            var jobResults = fromEventSample.LongRunningJobs.Select(x => $"Executed {x.jobName} in {x.ms}ms");

            using (var subx = jobResults.SubscribeConsole())
            {

                var tasks = jobs
                    .Select(fromEventSample.DoLongRunningJobAsync);
                await Task.WhenAll(tasks);
                Console.WriteLine("All tasks executed");
            }

            Console.WriteLine("Using rx instead of await");
            Console.WriteLine("Enter to start");
            Console.ReadLine();
            //a different approach, using rx all the way
            var subx2 = jobResults.SubscribeConsole();
            jobs = new[] {"Finally understand backpropogation",
                          "Read the art of computer programming",
                          "Read Ulysses"};
            //Convert the task collection to an observable that completes when the
            //task processing finishes
            var completed = Observable.FromAsync(async () => {
                var tasks = jobs.Select(fromEventSample.DoLongRunningJobAsync);
                await Task.WhenAll(tasks);
            });

            //here, we subscribe to the task execution. When it finishes,
            //we need to be sure to unsubscribe from the jobResults object
            //to prevent memory leaks from the event handler reference
            completed.Subscribe(
                onNext: x => Console.WriteLine("All jobs executed"),
                onError: ex => Console.WriteLine(ex.ToString()),
                onCompleted: subx2.Dispose);
        }

        private static void SimpleGenerate() {
            Console.WriteLine("Demo - Simple generate");
            Console.WriteLine("Enter to start");
            Console.ReadLine();

            var obs = Observable.Generate(0,
            i => i < 10,
            i => i+ 1,
            i => Math.Pow(i, i));
            obs.SubscribeConsole();

        }

        private static void ObservableGenerateFromFile() {
            Console.WriteLine("Demo - generating observables from file strea");
            Console.WriteLine("Enter to start");
            Console.ReadLine();

            var fileReadSample = new FileReadSample();
            var linesSync = fileReadSample.GetLinesSync();
            linesSync.SubscribeConsole();

            Console.WriteLine("Demo - generating observables from file stream using async io");
            Console.WriteLine("Enter to start");
            Console.ReadLine();
            var linesAsync = fileReadSample.GetLinesAsync();
            linesAsync.SubscribeConsole();
        }

        private static void CancellationDemo()
        {
            NextDemo("Cancellation demo");
            var slowObservable = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
            var cts = new CancellationTokenSource();
            cts.Token.Register(() => Console.WriteLine("Cancelled!"));
            slowObservable.Subscribe(x => Console.WriteLine($"Tick {x * 500}"), cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            Thread.Sleep(5500);

        }

    }

    public static class Extension {
        public static IDisposable SubscribeConsole<T>(this IObservable<T> source) {
            return source.Subscribe(
                onNext: next => Console.WriteLine(next),
                onCompleted: () => Console.WriteLine("Observable Complete"),
                onError: ex => Console.WriteLine(ex));
        }
    }
}
