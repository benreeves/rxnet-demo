using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;

namespace console
{

    public class FileReadSample
    {
        private static string _samplePath;
        private static string _samplePath2;
        static FileReadSample()
        {
            GetFilePath();
        }

        private static void GetFilePath([CallerFilePath] string thisFilePath=null)
        {
            _samplePath = Path.Join(thisFilePath, "../sampletext.txt");
            _samplePath2 = Path.Join(thisFilePath, "../sampletext2.txt");
        }

        public IObservable<string> GetLinesSync()
        {
            var lines = Observable.Using(
                () => File.OpenText(_samplePath),
                stream => Observable.Generate(
                    stream,
                    s => !s.EndOfStream,
                    s => s,
                    s => s.ReadLine()
                )
            );
            return lines;
        }
        public IObservable<string> GetLinesAsync()
        {
            var lines = Observable.Using(
                () => File.OpenText(_samplePath2),
                stream => Observable.Generate(
                    stream,
                    s => !s.EndOfStream,
                    s => s,
                    s => s.ReadLineAsync()
                )
            );
            var ordered = lines
                .Select(x => Observable.FromAsync(() => x))
                .SelectMany(x => x);
            return ordered;
        }

    }

}