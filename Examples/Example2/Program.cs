﻿using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

//
// Example of downloading text from a URL using a promise.
//
namespace Example1
{
    class Program
    {
        static void Main(string[] args)
        {
            var running = true;

            Download("http://www.bugglebogglebazzooo.com")   // Schedule async operation, this time the URL is bad!
                .Catch(result => 
                {
                    Console.WriteLine("Async operation errored.");
                    Console.WriteLine(result);
                    running = false;
                });

            Console.WriteLine("Waiting");

            while (running)
            {
                Thread.Sleep(10);
            }

            Console.WriteLine("Exiting");
        }

        /// <summary>
        /// Download text from a URL.
        /// A promise is returned that is resolved when the download has completed.
        /// The promise is rejected if an error occurs during download.
        /// </summary>
        static IPromise<string> Download(string url)
        {
            var promise = new Promise<string>();
            using (var client = new WebClient())
            {
                client.DownloadStringCompleted +=
                    (s, ev) =>
                    {
                        if (ev.Error != null)
                        {
                            // Error during download, reject the promise.
                            promise.Reject(ev.Error);
                        }
                        else
                        {
                            // Downloaded completed successfully, resolve the promise.
                            promise.Resolve(ev.Result);
                        }
                    };

                client.DownloadStringAsync(new Uri(url), null);
            }
            return promise;
        }
    }
}