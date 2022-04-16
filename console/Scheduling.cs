using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using System.Reactive;
using System.Threading;
using System.Reactive.Disposables;

namespace console
{
    public class Scheduling
    {

        public static void ScheduleWorkThreads() {
            Console.WriteLine($"Executing from thread {Thread.CurrentThread.ManagedThreadId}");
            Console.WriteLine("enter to continue");
            var currentThreadScheduler = CurrentThreadScheduler.Instance;
            currentThreadScheduler.Schedule(Unit.Default, TimeSpan.FromSeconds(1),
                (schd, _) => {
                        Console.WriteLine($"Doing work on thread {Thread.CurrentThread.ManagedThreadId}");
                        return Disposable.Empty;
                });

            var scheduler = NewThreadScheduler.Default;
            scheduler.Schedule(Unit.Default, TimeSpan.FromSeconds(2),
                (schd, _) => {
                        Console.WriteLine($"Doing work on thread {Thread.CurrentThread.ManagedThreadId}");
                        return Disposable.Empty;
                });
            Console.WriteLine("enter to continue");
            Console.ReadLine();
        }

    }
}