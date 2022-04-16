using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace console
{
    public class SubjectBasics
    {
        public static void PassingDataToSubject() {
            var subject = new Subject<string>();
            using (subject.SubscribeConsole())
            {
                subject.OnNext("Hi there");
                subject.OnNext("I'm getting data");
                subject.OnError(new ApplicationException("Whoops error occured"));
                subject.OnNext("Trying to send more data");
            }

            var subject2 = new Subject<string>();
            var subx1 = subject2.SubscribeConsole();
            subject2.OnNext("Second subject");
            subject2.OnNext("Creating a new subscription, will not get old messages!");
            Console.WriteLine("Enter to continue");
            Console.ReadLine();
            var subx2 = subject2.SubscribeConsole();
            subject2.OnNext("Two messages this time");

            subx1.Dispose();
            subx2.Dispose();

            Console.WriteLine("Enter to continue");
            Console.ReadLine();
        }

        internal static void UsingAsyncSubjects()
        {
            Console.WriteLine("Async subject passes last and only last to subscriber");
            var asyncSubject = new AsyncSubject<string>();
            asyncSubject.SubscribeConsole();
            asyncSubject.OnNext("First data");
            asyncSubject.OnNext("Are you receiving?");
            asyncSubject.OnNext("Helloooo out there");
            asyncSubject.OnNext("Last effort before I give up");
            Console.ReadLine();
            asyncSubject.OnCompleted();
            Console.WriteLine("Create a new subscription, get same last value");
            Console.WriteLine("Enter to continue");
            Console.ReadLine();
            asyncSubject.SubscribeConsole();
            Console.ReadLine();
        }

        public static void BehaviorSubject()
        {
            var subject = new BehaviorSubject<string>("initial seed");
            using (subject.SubscribeConsole())
            {
                subject.OnNext("Hi there");
                subject.OnNext("I'm getting data");
            }
            Console.WriteLine("Creating a new subscription to bs, gets last emission");
            Console.WriteLine("Enter to continue");
            Console.ReadLine();
            using (subject.SubscribeConsole())
            {
                subject.OnNext("Killing with exception");
                subject.OnError(new ApplicationException("Killing bh"));
            }
            Console.WriteLine("Creating a new subscription to bs just gets error");
            Console.WriteLine("Enter to continue");
            Console.ReadLine();
            using (subject.SubscribeConsole()){};
            Console.WriteLine("Enter to continue");
            Console.ReadLine();

        }

        public static void ReplaySubjectExample()
        {
            Console.WriteLine("Replay subject demo - caching up to 10 of last values");
            Console.WriteLine("Enter to continue");
            Console.ReadLine();
            var replay = new ReplaySubject<int>(bufferSize: 5, window: TimeSpan.FromMinutes(1));
            var obs = Observable.Range(0, 10);
            obs.Subscribe(replay);
            replay.SubscribeConsole();
            Console.WriteLine("Enter to continue");
            Console.ReadLine();
        }

        public static void CommonPitfall() {

            Console.WriteLine("Trying to merge with a subject");
            var subject = new Subject<long>();

            var first = Observable.Range(0, 10).Select(x => (long)x);

            var second = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(250))
                .Select(x => 250 * x);
                
            subject.SubscribeConsole();

            second.Subscribe(subject);
            Console.WriteLine("Subscribing to first (continuous timer)");
            Thread.Sleep(500);
            Console.WriteLine("Subscribing to second (finite observable)");
            Thread.Sleep(1000);
            first.Subscribe(subject);

            Console.ReadLine();
            Console.WriteLine("Proper usage with merge");
            var subj2 = new Subject<long>();
            var subx = subj2.SubscribeConsole();
            first.Merge(second).Subscribe(subj2);
            Thread.Sleep(3000);
            subx.Dispose();
            Console.WriteLine("Enter to continue");
            Console.ReadLine();

        }

    }

}