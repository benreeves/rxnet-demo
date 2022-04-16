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
    public class LifestyleManagement
    {
        private Subject<Unit> _stop = new Subject<Unit>();
        public IObservable<long> TakeUntilElapsed(int seconds)
        {
            return Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(500))
                .TakeUntil(
                    Observable.Timer(TimeSpan.FromSeconds(seconds))
                );

        }
        public IObservable<long> TakeUntilMethodCalled()
        {
            return Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(500))
                .TakeUntil(_stop);
        }
        public IObservable<string> StartupSteps()
        {
            var steps = new[] {
                "Scaffold db",
                "Seed db",
                "Initialize app",
                "RUN",
                "RUN",
                "RUN",
                "CRASH",
                "Send crash report",
                "Reboot",
                "Initialize app",
                "RUN",
                "RUN",
                "RUN",
            };
            return steps.Select(x => 
                Observable.FromAsync(async () =>
                 { 
                     await Task.Delay(30);
                      return x;
                 }))
                .Concat();
        }

        public IObservable<string> FacebookMessages(){
            return Observable.Range(0, 10)
                .Select(x => Observable.FromAsync(
                    async () => 
                    { 
                        await Task.Delay(50);
                        return $"FB Message {x}"; 
                    }))
                .Concat();
        }
        public IObservable<string> TwitterMessages(){
            return Observable.Range(0, 10)
                .Select(x => Observable.FromAsync(
                    async () => 
                    { 
                        await Task.Delay(50);
                        return $"Twitter Message {x}"; 
                    }))
                .Concat();
        }
        public IObservable<string> AllMessages() {
            return Observable.Merge(
                FacebookMessages(),
                TwitterMessages());
        }

        public IObservable<string> SkipUntilStart()
        {
            var steps = StartupSteps()
                .Do(step => Console.WriteLine($"STEP: {step}"));
                

            return this.AllMessages()
                .SkipUntil( 
                    steps.Where(x => x == "RUN")
                );
        }
        public IObservable<string> SkipUntilStartWithRepeat()
        {
            var steps = StartupSteps()
                .Publish()
                .RefCount();
            steps.Do(step => Console.WriteLine($"STEP: {step}")).Subscribe();
            return this.AllMessages()
                .SkipUntil(steps.Where(x => x == "RUN"))
                .TakeUntil(steps.Where(x => x == "CRASH"))
                .Repeat();
        }

        public void StopTaking()
        {
            _stop.OnNext(Unit.Default);
        }

    }

}