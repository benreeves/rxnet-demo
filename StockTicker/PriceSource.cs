using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace StockTicker
{
    public class StockInfo
    {
        public double Price {get; set;}
        public DateTime Timestamp {get; set;}
        public string Ticker {get; set;}
    }
    public class PriceSource
    {
        private static Random _rnd = new Random();
        private Dictionary<string, BehaviorSubject<double>> lastPrices 
            = new Dictionary<string, BehaviorSubject<double>>();
        private Dictionary<string, IObservable<StockInfo>> observables 
            = new Dictionary<string, IObservable<StockInfo>>();
        public PriceSource()
        {
            
        }

        public IObservable<StockInfo> GetStreamFor(string ticker, double lastPrice)
        {
            var first = Observable.Return(new StockInfo() {
                Ticker = ticker,
                Timestamp = DateTime.UtcNow,
                Price = lastPrice
            });
            var lastPriceBh = new BehaviorSubject<double>(lastPrice);
            lastPrices[ticker] = lastPriceBh;
            //simulate data which comes in at quick intervals and potentially out of order
            var obs = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(20))
                .Select(_ => {
                    double centsToAdd = (double)_rnd.NextDouble() / 10;
                    return centsToAdd = _rnd.Next(0, 2) > 0 ? centsToAdd : -1 * centsToAdd;
                })
                .WithLatestFrom(lastPrices[ticker], (cents, last) => (cents, last))
                .Select(x => new StockInfo() {
                    Price = x.cents + x.last,
                    Ticker = ticker,
                    Timestamp = DateTime.UtcNow
                })
                .Do(x => lastPrices[ticker].OnNext(x.Price))
                .Publish()
                .RefCount();
            return obs;
        }
        public IObservable<StockInfo> GetStreamBetter(string ticker, double lastPrice)
        {
            if(!lastPrices.TryGetValue(ticker, out var bs)) {
                //Usually an operator which handles what you want to do!
                var obs = Observable
                    .Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(20))
                    .Select(_ => {
                        double centsToAdd = (double)_rnd.NextDouble() / 10;
                        return centsToAdd = _rnd.Next(0, 2) > 0 ? centsToAdd : -1 * centsToAdd;
                    })
                    .Scan(lastPrice, (price, cents) => price + cents)
                    .Select(x => new StockInfo() {
                        Price = x,
                        Ticker = ticker,
                        Timestamp = DateTime.UtcNow
                    })
                    .Publish()
                    .RefCount();
                this.observables[ticker] = obs;
            }
            return observables[ticker];
        }
    }
}
