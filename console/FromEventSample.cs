using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace console
{
    public class SomeEvent : EventArgs
    {
        public string Message {get; set;}
        public int Data {get; set;}

    }

    public delegate void JobCompletedWithDuration(string jobName, int ms);
    public class FromEventSample
    {
        private event EventHandler<SomeEvent> _methodCalled;
        private event JobCompletedWithDuration _jobCompletedWithDuration = delegate {};
        private static Random _rando = new Random();
        public IObservable<SomeEvent> Messages
        {
            get
            {
                return Observable
                    .FromEventPattern<SomeEvent>(
                        h => this._methodCalled += h,
                        h => this._methodCalled -= h)
                    .Select(x => x.EventArgs);
            }
        }
        public IObservable<(string jobName, int ms)> LongRunningJobs
        {
            get
            {
                return Observable
                    .FromEvent<JobCompletedWithDuration, (string jobJame, int ms)>(
                        rxHandler => 
                            (job, time) => rxHandler((job, time)), //need to convert the two arguments into a single argument for rx
                        h => this._jobCompletedWithDuration += h,
                        h => this._jobCompletedWithDuration -= h)
                    .Select(x => x);
            }
        }

        public void DoSomeWork(string job)
        {
            var msg = $"Completed {job}";
            var data = _rando.Next(100);
            _methodCalled?.Invoke(
                this,
                new SomeEvent()
                {
                    Message = msg,
                    Data = data
                }
            );
        }

        public async Task DoLongRunningJobAsync(string job)
        {
            var toSleep = _rando.Next(50);
            await Task.Delay(toSleep);
            _jobCompletedWithDuration?.Invoke(job, toSleep);
        }

    }

}