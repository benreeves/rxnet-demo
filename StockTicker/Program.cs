using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using Google.Cloud.Firestore;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;

namespace StockTicker
{
    class Program
    {
        private static FirestoreDb _db = FirestoreDb.Create("rxnet-demo");
        private static PriceSource _priceSource = new PriceSource();
        private static Dictionary<string, IDisposable> _subscriptions = new Dictionary<string, IDisposable>();
        static async Task Main(string[] args)
        {
            var stocks = await GetStocks();
            foreach (var (id, ticker, price) in stocks)
            {
                WireStockToFirestore(id, ticker, price);
            }
            while(true) {
                Console.ReadLine();
            }
        }

        private static void WireStockToFirestore(string id, string ticker, double price)
        {
            var obs = _priceSource.GetStreamBetter(ticker, price)
                .Buffer(10, 1)
                .Select(stocks => {
                    var recent = stocks.Last();
                    var high = stocks.Max(x => x.Price);
                    var low = stocks.Min(x => x.Price);
                    return new StockModel() {
                        id = id,
                        timestamp = recent.Timestamp.ToString("o"),
                        name = recent.Ticker,
                        high = high,
                        low = low,
                        price = recent.Price
                    };
                })
                .Sample(TimeSpan.FromMilliseconds(350))
                .Select(x => Observable.FromAsync(async token => {
                    var docRef = _db.Collection("stocks").Document(id);
                    Dictionary<string, object> stock = new Dictionary<string, object>
                    {
                        { "name", x.name },
                        { "price", x.price },
                        { "high", x.high },
                        { "low", x.low },
                        { "timestamp", x.timestamp },
                    };
                    await docRef.SetAsync(stock, cancellationToken: token);
                    Console.WriteLine($"{x.timestamp} | {x.name} @ {x.price}");
                }))
                .Switch()
                // .Concat()
                .ObserveOn(TaskPoolScheduler.Default);
            var subx = obs.Subscribe(
                onNext: _ => {},
                onError: ex => Console.WriteLine(ex.ToString()));
            _subscriptions[ticker] = subx;
        }

        public static void StopListening(string ticker) {
            if (_subscriptions.TryGetValue(ticker, out var subx))
            {
                subx.Dispose();
            }
        }

        public static async Task<IEnumerable<(string id, string ticker, double price)>> GetStocks(){
            CollectionReference usersRef = _db.Collection("stocks");
            QuerySnapshot snapshot = await usersRef.GetSnapshotAsync();
            var stocks = new LinkedList<(string id, string ticker, double price)>();
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Console.WriteLine("Stock: {0}", document.Id);
                Dictionary<string, object> documentDictionary = document.ToDictionary();
                var ticker = (string)documentDictionary["name"];
                var price = double.Parse(documentDictionary["price"].ToString());
                var id = document.Id;
                stocks.AddLast((id, ticker, price));
                Console.WriteLine("Name: {0}", ticker);
                Console.WriteLine("Price: {0}", price);
                Console.WriteLine();
            }
            return stocks;
        }

    }
}
